import { Injectable, OnDestroy } from '@angular/core';
import { BrimboriumMessageFlowApiService } from '../message-flow-api/services';
import { Subscription, BehaviorSubject, connectable, filter, switchMap, map, merge, mergeMap, combineLatest } from 'rxjs'
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { RequestVersioned, ValueVersioned, mergeNextValueVersioned, mergeValueVersioned, toReturnFailedValue, toReturnOkValue, toValueVersioned } from 'src/app/utils/ValueVersioned';
import { MessageFlowGraph } from '../message-flow-api/models';
import { emptyMessageFlowGraph } from '../message-flow-api/models/message-flow-graph';
@Injectable({
  providedIn: 'root'
})
export class MessageFlowService implements OnDestroy {
  private subscription = new Subscription();
  public triggerLoadNames$ = new PropertySubject<string | null>(null);
  public listMessageFlowName$ = new PropertySubject<string[]>([]);

  public triggerLoadMessageFlow$ = new PropertySubject<string[] | null>(null);
  public mapMessageFlow$ = new PropertySubject<Map<string, ValueVersioned<MessageFlowGraph>>>(new Map());

  constructor(
    public messageFlowApiService: BrimboriumMessageFlowApiService
  ) {

    this.subscription.add(
      this.triggerLoadNames$.pipe(
        filter(t => t.logicalTimestamp !== 0),
        switchMap((trigger) => {
          return this.messageFlowApiService.getListMessageFlowName$Response({}).pipe(
            map((response) => {
              if (response.ok && response.body) {
                return toReturnOkValue(response.body, trigger.logicalTimestamp, trigger);
              } else {
                return toReturnFailedValue(response, trigger.logicalTimestamp, trigger);
              }
            })
          );
        })
      ).subscribe({
        next: (value) => {
          if (value.mode === 'ok') {
            const listMessageFlowNameSorted = value.value.sort();
            this.setListMessageFlowName(
              toValueVersioned(listMessageFlowNameSorted, 0, value.logicalTimestamp)
            );
          }
        }
      })
    );

    this.subscription.add(
      this.triggerLoadMessageFlow$.pipe(
        filter(t => t.logicalTimestamp !== 0),
        switchMap((trigger) => {
          const listTriggers = (trigger.value || []).map(
            name => toValueVersioned(name, 0, trigger.logicalTimestamp)
          );

          const obsLoad = listTriggers.map(
            (trigger) => this.messageFlowApiService.getMessageFlowGraph$Response({ name: trigger.value }).pipe(
              map((response) => {
                if (response.ok && response.body) {
                  return toReturnOkValue(response.body, trigger.logicalTimestamp, trigger);
                } else {
                  return toReturnFailedValue(response, trigger.logicalTimestamp, trigger);
                }
              })
            )
          );
          const result = combineLatest(obsLoad);
          return result;
        })
      ).subscribe({
        next: (listValue) => {
          const currentVV = this.mapMessageFlow$.getValue();
          const nextValue = new Map(currentVV.value);
          let logicalTimestamp = 0;
          for (const value of listValue) {
            if (value.mode == "ok") {
              const name = value.request?.value;
              if (!name) { continue; }
              const currentItem = nextValue.get(name) || toValueVersioned<MessageFlowGraph>(emptyMessageFlowGraph());
              nextValue.set(name, mergeValueVersioned(currentItem, value.value, 0, value.logicalTimestamp));
              if (logicalTimestamp < value.logicalTimestamp) {
                logicalTimestamp = value.logicalTimestamp;
              }
            }
          }
          if (logicalTimestamp > 0) {
            this.mapMessageFlow$.next(mergeValueVersioned(currentVV, nextValue, 0, logicalTimestamp));
          }
        }
      }));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  setListMessageFlowName(listMessageFlowName: ValueVersioned<string[]>) {
    if (listMessageFlowName === this.listMessageFlowName$.getValue()) {
      // skip
    } else {
      this.listMessageFlowName$.next(listMessageFlowName);
    }
  }

  public load(triggerReason: string | null) {
    const currentValue = this.triggerLoadNames$.value;
    this.triggerLoadNames$.next(toValueVersioned(triggerReason, 0, currentValue.logicalTimestamp + 1));
    // this.triggerLoadNames$.next(this.triggerLoadNames$.value + 1);
  }
}

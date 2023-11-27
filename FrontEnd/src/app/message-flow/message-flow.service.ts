import { Injectable } from '@angular/core';
import { BrimboriumMessageFlowApiService } from '../message-flow-api/services';
import { BehaviorSubject, connectable, filter, switchMap, map } from 'rxjs'
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { ValueVersioned, toReturnFailedValue, toReturnOkValue, toValueVersioned } from 'src/app/utils/ValueVersioned';
@Injectable({
  providedIn: 'root'
})
export class MessageFlowService {
  public triggerLoadNames$ = new PropertySubject<string|null>(null);
  public listMessageFlowName$ = new PropertySubject<string[]>([]);

  constructor(
    public messageFlowApiService: BrimboriumMessageFlowApiService
  ) {
here
    this.triggerLoadNames$.pipe(
      filter(t => t.logicalTimestamp !== 0),
      switchMap((trigger) => {
        return this.messageFlowApiService.getListMessageFlowName$Response({}).pipe(
          map((response)=>{
            if (response.ok && response.body) {
              return toReturnOkValue(response.body, 0, trigger);
            } else{
              return toReturnFailedValue(response, 0, trigger);
            }
          })
        );
      }),
      map((value) => {
        if (value.mode==='ok') {
          const listMessageFlowNameSorted = value.value.sort();
          this.setListMessageFlowName(
            toValueVersioned(listMessageFlowNameSorted, 0, value.logicalTimestamp)
            );
        }
        return value;
      })
    );
  }

  setListMessageFlowName(listMessageFlowName: ValueVersioned<string[]>) {
    if (listMessageFlowName === this.listMessageFlowName$.getValue()) {
      // skip
    } else {
      this.listMessageFlowName$.next(listMessageFlowName);
    }
  }

  public load(triggerReason:string|null) {
    const currentValue = this.triggerLoadNames$.value;
    this.triggerLoadNames$.next(toValueVersioned(triggerReason, 0, currentValue.logicalTimestamp+1));
    // this.triggerLoadNames$.next(this.triggerLoadNames$.value + 1);
  }
}

import { CdkDragEnd } from '@angular/cdk/drag-drop';
import { Component, ElementRef, OnInit, OnDestroy, ViewChild, afterNextRender } from '@angular/core';
import { Subscription, BehaviorSubject, map, tap, combineLatest, merge, debounceTime, filter, switchMap, distinctUntilChanged } from 'rxjs';
import { CdkDrag } from '@angular/cdk/drag-drop';
import { WindowSizeService } from 'src/app/utils/window-size.service';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { toValueVersioned } from 'src/app/utils/ValueVersioned';
import { MessageFlowService } from 'src/app/message-flow/message-flow.service';
import { MessageFlowGraph, MessageGraphNode } from 'src/app/message-flow-api/models';

@Component({
  selector: 'app-monitor',
  templateUrl: './monitor.component.html',
  styleUrls: ['./monitor.component.scss'],
})
export class MonitorComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  public topRightWidth$ = new BehaviorSubject<number>(200);
  public topRightWidthPx$ = new BehaviorSubject<string>("100px");

  public bottomHeight$ = new BehaviorSubject<number>(200);
  public bottomHeightPx$ = new BehaviorSubject<string>("100px");

  public leftPaneSize$ = new BehaviorSubject<{ width: number; height: number }>({ width: 0, height: 0 });

  public listMessageFlowName$ = new BehaviorSubject<string[]>([]);
  public currentMessageFlowName$ = new BehaviorSubject<string | null>(null);
  public currentMessageFlow$ = new PropertySubject<MessageFlowGraph | null>(null);
  public currentMessageFlowNode$ = new BehaviorSubject<MessageGraphNode | null>(null);

  @ViewChild('leftpane') leftpaneRef: ElementRef | null = null;

  constructor(
    private windowSizeService: WindowSizeService,
    private messageFlowService: MessageFlowService
  ) {
    this.subscription.add(
      this.topRightWidth$.pipe(
        map((value) => `${value}px`)
      ).subscribe(this.topRightWidthPx$));
    this.subscription.add(
      this.bottomHeight$.pipe(
        map((value) => `${value}px`)
      ).subscribe(this.bottomHeightPx$));

    afterNextRender(() => {
      //this.leftPaneSize$.next(this.getLeftPaneSize());
      this.subscription.add(
        merge(
          windowSizeService.windowSize$, // .pipe(tap(() => console.log("windowSize"))),
          this.topRightWidthPx$, // .pipe(tap(() => console.log("topRightWidthPx"))),
          this.bottomHeightPx$, // .pipe(tap(() => console.log("bottomHeightPx"))),
        ).pipe(
          debounceTime(1),
          map(() => { return this.getLeftPaneSize(); })
        ).subscribe(this.leftPaneSize$));
    });
  }
  ngOnInit(): void {
    this.subscription.add(
      this.messageFlowService.mapMessageFlow$.pipe(
        map((mapMessageFlowVV) => {
          const keys = Array.from(mapMessageFlowVV.value.keys());
          return keys;
        })
      ).subscribe(this.listMessageFlowName$)
    );
    this.subscription.add(
      combineLatest({
        currentMessageFlowName: this.currentMessageFlowName$,
        listMessageFlowName: this.listMessageFlowName$
      })
        .pipe(
          filter(({ currentMessageFlowName, listMessageFlowName }) => !currentMessageFlowName && listMessageFlowName.length > 0)
        ).subscribe({
          next: ({ currentMessageFlowName, listMessageFlowName }) => {
            this.currentMessageFlowName$.next(listMessageFlowName[0]);
          }
        })
    );
    this.subscription.add(
      combineLatest({
        currentMessageFlowName: this.currentMessageFlowName$,
        mapMessageFlowVV: this.messageFlowService.mapMessageFlow$.pipe(
          distinctUntilChanged((a, b) => a.valueVersion == b.valueVersion)
        )
      })
        .pipe(
          map(({ currentMessageFlowName, mapMessageFlowVV }) => {
            // console.log("currentMessageFlowName", currentMessageFlowName);
            // console.log("mapMessageFlowVV", mapMessageFlowVV);
            if (currentMessageFlowName) {
              const resultVV = mapMessageFlowVV.value.get(currentMessageFlowName);
              if (resultVV) {
                console.log("found", resultVV)
                return resultVV;
              }
            }
            // console.log("not found")
            return toValueVersioned<MessageFlowGraph | null>(null);
          })
        ).subscribe(this.currentMessageFlow$));
    this.load();
  }

  load(): void {
    const rq = toValueVersioned("load", 0, (new Date()).getTime());
    const s = new Subscription();
    this.subscription.add(s);
    s.add(
      this.messageFlowService.listMessageFlowName$.pipe(
        filter((vvListName) => vvListName.logicalTimestamp >= rq.logicalTimestamp)
      ).subscribe({
        next: (vvListName) => {
          this.messageFlowService.triggerLoadMessageFlow$.next(toValueVersioned(vvListName, 0, rq.logicalTimestamp));
        }
      }));
    this.messageFlowService.triggerLoadNames$.next(rq);
  }

  getLeftPaneSize() {
    const div = this.leftpaneRef?.nativeElement as (HTMLDivElement | undefined);
    if (div) {
      var rect = div.getBoundingClientRect();
      return ({ width: rect.width, height: rect.height })
    } else {
      return ({ width: 0, height: 0 });
    }
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  splitterTopRightEnded($event: CdkDragEnd) {
    const nextValue = this.topRightWidth$.value - $event.distance.x;
    $event.source.reset();
    this.topRightWidth$.next(nextValue);
  }

  splitterBottomEnded($event: CdkDragEnd) {
    const nextValue = this.bottomHeight$.value - $event.distance.y;
    this.bottomHeight$.next(nextValue);
    $event.source.reset();
  }

  nodeSelectedDiagramPane(data: MessageGraphNode | undefined) {
    this.currentMessageFlowNode$.next(data || null);
  }

}

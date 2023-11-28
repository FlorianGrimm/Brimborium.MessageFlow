import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subscription, combineLatest, map } from 'rxjs';
import { MessageFlowGraph, MessageGraphNode } from 'src/app/message-flow-api/models';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { ValueVersioned, toValueVersioned } from 'src/app/utils/ValueVersioned';

type TProperty = {
  name: string;
  value: string;
};
@Component({
  selector: 'app-property-pane',
  templateUrl: './property-pane.component.html',
  styleUrls: ['./property-pane.component.scss']
})
export class PropertyPaneComponent implements OnInit, OnDestroy {

  private subscription = new Subscription();

  public currentMessageFlow$ = new PropertySubject<MessageFlowGraph | null>(null);
  public currentMessageFlowNode$ = new BehaviorSubject<MessageGraphNode | null>(null);

  public properties$ = new BehaviorSubject<TProperty[]>([]);

  @Input()
  set currentMessageFlow(value: ValueVersioned<MessageFlowGraph | null> | null) {
    if (value === null || value.value === null) {
      this.currentMessageFlow$.next(toValueVersioned<MessageFlowGraph | null>(null));
    } else {
      this.currentMessageFlow$.next(value);
    }
  }

  @Input()
  set currentMessageFlowNode(value: MessageGraphNode | null) {
    this.currentMessageFlowNode$.next(value || null);
  }

  public displayedColumns: string[] = ['name', 'value'];

  constructor() {
    this.subscription.add(
      combineLatest({
        currentMessageFlow: this.currentMessageFlow$,
        currentMessageFlowNode: this.currentMessageFlowNode$
      })
        .pipe(
          map(({ currentMessageFlow, currentMessageFlowNode
          }) => {
            const result: TProperty[] = [];
            if (currentMessageFlowNode) {
              result.push({ name: "Name", value: currentMessageFlowNode.nameId || "" });
              result.push({ name: "Order", value: currentMessageFlowNode.order.toString() || "" });
              // result.push({ name: "b", value: "" });
              // result.push({ name: "c", value: "" });
              // result.push({ name: "d", value: "" });
            } else if (currentMessageFlow.value && currentMessageFlow.value.listNode) {
              for(const node of currentMessageFlow.value.listNode){
                result.push({ name: node.nameId || "", value: node.order.toString() || "" });
              }

            }

            return result;
          })
        ).subscribe(this.properties$));
  }

  ngOnInit(): void {
  }
  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}

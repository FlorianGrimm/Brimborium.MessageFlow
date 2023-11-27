import { AfterViewInit, Component, ElementRef, Input, NgZone, OnDestroy, OnInit } from '@angular/core';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { Subscription} from 'rxjs';
import { toValueVersioned,ValueVersioned } from 'src/app/utils/ValueVersioned';
export type Size = { width: number; height: number }

@Component({
  selector: 'app-diagram-pane',
  templateUrl: './diagram-pane.component.html',
  styleUrls: ['./diagram-pane.component.scss']
})
export class DiagramPaneComponent implements OnInit, OnDestroy, AfterViewInit  {
  private subscription = new Subscription();
  public hostSize$ = new PropertySubject<Size>({ width: 0, height: 0 });

  @Input()
  set hostSize(value: ValueVersioned<Size>|Size) {
    this.hostSize$.nextIfChanged(toValueVersioned(value));
  }

  constructor(
  ) {
  }

  ngOnInit(): void {
  }

  ngAfterViewInit(): void {
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}

import { CdkDragEnd } from '@angular/cdk/drag-drop';
import { Component, OnDestroy } from '@angular/core';
import { Subscription, BehaviorSubject, map } from 'rxjs';
import { CdkDrag } from '@angular/cdk/drag-drop';
import { WindowSizeService } from 'src/app/utils/window-size.service';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { toValueVersioned } from 'src/app/utils/ValueVersioned';

@Component({
  selector: 'app-monitor',
  templateUrl: './monitor.component.html',
  styleUrls: ['./monitor.component.scss'],
})
export class MonitorComponent implements OnDestroy {
  private subscription = new Subscription();

  public topRightWidth$ = new BehaviorSubject<number>(200);
  public topRightWidthPx$ = new BehaviorSubject<string>("100px");

  public bottomHeight$ = new BehaviorSubject<number>(200);
  public bottomHeightPx$ = new BehaviorSubject<string>("100px");

  constructor(
    private windowSizeService: WindowSizeService
  ) {
    this.subscription.add(
      this.topRightWidth$.pipe(
        map((value) => `${value}px`)
      ).subscribe(this.topRightWidthPx$));
    this.subscription.add(
      this.bottomHeight$.pipe(
        map((value) => `${value}px`)
      ).subscribe(this.bottomHeightPx$));

    this.subscription.add(
      windowSizeService.windowSize$.subscribe({
        next: (size) => {

        }
      }));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  splitterTopRightEnded($event: CdkDragEnd) {
    const nextValue = this.topRightWidth$.value - $event.distance.x;
    this.topRightWidth$.next(nextValue);
    $event.source.reset();
  }


  splitterBottomEnded($event: CdkDragEnd) {
    const nextValue = this.bottomHeight$.value - $event.distance.y;
    this.bottomHeight$.next(nextValue);
    $event.source.reset();
  }
}

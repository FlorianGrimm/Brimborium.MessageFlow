import { CdkDragEnd } from '@angular/cdk/drag-drop';
import { Component } from '@angular/core';
import {CdkDrag} from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-monitor',
  templateUrl: './monitor.component.html',
  styleUrls: ['./monitor.component.scss'],
})
export class MonitorComponent {
  public rightWidth = 100;
  public rightWidthPx='100px';
  splitterEnded($event: CdkDragEnd) {
    this.rightWidth -= $event.distance.x;
    this.rightWidthPx = `${this.rightWidth}px`
    $event.source.reset();
  }

}

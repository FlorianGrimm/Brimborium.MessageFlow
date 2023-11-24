import { NgModule } from '@angular/core';
import { BrimboriumMessageFlowComponent } from './brimborium-message-flow.component';
import { DiagramPaneComponent } from './diagram-pane/diagram-pane.component';
import { PropertyPaneComponent } from './property-pane/property-pane.component';

@NgModule({
  declarations: [
    BrimboriumMessageFlowComponent,
    DiagramPaneComponent,
    PropertyPaneComponent
  ],
  imports: [
  ],
  exports: [
    BrimboriumMessageFlowComponent,
    DiagramPaneComponent,
    PropertyPaneComponent
  ]
})
export class BrimboriumMessageFlowModule { }

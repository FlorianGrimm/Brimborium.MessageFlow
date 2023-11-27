import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { AppComponent } from './app.component';
import { HomeComponent } from './page/home/home.component';
import { MonitorComponent } from './page/monitor/monitor.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MessageFlowApiModule } from './message-flow-api/message-flow-api.module';
import { DiagramPaneComponent } from './message-flow/diagram-pane/diagram-pane.component';
import { PropertyPaneComponent } from './message-flow/property-pane/property-pane.component';
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    MonitorComponent,
    DiagramPaneComponent,
    PropertyPaneComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    HttpClientModule,
    MessageFlowApiModule.forRoot({ rootUrl: '' }),
    MatDividerModule,
    MatIconModule,
    MatMenuModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    DragDropModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }

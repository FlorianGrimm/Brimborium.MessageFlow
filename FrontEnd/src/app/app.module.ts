import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import {DragDropModule} from '@angular/cdk/drag-drop';

import { AppComponent } from './app.component';
import { HomeComponent } from './page/home/home.component';
import { MonitorComponent } from './page/monitor/monitor.component';
import { BrimboriumMessageFlowModule } from 'projects/brimborium-message-flow/src/public-api';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

//C:\github.com\FlorianGrimm\Brimborium.MessageFlow\FrontEnd\projects\brimborium-message-flow\src\lib\
//import { API_BASE_URL } from 'projects/brimborium-message-flow/src/lib/brimborium-message-flow-client.ts';
//import { API_BASE_URL } from '../';
//import { API_BASE_URL } from 'Brimborium-MessageFlow';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    MonitorComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrimboriumMessageFlowModule,
    BrowserAnimationsModule,
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

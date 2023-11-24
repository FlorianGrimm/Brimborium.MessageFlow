import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './page/home/home.component';
import { MonitorComponent } from './page/monitor/monitor.component';
import { BrimboriumMessageFlowModule } from 'projects/brimborium-message-flow/src/public-api';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    MonitorComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrimboriumMessageFlowModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }

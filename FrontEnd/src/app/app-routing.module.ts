import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './page/home/home.component';
import { MonitorComponent } from './page/monitor/monitor.component';

const routes: Routes = [
  {path:"", component: HomeComponent},
  {path:"monitor", component: MonitorComponent}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }

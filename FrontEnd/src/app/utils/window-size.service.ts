import { HostListener, Injectable } from '@angular/core';
import { PropertySubject } from './PropertySubject';
import { toValueVersioned } from './ValueVersioned';

export type WindowSize={
  innerWidth:number;
  innerHeight:number;
}

@Injectable({
  providedIn: 'root'
})
export class WindowSizeService {
  public windowSize$ = new PropertySubject<WindowSize>({innerWidth:0, innerHeight:0});

  constructor() {
    this.onResize(undefined);
  }

  @HostListener('window:resize', ['$event'])
  onResize(event:UIEvent|undefined) {
    if (window){
      this.windowSize$.nextIfChanged(toValueVersioned({
        innerWidth:window.innerWidth,
        innerHeight:window.innerHeight
      }));
    }
  }
}

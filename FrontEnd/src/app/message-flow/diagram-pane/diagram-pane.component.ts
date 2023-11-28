import { AfterViewInit, Component, ElementRef, Input, NgZone, OnDestroy, OnInit, ViewChild, afterNextRender } from '@angular/core';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { BehaviorSubject, Subscription, filter } from 'rxjs';
import { toValueVersioned, ValueVersioned } from 'src/app/utils/ValueVersioned';
import * as d3 from 'd3';
export type Size = { width: number; height: number }

@Component({
  selector: 'app-diagram-pane',
  templateUrl: './diagram-pane.component.html',
  styleUrls: ['./diagram-pane.component.scss']
})
export class DiagramPaneComponent implements OnInit, OnDestroy, AfterViewInit {
  private subscription = new Subscription();
  public hostSize$ = new BehaviorSubject<Size>({ width: 0, height: 0 });
  @ViewChild('diagram') diagramRef: ElementRef | null = null;

  @Input()
  set hostSize(value: Size | null) {
    if (value === null) {
      console.log("hostSize", "no value");
    } else if (value.width <= 0 && value.height <= 0) {
      console.log("hostSize", "0 value");
    } else {
      console.log("hostSize", "a value");
      const currentValue = this.hostSize$.value;
      if (currentValue.width == value.width && currentValue.height === value.height){
        // skip
      } else {
        this.hostSize$.next(value);
      }
    }
  }

  constructor(
  ) {
    //afterNextRender(() => {    });
  }

  svgD3: d3.Selection<SVGSVGElement, unknown, null, undefined> | undefined;
  initDiagram() {
    if (this.svgD3 !== undefined) { return; }
    const div = this.diagramRef?.nativeElement as (HTMLDivElement | null);
    if (!div) {
      console.log("no div");
      return;
    }
    const svgD3 = d3.select(div).append('svg').attr("class", "diagram");
    this.svgD3 = svgD3;
    const size = this.hostSize$.getValue();
    console.log("added svg", size);

    const coordinateGD3 = svgD3.append("g");

    const coordinateGD3V = coordinateGD3.append("line").attr("class", "lineCoordinate");
    const sizeCoordinateGD3V = (size:Size) => coordinateGD3V.attr("x1", size.width/2).attr("y1", 0).attr("x2", size.width/2).attr("y2", size.height);
    sizeCoordinateGD3V(size);

    const coordinateGD3H = coordinateGD3.append("line").attr("class", "lineCoordinate");
    const sizeCoordinateGD3H = (size:Size) => coordinateGD3H.attr("x1", 0).attr("y1", size.height/2).attr("x2", size.width).attr("y2", size.height/2);
    sizeCoordinateGD3H(size);


    this.subscription.add(
      this.hostSize$.subscribe({
        next: (size) => {
          console.log("new size", size);
          sizeCoordinateGD3V(size);
          sizeCoordinateGD3H(size);
        }
      }));
  }



  ngOnInit(): void {
  }

  ngAfterViewInit(): void {
    const s=new Subscription();
    this.subscription.add(s);
    s.add(
      this.hostSize$.pipe(filter(size=>size.width>0 && size.height>0)).subscribe({
        next:()=>{
          this.initDiagram();
          s.unsubscribe();
        }}));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}

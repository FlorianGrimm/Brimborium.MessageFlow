import { AfterViewInit, Component, ElementRef, EventEmitter, Input, NgZone, OnDestroy, OnInit, Output, ViewChild, afterNextRender } from '@angular/core';
import { PropertySubject } from 'src/app/utils/PropertySubject';
import { BehaviorSubject, Subscription, combineLatest, empty, filter } from 'rxjs';
import { toValueVersioned, ValueVersioned } from 'src/app/utils/ValueVersioned';
import * as d3 from 'd3';
import { MessageFlowGraph, MessageGraphConnection, MessageGraphNode } from 'src/app/message-flow-api/models';
import { emptyMessageFlowGraph } from 'src/app/message-flow-api/models/message-flow-graph';
export type Size = { width: number; height: number }

/*

export type TGraphNode = {
  name: string;
  isSource?: boolean;
  isSink?: boolean;
  listSink?: { listSource: TId[] }[] | undefined | null;
}
*/
export type TId = string;
export type TGraphNode = {
  id: TId;
  isFixed: boolean;
  data: MessageGraphNode;
}
export type TGraphNodeSimulation = TGraphNode & d3.SimulationNodeDatum;


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
      // skip trash
    } else if (value.width <= 0 && value.height <= 0) {
      // skip trash
    } else {
      const currentValue = this.hostSize$.value;
      if (currentValue.width == value.width && currentValue.height === value.height) {
        // skip no change
      } else {
        this.hostSize$.next(value);
      }
    }
  }

  public graph$ = new PropertySubject<MessageFlowGraph | null>(null);
  @Input()
  set graph(value: ValueVersioned<MessageFlowGraph | null> | null) {
    if (value === null || value.value === null) {
      this.graph$.nextIfChanged(toValueVersioned(null, 0, 0));
    } else {
      this.graph$.nextIfChanged(value);
    }
  }

  @Output()
  public nodeSelected = new EventEmitter<MessageGraphNode>();

  constructor(
  ) {
    //afterNextRender(() => {    });
  }

  graphD3: {
    svgD3: d3.Selection<SVGSVGElement, unknown, null, undefined>;
    linkSelected: d3.Selection<SVGGElement, unknown, null, undefined>;
    nodeSelected: d3.Selection<SVGGElement, unknown, null, undefined>;
  } | undefined;
  simulation: d3.Simulation<TGraphNodeSimulation, d3.SimulationLinkDatum<TGraphNodeSimulation>> | undefined;

  initDiagram() {
    if (this.graphD3 !== undefined) {
      return this.graphD3;
    }

    const div = this.diagramRef?.nativeElement as (HTMLDivElement | null);
    if (!div) { return undefined; }

    const size = this.hostSize$.getValue();

    const svgD3 = d3.select(div).append('svg').attr("class", "diagram");

    const setSizeSvg = (size: Size) => {
      svgD3.attr("viewBox", [-size.width / 2, -size.height / 2, size.width, size.height])
    };
    setSizeSvg(size);

    const coordinateGD3 = svgD3.append("g");

    const coordinateGD3V = coordinateGD3.append("line").attr("class", "lineCoordinate");
    const setSizeCoordinateGD3V = (size: Size) => coordinateGD3V.attr("x1", size.width / 2).attr("y1", 0).attr("x2", size.width / 2).attr("y2", size.height);
    setSizeCoordinateGD3V(size);

    const coordinateGD3H = coordinateGD3.append("line").attr("class", "lineCoordinate");
    const setSizeCoordinateGD3H = (size: Size) => coordinateGD3H.attr("x1", 0).attr("y1", size.height / 2).attr("x2", size.width).attr("y2", size.height / 2);
    setSizeCoordinateGD3H(size);

    // Append links.
    const linkSelected = svgD3.append("g")
      .attr("stroke", "#999")
      .attr("stroke-opacity", 0.6)
    //.selectAll("line");

    // Append nodes.
    const nodeSelected = svgD3.append("g")
      .attr("fill", "#fff")
      .attr("stroke", "#000")
      .attr("stroke-width", 1.5)
    //.selectAll("g.node");

    const resultGraphD3 = this.graphD3 = { svgD3: svgD3, linkSelected: linkSelected, nodeSelected: nodeSelected };

    {
      const listNode: TGraphNodeSimulation[] = [
        {
          id: "load",
          data: {
            nameId: "load",
            order: 1,
            listIncomingSinkId: [],
            listOutgoingSourceId: ["ing..."],
            listChildren: []
          },
          isFixed: false
        },
        {
          id: "ing...",
          data: {
            nameId: "ing...",
            order: 2,
            listIncomingSinkId: ["load"],
            listOutgoingSourceId: [],
            listChildren: []
          },
          isFixed: false
        }
      ];
      const listLinks: d3.SimulationLinkDatum<TGraphNodeSimulation>[] = [
        {
          source: "load",
          target: "ing..."
        }
      ];
      this.createSimulation(listNode, listLinks);
    }
    this.subscription.add(
      {
        unsubscribe: () => {
          this.simulation?.stop();
        }
      }
    );

    this.subscription.add(
      this.hostSize$.subscribe({
        next: (size) => {
          setSizeSvg(size);
          setSizeCoordinateGD3V(size);
          setSizeCoordinateGD3H(size);
        }
      }));

    this.subscription.add(
      this.graph$.subscribe({
        next: (graphVV) => {
          const graph = (graphVV.value) ? graphVV.value : emptyMessageFlowGraph();
          const nextListConnection: MessageGraphConnection[] = graph.listConnection || [];
          const nextListNode: MessageGraphNode[] = graph.listNode || [];
          if (0 === nextListNode.length && 0 === nextListConnection.length) {
            return;
          }
          const nextListGraphNode: TGraphNodeSimulation[] = nextListNode.map((node, index) => {
            const result: TGraphNodeSimulation = {
              id: node.nameId || "",
              isFixed: false,
              data: node,
            };
            return result;
          })
          console.log("nextListGraphNode", nextListGraphNode);
          const nextListLink = nextListConnection.map((connection) => {
            const result: d3.SimulationLinkDatum<TGraphNodeSimulation> = {
              source: connection.sourceNodeId || "",
              target: connection.sinkNodeId || ""
            };
            return result;
          }).filter(link => link.source !== "" && link.target !== "");
          console.log("nextListLink", nextListLink);

          this.createSimulation(nextListGraphNode, nextListLink);
          // TODO svgD3.data(listNode)
          // https://observablehq.com/@d3/modifying-a-force-directed-graph
        }
      }));
    return resultGraphD3;
  }
  createSimulation(
    listNode: TGraphNodeSimulation[],
    listLinks: d3.SimulationLinkDatum<TGraphNodeSimulation>[]
  ) {
    const graphD3 = this.initDiagram();
    if (graphD3 === undefined) { return; }
    const {
      svgD3
      , linkSelected
      , nodeSelected
    } = graphD3;
    nodeSelected.selectAll("g.node").remove();
    linkSelected.selectAll("line.link").remove();

    this.simulation?.stop();
    this.simulation = undefined;
    let minOrder = listNode.length * 2;
    let maxOrder = 0;
    for (const node of listNode) {
      if (minOrder < node.data.order) { minOrder = node.data.order; }
      if (node.data.order < maxOrder) { maxOrder = node.data.order; }
    }
    if (maxOrder < minOrder) { maxOrder = minOrder + 1; }
    let orderDiff = maxOrder - minOrder;
    let orderScale = 0.2 * (1 / orderDiff);

    const simulation = d3.forceSimulation<
      TGraphNodeSimulation,
      d3.SimulationLinkDatum<TGraphNodeSimulation>
    >(listNode)
      .force("charge", d3.forceManyBody().strength(-2000))
      .force("x", d3.forceX().strength((node) => {
        var graphNode = node as TGraphNodeSimulation;
        return (graphNode.data.order - orderDiff) * orderScale;
      }))
      .force("y", d3.forceY())
      .force("link", d3.forceLink(listLinks)
        .id((d) => (d as TGraphNodeSimulation).id)
        //.distance((a, b, c) => c.length)
        .strength(1)
      )
      .force("center", d3.forceCenter(0, 0).strength(0.1))
      ;
    this.simulation = simulation;
    const nodes = simulation.nodes();

    const l = nodes.length;
    nodes.forEach((node, index) => {
      node.x = (index * 27) % l * 100;
      node.y = (node.data.order - orderDiff) * 100;
    });
    /*
    <?xml version="1.0" encoding="utf-8"?>
<svg viewBox="0 0 500 200" width="500" height="200" xmlns="http://www.w3.org/2000/svg">
  <path style="stroke: rgb(0, 0, 0); fill:transparent" d="M 0 0 C 0 100 100 100 100 100 C 201 100 200 200 200 200"/>
  <path style="fill: rgb(216, 216, 216); stroke: rgb(0, 0, 0);" d="M 301 0 C 300 120 300 100 400 100 C 500 100 500 80 500 200"/>
</svg>
    */

    const node = nodeSelected
      .selectAll("g.node")
      .data(listNode)
      .join(
        (enter) => {
          return enter.append("g")
            .attr("class", "node")
            .attr("transform", (d) => {
              const node = d as TGraphNodeSimulation;
              return `translate(${node.x || 0} ${node.y || 0})`;
            });
        },
        (update) => {
          return update.attr("transform", (d) => {
            const node = d as TGraphNodeSimulation;
            return `translate(${node.x || 0} ${node.y || 0})`;
          });
        }
      )
      ;

    const circleNode = node.append("circle")
      .attr("stroke", "#000")
      .attr("stroke-width", 1.5)
      // .attr("fill", (d) => (d as TGraphNodeSimulation).listSink ? null : "#000")
      // .attr("stroke", (d) => (d as TGraphNodeSimulation).listSink ? null : "#fff")
      .attr("fill", "#000")
      .attr("stroke", "#fff")
      .attr("r", 5.5)
      .on("click", (e, node) => {
        this.emitNodeSelected(node.data);
      })
      //.call(drag(simulation))
      ;

    const titleNode = node.append("text")
      .text((d) => {
        return (d as TGraphNodeSimulation).data.nameId;
      }).attr("transform", "translate(10 -10)")
      .on("click", (e, node) => {
        this.emitNodeSelected(node.data);
      })
      ;

    const link = linkSelected
      .selectAll("line.link")
      .data(listLinks)
      .join(
        (enter) => {
          return enter.append("line")
            .attr("class", "link");
        },
        (update) => {
          return update;
        }
      )

    simulation.on("tick", () => {
      link
        .attr("x1", (d) => { return (d.source as TGraphNodeSimulation).x || 0 })
        .attr("y1", (d) => { return (d.source as TGraphNodeSimulation).y || 0 })
        .attr("x2", (d) => { return (d.target as TGraphNodeSimulation).x || 0 })
        .attr("y2", (d) => { return (d.target as TGraphNodeSimulation).y || 0 });

      /*
      node
        .attr("cx", d => (d as TGraphNodeSimulation).x || 0)
        .attr("cy", d => (d as TGraphNodeSimulation).y || 0);
      */

      node.attr("transform", (d) => {
        const node = d as TGraphNodeSimulation;
        const nextX = (node.x || 0).toFixed(0);
        const nextY = (node.y || 0).toFixed(0);
        return `translate(${nextX} ${nextY})`;
      });

    });
  }

  emitNodeSelected(node: MessageGraphNode) {
    this.nodeSelected.emit(node);
  }

  ngOnInit(): void {
  }

  ngAfterViewInit(): void {
    const s = new Subscription();
    this.subscription.add(s);
    s.add(
      this.hostSize$.pipe(filter(size => size.width > 0 && size.height > 0)).subscribe({
        next: () => {
          this.initDiagram();
          s.unsubscribe();
        }
      }));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}

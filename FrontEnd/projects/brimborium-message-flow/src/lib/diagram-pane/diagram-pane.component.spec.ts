import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DiagramPaneComponent } from './diagram-pane.component';

describe('DiagramPaneComponent', () => {
  let component: DiagramPaneComponent;
  let fixture: ComponentFixture<DiagramPaneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DiagramPaneComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DiagramPaneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

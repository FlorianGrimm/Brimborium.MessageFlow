import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PropertyPaneComponent } from './property-pane.component';

describe('PropertyPaneComponent', () => {
  let component: PropertyPaneComponent;
  let fixture: ComponentFixture<PropertyPaneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PropertyPaneComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PropertyPaneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

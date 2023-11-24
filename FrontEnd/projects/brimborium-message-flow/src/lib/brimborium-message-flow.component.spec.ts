import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrimboriumMessageFlowComponent } from './brimborium-message-flow.component';

describe('BrimboriumMessageFlowComponent', () => {
  let component: BrimboriumMessageFlowComponent;
  let fixture: ComponentFixture<BrimboriumMessageFlowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BrimboriumMessageFlowComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BrimboriumMessageFlowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

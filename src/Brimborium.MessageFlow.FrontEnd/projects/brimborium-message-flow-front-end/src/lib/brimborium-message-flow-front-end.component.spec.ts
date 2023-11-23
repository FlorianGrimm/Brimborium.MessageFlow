import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrimboriumMessageFlowFrontEndComponent } from './brimborium-message-flow-front-end.component';

describe('BrimboriumMessageFlowFrontEndComponent', () => {
  let component: BrimboriumMessageFlowFrontEndComponent;
  let fixture: ComponentFixture<BrimboriumMessageFlowFrontEndComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BrimboriumMessageFlowFrontEndComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BrimboriumMessageFlowFrontEndComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

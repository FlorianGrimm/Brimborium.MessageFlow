import { TestBed } from '@angular/core/testing';

import { BrimboriumMessageFlowService } from './brimborium-message-flow.service';

describe('BrimboriumMessageFlowService', () => {
  let service: BrimboriumMessageFlowService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BrimboriumMessageFlowService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

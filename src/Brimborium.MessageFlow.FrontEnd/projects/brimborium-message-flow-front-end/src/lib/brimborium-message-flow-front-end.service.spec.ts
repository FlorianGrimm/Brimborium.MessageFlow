import { TestBed } from '@angular/core/testing';

import { BrimboriumMessageFlowFrontEndService } from './brimborium-message-flow-front-end.service';

describe('BrimboriumMessageFlowFrontEndService', () => {
  let service: BrimboriumMessageFlowFrontEndService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BrimboriumMessageFlowFrontEndService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

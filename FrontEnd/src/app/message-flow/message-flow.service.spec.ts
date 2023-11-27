import { TestBed } from '@angular/core/testing';

import { MessageFlowService } from './message-flow.service';
import { HttpClientModule } from '@angular/common/http';

describe('MessageFlowService', () => {
  let service: MessageFlowService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports:[
        HttpClientModule
      ]
    });
    service = TestBed.inject(MessageFlowService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

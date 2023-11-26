/* tslint:disable */
/* eslint-disable */
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { BaseService } from '../base-service';
import { ApiConfiguration } from '../api-configuration';
import { StrictHttpResponse } from '../strict-http-response';
import { RequestBuilder } from '../request-builder';
import { Observable, of, throwError } from 'rxjs';
import { map, filter, catchError } from 'rxjs/operators';

import { MessageFlowGraph } from '../models/message-flow-graph';

@Injectable({
  providedIn: 'root',
})
export class Brimborium_MessageFlow_APISampleService extends BaseService {
  constructor(
    config: ApiConfiguration,
    http: HttpClient
  ) {
    super(config, http);
  }

  /**
   * Path part for operation getListMessageFlowName
   */
  static readonly GetListMessageFlowNamePath = '/api/messageflow/names';

  /**
   * This method provides access to the full `HttpResponse`, allowing access to response headers.
   * To access only the response body, use `getListMessageFlowName()` instead.
   *
   * This method doesn't expect any request body.
   */
  getListMessageFlowName$Response(params?: {
  }): Observable<HttpResponse<Array<string>>> {

    const rb = new RequestBuilder(this.rootUrl, Brimborium_MessageFlow_APISampleService.GetListMessageFlowNamePath, 'get');
    if (params) {
    }

    return this.http.request(rb.build({
      responseType: 'json',
      accept: 'application/json'
    })).pipe(
      filter((r: any) => r instanceof HttpResponse)
    ) as Observable<HttpResponse<Array<string>>>;
  }

  /**
   * This method provides access only to the response body.
   * To access the full response (for headers, for example), `getListMessageFlowName$Response()` instead.
   *
   * This method doesn't expect any request body.
   */
  getListMessageFlowName(params?: {
  }): Observable<Array<string> | null> {

    return this.getListMessageFlowName$Response(params).pipe(
      map((r) => r.body as Array<string>)
    );
  }

  /**
   * Path part for operation getMessageFlowGraph
   */
  static readonly GetMessageFlowGraphPath = '/api/messageflow/running/{name}/graph';

  /**
   * This method provides access to the full `HttpResponse`, allowing access to response headers.
   * To access only the response body, use `getMessageFlowGraph()` instead.
   *
   * This method doesn't expect any request body.
   */
  getMessageFlowGraph$Response(params: {
    name: string;
  }): Observable<HttpResponse<MessageFlowGraph>> {

    const rb = new RequestBuilder(this.rootUrl, Brimborium_MessageFlow_APISampleService.GetMessageFlowGraphPath, 'get');
    if (params) {
      rb.path('name', params.name, {});
    }

    return this.http.request(rb.build({
      responseType: 'json',
      accept: 'application/json'
    })).pipe(
      filter((r: any) => r instanceof HttpResponse)
    ) as Observable<HttpResponse<MessageFlowGraph>>;
  }

  /**
   * This method provides access only to the response body.
   * To access the full response (for headers, for example), `getMessageFlowGraph$Response()` instead.
   *
   * This method doesn't expect any request body.
   */
  getMessageFlowGraph(params: {
    name: string;
  }): Observable<MessageFlowGraph | null> {

    return this.getMessageFlowGraph$Response(params).pipe(
      map((r) => r.body as MessageFlowGraph)
    );
  }

}

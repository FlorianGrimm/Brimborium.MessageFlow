//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v13.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

/* tslint:disable */
/* eslint-disable */
// ReSharper disable InconsistentNaming

import { mergeMap as _observableMergeMap, catchError as _observableCatch } from 'rxjs/operators';
import { Observable, from as _observableFrom, throwError as _observableThrow, of as _observableOf } from 'rxjs';
import { Injectable, Inject, Optional, InjectionToken } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse, HttpResponseBase, HttpContext } from '@angular/common/http';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

export interface IMessageFlowClient {
    /**
     * @return OK
     */
    getListMessageFlowName(): Observable<SwaggerResponse<string[]>>;
    /**
     * @return OK
     */
    getMessageFlowGraph(name: string): Observable<SwaggerResponse<MessageFlowGraph>>;
}

@Injectable({
    providedIn: 'root'
})
export class MessageFlowClient implements IMessageFlowClient {
    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl !== undefined && baseUrl !== null ? baseUrl : "";
    }

    /**
     * @return OK
     */
    getListMessageFlowName(httpContext?: HttpContext): Observable<SwaggerResponse<string[]>> {
        let url_ = this.baseUrl + "/api/messageflow/names";
        url_ = url_.replace(/[?&]$/, "");

        let options_ : any = {
            observe: "response",
            responseType: "blob",
            context: httpContext,
            headers: new HttpHeaders({
                "Accept": "application/json"
            })
        };

        return _observableFrom(this.transformOptions(options_)).pipe(_observableMergeMap(transformedOptions_ => {
            return this.http.request("get", url_, transformedOptions_);
        })).pipe(_observableMergeMap((response_: any) => {
            return this.transformResult(url_, response_, (r) => this.processGetListMessageFlowName(r as any));
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.transformResult(url_, response_, (r) => this.processGetListMessageFlowName(r as any));
                } catch (e) {
                    return _observableThrow(e) as any as Observable<SwaggerResponse<string[]>>;
                }
            } else
                return _observableThrow(response_) as any as Observable<SwaggerResponse<string[]>>;
        }));
    }

    protected processGetListMessageFlowName(response: HttpResponseBase): Observable<SwaggerResponse<string[]>> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;

        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); }}
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result200: any = null;
            let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
            if (Array.isArray(resultData200)) {
                result200 = [] as any;
                for (let item of resultData200)
                    result200!.push(item);
            }
            else {
                result200 = <any>null;
            }
            return _observableOf(new SwaggerResponse(status, _headers, result200));
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            }));
        }
        return _observableOf<SwaggerResponse<string[]>>(new SwaggerResponse(status, _headers, null as any));
    }

    /**
     * @return OK
     */
    getMessageFlowGraph(name: string, httpContext?: HttpContext): Observable<SwaggerResponse<MessageFlowGraph>> {
        let url_ = this.baseUrl + "/api/messageflow/running/{name}/graph";
        if (name === undefined || name === null)
            throw new Error("The parameter 'name' must be defined.");
        url_ = url_.replace("{name}", encodeURIComponent("" + name));
        url_ = url_.replace(/[?&]$/, "");

        let options_ : any = {
            observe: "response",
            responseType: "blob",
            context: httpContext,
            headers: new HttpHeaders({
                "Accept": "application/json"
            })
        };

        return _observableFrom(this.transformOptions(options_)).pipe(_observableMergeMap(transformedOptions_ => {
            return this.http.request("get", url_, transformedOptions_);
        })).pipe(_observableMergeMap((response_: any) => {
            return this.transformResult(url_, response_, (r) => this.processGetMessageFlowGraph(r as any));
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.transformResult(url_, response_, (r) => this.processGetMessageFlowGraph(r as any));
                } catch (e) {
                    return _observableThrow(e) as any as Observable<SwaggerResponse<MessageFlowGraph>>;
                }
            } else
                return _observableThrow(response_) as any as Observable<SwaggerResponse<MessageFlowGraph>>;
        }));
    }

    protected processGetMessageFlowGraph(response: HttpResponseBase): Observable<SwaggerResponse<MessageFlowGraph>> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;

        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); }}
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            let result200: any = null;
            let resultData200 = _responseText === "" ? null : JSON.parse(_responseText, this.jsonParseReviver);
            result200 = MessageFlowGraph.fromJS(resultData200);
            return _observableOf(new SwaggerResponse(status, _headers, result200));
            }));
        } else if (status === 404) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("Not Found", status, _responseText, _headers);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
            return throwException("An unexpected server error occurred.", status, _responseText, _headers);
            }));
        }
        return _observableOf<SwaggerResponse<MessageFlowGraph>>(new SwaggerResponse(status, _headers, null as any));
    }
}

export class MessageFlowGraph implements IMessageFlowGraph {
    listNode?: MessageGraphNode[] | undefined;
    listConnection?: MessageGraphConnection[] | undefined;

    constructor(data?: IMessageFlowGraph) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            if (Array.isArray(_data["listNode"])) {
                this.listNode = [] as any;
                for (let item of _data["listNode"])
                    this.listNode!.push(MessageGraphNode.fromJS(item));
            }
            if (Array.isArray(_data["listConnection"])) {
                this.listConnection = [] as any;
                for (let item of _data["listConnection"])
                    this.listConnection!.push(MessageGraphConnection.fromJS(item));
            }
        }
    }

    static fromJS(data: any): MessageFlowGraph {
        data = typeof data === 'object' ? data : {};
        let result = new MessageFlowGraph();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        if (Array.isArray(this.listNode)) {
            data["listNode"] = [];
            for (let item of this.listNode)
                data["listNode"].push(item.toJSON());
        }
        if (Array.isArray(this.listConnection)) {
            data["listConnection"] = [];
            for (let item of this.listConnection)
                data["listConnection"].push(item.toJSON());
        }
        return data;
    }
}

export interface IMessageFlowGraph {
    listNode?: MessageGraphNode[] | undefined;
    listConnection?: MessageGraphConnection[] | undefined;
}

export class MessageGraphConnection implements IMessageGraphConnection {
    sourceId?: string | undefined;
    sourceNodeId?: string | undefined;
    sinkId?: string | undefined;
    sinkNodeId?: string | undefined;

    constructor(data?: IMessageGraphConnection) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.sourceId = _data["sourceId"];
            this.sourceNodeId = _data["sourceNodeId"];
            this.sinkId = _data["sinkId"];
            this.sinkNodeId = _data["sinkNodeId"];
        }
    }

    static fromJS(data: any): MessageGraphConnection {
        data = typeof data === 'object' ? data : {};
        let result = new MessageGraphConnection();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["sourceId"] = this.sourceId;
        data["sourceNodeId"] = this.sourceNodeId;
        data["sinkId"] = this.sinkId;
        data["sinkNodeId"] = this.sinkNodeId;
        return data;
    }
}

export interface IMessageGraphConnection {
    sourceId?: string | undefined;
    sourceNodeId?: string | undefined;
    sinkId?: string | undefined;
    sinkNodeId?: string | undefined;
}

export class MessageGraphNode implements IMessageGraphNode {
    nameId?: string | undefined;
    listOutgoingSourceId?: string[] | undefined;
    listIncomingSinkId?: string[] | undefined;
    listChildren?: string[] | undefined;
    order?: number;

    constructor(data?: IMessageGraphNode) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.nameId = _data["nameId"];
            if (Array.isArray(_data["listOutgoingSourceId"])) {
                this.listOutgoingSourceId = [] as any;
                for (let item of _data["listOutgoingSourceId"])
                    this.listOutgoingSourceId!.push(item);
            }
            if (Array.isArray(_data["listIncomingSinkId"])) {
                this.listIncomingSinkId = [] as any;
                for (let item of _data["listIncomingSinkId"])
                    this.listIncomingSinkId!.push(item);
            }
            if (Array.isArray(_data["listChildren"])) {
                this.listChildren = [] as any;
                for (let item of _data["listChildren"])
                    this.listChildren!.push(item);
            }
            this.order = _data["order"];
        }
    }

    static fromJS(data: any): MessageGraphNode {
        data = typeof data === 'object' ? data : {};
        let result = new MessageGraphNode();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["nameId"] = this.nameId;
        if (Array.isArray(this.listOutgoingSourceId)) {
            data["listOutgoingSourceId"] = [];
            for (let item of this.listOutgoingSourceId)
                data["listOutgoingSourceId"].push(item);
        }
        if (Array.isArray(this.listIncomingSinkId)) {
            data["listIncomingSinkId"] = [];
            for (let item of this.listIncomingSinkId)
                data["listIncomingSinkId"].push(item);
        }
        if (Array.isArray(this.listChildren)) {
            data["listChildren"] = [];
            for (let item of this.listChildren)
                data["listChildren"].push(item);
        }
        data["order"] = this.order;
        return data;
    }
}

export interface IMessageGraphNode {
    nameId?: string | undefined;
    listOutgoingSourceId?: string[] | undefined;
    listIncomingSinkId?: string[] | undefined;
    listChildren?: string[] | undefined;
    order?: number;
}

export class SwaggerResponse<TResult> {
    status: number;
    headers: { [key: string]: any; };
    result: TResult;

    constructor(status: number, headers: { [key: string]: any; }, result: TResult)
    {
        this.status = status;
        this.headers = headers;
        this.result = result;
    }
}

export class ApiException extends Error {
    override message: string;
    status: number;
    response: string;
    headers: { [key: string]: any; };
    result: any;

    constructor(message: string, status: number, response: string, headers: { [key: string]: any; }, result: any) {
        super();

        this.message = message;
        this.status = status;
        this.response = response;
        this.headers = headers;
        this.result = result;
    }

    protected isApiException = true;

    static isApiException(obj: any): obj is ApiException {
        return obj.isApiException === true;
    }
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any; }, result?: any): Observable<any> {
    return _observableThrow(new ApiException(message, status, response, headers, result));
}

function blobToText(blob: any): Observable<string> {
    return new Observable<string>((observer: any) => {
        if (!blob) {
            observer.next("");
            observer.complete();
        } else {
            let reader = new FileReader();
            reader.onload = event => {
                observer.next((event.target as any).result);
                observer.complete();
            };
            reader.readAsText(blob);
        }
    });
}
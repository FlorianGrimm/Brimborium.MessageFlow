/* tslint:disable */
/* eslint-disable */
import { MessageGraphConnection } from './message-graph-connection';
import { MessageGraphNode } from './message-graph-node';
export interface MessageFlowGraph {
  listConnection: null | Array<MessageGraphConnection>;
  listNode: null | Array<MessageGraphNode>;
}


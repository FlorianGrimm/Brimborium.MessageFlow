import { fastDeepEqual } from "./fastDeepEqual";

export type FnEqual<T> = (a: T, b: T) => boolean;

export type ValueVersioned<T> = {
  /**
   * the value
   */
  value: T;
  /**
   * the valueVersion needs to change if the value changed, but not if the old and new value is equal.
   */
  valueVersion: number;
  /**
   * the logicalTimestamp increases if the value is changed because a request completes.
   * the logicalTimestamp is set to max(this.logicalTimestamp, request.logicalTimestamp)
   */
  logicalTimestamp: number;
};

export type ReturnValue<T, F = any, Rq = any> =
  ReturnOkValue<T, Rq>
  | ReturnFailedValue<F, Rq>;

export type ReturnOkValue<T = any, Rq = any> = {
  mode: 'ok';
  value: T;
  logicalTimestamp: number;
  request: Rq | undefined;
}

export type ReturnFailedValue<F = any, Rq = any> = {
  mode: 'failed'
  failure: F;
  logicalTimestamp: number;
  request: Rq | undefined;
};

export function toValueVersioned<T=any>(
  value: T | ValueVersioned<T>,
  valueVersion: number = 0,
  logicalVersion: number = 0
): ValueVersioned<T> {
  if (value && typeof (value) === "object" && Object.hasOwn(value, "logicalTimestamp")) {
    return value as ValueVersioned<T>;
  }
  return {
    value: value as T,
    valueVersion: valueVersion,
    logicalTimestamp: logicalVersion
  };
}

export function mergeValueVersioned<T>(
  currentValue: ValueVersioned<T>,
  value: T,
  valueVersion: number = 0,
  logicalTimestamp: number = 0,
  isEqual: (undefined | FnEqual<T>) = undefined
): ValueVersioned<T> {
  const areValuesEqual = (isEqual || fastDeepEqual)(currentValue.value, value);
  if (areValuesEqual && (valueVersion <= currentValue.valueVersion)) {
    valueVersion = currentValue.valueVersion || 1;
  } else {
    const nextVersion = currentValue.valueVersion + 1;
    if (valueVersion < nextVersion) { valueVersion = nextVersion; }
  }
  if (logicalTimestamp < currentValue.logicalTimestamp) {
    logicalTimestamp = currentValue.logicalTimestamp;
  }
  return {
    value: value,
    valueVersion: valueVersion,
    logicalTimestamp: logicalTimestamp
  };
}

export function mergeNextValueVersioned<T>(
  currentValue: ValueVersioned<T>,
  nextValue: ValueVersioned<T>,
  isEqual: (undefined | FnEqual<T>) = undefined
): ValueVersioned<T> {
  const value: T = nextValue.value;
  let valueVersion: number = nextValue.valueVersion;
  let logicalTimestamp: number = nextValue.logicalTimestamp;

  const areValuesEqual = (isEqual || fastDeepEqual)(currentValue.value, value);
  if (areValuesEqual && (valueVersion <= currentValue.valueVersion)) {
    valueVersion = currentValue.valueVersion || 1;
  } else {
    const nextVersion = currentValue.valueVersion + 1;
    if (valueVersion < nextVersion) { valueVersion = nextVersion; }
  }
  if (logicalTimestamp < currentValue.logicalTimestamp) {
    logicalTimestamp = currentValue.logicalTimestamp;
  }
  return {
    value: value,
    valueVersion: valueVersion,
    logicalTimestamp: logicalTimestamp
  };
}

export function toReturnOkValue<T = any, Rq = any>(
  value: T,
  logicalTimestamp: number = 0,
  request: Rq
  ): ReturnOkValue<T, Rq> {
  return {
    mode: 'ok',
    value: value,
    logicalTimestamp: logicalTimestamp,
    request: request
  }
}

export function toReturnFailedValue<F = any, Rq = any>(
  failure: F,
  logicalTimestamp: number = 0,
  request: Rq): ReturnFailedValue<F, Rq> {
  return {
    mode: 'failed',
    failure: failure,
    logicalTimestamp: logicalTimestamp,
    request: request
  };
}

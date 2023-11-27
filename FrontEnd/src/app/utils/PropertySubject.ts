import { BehaviorSubject } from 'rxjs';
import { FnEqual, ValueVersioned, mergeValueVersioned } from './ValueVersioned';
import { fastDeepEqual } from './fastDeepEqual';

export class PropertySubject<T> extends BehaviorSubject<ValueVersioned<T>> {
  constructor(
    value: T,
    public isEqual: (undefined | FnEqual<T>) = undefined) {
    super({ value: value, valueVersion: 0, logicalTimestamp: 0 });
    if (isEqual === undefined) {
      this.isEqual = fastDeepEqual;
    }
  }

  override next(value: ValueVersioned<T>): void {
    const currentValue = this.getValue();
    super.next({
      value: value.value,
      valueVersion: (currentValue?.valueVersion || 0) + 1,
      logicalTimestamp: value.logicalTimestamp
    });
  }

  public nextIfChanged(value: ValueVersioned<T>): void {
    const currentValue = this.getValue();
    const isEqual = (this.isEqual ??= fastDeepEqual);
    let valueVersion: number = value.valueVersion;
    let logicalTimestamp: number = value.logicalTimestamp;
    const areValuesEqual = isEqual(value.value, currentValue.value);
    let setNextValue = false;
    if (areValuesEqual) {
      // skip
      valueVersion = currentValue.valueVersion;
      if (currentValue.logicalTimestamp < logicalTimestamp) {
        logicalTimestamp = currentValue.logicalTimestamp;
        setNextValue = true;
      }
    } else {
      setNextValue = true;
      if (valueVersion < (currentValue.valueVersion + 1)) {
        valueVersion = currentValue.valueVersion + 1;
      }
      if (currentValue.logicalTimestamp < logicalTimestamp) {
        logicalTimestamp = currentValue.logicalTimestamp;
      }
    }
    if (setNextValue) {
      const nextValue: ValueVersioned<T> = {
        value: value.value,
        valueVersion: valueVersion,
        logicalTimestamp: logicalTimestamp
      };
      super.next(nextValue);
    }
  }
}

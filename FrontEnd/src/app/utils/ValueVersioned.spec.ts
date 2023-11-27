import { TestBed } from '@angular/core/testing';
import { toValueVersioned, mergeValueVersioned } from './ValueVersioned';

describe('mergeValueVersioned', () => {
  const prop = toValueVersioned(0, 1 , 1);
  const actDiff = mergeValueVersioned(prop, 42);
  const actSame = mergeValueVersioned(actDiff, 42);
  const actDiff2 = mergeValueVersioned(actSame, 21, 0, actSame.logicalTimestamp+1);

  it('logicalTimestamp should be max', () => {
    expect(prop.logicalTimestamp).withContext("actDiff").toBe(actDiff.logicalTimestamp);
    expect(actDiff.logicalTimestamp).withContext("actSame").toBe(actSame.logicalTimestamp);
    expect(actSame.logicalTimestamp).withContext("actDiff2").toBeLessThan(actDiff2.logicalTimestamp);
  });

  it('valueVersion should increase if changed', () => {
    expect(prop.valueVersion).withContext("prop-actDiff").toBeLessThan(actDiff.valueVersion);
    expect(prop.valueVersion).withContext("prop-actSame").toBeLessThan(actSame.valueVersion);
    expect(actDiff.valueVersion).withContext("actDiff-actSame").toBe(actSame.valueVersion);
    expect(actSame.valueVersion).withContext("actSame-actDiff2").toBeLessThan(actDiff2.valueVersion);
  });
});

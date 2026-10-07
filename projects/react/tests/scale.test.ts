import { describe, expect, it } from 'vitest';
import { linearScale, logScale, niceStep } from '../src/charts/scale';

describe('niceStep', () => {
  it('picks 1, 2 or 5 times a power of ten', () => {
    expect(niceStep(10)).toBe(2);
    expect(niceStep(44)).toBe(10);
    expect(niceStep(0.7)).toBe(0.2);
    expect(niceStep(1000)).toBe(200);
  });
  it('falls back to 1 on an empty or invalid range', () => {
    expect(niceStep(0)).toBe(1);
    expect(niceStep(Number.NaN)).toBe(1);
  });
});

describe('linearScale', () => {
  it('widens the domain to whole steps and maps its ends to the range', () => {
    const s = linearScale(0, 44, 100, 0);
    expect(s.ticks).toEqual([0, 10, 20, 30, 40, 50]);
    expect(s(0)).toBe(100);
    expect(s(50)).toBe(0);
    expect(s(25)).toBe(50);
  });
  it('prints decimal ticks without float noise', () => {
    expect(linearScale(0, 0.9, 0, 1).ticks).toEqual([0, 0.2, 0.4, 0.6, 0.8, 1]);
  });
  it('keeps a flat series drawable', () => {
    const s = linearScale(7, 7, 0, 100);
    expect(s.ticks.length).toBeGreaterThan(1);
    expect(Number.isFinite(s(7))).toBe(true);
  });
});

describe('logScale', () => {
  it('spans whole decades, with 1-2-5 ticks over few decades', () => {
    const s = logScale(3, 70, 0, 200);
    expect(s.ticks).toEqual([1, 2, 5, 10, 20, 50, 100]);
    expect(s(1)).toBe(0);
    expect(s(100)).toBe(200);
    expect(s(10)).toBeCloseTo(100);
  });
  it('keeps only the powers of ten over many decades', () => {
    expect(logScale(1000, 300000, 0, 1).ticks).toEqual([1000, 10000, 100000, 1000000]);
  });
  it('refuses values that are not positive', () => {
    expect(() => logScale(0, 10, 0, 1)).toThrow(RangeError);
  });
});

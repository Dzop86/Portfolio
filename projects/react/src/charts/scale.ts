// Scales and ticks of the SVG charts, kept apart from React so they are tested on their own.

export interface Scale {
  (value: number): number;
  ticks: number[];
}

/** A step of 1, 2 or 5 times a power of ten giving about `count` intervals over [0, max]. */
export function niceStep(max: number, count = 5): number {
  if (!(max > 0)) return 1;
  const raw = max / count;
  const power = 10 ** Math.floor(Math.log10(raw));
  const unit = raw / power;
  return (unit <= 1 ? 1 : unit <= 2 ? 2 : unit <= 5 ? 5 : 10) * power;
}

/** Linear scale from [min, max] to [from, to]; the domain is widened to whole steps, ticks included. */
export function linearScale(min: number, max: number, from: number, to: number, count = 5): Scale {
  // A flat series (min = max) gets a span of its own magnitude: a step of nearly 0 would never end.
  const span = max - min > 0 ? max - min : Math.abs(max) || 1;
  const step = niceStep(span, count);
  const lo = Math.floor(min / step) * step;
  const hi = Math.max(Math.ceil(max / step) * step, lo + step);
  const ticks: number[] = [];
  // Rounded to the step's decimals: 0.1 + 0.2 must print as 0.3.
  const decimals = Math.max(0, -Math.floor(Math.log10(step)));
  for (let v = lo; v <= hi + step / 2; v += step) ticks.push(Number(v.toFixed(decimals)));
  const scale = ((v: number) => from + ((v - lo) / (hi - lo)) * (to - from)) as Scale;
  scale.ticks = ticks;
  return scale;
}

/** Logarithmic scale; ticks at 1, 2 and 5 times the powers of ten when the domain spans few decades. */
export function logScale(min: number, max: number, from: number, to: number): Scale {
  if (!(min > 0) || !(max > 0)) throw new RangeError('a log scale needs positive values');
  const lo = Math.floor(Math.log10(min));
  const hi = Math.max(Math.ceil(Math.log10(max)), lo + 1);
  const ticks: number[] = [];
  const mantissas = hi - lo <= 2 ? [1, 2, 5] : [1];
  for (let e = lo; e <= hi; e++) {
    for (const m of mantissas) {
      const v = m * 10 ** e;
      if (v <= 10 ** hi) ticks.push(Number(v.toPrecision(12)));
    }
  }
  const scale = ((v: number) => from + ((Math.log10(v) - lo) / (hi - lo)) * (to - from)) as Scale;
  scale.ticks = ticks;
  return scale;
}

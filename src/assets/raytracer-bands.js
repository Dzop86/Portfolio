// How the ray tracer's image is shared between web workers (D45, R5): bands of a few rows, dealt in turn
// like cards, so that every worker gets some sky, some floor and some of the object. A row always belongs
// to the same worker, which keeps its samples: the image is the same whatever the number of workers.

export const BAND = 8;          // rows per band: small enough to share the work evenly
export const MAX_WORKERS = 8;   // beyond that, the gain hardly pays for the memory of one module each
export const MAX_SAMPLES = 256; // passes per image: it hardly changes after that

/** Bands [y0, y1) of the worker `k` out of `workers`, for an image `height` rows high. */
export function bandsFor(height, workers, k) {
  const bands = [];
  for (let i = k, y0 = k * BAND; y0 < height; i += workers, y0 = i * BAND) bands.push([y0, Math.min(y0 + BAND, height)]);
  return bands;
}

/** Number of workers to start by default: one core is left to the page, at least one, at most MAX_WORKERS. */
export function defaultWorkers(hardwareConcurrency) {
  const n = Number.isFinite(hardwareConcurrency) ? Math.floor(hardwareConcurrency) : 1;
  return Math.max(1, Math.min(MAX_WORKERS, n - 1));
}

/**
 * Tracks the passes of every band of the image; `samples` is the fewest, the number of passes shown.
 * `record` returns true when that number grew.
 */
export class PassCounter {
  constructor(height) {
    this.rows = new Uint32Array(Math.ceil(height / BAND));
    this.samples = 0;
  }

  record(y0, samples) {
    this.rows[Math.floor(y0 / BAND)] = samples;
    let min = Infinity;
    for (const s of this.rows) if (s < min) min = s;
    const grew = min > this.samples;
    this.samples = min;
    return grew;
  }
}

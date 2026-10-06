// Wrapper over the WebAssembly build of the C++ topology library (projects/topologie), shared by the
// viewer and the tests.
import { MAX_BYTES } from './meshlib-api.js';

/** Instantiates the Emscripten module produced by projects/topologie/scripts/build-wasm.sh. */
export function loadTopo(createTopo, options = {}) {
  return createTopo(options);
}

const INVALID_MESH = 10;

/**
 * Reads an OBJ or PLY file given as bytes. On success returns the invariants and copies of the arrays
 * (positions centred in the unit sphere, triangle indices, curvature density and angle defect per vertex);
 * otherwise { ok: false, status, message, line } with status 1..4 from lib-c, 'invalid' or 'too-large'.
 */
export function readTopology(lib, bytes) {
  if (bytes.byteLength > MAX_BYTES) return { ok: false, status: 'too-large' };
  const ptr = lib._malloc(Math.max(bytes.byteLength, 1));
  if (!ptr) return { ok: false, status: 'too-large' };
  try {
    lib.HEAPU8.set(bytes, ptr);
    const status = lib._topoc_read(ptr, bytes.byteLength);
    if (status !== 0) {
      return {
        ok: false,
        status: status === INVALID_MESH ? 'invalid' : status,
        message: lib.UTF8ToString(lib._topoc_error()),
        line: lib._topoc_error_line(),
      };
    }
    const nv = lib._topoc_vertex_count();
    const ni = lib._topoc_index_count();
    // Views are taken after every call that may grow memory, then copied out.
    const f32 = (p, n) => lib.HEAPF32.slice(p / 4, p / 4 + n);
    const { totalCurvature, ...invariants } = JSON.parse(lib.UTF8ToString(lib._topoc_summary()));
    return {
      ok: true,
      invariants,
      totalCurvature,
      positions: f32(lib._topoc_positions(), 3 * nv),
      indices: lib.HEAPU32.slice(lib._topoc_indices() / 4, lib._topoc_indices() / 4 + ni),
      curvature: f32(lib._topoc_curvature(), nv),
      defect: f32(lib._topoc_defect(), nv),
      boundary: lib.HEAPU8.slice(lib._topoc_boundary(), lib._topoc_boundary() + nv),
    };
  } finally {
    lib._free(ptr);
  }
}

/**
 * Curvature to colour each vertex with. On the boundary the angle defect measures how much the boundary
 * turns, not the Gaussian curvature, so boundary vertices take the mean of their already known neighbours
 * (through shared triangles): interior ones first, then, pass after pass, boundary ones filled before them
 * (a corner may touch no interior vertex). Vertices that never get a value read as 0.
 */
export function interiorCurvature(curvature, boundary, indices) {
  const values = Float32Array.from(curvature);
  const known = Uint8Array.from(boundary, (b) => (b ? 0 : 1));
  for (let progress = true; progress;) {
    progress = false;
    const sum = new Float64Array(values.length);
    const count = new Uint32Array(values.length);
    for (let i = 0; i < indices.length; i += 3) {
      const tri = [indices[i], indices[i + 1], indices[i + 2]];
      for (const v of tri) {
        if (known[v]) continue;
        for (const w of tri) {
          if (!known[w]) continue;
          sum[v] += values[w];
          count[v] += 1;
        }
      }
    }
    count.forEach((n, v) => {
      if (n === 0) return;
      values[v] = sum[v] / n;
      known[v] = 1;
      progress = true;
    });
  }
  known.forEach((k, v) => { if (!k) values[v] = 0; });
  return values;
}

/**
 * Colour scale by quantiles: t(k) is the share of interior vertices whose |K| is at most |k|, signed like k,
 * in [-1, 1]. Curvature of real meshes is heavy-tailed (sharp creases reach 1000 times the median), so a
 * linear scale would leave almost everything neutral. `ticks` gives the |K| shown at 50 % and 90 % of the
 * legend. Boundary vertices are left out (their defect measures the turning of the boundary).
 */
export function quantileScale(curvature, boundary) {
  const sorted = [];
  curvature.forEach((k, v) => { if (!boundary[v]) sorted.push(Math.abs(k)); });
  sorted.sort((a, b) => a - b);
  const n = sorted.length;
  const rank = (x) => { // number of values <= x
    let lo = 0;
    let hi = n;
    while (lo < hi) {
      const mid = (lo + hi) >> 1;
      if (sorted[mid] <= x) lo = mid + 1;
      else hi = mid;
    }
    return lo;
  };
  return {
    t: (k) => (k === 0 || n === 0 ? 0 : Math.sign(k) * (rank(Math.abs(k)) / n)),
    ticks: [0.5, 0.9].map((at) => ({ at, value: n ? sorted[Math.floor(at * (n - 1))] : 0 })),
  };
}

/** Total curvature in turns (multiples of 2 pi), rounded to 3 decimals; adding 0 turns -0 into 0. */
export function turns(total) {
  return Math.round((total / (2 * Math.PI)) * 1000) / 1000 + 0;
}

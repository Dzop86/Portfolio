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
    const status = lib._topojs_read(ptr, bytes.byteLength);
    if (status !== 0) {
      return {
        ok: false,
        status: status === INVALID_MESH ? 'invalid' : status,
        message: lib.UTF8ToString(lib._topojs_error()),
        line: lib._topojs_error_line(),
      };
    }
    const nv = lib._topojs_vertex_count();
    const ni = lib._topojs_index_count();
    // Views are taken after every call that may grow memory, then copied out.
    const f32 = (p, n) => lib.HEAPF32.slice(p / 4, p / 4 + n);
    const { totalCurvature, ...invariants } = JSON.parse(lib.UTF8ToString(lib._topojs_summary()));
    return {
      ok: true,
      invariants,
      totalCurvature,
      positions: f32(lib._topojs_positions(), 3 * nv),
      indices: lib.HEAPU32.slice(lib._topojs_indices() / 4, lib._topojs_indices() / 4 + ni),
      curvature: f32(lib._topojs_curvature(), nv),
      defect: f32(lib._topojs_defect(), nv),
      boundary: lib.HEAPU8.slice(lib._topojs_boundary(), lib._topojs_boundary() + nv),
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

/** Half-width of the symmetric colour scale: 95th percentile of |K| over interior vertices, 1 if flat. */
export function curvatureScale(curvature, boundary) {
  const magnitudes = [];
  curvature.forEach((k, v) => { if (!boundary[v]) magnitudes.push(Math.abs(k)); });
  magnitudes.sort((a, b) => a - b);
  return magnitudes[Math.floor(0.95 * (magnitudes.length - 1))] || 1;
}

/** Total curvature in turns (multiples of 2 pi), rounded to 3 decimals; adding 0 turns -0 into 0. */
export function turns(total) {
  return Math.round((total / (2 * Math.PI)) * 1000) / 1000 + 0;
}

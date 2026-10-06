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
    };
  } finally {
    lib._free(ptr);
  }
}

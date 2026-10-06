// Thin wrapper over the WebAssembly build of lib-c (projects/lib-c), shared by the demo and the tests.

/** Files above this size are refused before reaching WebAssembly (binary STL models of 20 MB fit). */
export const MAX_BYTES = 32 * 1024 * 1024;

/** Instantiates the Emscripten module produced by projects/lib-c/scripts/build-wasm.sh. */
export function loadMeshLib(createMeshLib, options = {}) {
  return createMeshLib(options);
}

const FORMATS = ['OBJ', 'PLY', 'STL']; // mesh_format in lib-c

/**
 * Reads an OBJ or PLY file given as bytes. Returns the figures, or { ok: false, status, message, line }.
 * The copy in WebAssembly memory is always freed.
 */
export function readMesh(lib, bytes) {
  if (bytes.byteLength > MAX_BYTES) return { ok: false, status: 'too-large' };
  const ptr = lib._malloc(Math.max(bytes.byteLength, 1));
  if (!ptr) return { ok: false, status: 'too-large' };
  try {
    lib.HEAPU8.set(bytes, ptr);
    const status = lib._meshjs_read(ptr, bytes.byteLength);
    if (status !== 0) {
      return { ok: false, status, message: lib.UTF8ToString(lib._meshjs_status_string(status)), line: lib._meshjs_error_line() };
    }
    const axis = (corner) => [0, 1, 2].map((a) => lib._meshjs_bbox(corner, a));
    return {
      ok: true,
      format: FORMATS[lib._meshjs_format()],
      vertices: lib._meshjs_vertex_count(),
      polygons: lib._meshjs_polygon_count(),
      triangles: lib._meshjs_triangle_count(),
      edges: lib._meshjs_edge_count(),
      boundaryEdges: lib._meshjs_boundary_edge_count(),
      euler: lib._meshjs_euler_characteristic(),
      bbox: { min: axis(0), max: axis(1) },
    };
  } finally {
    lib._free(ptr);
  }
}

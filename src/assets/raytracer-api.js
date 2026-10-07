// Wrapper over the WebAssembly build of the ray tracer (projects/raytracer), shared by its web worker and
// the tests.
import { MAX_BYTES } from './meshlib-api.js';

export const SCENES = { spheres: 0, mesh: 1 };
export const FINISHES = { diffuse: 0, metal: 1, glass: 2 };
const NO_MESH = 11;

/** Instantiates the Emscripten module produced by projects/raytracer/scripts/build-wasm.sh. */
export function loadRaytracer(createRaytracer, options = {}) {
  return createRaytracer(options);
}

/**
 * Reads a mesh (OBJ, PLY or STL bytes) for the mesh scene. Returns { ok: true, triangles } or
 * { ok: false, status, message, line } with status 1..4 from lib-c, 'invalid' (no triangle) or 'too-large'.
 */
export function loadMesh(lib, bytes) {
  if (bytes.byteLength > MAX_BYTES) return { ok: false, status: 'too-large' };
  const ptr = lib._malloc(Math.max(bytes.byteLength, 1));
  if (!ptr) return { ok: false, status: 'too-large' };
  try {
    lib.HEAPU8.set(bytes, ptr);
    const status = lib._rtc_load_mesh(ptr, bytes.byteLength);
    if (status !== 0) {
      return { ok: false, status: status === 10 ? 'invalid' : status, message: lib.UTF8ToString(lib._rtc_error()), line: lib._rtc_error_line() };
    }
    return { ok: true, triangles: lib._rtc_triangles() };
  } finally {
    lib._free(ptr);
  }
}

/** Chooses the scene ('spheres' or 'mesh') and the mesh's finish; returns its default view. */
export function setScene(lib, scene, finish = 'diffuse') {
  const status = lib._rtc_set_scene(SCENES[scene] ?? -1, FINISHES[finish] ?? -1);
  if (status === NO_MESH) throw new Error('the mesh scene needs a mesh: call loadMesh first');
  if (status !== 0) throw new Error(`unknown scene or finish: ${scene}, ${finish}`);
  return { yaw: lib._rtc_default_yaw(), pitch: lib._rtc_default_pitch(), distance: lib._rtc_default_distance() };
}

/** Orbits the camera (degrees, scene units) and forgets the samples. */
export function setView(lib, { yaw, pitch, distance }) {
  lib._rtc_set_view(yaw, pitch, distance);
}

/** Sets the image size in pixels and forgets the samples. */
export function resize(lib, width, height) {
  if (lib._rtc_resize(width, height) !== 0) throw new Error(`bad image size: ${width} x ${height}`);
}

/** Adds one sample to rows [y0, y1); returns the fewest samples of any row. */
export function renderRows(lib, y0, y1) {
  return lib._rtc_render(y0, y1);
}

/** A copy of the RGBA image (the module's memory may move when it grows). */
export function pixels(lib) {
  const n = lib._rtc_width() * lib._rtc_height() * 4;
  const p = lib._rtc_pixels();
  return lib.HEAPU8.slice(p, p + n);
}

/** A copy of the RGBA rows [y0, y1), the band a worker sends to the page. */
export function rows(lib, y0, y1) {
  const stride = lib._rtc_width() * 4;
  const p = lib._rtc_pixels();
  return lib.HEAPU8.slice(p + y0 * stride, p + y1 * stride);
}

/** The settings, in the order of the C API: light azimuth and elevation (degrees), colour (K), metal fuzz, glass index. */
export const SETTINGS = ['azimuth', 'elevation', 'kelvin', 'fuzz', 'ior'];

/** The engine's default settings, for the page's controls. */
export function defaultSettings(lib) {
  return Object.fromEntries(SETTINGS.map((key, i) => [key, lib._rtc_default_setting(i)]));
}

/** Sets the light and the materials of every scene from now on, and forgets the samples. */
export function setSettings(lib, settings) {
  const values = SETTINGS.map((key) => Number(settings[key]));
  if (lib._rtc_set_settings(...values) !== 0) throw new Error(`settings out of range: ${JSON.stringify(settings)}`);
}

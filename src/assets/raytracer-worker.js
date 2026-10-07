// Web worker of the ray tracer (projects/raytracer in WebAssembly). The page starts several of them and
// gives each its own bands of rows (raytracer-bands.js); a worker renders its bands pass after pass, one
// band at a time so that a new scene or view is taken into account quickly, and posts each band's rows.
//
// Messages in:
//   { type: 'scene', generation, mesh, finish, width, height, bands, view, settings } where mesh is null
//     (spheres), { url } (a sample) or { key, bytes } (the visitor's file; bytes may be left out once sent),
//     view is null for the scene's default view and settings null for the engine's defaults;
//   { type: 'view', generation, view }, { type: 'settings', generation, settings }, { type: 'pause' },
//   { type: 'resume' }.
// Messages out: 'loaded'; 'ready' (generation, defaultView, defaultSettings, triangles); 'band' (generation, y0, y1, pixels,
// samples); 'done' (generation); 'error' (generation, status, line, message). Each carries the generation
// of the scene or view it belongs to, so that the page can drop those of an older one.
import createRaytracer from './wasm/raytracer.js';
import { loadRaytracer, loadMesh, setScene, setView, setSettings, defaultSettings, resize, renderRows, rows } from './raytracer-api.js';
import { MAX_SAMPLES } from './raytracer-bands.js';

const lib = await loadRaytracer(createRaytracer);
let bands = [];
let next = 0;          // index of the next band to render
let pass = 0;          // passes completed on every band of this worker
let running = false;
let paused = false;
let timer = 0;
let generation = 0;
const meshes = new Map(); // URL or file key -> bytes, so that a new finish does not download again
let sceneRequest = 0;     // a newer scene request wins over one still downloading its mesh

function schedule() {
  if (!timer && running && !paused) timer = setTimeout(step, 0);
}

function step() {
  timer = 0;
  if (!running || paused) return;
  const [y0, y1] = bands[next];
  renderRows(lib, y0, y1);
  const pixels = rows(lib, y0, y1);
  postMessage({ type: 'band', generation, y0, y1, pixels, samples: pass + 1 }, [pixels.buffer]);
  if (++next === bands.length) {
    next = 0;
    if (++pass >= MAX_SAMPLES) {
      running = false;
      postMessage({ type: 'done', generation });
      return;
    }
  }
  schedule();
}

function restart() {
  next = 0;
  pass = 0;
  running = bands.length > 0;
  schedule();
}

async function meshBytes(mesh) {
  const key = mesh.url ?? mesh.key;
  if (mesh.bytes) meshes.set(key, new Uint8Array(mesh.bytes));
  if (!meshes.has(key)) {
    const response = await fetch(mesh.url);
    if (!response.ok) throw new Error(`${mesh.url}: HTTP ${response.status}`);
    meshes.set(key, new Uint8Array(await response.arrayBuffer()));
  }
  return meshes.get(key);
}

/** A mesh that lib-c refuses: the page shows its status (1 to 4, 'invalid', 'too-large') and line. */
class MeshError extends Error {
  constructor({ status, line, message }) {
    super(message ?? String(status));
    Object.assign(this, { status, line });
  }
}

onmessage = async ({ data }) => {
  try {
    if (data.type === 'scene') {
      running = false;
      generation = data.generation;
      const request = ++sceneRequest;
      if (data.mesh) {
        const bytes = await meshBytes(data.mesh);
        if (request !== sceneRequest) return;
        const result = loadMesh(lib, bytes);
        if (!result.ok) throw new MeshError(result);
      }
      resize(lib, data.width, data.height);
      setSettings(lib, data.settings ?? defaultSettings(lib));
      const defaultView = setScene(lib, data.mesh ? 'mesh' : 'spheres', data.finish);
      if (data.view) setView(lib, data.view);
      bands = data.bands;
      postMessage({ type: 'ready', generation, defaultView, defaultSettings: defaultSettings(lib), triangles: data.mesh ? lib._rtc_triangles() : 0 });
      restart();
    } else if (data.type === 'view') {
      generation = data.generation;
      setView(lib, data.view);
      restart();
    } else if (data.type === 'settings') {
      generation = data.generation;
      setSettings(lib, data.settings);
      restart();
    } else if (data.type === 'pause') {
      paused = true;
    } else if (data.type === 'resume') {
      paused = false;
      schedule();
    }
  } catch (e) {
    running = false;
    postMessage({ type: 'error', generation, status: e.status ?? 'engine', line: e.line ?? 0, message: String(e.message ?? e) });
  }
};

// The page sends nothing before this: messages posted while the module was still loading could be lost.
postMessage({ type: 'loaded' });

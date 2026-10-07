// Web worker of the ray tracer (projects/raytracer in WebAssembly): renders pass after pass, one band of
// rows at a time so that a new scene or view is taken into account within a band, and posts the image
// after every pass. Messages in: { type: 'scene', generation, finish, mesh (URL or null), width, height },
// { type: 'view', view, generation }, { type: 'pause' }, { type: 'resume' }. Messages out: 'ready' (with the
// default view), 'frame' (pixels, samples, ms per pass), 'done', 'error'; frames and 'done' carry the
// generation of the scene or view they show, so that the page can drop those of an older one.
import createRaytracer from './wasm/raytracer.js';
import { loadRaytracer, loadMesh, setScene, setView, resize, renderRows, pixels } from './raytracer-api.js';

const BAND = 24;           // rows per step: short enough to answer messages quickly
const MAX_SAMPLES = 256;   // the image hardly changes after that

const lib = await loadRaytracer(createRaytracer);
let running = false;
let paused = false;
let row = 0;
let passStart = 0;
let timer = 0;
const meshes = new Map(); // URL -> bytes, so that changing the finish does not download again
let sceneRequest = 0;     // a newer scene request wins over one still downloading its mesh
let generation = 0;

function schedule() {
  if (!timer && running && !paused) timer = setTimeout(step, 0);
}

function step() {
  timer = 0;
  if (!running || paused) return;
  const height = lib._rtc_height();
  if (row === 0) passStart = performance.now();
  const end = Math.min(row + BAND, height);
  const samples = renderRows(lib, row, end);
  row = end;
  if (row >= height) {
    row = 0;
    const image = pixels(lib);
    postMessage({ type: 'frame', generation, pixels: image, samples, ms: performance.now() - passStart }, [image.buffer]);
    if (samples >= MAX_SAMPLES) {
      running = false;
      postMessage({ type: 'done', generation, samples });
      return;
    }
  }
  schedule();
}

function restart() {
  row = 0;
  running = true;
  schedule();
}

async function meshBytes(url) {
  if (!meshes.has(url)) {
    const response = await fetch(url);
    if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
    meshes.set(url, new Uint8Array(await response.arrayBuffer()));
  }
  return meshes.get(url);
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
        if (!result.ok) throw new Error(`${data.mesh}: ${result.message ?? result.status}`);
      }
      resize(lib, data.width, data.height);
      const view = setScene(lib, data.mesh ? 'mesh' : 'spheres', data.finish);
      postMessage({ type: 'ready', view, triangles: data.mesh ? lib._rtc_triangles() : 0 });
      restart();
    } else if (data.type === 'view') {
      setView(lib, data.view);
      generation = data.generation;
      restart();
    } else if (data.type === 'pause') {
      paused = true;
    } else if (data.type === 'resume') {
      paused = false;
      schedule();
    }
  } catch (e) {
    running = false;
    postMessage({ type: 'error', message: String(e.message ?? e) });
  }
};

// The page sends nothing before this: messages posted while the module was still loading could be lost.
postMessage({ type: 'loaded' });

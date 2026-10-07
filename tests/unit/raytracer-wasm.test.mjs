// Integration test: the committed WebAssembly build of the ray tracer, through the worker's wrapper,
// against the reference images rendered by the native build (projects/raytracer/tests/reference).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { loadRaytracer, loadMesh, setScene, setView, resize, renderRows, pixels } from '../../src/assets/raytracer-api.js';

const create = (await import('../../src/assets/wasm/raytracer.js')).default;
const sample = (name) => readFileSync(join(ROOT, 'projects/topologie/samples', name));

/** RGB bytes of a binary PPM. */
function readPpm(path) {
  const data = readFileSync(path);
  const header = data.subarray(0, 32).toString('latin1').match(/^P6\s+(\d+)\s+(\d+)\s+255\s/);
  return { width: Number(header[1]), height: Number(header[2]), rgb: data.subarray(header[0].length) };
}

function psnr(rgba, rgb) {
  let se = 0;
  for (let i = 0, j = 0; j < rgb.length; i += 4, j += 3) {
    for (let k = 0; k < 3; k++) se += (rgba[i + k] - rgb[j + k]) ** 2;
  }
  return se === 0 ? Infinity : 10 * Math.log10((255 * 255) / (se / rgb.length));
}

test('the browser build renders the same images as the native one', async () => {
  const lib = await loadRaytracer(create);
  for (const [name, scene, finish] of [['spheres', 'spheres', 'diffuse'], ['torus-metal', 'mesh', 'metal']]) {
    if (scene === 'mesh') assert.equal(loadMesh(lib, sample('torus.obj')).ok, true);
    const ref = readPpm(join(ROOT, 'projects/raytracer/tests/reference', `${name}.ppm`));
    resize(lib, ref.width, ref.height);
    setScene(lib, scene, finish);
    let samples = 0;
    for (let i = 0; i < 16; i++) samples = renderRows(lib, 0, ref.height);
    assert.equal(samples, 16);
    const db = psnr(pixels(lib), ref.rgb);
    assert.ok(db > 45, `${name}: ${db.toFixed(1)} dB`);
  }
});

test('bands rendered in any order give the image of whole passes', async () => {
  const a = await loadRaytracer(create), b = await loadRaytracer(create);
  for (const lib of [a, b]) {
    resize(lib, 40, 20);
    setScene(lib, 'spheres');
  }
  for (let i = 0; i < 2; i++) {
    renderRows(a, 0, 20);
    renderRows(b, 10, 20);
    assert.equal(renderRows(b, 0, 10), i + 1);
  }
  assert.deepEqual(pixels(a), pixels(b));
});

test('moving the camera starts again from no sample, and the default view comes back with the scene', async () => {
  const lib = await loadRaytracer(create);
  resize(lib, 16, 9);
  const view = setScene(lib, 'spheres');
  assert.deepEqual(view, { yaw: 20, pitch: 14, distance: 6.2 });
  renderRows(lib, 0, 9);
  assert.equal(lib._rtc_samples(), 1);
  setView(lib, { ...view, yaw: 90 });
  assert.equal(lib._rtc_samples(), 0);
  assert.deepEqual(setScene(lib, 'spheres'), view);
});

test('mesh errors come back with lib-c\'s status and line; the mesh scene needs a mesh', async () => {
  const lib = await loadRaytracer(create);
  assert.throws(() => setScene(lib, 'mesh'), /needs a mesh/);
  const bad = loadMesh(lib, new TextEncoder().encode('v 0 0 0\nv 1 0 0\nf 1 2 9\n'));
  assert.deepEqual([bad.ok, bad.status, bad.line], [false, 4, 3]);
  assert.equal(loadMesh(lib, new TextEncoder().encode('v 0 0 0\n')).status, 'invalid');
  assert.equal(loadMesh(lib, new Uint8Array(33 * 1024 * 1024)).status, 'too-large');
  const ok = loadMesh(lib, sample('mobius.obj'));
  assert.equal(ok.ok, true);
  assert.ok(ok.triangles > 100);
  assert.throws(() => setScene(lib, 'mesh', 'velvet'), /unknown/);
  assert.throws(() => resize(lib, 0, 10), /bad image size/);
});

/** A binary STL of one tetrahedron: 80-byte header, triangle count, then 50 bytes per triangle. */
function binaryStl() {
  const v = [[0, 0, 0], [1, 0, 0], [0, 1, 0], [0, 0, 1]];
  const faces = [[0, 2, 1], [0, 1, 3], [0, 3, 2], [1, 2, 3]];
  const view = new DataView(new ArrayBuffer(84 + 50 * faces.length));
  view.setUint32(80, faces.length, true);
  faces.forEach((f, i) => f.forEach((vi, j) => v[vi].forEach((x, k) => view.setFloat32(84 + 50 * i + 12 + 12 * j + 4 * k, x, true))));
  return new Uint8Array(view.buffer);
}

test('a visitor\'s mesh in each format lib-c reads is rendered in place of the sample', async () => {
  const lib = await loadRaytracer(create);
  resize(lib, 32, 18);
  setScene(lib, 'spheres');
  renderRows(lib, 0, 18);
  const spheres = pixels(lib);
  const data = (name) => readFileSync(join(ROOT, 'projects/lib-c/tests/data', name));
  for (const [name, bytes, triangles] of [['cube.obj', data('cube.obj'), 12], ['cube.stl', data('cube.stl'), 12],
    ['tetrahedron.ply', data('tetrahedron.ply'), 4], ['binary.stl', binaryStl(), 4]]) {
    const result = loadMesh(lib, bytes);
    assert.deepEqual(result, { ok: true, triangles }, name);
    setScene(lib, 'mesh', 'diffuse');
    renderRows(lib, 0, 18);
    assert.notDeepEqual(pixels(lib), spheres, name);
  }
});

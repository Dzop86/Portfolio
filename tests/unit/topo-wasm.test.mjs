// Integration test: the committed WebAssembly build of the C++ topology library, through the viewer's wrapper.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { loadTopo, readTopology, interiorCurvature, quantileScale, turns } from '../../src/assets/topo-api.js';

const lib = await loadTopo((await import('../../src/assets/wasm/topo.js')).default);
const sample = (name) => readFileSync(join(ROOT, 'projects/lib-c/tests/data', name));

test('the torus has genus 1 and total curvature 0', () => {
  const r = readTopology(lib, sample('torus.obj'));
  assert.equal(r.ok, true);
  assert.deepEqual(
    [r.invariants.components, r.invariants.boundaryLoops, r.invariants.orientable, r.invariants.manifold, r.invariants.euler, r.invariants.genus],
    [1, 0, true, true, 0, 1],
  );
  assert.ok(Math.abs(r.totalCurvature) < 1e-9);
  assert.equal(r.positions.length, 3 * 48);
  assert.equal(r.indices.length, 3 * 96);
  assert.equal(r.curvature.length, 48);
});

test('positions are centred and fit in the unit sphere', () => {
  const { positions } = readTopology(lib, sample('cube.obj'));
  let max = 0;
  for (let i = 0; i < positions.length; i += 3) max = Math.max(max, Math.hypot(positions[i], positions[i + 1], positions[i + 2]));
  assert.ok(Math.abs(max - 1) < 1e-6, `max radius ${max}`);
  assert.ok(Math.abs(positions[0] + 0.57735) < 1e-4, 'corner (0,0,0) moves to -1/sqrt(3)');
});

test('the tetrahedron carries 4 pi of curvature', () => {
  const r = readTopology(lib, sample('tetrahedron.ply'));
  assert.ok(Math.abs(r.totalCurvature - 4 * Math.PI) < 1e-9);
  assert.equal(r.invariants.genus, 0);
});

test('a face listed against its neighbours is flagged as inconsistently oriented', () => {
  // Faces 1 and 2 both run along edge 2 -> 3 in the same direction.
  const obj = 'v 0 0 0\nv 1 0 0\nv 0 1 0\nv 0 0 1\nv 1 1 0\nv 0 1 1\nf 1 2 3\nf 2 3 4\nf 1 3 5\n';
  const r = readTopology(lib, new TextEncoder().encode(obj));
  assert.equal(r.ok, true);
  assert.equal(r.invariants.consistentlyOriented, false);
});

test('reader errors come back with their line, invalid meshes with their message', () => {
  const bad = readTopology(lib, new TextEncoder().encode('v 0 0 0\nv 1 0 0\nf 1 2 3\n'));
  assert.deepEqual([bad.ok, bad.status, bad.line], [false, 4, 3]);
  const degenerate = readTopology(lib, new TextEncoder().encode('v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 2\n'));
  assert.deepEqual([degenerate.ok, degenerate.status], [false, 'invalid']);
  assert.match(degenerate.message, /degenerate/);
});

test('the library keeps working after an error and frees its memory', () => {
  const bytes = new TextEncoder().encode(`# ${'x'.repeat(1 << 20)}\n${sample('torus.obj')}`);
  readTopology(lib, bytes);
  const before = lib.HEAPU8.length;
  for (let i = 0; i < 64; i++) {
    readTopology(lib, new TextEncoder().encode('v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 2\n'));
    assert.equal(readTopology(lib, bytes).ok, true);
  }
  assert.equal(lib.HEAPU8.length, before);
});

const topoSample = (name) => readFileSync(join(ROOT, 'projects/topologie/samples', name));

test('boundary vertices are flagged and take the curvature of their interior neighbours', () => {
  const r = readTopology(lib, topoSample('saddle.obj'));
  assert.equal(r.boundary.reduce((a, b) => a + b, 0), 4 * 24, 'the 25 x 25 grid has 96 boundary vertices');
  const values = interiorCurvature(r.curvature, r.boundary, r.indices);
  for (let v = 0; v < values.length; v++) assert.ok(values[v] < 0, `saddle vertex ${v} reads as a saddle`);
  // Interior vertices keep their own value.
  const centre = 12 * 25 + 12;
  assert.equal(values[centre], r.curvature[centre]);
});

test('the colour scale ranks |K| by quantile, keeps the sign and ignores the boundary', () => {
  const k = new Float32Array([-8, -1, 0, 1, 2, 4, 1000]);
  const boundary = new Uint8Array([0, 0, 0, 0, 0, 0, 0]);
  const scale = quantileScale(k, boundary);
  assert.equal(scale.t(0), 0);
  assert.equal(scale.t(1000), 1, 'the largest |K| gets full colour');
  assert.ok(scale.t(-8) < 0 && Math.abs(scale.t(-8)) > Math.abs(scale.t(-1)), 'negative values keep their sign, ordered by |K|');
  assert.ok(scale.t(2) > 0.3, 'a heavy tail does not wash out ordinary values');
  for (let i = 1; i < 50; i++) assert.ok(scale.t(i) >= scale.t(i - 1), 'monotonic');
  assert.deepEqual(scale.ticks.map((t) => t.at), [0.5, 0.9]);
  assert.equal(scale.ticks[0].value, 2, 'median of |K|');

  const saddle = readTopology(lib, topoSample('saddle.obj'));
  const s2 = quantileScale(saddle.curvature, saddle.boundary);
  assert.ok(s2.ticks[1].value < 10, 'boundary turning does not enter the scale');
  assert.equal(quantileScale(new Float32Array([0, 0]), new Uint8Array(2)).t(0), 0, 'flat meshes stay neutral');
});

test('the total curvature is shown in turns, rounded, never as -0', () => {
  assert.ok(Object.is(turns(-1e-15), 0));
  assert.equal(turns(4 * Math.PI), 2);
  assert.equal(turns(2 * Math.PI * 0.12345), 0.123);
});

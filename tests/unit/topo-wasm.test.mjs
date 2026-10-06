// Integration test: the committed WebAssembly build of the C++ topology library, through the viewer's wrapper.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { loadTopo, readTopology } from '../../src/assets/topo-api.js';

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

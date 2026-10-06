// Integration test: the committed WebAssembly build of lib-c, driven through the same wrapper as the demo.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT } from '../../src/lib.mjs';
import { loadMeshLib, readMesh, MAX_BYTES } from '../../src/assets/meshlib-api.js';

const lib = await loadMeshLib((await import('../../src/assets/wasm/meshlib.js')).default);
const sample = (name) => readFileSync(join(ROOT, 'projects/lib-c/tests/data', name));

test('the WebAssembly build reads an OBJ cube and computes its topology', () => {
  const r = readMesh(lib, sample('cube.obj'));
  assert.equal(r.ok, true);
  assert.deepEqual(
    [r.format, r.vertices, r.polygons, r.triangles, r.edges, r.boundaryEdges, r.euler],
    ['OBJ', 8, 6, 12, 18, 0, 2],
  );
  assert.deepEqual(r.bbox, { min: [0, 0, 0], max: [1, 1, 1] });
});

test('the WebAssembly build reads a PLY tetrahedron', () => {
  const r = readMesh(lib, sample('tetrahedron.ply'));
  assert.deepEqual([r.ok, r.format, r.vertices, r.triangles, r.euler], [true, 'PLY', 4, 4, 2]);
});

test('the WebAssembly build reads an ASCII STL cube, welding its corners', () => {
  const r = readMesh(lib, sample('cube.stl'));
  assert.deepEqual([r.ok, r.format, r.vertices, r.triangles, r.edges, r.euler], [true, 'STL', 8, 12, 18, 2]);
});

test('errors come back with their status and line', () => {
  const r = readMesh(lib, new TextEncoder().encode('v 0 0 0\nv 1 0 0\nf 1 2 3\n'));
  assert.deepEqual(r, { ok: false, status: 4, message: 'index out of range', line: 3 });
});

test('the wrapper refuses files over the size limit without calling WebAssembly', () => {
  const r = readMesh(lib, { byteLength: MAX_BYTES + 1 });
  assert.deepEqual(r, { ok: false, status: 'too-large' });
});

test('memory is released: 64 reads of a 1 MB file do not grow the 16 MB heap', () => {
  const bytes = new TextEncoder().encode(`# ${'x'.repeat(1 << 20)}\n${sample('cube.obj')}`);
  readMesh(lib, bytes);
  const before = lib.HEAPU8.length;
  for (let i = 0; i < 64; i++) assert.equal(readMesh(lib, bytes).ok, true);
  assert.equal(lib.HEAPU8.length, before);
});

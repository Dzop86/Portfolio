// The height filtration of the topology viewer (topo-api.js): drawing order and counts, without WebAssembly.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { criticalCounts, filtration } from '../../src/assets/topo-api.js';

// Two triangles sharing an edge; vertex heights 0, 1, 2, 3 (vertex 3 highest).
const indices = new Uint32Array([1, 2, 3, 0, 1, 2]);
const height = new Float32Array([0, 1, 2, 3]);
const order = new Uint32Array([0, 1, 2, 3]);

test('triangles come in the order of their highest vertex, so a sublevel set is a prefix', () => {
  const f = filtration(indices, { height, order });
  assert.deepEqual([...f.indices], [0, 1, 2, 1, 2, 3]);
  assert.deepEqual([0, 1, 2, 3].map((r) => f.faces(r)), [0, 0, 1, 2]);
});

test('the rank at a height is the last vertex at or below it', () => {
  const f = filtration(indices, { height, order });
  assert.deepEqual([-1, 0, 0.5, 2, 2.99, 3, 10].map((h) => f.rankAt(h)), [-1, 0, 0, 2, 2, 3, 3]);
});

test('saddles count with their multiplicity, and the indices sum to chi', () => {
  const c = criticalCounts([
    { vertex: 0, kind: 'min', index: 1 },
    { vertex: 5, kind: 'saddle', index: -1 },
    { vertex: 6, kind: 'saddle', index: -2 },
    { vertex: 9, kind: 'max', index: 1 },
  ]);
  assert.deepEqual(c, { min: 1, saddle: 3, max: 1, other: 0, sum: -1 });
});

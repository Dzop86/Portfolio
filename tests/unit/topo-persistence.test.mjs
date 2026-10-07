// The persistence diagram of the topology viewer (topo-api.js): threshold, drawing cap and marked vertices,
// without WebAssembly.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { diagram } from '../../src/assets/topo-api.js';

const pair = (dimension, birthVertex, deathVertex, birth, death) => ({ dimension, birthVertex, deathVertex, birth, death });
const pairs = [
  pair(0, 1, 2, 0, 0.05),  // noise
  pair(0, 3, 4, 0.1, 0.9),
  pair(1, 5, 6, 0.4, 0.6),
  pair(1, 7, 7.5, 0.5, 0.5),  // a plateau tie: zero persistence
  pair(0, 0, null, -1, Infinity),
  pair(1, 8, null, 0.3, Infinity),
  pair(2, 9, null, 1, Infinity),
];

test('the threshold keeps the pairs that live longer than it, and every essential class', () => {
  const d = diagram(pairs, 0.1);
  assert.deepEqual(d.finite, [1, 1, 0]);
  assert.deepEqual(d.essential, [1, 1, 1]);
  assert.equal(d.kept.length, 5);
  assert.deepEqual([...d.vertices].sort((a, b) => a - b), [0, 3, 4, 5, 6, 8, 9]);
  // At zero, everything but the plateau ties, which tie-breaking made, not the shape.
  assert.equal(diagram(pairs, 0).kept.length, pairs.length - 1);
  assert.ok(!diagram(pairs, 0).vertices.has(7));
  // Above every finite pair, only the essential classes (the Betti numbers).
  assert.deepEqual(diagram(pairs, 5).finite, [0, 0, 0]);
  assert.deepEqual(diagram(pairs, 5).essential, [1, 1, 1]);
});

test('at most `max` points are drawn, the most persistent first, and the rest is counted', () => {
  const d = diagram(pairs, 0, 4);
  assert.equal(d.drawn.length, 4);
  assert.equal(d.hidden, 2);
  // The three essential classes, then the longest finite pair.
  assert.deepEqual(d.drawn.map((p) => p.birthVertex), [0, 8, 9, 3]);
  assert.equal(diagram(pairs, 0).hidden, 0);
});

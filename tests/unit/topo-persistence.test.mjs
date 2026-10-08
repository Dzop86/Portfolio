// The persistence diagram of the topology viewer (topo-api.js): threshold, drawing cap and marked vertices,
// the barcode, the classes alive at a height and the guided explanation (sprint 39), without WebAssembly.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { aliveAt, barcode, diagram, levelAt, levelBetween, tour } from '../../src/assets/topo-api.js';

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

test('the barcode groups the bars by dimension, from the earliest birth, without the zero-length pairs', () => {
  const b = barcode(pairs);
  assert.deepEqual(b.groups.map((g) => g.map((p) => p.birthVertex)), [[0, 1, 3], [8, 5], [9]]);
  assert.equal(b.hidden, 0);
  // Capped like the diagram: the most persistent are kept, then grouped.
  const capped = barcode(pairs, 3);
  assert.deepEqual(capped.groups.map((g) => g.map((p) => p.birthVertex)), [[0], [8], [9]]);
  assert.equal(capped.hidden, 3);
});

test('the classes alive at a height are born at or below it and not dead yet', () => {
  assert.deepEqual(aliveAt(pairs, -2), [0, 0, 0]);
  assert.deepEqual(aliveAt(pairs, -1), [1, 0, 0]);
  // Born at 0, dead at 0.05: alive at its birth, not at its death.
  assert.deepEqual(aliveAt(pairs, 0), [2, 0, 0]);
  assert.deepEqual(aliveAt(pairs, 0.05), [1, 0, 0]);
  // The plateau tie (0.5, 0.5) is never alive.
  assert.deepEqual(aliveAt(pairs, 0.5), [2, 2, 0]);
  assert.deepEqual(aliveAt(pairs, 1), [1, 1, 1]);
});

test('the slider goes between a birth and a death, where the pair is alive', () => {
  const lo = -1, hi = 1;
  const at = (v) => lo + (v / 1000) * (hi - lo);
  for (const p of pairs.filter((q) => q.death - q.birth > 0.002)) {
    const v = levelBetween(p, lo, hi);
    assert.ok(v >= 0 && v <= 1000);
    assert.ok(p.birth <= at(v) && at(v) < p.death, `${p.birthVertex} at ${v}`);
  }
  // Halfway: born at 0.1 (550), dead at 0.9 (950).
  assert.equal(levelBetween(pairs[1], lo, hi), 749);
  // Never dies: between its birth and the top.
  assert.equal(levelBetween(pairs[5], lo, hi), 825);
  // Too short to fall between two positions: at its birth.
  assert.equal(levelBetween(pair(0, 1, 2, 0.1001, 0.1002), lo, hi), 551);
  assert.equal(levelAt(-5, lo, hi), 0);
  assert.equal(levelAt(0.1, lo, hi), 550);
});

test('the guided explanation follows the level up, one step per kind of event', () => {
  const steps = tour(pairs, -1, 1);
  // The loop told is the longest one, which never dies: born at 0.3, before the second component dies.
  assert.deepEqual(steps.map((s) => s.key), ['first', 'second', 'loop', 'merge', 'cavity', 'end']);
  assert.deepEqual(steps.map((s) => s.level), [0, 550, 650, 950, 1000, 1000]);
  // The second component is the longest finite H0 pair, not the noise.
  assert.equal(steps[1].pair.birthVertex, 3);
  assert.equal(steps[2].pair.birthVertex, 8);
  assert.equal(steps.at(-1).pair, null);
  // A loop that dies is filled at its death.
  const filled = tour([pair(0, 0, null, -1, Infinity), pair(1, 5, 6, 0.4, 0.6)], -1, 1);
  assert.deepEqual(filled.map((s) => [s.key, s.level]), [['first', 0], ['loop', 700], ['fill', 800], ['end', 1000]]);
});

test('a sphere tells a component, a cavity and the end; a lone vertex only the end', () => {
  const sphere = [pair(0, 0, null, -1, Infinity), pair(2, 1, null, 1, Infinity)];
  assert.deepEqual(tour(sphere, -1, 1).map((s) => s.key), ['first', 'cavity', 'end']);
  assert.deepEqual(tour([], 0, 0).map((s) => s.key), ['end']);
});

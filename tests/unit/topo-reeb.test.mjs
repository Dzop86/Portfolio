// The Reeb graph of the topology viewer (topo-api.js): the kind of each node, and the arcs clipped at the
// height threshold, without WebAssembly.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { clipArcs, nodeKind } from '../../src/assets/topo-api.js';

test('a node is a minimum with nothing below, a maximum with nothing above, a saddle otherwise', () => {
  assert.equal(nodeKind({ down: 0, up: 1 }), 'min');
  assert.equal(nodeKind({ down: 1, up: 0 }), 'max');
  assert.equal(nodeKind({ down: 1, up: 2 }), 'saddle');
  assert.equal(nodeKind({ down: 2, up: 1 }), 'saddle');
  assert.equal(nodeKind({ down: 0, up: 0 }), 'other');  // an isolated vertex
});

// Two nodes, vertex 0 at height 0 and vertex 1 at height 1 (along y), one arc through (0, 0.5, 0).
const positions = new Float32Array([0, 0, 0, 0, 1, 0]);
const height = new Float32Array([0, 1]);
const graph = { nodes: [{ vertex: 0, down: 0, up: 1 }, { vertex: 1, down: 1, up: 0 }], arcs: [{ lower: 0, upper: 1, path: [0.2, 0.5, 0] }] };

test('arcs are clipped at the threshold, ending on it', () => {
  assert.deepEqual(clipArcs(graph, positions, height, [0, 1, 0], 2), [[0, 0, 0, 0.2, 0.5, 0, 0, 1, 0]]);
  // Halfway between the path point and the top: the arc stops on the threshold.
  const [line] = clipArcs(graph, positions, height, [0, 1, 0], 0.75);
  assert.equal(line.length, 9);
  assert.deepEqual(line.slice(0, 6), [0, 0, 0, 0.2, 0.5, 0]);
  assert.ok(Math.abs(line[6] - 0.1) < 1e-9 && Math.abs(line[7] - 0.75) < 1e-9 && line[8] === 0);
  // Below the lower node, nothing.
  assert.deepEqual(clipArcs(graph, positions, height, [0, 1, 0], -1), []);
});

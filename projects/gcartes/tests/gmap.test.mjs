// Unit tests of the 2-G-map library on surfaces whose invariants are known: a square, the cube,
// tori, cylinders, the Moebius strip; and the constraints a broken map violates.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { CUBE_FACES, GMap, ORBITS, fromFaces, gridFaces, moebiusFaces } from '../src/gmap.js';

const inv = (faces) => fromFaces(faces).map.invariants();
const pick = (o, keys) => Object.fromEntries(keys.map((k) => [k, o[k]]));
const KEYS = ['darts', 'vertices', 'edges', 'faces', 'components', 'euler', 'boundaryLoops', 'orientable', 'genus'];

test('a square: 8 darts, one face, one boundary loop', () => {
  assert.deepEqual(pick(inv([[0, 1, 2, 3]]), KEYS),
    { darts: 8, vertices: 4, edges: 4, faces: 1, components: 1, euler: 1, boundaryLoops: 1, orientable: true, genus: 0 });
});

test('the cube: 48 darts, 8 vertices, 12 edges, 6 faces, chi = 2, a sphere', () => {
  const { map, darts } = fromFaces(CUBE_FACES);
  assert.deepEqual(map.check(), []);
  assert.deepEqual(pick(map.invariants(), KEYS),
    { darts: 48, vertices: 8, edges: 12, faces: 6, components: 1, euler: 2, boundaryLoops: 0, orientable: true, genus: 0 });
  // Each vertex orbit <alpha1, alpha2> of the cube has 6 darts (3 faces meet, 2 darts each), all at that vertex.
  for (const orbit of map.cells(ORBITS.vertex)) {
    assert.equal(orbit.length, 6);
    assert.equal(new Set(orbit.map((d) => darts[d].vertex)).size, 1);
  }
  for (const orbit of map.cells(ORBITS.edge)) assert.equal(orbit.length, 4);
  for (const orbit of map.cells(ORBITS.face)) assert.equal(orbit.length, 8);
});

test('every alpha is an involution, alpha0 alpha2 too, and no dart of a closed surface is 2-free', () => {
  const { map } = fromFaces(CUBE_FACES);
  for (let d = 0; d < map.size; d++) {
    for (let i = 0; i <= 2; i++) {
      assert.equal(map.alpha[i][map.alpha[i][d]], d);
      assert.notEqual(map.alpha[i][d], d, `dart ${d} is ${i}-free`);
    }
    assert.equal(map.alpha[0][map.alpha[2][map.alpha[0][map.alpha[2][d]]]], d);
  }
});

test('tori of several sizes: chi = 0, genus 1; cylinders: two boundary loops', () => {
  for (const [n, m] of [[3, 3], [4, 6], [10, 7]]) {
    const t = inv(gridFaces(n, m, true));
    assert.deepEqual(pick(t, ['vertices', 'edges', 'faces', 'euler', 'boundaryLoops', 'orientable', 'genus']),
      { vertices: n * m, edges: 2 * n * m, faces: n * m, euler: 0, boundaryLoops: 0, orientable: true, genus: 1 });
    const c = inv(gridFaces(n, m, false));
    assert.deepEqual(pick(c, ['euler', 'boundaryLoops', 'orientable', 'genus']), { euler: 0, boundaryLoops: 2, orientable: true, genus: 0 });
  }
});

test('the Moebius strip is not orientable and has a single boundary loop', () => {
  for (const k of [3, 5, 12]) {
    const { map } = fromFaces(moebiusFaces(k));
    assert.deepEqual(map.check(), []);
    assert.deepEqual(pick(map.invariants(), ['euler', 'boundaryLoops', 'orientable', 'components']),
      { euler: 0, boundaryLoops: 1, orientable: false, components: 1 });
  }
});

test('two separate cubes: two components, chi = 4', () => {
  const shifted = CUBE_FACES.map((f) => f.map((v) => v + 8));
  assert.deepEqual(pick(inv([...CUBE_FACES, ...shifted]), ['components', 'euler', 'genus']), { components: 2, euler: 4, genus: 0 });
});

test('orbits are closed under their involutions and start from the given dart', () => {
  const { map } = fromFaces(CUBE_FACES);
  const orbit = map.orbit(5, ORBITS.edge);
  assert.equal(orbit[0], 5);
  for (const d of orbit) for (const i of ORBITS.edge) assert.ok(orbit.includes(map.alpha[i][d]));
  assert.equal(map.orbit(0, ORBITS.component).length, 48);
});

test('check reports the constraints a hand-made map violates', () => {
  const m = new GMap();
  for (let k = 0; k < 4; k++) m.addDart();
  m.link(0, 0, 1);
  m.link(2, 0, 2);
  // alpha0 alpha2 alpha0 alpha2 from 0: 0 -2-> 2 -0-> 2 -2-> 0 -0-> 1, not 0.
  assert.ok(m.check().some((p) => /alpha0 alpha2 alpha0 alpha2 does not close at dart 0/.test(p)));
  m.alpha[1][3] = 0; // breaks the involution by hand
  assert.ok(m.check().includes('alpha1 is not an involution at dart 3'));
});

test('linking refuses a dart already linked, and a side shared by three faces is not a surface', () => {
  const m = new GMap();
  [0, 1, 2].forEach(() => m.addDart());
  m.link(1, 0, 1);
  assert.doesNotThrow(() => m.link(1, 1, 0));
  assert.throws(() => m.link(1, 0, 2), /already linked/);
  assert.throws(() => fromFaces([[0, 1, 2], [1, 0, 3], [0, 1, 4]]), /shared by 3 faces/);
  assert.throws(() => fromFaces([[0, 1]]), /fewer than 3/);
});

test('the course\'s cube net is a valid cube: every edge on two squares, chi = 2', async () => {
  const { cubeNetMap, dartGeometry } = await import('../src/net.js');
  const { map, darts } = cubeNetMap();
  assert.deepEqual(map.check(), []);
  assert.deepEqual(pick(map.invariants(), ['darts', 'vertices', 'edges', 'faces', 'euler', 'boundaryLoops']),
    { darts: 48, vertices: 8, edges: 12, faces: 6, euler: 2, boundaryLoops: 0 });
  // Linked darts sit at the same vertex for alpha1 and alpha2, at the two ends of a side for alpha0.
  for (let d = 0; d < map.size; d++) {
    assert.equal(darts[map.alpha[1][d]].letter, darts[d].letter);
    assert.equal(darts[map.alpha[2][d]].letter, darts[d].letter);
    assert.notEqual(darts[map.alpha[0][d]].letter, darts[d].letter);
  }
  // Every dart is drawn inside its square.
  dartGeometry(darts).forEach((g, d) => {
    const { col, row } = darts[d].square;
    for (const [x, y] of [g.start, g.end]) assert.ok(x > col * 100 && x < col * 100 + 100 && y > row * 100 && y < row * 100 + 100);
  });
});

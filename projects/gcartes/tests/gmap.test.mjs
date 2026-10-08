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

// The claims of lesson 3 and of the quiz, on the cube: alpha0 and alpha2 commute; alpha0 alpha1 needs 4 steps
// round a square, alpha1 alpha2 3 steps round a vertex, so neither is an involution.
test('alpha0 and alpha2 commute; alpha0 alpha1 and alpha1 alpha2 are not involutions on the cube', () => {
  const { map } = fromFaces(CUBE_FACES);
  const [a0, a1, a2] = map.alpha;
  const order = (f, d) => { let k = 1; for (let e = f(d); e !== d; e = f(e)) k++; return k; };
  for (let d = 0; d < map.size; d++) {
    assert.equal(a2[a0[d]], a0[a2[d]], `dart ${d}`);
    assert.equal(order((e) => a0[a1[e]], d), 4, `dart ${d}: round a square`);
    assert.equal(order((e) => a1[a2[e]], d), 3, `dart ${d}: round a vertex`);
  }
});

test('an edge sewn at one end only breaks the constraint, a boundary free at both ends does not', () => {
  const half = new GMap();
  for (let k = 0; k < 4; k++) half.addDart();
  half.link(0, 0, 1); // side of A: 0 at v, 1 at w
  half.link(0, 2, 3); // side of B: 2 at v, 3 at w
  half.link(2, 0, 2); // sewn at v only
  assert.ok(half.check().some((p) => /does not close/.test(p)));
  half.link(2, 1, 3); // and at w: the edge is sewn in one piece
  assert.deepEqual(half.check(), []);
  assert.deepEqual(fromFaces([[0, 1, 2, 3]]).map.check(), []); // a lone square: every side free at both ends
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

test('the decomposition of two squares goes up the dimensions: object, alpha0, alpha1, alpha2, G-map', async () => {
  const { decompositionStep, STEPS } = await import('../src/decompose.js');
  const { fromFaces } = await import('../src/gmap.js');
  const count = (step, alpha) => step.links.filter((l) => l.alpha === alpha).length;
  assert.equal(STEPS, 5);
  const steps = [0, 1, 2, 3, 4].map((k) => decompositionStep(k));
  // Step 0: the object (2 faces, 6 vertices); no darts, no links yet.
  assert.deepEqual([steps[0].faces.length, steps[0].dots.length, steps[0].darts.length, steps[0].links.length], [2, 6, 0, 0]);
  // alpha0: each of the 8 sides of the faces becomes two darts, joined by one alpha0 link.
  assert.equal(steps[1].darts.length, 16);
  assert.deepEqual([count(steps[1], 0), count(steps[1], 1), count(steps[1], 2)], [8, 0, 0]);
  // alpha1: the 8 corners of the faces.
  assert.deepEqual([count(steps[2], 0), count(steps[2], 1), count(steps[2], 2)], [8, 8, 0]);
  // alpha2: the shared edge sewn, one link at each of its ends; then the G-map, its faces back.
  assert.deepEqual([count(steps[3], 0), count(steps[3], 1), count(steps[3], 2)], [8, 8, 2]);
  assert.deepEqual([count(steps[4], 0), count(steps[4], 1), count(steps[4], 2)], [8, 8, 2]);
  assert.equal(steps[4].faces.length, 2);
  // Each link joins the right darts, at the right place on them.
  const { map } = fromFaces([[0, 1, 4, 5], [1, 2, 3, 4]]);
  const same = (p, q) => Math.hypot(p[0] - q[0], p[1] - q[1]) < 1e-12;
  for (const step of steps.slice(1)) {
    for (const l of step.links) {
      const [d, e] = l.darts;
      assert.equal(map.alpha[l.alpha][d], e, `alpha${l.alpha} ${d}`);
      const [innerD, endD] = step.darts[d];
      const [innerE, endE] = step.darts[e];
      if (l.alpha === 0) assert.ok(same(l.from, innerD) && same(l.to, innerE));
      if (l.alpha === 1) assert.ok(same(l.from, endD) && same(l.to, endE));
      if (l.alpha === 2) assert.ok(same(l.from, [(innerD[0] + endD[0]) / 2, (innerD[1] + endD[1]) / 2]));
    }
  }
  // Without being mistaken for a dart: alpha0 strokes are shorter than half a dart, alpha1 arcs turn around a corner
  // the two darts share, alpha2 strokes cross the darts instead of running along them.
  const len = ([a, b]) => Math.hypot(a[0] - b[0], a[1] - b[1]);
  const dart = len(steps[3].darts[0]);
  for (const l of steps[3].links) {
    if (l.alpha === 0) assert.ok(len([l.from, l.to]) < dart / 2, 'alpha0 short');
    if (l.alpha === 1) assert.ok(l.corner && len([l.corner, l.from]) < dart && len([l.corner, l.to]) < dart, 'alpha1 at a corner');
    if (l.alpha === 2) {
      const [inner, end] = steps[3].darts[l.darts[0]];
      const u = [end[0] - inner[0], end[1] - inner[1]], v = [l.to[0] - l.from[0], l.to[1] - l.from[1]];
      const cos = Math.abs(u[0] * v[0] + u[1] * v[1]) / (Math.hypot(...u) * Math.hypot(...v));
      assert.ok(cos < 0.1, 'alpha2 across the darts');
    }
  }
  // The faces come apart once cut into darts, and touch again in the G-map less than in the cut steps.
  const xs = (step, f) => step.faces[f].map((p) => p[0]);
  assert.equal(Math.max(...xs(steps[0], 0)), Math.min(...xs(steps[0], 1)));
  assert.ok(Math.min(...xs(steps[4], 1)) - Math.max(...xs(steps[4], 0)) > 0);
  for (const [inner, end] of steps[1].darts) assert.ok(len([inner, end]) > 0.2);
});

test('links are drawn as in textbooks: a stroke, an arc, a double stroke', async () => {
  const { linkPath, DOUBLE_GAP } = await import('../src/links.js');
  assert.equal(linkPath(0, [0, 0], [10, 0]), 'M0.0,0.0 L10.0,0.0');
  // alpha1: a quadratic arc from one dart to the other, controlled by their corner.
  assert.equal(linkPath(1, [10, 0], [0, 10], [0, 0]), 'M10.0,0.0 Q0.0,0.0 0.0,10.0');
  // alpha2: two parallel strokes, DOUBLE_GAP apart, on each side of the line between the darts.
  const d = linkPath(2, [0, 0], [10, 0]);
  const nums = d.match(/-?\d+\.\d/g).map(Number);
  assert.equal(d.split('M').length - 1, 2);
  assert.deepEqual(nums, [0, DOUBLE_GAP / 2, 10, DOUBLE_GAP / 2, 0, -DOUBLE_GAP / 2, 10, -DOUBLE_GAP / 2].map((x) => Math.round(x * 10) / 10));
});

test('in the cube figure, alpha1 links are long enough to see, and alpha0 leaves a gap between halves', async () => {
  const { cubeNetMap, dartGeometry } = await import('../src/net.js');
  const { map, darts } = cubeNetMap();
  const g = dartGeometry(darts);
  const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
  for (let d = 0; d < map.size; d++) {
    assert.ok(dist(g[d].end, g[map.alpha[1][d]].end) >= 10, `alpha1 at dart ${d}`);
    assert.ok(dist(g[d].start, g[map.alpha[0][d]].start) >= 10, `alpha0 at dart ${d}`);
    // The two darts of an alpha1 link share the corner their arc turns around.
    assert.deepEqual(g[d].corner, g[map.alpha[1][d]].corner, `corner at dart ${d}`);
  }
});

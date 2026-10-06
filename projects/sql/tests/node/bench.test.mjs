// Unit tests of the mesh generator and the CSV writer, and an integration test of a quick campaign
// through the WebAssembly builds of lib-c and topologie.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { FAMILIES, FORMATS, encode, makeMesh } from '../../bench/meshes.mjs';
import { csvField, csvLine, parseCsv } from '../../bench/csv.mjs';
import { COLUMNS, QUICK, loadImplementations, runCampaign } from '../../bench/run.mjs';

test('every family has 4 k² triangles and the vertex count its topology implies', () => {
  for (const k of [3, 5, 16]) {
    const counts = Object.fromEntries(Object.keys(FAMILIES).map((f) => {
      const m = makeMesh(f, k);
      return [f, [m.vertices.length / 3, m.triangles.length / 3]];
    }));
    assert.deepEqual(counts, {
      torus: [2 * k * k, 4 * k * k],
      cylinder: [2 * k * (k + 1), 4 * k * k],
      sphere: [2 * k * k + 2, 4 * k * k],
    });
  }
});

test('triangle indices stay in range and no triangle repeats a corner', () => {
  for (const f of Object.keys(FAMILIES)) {
    const { vertices, triangles } = makeMesh(f, 6);
    const nv = vertices.length / 3;
    for (let t = 0; t < triangles.length; t += 3) {
      const [a, b, c] = triangles.subarray(t, t + 3);
      assert.ok(a < nv && b < nv && c < nv, `${f}: index out of range`);
      assert.ok(a !== b && b !== c && a !== c, `${f}: degenerate triangle ${t / 3}`);
    }
  }
});

test('the generator is deterministic', () => {
  for (const format of FORMATS) {
    assert.deepEqual(encode(makeMesh('sphere', 5), format), encode(makeMesh('sphere', 5), format));
  }
});

test('the encodings have the sizes their formats dictate', () => {
  const m = makeMesh('torus', 4); // 32 vertices, 64 triangles
  assert.equal(encode(m, 'stl').byteLength, 84 + 50 * 64);
  const ply = encode(m, 'ply');
  const header = new TextDecoder().decode(ply).indexOf('end_header\n') + 'end_header\n'.length;
  assert.equal(ply.byteLength - header, 12 * 32 + 13 * 64);
  const obj = new TextDecoder().decode(encode(m, 'obj'));
  assert.equal(obj.match(/^v /gm).length, 32);
  assert.equal(obj.match(/^f /gm).length, 64);
});

test('bad arguments are refused', () => {
  assert.throws(() => makeMesh('klein', 4), /Unknown family/);
  assert.throws(() => makeMesh('torus', 2), /Resolution/);
  assert.throws(() => encode(makeMesh('torus', 3), 'gltf'), /Unknown format/);
});

test('CSV fields are quoted only when needed', () => {
  assert.equal(csvField('Intel(R) Core(TM) i5'), 'Intel(R) Core(TM) i5');
  assert.equal(csvField('a, b'), '"a, b"');
  assert.equal(csvField('say "hi"'), '"say ""hi"""');
  assert.equal(csvLine([1, 'x,y', 2.5]), '1,"x,y",2.5');
});

test('parseCsv reads back what csvLine writes', () => {
  const rows = [['a', 'b, c', 'say "hi"'], ['', 'two\nlines', '3']];
  assert.deepEqual(parseCsv(`${rows.map(csvLine).join('\r\n')}\n`), rows);
  assert.deepEqual(parseCsv('x,y'), [['x', 'y']]);
  assert.throws(() => parseCsv('"open'), /Unterminated/);
});

test('both libraries find the invariants of every family in every format', async () => {
  const impls = await loadImplementations();
  for (const [family, expected] of Object.entries(FAMILIES)) {
    const mesh = makeMesh(family, 5);
    const want = { vertices: mesh.vertices.length / 3, triangles: mesh.triangles.length / 3, ...expected };
    for (const format of FORMATS) {
      for (const [name, impl] of Object.entries(impls)) {
        assert.ok(impl.check(impl.run(encode(mesh, format)), want), `${name}, ${family}, ${format}`);
      }
    }
  }
});

test('the check catches a wrong answer', async () => {
  const impls = await loadImplementations();
  const torus = encode(makeMesh('torus', 4), 'obj');
  assert.equal(impls['lib-c'].check(impls['lib-c'].run(torus), { vertices: 32, triangles: 64, euler: 2 }), false);
  assert.equal(impls.topologie.check(impls.topologie.run(torus), { euler: 0, boundaryLoops: 2 }), false);
});

test('a quick campaign writes one row per mesh, format, implementation and repetition', async () => {
  const [header, ...rows] = parseCsv(await runCampaign(QUICK));
  assert.deepEqual(header, COLUMNS);
  const expected = Object.keys(FAMILIES).length * QUICK.resolutions.length * FORMATS.length * 2 * QUICK.repetitions;
  assert.equal(rows.length, expected);
  for (const row of rows) {
    assert.equal(row.length, COLUMNS.length);
    assert.ok(Number(row.at(-1)) > 0, 'duration is positive');
  }
});

// Computing times of the topology viewer (sprint 40): the helpers that time and show them, the measured table and
// the project page that prints it, with its notes on the GPU and on the 32 MB limit.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';
import { benchDuration, readTopoBench } from '../../src/templates.mjs';
import { duration, loadTopo, readTopology, timed } from '../../src/assets/topo-api.js';
import { MAX_BYTES } from '../../src/assets/meshlib-api.js';
import { SIZES, torusObj } from '../../projects/topologie/scripts/bench.mjs';

test('timed returns the value and the time between two clock readings', () => {
  const clock = [10, 12.5];
  const r = timed(() => 42, () => clock.shift());
  assert.deepEqual(r, { value: 42, ms: 2.5 });
});

test('durations read in milliseconds, then seconds', () => {
  assert.equal(duration(0.4, 'en'), '< 1 ms');
  assert.equal(duration(12.6, 'en'), '13 ms');
  assert.equal(duration(2171.7, 'en'), '2.17 s');
  assert.equal(duration(2171.7, 'fr'), '2,17 s');
  assert.equal(benchDuration(597.32, 'fr'), '597 ms');
  assert.equal(benchDuration(1234.5, 'en'), '1.23 s');
});

test('the benchmark torus is the native one: same sizes, and a torus once read', async () => {
  assert.deepEqual(SIZES.map(([n, m]) => 2 * n * m), [10000, 30000, 100000, 300000, 1000000]);
  const lib = await loadTopo((await import('../../src/assets/wasm/topo.js')).default);
  const r = readTopology(lib, new TextEncoder().encode(torusObj(20, 10)));
  assert.equal(r.ok, true);
  assert.equal(r.indices.length / 3, 400);
  assert.deepEqual([r.invariants.euler, r.invariants.genus, r.invariants.boundaryLoops], [0, 1, 0]);
  // The page's limit can be lifted by the benchmark only.
  const big = new Uint8Array(MAX_BYTES + 1);
  assert.equal(readTopology(lib, big).status, 'too-large');
});

test('the measured table: every size natively, WebAssembly refused above the page limit only', () => {
  const bench = readTopoBench();
  assert.equal(bench.limits.bytes, MAX_BYTES);
  assert.deepEqual(bench.rows.map((r) => r.triangles), SIZES.map(([n, m]) => 2 * n * m));
  for (const r of bench.rows) {
    for (const step of ['elevation', 'persistence', 'reeb']) assert.ok(r.native[step] > 0, `${r.triangles} ${step}`);
    assert.ok(r.wasm.read > 0 && r.wasm.elevation > 0);
    const refused = r.triangles > bench.limits.triangles;
    assert.equal(r.wasm.persistence === null, refused, `${r.triangles}`);
    assert.equal(r.wasm.reeb === null, refused, `${r.triangles}`);
  }
  // The largest file is over the page's 32 MB: the reason for the note.
  assert.ok(bench.rows.at(-1).bytes > MAX_BYTES);
});

test('the topology page prints the table, the GPU note and the 32 MB note, and the timing lines', () => {
  const out = build(mkdtempSync(join(tmpdir(), 'topo-bench-')));
  const bench = readTopoBench();
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-topologie.html'), 'utf8');
    const table = html.slice(html.indexOf('<table data-topo-bench>'), html.indexOf('</table>', html.indexOf('<table data-topo-bench>')));
    assert.equal((table.match(/<tr><th scope="row"/g) || []).length, bench.rows.length, lang);
    assert.ok(html.includes(bench.machine.cpu), lang);
    assert.ok(html.includes('data-topo-gpu') && html.includes('data-topo-limit'), lang);
    assert.match(html, lang === 'fr' ? /refusé par la page/ : /refused by the page/);
    for (const step of ['read', 'height', 'persistence', 'reeb']) assert.ok(html.includes(`data-time="${step}"`), `${lang} ${step}`);
  }
});

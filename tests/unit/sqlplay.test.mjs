// Integration test of the SQL playground's core on the real campaign, with sql.js in Node: the SQLite
// copy must hold the same data and give the same answers as values computed here independently.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import initSqlJs from 'sql.js';
import { ROOT } from '../../src/lib.mjs';
import { parseCsv } from '../../projects/sql/bench/csv.mjs';
import { MAX_ROWS, describe, openBenchDb, parseExamples, runQuery } from '../../src/sqlplay/core.js';

const SQL = await initSqlJs();
const read = (path) => readFileSync(join(ROOT, path), 'utf8');
const schema = read('projects/sql/sqlite/schema.sql');
const csv = read('projects/sql/data/measurements.csv');
const db = openBenchDb(SQL, schema, csv);
const [header, ...rows] = parseCsv(csv);
const col = (name) => header.indexOf(name);
const value = (sql) => runQuery(db, sql).rows[0][0];

const median = (xs) => {
  const s = [...xs].sort((a, b) => a - b);
  return s.length % 2 ? s[(s.length - 1) / 2] : (s[s.length / 2 - 1] + s[s.length / 2]) / 2;
};

// Durations grouped by file and implementation, straight from the CSV.
const groups = new Map();
for (const r of rows) {
  const key = [r[col('family')], Number(r[col('resolution')]), r[col('format')], r[col('implementation')]].join('|');
  groups.set(key, [...(groups.get(key) ?? []), Number(r[col('duration_ms')])]);
}

test('every CSV row becomes one measurement, normalised into 45 files', () => {
  assert.equal(value('SELECT count(*) FROM measurement'), rows.length);
  assert.equal(value('SELECT count(*) FROM file'), 45);
  assert.equal(value('SELECT count(*) FROM mesh'), 15);
  assert.equal(value('SELECT count(*) FROM campaign'), 1);
});

test('the timing view gives the median of each group, as computed in JavaScript', () => {
  const { rows: timing } = runQuery(db, 'SELECT family, resolution, format, implementation, median_ms, runs FROM timing', 1000);
  assert.equal(timing.length, groups.size);
  for (const [family, resolution, format, impl, med, runs] of timing) {
    const xs = groups.get([family, resolution, format, impl].join('|'));
    assert.equal(runs, xs.length);
    assert.ok(Math.abs(med - median(xs)) < 1e-9, `${family} ${resolution} ${format} ${impl}: ${med} vs ${median(xs)}`);
  }
});

test('the complexity view is the least-squares slope of log time against log size', () => {
  for (const [impl, format, exponent] of runQuery(db, 'SELECT implementation, format, exponent FROM complexity').rows) {
    const pts = [...groups].filter(([k]) => k.endsWith(`|${format}|${impl}`)).map(([k, xs]) => {
      const res = Number(k.split('|')[1]);
      return [Math.log(4 * res * res), Math.log(median(xs))];
    });
    const n = pts.length;
    const mx = pts.reduce((a, [x]) => a + x, 0) / n;
    const my = pts.reduce((a, [, y]) => a + y, 0) / n;
    const slope = pts.reduce((a, [x, y]) => a + (x - mx) * (y - my), 0) / pts.reduce((a, [x]) => a + (x - mx) ** 2, 0);
    assert.equal(n, 15);
    assert.ok(Math.abs(exponent - slope) < 1e-9, `${impl} ${format}: ${exponent} vs ${slope}`);
  }
});

test('format_ranking ranks each mesh from its fastest format, which has a slowdown of 1', () => {
  assert.equal(value('SELECT count(*) FROM format_ranking WHERE rank = 1 AND abs(slowdown - 1) > 1e-12'), 0);
  assert.equal(value('SELECT count(*) FROM format_ranking WHERE rank = 1'), 30);
});

test('the SQLite copy keeps the constraints of the PostgreSQL schema', () => {
  const fails = (sql, re) => assert.throws(() => runQuery(db, sql), re, sql);
  fails('INSERT INTO measurement VALUES (1, 1, 1, 8, 1.0)', /repetition exceeds/);
  fails('INSERT INTO measurement VALUES (1, 1, 1, 1, 1.0)', /UNIQUE/);
  fails('UPDATE measurement SET duration_ms = 0 WHERE rowid = 1', /CHECK/);
  fails("INSERT INTO file (mesh_id, format, bytes) VALUES (1, 'gltf', 1)", /CHECK/);
  fails("INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops) VALUES ('sphere', 999, 10, 16, 0, 0)", /CHECK/);
  fails('INSERT INTO measurement VALUES (1, 1, 99, 1, 1.0)', /FOREIGN KEY/);
  fails("UPDATE measurement SET duration_ms = 'slow' WHERE rowid = 1", /cannot store TEXT value in REAL column/);
});

test('a CSV that breaks a constraint loads nothing', () => {
  const bad = csv.replace(/,lib-c,(\d+),/, ',fortran,$1,');
  assert.throws(() => openBenchDb(SQL, schema, bad), /NOT NULL/);
});

test('runQuery caps the rows, keeps the true count, and returns the last result', () => {
  const r = runQuery(db, 'SELECT * FROM measurement');
  assert.deepEqual([r.rows.length, r.total, r.truncated], [MAX_ROWS, rows.length, true]);
  assert.equal(r.columns.length, 5);
  const last = runQuery(db, 'SELECT 1 AS a; SELECT 2 AS b, 3 AS c');
  assert.deepEqual([last.columns, last.rows, last.truncated], [['b', 'c'], [[2, 3]], false]);
  assert.throws(() => runQuery(db, 'SELECT * FROM nowhere'), /no such table/);
});

test('changes are counted, and stay in the visitor\'s copy only', () => {
  const copy = openBenchDb(SQL, schema, csv);
  const r = runQuery(copy, "DELETE FROM measurement WHERE implementation_id = (SELECT implementation_id FROM implementation WHERE name = 'lib-c')");
  assert.deepEqual([r.columns, r.changes], [[], rows.length / 2]);
  assert.equal(value('SELECT count(*) FROM measurement'), rows.length);
  copy.close();
});

test('describe lists the five tables and six views with their columns', () => {
  const objects = describe(db);
  assert.deepEqual(objects.filter((o) => o.type === 'table').map((o) => o.name),
    ['campaign', 'mesh', 'file', 'implementation', 'measurement']);
  assert.deepEqual(objects.filter((o) => o.type === 'view').map((o) => o.name),
    ['timing', 'format_ranking', 'scaling', 'complexity', 'topology_overhead', 'campaign_change']);
  assert.ok(objects.find((o) => o.name === 'timing').columns.includes('median_ms'));
});

test('every example runs, returns rows and has a title in both languages', () => {
  const examples = parseExamples(read('projects/sql/sqlite/examples.sql'));
  assert.equal(examples.length, 6);
  for (const lang of ['fr', 'en']) {
    const i18n = JSON.parse(read(`data/i18n/${lang}.json`));
    for (const { key } of examples) assert.ok(i18n[`sql.example.${key}`], `sql.example.${key} in ${lang}`);
  }
  for (const { key, sql } of examples) {
    const r = runQuery(db, sql);
    assert.ok(r.total > 0, `${key} returns rows`);
    assert.ok(!r.rows.flat().some((v) => typeof v === 'number' && !Number.isFinite(v)), `${key} has finite numbers`);
  }
});

test('parseExamples splits on the markers and trims each query', () => {
  assert.deepEqual(parseExamples('-- header\n-- example: a\nSELECT 1;\n\n-- example: b\nSELECT 2;\n'),
    [{ key: 'a', sql: 'SELECT 1;' }, { key: 'b', sql: 'SELECT 2;' }]);
});

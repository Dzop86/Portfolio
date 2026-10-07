// The static JSON API of the dashboards (D43).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { API_VERSION, buildApi, median, meshIo } from '../../src/api.mjs';
import { build } from '../../src/build.mjs';
import { loadData } from '../../src/lib.mjs';

test('median: odd and even counts, unsorted input left alone, empty list refused', () => {
  const input = [5, 1, 3];
  assert.equal(median(input), 3);
  assert.deepEqual(input, [5, 1, 3]);
  assert.equal(median([4, 1, 3, 2]), 2.5);
  assert.equal(median([7]), 7);
  assert.throws(() => median([]), /empty/);
});

const CSV = `run_at,runtime,os,arch,cpu,git_commit,warmup,repetitions,family,resolution,vertices,triangles,euler,boundary_loops,format,bytes,implementation,repetition,duration_ms
2026-10-06T16:03:05.556Z,node 22,linux,x64,Some CPU,abc,2,3,torus,16,512,1024,0,0,obj,100,lib-c,1,5
2026-10-06T16:03:05.556Z,node 22,linux,x64,Some CPU,abc,2,3,torus,16,512,1024,0,0,obj,100,lib-c,2,9
2026-10-06T16:03:05.556Z,node 22,linux,x64,Some CPU,abc,2,3,torus,16,512,1024,0,0,obj,100,lib-c,3,6
2026-10-06T16:03:05.556Z,node 22,linux,x64,Some CPU,abc,2,3,torus,16,512,1024,0,0,ply,80,lib-c,1,4
2026-10-06T16:03:05.556Z,node 22,linux,x64,Some CPU,abc,2,3,sphere,16,500,996,2,0,obj,90,topologie,1,2
`;

test('meshIo: one row per library, family, resolution and format, with the median of its runs', () => {
  const { machine, results } = meshIo(CSV);
  assert.deepEqual(machine, { cpu: 'Some CPU', runtime: 'node 22', date: '2026-10-06' });
  assert.equal(results.length, 3);
  const obj = results.find((r) => r.implementation === 'lib-c' && r.format === 'obj');
  assert.deepEqual(obj, { implementation: 'lib-c', family: 'torus', resolution: 16, format: 'obj', vertices: 512, triangles: 1024, bytes: 100, repetitions: 3, median_ms: 6 });
  // Sorted by library, family, format, resolution.
  assert.deepEqual(results.map((r) => `${r.implementation}/${r.family}/${r.format}`), ['lib-c/torus/obj', 'lib-c/torus/ply', 'topologie/sphere/obj']);
});

test('meshIo: a missing column is named', () => {
  assert.throws(() => meshIo('family,format\ntorus,obj\n'), /no "implementation" column/);
});

test('the API is complete, bilingual where it is shown, and drawn from the same files as the pages', () => {
  const data = loadData();
  const api = buildApi(data);
  assert.deepEqual(api['index.json'], { version: 'v1', files: ['mesh-io.json', 'ml.json', 'parallel-bench.json', 'projects.json', 'sprints.json'] });

  const { projects, techs } = api['projects.json'];
  assert.equal(projects.length, data.projects.length);
  for (const p of projects) {
    for (const key of ['name', 'pitch']) assert.ok(p[key].fr && p[key].en, `${p.id} ${key}`);
    assert.ok(['done', 'in-progress', 'planned'].includes(p.status), p.id);
    assert.ok(p.techs.every((x) => techs.includes(x)), p.id);
    assert.equal(p.page.fr, `fr/project-${p.id}.html`);
  }

  const sp = api['sprints.json'];
  assert.equal(sp.sprints.length, data.sprints.length);
  assert.ok(sp.sprints.every((s) => s.goal.fr && s.goal.en));
  // No French-only text: the stories carry their points and state, not their wording.
  assert.ok(sp.sprints.every((s) => s.stories.every((st) => Object.keys(st).join() === 'points,done,closed')));
  assert.equal(sp.burndown.remaining[0], sp.burndown.total);
  assert.equal(sp.velocity.length, sp.sprints.length);

  // The campaign of projects/sql: 2 libraries x 3 families x 5 resolutions x 3 formats, 7 runs each.
  const io = api['mesh-io.json'];
  assert.equal(io.results.length, 90);
  assert.ok(io.results.every((r) => r.repetitions === 7 && r.median_ms > 0));

  assert.ok(api['parallel-bench.json'].results.length > 0);
  const ml = api['ml.json'];
  assert.ok(ml.test_accuracy > 0.9 && ml.classes.length === 6);
});

test('integration: the build writes the API under api/v1, as valid JSON identical to buildApi', () => {
  const out = build(mkdtempSync(join(tmpdir(), 'api-')));
  const dir = join(out, 'api', API_VERSION);
  const api = buildApi(loadData());
  assert.deepEqual(readdirSync(dir).sort(), Object.keys(api).sort());
  for (const [file, body] of Object.entries(api)) assert.deepEqual(JSON.parse(readFileSync(join(dir, file), 'utf8')), body, file);
  // The dashboard is copied only once built; without it the site still builds.
  assert.equal(existsSync(join(out, 'dashboard')), existsSync(join(import.meta.dirname, '../../projects/react/dist')));
});

test('the React project page links to the dashboard in its own language', () => {
  const out = build(mkdtempSync(join(tmpdir(), 'react-page-')));
  for (const lang of ['fr', 'en']) {
    const html = readFileSync(join(out, lang, 'project-react.html'), 'utf8');
    assert.match(html, new RegExp(`href="\\.\\./dashboard/\\?lang=${lang}" data-link="demo"`));
  }
});

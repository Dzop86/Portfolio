import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadData, teachingTotals, sprintRange, LANGS, TECHS, techsOf } from '../../src/lib.mjs';

const { cv, projects, scrum, sprints } = loadData();

test('teaching hours match the academic CV (140 h TD, 236 h TP)', () => {
  assert.deepEqual(teachingTotals(cv.teaching), { td: 140, tp: 236, total: 376 });
});

test('the CV lists 6 publications', () => {
  assert.equal(cv.publications.length, 6);
});

test('project ids are unique and every project is translated', () => {
  const ids = projects.map((p) => p.id);
  assert.equal(new Set(ids).size, ids.length);
  for (const p of projects) {
    for (const lang of LANGS) {
      assert.ok(p.name[lang], `${p.id}: missing name.${lang}`);
      assert.ok(p.pitch[lang], `${p.id}: missing pitch.${lang}`);
    }
    assert.ok(p.stack.length > 0, `${p.id}: empty stack`);
  }
});

test('the portfolio covers the required technologies', () => {
  const stack = new Set(projects.flatMap((p) => p.stack));
  const required = ['HTML', 'CSS', 'JavaScript', 'C', 'C++', 'Qt', 'Python', 'PyTorch', 'SQL',
    'Flex', 'Bison', 'OCaml', 'Ada', 'React', 'Angular', 'Docker', 'Java', 'JavaFX', 'C#',
    'Godot 4', 'ASP.NET Core', 'EF Core', 'PostgreSQL', 'JWT',
    'DVC', 'MLflow', 'LaTeX', 'TypeScript', 'OpenMP', 'CUDA'];
  const missing = required.filter((r) => !stack.has(r));
  assert.deepEqual(missing, []);
});

test('risk scores stay within a 3 x 3 matrix', () => {
  for (const r of scrum.risks) {
    assert.ok(r.p >= 1 && r.p <= 3 && r.i >= 1 && r.i <= 3, r.id);
  }
});

test('roadmap phases cover every sprint once, up to the last sprint of the projects', () => {
  const ranges = scrum.phases.map((ph) => sprintRange(ph.sprints));
  assert.equal(ranges[0][0], 1);
  for (let i = 1; i < ranges.length; i++) assert.equal(ranges[i][0], ranges[i - 1][1] + 1, scrum.phases[i].sprints);
  assert.equal(ranges.at(-1)[1], scrum.sprintCount);
  const lastProjectSprint = Math.max(...projects.flatMap((p) => p.sprint.match(/\d+/g).map(Number)));
  assert.equal(scrum.sprintCount, lastProjectSprint);
});

test('project ids are URL-safe slugs and project links are absolute https URLs or a folder of the site', () => {
  for (const p of projects) {
    assert.match(p.id, /^[a-z0-9]+(-[a-z0-9]+)*$/, p.id);
    for (const [kind, url] of Object.entries(p.links ?? {})) {
      assert.ok(['code', 'demo'].includes(kind), `${p.id}: unknown link kind ${kind}`);
      // A folder of the published site (the dashboards, D43, D44) stays relative: it then works in Docker too.
      assert.match(url, /^(https:\/\/|\.\.\/(dashboard|angular)\/)/, `${p.id}: ${kind}`);
    }
  }
});

test('every sprint file has a numbered story table with points', () => {
  assert.ok(sprints.length >= 3);
  sprints.forEach((sprint, i) => {
    assert.equal(sprint.number, i + 1, 'sprints are numbered from 1 without gaps');
    for (const story of sprint.stories) assert.ok(story.points > 0, `sprint ${sprint.number}: ${story.text}`);
  });
});

test('sprint labels are well formed and follow the order decided on 6 October 2026', () => {
  const first = (id) => Number(projects.find((p) => p.id === id).sprint.match(/^S(\d+)/)[1]);
  for (const p of projects) assert.match(p.sprint, /^S\d+(-S\d+)?(\+S\d+)?$/, p.id);
  const order = ['ada', 'sql', 'langage', 'latex', 'gcartes'];
  for (let i = 1; i < order.length; i++) assert.ok(first(order[i - 1]) < first(order[i]), `${order[i - 1]} before ${order[i]}`);
  assert.ok(first('gcartes') < first('qt'), 'the Qt viewer comes after the G-maps course');
  for (const game of ['othello', 'naval', 'aventure', 'bataille', 'morpion', 'rogue']) {
    assert.ok(first('ml') < first(game) && first(game) < first('qt'), `${game}: after the ML follow-up, before Qt (D26)`);
  }
  assert.ok(!projects.some((p) => p.id === 'spring' || p.id === 'aspnet'), 'Spring and ASP.NET dropped (D25)');
});

test('every project names its main technologies, all of them known', () => {
  for (const p of projects) {
    assert.ok(Array.isArray(p.techs) && p.techs.length > 0, p.id);
    for (const l of p.techs) assert.ok(TECHS.includes(l), `${p.id}: unknown technology ${l}`);
  }
  // C, C++, C# and Java come first, as asked by Charles.
  assert.deepEqual(techsOf(projects).slice(0, 4), ['C', 'C++', 'C#', 'Java']);
});

test('every technology of every project says what it does there, in both languages', () => {
  const { projects } = loadData();
  for (const p of projects) {
    assert.deepEqual(Object.keys(p.roles ?? {}), p.stack, `${p.id}: one role per technology, in the order of the stack`);
    for (const [tech, role] of Object.entries(p.roles)) {
      for (const lang of ['fr', 'en']) assert.ok(role[lang]?.length > 20, `${p.id} ${tech} ${lang}`);
    }
  }
});

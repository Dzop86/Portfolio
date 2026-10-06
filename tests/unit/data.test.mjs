import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadData, teachingTotals, sprintRange, LANGS } from '../../src/lib.mjs';

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
  const required = ['HTML', 'CSS', 'JavaScript', 'Java', 'Spring Boot', 'C', 'C++', 'Qt', 'Python', 'PyTorch', 'SQL',
    'Flex', 'Bison', 'OCaml', 'Ada', 'React', 'Angular', 'Bootstrap', 'C#', 'ASP.NET', 'Docker', 'Jenkins',
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

test('project ids are URL-safe slugs and project links are absolute https URLs', () => {
  for (const p of projects) {
    assert.match(p.id, /^[a-z0-9]+(-[a-z0-9]+)*$/, p.id);
    for (const [kind, url] of Object.entries(p.links ?? {})) {
      assert.ok(['code', 'demo'].includes(kind), `${p.id}: unknown link kind ${kind}`);
      assert.match(url, /^https:\/\//, `${p.id}: ${kind}`);
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
  assert.ok(first('gcartes') < first('aspnet') && first('gcartes') < first('spring'), 'the .NET and Spring APIs come after');
});

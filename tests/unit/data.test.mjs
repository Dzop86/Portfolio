import { test } from 'node:test';
import assert from 'node:assert/strict';
import { loadData, teachingTotals, LANGS } from '../../src/lib.mjs';

const { cv, projects, scrum } = loadData();

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
    'GitLab CI', 'MLflow', 'LaTeX', 'TypeScript', 'OpenMP', 'CUDA'];
  const missing = required.filter((r) => !stack.has(r));
  assert.deepEqual(missing, []);
});

test('risk scores stay within a 3 x 3 matrix', () => {
  for (const r of scrum.risks) {
    assert.ok(r.p >= 1 && r.p <= 3 && r.i >= 1 && r.i <= 3, r.id);
  }
});

test('roadmap phases are contiguous from September 2025 to October 2026', () => {
  assert.equal(scrum.phases[0].from, '2025-09');
  assert.equal(scrum.phases.at(-1).to, '2026-10');
});

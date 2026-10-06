import { test } from 'node:test';
import assert from 'node:assert/strict';
import { esc, pick, makeT, teachingTotals, riskLevel, i18nParity, loadData, normalizeBase, projectPage, neighbours, parseSprint, progress, sprintRange, roadmapState } from '../../src/lib.mjs';

test('esc neutralises HTML special characters', () => {
  assert.equal(esc('<a href="x">\'&'), '&lt;a href=&quot;x&quot;&gt;&#39;&amp;');
  assert.equal(esc(undefined), '');
});

test('pick returns the requested language and fails loudly when missing', () => {
  assert.equal(pick({ fr: 'Bonjour', en: 'Hello' }, 'en'), 'Hello');
  assert.equal(pick('plain', 'fr'), 'plain');
  assert.throws(() => pick({ fr: 'Seulement' }, 'en'), /Missing "en"/);
});

test('makeT throws on an unknown key instead of rendering it silently', () => {
  const t = makeT({ a: 'A' }, 'fr');
  assert.equal(t('a'), 'A');
  assert.throws(() => t('b'), /Missing i18n key "b"/);
});

test('teachingTotals sums TD and TP', () => {
  assert.deepEqual(teachingTotals([{ td: 2, tp: 3 }, { td: 0, tp: 5 }]), { td: 2, tp: 8, total: 10 });
});

test('riskLevel thresholds', () => {
  assert.equal(riskLevel(9), 'high');
  assert.equal(riskLevel(6), 'high');
  assert.equal(riskLevel(4), 'medium');
  assert.equal(riskLevel(2), 'low');
});

test('sprintRange reads single sprints and ranges', () => {
  assert.deepEqual(sprintRange('S9'), [9, 9]);
  assert.deepEqual(sprintRange('S11-S12'), [11, 12]);
  assert.throws(() => sprintRange('Sprint 3'), /Bad sprint label/);
});
test('French and English dictionaries have the same keys', () => {
  const { i18n } = loadData();
  assert.deepEqual(i18nParity(i18n), { missingInEn: [], missingInFr: [] });
});

test('normalizeBase always returns a "/path/" form', () => {
  assert.equal(normalizeBase(undefined), '/');
  assert.equal(normalizeBase(''), '/');
  assert.equal(normalizeBase('/'), '/');
  assert.equal(normalizeBase('/Portfolio'), '/Portfolio/');
  assert.equal(normalizeBase('Portfolio/'), '/Portfolio/');
  assert.equal(normalizeBase(' //a/b// '), '/a/b/');
});

test('projectPage names the detail page after the project id', () => {
  assert.equal(projectPage('lib-c'), 'project-lib-c');
});

test('neighbours returns the previous and next items, null at both ends', () => {
  const list = [{ id: 'a' }, { id: 'b' }, { id: 'c' }];
  assert.deepEqual(neighbours(list, 'b'), { prev: list[0], next: list[2] });
  assert.deepEqual(neighbours(list, 'a'), { prev: null, next: list[1] });
  assert.deepEqual(neighbours(list, 'c'), { prev: list[1], next: null });
  assert.throws(() => neighbours(list, 'z'), /Unknown id "z"/);
});

const SPRINT_MD = `# Sprint 7 : un titre

**Objectif :** peu importe.

| Story | Points | État |
|---|---|---|
| En tant que Charles, je fais \`projects/lib-c/\` : CMake. | 3 | Fait |
| En tant que recruteur, j'essaie lib-c dans le navigateur. | 2 | En cours |
| En tant que Charles, le miroir GitLab de lib-c tourne. | 1 | Bloqué : jeton à créer |
| En tant que visiteur, je lis la page SQL. | 2 | À faire |

## Rétro
| a | b | c |
`;

test('parseSprint reads the number, the title and the story table', () => {
  const sprint = parseSprint(SPRINT_MD, 'sprint-07.md');
  assert.equal(sprint.number, 7);
  assert.equal(sprint.title, 'un titre');
  assert.equal(sprint.stories.length, 4);
  assert.deepEqual(sprint.stories.map((s) => [s.points, s.done]), [[3, true], [2, false], [1, false], [2, false]]);
});

test('parseSprint fails loudly on a sprint file without stories', () => {
  assert.throws(() => parseSprint('# Sprint 1 : vide\n', 'sprint-01.md'), /No story table in sprint-01\.md/);
});

test('progress counts projects, sprint points and story points per project', () => {
  const sprints = [parseSprint(SPRINT_MD, 'sprint-07.md'), parseSprint(SPRINT_MD.replace('Sprint 7', 'Sprint 6'), 'sprint-06.md')];
  const projects = [
    { id: 'lib-c', status: 'in-progress', points: 8 },
    { id: 'sql', status: 'planned', points: 5 },
    { id: 'vitrine', status: 'done', points: 5 },
  ];
  const p = progress(projects, sprints);
  assert.deepEqual(p.portfolio, { done: 1, inProgress: 1, total: 3 });
  assert.deepEqual(p.sprint, { number: 7, title: 'un titre', done: 3, total: 8 });
  // lib-c: 2 × 3 done points over max(8, 2 × 6 planned) = 12; "SQL" in capitals does not count as sql.
  assert.deepEqual(p.projects, [{ id: 'lib-c', done: 6, total: 12 }]);
});

test('roadmapState: closed sprints and the current one, abandoned stories counting as closed', () => {
  const sprint = (number, ...states) => ({ number, stories: states.map((st) => ({ points: 1, done: st === 'Fait', closed: st !== 'À faire' })) });
  assert.deepEqual(roadmapState([sprint(1, 'Fait'), sprint(2, 'Fait', 'Abandonné')]), { done: 2, current: 3 });
  assert.deepEqual(roadmapState([sprint(1, 'Fait'), sprint(2, 'Fait', 'À faire')]), { done: 1, current: 2 });
});

test('parseSprint marks abandoned stories as closed but not done', () => {
  const md = '# Sprint 3 : t\n\n| Story | Points | État |\n|---|---|---|\n| a | 1 | Fait |\n| b | 1 | Abandonné (décision) |\n| c | 2 | En cours |\n';
  assert.deepEqual(parseSprint(md, 'x.md').stories.map((s) => [s.done, s.closed]), [[true, true], [false, true], [false, false]]);
});

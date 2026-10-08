import { test } from 'node:test';
import assert from 'node:assert/strict';
import { esc, pick, makeT, teachingTotals, riskLevel, i18nParity, loadData, normalizeBase, projectPage, neighbours, parseSprint, progress, sprintRange, roadmapState, periodYears, newestFirst, bySprint, velocity, burndown, lastSprint } from '../../src/lib.mjs';

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

**Objectif :** livrer \`la démo\`.

**Goal:** ship \`the demo\`.

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
  assert.deepEqual(sprint.goal, { fr: 'livrer `la démo`.', en: 'ship `the demo`.' });
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
  // lib-c: 2 × 3 done points over its 2 × 6 points of stories; "SQL" in capitals does not count as sql.
  assert.deepEqual(p.projects, [{ id: 'lib-c', done: 6, total: 12 }]);
  // A project left in progress whose stories are all delivered is not shown as under way (the showcase, "4 points
  // of 9": its estimate counted stories of sprints 1 to 9 that never named it).
  const story = (text, points, done) => ({ text, points, done, closed: done });
  const delivered = progress([{ id: 'vitrine', status: 'in-progress', points: 9 }], [
    { number: 35, title: 't', stories: [story('x (vitrine)', 1, true), story('y (vitrine)', 1, true)] },
    { number: 40, title: 't', stories: [story('z (vitrine)', 2, true)] },
  ]);
  assert.deepEqual(delivered.projects, []);
  const open = progress([{ id: 'vitrine', status: 'in-progress', points: 9 }], [
    { number: 40, title: 't', stories: [story('z (vitrine)', 2, true), story('w (vitrine)', 1, false)] },
  ]);
  assert.deepEqual(open.projects, [{ id: 'vitrine', done: 2, total: 3 }]);
});

test('roadmapState: closed sprints and the current one, abandoned stories counting as closed', () => {
  const sprint = (number, ...states) => ({ number, stories: states.map((st) => ({ points: 1, done: st === 'Fait', closed: st !== 'À faire' })) });
  assert.deepEqual(roadmapState([sprint(1, 'Fait'), sprint(2, 'Fait', 'Abandonné')]), { done: 2, current: 3 });
  assert.deepEqual(roadmapState([sprint(1, 'Fait'), sprint(2, 'Fait', 'À faire')]), { done: 1, current: 2 });
});

test('parseSprint marks abandoned stories as closed but not done', () => {
  const md = '# Sprint 3 : t\n\n**Objectif :** o\n\n**Goal:** g\n\n| Story | Points | État |\n|---|---|---|\n| a | 1 | Fait |\n| b | 1 | Abandonné (décision) |\n| c | 2 | En cours |\n';
  assert.deepEqual(parseSprint(md, 'x.md').stories.map((s) => [s.done, s.closed]), [[true, true], [false, true], [false, false]]);
});

test('periodYears reads single years, ranges and academic years', () => {
  assert.deepEqual(periodYears('2025'), [2025, 2025]);
  assert.deepEqual(periodYears('2022 – 2024'), [2024, 2022]);
  assert.deepEqual(periodYears('2023/2024, 2024/2025'), [2025, 2023]);
  assert.throws(() => periodYears('soon'), /No year/);
});

test('newestFirst sorts by last year, then by the later start, and keeps ties in place', () => {
  const list = ['2022 – 2024', '2025', '2024', '2019 – 2021', '2024'].map((period, n) => ({ period, n }));
  assert.deepEqual(newestFirst(list).map((x) => x.n), [1, 2, 4, 0, 3]);
});

test('bySprint orders projects by first sprint, then last, labels like S8+S16 included', () => {
  const ids = bySprint([{ id: 'c', sprint: 'S9' }, { id: 'a', sprint: 'S1-S9' }, { id: 'b', sprint: 'S8+S16' }, { id: 'd', sprint: 'S2-S6' }])
    .map((p) => p.id);
  assert.deepEqual(ids, ['a', 'd', 'b', 'c']);
});

test('loadData gives the projects in sprint order and every dated CV list newest first', () => {
  const { projects, cv } = loadData();
  const starts = projects.map((p) => Number(p.sprint.match(/\d+/)[0]));
  assert.deepEqual(starts, [...starts].sort((a, b) => a - b));
  assert.ok(projects.findIndex((p) => p.id === 'ada') < projects.findIndex((p) => p.id === 'sql'), 'ada (S9) before sql (S10)');
  for (const key of ['education', 'experience', 'supervision', 'responsibilities']) {
    const ends = cv[key].map((x) => periodYears(x.period)[0]);
    assert.deepEqual(ends, [...ends].sort((a, b) => b - a), key);
  }
  assert.deepEqual(cv.responsibilities.map((r) => r.period), ['2025', '2024', '2022 – 2024']);
  const years = cv.publications.map((p) => p.year);
  assert.deepEqual(years, [...years].sort((a, b) => b - a));
});

test('parseSprint needs the goal in both languages', () => {
  const md = SPRINT_MD.replace(/^\*\*Goal:\*\*.*$/m, '');
  assert.throws(() => parseSprint(md, 'sprint-07.md'), /sprint-07\.md needs both/);
});

test('velocity follows the sprints', () => {
  const story = (points, done) => ({ text: 's', points, done, closed: done });
  const v = velocity([{ number: 2, stories: [story(3, true), story(2, false)] }, { number: 1, stories: [story(4, true)] }]);
  assert.deepEqual(v, [{ number: 1, committed: 4, done: 4 }, { number: 2, committed: 5, done: 3 }]);
  assert.equal(lastSprint('S23-S25'), 25);
  assert.equal(lastSprint('S8+S16'), 16);
});

test('the burndown counts delivered story points, and a scope added later is a step where it arrives (D49)', () => {
  const story = (points, state) => ({ text: 's', points, done: state === 'done', closed: state !== 'open' });
  const sprints = [
    { number: 1, stories: [story(3, 'done'), story(2, 'done'), story(1, 'dropped')] },
    { number: 2, stories: [story(4, 'done')] },
    // Added after sprint 2: a step of 5 points there, the past untouched.
    { number: 3, stories: [story(2, 'done'), story(3, 'open')] },
  ];
  const steps = [{ after: 2, sprints: [3], decisions: ['D9'] }];
  const b = burndown(sprints, steps);
  // A dropped story is no scope; today's scope is 14 points, 9 at the start.
  assert.equal(b.total, 14);
  assert.equal(b.start, 9);
  assert.deepEqual(b.remaining, [9, 4, 5, 3]);
  // The path draws the step as a vertical segment at sprint 2.
  assert.deepEqual(b.path, [[0, 9], [1, 4], [2, 0], [2, 5], [3, 3]]);
  assert.deepEqual(b.steps, [{ after: 2, decisions: ['D9'], points: 5 }]);
  // What is left is exactly the open stories.
  assert.equal(b.open, 3);
  assert.equal(b.remaining.at(-1), b.open);
  // Adding a step later changes nothing before it.
  const before = burndown(sprints.slice(0, 2), []);
  assert.deepEqual(b.remaining.slice(0, 2), before.remaining.slice(0, 2));
  // A sprint in two steps, or a step on a sprint that does not exist, is a mistake in the data.
  assert.throws(() => burndown(sprints, [...steps, { after: 1, sprints: [3], decisions: ['D8'] }]), /sprint 3/);
  assert.throws(() => burndown(sprints, [{ after: 2, sprints: [7], decisions: ['D9'] }]), /sprint 7/);
});

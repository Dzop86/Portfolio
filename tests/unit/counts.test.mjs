// The number of projects and points, wherever it is written (sprint 40): computed from data/projects.json, never
// left stale in the texts, the plan, the risk log or the dashboard captures.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, loadData, projectSummary } from '../../src/lib.mjs';

const data = loadData();
const summary = projectSummary(data.projects);
const read = (f) => readFileSync(join(ROOT, f), 'utf8');

test('the summary counts like the dashboards do', () => {
  const projects = [{ status: 'done', points: 3 }, { status: 'in-progress', points: 5 }, { status: 'planned', points: 2 }, { status: 'done', points: 1 }];
  assert.deepEqual(projectSummary(projects), { total: 4, done: 2, inProgress: 1, points: 11, pointsDone: 4 });
});

test('every "N projects" in the texts and the README is the number of projects', () => {
  const texts = [read('data/i18n/fr.json'), read('data/i18n/en.json'), read('README.md'), read('PLAN.md')];
  for (const text of texts) {
    for (const m of text.matchAll(/\b(\d+) (projets|projects)\b/g)) assert.equal(Number(m[1]), summary.total, m[0]);
  }
  assert.match(read('PLAN.md'), new RegExp(`## Les ${summary.total} projets`));
});

test('the plan gives the total of points and as many sprints as there are sprint files', () => {
  const sprints = readdirSync(join(ROOT, 'scrum')).filter((f) => /^sprint-\d+\.md$/.test(f)).length;
  assert.match(read('PLAN.md'), new RegExp(`Total : ${summary.points} points sur ${sprints} sprints`));
});

test('the risk log ends on the current total of points, in each language', () => {
  const texts = JSON.stringify(data.scrum);
  const fr = [...texts.matchAll(/(\d+) en tout/g)].at(-1);
  const en = [...texts.matchAll(/(\d+) in all/g)].at(-1);
  assert.equal(Number(fr[1]), summary.points, fr[0]);
  assert.equal(Number(en[1]), summary.points, en[0]);
});

test('no text writes the counts of the captures by hand: they come from the data', () => {
  for (const lang of ['fr', 'en']) {
    const alt = JSON.parse(read(`data/i18n/${lang}.json`))['angcmp.projects.alt'];
    for (const key of ['{done}', '{total}', '{pointsDone}', '{points}']) assert.ok(alt.includes(key), `${lang} ${key}`);
  }
});

test('the Angular projects capture was taken with the current counts', () => {
  const shot = JSON.parse(read('projects/angular/data/screenshots.json')).projects;
  assert.deepEqual(shot, summary,
    'the counts changed since the capture: build the dashboards and the site, serve it, then run node projects/angular/scripts/screenshots.mjs');
});

import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

export const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
export const LANGS = ['fr', 'en'];
export const PAGES = ['index', 'projects', 'research', 'method', 'contact'];
// Set automatically in CI from the GitHub repository; the default is the public repo.
export const REPO_URL = process.env.REPO_URL || 'https://github.com/Dzop86/Portfolio';

/** Normalises a site base path to the "/path/" form used by <base href>. */
export function normalizeBase(value) {
  const trimmed = String(value ?? '').trim().replace(/^\/+|\/+$/g, '');
  return trimmed ? `/${trimmed}/` : '/';
}

// Path the site is served from: "/" locally and in Docker, "/<repo>/" on GitHub Pages (set by the deploy workflow).
export const BASE_PATH = normalizeBase(process.env.BASE_PATH);

const readJson = (p) => JSON.parse(readFileSync(join(ROOT, p), 'utf8'));

// Every dated list of the CV, newest first, whatever the order in the file.
function sortCv(cv) {
  return {
    ...cv,
    education: newestFirst(cv.education),
    experience: newestFirst(cv.experience),
    publications: newestFirst(cv.publications, (p) => String(p.year)),
    teaching: newestFirst(cv.teaching, (r) => r.years),
    supervision: newestFirst(cv.supervision),
    responsibilities: newestFirst(cv.responsibilities),
  };
}

export function loadData() {
  return {
    cv: sortCv(readJson('data/cv.json')),
    // Projects in the order of their sprints, everywhere (cards, previous and next links).
    projects: bySprint(readJson('data/projects.json')),
    scrum: readJson('data/scrum.json'),
    sprints: readdirSync(join(ROOT, 'scrum'))
      .filter((f) => /^sprint-\d+\.md$/.test(f))
      .map((f) => parseSprint(readFileSync(join(ROOT, 'scrum', f), 'utf8'), f))
      .sort((a, b) => a.number - b.number),
    i18n: { fr: readJson('data/i18n/fr.json'), en: readJson('data/i18n/en.json') },
  };
}

export function esc(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

export function makeT(dict, lang) {
  return (key) => {
    if (!(key in dict)) throw new Error(`Missing i18n key "${key}" for ${lang}`);
    return dict[key];
  };
}

/** Picks the right language from a {fr, en} object, or returns plain values as is. */
export function pick(value, lang) {
  if (value && typeof value === 'object' && !Array.isArray(value)) {
    if (!(lang in value)) throw new Error(`Missing "${lang}" translation in ${JSON.stringify(value)}`);
    return value[lang];
  }
  return value;
}

export function teachingTotals(teaching) {
  return teaching.reduce(
    (acc, row) => ({ td: acc.td + row.td, tp: acc.tp + row.tp, total: acc.total + row.td + row.tp }),
    { td: 0, tp: 0, total: 0 },
  );
}

export function riskLevel(score) {
  if (score >= 6) return 'high';
  if (score >= 3) return 'medium';
  return 'low';
}

/**
 * Years of a period such as "2025", "2022 – 2024" or "2023/2024, 2024/2025": [last year, first year].
 * Academic years count by the year they end in.
 */
export function periodYears(period) {
  const years = String(period).match(/\d{4}/g)?.map(Number);
  if (!years) throw new Error(`No year in period "${period}"`);
  return [Math.max(...years), Math.min(...years)];
}

/** Newest first: by last year, then by first year (a shorter, later-starting period is more recent). Stable. */
export function newestFirst(list, period = (x) => x.period) {
  return list
    .map((x, i) => [x, periodYears(period(x)), i])
    .sort(([, [ea, sa], ia], [, [eb, sb], ib]) => eb - ea || sb - sa || ia - ib)
    .map(([x]) => x);
}

/** Projects in the order they are built: by first sprint, then last sprint (labels like "S8+S16" too). */
export function bySprint(projects) {
  const key = (p) => String(p.sprint).match(/\d+/g).map(Number);
  return projects
    .map((p, i) => [p, key(p), i])
    .sort(([, a, ia], [, b, ib]) => a[0] - b[0] || a.at(-1) - b.at(-1) || ia - ib)
    .map(([p]) => p);
}

/** First and last sprint of a label such as "S9" or "S11-S12". */
export function sprintRange(label) {
  const m = String(label).match(/^S(\d+)(?:-S(\d+))?$/);
  if (!m) throw new Error(`Bad sprint label "${label}"`);
  return [Number(m[1]), Number(m[2] ?? m[1])];
}

/** Last sprint whose stories are all closed (done or abandoned), and the sprint in progress after it. */
export function roadmapState(sprints) {
  const sorted = [...sprints].sort((a, b) => a.number - b.number);
  let done = 0;
  for (const sp of sorted) {
    if (sp.number === done + 1 && sp.stories.every((st) => st.closed)) done = sp.number;
    else break;
  }
  return { done, current: done + 1 };
}


/** Name of a project's detail page, e.g. "project-lib-c" (served as <lang>/project-lib-c.html). */
/**
 * Counts shown by the dashboards (projects/react/src/model.ts): projects in all, done and in progress, points in all
 * and points of the done projects. The captures' texts and the test of stale counts use it too (sprint 40).
 */
export function projectSummary(projects) {
  const count = (s) => projects.filter((p) => p.status === s).length;
  const points = projects.reduce((acc, p) => acc + p.points, 0);
  const pointsDone = projects.filter((p) => p.status === 'done').reduce((acc, p) => acc + p.points, 0);
  return { total: projects.length, done: count('done'), inProgress: count('in-progress'), points, pointsDone };
}

export function projectPage(id) {
  return `project-${id}`;
}

/** Previous and next items around `id` in a list, null at both ends. */
export function neighbours(list, id) {
  const i = list.findIndex((item) => item.id === id);
  if (i < 0) throw new Error(`Unknown id "${id}"`);
  return { prev: list[i - 1] ?? null, next: list[i + 1] ?? null };
}

/**
 * Reads a scrum/sprint-NN.md file: number and title from "# Sprint N : title", stories from the
 * first table whose header starts with "Story" (columns: story, points, state; "Fait" means done, and
 * "Abandonné" closes a story without doing it).
 */
export function parseSprint(md, file) {
  const heading = md.match(/^# Sprint (\d+)\s*:\s*(.+)$/m);
  if (!heading) throw new Error(`No "# Sprint N : title" heading in ${file}`);
  const lines = md.split(/\r?\n/);
  const start = lines.findIndex((l) => /^\|\s*Story\s*\|/.test(l));
  if (start < 0) throw new Error(`No story table in ${file}`);
  const stories = [];
  for (const line of lines.slice(start + 2)) {
    if (!line.startsWith('|')) break;
    const [text, points, state] = line.split('|').slice(1, -1).map((c) => c.trim());
    // No \b after "Abandonné": JavaScript's \b is ASCII-only and never matches after « é ».
    stories.push({ text, points: Number(points), done: /^Fait\b/.test(state), closed: /^(Fait\b|Abandonné)/.test(state) });
  }
  // The sprint goal, in French and in English (a missing language fails the build, as for the texts).
  const fr = md.match(/^\*\*Objectif :\*\* (.+)$/m);
  const en = md.match(/^\*\*Goal:\*\* (.+)$/m);
  if (!fr || !en) throw new Error(`${file} needs both "**Objectif :**" and "**Goal:**" lines`);
  return { number: Number(heading[1]), title: heading[2].trim(), goal: { fr: fr[1].trim(), en: en[1].trim() }, stories };
}

/** Story points committed (all stories) and delivered (done stories) in each sprint, in order. */
export function velocity(sprints) {
  const sum = (list) => list.reduce((acc, s) => acc + s.points, 0);
  return [...sprints].sort((a, b) => a.number - b.number)
    .map((s) => ({ number: s.number, committed: sum(s.stories), done: sum(s.stories.filter((x) => x.done)) }));
}

/** Last sprint of a project ("S23-S25" -> 25, "S8+S16" -> 16). */
export function lastSprint(label) {
  return Math.max(...label.match(/\d+/g).map(Number));
}

/**
 * Release burndown in project points: the estimate of the projects not yet done after each sprint,
 * from sprint 0 (nothing done) to `upTo`. A project counts as done at its last sprint once its status
 * is "done"; the showcase, built all along, stays in the remaining work until the end.
 */
/**
 * Burndown in story points (D49, sprint 40): after each sprint, the points of the planned stories not yet delivered,
 * from the sprint files. A story counts for the sprint whose file holds it, so the past never moves; an abandoned
 * one is no scope. `steps` are the scope added along the way ({ after, sprints, decisions }): the stories of
 * `sprints` join the scope after sprint `after`, a vertical step in `path` there. Returns today's scope (`total`),
 * the scope at the start, the points of the open stories, what is left after each sprint up to the last one that
 * delivered something (`remaining`, steps included), the path to draw and the size of each step.
 */
export function burndown(sprints, steps = []) {
  const numbers = new Set(sprints.map((s) => s.number));
  const addedAfter = new Map();
  for (const step of steps) {
    for (const n of step.sprints) {
      if (!numbers.has(n) || addedAfter.has(n)) throw new Error(`burndown: sprint ${n} is missing or in two scope steps`);
      addedAfter.set(n, step.after);
    }
  }
  const scope = (s) => s.stories.filter((x) => x.done || !x.closed).reduce((acc, x) => acc + x.points, 0);
  const delivered = (s) => s.stories.filter((x) => x.done).reduce((acc, x) => acc + x.points, 0);
  const plannedBy = (k, strict) => sprints.filter((s) => !addedAfter.has(s.number) || (strict ? addedAfter.get(s.number) < k : addedAfter.get(s.number) <= k))
    .reduce((acc, s) => acc + scope(s), 0);
  const doneBy = (k) => sprints.filter((s) => s.number <= k).reduce((acc, s) => acc + delivered(s), 0);
  const last = Math.max(0, ...sprints.filter((s) => delivered(s) > 0).map((s) => s.number));
  const remaining = [];
  const path = [];
  for (let k = 0; k <= last; k++) {
    const before = plannedBy(k, true) - doneBy(k);
    const after = plannedBy(k, false) - doneBy(k);
    if (before !== after) path.push([k, before]);
    path.push([k, after]);
    remaining.push(after);
  }
  return {
    total: sprints.reduce((acc, s) => acc + scope(s), 0),
    start: plannedBy(0, true),
    open: sprints.flatMap((s) => s.stories).filter((x) => !x.closed).reduce((acc, x) => acc + x.points, 0),
    remaining,
    path,
    steps: steps.map((st) => ({ after: st.after, decisions: st.decisions, points: sprints.filter((s) => st.sprints.includes(s.number)).reduce((acc, s) => acc + scope(s), 0) })),
  };
}

/**
 * Temporary progress figures for the home page: projects done or in progress, points done in the
 * latest sprint, and for each project in progress the done points of the stories that name it
 * (its id as a whole word), over its estimate or the planned story points if larger.
 */
export function progress(projects, sprints) {
  const stories = sprints.flatMap((s) => s.stories);
  const current = sprints.reduce((a, b) => (b.number > a.number ? b : a));
  const sum = (list) => list.reduce((acc, s) => acc + s.points, 0);
  const names = (id) => new RegExp(`(?<![\\w-])${id.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(?![\\w-])`);
  return {
    portfolio: {
      done: projects.filter((p) => p.status === 'done').length,
      inProgress: projects.filter((p) => p.status === 'in-progress').length,
      total: projects.length,
    },
    sprint: { number: current.number, title: current.title, done: sum(current.stories.filter((s) => s.done)), total: sum(current.stories) },
    projects: projects
      .filter((p) => p.status === 'in-progress')
      .map((p) => ({ p, own: stories.filter((s) => names(p.id).test(s.text)) }))
      // Only a project with stories still open is under way; its points are those of the stories that name it (its
      // estimate also covers early stories that never did: the showcase showed "4 points of 9" with nothing open).
      .filter(({ own }) => own.some((s) => !s.closed))
      .map(({ p, own }) => ({ id: p.id, done: sum(own.filter((s) => s.done)), total: sum(own) })),
  };
}

/**
 * Main technologies of the projects (their "techs" field: languages, and HTML/CSS), in a fixed order:
 * the compiled languages first, as recruiters read them, then the others. One missing here fails the tests.
 */
export const TECHS = ['C', 'C++', 'C#', 'Java', 'Python', 'Ada', 'OCaml', 'SQL', 'JavaScript', 'TypeScript', 'LaTeX', 'HTML/CSS'];

/** The technologies used by at least one project, in the order of TECHS. */
export function techsOf(projects) {
  const used = new Set(projects.flatMap((p) => p.techs));
  return TECHS.filter((l) => used.has(l));
}

export function i18nParity(i18n) {
  const fr = Object.keys(i18n.fr);
  const en = Object.keys(i18n.en);
  return {
    missingInEn: fr.filter((k) => !en.includes(k)),
    missingInFr: en.filter((k) => !fr.includes(k)),
  };
}

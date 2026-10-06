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

export function loadData() {
  return {
    cv: readJson('data/cv.json'),
    projects: readJson('data/projects.json'),
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

/** Month index (0-based) from the start month, e.g. "2025-11" relative to "2025-09" is 2. */
export function monthOffset(start, ym) {
  const [sy, sm] = start.split('-').map(Number);
  const [y, m] = ym.split('-').map(Number);
  return (y - sy) * 12 + (m - sm);
}

/** Name of a project's detail page, e.g. "project-lib-c" (served as <lang>/project-lib-c.html). */
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
 * first table whose header starts with "Story" (columns: story, points, state; "Fait" means done).
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
    stories.push({ text, points: Number(points), done: /^Fait\b/.test(state) });
  }
  return { number: Number(heading[1]), title: heading[2].trim(), stories };
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
      .filter(({ own }) => own.length > 0)
      .map(({ p, own }) => ({ id: p.id, done: sum(own.filter((s) => s.done)), total: Math.max(p.points, sum(own)) })),
  };
}

export function i18nParity(i18n) {
  const fr = Object.keys(i18n.fr);
  const en = Object.keys(i18n.en);
  return {
    missingInEn: fr.filter((k) => !en.includes(k)),
    missingInFr: en.filter((k) => !fr.includes(k)),
  };
}

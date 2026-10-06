import { readFileSync } from 'node:fs';
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

export function i18nParity(i18n) {
  const fr = Object.keys(i18n.fr);
  const en = Object.keys(i18n.en);
  return {
    missingInEn: fr.filter((k) => !en.includes(k)),
    missingInFr: en.filter((k) => !fr.includes(k)),
  };
}

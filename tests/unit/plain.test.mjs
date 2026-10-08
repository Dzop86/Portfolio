// Each project in plain words (D53): three short sentences per language, without the jargon a non-developer would
// stumble on, shown at the top of every project page in a box that opens without JavaScript.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { build } from '../../src/build.mjs';
import { LANGS, loadData, projectPage, esc } from '../../src/lib.mjs';

const { projects } = loadData();
// Words a recruiter outside IT would not know; language names (Java, Python, C…) are allowed, they are what a job
// offer lists.
const JARGON = ['webassembly', 'api', 'framework', 'compil', 'backend', 'frontend', 'jwt', 'cuda', 'openmp', 'opencl',
  'minimax', 'bitboard', 'involution', 'homolog', 'docker', 'conteneur', 'container', 'pipeline', 'dépôt', 'repository',
  'regex', 'refactor', 'cli', 'sdk', 'orm', 'gpu', 'cpu', 'webgl', 'shader', 'bvh', 'persistance', 'persistence'];

test('every project has its three plain sentences in each language, short and without jargon', () => {
  for (const p of projects) {
    for (const lang of LANGS) {
      const plain = p.plain?.[lang];
      assert.ok(plain, `${p.id} ${lang}`);
      for (const key of ['what', 'why', 'shows']) {
        const text = plain[key];
        assert.equal(typeof text, 'string', `${p.id} ${lang} ${key}`);
        assert.ok(text.length >= 30 && text.length <= 260, `${p.id} ${lang} ${key}: ${text.length} characters`);
        const words = text.toLowerCase();
        for (const j of JARGON) assert.ok(!new RegExp(`(^|[^\\p{L}])${j}`, 'u').test(words), `${p.id} ${lang} ${key}: "${j}"`);
        // In the first person, as the rest of the site (Charles's review): "mon cœur de métier", not "celui de Charles".
        assert.ok(!/Charles|\bses études|\bsa thèse|\bhis\b|\bhe\b/i.test(text), `${p.id} ${lang} ${key}: first person`);
      }
    }
  }
});

test('the plain box sits at the top of every project page, closed, in the page language', () => {
  const out = build(mkdtempSync(join(tmpdir(), 'plain-')));
  const labels = { fr: 'En bref, sans jargon', en: 'In short, no jargon' };
  const actions = { fr: ['Lire le résumé', 'Refermer'], en: ['Read the summary', 'Close'] };
  for (const lang of LANGS) {
    for (const p of projects) {
      const html = readFileSync(join(out, lang, `${projectPage(p.id)}.html`), 'utf8');
      const box = html.match(/<details class="plain" data-plain>([\s\S]*?)<\/details>/);
      assert.ok(box, `${lang} ${p.id}`);
      assert.ok(box[1].includes(labels[lang]), `${lang} ${p.id}: title`);
      // It says it opens, and how to close it (Charles's review: it did not look clickable).
      const summary = box[1].match(/<summary>([\s\S]*?)<\/summary>/)[1];
      assert.ok(summary.includes('plain-chevron'), `${lang} ${p.id}: chevron`);
      for (const a of actions[lang]) assert.ok(summary.includes(esc(a)), `${lang} ${p.id}: ${a}`);
      for (const key of ['what', 'why', 'shows']) assert.ok(box[1].includes(esc(p.plain[lang][key])), `${lang} ${p.id}: ${key}`);
      // Before the demonstrations and the links: right under the title and the status.
      assert.ok(html.indexOf('data-plain') < html.indexOf('class="actions'), `${lang} ${p.id}: before the links`);
    }
  }
});

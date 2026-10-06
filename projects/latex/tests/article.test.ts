// The article shown in the editor must render cleanly in both languages: no diagnostic at all, every
// reference and citation resolved, the same structure in French and English.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { renderLatex } from '../src/index.ts';

const article = (lang: 'fr' | 'en') => readFileSync(new URL(`../article/portfolio.${lang}.tex`, import.meta.url), 'utf8');

for (const lang of ['fr', 'en'] as const) {
  test(`the ${lang} article renders without any diagnostic`, () => {
    const r = renderLatex(article(lang), { lang });
    assert.deepEqual(r.diagnostics.map((d) => `${d.pos.line}:${d.pos.column} ${d.message}`), []);
    assert.doesNotMatch(r.html, /\?\?|tex-unknown|tex-error/);
    assert.equal((r.html.match(/class="katex-tag"/g) ?? []).length, 3, 'three numbered equations');
    assert.match(r.html, /<header class="tex-title"><h1>/);
    assert.match(r.html, /href="https:\/\/theses.fr\/s342053"/);
  });
}

test('both articles have the same outline, section for section', () => {
  const outline = (lang: 'fr' | 'en') => renderLatex(article(lang), { lang }).outline.map((e) => e.number);
  assert.deepEqual(outline('fr'), outline('en'));
  assert.equal(outline('fr').length, 5);
});

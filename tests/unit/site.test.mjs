// Integration test: builds the whole site and checks the generated pages.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname, resolve } from 'node:path';
import { build } from '../../src/build.mjs';
import { LANGS, PAGES, REPO_URL, loadData } from '../../src/lib.mjs';

const dist = build(mkdtempSync(join(tmpdir(), 'portfolio-')));
const page = (lang, p) => readFileSync(join(dist, lang, `${p}.html`), 'utf8');

test('every page exists in both languages with the right lang attribute', () => {
  for (const lang of LANGS) {
    for (const p of PAGES) {
      assert.match(page(lang, p), new RegExp(`<html lang="${lang}">`), `${lang}/${p}`);
    }
  }
});

test('every internal link and asset resolves to a file', () => {
  for (const lang of LANGS) {
    for (const p of PAGES) {
      const from = join(dist, lang, `${p}.html`);
      const refs = [...page(lang, p).matchAll(/(?:href|src)="(\.{1,2}\/[^"#]+)"/g)].map((m) => m[1]);
      for (const ref of refs) {
        assert.ok(existsSync(resolve(dirname(from), ref)), `${lang}/${p}: broken link ${ref}`);
      }
    }
  }
});

test('the language switch points to the same page in the other language', () => {
  assert.match(page('fr', 'research'), /href="\.\.\/en\/research\.html"/);
  assert.match(page('en', 'method'), /href="\.\.\/fr\/method\.html"/);
});

test('each page marks itself as current in the navigation', () => {
  for (const p of PAGES) {
    assert.match(page('fr', p), new RegExp(`href="\\./${p}\\.html" aria-current="page"`), p);
  }
});

test('the research page shows the teaching totals and the 6 publications', () => {
  const html = page('fr', 'research');
  assert.match(html, /data-total="td">140</);
  assert.match(html, /data-total="tp">236</);
  assert.match(html, /data-total="all">376</);
  assert.equal((html.match(/class="pub-title"/g) || []).length, 6);
});

test('the method page flags the reference plan as illustrative', () => {
  assert.match(page('fr', 'method'), /illustratif/);
  assert.match(page('en', 'method'), /[Ii]llustrative/);
});

test('no raw i18n key leaks into the HTML', () => {
  const { i18n } = loadData();
  for (const lang of LANGS) {
    for (const p of PAGES) {
      const html = page(lang, p);
      for (const key of Object.keys(i18n[lang])) {
        assert.ok(!html.includes(`>${key}<`) && !html.includes(`"${key}"`), `${lang}/${p}: raw key ${key}`);
      }
    }
  }
});

test('the footer links to the source repository on every page', () => {
  assert.match(REPO_URL, /^https:\/\/github\.com\/[\w.-]+\/[\w.-]+$/);
  for (const lang of LANGS) {
    for (const p of PAGES) {
      assert.ok(page(lang, p).includes(`href="${REPO_URL}"`), `${lang}/${p}`);
    }
  }
});

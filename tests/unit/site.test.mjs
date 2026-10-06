// Integration test: builds the whole site and checks the generated pages.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname, resolve } from 'node:path';
import { build } from '../../src/build.mjs';
import { LANGS, PAGES, REPO_URL, loadData, pick, projectPage, esc } from '../../src/lib.mjs';

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
    for (const file of readdirSync(join(dist, lang))) {
      const from = join(dist, lang, file);
      const refs = [...readFileSync(from, 'utf8').matchAll(/(?:href|src)="(\.{1,2}\/[^"#]+)"/g)].map((m) => m[1]);
      for (const ref of refs) {
        assert.ok(existsSync(resolve(dirname(from), ref)), `${lang}/${file}: broken link ${ref}`);
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

test('the 404 page resolves its assets and links from the site root', () => {
  const pagesDist = build(mkdtempSync(join(tmpdir(), 'portfolio-')), { basePath: '/Portfolio' });
  const html = readFileSync(join(pagesDist, '404.html'), 'utf8');
  assert.match(html, /<base href="\/Portfolio\/">/);
  const refs = [...html.matchAll(/(?:href|src)="(\.\/[^"#]+)"/g)].map((m) => m[1]);
  assert.ok(refs.length >= 4, 'stylesheets and home links');
  for (const ref of refs) assert.ok(existsSync(join(pagesDist, ref)), `broken link ${ref}`);
  assert.match(readFileSync(join(dist, '404.html'), 'utf8'), /<base href="\/">/);
});

test('the contact page links to ORCID and to the HAL publications of the author', () => {
  for (const lang of LANGS) {
    const html = page(lang, 'contact');
    assert.ok(html.includes('href="https://orcid.org/0009-0008-9314-8237"'), `${lang} ORCID`);
    assert.ok(html.includes('href="https://hal.science/search/index/?q=authIdHal_s:charles-lepaire"'), `${lang} HAL`);
  }
});

test('browser chrome uses the dark grey of the theme', () => {
  const tokens = readFileSync(resolve('src/assets/tokens.css'), 'utf8');
  assert.match(tokens, /--gris-titre: #181818;/);
  assert.match(tokens, /--gris-fond: #1f1f1f;/);
  assert.ok(page('fr', 'index').includes('<meta name="theme-color" content="#181818">'));
  const manifest = JSON.parse(readFileSync(join(dist, 'manifest.webmanifest'), 'utf8'));
  assert.equal(manifest.theme_color, '#181818');
  assert.equal(manifest.background_color, '#1f1f1f');
});

test('every project has a detail page in both languages, linked from its card', () => {
  const { projects } = loadData();
  for (const lang of LANGS) {
    const list = page(lang, 'projects');
    for (const p of projects) {
      const name = projectPage(p.id);
      const html = page(lang, name);
      assert.match(html, new RegExp(`<html lang="${lang}">`), `${lang}/${name}`);
      assert.ok(html.includes(`<h1>${esc(pick(p.name, lang))}</h1>`), `${lang}/${name}: title`);
      assert.ok(html.includes(`href="../${lang === 'fr' ? 'en' : 'fr'}/${name}.html"`), `${lang}/${name}: language switch`);
      assert.ok(html.includes('href="./projects.html" aria-current="page"'), `${lang}/${name}: nav`);
      assert.ok(list.includes(`href="./${name}.html"`), `${lang}: card link to ${name}`);
    }
  }
});

test('a detail page shows the stack, the Definition of Done and the neighbours', () => {
  const html = page('en', projectPage('lib-c'));
  for (const tech of ['CMake', 'Unity', 'Valgrind']) assert.ok(html.includes(`<li>${tech}</li>`), tech);
  assert.ok(html.includes('Unit and integration tests green'));
  assert.ok(html.includes(`href="./${projectPage('topologie')}.html"`), 'previous project');
  assert.ok(html.includes(`href="./${projectPage('qt')}.html"`), 'next project');
});

test('project links are shown only when the project has them', () => {
  const { links } = loadData().projects.find((p) => p.id === 'vitrine');
  assert.ok(page('fr', projectPage('vitrine')).includes(`href="${links.code}" data-link="code"`));
  const planned = page('fr', projectPage('spring'));
  assert.ok(!planned.includes('data-link='));
  assert.ok(planned.includes('data-no-links'));
});

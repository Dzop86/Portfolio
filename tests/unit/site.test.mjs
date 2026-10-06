// Integration test: builds the whole site and checks the generated pages.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname, resolve } from 'node:path';
import { build } from '../../src/build.mjs';
import { LANGS, PAGES, REPO_URL, loadData, pick, projectPage, esc, progress } from '../../src/lib.mjs';

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

test('the method page shows the roadmap by sprint, with done, current and planned phases', () => {
  const { scrum, sprints } = loadData();
  const html = page('fr', 'method');
  assert.match(html, new RegExp(`--sprints:${scrum.sprintCount}`));
  assert.match(html, /data-state="done"[^>]*>[\s\S]*?S9/);
  assert.match(html, /data-state="current"/);
  assert.match(html, /data-state="planned"/);
  const closed = sprints.filter((sp) => sp.stories.every((st) => st.closed)).length;
  assert.ok(html.includes(`Sprints 1 à ${closed} terminés`), 'the notice says how far the project is');
  assert.match(page('en', 'method'), /forecast/);
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
  // Neighbours follow the sprints: vitrine (S1) before lib-c (S2), topologie (S4) after it.
  assert.ok(html.includes(`rel="prev" href="./${projectPage('vitrine')}.html"`), 'previous project');
  assert.ok(html.includes(`rel="next" href="./${projectPage('topologie')}.html"`), 'next project');
});

test('project links are shown only when the project has them', () => {
  const { links } = loadData().projects.find((p) => p.id === 'vitrine');
  assert.ok(page('fr', projectPage('vitrine')).includes(`href="${links.code}" data-link="code"`));
  const planned = page('fr', projectPage('spring'));
  assert.ok(!planned.includes('data-link='));
  assert.ok(planned.includes('data-no-links'));
});

test('the home page shows the temporary progress bars, computed from the data', () => {
  const { projects, sprints } = loadData();
  const p = progress(projects, sprints);
  for (const lang of LANGS) {
    const html = page(lang, 'index');
    const bars = [...html.matchAll(/<div class="progress-bar" role="progressbar"([^>]*)>/g)].map((m) => m[1]);
    assert.equal(bars.length, 2 + p.projects.length, `${lang}: one bar each for portfolio, sprint and project`);
    for (const attrs of bars) {
      assert.match(attrs, /aria-labelledby="[\w-]+"/);
      assert.match(attrs, /aria-valuemin="0"/);
      assert.match(attrs, /aria-valuemax="100"/);
      assert.match(attrs, /aria-valuenow="\d+"/);
    }
    const sprintBar = bars.find((a) => a.includes('data-progress="sprint"'));
    assert.ok(sprintBar.includes(`aria-valuenow="${Math.round((100 * p.sprint.done) / p.sprint.total)}"`));
  }
});

test('the lib-c page embeds the WebAssembly demo with its samples, and only that page does', () => {
  for (const lang of LANGS) {
    const html = page(lang, projectPage('lib-c'));
    assert.ok(html.includes('data-mesh-demo'), `${lang}: demo section`);
    assert.ok(html.includes('<script type="module" src="../assets/meshdemo.js"></script>'));
    const samples = [...html.matchAll(/data-sample="([^"]+)"/g)].map((m) => m[1]);
    assert.deepEqual(samples.map((s) => s.split('/').pop()), ['cube.obj', 'tetrahedron.ply', 'torus.obj']);
    for (const s of samples) assert.ok(existsSync(resolve(join(dist, lang), s)), `${lang}: sample ${s}`);
    const labels = JSON.parse(html.match(/data-labels="([^"]+)"/)[1].replaceAll('&quot;', '"').replaceAll('&#39;', "'").replaceAll('&amp;', '&'));
    for (const key of ['vertices', 'edges', 'euler', 'genus', 'error.3', 'error.4', 'error.too-large']) {
      assert.ok(labels[key], `${lang}: label ${key}`);
    }
  }
  assert.ok(!page('fr', projectPage('spring')).includes('data-mesh-demo'));
  assert.ok(existsSync(join(dist, 'assets/wasm/meshlib.wasm')));
});

test('the topology page embeds the viewer, its bundle, samples and labels', () => {
  for (const lang of LANGS) {
    const html = page(lang, projectPage('topologie'));
    assert.ok(html.includes('data-topo-viewer'), `${lang}: viewer section`);
    assert.ok(html.includes('<script type="module" src="../assets/topoviewer.js"></script>'));
    assert.match(html, /<canvas[^>]*role="img"[^>]*aria-label="[^"]+"/);
    const samples = [...html.matchAll(/data-sample="([^"]+)"/g)].map((m) => m[1]);
    assert.deepEqual(samples.map((s) => s.split('/').pop()), ['torus.obj', 'sphere.obj', 'mobius.obj', 'saddle.obj']);
    for (const s of samples) assert.ok(existsSync(resolve(join(dist, lang), s)), `${lang}: sample ${s}`);
    const labels = JSON.parse(html.match(/data-labels="([^"]+)"/)[1].replaceAll('&quot;', '"').replaceAll('&#39;', "'").replaceAll('&amp;', '&'));
    for (const key of ['genus', 'orientable', 'yes', 'no', 'legend.neg', 'legend.pos', 'nowebgl', 'error.invalid', 'error.4']) {
      assert.ok(labels[key], `${lang}: label ${key}`);
    }
  }
  assert.ok(!page('fr', projectPage('lib-c')).includes('data-topo-viewer'));
});

test('the viewer bundle includes three.js, keeps its licence and stays under 700 KB', () => {
  const bundle = readFileSync(join(dist, 'assets/topoviewer.js'), 'utf8');
  assert.ok(!/from\s*["']three/.test(bundle), 'three.js is bundled, not imported bare');
  assert.match(bundle, /Copyright 2010-\d{4} Three\.js Authors/);
  assert.ok(bundle.length < 700_000, `${bundle.length} bytes`);
  assert.ok(!existsSync(join(dist, 'assets/viewer')), 'viewer sources are not published as is');
});

test('the research page shows professional experience and education side by side, newest first', () => {
  for (const lang of LANGS) {
    const html = page(lang, 'research');
    const block = (name) => html.match(new RegExp(`data-timeline="${name}">([\\s\\S]*?)</ol>`))[1];
    const periods = (name) => [...block(name).matchAll(/tl-period">([^<]+)/g)].map((m) => m[1]);
    assert.deepEqual(periods('experience'), ['2025 – 2026', '2022 – 2025', '2021', '2019'], lang);
    assert.deepEqual(periods('education'), ['2026', '2022 – 2025', '2019 – 2021', '2015 – 2019'], lang);
    assert.ok(block('experience').includes('ATER') && !block('education').includes('ATER'), 'ATER is a job, not a degree');
    assert.ok(html.indexOf('data-timeline="experience"') < html.indexOf('data-timeline="education"'));
  }
});

test('the thesis links to its theses.fr record', () => {
  const { cv } = loadData();
  assert.match(cv.thesis.url, /^https:\/\/theses\.fr\/\w+$/);
  assert.ok(page('fr', 'research').includes(`href="${cv.thesis.url}" data-link="theses"`));
});

test('the projects page lists the projects in sprint order', () => {
  const html = page('en', 'projects');
  const ids = [...html.matchAll(/href="\.\/project-([\w-]+)\.html"/g)].map((m) => m[1]);
  const order = loadData().projects.map((p) => p.id);
  assert.deepEqual([...new Set(ids)], order);
  assert.deepEqual(order.slice(0, 7), ['vitrine', 'lib-c', 'topologie', 'fastapi', 'ml', 'ada', 'sql']);
});

test('the risk register lists R1 to R9 in order, each with a matrix marking its own cell', () => {
  const { scrum } = loadData();
  const html = page('en', 'method');
  const rows = [...html.matchAll(/<tr data-id="(\d+)" data-p="(\d)" data-i="(\d)" data-score="(\d)">([\s\S]*?)<\/tr>/g)];
  assert.deepEqual(rows.map((r) => Number(r[1])), scrum.risks.map((_, k) => k + 1));
  for (const [, id, p, i, score, cells] of rows) {
    assert.equal(Number(score), p * i, `R${id} score`);
    const marks = [...cells.matchAll(/class="cell score-(\w+)( is-risk)?"/g)];
    assert.equal(marks.length, 9, `R${id} has 9 cells`);
    const own = marks.findIndex((m) => m[2]);
    // Rows run from impact 3 down to 1, columns from probability 1 to 3.
    assert.equal(own, (3 - i) * 3 + (p - 1), `R${id} marks probability ${p}, impact ${i}`);
    assert.match(cells, new RegExp(`aria-label="Probability ${p}, impact ${i}: score ${score}`));
  }
  for (const key of ['id', 'p', 'i', 'score']) assert.ok(html.includes(`data-sort="${key}"`), key);
});

test('the jury gives each member a grade and a role, chair first', () => {
  const { cv } = loadData();
  for (const j of cv.thesis.jury) for (const lang of LANGS) assert.ok(pick(j.grade, lang) && pick(j.role, lang), j.name);
  assert.equal(cv.thesis.jury[0].name, 'David Cazier');
  const html = page('fr', 'research');
  assert.ok(html.includes('<strong>Hakim Belhaouari</strong>, Maître de conférences'));
  assert.ok(html.includes('<strong>Julien Tierny</strong>, Directeur de recherche'));
  assert.ok(html.includes('Président du jury'));
});

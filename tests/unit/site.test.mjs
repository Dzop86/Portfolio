// Integration test: builds the whole site and checks the generated pages.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { createHash } from 'node:crypto';
import { join, dirname, resolve } from 'node:path';
import { build } from '../../src/build.mjs';
import { renderPage, renderProjectPage } from '../../src/templates.mjs';
import { LANGS, PAGES, REPO_URL, ROOT as ROOT_DIR, loadData, pick, projectPage, esc, progress, techsOf, makeT, velocity, burndown } from '../../src/lib.mjs';

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
  // The dashboards are built apart (projects/react and projects/angular, D43, D44): where one is not built
  // (the unit job), its links are the ones excused; the deployment sets REQUIRE_DASHBOARD and builds both.
  const unbuilt = ['dashboard', 'angular'].filter((folder) => !existsSync(join(dist, folder, 'index.html')));
  assert.ok(unbuilt.length === 0 || !process.env.REQUIRE_DASHBOARD, `REQUIRE_DASHBOARD is set but not built: ${unbuilt}`);
  for (const lang of LANGS) {
    for (const file of readdirSync(join(dist, lang))) {
      const from = join(dist, lang, file);
      // Without the fingerprint query (style.css?v=...), which only changes the address.
      const refs = [...readFileSync(from, 'utf8').matchAll(/(?:href|src)="(\.{1,2}\/[^"#?]+)/g)].map((m) => m[1]);
      for (const ref of refs) {
        if (unbuilt.some((folder) => ref.startsWith(`../${folder}/`))) continue;
        assert.ok(existsSync(resolve(dirname(from), ref)), `${lang}/${file}: broken link ${ref}`);
      }
    }
  }
});

test('every stylesheet and script carries the fingerprint of its current content', () => {
  for (const lang of LANGS) {
    for (const file of readdirSync(join(dist, lang))) {
      const html = readFileSync(join(dist, lang, file), 'utf8');
      const links = [...html.matchAll(/(?:href|src)="\.\.\/assets\/([^"?]+\.(?:css|js))(\?v=([0-9a-f]+))?"/g)];
      assert.ok(links.length >= 3, `${lang}/${file}: tokens.css, style.css and app.js at least`);
      for (const [, asset, , version] of links) {
        const hash = createHash('sha256').update(readFileSync(join(dist, 'assets', asset))).digest('hex').slice(0, 10);
        assert.equal(version, hash, `${lang}/${file}: ${asset}`);
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
  const closed = sprints.filter((sp) => sp.stories.every((st) => st.closed)).length;
  const finished = closed === scrum.sprintCount;
  // A phase is current while one of its sprints is open: none once every sprint is closed.
  assert.equal(/data-state="current"/.test(html), !finished);
  // A phase is planned only while it starts after the next sprint (none once the last phase has begun).
  const later = scrum.phases.filter((ph) => Number(ph.sprints.match(/\d+/)[0]) > closed + 1).length;
  assert.equal((html.match(/data-state="planned"/g) || []).length, later);
  // The notice says how far the project is; once it is finished, nothing is a forecast any more.
  if (finished) {
    assert.ok(html.includes(`Les ${closed} sprints sont terminés`));
    assert.doesNotMatch(page('en', 'method'), /forecast/);
    // Projects may still be in progress (awaiting review); no sprint is left to place them in, and the page
    // names them from the data rather than from a fixed example.
    const open = loadData().projects.filter((p) => p.status !== 'done');
    if (open.length === 0) assert.ok(html.includes('Tous les points prévus ont été livrés.'));
    for (const p of open) assert.ok(html.includes(esc(p.name.fr)), `names ${p.id}`);
    assert.doesNotMatch(html, /comme la vitrine/);
    assert.doesNotMatch(html, new RegExp(`sprints ${closed + 1} à`));
  } else {
    assert.ok(html.includes(`Sprints 1 à ${closed} terminés`));
    assert.match(page('en', 'method'), /forecast/);
    // The capacity counts closed sprints only (an open sprint is not a slow one), and the forecast starts
    // at the sprint in progress.
    const done = velocity(sprints).filter((x) => x.number <= closed).map((x) => x.done);
    assert.ok(html.includes(`de ${Math.min(...done)} à ${Math.max(...done)} ;`), 'capacity range of the closed sprints');
    assert.ok(html.includes(`dans les sprints ${closed + 1} à ${scrum.sprintCount}.`), 'forecast from the sprint in progress');
  }
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
  const refs = [...html.matchAll(/(?:href|src)="(\.\/[^"#?]+)/g)].map((m) => m[1]);
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

test('an element rendered hidden stays hidden: its class gets a [hidden] rule when the stylesheet would show it', () => {
  // A class with a display rule beats the browser's [hidden] rule, and that rule does not reach SVG elements:
  // both left the sprint 35 car and the sprint 36 height panel visible.
  const css = readFileSync(resolve('src/assets/style.css'), 'utf8');
  const shown = new Set();
  for (const [, selectors, body] of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
    if (!/display:\s*(?!none)/.test(body)) continue;
    for (const s of selectors.split(',')) if (/^\.[\w-]+$/.test(s.trim())) shown.add(s.trim().slice(1));
  }
  const svg = new Set(['rect', 'circle', 'path', 'g', 'text', 'line', 'polyline', 'polygon', 'ellipse']);
  const missing = new Set();
  for (const lang of LANGS) {
    for (const file of readdirSync(join(dist, lang)).filter((f) => f.endsWith('.html'))) {
      for (const [, tag, attrs] of readFileSync(join(dist, lang, file), 'utf8').matchAll(/<([a-z][\w-]*)\s([^>]*)>/g)) {
        if (!/(?:^|\s)hidden(?=[\s/=]|$)/.test(attrs)) continue;
        const cls = attrs.match(/class="([\w-]+)/)?.[1];
        if (!cls || !(shown.has(cls) || svg.has(tag))) continue;
        if (!css.includes(`.${cls}[hidden]`)) missing.add(`${tag}.${cls} (${lang}/${file})`);
      }
    }
  }
  assert.deepEqual([...missing], []);
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
  const { projects } = loadData();
  const { links } = projects.find((p) => p.id === 'vitrine');
  assert.ok(page('fr', projectPage('vitrine')).includes(`href="${links.code}" data-link="code"`));
  // Every project has links once it starts: the case is checked on a copy of one, with its links removed.
  const data = loadData();
  const { links: _, ...unlinked } = data.projects[0];
  const planned = renderProjectPage(unlinked, { lang: 'fr', t: makeT(data.i18n.fr, 'fr'), data });
  assert.ok(!planned.includes('data-link='));
  assert.ok(planned.includes('data-no-links'));
});

test('the home page shows the temporary progress bars, computed from the data', () => {
  const { projects, sprints } = loadData();
  const p = progress(projects, sprints);
  for (const lang of LANGS) {
    const html = page(lang, 'index');
    const bars = [...html.matchAll(/<div class="progress-bar" role="progressbar"([^>]*)>/g)].map((m) => m[1]);
    // A finished sprint leaves the panel: its bar shows only while some of its stories are open.
    const running = p.sprint.done < p.sprint.total;
    // Every project done and no sprint under way: the temporary panel has left the home page (D14).
    if (p.portfolio.done === p.portfolio.total && !running) {
      assert.equal(bars.length, 0, `${lang}: no progress panel once everything is done`);
      assert.ok(!html.includes('id="h-progress"'), lang);
      continue;
    }
    assert.equal(bars.length, 1 + (running ? 1 : 0) + p.projects.length, `${lang}: one bar each for portfolio, running sprint and project`);
    for (const attrs of bars) {
      assert.match(attrs, /aria-labelledby="[\w-]+"/);
      assert.match(attrs, /aria-valuemin="0"/);
      assert.match(attrs, /aria-valuemax="100"/);
      assert.match(attrs, /aria-valuenow="\d+"/);
    }
    const sprintBar = bars.find((a) => a.includes('data-progress="sprint"'));
    assert.equal(Boolean(sprintBar), running);
    if (running) {
      assert.ok(sprintBar.includes(`aria-valuenow="${Math.round((100 * p.sprint.done) / p.sprint.total)}"`));
      const label = { fr: 'Sprint en cours (%n)', en: 'Current sprint (%n)' }[lang];
      assert.ok(html.includes(label.replace('%n', p.sprint.number)), `${lang}: ${label}`);
    }
    assert.ok(!/Sprint \d+ (terminé|finished)/.test(html), `${lang}: no finished sprint on the home page`);
  }
});

test('the progress figures tell a running sprint from a finished one', () => {
  const projects = [{ id: 'demo', status: 'in-progress', points: 3 }];
  const sprint = (states) => ({ number: 7, title: 't', stories: states.map((done, k) => ({ text: `story ${k} (demo)`, points: 1, done, closed: done })) });
  const running = progress(projects, [sprint([true, false])]);
  assert.deepEqual([running.sprint.done, running.sprint.total], [1, 2]);
  const finished = progress(projects, [sprint([true, true])]);
  assert.equal(finished.sprint.done, finished.sprint.total);
});

test('the home page counts the projects and their technologies from the data', () => {
  const { projects } = loadData();
  for (const lang of LANGS) {
    const html = page(lang, 'index');
    const lead = html.match(/<p class="lead">([^<]+)<\/p>/)[1];
    assert.ok(lead.startsWith(`${projects.length} `), lead);
    assert.ok(lead.includes(` ${techsOf(projects).length} `), lead);
    // The menu already links to the projects and the CV: no buttons for them, no project cards either.
    assert.ok(!html.includes('class="btn btn-primary" href="./projects.html"'));
    assert.ok(!/<article class="card"/.test(html), `${lang}: no project cards on the home page`);
  }
});

test('the lib-c page embeds the WebAssembly demo with its samples, and only that page does', () => {
  for (const lang of LANGS) {
    const html = page(lang, projectPage('lib-c'));
    assert.ok(html.includes('data-mesh-demo'), `${lang}: demo section`);
    assert.match(html, /<script type="module" src="\.\.\/assets\/meshdemo\.js\?v=[0-9a-f]{10}"><\/script>/);
    const samples = [...html.matchAll(/data-sample="([^"]+)"/g)].map((m) => m[1]);
    assert.deepEqual(samples.map((s) => s.split('/').pop()), ['cube.obj', 'tetrahedron.ply', 'torus.obj']);
    for (const s of samples) assert.ok(existsSync(resolve(join(dist, lang), s)), `${lang}: sample ${s}`);
    const labels = JSON.parse(html.match(/data-labels="([^"]+)"/)[1].replaceAll('&quot;', '"').replaceAll('&#39;', "'").replaceAll('&amp;', '&'));
    for (const key of ['vertices', 'edges', 'euler', 'genus', 'error.3', 'error.4', 'error.too-large']) {
      assert.ok(labels[key], `${lang}: label ${key}`);
    }
  }
  assert.ok(!page('fr', projectPage('qt')).includes('data-mesh-demo'));
  assert.ok(existsSync(join(dist, 'assets/wasm/meshlib.wasm')));
});

test('the topology page embeds the viewer, its bundle, samples and labels', () => {
  for (const lang of LANGS) {
    const html = page(lang, projectPage('topologie'));
    assert.ok(html.includes('data-topo-viewer'), `${lang}: viewer section`);
    assert.match(html, /<script type="module" src="\.\.\/assets\/topoviewer\.js\?v=[0-9a-f]{10}"><\/script>/);
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
    assert.deepEqual(periods('experience'), ['2025 – 2026', '2022 – 2025', '2021', '2020 – 2021', '2016 – 2020', '2019', '2015'], lang);
    assert.deepEqual(periods('education'), ['2026', '2022 – 2025', '2019 – 2021', '2015 – 2019', '2012 – 2015'], lang);
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
  const all = loadData().projects;
  // Each grid in sprint order; the games of Charles's studies in their own section, after the rest (D26).
  const order = [...all.filter((p) => p.group !== 'games'), ...all.filter((p) => p.group === 'games')].map((p) => p.id);
  assert.deepEqual([...new Set(ids)], order);
  assert.ok(html.includes('data-filter="games">Games</button>'), 'a Games filter');
  assert.ok(page('fr', 'projects').includes('data-filter="games">Jeux</button>'), 'un filtre Jeux');
  const games = html.slice(html.indexOf('data-games'));
  for (const id of ['othello', 'naval', 'aventure', 'bataille', 'morpion', 'rogue']) assert.ok(games.includes(`project-${id}.html`), id);
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

test('each project page says what each technology does in the project', () => {
  const { projects } = loadData();
  for (const lang of LANGS) {
    for (const p of projects) {
      const html = page(lang, projectPage(p.id));
      const block = html.slice(html.indexOf('data-roles'), html.indexOf('</dl>', html.indexOf('data-roles')));
      const terms = [...block.matchAll(/<dt>([^<]+)<\/dt><dd>([^<]+)<\/dd>/g)];
      assert.deepEqual(terms.map((m) => m[1]), p.stack.map(esc), `${lang} ${p.id}`);
      terms.forEach((m, k) => assert.equal(m[2], esc(pick(p.roles[p.stack[k]], lang))));
    }
  }
});

test('every risk has its outcome in both languages, shown in its own column', () => {
  const { scrum } = loadData();
  for (const lang of LANGS) {
    const html = page(lang, 'method');
    const outcomes = [...html.matchAll(/<td class="c-outcome" data-label="[^"]+">([^<]+)<\/td>/g)].map((m) => m[1]);
    assert.equal(outcomes.length, scrum.risks.length, lang);
    scrum.risks.forEach((r, k) => assert.equal(outcomes[k], esc(pick(r.outcome, lang)), `${lang} ${r.id}`));
  }
});

test('an empty backlog says every project is delivered instead of showing an empty table', () => {
  const data = loadData();
  const done = { ...data, projects: data.projects.map((p) => ({ ...p, status: 'done' })) };
  const t = makeT(data.i18n.fr, 'fr');
  const html = renderPage('method', { lang: 'fr', t, data: done });
  assert.ok(html.includes('data-backlog-empty'));
  assert.ok(!html.includes('data-backlog>'));
  const total = data.projects.reduce((acc, p) => acc + p.points, 0);
  assert.ok(html.includes(`les ${data.projects.length} projets sont livrés, ${total} points sur ${total}`));
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

test('the generalized maps course has its lessons, the 48 darts of the cube net and the quiz, in both languages', () => {
  for (const lang of LANGS) {
    const html = page(lang, projectPage('gcartes'));
    assert.equal((html.match(/<section class="gm-lesson"/g) ?? []).length, 6, `${lang}: six lessons`);
    assert.equal((html.match(/data-dart="\d+"/g) ?? []).length, 48, `${lang}: 48 darts drawn`);
    assert.equal((html.match(/<fieldset class="gm-question"/g) ?? []).length, 6, `${lang}: six questions`);
    assert.match(html, /<code>α2 α0 = α0 α2<\/code>/, `${lang}: code spans rendered`);
    assert.doesNotMatch(html, /`/, `${lang}: no backquote left`);
  }
});

test('the ML page shows the measured results: accuracies, threshold, both confusion matrices', () => {
  const read = (f) => JSON.parse(readFileSync(join(ROOT_DIR, 'projects/ml', f), 'utf8'));
  const metrics = read('metrics.json');
  const confusion = read('confusion.json');
  for (const lang of LANGS) {
    const html = page(lang, projectPage('ml'));
    const pct = (x) => `${(100 * x).toLocaleString(lang, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} %`;
    assert.ok(html.includes(pct(metrics.baseline.accuracy)) && html.includes(pct(metrics.pointnet.accuracy)), lang);
    assert.ok(html.includes(lang === 'fr' ? 'passe sous 90,0 %' : 'drops below 90.0 %'), `${lang}: threshold`);
    assert.equal((html.match(/<table class="confusion">/g) ?? []).length, 2);
    // Every count of the matrices is on the page, the diagonal marked.
    const diag = confusion.pointnet.map((row, i) => row[i]);
    assert.equal((html.match(/cm cm-diag/g) ?? []).length, 2 * diag.length);
  }
});

test('the roguelike page shows the Godot client in each language, with the commands to play', () => {
  for (const lang of LANGS) {
    const html = page(lang, 'project-rogue');
    const img = html.match(/<img src="\.\.\/assets\/images\/(rogue-(\w+)\.png)" width="1280" height="720" loading="lazy" alt="([^"]+)">/);
    assert.ok(img, lang);
    assert.equal(img[2], lang);
    assert.ok(existsSync(join(ROOT_DIR, 'src/assets/images', img[1])), img[1]);
    assert.ok(img[3].length > 80, 'a descriptive alt text');
    assert.match(html, /dotnet run --project src\/Rogue\.Cli/);
    assert.match(html, /docker compose up --build rogue-api/);
    assert.match(html, /godot --path godot/);
  }
});

test('the Qt viewer page shows the application in each language, with the commands to build it', () => {
  for (const lang of LANGS) {
    const html = page(lang, 'project-qt');
    const img = html.match(/<img src="\.\.\/assets\/images\/(qt-(\w+)\.png)" width="1280" height="720" loading="lazy" alt="([^"]+)">/);
    assert.ok(img, lang);
    assert.equal(img[2], lang);
    assert.ok(existsSync(join(ROOT_DIR, 'src/assets/images', img[1])), img[1]);
    assert.ok(img[3].length > 80, 'a descriptive alt text');
    assert.match(html, /cmake -S \. -B build/);
    assert.match(html, /qtviewer --lang (fr|en) sample:torus/);
    assert.match(html, /href="https:\/\/github\.com\/Dzop86\/Portfolio\/actions\/workflows\/qt\.yml"/);
  }
});

test('the projects page filters by category and by technology', () => {
  const { projects } = loadData();
  for (const lang of LANGS) {
    const html = page(lang, 'projects');
    const chips = [...html.matchAll(/data-tech="([^"]+)"/g)].map((m) => m[1]);
    assert.deepEqual(chips, ['all', ...techsOf(projects)]);
    for (const p of projects) assert.ok(html.includes(`data-techs="${esc(p.techs.join('|'))}" id="${p.id}"`), p.id);
    assert.ok(html.includes('data-filter-empty hidden'));
  }
});

test('the project management page shows the backlog, the sprint log and the metrics from the data', () => {
  const { projects, sprints, scrum } = loadData();
  const open = projects.filter((p) => p.status !== 'done');
  for (const lang of LANGS) {
    const html = page(lang, 'method');
    if (open.length === 0) {
      assert.ok(html.includes('data-backlog-empty') && !html.includes('data-backlog>'), `${lang}: the empty backlog says so`);
    } else {
      const backlog = html.slice(html.indexOf('data-backlog>'), html.indexOf('</table>', html.indexOf('data-backlog>')));
      assert.equal((backlog.match(/<tr>/g) || []).length - 1, open.length, `${lang}: one row per open project`);
    }
    // Every sprint, newest first, with its goal in the page's language and a link to its report.
    const items = [...html.matchAll(/<li class="sprint-item">[\s\S]*?<\/li>/g)].map((m) => m[0]);
    assert.equal(items.length, sprints.length);
    assert.ok(items[0].includes(`<strong>Sprint ${Math.max(...sprints.map((s) => s.number))}</strong>`));
    for (const s of sprints) {
      const item = items.find((i) => i.includes(`/scrum/sprint-${String(s.number).padStart(2, '0')}.md"`));
      assert.ok(item, `${lang}: sprint ${s.number}`);
      const goal = esc(s.goal[lang]).replace(/`([^`]+)`/g, '<code>$1</code>');
      assert.ok(item.includes(`<p class="sprint-goal">${goal}</p>`), `${lang}: goal of sprint ${s.number}`);
    }
    // One bar per sprint in the velocity chart; one point per sprint, from sprint 0, in the burndown.
    const velocityChart = html.slice(html.indexOf('data-chart="velocity"'), html.indexOf('</svg>', html.indexOf('data-chart="velocity"')));
    assert.equal((velocityChart.match(/class="chart-bar"/g) || []).length, sprints.length);
    const burn = html.slice(html.indexOf('data-chart="burndown"'), html.indexOf('</svg>', html.indexOf('data-chart="burndown"')));
    // In story points (D49): one point per sprint up to the last one that delivered, a labelled step per scope added.
    const bd = burndown(sprints, scrum.scopeSteps);
    assert.equal((burn.match(/class="chart-target"/g) || []).length, bd.remaining.length);
    assert.ok(html.includes(`>${scrum.sprintCount}</text>`), 'the burndown runs to the last planned sprint');
    for (const st of bd.steps) assert.ok(burn.includes(`data-step="${st.after}"`) && html.includes(`data-step-key="${st.after}"`) && html.includes(`+${st.points} `), `${lang}: step after ${st.after}`);
    // The lead gives the first scope and today's, and every decision that changed it.
    const lead = html.slice(html.indexOf('id="h-burndown"'), html.indexOf('data-chart="burndown"'));
    assert.ok(lead.includes(String(bd.start)) && lead.includes(String(bd.total)), `${lang}: burndown lead gives the scope`);
    for (const d of scrum.scopeDecisions) assert.ok(lead.includes(d), `${lang}: burndown lead cites ${d}`);
    // What the backlog says is left is the open stories, not the estimate of every reopened project.
    if (open.length > 0) assert.match(html, new RegExp(`data-backlog-total>[^<]*\\b${bd.open}\\b[^<]*\\b${bd.total}\\b`), `${lang}: open stories`);
    for (const id of ['h-backlog', 'h-sprintlog', 'h-metrics', 'h-estimation', 'h-retro', 'h-risks']) assert.ok(html.includes(`id="${id}"`), id);
  }
});

test('the parallel computing benchmarks are complete, exact where they must be, and shown as measured', async () => {
  const { readBench } = await import('../../src/templates.mjs');
  const { bench, sizes, backends, at } = readBench();
  assert.ok(sizes.length >= 3, 'several sizes');
  assert.ok(backends.includes('sequential') && backends.includes('openmp'));
  for (const size of sizes) {
    for (const b of backends) {
      const r = at(b, size);
      assert.ok(r, `${b} at ${size}`);
      assert.ok(r.ms > 0 && r.vertices > 0);
      // Sequential and OpenMP are exact; OpenCL and CUDA in double within 1e-12; float is only measured.
      if (b === 'sequential' || b === 'openmp') assert.equal(r.max_defect_error, 0, `${b} at ${size}`);
      if (b === 'opencl' || b === 'cuda-double') assert.ok(r.max_defect_error <= 1e-12, `${b} at ${size}`);
      if (b.startsWith('cuda')) {
        for (const k of ['upload_ms', 'kernels_ms', 'download_ms']) assert.ok(r[k] > 0, `${b} ${k}`);
        // The page shows the rest of the wall time as work on the processor: it cannot be negative.
        assert.ok(r.ms >= r.upload_ms + r.kernels_ms + r.download_ms, `${b} at ${size}: wall time covers the card's`);
      }
    }
  }
  assert.ok(bench.machine.cpu && bench.machine.openmp_threads > 0 && bench.runs >= 3);
  for (const lang of LANGS) {
    const html = page(lang, 'project-parallele');
    const chart = html.slice(html.indexOf('data-chart="bench"'), html.indexOf('</svg>', html.indexOf('data-chart="bench"')));
    assert.equal((chart.match(/class="chart-bar"/g) || []).length, backends.length);
    const table = html.slice(html.indexOf('data-bench'), html.indexOf('</table>', html.indexOf('data-bench')));
    assert.equal((table.match(/<tr>/g) || []).length, sizes.length + 1);
  }
});

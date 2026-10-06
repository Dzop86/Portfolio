import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { parseExamples } from './sqlplay/core.js';
import { ROOT, esc, pick, teachingTotals, riskLevel, sprintRange, roadmapState, projectPage, neighbours, progress, PAGES, REPO_URL } from './lib.mjs';

const SEAL = `<svg class="seal" viewBox="0 0 40 40" aria-hidden="true"><rect x="2" y="2" width="36" height="36" rx="7"/><text x="20" y="21" text-anchor="middle" dominant-baseline="central">CL</text></svg>`;

const ICON_SUN = `<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg>`;

function layout({ lang, page, navPage = page, t, title, body }) {
  const other = lang === 'fr' ? 'en' : 'fr';
  const nav = PAGES.map((p) => {
    const key = p === 'index' ? 'home' : p;
    const current = p === navPage ? ' aria-current="page"' : '';
    return `<a href="./${p}.html"${current}>${esc(t(`nav.${key}`))}</a>`;
  }).join('');

  return `<!doctype html>
<html lang="${lang}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(title)} · Charles Lepaire</title>
<meta name="description" content="${esc(t('home.kicker'))}">
<link rel="alternate" hreflang="${other}" href="../${other}/${page}.html">
<link rel="icon" href="../assets/favicon.svg" type="image/svg+xml">
<link rel="manifest" href="../manifest.webmanifest">
<meta name="theme-color" content="#181818">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=Zen+Maru+Gothic:wght@500;700&display=swap">
<link rel="stylesheet" href="../assets/tokens.css">
<link rel="stylesheet" href="../assets/style.css">
<script>try{var s=localStorage.getItem('theme');if(s)document.documentElement.dataset.theme=s}catch(e){}</script>
</head>
<body data-page="${page}">
<a class="skip" href="#main">${esc(t('nav.skip'))}</a>
<header class="site-header">
  <div class="wrap header-row">
    <a class="brand" href="./index.html">${SEAL}<span>Charles Lepaire</span></a>
    <div class="header-tools">
      <a class="lang" href="../${other}/${page}.html" hreflang="${other}" lang="${other}">${esc(t('lang.switch'))}</a>
      <button class="theme-toggle" type="button" aria-label="${esc(t('theme.toggle'))}" title="${esc(t('theme.toggle'))}">${ICON_SUN}</button>
    </div>
  </div>
  <nav class="wrap site-nav" aria-label="Navigation">${nav}</nav>
</header>
<main id="main" class="wrap">
${body}
</main>
<footer class="site-footer">
  <div class="wrap footer-row">
    <span>${esc(t('footer.made'))}</span>
    <a href="${REPO_URL}">${esc(t('footer.source'))}</a>
  </div>
</footer>
<script src="../assets/app.js" defer></script>
</body>
</html>
`;
}

function pageHead(title, lead, kicker = '') {
  return `<section class="page-head">
  ${kicker ? `<p class="kicker">${esc(kicker)}</p>` : ''}
  <h1>${esc(title)}</h1>
  <p class="lead">${esc(lead)}</p>
</section>`;
}

function home({ lang, t, data }) {
  const { cv, projects } = data;
  const hours = teachingTotals(cv.teaching).total;
  const featured = projects.filter((p) => ['topologie', 'ml', 'spring', 'langage'].includes(p.id));
  return `<section class="hero">
  <p class="kicker">${esc(t('home.kicker'))}</p>
  <h1>Charles Lepaire</h1>
  <p class="lead">${esc(t('home.lead'))}</p>
  <div class="actions">
    <a class="btn btn-primary" href="./projects.html">${esc(t('home.cta.projects'))}</a>
    <a class="btn btn-ghost" href="./research.html">${esc(t('home.cta.research'))}</a>
  </div>
</section>
${progressPanel({ lang, t, data })}
<section class="stats" aria-label="Chiffres clés">
  <div class="stat"><strong data-stat="projects">${projects.length}</strong><span>${esc(t('home.stats.projects'))}</span></div>
  <div class="stat"><strong data-stat="publications">${cv.publications.length}</strong><span>${esc(t('home.stats.publications'))}</span></div>
  <div class="stat"><strong data-stat="hours">${hours}</strong><span>${esc(t('home.stats.hours'))}</span></div>
</section>
<section class="split">
  <article class="panel">
    <h2>${esc(t('home.thread.title'))}</h2>
    <p>${esc(t('home.thread.text'))}</p>
  </article>
  <article class="panel panel-accent">
    <h2>${esc(t('home.genai.title'))}</h2>
    <p>${esc(t('home.genai.text'))}</p>
  </article>
</section>
<section>
  <div class="cards">${featured.map((p) => projectCard(p, lang, t)).join('')}</div>
</section>`;
}

/** Replaces {name} placeholders in a translated string. */
function fill(text, vars) {
  return text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
}

const percent = (part, total) => (total ? Math.round((100 * part) / total) : 0);

function progressBar(id, label, detail, value, soft = value) {
  return `<div class="progress-row">
    <div class="progress-head"><span id="pg-${id}">${esc(label)}</span><span class="meta">${esc(detail)}</span></div>
    <div class="progress-bar" role="progressbar" aria-labelledby="pg-${id}" data-progress="${id}" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${value}" aria-valuetext="${esc(detail)}">
      <span class="progress-fill progress-soft" style="--w:${soft}%"></span><span class="progress-fill" style="--w:${value}%"></span>
    </div>
  </div>`;
}

// Temporary: remove this panel (and its call in home) once the 17 projects are done (D14).
function progressPanel({ lang, t, data }) {
  const p = progress(data.projects, data.sprints);
  const { done, inProgress, total } = p.portfolio;
  const bars = [
    progressBar('portfolio', t('progress.portfolio'), fill(t('progress.portfolio.detail'), { done, inProgress, total }),
      percent(done, total), percent(done + inProgress, total)),
    progressBar('sprint', fill(t('progress.sprint'), { n: p.sprint.number }), fill(t('progress.points'), p.sprint),
      percent(p.sprint.done, p.sprint.total)),
    ...p.projects.map((pr) => {
      const name = pick(data.projects.find((x) => x.id === pr.id).name, lang);
      return progressBar(`project-${pr.id}`, fill(t('progress.project'), { name }), fill(t('progress.points'), pr),
        percent(pr.done, pr.total));
    }),
  ];
  return `<section class="panel progress-panel" aria-labelledby="h-progress">
  <h2 id="h-progress">${esc(t('progress.title'))}</h2>
  ${bars.join('\n  ')}
  <p class="meta">${esc(t('progress.note'))}</p>
</section>`;
}

function projectCard(p, lang, t) {
  const teaching = p.teaching ? `<p class="card-note">${esc(pick(p.teaching, lang))}</p>` : '';
  return `<article class="card" data-group="${esc(p.group)}" id="${esc(p.id)}">
  <div class="card-top">
    <span class="badge badge-${esc(p.status)}">${esc(t(`projects.status.${p.status}`))}</span>
    <span class="meta">${esc(t('projects.sprint'))} ${esc(p.sprint)} · ${p.points} ${esc(t('projects.points'))}</span>
  </div>
  <h3><a href="./${projectPage(p.id)}.html">${esc(pick(p.name, lang))}</a></h3>
  <p>${esc(pick(p.pitch, lang))}</p>
  ${teaching}
  <ul class="tags">${p.stack.map((s) => `<li>${esc(s)}</li>`).join('')}</ul>
</article>`;
}

function projects({ lang, t, data }) {
  const groups = ['web', 'back', 'systems', 'science'];
  const filters = [`<button type="button" class="chip" aria-pressed="true" data-filter="all">${esc(t('projects.filter.all'))}</button>`]
    .concat(groups.map((g) => `<button type="button" class="chip" aria-pressed="false" data-filter="${g}">${esc(t(`projects.group.${g}`))}</button>`))
    .join('');
  return `${pageHead(t('projects.title'), t('projects.lead'))}
<div class="chips" role="group" aria-label="${esc(t('projects.title'))}">${filters}</div>
<div class="cards" data-filterable>${data.projects.map((p) => projectCard(p, lang, t)).join('')}</div>`;
}

function research({ lang, t, data }) {
  const { cv } = data;
  const totals = teachingTotals(cv.teaching);
  const max = Math.max(...cv.teaching.map((r) => r.td + r.tp));
  const levels = [...new Set(cv.teaching.map((r) => r.level))].sort();

  // Lists come newest first from loadData (sortCv).
  const timeline = (list) => list.map((e) => {
    const detail = pick(e.detail, lang);
    return `<li>
    <span class="tl-period">${esc(e.period)}</span>
    <div><h3>${esc(pick(e.title, lang))}</h3><p class="muted">${esc(pick(e.place, lang))}</p>${detail ? `<p>${esc(detail)}</p>` : ''}</div>
  </li>`;
  }).join('');

  const jury = cv.thesis.jury.map((j) =>
    `<li><strong>${esc(j.name)}</strong>, ${esc(pick(j.grade, lang))} <span class="muted">(${esc(j.affiliation)})</span> · ${esc(pick(j.role, lang))}</li>`).join('');

  const pubs = cv.publications.map((p) => {
    const doi = p.doi ? ` <a href="https://doi.org/${esc(p.doi)}">DOI</a>` : '';
    const note = p.note ? ` <span class="badge badge-done">${esc(pick(p.note, lang))}</span>` : '';
    return `<li>
      <span class="pub-type">${esc(t(`research.pubtype.${p.type}`))} · ${p.year}</span>
      <p class="pub-title">${esc(p.title)}</p>
      <p class="muted">${esc(p.authors)}. ${esc(p.venue)}.${doi}${note}</p>
    </li>`;
  }).join('');

  const rows = cv.teaching.map((r) => {
    const sum = r.td + r.tp;
    return `<tr data-level="${esc(r.level)}" data-td="${r.td}" data-tp="${r.tp}">
      <th scope="row">${esc(pick(r.course, lang))}<span class="muted cell-sub">${esc(r.institution)}</span></th>
      <td>${esc(r.years)}</td>
      <td>${esc(r.level)}</td>
      <td class="num">${r.td || '–'}</td>
      <td class="num">${r.tp || '–'}</td>
      <td class="num"><span class="bar" style="--w:${Math.round((sum / max) * 100)}%"></span>${sum}</td>
    </tr>`;
  }).join('');

  const levelChips = [`<button type="button" class="chip" aria-pressed="true" data-level="all">${esc(t('projects.filter.all'))}</button>`]
    .concat(levels.map((l) => `<button type="button" class="chip" aria-pressed="false" data-level="${esc(l)}">${esc(l)}</button>`))
    .join('');

  const sup = cv.supervision.map((s) => `<li><span class="tl-period">${esc(s.period)}</span><div><strong>${esc(s.level)}</strong> · ${esc(pick(s.topic, lang))}</div></li>`).join('');
  const resp = cv.responsibilities.map((r) => `<li><span class="tl-period">${esc(r.period)}</span><div>${esc(pick(r.title, lang))}</div></li>`).join('');

  return `${pageHead(t('research.title'), t('research.lead'), pick(cv.person.title, lang))}
<p class="summary">${esc(pick(cv.summary, lang))}</p>

<section class="split block paths">
  <div aria-labelledby="h-exp" role="region">
    <h2 id="h-exp">${esc(t('research.experience'))}</h2>
    <ol class="timeline" data-timeline="experience">${timeline(cv.experience)}</ol>
  </div>
  <div aria-labelledby="h-edu" role="region">
    <h2 id="h-edu">${esc(t('research.education'))}</h2>
    <ol class="timeline" data-timeline="education">${timeline(cv.education)}</ol>
  </div>
</section>

<section class="block" aria-labelledby="h-thesis">
  <h2 id="h-thesis">${esc(t('research.thesis'))}</h2>
  <article class="panel">
    <h3 class="thesis-title">${esc(pick(cv.thesis.title, lang))}</h3>
    <p>${esc(pick(cv.thesis.abstract, lang))}</p>
    <p class="label">${esc(t('research.keywords'))}</p>
    <ul class="tags">${cv.thesis.keywords.map((k) => `<li>${esc(k)}</li>`).join('')}</ul>
    <p class="label">${esc(t('research.jury'))}</p>
    <ul class="plain">${jury}</ul>
    <p class="actions"><a class="btn btn-ghost" href="${esc(cv.thesis.url)}" data-link="theses">${esc(t('research.thesis.link'))}</a></p>
  </article>
</section>

<section class="block" aria-labelledby="h-pubs">
  <h2 id="h-pubs">${esc(t('research.publications'))}</h2>
  <ol class="pubs">${pubs}</ol>
</section>

<section class="block" aria-labelledby="h-teach">
  <h2 id="h-teach">${esc(t('research.teaching'))}</h2>
  <div class="chips" role="group" aria-label="${esc(t('research.teaching.filter'))}">${levelChips}</div>
  <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-teach">
    <table class="teaching" data-teaching>
      <thead><tr>
        <th scope="col">${esc(t('research.teaching.course'))}</th>
        <th scope="col">${esc(t('research.teaching.years'))}</th>
        <th scope="col">${esc(t('research.teaching.level'))}</th>
        <th scope="col" class="num">${esc(t('research.teaching.td'))}</th>
        <th scope="col" class="num">${esc(t('research.teaching.tp'))}</th>
        <th scope="col" class="num">${esc(t('research.teaching.total'))}</th>
      </tr></thead>
      <tbody>${rows}</tbody>
      <tfoot><tr>
        <th scope="row" colspan="3">${esc(t('research.teaching.total'))}</th>
        <td class="num" data-total="td">${totals.td}</td>
        <td class="num" data-total="tp">${totals.tp}</td>
        <td class="num" data-total="all">${totals.total}</td>
      </tr></tfoot>
    </table>
  </div>
</section>

<section class="split block">
  <div>
    <h2>${esc(t('research.supervision'))}</h2>
    <ol class="timeline compact">${sup}</ol>
  </div>
  <div>
    <h2>${esc(t('research.responsibilities'))}</h2>
    <ol class="timeline compact">${resp}</ol>
  </div>
</section>`;
}

/**
 * 3 x 3 probability-impact matrix of one risk: probability across, impact up, each cell coloured by the
 * level of its score, the risk's own cell marked. Read by screen readers as one labelled image.
 */
function riskMatrix(p, i, label) {
  const cells = [];
  for (let impact = 3; impact >= 1; impact--) {
    for (let prob = 1; prob <= 3; prob++) {
      const own = prob === p && impact === i ? ' is-risk' : '';
      cells.push(`<span class="cell score-${riskLevel(prob * impact)}${own}"></span>`);
    }
  }
  return `<span class="risk-matrix" role="img" aria-label="${esc(label)}">${cells.join('')}</span>`;
}

function method({ lang, t, data }) {
  const { scrum } = data;
  const count = scrum.sprintCount;
  const state = roadmapState(data.sprints);
  // Sprint numbers; on narrow screens only the "major" ones (1, 5, 10...) stay visible.
  const header = Array.from({ length: count }, (_, i) =>
    `<span${i === 0 || (i + 1) % 5 === 0 ? ' data-major' : ''}>${i + 1}</span>`).join('');

  const rows = scrum.phases.map((ph) => {
    const [a, b] = sprintRange(ph.sprints);
    const status = b <= state.done ? 'done' : a <= state.current ? 'current' : 'planned';
    return `<li class="gantt-row" data-state="${status}">
      <span class="gantt-label"><strong>${esc(ph.sprints)}</strong> ${esc(pick(ph.label, lang))} <span class="gantt-state">${esc(t(`method.state.${status}`))}</span></span>
      <span class="gantt-track" style="--sprints:${count}"><span class="gantt-bar kind-${esc(ph.kind)}" style="grid-column:${a} / ${b + 1}"></span></span>
    </li>`;
  }).join('');

  // Register in the order of the ids (R1, R2...); the column buttons re-sort it in the browser (app.js).
  const risks = [...scrum.risks]
    .sort((x, y) => Number(x.id.slice(1)) - Number(y.id.slice(1)))
    .map((r) => {
      const score = r.p * r.i;
      const level = riskLevel(score);
      // data-label names the value when a narrow screen shows each risk as a card (style.css).
      return `<tr data-id="${Number(r.id.slice(1))}" data-p="${r.p}" data-i="${r.i}" data-score="${score}">
        <td class="muted c-id">${esc(r.id)}</td>
        <th scope="row" class="c-label">${esc(pick(r.label, lang))}</th>
        <td class="num c-val" data-label="${esc(t('method.probability'))}">${r.p}</td>
        <td class="num c-val" data-label="${esc(t('method.impact'))}">${r.i}</td>
        <td class="num c-val" data-label="${esc(t('method.score'))}"><span class="score score-${level}">${score}</span></td>
        <td class="c-matrix">${riskMatrix(r.p, r.i, fill(t('method.matrix.label'), { p: r.p, i: r.i, score, level: t(`method.level.${level}`) }))}</td>
        <td class="c-mitigation">${esc(pick(r.mitigation, lang))}</td>
      </tr>`;
    }).join('');
  const sortable = (key, label, num = true) => `<th scope="col"${num ? ' class="num"' : ''}${key === 'id' ? ' aria-sort="ascending"' : ''}>
          <button type="button" class="sort" data-sort="${key}">${esc(label)}<span class="sort-mark" aria-hidden="true"></span></button></th>`;

  const steps = t('method.genai.steps').split('|').map((s) => `<li>${esc(s)}</li>`).join('');
  const dod = scrum.dod[lang].map((d) => `<li>${esc(d)}</li>`).join('');

  return `${pageHead(t('method.title'), t('method.lead'))}
<p class="notice">${esc(fill(t('method.disclaimer'), { done: state.done }))}</p>

<section class="block" aria-labelledby="h-roadmap">
  <h2 id="h-roadmap">${esc(t('method.roadmap'))}</h2>
  <div class="gantt">
    <div class="gantt-row gantt-head"><span class="gantt-label">${esc(t('method.sprints'))}</span><span class="gantt-track months" style="--sprints:${count}">${header}</span></div>
    <ol class="plain">${rows}</ol>
  </div>
</section>

<section class="block" aria-labelledby="h-risks">
  <h2 id="h-risks">${esc(t('method.risks'))}</h2>
  <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-risks">
    <table class="risks" data-risks>
      <caption class="visually-hidden">${esc(t('method.sort.help'))}</caption>
      <thead><tr>
        ${sortable('id', t('method.id'), false)}
        <th scope="col">${esc(t('method.risk'))}</th>
        ${sortable('p', t('method.probability'))}
        ${sortable('i', t('method.impact'))}
        ${sortable('score', t('method.score'))}
        <th scope="col">${esc(t('method.matrix'))}</th>
        <th scope="col">${esc(t('method.mitigation'))}</th>
      </tr></thead>
      <tbody>${risks}</tbody>
    </table>
  </div>
  <p class="meta">${esc(t('method.matrix.help'))}</p>
</section>

<section class="split block">
  <article class="panel">
    <h2>${esc(t('method.genai'))}</h2>
    <ol class="steps">${steps}</ol>
  </article>
  <article class="panel">
    <h2>${esc(t('method.dod'))}</h2>
    <ul class="checks">${dod}</ul>
  </article>
</section>`;
}

function contact({ t, data }) {
  const { person } = data.cv;
  return `${pageHead(t('contact.title'), t('contact.lead'))}
<div class="actions">
  <a class="btn btn-primary" href="${esc(person.linkedin)}" rel="me">${esc(t('contact.linkedin'))}</a>
  <a class="btn btn-ghost" href="${esc(person.orcid)}" rel="me">${esc(t('contact.orcid'))}</a>
  <a class="btn btn-ghost" href="${esc(person.hal)}">${esc(t('contact.hal'))}</a>
</div>`;
}

function projectDetail(p, { lang, t, data }) {
  const { prev, next } = neighbours(data.projects, p.id);
  const links = Object.entries(p.links ?? {});
  const linkBlock = links.length
    ? `<div class="actions">${links.map(([kind, url], i) => `<a class="btn ${i ? 'btn-ghost' : 'btn-primary'}" href="${esc(url)}" data-link="${esc(kind)}">${esc(t(`project.link.${kind}`))}</a>`).join('')}</div>`
    : `<p class="notice" data-no-links>${esc(t('project.nolinks'))} ${esc(p.sprint)}.</p>`;
  const teaching = p.teaching ? `<p class="card-note">${esc(pick(p.teaching, lang))}</p>` : '';
  const pager = [
    prev ? `<a class="btn btn-ghost" rel="prev" href="./${projectPage(prev.id)}.html">← ${esc(pick(prev.name, lang))}</a>` : '',
    next ? `<a class="btn btn-ghost" rel="next" href="./${projectPage(next.id)}.html">${esc(pick(next.name, lang))} →</a>` : '',
  ].join('');

  return `<p class="crumb"><a href="./projects.html">← ${esc(t('project.back'))}</a></p>
${pageHead(pick(p.name, lang), pick(p.pitch, lang), t(`projects.group.${p.group}`))}
<div class="card-top project-meta">
  <span class="badge badge-${esc(p.status)}">${esc(t(`projects.status.${p.status}`))}</span>
  <span class="meta">${esc(t('projects.sprint'))} ${esc(p.sprint)} · ${p.points} ${esc(t('projects.points'))}</span>
</div>
${teaching}
${linkBlock}
${p.widget === 'mesh-reader' ? meshDemo(t) : ''}
${p.widget === 'topology-viewer' ? topoViewer(t) : ''}
${p.widget === 'sql-playground' ? sqlPlayground(t) : ''}
${p.widget === 'maille-playground' ? maillePlayground(t) : ''}
<section class="split">
  <article class="panel">
    <h2>${esc(t('project.stack'))}</h2>
    <ul class="tags">${p.stack.map((s) => `<li>${esc(s)}</li>`).join('')}</ul>
  </article>
  <article class="panel panel-accent">
    <h2>${esc(t('project.dod'))}</h2>
    <ul class="checks">${pick(data.scrum.dod, lang).map((d) => `<li>${esc(d)}</li>`).join('')}</ul>
  </article>
</section>
<nav class="actions pager" aria-label="${esc(t('project.pager'))}">${pager}</nav>`;
}

const DEMO_LABELS = ['format', 'vertices', 'polygons', 'triangles', 'edges', 'boundary', 'euler', 'bbox', 'genus',
  'open', 'closed.other', 'loading', 'atline', 'error', 'error.1', 'error.2', 'error.3', 'error.4', 'error.too-large',
  'error.load'];
const DEMO_SAMPLES = [['cube', 'cube.obj'], ['tetrahedron', 'tetrahedron.ply'], ['torus', 'torus.obj']];

// lib-c compiled to WebAssembly (D15); labels go to the script as JSON so it needs no i18n of its own.
function meshDemo(t) {
  const labels = Object.fromEntries(DEMO_LABELS.map((k) => [k, t(`demo.label.${k}`)]));
  const samples = DEMO_SAMPLES.map(([key, file]) =>
    `<button type="button" class="btn btn-ghost" data-sample="../assets/samples/${file}">${esc(t(`demo.sample.${key}`))}</button>`).join('');
  return `<section class="block panel demo" aria-labelledby="h-demo" data-mesh-demo data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-demo">${esc(t('demo.title'))}</h2>
  <p>${esc(t('demo.lead'))}</p>
  <div class="drop" data-drop>
    <label class="btn btn-primary file-pick">${esc(t('demo.choose'))}<input type="file" accept=".obj,.ply,.stl" class="visually-hidden"></label>
    <span class="muted">${esc(t('demo.drop'))}</span>
  </div>
  <div class="actions" role="group" aria-label="${esc(t('demo.samples'))}"><span class="muted demo-samples">${esc(t('demo.samples'))}</span>${samples}</div>
  <div data-result aria-live="polite"></div>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/meshdemo.js"></script>
</section>`;
}

const TOPO_LABELS = ['canvas', 'components', 'boundary', 'euler', 'genus', 'orientable', 'manifold', 'total', 'yes', 'no',
  'tip', 'tip.boundary', 'nowebgl', 'legend.neg', 'legend.pos', 'error.invalid'];
const TOPO_SAMPLES = ['torus', 'sphere', 'mobius', 'saddle'];

// C++ topology compiled to WebAssembly, drawn with three.js (D16, D17). Error labels are shared with the lib-c demo.
function topoViewer(t) {
  const labels = {
    ...Object.fromEntries(TOPO_LABELS.map((k) => [k, t(`topo.label.${k}`)])),
    ...Object.fromEntries(['atline', 'error', 'error.1', 'error.2', 'error.3', 'error.4', 'error.too-large', 'error.load']
      .map((k) => [k, t(`demo.label.${k}`)])),
  };
  const samples = TOPO_SAMPLES.map((key) =>
    `<button type="button" class="btn btn-ghost" aria-pressed="false" data-sample="../assets/samples/topologie/${key}.obj">${esc(t(`topo.sample.${key}`))}</button>`).join('');
  return `<section class="block panel viewer" aria-labelledby="h-viewer" data-topo-viewer data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-viewer">${esc(t('topo.title'))}</h2>
  <p>${esc(t('topo.lead'))}</p>
  <div class="actions" role="group" aria-label="${esc(t('demo.samples'))}"><span class="muted demo-samples">${esc(t('demo.samples'))}</span>${samples}
    <label class="btn btn-ghost file-pick">${esc(t('demo.choose'))}<input type="file" accept=".obj,.ply,.stl" class="visually-hidden"></label>
  </div>
  <p class="notice demo-error" data-error role="alert" hidden></p>
  <div class="viewer-grid">
    <div class="viewer-stage">
      <canvas role="img" aria-label="${esc(t('topo.label.loading'))}"></canvas>
      <div class="viewer-tip" aria-hidden="true" hidden></div>
    </div>
    <div>
      <dl class="demo-stats viewer-stats" data-result aria-live="polite"></dl>
      <div class="legend" data-legend hidden>
        <div class="legend-bar"></div>
        <div class="legend-ticks"><span data-tick="1" data-side="-1"></span><span data-tick="0" data-side="-1"></span><span style="left:50%">0</span><span data-tick="0" data-side="1"></span><span data-tick="1" data-side="1"></span></div>
        <div class="legend-scale muted"><span>${esc(t('topo.label.legend.neg'))}</span><span>${esc(t('topo.label.legend.pos'))}</span></div>
        <p class="meta legend-note">${esc(t('topo.legend.quantiles'))}</p>
      </div>
      <p class="meta">${esc(t('topo.help'))}</p>
      <p class="meta">${esc(t('topo.note.boundary'))}</p>
    </div>
  </div>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/topoviewer.js"></script>
</section>`;
}

const SQL_LABELS = ['running', 'rows', 'row', 'truncated', 'changes', 'ms', 'caption', 'error', 'error.load', 'timeout',
  'reset', 'schema.table', 'schema.view'];

// SQLite copy of the benchmark database in a web worker (sql.js, D20). Examples come from projects/sql.
function sqlPlayground(t) {
  const labels = Object.fromEntries(SQL_LABELS.map((k) => [k, t(`sql.label.${k}`)]));
  const examples = parseExamples(readFileSync(join(ROOT, 'projects/sql/sqlite/examples.sql'), 'utf8'))
    .map(({ key, sql }) => `<button type="button" class="btn btn-ghost" aria-pressed="false" data-example="${esc(key)}" data-sql="${esc(sql)}">${esc(t(`sql.example.${key}`))}</button>`)
    .join('');
  return `<section class="block panel demo sqlplay" aria-labelledby="h-sql" data-sql-playground data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-sql">${esc(t('sql.title'))}</h2>
  <p>${esc(t('sql.lead'))}</p>
  <div class="actions" role="group" aria-label="${esc(t('sql.examples'))}"><span class="muted demo-samples">${esc(t('sql.examples'))}</span>${examples}</div>
  <label class="sql-label" for="sql-editor">${esc(t('sql.query'))}</label>
  <textarea id="sql-editor" class="sql-editor" rows="8" spellcheck="false" autocapitalize="off" autocomplete="off" aria-describedby="sql-help"></textarea>
  <p class="meta" id="sql-help">${esc(t('sql.help'))}</p>
  <div class="actions">
    <button type="button" class="btn btn-primary" data-run>${esc(t('sql.run'))}</button>
    <button type="button" class="btn btn-ghost" data-reset>${esc(t('sql.reset'))}</button>
  </div>
  <p class="meta" data-status aria-live="polite"></p>
  <p class="notice demo-error" data-error role="alert" hidden></p>
  <div data-result></div>
  <details class="sql-schema">
    <summary>${esc(t('sql.schema'))}</summary>
    <ul data-schema><li class="muted">${esc(t('sql.schema.pending'))}</li></ul>
  </details>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/sqlplay.js"></script>
</section>`;
}

const MAILLE_LABELS = ['loading', 'running', 'error.load', 'syntax', 'type error', 'runtime error', 'at', 'goto', 'more',
  'node', 'too-large', 'message'];
// Examples shown on the page, in this order; their source comes from projects/langage/examples.
const MAILLE_EXAMPLES = ['genus', 'euler', 'polymorphism', 'compose', 'factorial', 'type_error', 'syntax_error'];

// Maille in the browser (D22): C parser in WebAssembly, OCaml interpreter through js_of_ocaml.
function maillePlayground(t) {
  const labels = Object.fromEntries(MAILLE_LABELS.map((k) => [k, t(`maille.label.${k}`)]));
  const examples = MAILLE_EXAMPLES.map((key) => {
    const source = readFileSync(join(ROOT, `projects/langage/examples/${key}.maille`), 'utf8');
    return `<button type="button" class="btn btn-ghost" aria-pressed="false" data-example="${esc(key)}" data-source="${esc(source)}">${esc(t(`maille.example.${key}`))}</button>`;
  }).join('');
  return `<section class="block panel demo mailleplay" aria-labelledby="h-maille" data-maille-playground data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-maille">${esc(t('maille.title'))}</h2>
  <p>${esc(t('maille.lead'))}</p>
  <div class="actions" role="group" aria-label="${esc(t('maille.examples'))}"><span class="muted demo-samples">${esc(t('maille.examples'))}</span>${examples}</div>
  <label class="sql-label" for="maille-editor">${esc(t('maille.program'))}</label>
  <textarea id="maille-editor" class="sql-editor" rows="8" spellcheck="false" autocapitalize="off" autocomplete="off" aria-describedby="maille-help"></textarea>
  <p class="meta" id="maille-help">${esc(t('maille.help'))}</p>
  <div class="actions"><button type="button" class="btn btn-primary" data-run>${esc(t('maille.run'))}</button></div>
  <p class="maille-result" data-result aria-live="polite"></p>
  <div class="notice demo-error" data-error role="alert" hidden></div>
  <details class="maille-tree" open>
    <summary>${esc(t('maille.tree'))}</summary>
    <p class="meta">${esc(t('maille.tree.help'))}</p>
    <div data-tree></div>
  </details>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script src="../assets/wasm/maille-interp.js" defer></script>
  <script type="module" src="../assets/mailleplay.js"></script>
</section>`;
}

/** Detail page of one project, highlighted as "projects" in the navigation. */
export function renderProjectPage(p, ctx) {
  const body = projectDetail(p, ctx);
  return layout({ ...ctx, page: projectPage(p.id), navPage: 'projects', title: pick(p.name, ctx.lang), body });
}

const RENDERERS = { index: home, projects, research, method, contact };
const TITLES = { index: 'home.title', projects: 'projects.title', research: 'research.title', method: 'method.title', contact: 'contact.title' };

export function renderPage(page, ctx) {
  const body = RENDERERS[page](ctx);
  return layout({ ...ctx, page, title: ctx.t(TITLES[page]), body });
}

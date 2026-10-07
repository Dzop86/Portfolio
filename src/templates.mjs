import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { parseExamples } from './sqlplay/core.js';
import { CUBE_NET, cubeNetMap, dartGeometry } from '../projects/gcartes/src/net.js';
import { decompositionStep } from '../projects/gcartes/src/decompose.js';
import { ROOT, esc, pick, teachingTotals, riskLevel, sprintRange, roadmapState, projectPage, neighbours, progress, PAGES, REPO_URL, techsOf, velocity, burndown } from './lib.mjs';

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
  // Counted from the data: the text cannot fall behind when a project is added.
  const lead = fill(t('home.lead'), { projects: projects.length, techs: techsOf(projects).length });
  return `<section class="hero">
  <p class="kicker">${esc(t('home.kicker'))}</p>
  <h1>Charles Lepaire</h1>
  <p class="lead">${esc(lead)}</p>
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

// Temporary: remove this panel (and its call in home) once the 21 projects are done (D14).
function progressPanel({ lang, t, data }) {
  const p = progress(data.projects, data.sprints);
  const { done, inProgress, total } = p.portfolio;
  const bars = [
    progressBar('portfolio', t('progress.portfolio'), fill(t('progress.portfolio.detail'), { done, inProgress, total }),
      percent(done, total), percent(done + inProgress, total)),
    // Only a sprint in progress has a bar: a finished sprint leaves the panel (remark of Charles).
    ...(p.sprint.done < p.sprint.total
      ? [progressBar('sprint', fill(t('progress.sprint'), { n: p.sprint.number }), fill(t('progress.points'), p.sprint),
        percent(p.sprint.done, p.sprint.total))]
      : []),
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
  return `<article class="card" data-group="${esc(p.group)}" data-techs="${esc(p.techs.join('|'))}" id="${esc(p.id)}">
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
  const groups = ['web', 'back', 'systems', 'science', 'games'];
  const chip = (attr, value, label, pressed = false) =>
    `<button type="button" class="chip" aria-pressed="${pressed}" data-${attr}="${esc(value)}">${esc(label)}</button>`;
  const groupChips = [chip('filter', 'all', t('projects.filter.all'), true)]
    .concat(groups.map((g) => chip('filter', g, t(`projects.group.${g}`)))).join('');
  const techChips = [chip('tech', 'all', t('projects.filter.all'), true)]
    .concat(techsOf(data.projects).map((l) => chip('tech', l, l))).join('');
  // The games from Charles's studies get their own section after the other projects (D26).
  const games = data.projects.filter((p) => p.group === 'games');
  return `${pageHead(t('projects.title'), t('projects.lead'))}
<div class="filters">
  <div class="filter-row"><span class="filter-label" id="f-groups">${esc(t('projects.filter.groups'))}</span>
    <div class="chips" role="group" aria-labelledby="f-groups">${groupChips}</div></div>
  <div class="filter-row"><span class="filter-label" id="f-techs">${esc(t('projects.filter.techs'))}</span>
    <div class="chips" role="group" aria-labelledby="f-techs">${techChips}</div></div>
</div>
<p class="notice" data-filter-empty hidden>${esc(t('projects.filter.none'))}</p>
<div class="cards" data-filterable>${data.projects.filter((p) => p.group !== 'games').map((p) => projectCard(p, lang, t)).join('')}</div>
<section class="block" aria-labelledby="h-games" data-games>
  <h2 id="h-games">${esc(t('projects.games.title'))}</h2>
  <p class="lead">${esc(t('projects.games.lead'))}</p>
  <div class="cards" data-filterable>${games.map((p) => projectCard(p, lang, t)).join('')}</div>
</section>`;
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


// ---- Project management: backlog, sprint log, metrics (remarks of Charles, 7 October 2026, D40) ----

const number = (x, lang, digits = 1) => x.toLocaleString(lang === 'fr' ? 'fr-FR' : 'en-GB', { maximumFractionDigits: digits });

/** Escapes a sprint goal and turns its `code` spans into <code>. */
const inlineCode = (text) => esc(text).replace(/`([^`]+)`/g, '<code>$1</code>');

function backlogSection({ lang, t, data }) {
  const open = data.projects.filter((p) => p.status !== 'done');
  const { total } = burndown(data.projects, 0);
  const remaining = open.reduce((acc, p) => acc + p.points, 0);
  const rows = open.map((p) => `<tr>
      <th scope="row"><a href="./${projectPage(p.id)}.html">${esc(pick(p.name, lang))}</a></th>
      <td class="num">${p.points}</td>
      <td>${esc(p.sprint)}</td>
      <td><span class="badge badge-${esc(p.status)}">${esc(t(`projects.status.${p.status}`))}</span></td>
    </tr>`).join('');
  return `<section class="block" aria-labelledby="h-backlog">
  <h2 id="h-backlog">${esc(t('method.backlog'))}</h2>
  <p>${esc(t('method.backlog.lead'))}</p>
  <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-backlog">
    <table class="backlog" data-backlog>
      <thead><tr><th scope="col">${esc(t('method.backlog.project'))}</th><th scope="col" class="num">${esc(t('method.backlog.estimate'))}</th><th scope="col">${esc(t('method.backlog.sprints'))}</th><th scope="col">${esc(t('method.backlog.state'))}</th></tr></thead>
      <tbody>${rows}</tbody>
    </table>
  </div>
  <p class="meta">${esc(fill(t('method.backlog.total'), { remaining, total }))}</p>
</section>`;
}

function sprintLogSection({ lang, t, data }) {
  const v = new Map(velocity(data.sprints).map((x) => [x.number, x]));
  const item = (s) => `<li class="sprint-item">
      <div class="sprint-head"><strong>${esc(t('method.sprint'))} ${s.number}</strong>
        <span class="meta">${esc(fill(t('method.sprintlog.points'), v.get(s.number)))}</span></div>
      <p class="sprint-goal">${inlineCode(pick(s.goal, lang))}</p>
      <a href="${REPO_URL}/blob/main/scrum/sprint-${String(s.number).padStart(2, '0')}.md">${esc(fill(t('method.sprintlog.report'), { n: s.number }))}</a>
    </li>`;
  // Newest first; the five latest sprints open, the others folded.
  const sprints = [...data.sprints].sort((a, b) => b.number - a.number);
  const recent = sprints.slice(0, 5);
  const older = sprints.slice(5);
  const folded = older.length ? `<details class="sprint-older">
    <summary>${esc(fill(t('method.sprintlog.older'), { from: older.at(-1).number, to: older[0].number }))}</summary>
    <ol class="plain sprint-log">${older.map(item).join('')}</ol>
  </details>` : '';
  return `<section class="block" aria-labelledby="h-sprintlog">
  <h2 id="h-sprintlog">${esc(t('method.sprintlog'))}</h2>
  <p>${esc(t('method.sprintlog.lead'))}</p>
  <ol class="plain sprint-log" data-sprint-log>${recent.map(item).join('')}</ol>
  ${folded}
</section>`;
}

// Charts drawn at build time in SVG: one data series each (no legend box for velocity), a muted dashed
// reference, native tooltips (<title>) on generous hit targets, and the same figures in a table.
const CHART = { w: 720, h: 260, left: 40, right: 72, top: 16, bottom: 32 };

function axisY(max, step, y) {
  const ticks = [];
  for (let v = 0; v <= max; v += step) ticks.push(`<line class="chart-grid" x1="${CHART.left}" x2="${CHART.w - CHART.right}" y1="${y(v)}" y2="${y(v)}"/>
    <text class="chart-tick" x="${CHART.left - 6}" y="${y(v) + 4}" text-anchor="end">${v}</text>`);
  return ticks.join('');
}

function velocityChart({ lang, t, data }) {
  const v = velocity(data.sprints);
  const mean = v.reduce((acc, x) => acc + x.done, 0) / v.length;
  const max = 10;
  const plotW = CHART.w - CHART.left - CHART.right;
  const plotH = CHART.h - CHART.top - CHART.bottom;
  const slot = plotW / v.length;
  const barW = Math.min(16, slot * 0.6);
  const y = (val) => CHART.top + plotH - (val / max) * plotH;
  const bars = v.map((x, i) => {
    const cx = CHART.left + slot * (i + 0.5);
    const top = y(x.done);
    const r = Math.min(4, (CHART.top + plotH - top) / 2);
    const x0 = cx - barW / 2;
    const base = CHART.top + plotH;
    const label = fill(t('method.velocity.bar'), { n: x.number, done: x.done, committed: x.committed });
    // Rounded at the data end, square at the baseline.
    return `<g class="chart-hit"><title>${esc(label)}</title>
      <rect class="chart-target" x="${cx - slot / 2}" y="${CHART.top}" width="${slot}" height="${plotH}"/>
      <path class="chart-bar" d="M${x0},${base} V${top + r} Q${x0},${top} ${x0 + r},${top} H${x0 + barW - r} Q${x0 + barW},${top} ${x0 + barW},${top + r} V${base} Z"/>
    </g>`;
  }).join('');
  const ticksX = v.filter((x) => x.number === 1 || x.number % 5 === 0 || x.number === v.length)
    .map((x) => `<text class="chart-tick" x="${CHART.left + slot * (x.number - 0.5)}" y="${CHART.h - 10}" text-anchor="middle">${x.number}</text>`).join('');
  const meanLabel = fill(t('method.velocity.mean'), { mean: number(mean, lang) });
  const svg = `<svg class="chart" viewBox="0 0 ${CHART.w} ${CHART.h}" role="img" aria-labelledby="h-velocity" data-chart="velocity">
    ${axisY(max, 2, y)}
    ${bars}
    <line class="chart-ref" x1="${CHART.left}" x2="${CHART.w - CHART.right}" y1="${y(mean)}" y2="${y(mean)}"/>
    <text class="chart-label" x="${CHART.w - CHART.right + 6}" y="${y(mean) + 4}">${esc(meanLabel)}</text>
    ${ticksX}
  </svg>`;
  const rows = v.map((x) => `<tr><th scope="row">${x.number}</th><td class="num">${x.committed}</td><td class="num">${x.done}</td></tr>`).join('');
  return { mean, svg, table: `<table><thead><tr><th scope="col">${esc(t('method.sprint'))}</th><th scope="col" class="num">${esc(t('method.committed'))}</th><th scope="col" class="num">${esc(t('method.delivered'))}</th></tr></thead><tbody>${rows}</tbody></table>` };
}

function burndownChart({ lang, t, data }) {
  const count = data.scrum.sprintCount;
  const last = Math.max(...data.sprints.map((s) => s.number));
  const { total, remaining } = burndown(data.projects, last);
  const plotW = CHART.w - CHART.left - CHART.right;
  const plotH = CHART.h - CHART.top - CHART.bottom;
  const max = Math.ceil(total / 50) * 50;
  const x = (k) => CHART.left + (k / count) * plotW;
  const y = (val) => CHART.top + plotH - (val / max) * plotH;
  const line = remaining.map((left, k) => `${x(k)},${y(left)}`).join(' ');
  const points = remaining.map((left, k) => `<g class="chart-hit"><title>${esc(fill(t('method.burndown.point'), { n: k, left }))}</title>
      <circle class="chart-target" cx="${x(k)}" cy="${y(left)}" r="9"/></g>`).join('');
  const end = remaining.at(-1);
  const ticksX = Array.from({ length: count + 1 }, (_, k) => k).filter((k) => k % 5 === 0 || k === count)
    .map((k) => `<text class="chart-tick" x="${x(k)}" y="${CHART.h - 10}" text-anchor="middle">${k}</text>`).join('');
  const svg = `<svg class="chart" viewBox="0 0 ${CHART.w} ${CHART.h}" role="img" aria-labelledby="h-burndown" data-chart="burndown">
    ${axisY(max, 50, y)}
    <line class="chart-ref" x1="${x(0)}" y1="${y(total)}" x2="${x(count)}" y2="${y(0)}"/>
    <polyline class="chart-line" points="${line}"/>
    <circle class="chart-dot" cx="${x(last)}" cy="${y(end)}" r="4"/>
    <text class="chart-label" x="${x(last) + 10}" y="${y(end) - 8}">${esc(fill(t('method.burndown.end'), { left: end }))}</text>
    ${points}
    ${ticksX}
  </svg>`;
  const legend = `<ul class="chart-legend plain">
    <li><span class="key key-line" aria-hidden="true"></span>${esc(t('method.burndown.actual'))}</li>
    <li><span class="key key-ref" aria-hidden="true"></span>${esc(t('method.burndown.ideal'))}</li>
  </ul>`;
  const rows = remaining.map((left, k) => `<tr><th scope="row">${k}</th><td class="num">${left}</td></tr>`).join('');
  return { total, remaining: end, last, count, svg, legend, table: `<table><thead><tr><th scope="col">${esc(t('method.sprint'))}</th><th scope="col" class="num">${esc(t('method.left'))}</th></tr></thead><tbody>${rows}</tbody></table>` };
}

function metricsSection({ lang, t, data }) {
  const vel = velocityChart({ lang, t, data });
  const bd = burndownChart({ lang, t, data });
  const v = velocity(data.sprints);
  const committed = v.reduce((acc, x) => acc + x.committed, 0);
  const delivered = v.reduce((acc, x) => acc + x.done, 0);
  const figure = (id, title, lead, chart, extra = '') => `<figure class="chart-figure">
    <h3 id="h-${id}">${esc(title)}</h3>
    <p class="meta">${esc(lead)}</p>
    ${extra}
    <div class="chart-wrap" tabindex="0" role="region" aria-labelledby="h-${id}">${chart.svg}</div>
    <details class="chart-data"><summary>${esc(t('method.data'))}</summary><div class="table-wrap">${chart.table}</div></details>
  </figure>`;
  return `<section class="block" aria-labelledby="h-metrics">
  <h2 id="h-metrics">${esc(t('method.metrics'))}</h2>
  ${figure('velocity', t('method.velocity'), t('method.velocity.lead'), vel)}
  ${figure('burndown', t('method.burndown'), fill(t('method.burndown.lead'), { count: bd.count }), bd, bd.legend)}
</section>
<section class="block" aria-labelledby="h-estimation">
  <h2 id="h-estimation">${esc(t('method.estimation'))}</h2>
  <p>${esc(fill(t('method.estimation.text'), {
    mean: number(vel.mean, lang), min: Math.min(...v.map((x) => x.done)), max: Math.max(...v.map((x) => x.done)),
    delivered, committed, remaining: bd.remaining, next: bd.last + 1, last: bd.count,
  }))}</p>
</section>
<section class="block" aria-labelledby="h-retro">
  <h2 id="h-retro">${esc(t('method.retro'))}</h2>
  <p>${esc(t('method.retro.text'))} <a href="${REPO_URL}/tree/main/scrum">${esc(t('method.retro.link'))}</a></p>
</section>`;
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

${backlogSection({ lang, t, data })}

${sprintLogSection({ lang, t, data })}

${metricsSection({ lang, t, data })}

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
${p.widget === 'latex-editor' ? latexEditor(t, lang) : ''}
${p.widget === 'gmap-course' ? gmapCourse(t, lang) : ''}
${p.widget === 'ml-results' ? mlResults(t, lang) : ''}
${p.widget === 'othello-board' ? othelloBoard(t) : ''}
${p.widget === 'naval-screenshot' ? navalScreenshot(t, lang) : ''}
${p.widget === 'rogue-screenshot' ? rogueScreenshot(t, lang) : ''}
${p.widget === 'qt-screenshot' ? qtScreenshot(t, lang) : ''}
${p.widget === 'adventure-terminal' ? adventureTerminal(t, lang) : ''}
${p.widget === 'war-stats' ? warStats(t, lang) : ''}
${p.widget === 'parallel-bench' ? parallelBench(t, lang) : ''}
${p.widget === 'tictactoe-board' ? tictactoeBoard(t) : ''}
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

const LATEX_LABELS = ['count', 'none', 'diag', 'error', 'warning'];

// Live LaTeX editor (D23) on the article of projects/latex, in the page's language first.
function latexEditor(t, lang) {
  const labels = Object.fromEntries(LATEX_LABELS.map((k) => [k, t(`latex.label.${k}`)]));
  const article = (l) => readFileSync(join(ROOT, `projects/latex/article/portfolio.${l}.tex`), 'utf8');
  const articles = ['fr', 'en'].map((l) =>
    `<button type="button" class="btn btn-ghost" aria-pressed="${l === lang}" data-article="${l}" data-source="${esc(article(l))}">${esc(t(`latex.article.${l}`))}</button>`).join('');
  return `<section class="block panel demo latexed" aria-labelledby="h-latex" data-latex-editor data-view="preview" data-labels="${esc(JSON.stringify(labels))}">
  <link rel="stylesheet" href="../assets/katex/katex.min.css">
  <h2 id="h-latex">${esc(t('latex.title'))}</h2>
  <p>${esc(t('latex.lead'))}</p>
  <div class="actions">${articles}<button type="button" class="btn btn-ghost" data-download>${esc(t('latex.download'))}</button></div>
  <div class="actions latex-tabs" role="group" aria-label="${esc(t('latex.tabs'))}">
    <button type="button" class="btn btn-ghost" data-tab="source" aria-pressed="false">${esc(t('latex.tab.source'))}</button>
    <button type="button" class="btn btn-ghost" data-tab="preview" aria-pressed="true">${esc(t('latex.tab.preview'))}</button>
  </div>
  <div class="latex-grid">
    <div class="latex-pane latex-source">
      <label class="sql-label" for="latex-source">${esc(t('latex.source'))}</label>
      <textarea id="latex-source" class="sql-editor" spellcheck="false" autocapitalize="off" autocomplete="off">${esc(article(lang))}</textarea>
    </div>
    <div class="latex-pane latex-preview-pane">
      <p class="sql-label" id="latex-preview-label">${esc(t('latex.preview'))}</p>
      <div class="latex-preview" data-preview role="region" aria-labelledby="latex-preview-label" tabindex="0"></div>
    </div>
  </div>
  <div class="split latex-side">
    <div>
      <h3>${esc(t('latex.diagnostics'))}</h3>
      <p class="meta" data-status aria-live="polite"></p>
      <ul class="plain latex-diags" data-diagnostics></ul>
    </div>
    <div>
      <h3>${esc(t('latex.outline'))}</h3>
      <ol class="plain latex-outline" data-outline></ol>
    </div>
  </div>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/latexeditor.js"></script>
</section>`;
}

const GMAP_LABELS = ['dart', 'highlight', 'face.top', 'face.left', 'face.front', 'face.right', 'face.back', 'face.bottom',
  'correct', 'wrong', 'score', 'unanswered', 'counts'];

// Text with `code` spans: escaped, then the spans wrapped in <code>.
const withCode = (text) => esc(text).replace(/`([^`]+)`/g, '<code>$1</code>');

/** The cube net with its 48 darts, drawn at build time (D24); assets/gcourse.js makes it interactive. */
function cubeNetSvg(t) {
  const { darts } = cubeNetMap();
  const geometry = dartGeometry(darts);
  const squares = CUBE_NET.map((sq) => {
    const x = sq.col * 100;
    const y = sq.row * 100;
    const corners = [[x, y], [x + 100, y], [x + 100, y + 100], [x, y + 100]];
    const letters = sq.corners.map((c, k) => {
      const [cx, cy] = corners[k];
      return `<text class="gm-letter" x="${cx + (k === 1 || k === 2 ? -4 : 4)}" y="${cy + (k >= 2 ? -4 : 11)}" text-anchor="${k === 1 || k === 2 ? 'end' : 'start'}">${c}</text>`;
    }).join('');
    return `<g class="gm-square"><rect x="${x}" y="${y}" width="100" height="100"/><text class="gm-face" x="${x + 50}" y="${y + 54}" text-anchor="middle">${esc(t(`gcartes.label.face.${sq.key}`))}</text>${letters}</g>`;
  }).join('');
  const dartsSvg = geometry.map((g, d) =>
    `<g class="gm-dart" data-dart="${d}"><line x1="${g.start[0].toFixed(1)}" y1="${g.start[1].toFixed(1)}" x2="${g.end[0].toFixed(1)}" y2="${g.end[1].toFixed(1)}"/><circle cx="${g.end[0].toFixed(1)}" cy="${g.end[1].toFixed(1)}" r="2.6"/><line class="gm-hit" x1="${g.start[0].toFixed(1)}" y1="${g.start[1].toFixed(1)}" x2="${g.end[0].toFixed(1)}" y2="${g.end[1].toFixed(1)}"/></g>`).join('');
  // Every link drawn, as in a textbook figure: alpha0 between the two halves of a side, alpha1 at a
  // corner, alpha2 across a side shared by two squares next to each other in the net (the folded ones
  // show when a dart is selected).
  const { map } = cubeNetMap();
  const mid = (g) => [(g.start[0] + g.end[0]) / 2, (g.start[1] + g.end[1]) / 2];
  const ends = { 0: (g) => g.start, 1: (g) => g.end, 2: mid };
  let staticLinks = '';
  for (let d = 0; d < map.size; d++) {
    for (let i = 0; i <= 2; i++) {
      const e = map.alpha[i][d];
      if (e <= d) continue;
      const [a, b] = [ends[i](geometry[d]), ends[i](geometry[e])];
      if (i === 2 && Math.hypot(a[0] - b[0], a[1] - b[1]) > 45) continue;
      staticLinks += `<line class="gm-static gm-l${i}" x1="${a[0].toFixed(1)}" y1="${a[1].toFixed(1)}" x2="${b[0].toFixed(1)}" y2="${b[1].toFixed(1)}"/>`;
    }
  }
  return `<svg class="gm-net gm-paper" viewBox="-6 -6 412 312" role="img" aria-labelledby="gm-net-title"><title id="gm-net-title">${esc(t('gcartes.figure.alt'))}</title><rect class="gm-bg" x="-6" y="-6" width="412" height="312" rx="8"/>${squares}<g>${staticLinks}</g><g data-links></g>${dartsSvg}</svg>`;
}

/** Two squares cut into a G-map in four steps, one SVG per step, drawn at build time (D24). */
function decompositionFigure(t) {
  const S = 110;
  const xy = ([x, y]) => [(20 + x * S).toFixed(1), (20 + (1 - y) * S).toFixed(1)];
  const line = (a, b, cls) => {
    const [x1, y1] = xy(a);
    const [x2, y2] = xy(b);
    return `<line class="${cls}" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}"/>`;
  };
  const steps = [0, 1, 2, 3].map((k) => {
    const st = decompositionStep(k);
    const faces = st.faces.map((pts) => `<polygon class="gm-face-fill" points="${pts.map((p) => xy(p).join(',')).join(' ')}"/>`).join('');
    const sides = st.sides.map(([a, b]) => line(a, b, 'gm-side')).join('');
    const darts = st.darts.map(([inner, end]) => {
      const [cx, cy] = xy(end);
      return `${line(inner, end, 'gm-dart-line')}<circle class="gm-dart-dot" cx="${cx}" cy="${cy}" r="3"/>`;
    }).join('');
    const dots = st.dots.map((p) => {
      const [cx, cy] = xy(p);
      return `<circle class="gm-vertex" cx="${cx}" cy="${cy}" r="4"/>`;
    }).join('');
    const links = st.links.map((l) => line(l.from, l.to, `gm-static gm-l${l.alpha}`)).join('');
    return `<div class="gm-step" data-step="${k}">
      <svg class="gm-paper gm-step-svg" viewBox="0 0 260 150" role="img" aria-labelledby="gm-step-${k}"><title id="gm-step-${k}">${esc(t(`gcartes.step.${k}.title`))}</title><rect class="gm-bg" width="260" height="150" rx="8"/>${faces}${sides}${links}${darts}${dots}</svg>
      <p class="gm-step-caption"><strong>${esc(t(`gcartes.step.${k}.title`))}</strong> ${esc(t(`gcartes.step.${k}.text`))}</p>
    </div>`;
  }).join('');
  const buttons = [0, 1, 2, 3].map((k) => `<button type="button" class="btn btn-ghost" aria-pressed="${k === 0}" data-goto-step="${k}">${esc(t(`gcartes.step.${k}.button`))}</button>`).join('');
  return `<figure class="gm-figure gm-decompose" data-decompose>
    <div class="actions" role="group" aria-label="${esc(t('gcartes.steps'))}">${buttons}</div>
    ${steps}
    <figcaption class="meta">${esc(t('gcartes.legend'))}</figcaption>
  </figure>`;
}

// Course on generalized maps (D24): lessons from projects/gcartes/course.json, the cube net, a quiz.
function gmapCourse(t, lang) {
  const course = JSON.parse(readFileSync(join(ROOT, 'projects/gcartes/course.json'), 'utf8'));
  const labels = Object.fromEntries(GMAP_LABELS.map((k) => [k, t(`gcartes.label.${k}`)]));
  const lesson = (l) => `<section class="gm-lesson" aria-labelledby="gm-${l.id}"><h3 id="gm-${l.id}">${esc(pick(l.title, lang))}</h3>${pick(l.body, lang).map((p) => `<p>${withCode(p)}</p>`).join('')}</section>`;
  const orbit = (key) => `<button type="button" class="btn btn-ghost" aria-pressed="false" data-orbit="${key}">${esc(t(`gcartes.orbit.${key}`))}</button>`;
  const figure = `<figure class="gm-figure" data-gmap-figure data-labels="${esc(JSON.stringify(labels))}">
    ${cubeNetSvg(t)}
    <figcaption class="meta">${esc(t('gcartes.figure.caption'))}</figcaption>
    <p class="gm-status" data-status aria-live="polite">${esc(t('gcartes.figure.start'))}</p>
    <div class="actions" role="group" aria-label="${esc(t('gcartes.move'))}">
      ${[0, 1, 2].map((i) => `<button type="button" class="btn btn-ghost gm-alpha gm-a${i}" data-alpha="${i}">${esc(t('gcartes.apply'))} α${i}</button>`).join('')}
    </div>
    <div class="actions" role="group" aria-label="${esc(t('gcartes.orbits'))}">${['vertex', 'edge', 'face', 'component'].map(orbit).join('')}</div>
    <p class="meta" data-counts></p>
  </figure>`;
  const quiz = course.quiz.map((q, k) => `<fieldset class="gm-question" data-answer="${q.answer}">
      <legend>${k + 1}. ${esc(pick(q.question, lang))}</legend>
      ${q.options.map((o, j) => `<label class="gm-option"><input type="radio" name="gm-q${k}" value="${j}"> ${esc(pick(o, lang))}</label>`).join('')}
      <p class="gm-feedback" data-feedback hidden></p>
      <p class="meta gm-explain" data-explain hidden>${esc(pick(q.explain, lang))}</p>
    </fieldset>`).join('');
  const [l1, l2, ...rest] = course.lessons;
  return `<section class="block panel gm-course" aria-labelledby="h-gm" data-gmap-course>
  <h2 id="h-gm">${esc(t('gcartes.title'))}</h2>
  <p>${esc(t('gcartes.lead'))}</p>
  ${lesson(l1)}${lesson(l2)}
  ${decompositionFigure(t)}
  ${figure}
  ${rest.map(lesson).join('')}
  <section class="gm-quiz" aria-labelledby="h-gm-quiz">
    <h3 id="h-gm-quiz">${esc(t('gcartes.quiz'))}</h3>
    <form data-quiz>${quiz}
      <div class="actions"><button type="submit" class="btn btn-primary">${esc(t('gcartes.check'))}</button></div>
      <p class="gm-score" data-score aria-live="polite"></p>
    </form>
  </section>
  <noscript><p class="notice">${esc(t('gcartes.noscript'))}</p></noscript>
  <script type="module" src="../assets/gcourse.js"></script>
</section>`;
}

// Results of the ML project (D27), read at build time from what its DVC pipeline wrote.
function mlResults(t, lang) {
  const read = (f) => readFileSync(join(ROOT, 'projects/ml', f), 'utf8');
  const metrics = JSON.parse(read('metrics.json'));
  const confusion = JSON.parse(read('confusion.json'));
  const exported = JSON.parse(read('export/pointnet.json'));
  const threshold = Number(read('params.yaml').match(/min_accuracy:\s*([\d.]+)/)[1]);
  const pct = (x) => `${(100 * x).toLocaleString(lang, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} %`;
  const num = (x, d) => x.toLocaleString(lang, { minimumFractionDigits: d, maximumFractionDigits: d });
  const rows = [
    [t('ml.model.baseline'), metrics.baseline.accuracy, num(metrics.baseline.f1_macro, 3), `${num(metrics.baseline.seconds, 0)} s`],
    [t('ml.model.pointnet'), metrics.pointnet.accuracy, num(metrics.pointnet.f1_macro, 3), `${num(metrics.pointnet.seconds, 0)} s`],
    [t('ml.model.onnx'), exported.test_accuracy, '–', '–'],
  ].map(([name, acc, f1, secs]) => `<tr><th scope="row">${esc(name)}</th><td class="num">${pct(acc)}</td><td class="num">${f1}</td><td class="num">${secs}</td></tr>`).join('');
  const label = (c) => t(`ml.class.${c}`);
  const matrix = (key) => {
    const m = confusion[key];
    const head = confusion.classes.map((c) => `<th scope="col">${esc(label(c))}</th>`).join('');
    const body = m.map((row, i) => `<tr><th scope="row">${esc(label(confusion.classes[i]))}</th>${row.map((n, j) =>
      `<td class="num cm${i === j ? ' cm-diag' : n ? ' cm-off' : ''}">${n}</td>`).join('')}</tr>`).join('');
    return `<div class="table-wrap" tabindex="0" role="region" aria-label="${esc(t(`ml.confusion.${key}`))}"><table class="confusion">
      <caption>${esc(t(`ml.confusion.${key}`))}</caption>
      <thead><tr><th scope="col">${esc(t('ml.confusion.corner'))}</th>${head}</tr></thead><tbody>${body}</tbody></table></div>`;
  };
  return `<section class="block panel" aria-labelledby="h-ml">
  <h2 id="h-ml">${esc(t('ml.title'))}</h2>
  <p>${esc(t('ml.lead'))}</p>
  <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-ml"><table data-ml-metrics>
    <thead><tr><th scope="col">${esc(t('ml.col.model'))}</th><th scope="col" class="num">${esc(t('ml.col.accuracy'))}</th><th scope="col" class="num">${esc(t('ml.col.f1'))}</th><th scope="col" class="num">${esc(t('ml.col.time'))}</th></tr></thead>
    <tbody>${rows}</tbody></table></div>
  <p class="notice">${esc(fill(t('ml.threshold'), { threshold: pct(threshold) }))}</p>
  <div class="split ml-confusions">${matrix('baseline')}${matrix('pointnet')}</div>
  <p class="meta">${esc(t('ml.confusion.help'))}</p>
</section>`;
}

const OTHELLO_LABELS = ['empty', 'black', 'white', 'legal', 'your-turn', 'ai-thinking', 'ai-played', 'you-played', 'pass-you',
  'pass-ai', 'over-win', 'over-lose', 'over-draw', 'score', 'loading', 'error.load', 'last'];

// Othello against the AI (D28): the C engine in WebAssembly, a board of 64 buttons with a roving tab stop.
function othelloBoard(t) {
  const labels = Object.fromEntries(OTHELLO_LABELS.map((k) => [k, t(`othello.label.${k}`)]));
  const cells = Array.from({ length: 64 }, (_, sq) =>
    `<button type="button" class="oth-cell" data-sq="${sq}" tabindex="${sq === 19 ? 0 : -1}" aria-label="${'abcdefgh'[sq % 8]}${Math.floor(sq / 8) + 1}"></button>`).join('');
  const files = [...'abcdefgh'].map((c) => `<span>${c}</span>`).join('');
  const ranks = [1, 2, 3, 4, 5, 6, 7, 8].map((r) => `<span>${r}</span>`).join('');
  const levels = [1, 2, 3, 4, 5, 6].map((d) => `<option value="${d}"${d === 3 ? ' selected' : ''}>${esc(t(`othello.level.${d}`))}</option>`).join('');
  return `<section class="block panel demo othello" aria-labelledby="h-oth" data-othello data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-oth">${esc(t('othello.title'))}</h2>
  <p>${esc(t('othello.lead'))}</p>
  <div class="oth-controls">
    <fieldset class="oth-colour"><legend>${esc(t('othello.colour'))}</legend>
      <label><input type="radio" name="oth-colour" value="0" checked> ${esc(t('othello.colour.black'))}</label>
      <label><input type="radio" name="oth-colour" value="1"> ${esc(t('othello.colour.white'))}</label>
    </fieldset>
    <label class="oth-level">${esc(t('othello.level'))} <select data-level>${levels}</select></label>
    <div class="actions">
      <button type="button" class="btn btn-primary" data-new>${esc(t('othello.new'))}</button>
      <button type="button" class="btn btn-ghost" data-undo>${esc(t('othello.undo'))}</button>
    </div>
  </div>
  <p class="oth-status" data-status aria-live="polite">${esc(t('othello.label.loading'))}</p>
  <p class="meta oth-score" data-score></p>
  <div class="oth-wrap">
    <div class="oth-files" aria-hidden="true">${files}</div>
    <div class="oth-inner">
      <div class="oth-ranks" aria-hidden="true">${ranks}</div>
      <div class="oth-board" role="group" aria-label="${esc(t('othello.board'))}" aria-describedby="oth-help" data-board>${cells}</div>
    </div>
  </div>
  <p class="meta" id="oth-help">${esc(t('othello.help'))}</p>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/othelloplay.js"></script>
</section>`;
}

const MORPION_LABELS = ['square', 'empty', 'last', 'your-turn', 'ai-thinking', 'ai-played', 'you-played', 'over-win', 'over-lose',
  'over-draw', 'loading', 'error.load'];

// Tic-tac-toe against the AI (D34): moves read from the book computed in Python, a board of 9 buttons with a
// roving tab stop.
function tictactoeBoard(t) {
  const labels = Object.fromEntries(MORPION_LABELS.map((k) => [k, t(`morpion.label.${k}`)]));
  const cells = Array.from({ length: 9 }, (_, i) =>
    `<button type="button" class="ttt-cell" data-cell="${i}" tabindex="${i === 4 ? 0 : -1}"></button>`).join('');
  return `<section class="block panel demo morpion" aria-labelledby="h-ttt" data-morpion data-labels="${esc(JSON.stringify(labels))}">
  <h2 id="h-ttt">${esc(t('morpion.title'))}</h2>
  <p>${esc(t('morpion.lead'))}</p>
  <div class="oth-controls">
    <fieldset class="oth-colour"><legend>${esc(t('morpion.mark'))}</legend>
      <label><input type="radio" name="ttt-mark" value="X" checked> ${esc(t('morpion.mark.x'))}</label>
      <label><input type="radio" name="ttt-mark" value="O"> ${esc(t('morpion.mark.o'))}</label>
    </fieldset>
    <label class="oth-level">${esc(t('morpion.level'))} <select data-level>
      <option value="unbeatable" selected>${esc(t('morpion.level.unbeatable'))}</option>
      <option value="beginner">${esc(t('morpion.level.beginner'))}</option>
    </select></label>
    <div class="actions">
      <button type="button" class="btn btn-primary" data-new>${esc(t('morpion.new'))}</button>
    </div>
  </div>
  <p class="oth-status" data-status aria-live="polite">${esc(t('morpion.label.loading'))}</p>
  <div class="ttt-board" role="group" aria-label="${esc(t('morpion.board'))}" aria-describedby="ttt-help" data-board>${cells}</div>
  <p class="meta" id="ttt-help">${esc(t('morpion.help'))}</p>
  <noscript><p class="notice">${esc(t('morpion.noscript'))}</p></noscript>
  <script type="module" src="../assets/morpionplay.js"></script>
</section>`;
}

// Battleship (D29): a JavaFX application cannot run in the page; its screenshot is made headless by its tests.
function navalScreenshot(t, lang) {
  return `<section class="block panel" aria-labelledby="h-naval">
  <h2 id="h-naval">${esc(t('naval.title'))}</h2>
  <p>${esc(t('naval.lead'))}</p>
  <figure class="naval-shot">
    <img src="../assets/images/naval-${lang}.png" width="716" height="538" loading="lazy" alt="${esc(t('naval.alt'))}">
    <figcaption class="meta">${esc(t('naval.caption'))}</figcaption>
  </figure>
  <pre class="naval-run" tabindex="0"><code>cd projects/naval
mvn javafx:run</code></pre>
</section>`;
}

// The roguelike (D32): Godot 4 does not export C# to the web; the screenshot comes from the client itself.
function rogueScreenshot(t, lang) {
  return `<section class="block panel" aria-labelledby="h-rogue">
  <h2 id="h-rogue">${esc(t('rogue.title'))}</h2>
  <p>${esc(t('rogue.lead'))}</p>
  <figure class="naval-shot rogue-shot">
    <img src="../assets/images/rogue-${lang}.png" width="1280" height="720" loading="lazy" alt="${esc(t('rogue.alt'))}">
    <figcaption class="meta">${esc(t('rogue.caption'))}</figcaption>
  </figure>
  <p>${esc(t('rogue.run'))}</p>
  <pre class="naval-run" tabindex="0"><code>cd projects/rogue
dotnet run --project src/Rogue.Cli -- --lang ${lang}     # ${esc(t('rogue.terminal'))}
docker compose up --build rogue-api                # ${esc(t('rogue.api'))}
godot --path godot                                 # ${esc(t('rogue.godot'))}</code></pre>
</section>`;
}

// The Qt/OpenGL viewer: a desktop application; the screenshot is taken by the application itself.
function qtScreenshot(t, lang) {
  return `<section class="block panel" aria-labelledby="h-qt">
  <h2 id="h-qt">${esc(t('qt.title'))}</h2>
  <p>${esc(t('qt.lead'))}</p>
  <figure class="naval-shot rogue-shot">
    <img src="../assets/images/qt-${lang}.png" width="1280" height="720" loading="lazy" alt="${esc(t('qt.alt'))}">
    <figcaption class="meta">${esc(t('qt.caption'))}</figcaption>
  </figure>
  <p>${esc(t('qt.run'))}</p>
  <pre class="naval-run" tabindex="0"><code>cd projects/qt
cmake -S . -B build && cmake --build build
./build/qtviewer --lang ${lang} sample:torus        # ${esc(t('qt.sample'))}
./build/qtviewer ../lib-c/tests/data/cube.obj      # ${esc(t('qt.file'))}</code></pre>
  <p>${esc(t('qt.download'))} <a href="https://github.com/Dzop86/Portfolio/actions/workflows/qt.yml">${esc(t('qt.downloadLink'))}</a></p>
</section>`;
}

// "The lab at night" (D30): the Java engine compiled to JavaScript by TeaVM, behind a small terminal.
function adventureTerminal(t, lang) {
  const quick = (lang === 'fr' ? ['regarder', 'sac', 'parler', 'aide'] : ['look', 'bag', 'talk', 'help'])
    .map((c) => `<button type="button" class="btn btn-ghost" data-command="${c}">${c}</button>`).join('');
  return `<section class="block panel adventure" aria-labelledby="h-adv" data-adventure data-error="${esc(t('aventure.error'))}">
  <h2 id="h-adv">${esc(t('aventure.title'))}</h2>
  <p>${esc(t('aventure.lead'))}</p>
  <div class="adv-log" data-log role="log" aria-live="polite" aria-label="${esc(t('aventure.log'))}" tabindex="0"></div>
  <form class="adv-form">
    <label for="adv-input" class="visually-hidden">${esc(t('aventure.input'))}</label>
    <span class="adv-prompt" aria-hidden="true">&gt;</span>
    <input id="adv-input" type="text" autocomplete="off" autocapitalize="off" spellcheck="false" disabled placeholder="${esc(t('aventure.placeholder'))}">
    <button type="submit" class="btn btn-primary">${esc(t('aventure.send'))}</button>
  </form>
  <div class="actions" role="group" aria-label="${esc(t('aventure.quick'))}">${quick}<button type="button" class="btn btn-ghost" data-restart>${esc(t('aventure.restart'))}</button></div>
  <noscript><p class="notice">${esc(t('demo.noscript'))}</p></noscript>
  <script type="module" src="../assets/aventureplay.js"></script>
</section>`;
}

// War (D33): what 100 000 games played by the Ada program say, read from its committed output.
function warStats(t, lang) {
  const s = JSON.parse(readFileSync(join(ROOT, 'projects/bataille/data/stats.json'), 'utf8'));
  const n = (x, d = 0) => Number(x).toLocaleString(lang, { minimumFractionDigits: d, maximumFractionDigits: d });
  const ended = s.games - s.endless;
  const tiles = [
    [`${n(s.endless_percent, 1)} %`, fill(t('bataille.endless'), { n: n(s.endless) })],
    [n(s.plies_mean, 0), t('bataille.plies')],
    [n(s.wars_mean, 1), t('bataille.wars')],
    [`${n((100 * s.first_wins) / ended, 1)} %`, t('bataille.first')],
    [`${n(s.plies_shortest)} – ${n(s.plies_longest)}`, t('bataille.range')],
  ].map(([value, label]) => `<div><dt>${esc(label)}</dt><dd>${esc(value)}</dd></div>`).join('');
  return `<section class="block panel" aria-labelledby="h-war">
  <h2 id="h-war">${esc(t('bataille.title'))}</h2>
  <p>${esc(fill(t('bataille.lead'), { games: n(s.games) }))}</p>
  <dl class="demo-stats war-stats">${tiles}</dl>
  <p class="notice">${esc(t('bataille.why'))}</p>
  <pre class="naval-run" tabindex="0"><code>bataille --seed ${s.longest_seed}   # ${esc(fill(t('bataille.longest'), { plies: n(s.plies_longest) }))}
bataille --stats 100000</code></pre>
</section>`;
}

// Parallel curvature (D42): benchmarks measured by parbench on Charles's machine, read from the committed
// JSON. Bars at the largest size, one hue (the speed-up is in the labels), and every size in a table.
const BENCH_ORDER = ['sequential', 'openmp', 'opencl', 'cuda-double', 'cuda-float'];

export function readBench() {
  const bench = JSON.parse(readFileSync(join(ROOT, 'projects/parallele/data/bench.json'), 'utf8'));
  const sizes = [...new Set(bench.results.map((r) => r.triangles))].sort((a, b) => a - b);
  const backends = BENCH_ORDER.filter((b) => bench.results.some((r) => r.backend === b));
  const at = (backend, triangles) => bench.results.find((r) => r.backend === backend && r.triangles === triangles);
  return { bench, sizes, backends, at };
}

function parallelBench(t, lang) {
  const { bench, sizes, backends, at } = readBench();
  const n = (x, d = 0) => Number(x).toLocaleString(lang === 'fr' ? 'fr-FR' : 'en-GB', { minimumFractionDigits: d, maximumFractionDigits: d });
  const ms = (x) => (x < 10 ? n(x, 2) : x < 100 ? n(x, 1) : n(x, 0));
  const m = bench.machine;
  const label = (b) => fill(t(`parbench.backend.${b}`), { threads: m.openmp_threads, gpu: m.cuda, cl: m.opencl });
  const largest = sizes.at(-1);
  const base = at('sequential', largest).ms;

  // Horizontal bars: one row per version, the label on the left, time and speed-up at the bar's end.
  const W = 720, left = 230, right = 150, row = 38, top = 8;
  const H = top * 2 + row * backends.length;
  const max = Math.max(...backends.map((b) => at(b, largest).ms));
  const bars = backends.map((b, i) => {
    const r = at(b, largest);
    const w = Math.max(2, ((W - left - right) * r.ms) / max);
    const y = top + i * row;
    const text = fill(t('parbench.bar'), { ms: ms(r.ms), speedup: n(base / r.ms, 1) });
    return `<g class="chart-hit"><title>${esc(`${label(b)} : ${text}`)}</title>
      <rect class="chart-target" x="0" y="${y}" width="${W}" height="${row}"/>
      <text class="chart-tick" x="${left - 10}" y="${y + row / 2 + 4}" text-anchor="end">${esc(label(b))}</text>
      <path class="chart-bar" d="M${left},${y + 9} H${left + w - Math.min(4, w / 2)} Q${left + w},${y + 9} ${left + w},${y + 13} V${y + row - 13} Q${left + w},${y + row - 9} ${left + w - Math.min(4, w / 2)},${y + row - 9} H${left} Z"/>
      <text class="chart-label" x="${left + w + 8}" y="${y + row / 2 + 4}">${esc(text)}</text>
    </g>`;
  }).join('');
  const svg = `<svg class="chart" viewBox="0 0 ${W} ${H}" role="img" aria-labelledby="h-bench-chart" data-chart="bench">${bars}</svg>`;

  const head = `<tr><th scope="col">${esc(t('parbench.size'))}</th>${backends.map((b) => `<th scope="col" class="num">${esc(label(b))}</th>`).join('')}</tr>`;
  const rows = sizes.map((size) => `<tr><th scope="row">${n(size)}</th>${backends.map((b) => {
    const r = at(b, size);
    return `<td class="num">${ms(r.ms)} ms <span class="muted">×${n(at('sequential', size).ms / r.ms, 1)}</span></td>`;
  }).join('')}</tr>`).join('');

  const gpu = backends.filter((b) => b.startsWith('cuda'));
  const cd = at('cuda-double', largest);
  const transfers = gpu.length ? `<p>${esc(fill(t('parbench.transfers'), {
    upload: ms(cd.upload_ms), kernels: ms(cd.kernels_ms), download: ms(cd.download_ms),
    host: ms(cd.ms - cd.upload_ms - cd.kernels_ms - cd.download_ms), floatKernels: backends.includes('cuda-float') ? ms(at('cuda-float', largest).kernels_ms) : '–',
  }))}</p>` : '';
  const precision = backends.includes('cuda-float')
    ? `<p>${esc(fill(t('parbench.float'), { error: at('cuda-float', largest).max_defect_error.toExponential(1) }))}</p>` : '';
  const machine = fill(t('parbench.machine'), { cpu: m.cpu, threads: m.openmp_threads, logical: m.logical_processors, gpu: m.cuda || '–', runs: bench.runs });

  return `<section class="block panel" aria-labelledby="h-bench">
  <h2 id="h-bench">${esc(t('parbench.title'))}</h2>
  <p>${esc(t('parbench.lead'))}</p>
  <p class="meta">${esc(machine)}</p>
  <figure class="chart-figure">
    <h3 id="h-bench-chart">${esc(fill(t('parbench.chart'), { size: n(largest) }))}</h3>
    <div class="chart-wrap" tabindex="0" role="region" aria-labelledby="h-bench-chart">${svg}</div>
  </figure>
  <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-bench">
    <table class="bench" data-bench><thead>${head}</thead><tbody>${rows}</tbody></table>
  </div>
  ${transfers}
  ${precision}
  <p class="notice">${esc(t('parbench.memory'))}</p>
  <pre class="naval-run" tabindex="0"><code>cd projects/parallele
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release && cmake --build build
./build/parbench --threads ${m.openmp_threads} --out data/bench.json</code></pre>
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

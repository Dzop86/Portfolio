// "How the pieces fit together" (D57): each project's diagram is described in data/architecture.json (boxes on a
// grid, named arrows, texts in French and English) and drawn here, in SVG, in the style of the first one (Osmose's):
// 200 x 64 boxes, 240 px apart across and 140 px down, arrows that stop at the edge of the boxes.
import { esc, pick } from './lib.mjs';

export const BOX = { w: 200, h: 64, dx: 240, dy: 140, margin: 20 };
// Rough widths of the fonts (17 px semibold, 13 px): enough to keep a text inside its box.
const NAME_CHARS = 18;
const TECH_CHARS = 26;
const LABEL_CHARS = 30;

/** Top-left corner of a box at grid cell [col, row]. */
export function boxAt([col, row]) {
  return { x: BOX.margin + col * BOX.dx, y: BOX.margin + row * BOX.dy };
}

/** Size of the drawing: as many columns and rows as the boxes use. */
export function canvas(arch) {
  const cols = Math.max(...arch.boxes.map((b) => b.at[0])) + 1;
  const rows = Math.max(...arch.boxes.map((b) => b.at[1])) + 1;
  return { width: cols * BOX.dx, height: rows * BOX.dy - 30 };
}

/** Where a line from the centre of a box towards (dx, dy) leaves it, a little outside the edge. */
function exit(cx, cy, dx, dy, gap) {
  const hw = BOX.w / 2 + gap;
  const hh = BOX.h / 2 + gap;
  const t = Math.min(dx ? hw / Math.abs(dx) : Infinity, dy ? hh / Math.abs(dy) : Infinity);
  return [cx + dx * t, cy + dy * t];
}

/** The arrow between two boxes: its ends, and where its label goes (beside a vertical line, above the others). */
export function arrowGeometry(from, to, label = {}) {
  const a = boxAt(from.at);
  const b = boxAt(to.at);
  const [ax, ay] = [a.x + BOX.w / 2, a.y + BOX.h / 2];
  const [bx, by] = [b.x + BOX.w / 2, b.y + BOX.h / 2];
  const len = Math.hypot(bx - ax, by - ay);
  const [ux, uy] = [(bx - ax) / len, (by - ay) / len];
  const [x1, y1] = exit(ax, ay, ux, uy, 0);
  const [x2, y2] = exit(bx, by, -ux, -uy, 2);
  const [mx, my] = [(x1 + x2) / 2, (y1 + y2) / 2];
  // A vertical line: the label to its right. A steep one: to its right too, further when the line goes down to the
  // right (it is then closer under the text). A flat or shallow one: above it, high enough to clear the line along
  // the whole label (13 px text, about 7 px a letter).
  let lx;
  let ly;
  let anchor;
  if (Math.abs(ux) < 1e-9) [lx, ly, anchor] = [mx + 10, my + 4, 'start'];
  else if (Math.abs(uy) >= 0.35) [lx, ly, anchor] = [mx + (ux * uy < 0 ? 14 : 22), my + 4, 'start'];
  else [lx, ly, anchor] = [mx, my - 8 - Math.abs(uy / ux) * 3.5 * (label.chars ?? 0), 'middle'];
  // A label that would run past the right edge of the drawing goes to the other side of its line.
  if (anchor === 'start' && label.width && lx + 7 * (label.chars ?? 0) > label.width) [lx, anchor] = [2 * mx - lx, 'end'];
  if (label.dx !== undefined) lx = mx + label.dx;
  if (label.dy !== undefined) ly = my + label.dy;
  if (label.anchor) anchor = label.anchor;
  return { x1, y1, x2, y2, lx, ly, anchor, horizontal: Math.abs(uy) < 1e-9, adjacent: Math.abs(from.at[0] - to.at[0]) + Math.abs(from.at[1] - to.at[1]) === 1 };
}

const round = (n) => Math.round(n * 10) / 10;

/** The text alternative: written in the data, or said from the boxes and arrows. */
export function altText(arch, lang, t) {
  if (arch.alt) return pick(arch.alt, lang);
  const name = (id) => pick(arch.boxes.find((b) => b.id === id).name, lang);
  return `${t('arch.diagram')} ${arch.links.map((l) => `${name(l.from)} → ${name(l.to)}${l.label ? ` (${pick(l.label, lang)})` : ''}`).join(' ; ')}.`;
}

/** The same in words, under the drawing (it reads better than a small drawing on a phone): written, or one line per arrow. */
export function wordsOf(arch, lang) {
  if (arch.notes) return arch.notes.map((n) => pick(n, lang));
  const box = (id) => arch.boxes.find((b) => b.id === id);
  const named = (b) => `${pick(b.name, lang)} (${pick(b.tech, lang)})`;
  return arch.links.map((l) => `${named(box(l.from))} → ${named(box(l.to))}${l.label ? `${lang === 'fr' ? ' : ' : ': '}${pick(l.label, lang)}` : ''}`);
}

export function archSvg(id, arch, lang, t) {
  const byId = Object.fromEntries(arch.boxes.map((b) => [b.id, b]));
  const { width, height } = canvas(arch);
  const titleId = `arch-title-${id}`;
  const arrows = arch.links.map((l) => {
    const g = arrowGeometry(byId[l.from], byId[l.to], { ...l.labelAt, width, chars: l.label ? pick(l.label, lang).length : 0 });
    return `<g class="arch-link">
      <line x1="${round(g.x1)}" y1="${round(g.y1)}" x2="${round(g.x2)}" y2="${round(g.y2)}" marker-end="url(#arch-arrow-${esc(id)})"></line>
      ${l.label ? `<text x="${round(g.lx)}" y="${round(g.ly)}" text-anchor="${g.anchor}">${esc(pick(l.label, lang))}</text>` : ''}
    </g>`;
  }).join('\n      ');
  const boxes = arch.boxes.map((b) => {
    const { x, y } = boxAt(b.at);
    return `<g class="arch-box${b.core ? ' arch-core' : ''}">
      <rect x="${x}" y="${y}" width="${BOX.w}" height="${BOX.h}" rx="8"></rect>
      <text x="${x + BOX.w / 2}" y="${y + 27}" class="arch-name">${esc(pick(b.name, lang))}</text>
      <text x="${x + BOX.w / 2}" y="${y + 48}" class="arch-tech">${esc(pick(b.tech, lang))}</text>
    </g>`;
  }).join('\n      ');
  return `<svg viewBox="0 0 ${width} ${height}" style="max-width:${width}px" role="img" aria-labelledby="${titleId}">
      <title id="${titleId}">${esc(altText(arch, lang, t))}</title>
      <defs><marker id="arch-arrow-${esc(id)}" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0,0 L10,5 L0,10 z"></path></marker></defs>
      ${arrows}
      ${boxes}
    </svg>`;
}

/** The section of a project page: the drawing, then the same in words. Nothing when the project has no diagram. */
export function archSection(id, arch, lang, t) {
  if (!arch) return '';
  const words = wordsOf(arch, lang);
  return `<section class="block panel" aria-labelledby="h-arch">
  <h2 id="h-arch">${esc(t('arch.title'))}</h2>
  <figure class="arch">
    ${archSvg(id, arch, lang, t)}
  </figure>
  ${words.length ? `<ul class="arch-list">\n    ${words.map((w) => `<li>${esc(w)}</li>`).join('\n    ')}\n  </ul>` : ''}
</section>`;
}

/**
 * What is wrong with a diagram, as sentences (none when it is right): unknown or repeated boxes, two boxes in one
 * cell, arrows to unknown boxes, a missing language, a text too long for its box, a label on an arrow too short
 * to hold it (between two boxes side by side).
 */
export function archProblems(id, arch) {
  const problems = [];
  const both = (o, what) => {
    if (!o || typeof o.fr !== 'string' || typeof o.en !== 'string' || !o.fr || !o.en) problems.push(`${id}: ${what} needs fr and en`);
  };
  const ids = new Set();
  const cells = new Set();
  for (const b of arch.boxes ?? []) {
    if (ids.has(b.id)) problems.push(`${id}: box ${b.id} twice`);
    ids.add(b.id);
    const cell = String(b.at);
    if (cells.has(cell)) problems.push(`${id}: two boxes at ${cell}`);
    cells.add(cell);
    both(b.name, `box ${b.id} name`);
    both(b.tech, `box ${b.id} tech`);
    for (const lang of ['fr', 'en']) {
      if ((b.name?.[lang] ?? '').length > NAME_CHARS) problems.push(`${id}: box ${b.id} name too long in ${lang}`);
      if ((b.tech?.[lang] ?? '').length > TECH_CHARS) problems.push(`${id}: box ${b.id} tech too long in ${lang}`);
    }
  }
  if (ids.size < 3) problems.push(`${id}: fewer than three boxes`);
  const byId = Object.fromEntries((arch.boxes ?? []).map((b) => [b.id, b]));
  for (const l of arch.links ?? []) {
    if (!byId[l.from] || !byId[l.to]) {
      problems.push(`${id}: arrow ${l.from} -> ${l.to} to an unknown box`);
      continue;
    }
    if (!l.label) continue;
    both(l.label, `arrow ${l.from} -> ${l.to} label`);
    for (const lang of ['fr', 'en']) {
      if ((l.label[lang] ?? '').length > LABEL_CHARS) problems.push(`${id}: arrow ${l.from} -> ${l.to} label too long in ${lang}`);
    }
    const g = arrowGeometry(byId[l.from], byId[l.to], l.labelAt);
    if (g.horizontal && g.adjacent && !l.labelAt) problems.push(`${id}: arrow ${l.from} -> ${l.to} too short for a label`);
  }
  if (!(arch.links ?? []).length) problems.push(`${id}: no arrow`);
  if (arch.alt) both(arch.alt, 'alt');
  for (const n of arch.notes ?? []) both(n, 'note');
  return problems;
}

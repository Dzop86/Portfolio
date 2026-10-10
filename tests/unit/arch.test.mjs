// "How the pieces fit together" (D57, sprint 63): the diagrams described in data/architecture.json and their drawing.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { BOX, archProblems, archSection, arrowGeometry, boxAt, canvas, wordsOf } from '../../src/arch.mjs';
import { loadData, makeT } from '../../src/lib.mjs';
import { renderProjectPage } from '../../src/templates.mjs';

const data = loadData();
const t = { fr: makeT(data.i18n.fr, 'fr'), en: makeT(data.i18n.en, 'en') };
const text = (fr, en = fr) => ({ fr, en });
const box = (id, at, extra = {}) => ({ id, at, name: text(id), tech: text(`${id} tech`), ...extra });
const tiny = () => ({
  boxes: [box('a', [0, 0]), box('b', [0, 1]), box('c', [2, 1])],
  links: [{ from: 'a', to: 'b', label: text('lit', 'reads') }, { from: 'b', to: 'c' }],
});

test('every project has a diagram, and every diagram is right', () => {
  assert.deepEqual(Object.keys(data.architecture).sort(), data.projects.map((p) => p.id).sort());
  for (const [id, arch] of Object.entries(data.architecture)) assert.deepEqual(archProblems(id, arch), [], id);
});

test('the checks catch an unknown box, a missing language, a crowded cell, a long text and a label with no room', () => {
  assert.deepEqual(archProblems('x', tiny()), []);
  const broken = tiny();
  broken.links.push({ from: 'a', to: 'zz' });
  broken.boxes.push(box('d', [0, 0]));
  broken.boxes.push(box('e', [1, 0], { name: { fr: 'nom' } }));
  broken.boxes.push(box('f', [1, 2], { tech: text('une technologie bien trop longue pour sa boîte') }));
  broken.boxes.push(box('g', [2, 2]));
  broken.links.push({ from: 'f', to: 'g', label: text('collé') });
  assert.deepEqual(archProblems('x', broken), [
    'x: two boxes at 0,0',
    'x: box e name needs fr and en',
    'x: box f tech too long in fr',
    'x: box f tech too long in en',
    'x: arrow a -> zz to an unknown box',
    'x: arrow f -> g too short for a label',
  ]);
  assert.deepEqual(archProblems('y', { boxes: [box('a', [0, 0])], links: [] }), ['y: fewer than three boxes', 'y: no arrow']);
});

test('boxes sit on the grid, and the drawing is as large as the boxes it holds', () => {
  assert.deepEqual(boxAt([2, 1]), { x: BOX.margin + 2 * BOX.dx, y: BOX.margin + BOX.dy });
  assert.deepEqual(canvas(tiny()), { width: 720, height: 250 });
  assert.deepEqual(canvas(data.architecture.rpg), { width: 720, height: 390 });
});

test('an arrow runs from edge to edge, its label beside a vertical line and above a flat one', () => {
  const [a, b, c] = tiny().boxes;
  const down = arrowGeometry(a, b, { chars: 3 });
  assert.deepEqual([down.x1, down.y1, down.x2, down.y2], [120, 84, 120, 158]);
  assert.deepEqual([down.lx, down.anchor], [130, 'start']);
  const across = arrowGeometry(b, c, { chars: 3 });
  assert.deepEqual([across.x1, across.x2, across.y1], [220, 498, 192]);
  assert.deepEqual([across.lx, across.ly, across.anchor, across.horizontal, across.adjacent], [359, 184, 'middle', true, false]);
});

test('a label that would leave the drawing goes to the other side of its line, and a written place wins', () => {
  const right = box('r', [2, 0]);
  const below = box('s', [2, 1]);
  assert.equal(arrowGeometry(right, below, { chars: 4, width: 720 }).anchor, 'start');
  const flipped = arrowGeometry(right, below, { chars: 20, width: 720 });
  assert.deepEqual([flipped.lx, flipped.anchor], [590, 'end']);
  const placed = arrowGeometry(right, below, { chars: 20, width: 720, dx: 5, dy: -3, anchor: 'middle' });
  assert.deepEqual([placed.lx, placed.anchor], [605, 'middle']);
});

test('the section draws each box and arrow in the page language, with the same in words', () => {
  for (const lang of ['fr', 'en']) {
    const html = archSection('x', tiny(), lang, t[lang]);
    assert.equal((html.match(/<rect /g) ?? []).length, 3);
    assert.equal((html.match(/<line /g) ?? []).length, 2);
    assert.match(html, /<svg viewBox="0 0 720 250" style="max-width:720px" role="img" aria-labelledby="arch-title-x">/);
    assert.ok(html.includes(lang === 'fr' ? '<title id="arch-title-x">Schéma : a → b (lit) ; b → c.</title>' : '<title id="arch-title-x">Diagram: a → b (reads) ; b → c.</title>'));
    assert.ok(html.includes(lang === 'fr' ? '<li>a (a tech) → b (b tech) : lit</li>' : '<li>a (a tech) → b (b tech): reads</li>'));
  }
  assert.equal(archSection('x', undefined, 'fr', t.fr), '');
  assert.deepEqual(wordsOf(data.architecture.rpg, 'en').length, 5, 'written notes win');
});

test('integration: every project page shows its diagram once, in both languages', () => {
  for (const p of data.projects) {
    for (const lang of ['fr', 'en']) {
      const html = renderProjectPage(p, { lang, t: t[lang], data });
      assert.equal((html.match(/id="h-arch"/g) ?? []).length, 1, `${p.id} ${lang}`);
      const arch = data.architecture[p.id];
      assert.equal((html.match(/<g class="arch-box/g) ?? []).length, arch.boxes.length, `${p.id} ${lang}`);
      for (const b of arch.boxes) assert.ok(html.includes(`>${b.name[lang].replaceAll('&', '&amp;').replaceAll("'", '&#39;')}</text>`), `${p.id} ${lang} ${b.id}`);
    }
  }
});

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { esc, pick, makeT, teachingTotals, riskLevel, monthOffset, i18nParity, loadData } from '../../src/lib.mjs';

test('esc neutralises HTML special characters', () => {
  assert.equal(esc('<a href="x">\'&'), '&lt;a href=&quot;x&quot;&gt;&#39;&amp;');
  assert.equal(esc(undefined), '');
});

test('pick returns the requested language and fails loudly when missing', () => {
  assert.equal(pick({ fr: 'Bonjour', en: 'Hello' }, 'en'), 'Hello');
  assert.equal(pick('plain', 'fr'), 'plain');
  assert.throws(() => pick({ fr: 'Seulement' }, 'en'), /Missing "en"/);
});

test('makeT throws on an unknown key instead of rendering it silently', () => {
  const t = makeT({ a: 'A' }, 'fr');
  assert.equal(t('a'), 'A');
  assert.throws(() => t('b'), /Missing i18n key "b"/);
});

test('teachingTotals sums TD and TP', () => {
  assert.deepEqual(teachingTotals([{ td: 2, tp: 3 }, { td: 0, tp: 5 }]), { td: 2, tp: 8, total: 10 });
});

test('riskLevel thresholds', () => {
  assert.equal(riskLevel(9), 'high');
  assert.equal(riskLevel(6), 'high');
  assert.equal(riskLevel(4), 'medium');
  assert.equal(riskLevel(2), 'low');
});

test('monthOffset counts across years', () => {
  assert.equal(monthOffset('2025-09', '2025-09'), 0);
  assert.equal(monthOffset('2025-09', '2026-01'), 4);
  assert.equal(monthOffset('2025-09', '2026-10'), 13);
});

test('French and English dictionaries have the same keys', () => {
  const { i18n } = loadData();
  assert.deepEqual(i18nParity(i18n), { missingInEn: [], missingInFr: [] });
});

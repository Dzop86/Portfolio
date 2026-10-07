import { describe, expect, it } from 'vitest';
import { DICTIONARY, formatMs, formatNumber, formatPercent, makeT, pickLang } from '../src/i18n';

const placeholders = (text: string) => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1]).sort();

describe('dictionary', () => {
  it('has the same keys in both languages, none empty', () => {
    expect(Object.keys(DICTIONARY.en).sort()).toEqual(Object.keys(DICTIONARY.fr).sort());
    for (const lang of ['fr', 'en'] as const) for (const [key, text] of Object.entries(DICTIONARY[lang])) expect(text.trim(), `${lang} ${key}`).not.toBe('');
  });
  it('uses the same placeholders in both languages', () => {
    for (const key of Object.keys(DICTIONARY.fr) as (keyof typeof DICTIONARY.fr)[]) {
      expect(placeholders(DICTIONARY.en[key]), key).toEqual(placeholders(DICTIONARY.fr[key]));
    }
  });
});

describe('makeT', () => {
  it('fills placeholders and leaves an unknown one visible', () => {
    const t = makeT('en');
    expect(t('projects.points', { n: 5 })).toBe('5 points');
    expect(t('projects.points')).toBe('{n} points');
  });
});

describe('pickLang', () => {
  it('prefers ?lang, then the browser, then French', () => {
    expect(pickLang('?lang=en', 'fr-FR')).toBe('en');
    expect(pickLang('?lang=de', 'en-US')).toBe('en');
    expect(pickLang('', 'de-DE')).toBe('fr');
    expect(pickLang('', undefined)).toBe('fr');
  });
});

describe('number formats', () => {
  it('follows each language', () => {
    // fr-FR groups thousands with a narrow no-break space.
    expect(formatNumber(1234.5, 'fr', 1)).toBe('1 234,5');
    expect(formatNumber(1234.5, 'en', 1)).toBe('1,234.5');
    expect(formatPercent(0.9517, 'en')).toBe('95.2%');
    expect(formatPercent(0.9517, 'fr')).toBe('95,2 %');
  });
  it('gives durations as many decimals as they deserve', () => {
    expect(formatMs(2.345, 'en')).toBe('2.35');
    expect(formatMs(21.64, 'en')).toBe('21.6');
    expect(formatMs(332.1, 'en')).toBe('332');
  });
});

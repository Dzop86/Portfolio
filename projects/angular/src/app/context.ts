// The page's language, the API's address and the theme, injected where needed.
import { InjectionToken, inject, signal } from '@angular/core';
import { makeT, pickLang, type Lang, type T } from './shared';

/** Where the API lives relative to the dashboard: the site serves it at ../api/v1/ (D43). */
export const API_BASE = new InjectionToken<string>('API_BASE', { factory: () => '../api/v1/' });

/** The site's root relative to the dashboard (links to the project pages). */
export const SITE_ROOT = new InjectionToken<string>('SITE_ROOT', { factory: () => '../' });

/** ?lang=fr|en, else the browser's language, French by default; read once, as the page loads. */
export const LANG = new InjectionToken<Lang>('LANG', {
  factory: () => pickLang(window.location.search, navigator.language),
});

/** The translation function of the page's language. */
export const TRANSLATE = new InjectionToken<T>('TRANSLATE', { factory: () => makeT(inject(LANG)) });

export type Theme = 'dark' | 'light';

/** The site's theme switch: same attribute, same storage key, so the choice follows the visitor. */
export function themeSignal() {
  const read = (): Theme => (document.documentElement.dataset['theme'] === 'light' ? 'light' : 'dark');
  const theme = signal<Theme>(read());
  const toggle = () => {
    const next: Theme = read() === 'light' ? 'dark' : 'light';
    document.documentElement.dataset['theme'] = next;
    try {
      localStorage.setItem('theme', next);
    } catch {
      // Private browsing: the theme just is not remembered.
    }
    theme.set(next);
  };
  return { theme: theme.asReadonly(), toggle };
}

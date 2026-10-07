import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError, loadApi, type Api, type Lang } from './api';
import { makeT } from './i18n';
import { ProjectsView } from './views/ProjectsView';
import { ResultsView } from './views/ResultsView';
import { SprintsView } from './views/SprintsView';

export const VIEWS = ['projects', 'sprints', 'results'] as const;
export type View = (typeof VIEWS)[number];

/** The view named by the URL's fragment (#sprints), the projects otherwise. */
export function viewFromHash(hash: string): View {
  const name = hash.replace(/^#/, '');
  return (VIEWS as readonly string[]).includes(name) ? (name as View) : 'projects';
}

type Load = { state: 'loading' } | { state: 'ready'; api: Api } | { state: 'error'; error: ApiError };

/** Loads the API, aborted if the component goes away; `retry` loads it again. */
export function useApi(base?: string): [Load, () => void] {
  const [load, setLoad] = useState<Load>({ state: 'loading' });
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setLoad({ state: 'loading' });
    loadApi(base, controller.signal).then(
      (api) => setLoad({ state: 'ready', api }),
      (e: unknown) => {
        if (controller.signal.aborted) return;
        setLoad({ state: 'error', error: e instanceof ApiError ? e : new ApiError(String(e), null) });
      },
    );
    return () => controller.abort();
  }, [base, attempt]);
  return [load, useCallback(() => setAttempt((a) => a + 1), [])];
}

type Theme = 'dark' | 'light';

function readTheme(): Theme {
  return document.documentElement.dataset.theme === 'light' ? 'light' : 'dark';
}

/** The site's theme switch: same attribute, same storage key, so the choice follows the visitor. */
function useTheme(): [Theme, () => void] {
  const [theme, setTheme] = useState<Theme>(readTheme);
  const toggle = useCallback(() => {
    const next: Theme = readTheme() === 'light' ? 'dark' : 'light';
    document.documentElement.dataset.theme = next;
    try {
      localStorage.setItem('theme', next);
    } catch {
      // Private browsing: the theme just is not remembered.
    }
    setTheme(next);
  }, []);
  return [theme, toggle];
}

export function App({ lang, apiBase, siteRoot = '../' }: { lang: Lang; apiBase?: string; siteRoot?: string }) {
  const t = useMemo(() => makeT(lang), [lang]);
  const [view, setView] = useState<View>(() => viewFromHash(window.location.hash));
  const [load, retry] = useApi(apiBase);
  const [theme, toggleTheme] = useTheme();

  useEffect(() => {
    document.documentElement.lang = lang;
    document.title = `${t('title')} · Charles Lepaire`;
  }, [lang, t]);
  useEffect(() => {
    const onHash = () => setView(viewFromHash(window.location.hash));
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const other: Lang = lang === 'fr' ? 'en' : 'fr';
  return (
    <>
      <header className="top">
        <div className="wrap top-inner">
          <a className="back" href={`${siteRoot}${lang}/project-react.html`}>
            {t('back')}
          </a>
          <div className="top-actions">
            <a className="btn" href={`?lang=${other}${window.location.hash}`} hrefLang={other} lang={other} aria-label={t('lang.switchLabel')}>
              {t('lang.switch')}
            </a>
            <button type="button" className="btn" onClick={toggleTheme}>
              {theme === 'dark' ? t('theme.toLight') : t('theme.toDark')}
            </button>
          </div>
        </div>
      </header>
      <main className="wrap">
        <h1>{t('title')}</h1>
        <p className="lead">{t('lead')}</p>
        <nav aria-label={t('nav.label')} className="tabs">
          {VIEWS.map((v) => (
            <a key={v} href={`#${v}`} aria-current={v === view ? 'page' : undefined}>
              {t(`nav.${v}`)}
            </a>
          ))}
        </nav>
        {load.state === 'loading' && (
          <p role="status" className="muted">
            {t('loading')}
          </p>
        )}
        {load.state === 'error' && (
          <div role="alert" className="notice">
            <h2>{t('error.title')}</h2>
            <p>{t('error.text', { url: load.error.url, status: load.error.status ?? t('error.network') })}</p>
            <button type="button" className="btn" onClick={retry}>
              {t('error.retry')}
            </button>
          </div>
        )}
        {load.state === 'ready' && view === 'projects' && <ProjectsView data={load.api.projects} lang={lang} t={t} siteRoot={siteRoot} />}
        {load.state === 'ready' && view === 'sprints' && <SprintsView data={load.api.sprints} lang={lang} t={t} />}
        {load.state === 'ready' && view === 'results' && <ResultsView data={load.api} lang={lang} t={t} />}
      </main>
    </>
  );
}

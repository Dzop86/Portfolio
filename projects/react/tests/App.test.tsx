// Integration: the whole dashboard rendered on the API the site really builds (src/api.mjs at the root),
// served by a fake fetch that can also fail.
import { act, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest';
import { buildApi } from '../../../src/api.mjs';
import { loadData } from '../../../src/lib.mjs';
import { App, viewFromHash } from '../src/App';

let files: Record<string, unknown>;
beforeAll(() => {
  files = buildApi(loadData()) as Record<string, unknown>;
});

/** fetch answering from the API files; `broken` names one that answers 503. */
function serve(broken?: string) {
  const fetch = vi.fn(async (url: string) => {
    const file = url.split('/').pop() ?? '';
    if (file === broken) return new Response('down', { status: 503 });
    if (!(file in files)) return new Response('missing', { status: 404 });
    return new Response(JSON.stringify(files[file]), { headers: { 'Content-Type': 'application/json' } });
  });
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

function goTo(view: string) {
  act(() => {
    window.location.hash = view;
    window.dispatchEvent(new HashChangeEvent('hashchange'));
  });
}

afterEach(() => {
  vi.unstubAllGlobals();
  window.location.hash = '';
});

describe('viewFromHash', () => {
  it('knows the three views and falls back to the projects', () => {
    expect(viewFromHash('#sprints')).toBe('sprints');
    expect(viewFromHash('#results')).toBe('results');
    expect(viewFromHash('#nope')).toBe('projects');
    expect(viewFromHash('')).toBe('projects');
  });
});

describe('the dashboard', () => {
  it('loads the five API files, then lists every project with a link to its page', async () => {
    const fetch = serve();
    render(<App lang="fr" apiBase="../api/v1/" />);
    expect(screen.getByRole('status')).toHaveTextContent('Chargement');
    const cards = await screen.findAllByRole('listitem');
    const api = files['projects.json'] as { projects: { id: string }[] };
    expect(cards).toHaveLength(api.projects.length);
    expect(fetch).toHaveBeenCalledTimes(5);
    expect(fetch.mock.calls.map(([u]) => u).sort()).toEqual(
      ['mesh-io.json', 'ml.json', 'parallel-bench.json', 'projects.json', 'sprints.json'].map((f) => `../api/v1/${f}`),
    );
    const parallele = cards.find((c) => c.dataset.project === 'parallele')!;
    expect(within(parallele).getByRole('link', { name: 'Fiche du projet' })).toHaveAttribute('href', '../fr/project-parallele.html');
    expect(within(parallele).getByText('Terminé')).toBeInTheDocument();
  });

  it('filters the projects by technology and status', async () => {
    serve();
    const user = userEvent.setup();
    render(<App lang="en" />);
    await screen.findAllByRole('listitem');
    await user.selectOptions(screen.getByLabelText('Technology'), 'Ada');
    const ada = screen.getAllByRole('listitem').map((li) => li.dataset.project);
    expect(ada).toContain('ada');
    expect(ada).not.toContain('parallele');
    await user.selectOptions(screen.getByLabelText('Status'), 'planned');
    expect(screen.queryAllByRole('listitem')).toHaveLength(0);
    expect(screen.getByText('No project matches these filters.')).toBeInTheDocument();
  });

  it('shows velocity and burndown on the sprints view, with one table row per sprint', async () => {
    serve();
    window.location.hash = '#sprints';
    render(<App lang="en" />);
    await screen.findByRole('heading', { name: 'Sprints', level: 2 });
    const sprints = files['sprints.json'] as { sprints: unknown[] };
    const table = document.querySelector('[data-table="velocity"]')!;
    expect(table.querySelectorAll('tbody tr')).toHaveLength(sprints.sprints.length);
    expect(document.querySelectorAll('[data-series]')).toHaveLength(3); // committed, delivered, remaining
    // The sprint in progress is not drawn as a sprint delivering nothing.
    const sp = files['sprints.json'] as { done: number; velocity: { number: number }[] };
    const delivered = document.querySelectorAll('[data-series="done"] circle');
    expect(delivered).toHaveLength(sp.velocity.filter((v) => v.number <= sp.done).length);
    expect(screen.getByRole('link', { name: 'Sprints' })).toHaveAttribute('aria-current', 'page');
  });

  it('shows the measured results: five parallel versions, reading times, model accuracy', async () => {
    serve();
    const user = userEvent.setup();
    render(<App lang="fr" />);
    await screen.findAllByRole('listitem');
    goTo('#results');
    expect(document.querySelectorAll('[data-bar]')).toHaveLength(5);
    expect(document.querySelector('[data-bar="cuda-double"]')).not.toBeNull();
    // Three formats on the reading chart, five sizes in its table; changing the family redraws both.
    expect(document.querySelectorAll('[data-series]')).toHaveLength(3);
    expect(document.querySelectorAll('[data-table="meshio"] tbody tr')).toHaveLength(5);
    await user.selectOptions(screen.getByLabelText('Famille'), 'sphere');
    expect(screen.getByLabelText('Famille')).toHaveValue('sphere');
    // textContent: toHaveTextContent would turn the French no-break space into a plain one.
    expect(screen.getByText(/sphère, tore, pavé, cylindre, cône, capsule/)).toBeInTheDocument();
    expect(document.querySelector('[data-stat="accuracy"]')?.textContent).toMatch(/^9\d,\d\u00a0%$/);
  });

  it('says which file failed, and loads again on demand', async () => {
    serve('mesh-io.json');
    const user = userEvent.setup();
    render(<App lang="en" />);
    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('../api/v1/mesh-io.json did not answer (503)');
    serve();
    await user.click(within(alert).getByRole('button', { name: 'Try again' }));
    expect(await screen.findAllByRole('listitem')).not.toHaveLength(0);
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('offers the other language and keeps the theme switch of the site', async () => {
    serve();
    const user = userEvent.setup();
    render(<App lang="fr" />);
    expect(screen.getByRole('link', { name: 'Read the dashboard in English' })).toHaveAttribute('href', '?lang=en');
    expect(document.documentElement.lang).toBe('fr');
    await user.click(screen.getByRole('button', { name: 'Thème clair' }));
    expect(document.documentElement.dataset.theme).toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
    expect(screen.getByRole('button', { name: 'Thème sombre' })).toBeInTheDocument();
    await screen.findAllByRole('listitem');
  });
});

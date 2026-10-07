// Integration: the pages rendered by TestBed on the API that the site really builds (written by
// global-setup.mjs), each request answered by Angular's HTTP testing controller.
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import type { Type } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { API_BASE, LANG } from '../src/app/context';
import { ProjectsPage } from '../src/app/pages/projects';
import { ResultsPage } from '../src/app/pages/results';

let lastHttp: HttpTestingController | undefined;
const api = (file: string): object => JSON.parse(readFileSync(join(process.env['PORTFOLIO_API_DIR']!, file), 'utf8'));

async function mount<T>(component: Type<T>, lang: 'fr' | 'en'): Promise<{ fixture: ComponentFixture<T>; http: HttpTestingController; el: HTMLElement }> {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(withFetch()), provideHttpClientTesting(), { provide: LANG, useValue: lang }, { provide: API_BASE, useValue: 'api/' }],
  });
  const fixture = TestBed.createComponent(component);
  await settle(fixture);
  lastHttp = TestBed.inject(HttpTestingController);
  return { fixture, http: lastHttp, el: fixture.nativeElement as HTMLElement };
}

/**
 * Runs the effects (a resource sends its request from one) and redraws. Not whenStable(): it would wait
 * for the requests still pending, which only the test answers.
 */
async function settle(fixture: ComponentFixture<unknown>) {
  await new Promise((resolve) => setTimeout(resolve, 0));
  TestBed.tick();
  fixture.detectChanges();
}

/** Answers the pending request for `file` with the real API file, then lets the page settle. */
async function serve(fixture: ComponentFixture<unknown>, http: HttpTestingController, file: string) {
  http.expectOne(`api/${file}`).flush(api(file));
  await settle(fixture);
}

function choose(select: HTMLSelectElement, value: string) {
  select.value = value;
  select.dispatchEvent(new Event('change'));
}

// No request left unanswered; kept from mount(), as Angular resets its testing module before this runs.
afterEach(() => {
  lastHttp?.verify();
  lastHttp = undefined;
});

describe('ProjectsPage', () => {
  it('lists every project of the API with a link to its page, then filters by technology and status', async () => {
    const { fixture, http, el } = await mount(ProjectsPage, 'en');
    expect(el.querySelector('[role="status"]')?.textContent).toContain('Loading');
    await serve(fixture, http, 'projects.json');
    const projects = (api('projects.json') as { projects: { id: string }[] }).projects;
    expect(el.querySelectorAll('[data-project]')).toHaveLength(projects.length);
    expect(el.querySelector('[data-project="react"] a')?.getAttribute('href')).toBe('../en/project-react.html');

    choose(el.querySelector<HTMLSelectElement>('#f-tech')!, 'Ada');
    fixture.detectChanges();
    const ids = [...el.querySelectorAll<HTMLElement>('[data-project]')].map((c) => c.dataset['project']);
    expect(ids).toContain('ada');
    expect(ids).not.toContain('react');
    choose(el.querySelector<HTMLSelectElement>('#f-status')!, 'planned');
    fixture.detectChanges();
    expect(el.querySelectorAll('[data-project]')).toHaveLength(0);
    expect(el.textContent).toContain('No project matches these filters.');
    // What the menus show is what filters (an Angular trap: a value bound before its options exist).
    expect(el.querySelector<HTMLSelectElement>('#f-tech')!.value).toBe('Ada');
  });

  it('names the file that failed, and loads it again on demand', async () => {
    const { fixture, http, el } = await mount(ProjectsPage, 'fr');
    http.expectOne('api/projects.json').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await settle(fixture);
    const alert = el.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Le fichier api/projects.json n’a pas répondu (503)');
    alert!.querySelector('button')!.click();
    await settle(fixture);
    await serve(fixture, http, 'projects.json');
    expect(el.querySelector('[role="alert"]')).toBeNull();
    expect(el.querySelectorAll('[data-project]').length).toBeGreaterThan(0);
  });
});

describe('ResultsPage', () => {
  async function results(lang: 'fr' | 'en') {
    const page = await mount(ResultsPage, lang);
    for (const file of ['parallel-bench.json', 'mesh-io.json', 'ml.json']) await serve(page.fixture, page.http, file);
    return page;
  }

  it('shows the five parallel versions, as bars and as a table', async () => {
    const { el } = await results('fr');
    expect(el.querySelectorAll('[data-bar]')).toHaveLength(5);
    expect(el.querySelectorAll('[data-table="parallel"] tbody tr')).toHaveLength(5);
    expect(el.querySelector('[data-bar="sequential"]')?.textContent).toContain('×1,0');
  });

  it('draws the reading times of the torus first, its family shown in the menu, and redraws for another', async () => {
    const { fixture, el } = await results('en');
    const family = el.querySelector<HTMLSelectElement>('#f-family')!;
    expect(family.value).toBe('torus');
    expect(el.querySelectorAll('[data-series]')).toHaveLength(3);
    const firstRow = () => el.querySelector('[data-table="meshio"] tbody tr')?.textContent;
    const torus = firstRow();
    choose(family, 'sphere');
    fixture.detectChanges();
    expect(family.value).toBe('sphere');
    expect(firstRow()).not.toBe(torus);
    expect(el.querySelectorAll('[data-table="meshio"] tbody tr')).toHaveLength(5);
  });

  it('gives the model accuracy in the language\'s format, the shapes translated', async () => {
    const { el } = await results('fr');
    expect(el.querySelector('[data-stat="accuracy"]')?.textContent?.trim()).toMatch(/^9\d,\d %$/);
    expect(el.textContent).toContain('sphère, tore, pavé, cylindre, cône, capsule');
  });
});

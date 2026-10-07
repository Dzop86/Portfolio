import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withHashLocation } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from '../src/app/app';
import { routes } from '../src/app/app.routes';
import { LANG } from '../src/app/context';

function setup(lang: 'fr' | 'en') {
  TestBed.configureTestingModule({
    providers: [provideRouter(routes, withHashLocation()), provideHttpClient(), provideHttpClientTesting(), { provide: LANG, useValue: lang }],
  });
}

afterEach(() => {
  delete document.documentElement.dataset['theme'];
  localStorage.clear();
});

describe('App', () => {
  it('names the page in its language and links back to the site and to the React dashboard', async () => {
    setup('fr');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('h1')!.textContent).toBe('Dashboard Angular');
    expect(document.documentElement.lang).toBe('fr');
    expect(document.title).toBe('Dashboard Angular · Charles Lepaire');
    const links = [...el.querySelectorAll('a')].map((a) => a.getAttribute('href'));
    expect(links).toContain('../fr/project-angular.html');
    expect(links).toContain('../dashboard/?lang=fr');
    expect(el.querySelector('[aria-label="Read the dashboard in English"]')!.getAttribute('href')).toMatch(/^\?lang=en#\//);
  });

  it('switches the theme of the site and remembers it', async () => {
    setup('en');
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const button = (fixture.nativeElement as HTMLElement).querySelector('button')!;
    expect(button.textContent!.trim()).toBe('Light theme');
    button.click();
    await fixture.whenStable();
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
    expect(button.textContent!.trim()).toBe('Dark theme');
  });

  it('routes to the two views, and to the projects by default or for an unknown path', async () => {
    setup('en');
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/');
    const { ProjectsPage } = await import('../src/app/pages/projects');
    const { ResultsPage } = await import('../src/app/pages/results');
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ProjectsPage);
    await harness.navigateByUrl('/results');
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ResultsPage);
    await harness.navigateByUrl('/nowhere');
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ProjectsPage);
  });
});

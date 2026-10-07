import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LANG, SITE_ROOT, TRANSLATE, themeSignal } from './context';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="top">
      <div class="wrap top-inner">
        <a class="back" [href]="siteRoot + lang + '/project-angular.html'">{{ t('back') }}</a>
        <div class="top-actions">
          <a class="btn" [href]="'?lang=' + other + '#' + fragment()" [attr.hreflang]="other" [attr.lang]="other" [attr.aria-label]="t('lang.switchLabel')">
            {{ t('lang.switch') }}
          </a>
          <button type="button" class="btn" (click)="theme.toggle()">
            {{ theme.theme() === 'dark' ? t('theme.toLight') : t('theme.toDark') }}
          </button>
        </div>
      </div>
    </header>
    <main class="wrap">
      <h1>{{ t('angular.title') }}</h1>
      <p class="lead">{{ t('angular.lead') }}</p>
      <p><a [href]="'../dashboard/?lang=' + lang">{{ t('angular.react') }}</a></p>
      <nav class="tabs" [attr.aria-label]="t('nav.label')">
        <a routerLink="/projects" routerLinkActive="active" ariaCurrentWhenActive="page">{{ t('nav.projects') }}</a>
        <a routerLink="/results" routerLinkActive="active" ariaCurrentWhenActive="page">{{ t('nav.results') }}</a>
      </nav>
      <router-outlet />
    </main>
  `,
})
export class App {
  protected readonly t = inject(TRANSLATE);
  protected readonly lang = inject(LANG);
  protected readonly other = this.lang === 'fr' ? 'en' : 'fr';
  protected readonly siteRoot = inject(SITE_ROOT);
  protected readonly theme = themeSignal();
  /** The current route, kept by the language switch: "/results". */
  protected fragment(): string {
    return window.location.hash.replace(/^#/, '') || '/projects';
  }

  constructor() {
    effect(() => {
      document.documentElement.lang = this.lang;
      document.title = `${this.t('angular.title')} · Charles Lepaire`;
    });
  }
}

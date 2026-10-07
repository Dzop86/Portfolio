import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { API_BASE, LANG, SITE_ROOT, TRANSLATE } from '../context';
import { filterProjects, projectSummary, type ProjectsFile, type Status, type StatusFilter } from '../shared';
import { LoadState } from './load-state';

@Component({
  selector: 'app-projects',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoadState],
  template: `
    <app-load-state [loading]="data.isLoading()" [error]="data.error()" [url]="url" (retry)="data.reload()" />
    @if (data.hasValue()) {
      <section aria-labelledby="h-projects">
        <h2 id="h-projects">{{ t('nav.projects') }}</h2>
        <p>{{ t('projects.summary', summary()) }}</p>
        <progress class="meter" [max]="summary().points" [value]="summary().pointsDone" [attr.aria-label]="t('projects.summary', summary())"></progress>
        <form class="filters" [attr.aria-label]="t('projects.filters')" (submit)="$event.preventDefault()">
          <label for="f-status">{{ t('projects.status') }}</label>
          <select id="f-status" (change)="status.set($any($event.target).value)">
            <option value="all" [selected]="status() === 'all'">{{ t('projects.all') }}</option>
            @for (s of statuses; track s) {
              <option [value]="s" [selected]="s === status()">{{ statusLabel(s) }}</option>
            }
          </select>
          <label for="f-tech">{{ t('projects.tech') }}</label>
          <select id="f-tech" (change)="tech.set($any($event.target).value)">
            <option value="" [selected]="tech() === ''">{{ t('projects.allTechs') }}</option>
            @for (x of data.value().techs; track x) {
              <option [value]="x" [selected]="x === tech()">{{ x }}</option>
            }
          </select>
        </form>
        <p class="muted" role="status">{{ shown().length ? t('projects.count', { n: shown().length }) : t('projects.none') }}</p>
        <ul class="cards">
          @for (p of shown(); track p.id) {
            <li class="card" [attr.data-project]="p.id">
              <h3>{{ p.name[lang] }}</h3>
              <p class="badges">
                <span [class]="'badge status-' + p.status">{{ statusLabel(p.status) }}</span>
                <span class="muted">{{ t('projects.sprint', { sprint: p.sprint }) }}</span>
                <span class="muted">{{ t('projects.points', { n: p.points }) }}</span>
              </p>
              <p>{{ p.pitch[lang] }}</p>
              <p class="tags">
                @for (s of p.stack; track s) {
                  <span class="tag">{{ s }}</span>
                }
              </p>
              <p class="links">
                <a [href]="siteRoot + p.page[lang]">{{ t('projects.page') }}</a>
                @if (p.code) {
                  <a [href]="p.code">{{ t('projects.code') }}</a>
                }
              </p>
            </li>
          }
        </ul>
      </section>
    }
  `,
})
export class ProjectsPage {
  protected readonly t = inject(TRANSLATE);
  protected readonly lang = inject(LANG);
  protected readonly siteRoot = inject(SITE_ROOT);
  protected readonly url = `${inject(API_BASE)}projects.json`;
  protected readonly data = httpResource<ProjectsFile>(() => this.url);
  protected readonly statuses: Status[] = ['done', 'in-progress', 'planned'];
  protected readonly status = signal<StatusFilter>('all');
  protected readonly tech = signal('');
  protected readonly summary = computed(() => projectSummary(this.data.hasValue() ? this.data.value().projects : []));
  protected statusLabel(s: Status): string {
    return this.t(`status.${s}`);
  }
  protected readonly shown = computed(() =>
    this.data.hasValue() ? filterProjects(this.data.value().projects, this.status(), this.tech()) : [],
  );
}

// Loading and error around one API file, as the React dashboard shows them.
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TRANSLATE } from '../context';

@Component({
  selector: 'app-load-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p role="status" class="muted">{{ t('loading') }}</p>
    }
    @if (error(); as e) {
      <div role="alert" class="notice">
        <h2>{{ t('error.title') }}</h2>
        <p>{{ t('error.text', { url: url(), status: status(e) }) }}</p>
        <button type="button" class="btn" (click)="retry.emit()">{{ t('error.retry') }}</button>
      </div>
    }
  `,
})
export class LoadState {
  readonly loading = input.required<boolean>();
  readonly error = input.required<unknown>();
  readonly url = input.required<string>();
  readonly retry = output<void>();
  protected readonly t = inject(TRANSLATE);
  protected status(e: unknown): string | number {
    return e instanceof HttpErrorResponse && e.status > 0 ? e.status : this.t('error.network');
  }
}

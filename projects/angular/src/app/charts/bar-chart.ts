// Horizontal bars, one hue: the label on the left, the value written at the end of each bar (as React's).
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface Bar {
  id: string;
  label: string;
  value: number;
  text: string;
}

const W = 720;
const LEFT = 230;
const RIGHT = 150;
const ROW = 38;
const TOP = 8;

@Component({
  selector: 'app-bar-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart-wrap" tabindex="0" role="region" [attr.aria-labelledby]="labelledBy()">
      <svg class="chart" [attr.viewBox]="'0 0 720 ' + height()" role="img" [attr.aria-labelledby]="labelledBy()">
        @for (b of rows(); track b.id) {
          <g class="chart-hit" [attr.data-bar]="b.id">
            <title>{{ b.label }} : {{ b.text }}</title>
            <text class="chart-tick" [attr.x]="left - 10" [attr.y]="b.y + 23" text-anchor="end">{{ b.label }}</text>
            <rect class="chart-bar" [attr.x]="left" [attr.y]="b.y + 9" [attr.width]="b.w" height="20" rx="4" />
            <text class="chart-label" [attr.x]="left + b.w + 8" [attr.y]="b.y + 23">{{ b.text }}</text>
          </g>
        }
      </svg>
    </div>
  `,
})
export class BarChart {
  readonly bars = input.required<Bar[]>();
  readonly labelledBy = input.required<string>();
  protected readonly left = LEFT;
  protected readonly height = computed(() => TOP * 2 + ROW * this.bars().length);
  protected readonly rows = computed(() => {
    const max = Math.max(...this.bars().map((b) => b.value), Number.MIN_VALUE);
    return this.bars().map((b, i) => ({ ...b, y: TOP + i * ROW, w: Math.max(2, ((W - LEFT - RIGHT) * b.value) / max) }));
  });
}

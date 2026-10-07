// Lines over x, at most three series told apart by stroke as well as colour, each named at its end.
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { linearScale, logScale, type Scale } from '../shared';

export interface Series {
  id: string;
  label: string;
  points: { x: number; y: number }[];
}

const W = 720;
const H = 320;
const M = { left: 64, right: 120, top: 16, bottom: 48 };

@Component({
  selector: 'app-line-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (plot(); as p) {
      <div class="chart-wrap" tabindex="0" role="region" [attr.aria-labelledby]="labelledBy()">
        <svg class="chart" viewBox="0 0 720 320" role="img" [attr.aria-labelledby]="labelledBy()">
          @for (t of p.yTicks; track t.value) {
            <line class="chart-grid" [attr.x1]="m.left" [attr.x2]="w - m.right" [attr.y1]="t.at" [attr.y2]="t.at" />
            <text class="chart-tick" [attr.x]="m.left - 8" [attr.y]="t.at + 4" text-anchor="end">{{ t.text }}</text>
          }
          @for (t of p.xTicks; track t.value) {
            <text class="chart-tick" [attr.x]="t.at" [attr.y]="h - m.bottom + 18" text-anchor="middle">{{ t.text }}</text>
          }
          <text class="chart-axis" [attr.x]="(m.left + w - m.right) / 2" [attr.y]="h - 8" text-anchor="middle">{{ xLabel() }}</text>
          <text class="chart-axis" x="14" [attr.y]="(m.top + h - m.bottom) / 2" text-anchor="middle"
            [attr.transform]="'rotate(-90 14 ' + (m.top + h - m.bottom) / 2 + ')'">{{ yLabel() }}</text>
          @for (s of p.lines; track s.id; let i = $index) {
            <g [class]="'chart-series series-' + (i % 3)" [attr.data-series]="s.id">
              <title>{{ s.label }}</title>
              <polyline class="chart-line" [attr.points]="s.path" />
              @for (d of s.dots; track d.key) {
                <circle class="chart-dot" [attr.cx]="d.x" [attr.cy]="d.y" r="3.5"><title>{{ d.title }}</title></circle>
              }
            </g>
          }
          @for (e of p.ends; track e.id) {
            <text class="chart-label" [attr.x]="e.x + 8" [attr.y]="e.y + 4">{{ e.label }}</text>
          }
        </svg>
      </div>
    }
  `,
})
export class LineChart {
  readonly series = input.required<Series[]>();
  readonly labelledBy = input.required<string>();
  readonly xLabel = input.required<string>();
  readonly yLabel = input.required<string>();
  readonly formatX = input.required<(x: number) => string>();
  readonly formatY = input.required<(y: number) => string>();
  readonly log = input(false);
  readonly zero = input(false);
  protected readonly w = W;
  protected readonly h = H;
  protected readonly m = M;

  protected readonly plot = computed(() => {
    const series = this.series();
    const all = series.flatMap((s) => s.points);
    if (all.length === 0) return null;
    const xs = all.map((p) => p.x);
    const ys = all.map((p) => p.y);
    const log = this.log();
    const make = (min: number, max: number, from: number, to: number): Scale =>
      log ? logScale(min, max, from, to) : linearScale(min, max, from, to);
    const x = make(Math.min(...xs), Math.max(...xs), M.left, W - M.right);
    const y = make(this.zero() && !log ? 0 : Math.min(...ys), Math.max(...ys), H - M.bottom, M.top);
    const fx = this.formatX();
    const fy = this.formatY();
    const lines = series.map((s) => ({
      id: s.id,
      label: s.label,
      path: s.points.map((p) => `${x(p.x)},${y(p.y)}`).join(' '),
      dots: s.points.map((p) => ({ key: p.x, x: x(p.x), y: y(p.y), title: `${s.label} : ${fx(p.x)} → ${fy(p.y)}` })),
    }));
    // Direct labels at the line ends, pushed apart when two would overlap.
    const ends = series
      .flatMap((s) => {
        const end = s.points[s.points.length - 1];
        return end ? [{ id: s.id, label: s.label, x: x(end.x), y: y(end.y) }] : [];
      })
      .sort((a, b) => a.y - b.y);
    for (let i = 1; i < ends.length; i++) {
      if (ends[i]!.y - ends[i - 1]!.y < 14) ends[i]!.y = ends[i - 1]!.y + 14;
    }
    return {
      xTicks: x.ticks.map((v) => ({ value: v, at: x(v), text: fx(v) })),
      yTicks: y.ticks.map((v) => ({ value: v, at: y(v), text: fy(v) })),
      lines,
      ends,
    };
  });
}

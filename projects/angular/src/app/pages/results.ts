import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal } from '@angular/core';
import { BarChart } from '../charts/bar-chart';
import { LineChart } from '../charts/line-chart';
import { API_BASE, LANG, TRANSLATE } from '../context';
import {
  distinct, formatMs, formatNumber, formatPercent, meshIoSeries, parallelAtLargest,
  type Key, type MeshIoFile, type MlFile, type ParallelBenchFile,
} from '../shared';
import { LoadState } from './load-state';

@Component({
  selector: 'app-results',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarChart, LineChart, LoadState],
  template: `
    <section aria-labelledby="h-results">
      <h2 id="h-results">{{ t('nav.results') }}</h2>

      <app-load-state [loading]="bench.isLoading()" [error]="bench.error()" [url]="base + 'parallel-bench.json'" (retry)="bench.reload()" />
      @if (parallel(); as p) {
        <figure class="chart-figure">
          <h3 id="h-parallel">{{ t('results.parallel', { size: n(p.size) }) }}</h3>
          <p class="muted">{{ t('results.parallelLead', { runs: p.runs, cpu: p.cpu, gpu: p.gpu }) }}</p>
          <app-bar-chart [bars]="p.bars" labelledBy="h-parallel" />
          <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-parallel">
            <table data-table="parallel">
              <thead>
                <tr>
                  <th scope="col">{{ t('results.version') }}</th>
                  <th scope="col" class="num">{{ t('results.time') }}</th>
                  <th scope="col" class="num">{{ t('results.speedup') }}</th>
                  <th scope="col" class="num">{{ t('results.error') }}</th>
                </tr>
              </thead>
              <tbody>
                @for (r of p.rows; track r.backend) {
                  <tr>
                    <th scope="row">{{ r.label }}</th>
                    <td class="num">{{ r.ms }} ms</td>
                    <td class="num">×{{ r.speedup }}</td>
                    <td class="num">{{ r.error }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </figure>
      }

      <app-load-state [loading]="io.isLoading()" [error]="io.error()" [url]="base + 'mesh-io.json'" (retry)="io.reload()" />
      @if (io.hasValue()) {
        <figure class="chart-figure">
          <h3 id="h-meshio">{{ t('results.meshIo') }}</h3>
          <p class="muted">{{ t('results.meshIoLead', meshLead()) }}</p>
          <form class="filters" [attr.aria-label]="t('results.meshIo')" (submit)="$event.preventDefault()">
            <label for="f-library">{{ t('results.library') }}</label>
            <select id="f-library" (change)="library.set($any($event.target).value)">
              @for (l of libraries(); track l) {
                <option [value]="l" [selected]="l === library()">{{ l }}</option>
              }
            </select>
            <label for="f-family">{{ t('results.family') }}</label>
            <select id="f-family" (change)="family.set($any($event.target).value)">
              @for (f of families(); track f) {
                <option [value]="f" [selected]="f === family()">{{ familyName(f) }}</option>
              }
            </select>
          </form>
          <app-line-chart
            [series]="lines()"
            labelledBy="h-meshio"
            [log]="true"
            [xLabel]="t('results.triangles')"
            [yLabel]="t('results.ms')"
            [formatX]="fx"
            [formatY]="fy"
          />
          <div class="table-wrap" tabindex="0" role="region" aria-labelledby="h-meshio">
            <table data-table="meshio">
              <thead>
                <tr>
                  <th scope="col">{{ t('results.triangles') }}</th>
                  @for (s of lines(); track s.id) {
                    <th scope="col" class="num">{{ s.label }}</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of table(); track row.size) {
                  <tr>
                    <th scope="row">{{ n(row.size) }}</th>
                    @for (c of row.cells; track $index) {
                      <td class="num">{{ c }}</td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </figure>
      }

      <app-load-state [loading]="ml.isLoading()" [error]="ml.error()" [url]="base + 'ml.json'" (retry)="ml.reload()" />
      @if (ml.hasValue()) {
        <figure class="chart-figure">
          <h3 id="h-ml">{{ t('results.ml') }}</h3>
          <p>
            {{ t('results.mlText', mlText()) }}
            <strong class="stat" data-stat="accuracy">{{ accuracy() }}</strong>
          </p>
          <progress class="meter" max="1" [value]="ml.value().test_accuracy" aria-labelledby="h-ml"></progress>
          <p class="muted">{{ t('results.mlOnnx', { gap: ml.value().onnx_max_logit_gap.toExponential(1) }) }}</p>
        </figure>
      }
    </section>
  `,
})
export class ResultsPage {
  protected readonly t = inject(TRANSLATE);
  protected readonly lang = inject(LANG);
  protected readonly base = inject(API_BASE);
  protected readonly bench = httpResource<ParallelBenchFile>(() => `${this.base}parallel-bench.json`);
  protected readonly io = httpResource<MeshIoFile>(() => `${this.base}mesh-io.json`);
  protected readonly ml = httpResource<MlFile>(() => `${this.base}ml.json`);

  protected n = (x: number, digits = 0) => formatNumber(x, this.lang, digits);
  protected readonly fx = (x: number) => formatNumber(x, this.lang);
  protected readonly fy = (y: number) => formatNumber(y, this.lang, y < 1 ? 1 : 0);
  protected familyName(f: string): string {
    return this.t(`family.${f}` as Key);
  }

  protected readonly parallel = computed(() => {
    if (!this.bench.hasValue()) return null;
    const bench = this.bench.value();
    const { size, rows } = parallelAtLargest(bench);
    const label = (b: string) => this.t(`backend.${b}` as Key, { threads: bench.machine.openmp_threads });
    return {
      size,
      runs: bench.runs,
      cpu: bench.machine.cpu,
      gpu: bench.machine.cuda || '–',
      bars: rows.map((r) => ({
        id: r.backend,
        label: label(r.backend),
        value: r.ms,
        text: this.t('results.bar', { ms: formatMs(r.ms, this.lang), speedup: formatNumber(r.speedup, this.lang, 1) }),
      })),
      rows: rows.map((r) => ({
        backend: r.backend,
        label: label(r.backend),
        ms: formatMs(r.ms, this.lang),
        speedup: formatNumber(r.speedup, this.lang, 1),
        error: r.error === 0 ? '0' : r.error.toExponential(1),
      })),
    };
  });

  protected readonly libraries = computed(() => (this.io.hasValue() ? distinct(this.io.value().results.map((r) => r.implementation)).sort() : []));
  protected readonly families = computed(() => (this.io.hasValue() ? distinct(this.io.value().results.map((r) => r.family)).sort() : []));
  // The choices follow the data once it arrives, then the visitor.
  protected readonly library = linkedSignal(() => this.libraries()[0] ?? '');
  protected readonly family = linkedSignal(() => (this.families().includes('torus') ? 'torus' : (this.families()[0] ?? '')));
  protected readonly meshLead = computed((): Record<string, string | number> => {
    const io = this.io.value();
    return io ? { runs: io.results[0]?.repetitions ?? 0, runtime: io.machine.runtime, cpu: io.machine.cpu } : {};
  });
  protected readonly lines = computed(() =>
    this.io.hasValue()
      ? meshIoSeries(this.io.value(), this.library(), this.family()).map((s) => ({ id: s.format, label: s.format.toUpperCase(), points: s.points }))
      : [],
  );
  protected readonly table = computed(() => {
    const lines = this.lines();
    const sizes = distinct(lines.flatMap((s) => s.points.map((p) => p.x))).sort((a, b) => a - b);
    return sizes.map((size) => ({
      size,
      cells: lines.map((s) => {
        const p = s.points.find((q) => q.x === size);
        return p ? `${formatMs(p.y, this.lang)} ms` : '–';
      }),
    }));
  });

  protected readonly mlText = computed((): Record<string, string | number> => {
    const ml = this.ml.value();
    if (!ml) return {};
    const shape = (c: string) => this.t(`shape.${c}` as Key);
    return { model: ml.model, points: ml.points, classes: ml.classes.length, list: ml.classes.map(shape).join(', ') };
  });
  protected readonly accuracy = computed(() => (this.ml.hasValue() ? formatPercent(this.ml.value().test_accuracy, this.lang) : ''));
}

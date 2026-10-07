import { useId, useState } from 'react';
import type { Api, Lang } from '../api';
import { BarChart } from '../charts/BarChart';
import { LineChart } from '../charts/LineChart';
import { DICTIONARY, formatMs, formatNumber, formatPercent, type Key, type T } from '../i18n';
import { distinct, meshIoSeries, parallelAtLargest } from '../model';

type Props = { data: Pick<Api, 'parallelBench' | 'meshIo' | 'ml'>; lang: Lang; t: T };

export function ResultsView({ data, lang, t }: Props) {
  return (
    <section aria-labelledby="h-results">
      <h2 id="h-results">{t('nav.results')}</h2>
      <ParallelResults data={data} lang={lang} t={t} />
      <MeshIoResults data={data} lang={lang} t={t} />
      <MlResults data={data} lang={lang} t={t} />
    </section>
  );
}

function ParallelResults({ data, lang, t }: Props) {
  const bench = data.parallelBench;
  const { size, rows } = parallelAtLargest(bench);
  const label = (b: string) => t(`backend.${b}` as Key, { threads: bench.machine.openmp_threads });
  const bars = rows.map((r) => ({
    id: r.backend,
    label: label(r.backend),
    value: r.ms,
    text: t('results.bar', { ms: formatMs(r.ms, lang), speedup: formatNumber(r.speedup, lang, 1) }),
  }));
  return (
    <figure className="chart-figure">
      <h3 id="h-parallel">{t('results.parallel', { size: formatNumber(size, lang) })}</h3>
      <p className="muted">
        {t('results.parallelLead', { runs: bench.runs, cpu: bench.machine.cpu, gpu: bench.machine.cuda || '–' })}
      </p>
      <BarChart bars={bars} labelledBy="h-parallel" />
      <div className="table-wrap" tabIndex={0} role="region" aria-labelledby="h-parallel">
        <table data-table="parallel">
          <thead>
            <tr>
              <th scope="col">{t('results.version')}</th>
              <th scope="col" className="num">{t('results.time')}</th>
              <th scope="col" className="num">{t('results.speedup')}</th>
              <th scope="col" className="num">{t('results.error')}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.backend}>
                <th scope="row">{label(r.backend)}</th>
                <td className="num">{formatMs(r.ms, lang)} ms</td>
                <td className="num">×{formatNumber(r.speedup, lang, 1)}</td>
                <td className="num">{r.error === 0 ? '0' : r.error.toExponential(1)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </figure>
  );
}

function MeshIoResults({ data, lang, t }: Props) {
  const io = data.meshIo;
  const libraries = distinct(io.results.map((r) => r.implementation)).sort();
  const families = distinct(io.results.map((r) => r.family)).sort();
  const [library, setLibrary] = useState(libraries[0] ?? '');
  const [family, setFamily] = useState(families.includes('torus') ? 'torus' : (families[0] ?? ''));
  const libraryId = useId();
  const familyId = useId();
  const series = meshIoSeries(io, library, family);
  const familyName = (f: string) => t(`family.${f}` as Key);
  const sizes = distinct(series.flatMap((s) => s.points.map((p) => p.x))).sort((a, b) => a - b);

  return (
    <figure className="chart-figure">
      <h3 id="h-meshio">{t('results.meshIo')}</h3>
      <p className="muted">
        {t('results.meshIoLead', { runs: io.results[0]?.repetitions ?? 0, runtime: io.machine.runtime, cpu: io.machine.cpu })}
      </p>
      <form className="filters" aria-label={t('results.meshIo')} onSubmit={(e) => e.preventDefault()}>
        <label htmlFor={libraryId}>{t('results.library')}</label>
        <select id={libraryId} value={library} onChange={(e) => setLibrary(e.target.value)}>
          {libraries.map((l) => (
            <option key={l} value={l}>
              {l}
            </option>
          ))}
        </select>
        <label htmlFor={familyId}>{t('results.family')}</label>
        <select id={familyId} value={family} onChange={(e) => setFamily(e.target.value)}>
          {families.map((f) => (
            <option key={f} value={f}>
              {familyName(f)}
            </option>
          ))}
        </select>
      </form>
      <LineChart
        labelledBy="h-meshio"
        log
        xLabel={t('results.triangles')}
        yLabel={t('results.ms')}
        formatX={(x) => formatNumber(x, lang)}
        formatY={(y) => formatNumber(y, lang, y < 1 ? 1 : 0)}
        series={series.map((s) => ({ id: s.format, label: s.format.toUpperCase(), points: s.points }))}
      />
      <div className="table-wrap" tabIndex={0} role="region" aria-labelledby="h-meshio">
        <table data-table="meshio">
          <thead>
            <tr>
              <th scope="col">{t('results.triangles')}</th>
              {series.map((s) => (
                <th key={s.format} scope="col" className="num">
                  {s.format.toUpperCase()}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sizes.map((size) => (
              <tr key={size}>
                <th scope="row">{formatNumber(size, lang)}</th>
                {series.map((s) => {
                  const p = s.points.find((q) => q.x === size);
                  return (
                    <td key={s.format} className="num">
                      {p ? `${formatMs(p.y, lang)} ms` : '–'}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </figure>
  );
}

function MlResults({ data, lang, t }: Props) {
  const ml = data.ml;
  return (
    <figure className="chart-figure">
      <h3 id="h-ml">{t('results.ml')}</h3>
      <p>
        {t('results.mlText', { model: ml.model, points: ml.points, classes: ml.classes.length, list: ml.classes.map((c) => (`shape.${c}` in DICTIONARY[lang] ? t(`shape.${c}` as Key) : c)).join(', ') })}{' '}
        <strong className="stat" data-stat="accuracy">
          {formatPercent(ml.test_accuracy, lang)}
        </strong>
      </p>
      <progress className="meter" max={1} value={ml.test_accuracy} aria-labelledby="h-ml" />
      <p className="muted">{t('results.mlOnnx', { gap: ml.onnx_max_logit_gap.toExponential(1) })}</p>
    </figure>
  );
}

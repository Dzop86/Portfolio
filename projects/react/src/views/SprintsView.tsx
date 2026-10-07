import type { Lang, SprintsFile } from '../api';
import { LineChart } from '../charts/LineChart';
import { formatNumber, type T } from '../i18n';
import { meanVelocity } from '../model';

export function SprintsView({ data, lang, t }: { data: SprintsFile; lang: Lang; t: T }) {
  const mean = formatNumber(meanVelocity(data), lang, 1);
  const n = (x: number) => formatNumber(x, lang);
  const latest = [...data.sprints].sort((a, b) => b.number - a.number).slice(0, 3);
  return (
    <section aria-labelledby="h-sprints">
      <h2 id="h-sprints">{t('nav.sprints')}</h2>
      <p>{t('sprints.summary', { done: data.done, count: data.sprintCount, mean })}</p>

      <figure className="chart-figure">
        <h3 id="h-velocity">{t('sprints.velocity')}</h3>
        <LineChart
          labelledBy="h-velocity"
          zero
          xLabel={t('sprints.sprint')}
          yLabel={t('sprints.points')}
          formatX={n}
          formatY={n}
          series={[
            { id: 'committed', label: t('sprints.committed'), points: data.velocity.map((v) => ({ x: v.number, y: v.committed })) },
            // Delivered points only for the finished sprints: the one in progress would drop to 0.
            { id: 'done', label: t('sprints.delivered'), points: data.velocity.filter((v) => v.number <= data.done).map((v) => ({ x: v.number, y: v.done })) },
          ]}
        />
      </figure>
      <div className="table-wrap" tabIndex={0} role="region" aria-labelledby="h-velocity">
        <table data-table="velocity">
          <thead>
            <tr>
              <th scope="col">{t('sprints.sprint')}</th>
              <th scope="col" className="num">{t('sprints.committed')}</th>
              <th scope="col" className="num">{t('sprints.delivered')}</th>
            </tr>
          </thead>
          <tbody>
            {data.velocity.map((v) => (
              <tr key={v.number}>
                <th scope="row">{v.number}</th>
                <td className="num">{v.committed}</td>
                <td className="num">{v.number <= data.done ? v.done : t('sprints.open')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <figure className="chart-figure">
        <h3 id="h-burndown">{t('sprints.burndown')}</h3>
        <LineChart
          labelledBy="h-burndown"
          zero
          xLabel={t('sprints.sprint')}
          yLabel={t('sprints.remaining')}
          formatX={n}
          formatY={n}
          series={[{ id: 'remaining', label: t('sprints.remaining'), points: data.burndown.remaining.map((y, x) => ({ x, y })) }]}
        />
      </figure>

      <h3>{t('sprints.latest')}</h3>
      <dl className="goals">
        {latest.map((s) => (
          <div key={s.number}>
            <dt>{t('sprints.goal', { n: s.number })}</dt>
            <dd>{s.goal[lang]}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

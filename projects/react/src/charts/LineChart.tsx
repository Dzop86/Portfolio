// Lines over x, at most three series told apart by stroke (solid, dashed, dotted) as well as colour,
// each named at its end: no legend to decode.
import { linearScale, logScale, type Scale } from './scale';

export interface Series {
  id: string;
  label: string;
  points: { x: number; y: number }[];
}

interface Props {
  series: Series[];
  labelledBy: string;
  xLabel: string;
  yLabel: string;
  formatX: (x: number) => string;
  formatY: (y: number) => string;
  log?: boolean;
  /** Start the y axis at 0 (linear scales only). */
  zero?: boolean;
}

const W = 720;
const H = 320;
const M = { left: 64, right: 120, top: 16, bottom: 48 };

export function LineChart({ series, labelledBy, xLabel, yLabel, formatX, formatY, log = false, zero = false }: Props) {
  const all = series.flatMap((s) => s.points);
  const xs = all.map((p) => p.x);
  const ys = all.map((p) => p.y);
  if (all.length === 0) return null;
  const make = (min: number, max: number, from: number, to: number): Scale =>
    log ? logScale(min, max, from, to) : linearScale(min, max, from, to);
  const x = make(Math.min(...xs), Math.max(...xs), M.left, W - M.right);
  const y = make(zero && !log ? 0 : Math.min(...ys), Math.max(...ys), H - M.bottom, M.top);
  // Direct labels at the line ends, pushed apart when two would overlap.
  const ends = series
    .map((s) => ({ s, end: s.points[s.points.length - 1] }))
    .filter((e): e is { s: Series; end: { x: number; y: number } } => e.end !== undefined)
    .map(({ s, end }) => ({ id: s.id, label: s.label, x: x(end.x), y: y(end.y) }))
    .sort((a, b) => a.y - b.y);
  for (let i = 1; i < ends.length; i++) {
    const prev = ends[i - 1]!;
    const cur = ends[i]!;
    if (cur.y - prev.y < 14) cur.y = prev.y + 14;
  }

  return (
    <div className="chart-wrap" tabIndex={0} role="region" aria-labelledby={labelledBy}>
      <svg className="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-labelledby={labelledBy}>
        {y.ticks.map((t) => (
          <g key={`y${t}`}>
            <line className="chart-grid" x1={M.left} x2={W - M.right} y1={y(t)} y2={y(t)} />
            <text className="chart-tick" x={M.left - 8} y={y(t) + 4} textAnchor="end">
              {formatY(t)}
            </text>
          </g>
        ))}
        {x.ticks.map((t) => (
          <text key={`x${t}`} className="chart-tick" x={x(t)} y={H - M.bottom + 18} textAnchor="middle">
            {formatX(t)}
          </text>
        ))}
        <text className="chart-axis" x={(M.left + W - M.right) / 2} y={H - 8} textAnchor="middle">
          {xLabel}
        </text>
        <text className="chart-axis" x={14} y={(M.top + H - M.bottom) / 2} textAnchor="middle" transform={`rotate(-90 14 ${(M.top + H - M.bottom) / 2})`}>
          {yLabel}
        </text>
        {series.map((s, i) => (
          <g key={s.id} className={`chart-series series-${i % 3}`} data-series={s.id}>
            <title>{s.label}</title>
            <polyline className="chart-line" points={s.points.map((p) => `${x(p.x)},${y(p.y)}`).join(' ')} />
            {s.points.map((p) => (
              <circle key={`${p.x}`} className="chart-dot" cx={x(p.x)} cy={y(p.y)} r={3.5}>
                <title>{`${s.label} : ${formatX(p.x)} → ${formatY(p.y)}`}</title>
              </circle>
            ))}
          </g>
        ))}
        {ends.map((e) => (
          <text key={`end-${e.id}`} className="chart-label" x={e.x + 8} y={e.y + 4}>
            {e.label}
          </text>
        ))}
      </svg>
    </div>
  );
}

// Horizontal bars, one hue: the label on the left, the value written at the end of each bar.

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

export function BarChart({ bars, labelledBy }: { bars: Bar[]; labelledBy: string }) {
  const height = TOP * 2 + ROW * bars.length;
  const max = Math.max(...bars.map((b) => b.value), Number.MIN_VALUE);
  return (
    <div className="chart-wrap" tabIndex={0} role="region" aria-labelledby={labelledBy}>
      <svg className="chart" viewBox={`0 0 ${W} ${height}`} role="img" aria-labelledby={labelledBy}>
        {bars.map((b, i) => {
          const w = Math.max(2, ((W - LEFT - RIGHT) * b.value) / max);
          const y = TOP + i * ROW;
          return (
            <g key={b.id} className="chart-hit" data-bar={b.id}>
              <title>{`${b.label} : ${b.text}`}</title>
              <text className="chart-tick" x={LEFT - 10} y={y + ROW / 2 + 4} textAnchor="end">
                {b.label}
              </text>
              <rect className="chart-bar" x={LEFT} y={y + 9} width={w} height={ROW - 18} rx={4} />
              <text className="chart-label" x={LEFT + w + 8} y={y + ROW / 2 + 4}>
                {b.text}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}

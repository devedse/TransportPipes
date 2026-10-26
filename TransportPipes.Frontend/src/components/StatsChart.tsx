import { useEffect, useMemo, useRef, useState, type PointerEvent } from "react";
import type { MomentDto, StatsRange } from "../api/contracts";
import { formatAmount } from "../app/format";
import {
  clusterMoments,
  logTicks,
  momentIcon,
  niceTicks,
  stack,
  time,
  timeTicks,
  whenLabel,
  type Point,
  type Series,
} from "../app/statistics";

const MARGIN = { left: 58, right: 14, top: 12, bottom: 24 };
/** The lane of moment flags under the plot */
const LANE = 24;

export interface StatsChartProps {
  series: Series[];
  stacked: boolean;
  steps?: boolean;
  /** Other companies' lines (compare) */
  compare?: Series[];
  from: number;
  to: number;
  founded: number;
  range: StatsRange;
  log: boolean;
  format: (value: number) => string;
  moments: MomentDto[];
  away: { from: number; to: number }[];
  /** A moment to point at (from the journal) */
  highlight: number | null;
  height?: number;
  /** The hover's time (default: the clock and the company's day) */
  timeLabel?: (t: number) => string;
}

/** The parts of a line between its gaps, as SVG path data */
function linePath(
  points: Point[],
  x: (t: number) => number,
  y: (v: number) => number,
  steps: boolean,
): string {
  let d = "";
  let open = false;
  for (const p of points) {
    if (p.v === null) {
      open = false;
      continue;
    }
    const px = x(p.t);
    const py = y(p.v);
    if (!open) {
      d += `M${px.toFixed(1)},${py.toFixed(1)}`;
      open = true;
    } else if (steps) {
      d += `H${px.toFixed(1)}V${py.toFixed(1)}`;
    } else {
      d += `L${px.toFixed(1)},${py.toFixed(1)}`;
    }
  }
  return d;
}

/** A band between two lines (a stacked part, or the area under a line) */
function bandPath(low: Point[], high: Point[], x: (t: number) => number, y: (v: number) => number): string {
  const top: string[] = [];
  const bottom: string[] = [];
  let d = "";
  const flush = () => {
    if (top.length > 1) {
      d += `M${top.join("L")}L${bottom.reverse().join("L")}Z`;
    }
    top.length = 0;
    bottom.length = 0;
  };
  high.forEach((p, i) => {
    const l = low[i];
    if (p.v === null || l?.v == null) {
      flush();
      return;
    }
    top.push(`${x(p.t).toFixed(1)},${y(p.v).toFixed(1)}`);
    bottom.push(`${x(p.t).toFixed(1)},${y(l.v).toFixed(1)}`);
  });
  flush();
  return d;
}

/** A graph of a company's statistics: its lines or stacked parts, the stretches it was away, its key moments */
export function StatsChart(props: StatsChartProps) {
  const {
    series,
    stacked,
    compare = [],
    from,
    to,
    founded,
    range,
    log,
    format,
    moments,
    away,
    highlight,
  } = props;
  const box = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(640);
  const [hover, setHover] = useState<number | null>(null);
  const height = props.height ?? 260;

  useEffect(() => {
    const element = box.current;
    if (element === null) {
      return;
    }
    const observer = new ResizeObserver(() => setWidth(Math.max(240, element.clientWidth)));
    observer.observe(element);
    setWidth(Math.max(240, element.clientWidth));
    return () => observer.disconnect();
  }, []);

  const plot = {
    left: MARGIN.left,
    right: width - MARGIN.right,
    top: MARGIN.top,
    bottom: height - MARGIN.bottom - LANE,
  };
  const bands = useMemo(() => (stacked ? stack(series) : []), [stacked, series]);

  // The value axis: from zero (or near the lowest value), or a log scale over the positive values
  const values = (stacked ? bands.map((b) => b.high) : [...series, ...compare].map((s) => s.points))
    .flat()
    .map((p) => p.v)
    .filter((v): v is number => v !== null && (!log || v > 0));
  const top = values.length === 0 ? 1 : Math.max(...values);
  const bottomValue = values.length === 0 ? 0 : Math.min(...values);
  const useLog = log && values.length > 0 && top > 0;
  const lo = useLog
    ? bottomValue / 1.3
    : bottomValue > 0 && bottomValue > 0.4 * top && !stacked
      ? bottomValue * 0.95
      : Math.min(0, bottomValue);
  const hi = useLog ? top * 1.3 : top === lo ? lo + 1 : top + (top - lo) * 0.06;
  const ticks = useLog
    ? logTicks(lo, hi)
    : niceTicks(lo, hi, height < 200 ? 3 : 4).filter((t) => t >= lo && t <= hi);
  const span = Math.max(1, to - from);
  const x = (t: number) => plot.left + ((t - from) / span) * (plot.right - plot.left);
  const y = (v: number) => {
    const share = useLog
      ? (Math.log(Math.max(v, lo)) - Math.log(lo)) / (Math.log(hi) - Math.log(lo))
      : (v - lo) / (hi - lo);
    return plot.bottom - share * (plot.bottom - plot.top);
  };
  const shownMoments = moments.filter((m) => time(m.at) >= from && time(m.at) <= to);
  const clusters = clusterMoments(shownMoments, x, 18);
  const times = series[0]?.points.map((p) => p.t) ?? [];

  const onMove = (event: PointerEvent<SVGSVGElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const px = event.clientX - rect.left;
    if (px < plot.left - 8 || px > plot.right + 8) {
      setHover(null);
      return;
    }
    const t = from + ((px - plot.left) / (plot.right - plot.left)) * span;
    setHover(
      times.reduce<number | null>(
        (best, c) => (best === null || Math.abs(c - t) < Math.abs(best - t) ? c : best),
        null,
      ),
    );
  };

  // The moments a hovered point stands for: since the point before it
  const hoverIndex = hover === null ? -1 : times.indexOf(hover);
  const hoverFrom = (hoverIndex > 0 ? times[hoverIndex - 1] : undefined) ?? from;
  const hoverMoments =
    hover === null ? [] : shownMoments.filter((m) => time(m.at) > hoverFrom && time(m.at) <= hover);
  const pointed = highlight !== null && highlight >= from && highlight <= to ? highlight : null;

  return (
    <div className="stats-chart" ref={box}>
      <svg
        width={width}
        height={height}
        role="img"
        aria-label={series.map((s) => s.label).join(", ")}
        onPointerMove={onMove}
        onPointerDown={onMove}
        onPointerLeave={() => setHover(null)}
      >
        <defs>
          <pattern
            id="stats-away"
            width="6"
            height="6"
            patternUnits="userSpaceOnUse"
            patternTransform="rotate(45)"
          >
            <rect width="6" height="6" className="away-fill" />
            <line x1="0" y1="0" x2="0" y2="6" className="away-hatch" />
          </pattern>
        </defs>
        {away.map((s) => {
          const ax = Math.max(plot.left, x(s.from));
          const bx = Math.min(plot.right, x(s.to));
          return bx - ax < 1 ? null : (
            <g key={s.from} className="stats-away">
              <rect
                x={ax}
                y={plot.top}
                width={bx - ax}
                height={plot.bottom - plot.top}
                fill="url(#stats-away)"
              />
              {bx - ax > 44 && (
                <text x={(ax + bx) / 2} y={plot.top + 12} textAnchor="middle">
                  away
                </text>
              )}
            </g>
          );
        })}
        {ticks.map((t) => (
          <g key={t} className="stats-grid">
            <line x1={plot.left} x2={plot.right} y1={y(t)} y2={y(t)} />
            <text x={plot.left - 6} y={y(t) + 4} textAnchor="end">
              {formatAmount(t)}
            </text>
          </g>
        ))}
        {timeTicks(from, to, founded, range).map((t) => (
          <g key={t.t} className="stats-time">
            <line x1={x(t.t)} x2={x(t.t)} y1={plot.top} y2={plot.bottom + LANE} />
            <text x={x(t.t)} y={height - 6} textAnchor="middle">
              {t.label}
            </text>
          </g>
        ))}
        {clusters
          .filter((c) => c.moments.some((m) => m.big))
          .map((c) => (
            <line
              key={`big-${c.x}`}
              className="stats-big-moment"
              x1={c.x}
              x2={c.x}
              y1={plot.top}
              y2={plot.bottom}
            />
          ))}
        {stacked
          ? bands.map((b) => (
              <path
                key={b.series.label}
                className="stats-band"
                d={bandPath(b.low, b.high, x, y)}
                fill={b.series.color}
                stroke={b.series.color}
              />
            ))
          : series.map((s) => (
              <g key={s.label}>
                {!useLog && lo === 0 && (
                  <path
                    className="stats-area"
                    d={bandPath(
                      s.points.map((p) => ({ t: p.t, v: p.v === null ? null : 0 })),
                      s.points,
                      x,
                      y,
                    )}
                    fill={s.color}
                  />
                )}
                <path
                  className={s.dashed ? "stats-line dashed" : "stats-line"}
                  d={linePath(s.points, x, y, props.steps ?? false)}
                  stroke={s.color}
                />
              </g>
            ))}
        {compare.map((s) => (
          <path
            key={s.label}
            className="stats-line compare"
            d={linePath(s.points, x, y, props.steps ?? false)}
            stroke={s.color}
          />
        ))}
        <line className="stats-axis" x1={plot.left} x2={plot.right} y1={plot.bottom} y2={plot.bottom} />
        {clusters.map((c) => (
          <g key={c.x} className={c.moments.some((m) => m.big) ? "stats-flag big" : "stats-flag"}>
            <title>{c.moments.map((m) => m.text).join("\n")}</title>
            <circle cx={c.x} cy={plot.bottom + LANE / 2} r={c.moments.some((m) => m.big) ? 10 : 7} />
            <text x={c.x} y={plot.bottom + LANE / 2 + 4} textAnchor="middle">
              {momentIcon(c.moments.find((m) => m.big) ?? c.moments[0])}
            </text>
            {c.moments.length > 1 && (
              <text className="stats-flag-count" x={c.x + 9} y={plot.bottom + 6}>
                {c.moments.length}
              </text>
            )}
          </g>
        ))}
        {pointed !== null && (
          <line
            className="stats-pointed"
            x1={x(pointed)}
            x2={x(pointed)}
            y1={plot.top}
            y2={plot.bottom + LANE}
          />
        )}
        {hover !== null && (
          <g className="stats-hover">
            <line x1={x(hover)} x2={x(hover)} y1={plot.top} y2={plot.bottom} />
            {(stacked
              ? bands.map((b) => ({ color: b.series.color, points: b.high }))
              : [...series, ...compare]
            ).map((s, i) => {
              const v = s.points.find((p) => p.t === hover)?.v;
              return v == null || (useLog && v <= 0) ? null : (
                <circle key={i} cx={x(hover)} cy={y(v)} r={4} fill={s.color} />
              );
            })}
          </g>
        )}
      </svg>
      {hover !== null && (
        <div
          className={x(hover) > width / 2 ? "stats-tip left" : "stats-tip"}
          style={x(hover) > width / 2 ? { right: width - x(hover) + 10 } : { left: x(hover) + 10 }}
        >
          <div className="small muted">{props.timeLabel?.(hover) ?? whenLabel(hover, founded)}</div>
          {[...series, ...compare].map((s) => {
            const v = s.points.find((p) => p.t === hover)?.v;
            return v == null ? null : (
              <div key={s.label} className="stats-tip-row">
                <span className="stats-swatch" style={{ background: s.color }} />
                {series.length + compare.length > 1 && <span>{s.label}</span>}
                <strong>{format(v)}</strong>
              </div>
            );
          })}
          {hoverMoments.map((m) => (
            <div key={`${m.at}${m.text}`} className={m.big ? "stats-tip-moment big" : "stats-tip-moment"}>
              {momentIcon(m)} {m.text}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

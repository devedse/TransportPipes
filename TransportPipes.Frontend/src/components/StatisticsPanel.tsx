import { useEffect, useMemo, useState } from "react";
import type { MomentDto, PaceDto, StatisticsDto, StatsRange, StatsSeriesDto } from "../api/contracts";
import { gameClient } from "../app/client";
import { formatAmount, possessive } from "../app/format";
import {
  DAY,
  GROUPS,
  PUBLIC,
  RANGE_WORDS,
  awayStretches,
  change,
  dayOf,
  formatValue,
  headline,
  insights,
  metricsOf,
  momentIcon,
  seriesOf,
  stack,
  time,
  wantsLog,
  type Metric,
  type Series,
} from "../app/statistics";
import { setState, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";
import { StatsChart } from "./StatsChart";

const RANGES: { id: StatsRange; name: string }[] = [
  { id: "day", name: "24 hours" },
  { id: "week", name: "7 days" },
  { id: "month", name: "30 days" },
  { id: "all", name: "All" },
];

/** A card's small graph: the line, or the stack's total */
export function Sparkline({ series, stacked }: { series: Series[]; stacked: boolean }) {
  const points = (stacked ? stack(series).at(-1)?.high : series[0]?.points) ?? [];
  const known = points.filter((p): p is { t: number; v: number } => p.v !== null);
  const [t0, t1] = [points[0]?.t ?? 0, points[points.length - 1]?.t ?? 0];
  if (known.length < 2) {
    return <svg className="sparkline" viewBox="0 0 100 28" aria-hidden="true" />;
  }
  const values = known.map((p) => p.v);
  const [lo, hi] = [Math.min(...values), Math.max(...values)];
  const x = (t: number) => ((t - t0) / (t1 - t0 || 1)) * 100;
  // A value that held steady is a line through the middle, not along the bottom (that reads as nothing)
  const y = (v: number) => (hi - lo <= Math.abs(hi) * 0.005 ? 14 : 26 - ((v - lo) / (hi - lo)) * 24);
  const line = known
    .map((p, i) => `${i === 0 ? "M" : "L"}${x(p.t).toFixed(1)},${y(p.v).toFixed(1)}`)
    .join("");
  const area = `${line}L${x(known[known.length - 1]?.t ?? t1).toFixed(1)},28L${x(known[0]?.t ?? t0).toFixed(1)},28Z`;
  return (
    <svg className="sparkline" viewBox="0 0 100 28" preserveAspectRatio="none" aria-hidden="true">
      <path className="sparkline-area" d={area} />
      <path className="sparkline-line" d={line} />
    </svg>
  );
}

/** What a card or the graph's head shows big: a value now, or what a rate added up to */
function bigNumber(metric: Metric, value: number | null): string {
  if (value === null) {
    return "–";
  }
  if (metric.kind === "value") {
    return formatValue(metric, value);
  }
  return `+${formatAmount(value)}${metric.resource ? "" : " g"}`;
}

function Card({
  metric,
  series,
  stats,
  active,
  onPick,
}: {
  metric: Metric;
  series: Series[];
  stats: StatisticsDto;
  active: boolean;
  onPick: () => void;
}) {
  const delta = metric.kind === "value" ? change(series[0]?.points ?? []) : "";
  return (
    <button className={active ? "stats-card active" : "stats-card"} aria-pressed={active} onClick={onPick}>
      <span className="stats-card-name">
        {metric.resource !== undefined && <ResourceIcon resource={metric.resource} />}
        {metric.name}
      </span>
      <span className="stats-card-value">
        <strong>{bigNumber(metric, headline(stats, metric, series))}</strong>
        {delta !== "" && (
          <span className={delta.startsWith("−") ? "stats-change down" : "stats-change"}>{delta}</span>
        )}
      </span>
      <Sparkline series={series} stacked={metric.kind === "stack"} />
    </button>
  );
}

/** The key moments of the range by day, newest first; a click points at one on the graph */
function Journal({ stats, onPick }: { stats: StatisticsDto; onPick: (t: number) => void }) {
  const [bigOnly, setBigOnly] = useState(false);
  const founded = time(stats.founded);
  const moments = stats.moments.filter((m) => !bigOnly || m.big).reverse();
  // By date (the company's days, which run from the hour it was founded, after it)
  const dateOf = (at: string) =>
    new Date(at).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
  const days: { date: string; moments: MomentDto[] }[] = [];
  for (const moment of moments) {
    const date = dateOf(moment.at);
    const open = days[days.length - 1];
    if (open?.date === date) {
      open.moments.push(moment);
    } else {
      days.push({ date, moments: [moment] });
    }
  }
  const companyDays = (group: MomentDto[]) => {
    const all = [...new Set(group.map((m) => dayOf(time(m.at), founded)))].sort((a, b) => a - b);
    return all.length === 1 ? `day ${all[0]}` : `days ${all[0]}–${all[all.length - 1]}`;
  };
  return (
    <section className="stats-journal" aria-label="Journal">
      <header>
        <h4>Journal</h4>
        {stats.own && (
          <div className="tabs compact">
            <button className={bigOnly ? "tab active" : "tab"} onClick={() => setBigOnly(true)}>
              Big moments
            </button>
            <button className={bigOnly ? "tab" : "tab active"} onClick={() => setBigOnly(false)}>
              All
            </button>
          </div>
        )}
      </header>
      {days.length === 0 && <p className="small muted">No moments {RANGE_WORDS[stats.range]}.</p>}
      {days.map((d) => {
        return (
          <div key={d.date} className="journal-day">
            <h5>
              {d.date} <span className="muted">· {companyDays(d.moments)}</span>
            </h5>
            <ul>
              {d.moments.map((m) => (
                <li key={`${m.at}${m.kind}${m.text}`} className={m.big ? "big" : undefined}>
                  <button className="link" onClick={() => onPick(time(m.at))}>
                    <span className="journal-time">
                      {new Date(m.at).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" })}
                    </span>
                    <span className="journal-icon">{momentIcon(m)}</span>
                    <span>{m.text}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        );
      })}
    </section>
  );
}

const RANGE_TITLES: Record<StatsRange, string> = {
  day: "The last 24 hours",
  week: "These 7 days",
  month: "These 30 days",
  all: "Since you founded your company",
};

/** The graph so many steps from one, round the list */
function step(metrics: Metric[], metric: Metric, by: number): Metric {
  return metrics[(metrics.indexOf(metric) + by + metrics.length) % metrics.length] ?? metric;
}

const toSeries = (dto: StatsSeriesDto): Series => ({
  label: dto.name,
  color: dto.color,
  points: dto.at.map((at, i) => ({ t: time(at), v: dto.values[i] ?? null })),
});

/** The admin's Pace: every company's gold earned by days since it was founded, next to the model's player */
function PaceView() {
  const [pace, setPace] = useState<PaceDto | null>(null);
  useEffect(() => {
    void gameClient.connection.getPace().then(setPace);
  }, []);
  if (pace === null) {
    return <p className="muted">Loading…</p>;
  }
  const days = Math.max(
    2,
    Math.ceil(Math.max(...pace.companies.map((c) => c.days[c.days.length - 1] ?? 0))) + 1,
  );
  const grid = Array.from({ length: Math.min(days, pace.model.length - 1) * 4 + 1 }, (_, i) => i / 4);
  // Every line on the same days, so a hover reads them all
  const at = (c: PaceDto["companies"][number], day: number): number | null => {
    const i = c.days.findIndex((d) => d >= day);
    const [d0, d1, e0, e1] = [c.days[i - 1], c.days[i], c.earned[i - 1], c.earned[i]];
    if (d1 === undefined || e1 === undefined) {
      return null;
    }
    return d0 === undefined || e0 === undefined || d1 === d0 ? e1 : e0 + ((day - d0) / (d1 - d0)) * (e1 - e0);
  };
  const lines: Series[] = pace.companies.map((c) => ({
    label: c.name,
    color: c.color,
    points: grid.map((d) => ({ t: d * DAY, v: at(c, d) })),
  }));
  const model: Series = {
    label: "The model's regular player",
    color: "#6b5c43",
    dashed: true,
    points: grid.map((d) => {
      const lo = Math.floor(d);
      const [a, b] = [pace.model[lo] ?? null, pace.model[lo + 1] ?? null];
      return { t: d * DAY, v: a === null ? null : a + (d - lo) * ((b ?? a) - a) };
    }),
  };
  return (
    <div className="stats-pace">
      <p className="small muted">
        Gold earned by days since each company was founded, on a log scale, next to the design&apos;s model
        player (three visits a day). Only you see this.
      </p>
      <StatsChart
        series={[...lines, model]}
        stacked={false}
        from={0}
        to={((grid.length - 1) * DAY) / 4}
        founded={0}
        range="all"
        log
        format={(v) => `${formatAmount(v)} g`}
        moments={[]}
        away={[]}
        highlight={null}
        height={340}
        timeLabel={(t) => `Day ${(t / DAY + 1).toFixed(2).replace(/\.00$/, "")}`}
      />
      <div className="stats-legend">
        {[...lines, model].map((s) => (
          <span key={s.label} className="stats-legend-item">
            <span
              className={s.dashed ? "stats-swatch dashed" : "stats-swatch"}
              style={{ background: s.color }}
            />
            {s.label}
          </span>
        ))}
      </div>
    </div>
  );
}

/** Statistics (docs/design-notes/statistics.md): a company's graphs over a range, its key moments, compared with others */
export function StatisticsPanel() {
  const open = useAppStore((s) => s.statsOpen);
  const stats = useAppStore((s) => s.stats);
  const range = useAppStore((s) => s.statsRange);
  const metricId = useAppStore((s) => s.statsMetric);
  const content = useAppStore((s) => s.content);
  const admin = useAppStore((s) => s.user?.isAdmin ?? false);
  const [view, setView] = useState<"graphs" | "pace">("graphs");
  const [logChoice, setLogChoice] = useState<boolean | null>(null);
  const [highlight, setHighlight] = useState<number | null>(null);
  const [picked, setPicked] = useState<number[]>([]);
  const [fetched, setFetched] = useState<StatsSeriesDto[]>([]);

  const name = (good: string) => content?.resourceName(good) ?? good;
  // Every graph of the statistics, and its lines
  const { metrics, allSeries } = useMemo(() => {
    const goodName = (good: string) => content?.resourceName(good) ?? good;
    const goodColor = (good: string) => content?.resourceColor(good) ?? "#888";
    const list = stats === null ? [] : metricsOf(stats, goodName);
    return {
      metrics: list,
      allSeries: new Map(
        stats === null ? [] : list.map((m) => [m.id, seriesOf(stats, m, goodColor, goodName)]),
      ),
    };
  }, [stats, content]);
  const metric = metrics.find((m) => m.id === metricId) ?? metrics[0];
  const comparable =
    stats !== null &&
    metric !== undefined &&
    stats.own &&
    metric.kind === "value" &&
    !metric.town &&
    PUBLIC.has(metric.key);
  const compareKey = comparable ? metric.key : null;
  // The players compared with, for the graph shown (what was fetched for another graph or range waits for its own)
  const compared = fetched.filter((t) => t.key === compareKey && picked.includes(t.company));

  useEffect(() => {
    if (!open || compareKey === null || picked.length === 0) {
      return;
    }
    let current = true;
    void Promise.all(
      picked.map((id) => gameClient.connection.getStatisticsSeries(id, compareKey, range)),
    ).then((all) => {
      if (current) {
        setFetched(all.filter((t): t is StatsSeriesDto => t !== null));
      }
    });
    return () => {
      current = false;
    };
  }, [open, compareKey, picked, range]);

  // ← and → step through the graphs (and don't move the map)
  useEffect(() => {
    if (!open || view !== "graphs") {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if ((event.key !== "ArrowLeft" && event.key !== "ArrowRight") || metric === undefined) {
        return;
      }
      event.preventDefault();
      event.stopPropagation();
      const next = step(metrics, metric, event.key === "ArrowLeft" ? -1 : 1);
      setState({ statsMetric: next.id });
    };
    window.addEventListener("keydown", onKey, true);
    return () => window.removeEventListener("keydown", onKey, true);
  }, [open, view, metrics, metric]);

  if (!open) {
    return null;
  }
  const close = () => setState({ statsOpen: false });
  const pick = (id: string) => {
    setState({ statsMetric: id });
    setHighlight(null);
  };
  const series = metric === undefined ? [] : (allSeries.get(metric.id) ?? []);
  const log = logChoice ?? (range === "all" && metric?.kind === "value" && wantsLog(series));
  const title =
    stats === null || stats.own ? "Statistics" : `${stats.name}${possessive(stats.name)} statistics`;
  const founded = stats === null ? 0 : time(stats.founded);

  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel stats-panel"
        role="dialog"
        aria-label="Statistics"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>{title}</h2>
          <div className="tabs compact stats-ranges">
            {RANGES.map((r) => (
              <button
                key={r.id}
                className={r.id === range ? "tab active" : "tab"}
                onClick={() => {
                  setHighlight(null);
                  void gameClient.refreshStatistics(r.id);
                }}
              >
                {r.name}
              </button>
            ))}
          </div>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {admin && (stats?.own ?? true) && (
          <div className="tabs compact stats-views">
            <button className={view === "graphs" ? "tab active" : "tab"} onClick={() => setView("graphs")}>
              Graphs
            </button>
            <button className={view === "pace" ? "tab active" : "tab"} onClick={() => setView("pace")}>
              Pace · admin
            </button>
          </div>
        )}
        {view === "pace" ? (
          <PaceView />
        ) : stats === null || metric === undefined ? (
          <p className="muted">Loading…</p>
        ) : (
          <div className="stats-body">
            <nav className="stats-cards" aria-label="Graphs">
              {GROUPS.map((group) => {
                const cards = metrics.filter((m) => m.group === group);
                return cards.length === 0 ? null : (
                  <div key={group} className="stats-group">
                    <h4>{group === "Town" ? stats.town.name : group}</h4>
                    {cards.map((m) => (
                      <Card
                        key={m.id}
                        metric={m}
                        series={allSeries.get(m.id) ?? []}
                        stats={stats}
                        active={m.id === metric.id}
                        onPick={() => pick(m.id)}
                      />
                    ))}
                  </div>
                );
              })}
            </nav>
            <section className="stats-main">
              <div className="stats-head">
                <div className="stats-title">
                  <button
                    className="icon-button"
                    title="The graph before (←)"
                    onClick={() => pick(step(metrics, metric, -1).id)}
                  >
                    ‹
                  </button>
                  <h3>
                    {metric.resource !== undefined && <ResourceIcon resource={metric.resource} />}
                    {metric.name}
                  </h3>
                  <button
                    className="icon-button"
                    title="The next graph (→)"
                    onClick={() => pick(step(metrics, metric, 1).id)}
                  >
                    ›
                  </button>
                </div>
                <div className="stats-now">
                  <strong>{bigNumber(metric, headline(stats, metric, series))}</strong>
                  <span className="small muted">
                    {metric.kind === "value" ? "now" : RANGE_WORDS[stats.range]}
                    {metric.kind === "value" &&
                      change(series[0]?.points ?? []) !== "" &&
                      ` · ${change(series[0]?.points ?? [])} ${RANGE_WORDS[stats.range]}`}
                  </span>
                </div>
                <p className="small muted">{metric.description}</p>
              </div>
              <StatsChart
                series={series}
                stacked={metric.kind === "stack"}
                steps={metric.steps}
                compare={compared.map(toSeries)}
                from={Math.max(time(stats.from), founded)}
                to={time(stats.now)}
                founded={founded}
                range={stats.range}
                log={log}
                format={(v) => formatValue(metric, v)}
                moments={stats.moments}
                away={metric.town ? [] : awayStretches(stats)}
                highlight={highlight}
              />
              <div className="stats-legend">
                {(metric.kind === "stack" || compared.length > 0) &&
                  [...series, ...compared.map(toSeries)].map((s) => (
                    <span key={s.label} className="stats-legend-item">
                      <span className="stats-swatch" style={{ background: s.color }} />
                      {s.label}
                    </span>
                  ))}
                {metric.kind === "value" && (
                  <label className="stats-log">
                    <input
                      type="checkbox"
                      checked={log}
                      onChange={(event) => setLogChoice(event.target.checked)}
                    />{" "}
                    Log scale
                  </label>
                )}
              </div>
              {comparable && stats.peers.length > 0 && (
                <div className="stats-compare">
                  <span className="small muted">Compare with</span>
                  {stats.peers.map((p) => {
                    const on = picked.includes(p.id);
                    return (
                      <button
                        key={p.id}
                        className={on ? "stats-peer on" : "stats-peer"}
                        aria-pressed={on}
                        disabled={!on && picked.length >= 3}
                        onClick={() => setPicked(on ? picked.filter((t) => t !== p.id) : [...picked, p.id])}
                      >
                        <span className="company-dot" style={{ background: p.color }} />
                        {p.name}
                        {p.crew && <span className="small muted"> · crew</span>}
                      </button>
                    );
                  })}
                </div>
              )}
              <div className="stats-lower">
                {stats.own && (
                  <section className="stats-insights" aria-label="Insights">
                    <h4>{RANGE_TITLES[stats.range]}</h4>
                    {insights(stats, name).map((line) => (
                      <p key={line}>{line}</p>
                    ))}
                    {stats.at.length < 3 && (
                      <p className="small muted">
                        A new point every 15 minutes: the graphs fill in as you play.
                      </p>
                    )}
                  </section>
                )}
                <Journal stats={stats} onPick={setHighlight} />
              </div>
            </section>
          </div>
        )}
      </div>
    </div>
  );
}

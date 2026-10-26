import type { MomentDto, StatisticsDto, StatsColumns, StatsRange } from "../api/contracts";
import { formatAmount } from "./format";

// Statistics (docs/design-notes/statistics.md): the series of a company's samples, drawn as values or as rates of
// counters, the stretches its player was away, the ticks of the axes and a few sentences about a range

export const MINUTE = 60_000;
export const HOUR = 60 * MINUTE;
export const DAY = 24 * HOUR;

export interface Point {
  /** The time, in milliseconds */
  t: number;
  v: number | null;
}

export type Group = "Gold" | "Production" | "Your plot" | "Research" | "Together" | "Town";

export const GROUPS: Group[] = ["Gold", "Production", "Your plot", "Research", "Together", "Town"];

/** A part of a stacked graph: a counter drawn as a rate, or what is left of a total after the other parts */
export interface StackPart {
  key: string;
  label: string;
  color: string;
}

/** How a graph is drawn */
export interface Metric {
  id: string;
  group: Group;
  name: string;
  /** After the number: "/min", "tiles" */
  unit: string;
  /** value: as sampled; rate: a counter's growth per minute; stack: several rates on top of each other */
  kind: "value" | "rate" | "stack";
  /** The series (a value or counter); for a stack, the total its parts add up to ("" when they are all counters) */
  key: string;
  parts?: StackPart[];
  /** From the town's samples */
  town?: boolean;
  /** Whole steps (levels, counts) */
  steps?: boolean;
  /** A resource for the card's icon */
  resource?: string;
  description: string;
}

export const COLORS = {
  line: "#236b69",
  gold: "#b07d10",
  market: "#2f8784",
  players: "#c9963a",
  orders: "#7a4a24",
  rewards: "#d8a92c",
  research: "#5a6cb0",
  land: "#5f9a36",
  gifts: "#a8558a",
  building: "#2f8784",
};

const RATE = "/min";

/** The graphs every company has (production adds one per good it made) */
export const METRICS: Metric[] = [
  {
    id: "income",
    group: "Gold",
    name: "Income",
    unit: RATE,
    kind: "value",
    key: "income",
    description: "Gold a minute your plot makes: the top bar's number at each moment.",
  },
  {
    id: "worth",
    group: "Gold",
    name: "Company value",
    unit: "g",
    kind: "value",
    key: "worth",
    description:
      "Your gold and what your buildings, levels and land are worth (the leaderboards' Net worth).",
  },
  {
    id: "earned",
    group: "Gold",
    name: "Gold earned",
    unit: "g",
    kind: "value",
    key: "earned",
    description:
      "All the gold you ever earned. Your Company Level comes from it; spending takes nothing away.",
  },
  {
    id: "earnedBy",
    group: "Gold",
    name: "Earned by source",
    unit: RATE,
    kind: "stack",
    key: "",
    parts: [
      { key: "earned:market", label: "The market", color: COLORS.market },
      { key: "earned:players", label: "Other players", color: COLORS.players },
      { key: "earned:orders", label: "Daily orders", color: COLORS.orders },
      { key: "earned:rewards", label: "Rewards", color: COLORS.rewards },
    ],
    description: "Gold a minute you earned, by where it came from.",
  },
  {
    id: "spentBy",
    group: "Gold",
    name: "Spent by kind",
    unit: RATE,
    kind: "stack",
    key: "spent",
    parts: [
      { key: "", label: "Buildings and levels", color: COLORS.building },
      { key: "spent:land", label: "Land", color: COLORS.land },
      { key: "spent:research", label: "Research", color: COLORS.research },
      { key: "spent:gifts", label: "Wonders and Monuments", color: COLORS.gifts },
    ],
    description: "Gold a minute you spent, by what it bought.",
  },
  {
    id: "gold",
    group: "Gold",
    name: "Gold in the bank",
    unit: "g",
    kind: "value",
    key: "gold",
    description: "The gold you had at each moment.",
  },
  {
    id: "level",
    group: "Gold",
    name: "Company Level",
    unit: "",
    kind: "value",
    key: "level",
    steps: true,
    description: "Your Company Level: it rises with the gold you earn.",
  },
  {
    id: "salesBy",
    group: "Production",
    name: "Sales by good",
    unit: RATE,
    kind: "stack",
    key: "",
    description: "Gold a minute your goods brought at the market and from other players.",
  },
  {
    id: "buildings",
    group: "Your plot",
    name: "Buildings",
    unit: "",
    kind: "value",
    key: "buildings",
    steps: true,
    description: "The buildings standing on your land.",
  },
  {
    id: "levels",
    group: "Your plot",
    name: "Building levels",
    unit: "",
    kind: "value",
    key: "levels",
    steps: true,
    description: "The levels of all your buildings together.",
  },
  {
    id: "land",
    group: "Your plot",
    name: "Land",
    unit: "tiles",
    kind: "value",
    key: "land",
    steps: true,
    description: "The tiles you own: your plot's parcels, outskirts, Deep Level and outposts.",
  },
  {
    id: "pipes",
    group: "Your plot",
    name: "Pipe tiles",
    unit: "",
    kind: "value",
    key: "pipes",
    steps: true,
    description: "The tiles of pipe you laid.",
  },
  {
    id: "perTile",
    group: "Your plot",
    name: "Gold per tile",
    unit: "/h",
    kind: "value",
    key: "perTile",
    description: "Gold an hour for each tile you own: how well you use your land.",
  },
  {
    id: "tiers",
    group: "Research",
    name: "Track tiers",
    unit: "",
    kind: "value",
    key: "tiers",
    steps: true,
    description: "The tiers of all your research tracks together.",
  },
  {
    id: "tree",
    group: "Research",
    name: "Era tree",
    unit: "items",
    kind: "value",
    key: "tree",
    steps: true,
    description: "The Era tree items you researched.",
  },
  {
    id: "era",
    group: "Research",
    name: "Era",
    unit: "",
    kind: "value",
    key: "era",
    steps: true,
    description: "Your Era: the highest a town of yours reached.",
  },
  {
    id: "partners",
    group: "Together",
    name: "Trading partners",
    unit: "",
    kind: "value",
    key: "partners",
    steps: true,
    description: "The players you trade with through your Yard.",
  },
  {
    id: "soldToPlayers",
    group: "Together",
    name: "Sold to players",
    unit: RATE,
    kind: "rate",
    key: "soldToPlayers",
    description: "Gold a minute other players paid you.",
  },
  {
    id: "boughtFromPlayers",
    group: "Together",
    name: "Bought from players",
    unit: RATE,
    kind: "rate",
    key: "boughtFromPlayers",
    description: "Gold a minute you paid other players.",
  },
  {
    id: "given",
    group: "Together",
    name: "Given to Wonders",
    unit: "g",
    kind: "value",
    key: "given",
    description: "What the goods you gave your town's Wonders were worth, in all.",
  },
  {
    id: "likes",
    group: "Together",
    name: "Likes",
    unit: "",
    kind: "value",
    key: "likes",
    steps: true,
    description: "Likes visitors gave your plot.",
  },
  {
    id: "townIncome",
    group: "Town",
    name: "Town income",
    unit: RATE,
    kind: "value",
    key: "income",
    town: true,
    description: "Gold a minute everyone in your town makes together.",
  },
  {
    id: "townPlayers",
    group: "Town",
    name: "Players",
    unit: "",
    kind: "value",
    key: "players",
    town: true,
    steps: true,
    description: "The players living in your town.",
  },
  {
    id: "townWonder",
    group: "Town",
    name: "Wonder stages",
    unit: "",
    kind: "value",
    key: "wonder",
    town: true,
    description: "The stages of the town's Wonder that stand, and how much of the next one is in.",
  },
];

export const time = (iso: string) => new Date(iso).getTime();

/** The columns of a metric: the company's or its town's */
export function columnsOf(stats: StatisticsDto, metric: Metric): StatsColumns {
  return metric.town ? stats.town : stats;
}

/** A series as it was sampled */
export function column(columns: StatsColumns, key: string): Point[] {
  const index = columns.keys.indexOf(key);
  return columns.at.map((at, i) => ({
    t: time(at),
    v: index < 0 ? null : (columns.rows[i]?.[index] ?? null),
  }));
}

/** Whether the columns have a series at all */
export function has(columns: StatsColumns, key: string): boolean {
  return columns.keys.includes(key);
}

/**
 * A counter's growth per minute between each point and the one before (null for the first, and for one less than five
 * minutes after it: the live point right after a sample would be all noise). It never goes below zero (a refund).
 */
export function rate(points: Point[]): Point[] {
  return points.map((p, i) => {
    const before = points[i - 1];
    if (before === undefined || p.v === null || before.v === null || p.t - before.t < 5 * MINUTE) {
      return { t: p.t, v: null };
    }
    return { t: p.t, v: Math.max(0, (p.v - before.v) / ((p.t - before.t) / MINUTE)) };
  });
}

export interface Series {
  label: string;
  color: string;
  points: Point[];
  dashed?: boolean;
}

/** The series anyone may see of another company (the server's Statistics.Public): the ones to compare with */
export const PUBLIC = new Set([
  "income",
  "worth",
  "earned",
  "level",
  "buildings",
  "levels",
  "land",
  "pipes",
  "perTile",
  "tiers",
  "tree",
  "era",
  "partners",
  "likes",
  "given",
]);

/** The goods a company made or sold, the most first */
export function goods(columns: StatsColumns, prefix: "made:" | "sold:"): string[] {
  return columns.keys
    .filter((k) => k.startsWith(prefix))
    .map((k) => ({ good: k.slice(prefix.length), total: growth(column(columns, k)) ?? 0 }))
    .filter((g) => g.total > 0)
    .sort((a, b) => b.total - a.total)
    .map((g) => g.good);
}

/** A production graph per good the company made in the range */
export function productionMetrics(stats: StatisticsDto, name: (good: string) => string): Metric[] {
  return goods(stats, "made:").map((good) => ({
    id: `made:${good}`,
    group: "Production",
    name: name(good),
    unit: RATE,
    kind: "rate",
    key: `made:${good}`,
    resource: good,
    description: `${name(good)} your buildings made a minute.`,
  }));
}

/** The graphs a company's statistics can draw (another's only what it shows) */
export function metricsOf(stats: StatisticsDto, name: (good: string) => string): Metric[] {
  const all = [
    ...METRICS.filter((m) => m.group !== "Production"),
    ...METRICS.filter((m) => m.group === "Production"),
  ];
  const shown = all.filter((m) => {
    const columns = columnsOf(stats, m);
    if (m.id === "salesBy") {
      return goods(columns, "sold:").length > 0;
    }
    return m.kind === "stack"
      ? (m.parts ?? []).some((p) => p.key !== "" && has(columns, p.key))
      : has(columns, m.key);
  });
  const production = productionMetrics(stats, name);
  const at = shown.findIndex((m) => m.group === "Production");
  shown.splice(at < 0 ? shown.length : at, 0, ...production);
  return GROUPS.flatMap((g) => shown.filter((m) => m.group === g));
}

/** The lines (or the stacked parts, bottom first) of a metric */
export function seriesOf(
  stats: StatisticsDto,
  metric: Metric,
  color: (good: string) => string,
  name: (good: string) => string,
): Series[] {
  const columns = columnsOf(stats, metric);
  if (metric.kind === "value") {
    return [{ label: metric.name, color: COLORS.line, points: column(columns, metric.key) }];
  }
  if (metric.kind === "rate") {
    return [
      {
        label: metric.name,
        color: metric.resource ? color(metric.resource) : COLORS.line,
        points: rate(column(columns, metric.key)),
      },
    ];
  }
  if (metric.id === "salesBy") {
    // The six goods that sold best, and the rest together
    const sold = goods(columns, "sold:");
    const top = sold
      .slice(0, 6)
      .map((g) => ({ label: name(g), color: color(g), points: rate(column(columns, `sold:${g}`)) }));
    const rest = sold.slice(6).map((g) => rate(column(columns, `sold:${g}`)));
    if (rest.length > 0) {
      top.push({ label: "Other goods", color: "#9a8a6a", points: sum(rest) });
    }
    return top;
  }
  const parts = metric.parts ?? [];
  const counted = parts
    .filter((p) => p.key !== "" && has(columns, p.key))
    .map((p) => ({ ...p, points: rate(column(columns, p.key)) }));
  const rest = parts.find((p) => p.key === "");
  if (rest !== undefined && metric.key !== "" && has(columns, metric.key)) {
    // The total less its other parts: what the rest took
    const total = rate(column(columns, metric.key));
    const others = sum(counted.map((p) => p.points));
    const points = total.map((p, i) => ({
      t: p.t,
      v: p.v === null ? null : Math.max(0, p.v - (others[i]?.v ?? 0)),
    }));
    return [{ label: rest.label, color: rest.color, points }, ...counted];
  }
  return counted;
}

/** Point by point, the sum of several series (null where all are) */
function sum(series: Point[][]): Point[] {
  return (series[0] ?? []).map((p, i) => {
    const values = series.map((s) => s[i]?.v).filter((v): v is number => v !== null && v !== undefined);
    return { t: p.t, v: values.length === 0 ? null : values.reduce((a, b) => a + b, 0) };
  });
}

/** Stacked parts as bands: each part's bottom and top at every point */
export function stack(series: Series[]): { series: Series; low: Point[]; high: Point[] }[] {
  const base = series[0]?.points.map((p) => ({ t: p.t, v: p.v === null ? null : 0 })) ?? [];
  return series.map((s) => {
    const low = base.map((p) => ({ ...p }));
    s.points.forEach((p, i) => {
      if (base[i] !== undefined && p.v !== null) {
        base[i].v = (base[i].v ?? 0) + p.v;
      }
    });
    return { series: s, low, high: base.map((p) => ({ ...p })) };
  });
}

export const first = (points: Point[]) => points.find((p) => p.v !== null)?.v ?? null;

export function last(points: Point[]): number | null {
  return points.findLast((p) => p.v !== null)?.v ?? null;
}

/** How much a counter grew over the points (null: no two values) */
export function growth(points: Point[]): number | null {
  const a = first(points);
  const b = last(points);
  return a === null || b === null ? null : b - a;
}

/** A value's change from the first point to the last: "+38%", "×3.2", "−12%" ("" when it can't be said) */
export function change(points: Point[]): string {
  const a = first(points);
  const b = last(points);
  if (a === null || b === null || a === b) {
    return "";
  }
  if (a <= 0) {
    return b > 0 ? "new" : "";
  }
  const ratio = b / a;
  if (ratio >= 2) {
    return `×${ratio >= 10 ? Math.round(ratio) : ratio.toFixed(1)}`;
  }
  const percent = Math.round((ratio - 1) * 100);
  return percent === 0 ? "" : `${percent > 0 ? "+" : "−"}${Math.abs(percent)}%`;
}

/** What a card shows big: a value now, or what a counter added up to over the range */
export function headline(stats: StatisticsDto, metric: Metric, series: Series[]): number | null {
  if (metric.kind === "value") {
    return last(series[0]?.points ?? []);
  }
  if (metric.kind === "rate") {
    return growth(column(columnsOf(stats, metric), metric.key));
  }
  // A stack: what all its parts added up to
  const totals = series.map((s) =>
    s.points.reduce(
      (a, p, i) => a + (p.v ?? 0) * (i === 0 ? 0 : (p.t - (s.points[i - 1]?.t ?? p.t)) / MINUTE),
      0,
    ),
  );
  return totals.reduce((a, b) => a + b, 0);
}

/** A number of a metric, with its unit */
export function formatValue(metric: Metric, value: number): string {
  if (metric.id === "townWonder") {
    const stages = Math.floor(value + 1e-9);
    const share = Math.round((value - stages) * 100);
    return share > 0 ? `${stages} · ${share}%` : `${stages}`;
  }
  const amount = metric.id === "perTile" && value < 100 ? value.toFixed(1) : formatAmount(value);
  return metric.unit === ""
    ? amount
    : metric.unit.startsWith("/")
      ? `${amount}${metric.unit}`
      : `${amount} ${metric.unit}`;
}

/** Each point with the one before it */
function pairs(points: Point[]): [Point, Point][] {
  return points.flatMap((b, i) => {
    const a = points[i - 1];
    return a === undefined ? [] : [[a, b] as [Point, Point]];
  });
}

/** The stretches the company's player was away: between two samples with no minutes played */
export function awayStretches(columns: StatsColumns): { from: number; to: number }[] {
  const stretches: { from: number; to: number }[] = [];
  for (const [a, b] of pairs(column(columns, "played"))) {
    if (a.v === null || b.v === null || b.v > a.v || b.t - a.t < 5 * MINUTE) {
      continue;
    }
    const open = stretches[stretches.length - 1];
    if (open !== undefined && open.to === a.t) {
      open.to = b.t;
    } else {
      stretches.push({ from: a.t, to: b.t });
    }
  }
  return stretches;
}

/** The company's day of a moment: day 1 is its first 24 hours (the days of the design's model) */
export function dayOf(t: number, founded: number): number {
  return Math.floor((t - founded) / DAY) + 1;
}

/** Ticks of a linear axis: about count of them, on 1, 2 or 5 times a power of ten (the step nearest to an even split) */
export function niceTicks(min: number, max: number, count = 4): number[] {
  if (!(max > min)) {
    return [min];
  }
  const raw = (max - min) / count;
  const power = Math.pow(10, Math.floor(Math.log10(raw)));
  const step = [1, 2, 5, 10]
    .map((m) => m * power)
    .reduce((best, s) => (Math.abs(Math.log(s / raw)) < Math.abs(Math.log(best / raw)) ? s : best));
  const ticks: number[] = [];
  for (let v = Math.ceil(min / step) * step; v <= max + step * 1e-9; v += step) {
    ticks.push(Math.abs(v) < step * 1e-9 ? 0 : v);
  }
  return ticks;
}

/** Ticks of a log axis: the powers of ten in between (with 3s over three decades or fewer) */
export function logTicks(min: number, max: number): number[] {
  min = Math.max(min, 1e-9);
  max = Math.max(max, min);
  const low = Math.floor(Math.log10(min));
  const high = Math.ceil(Math.log10(max));
  const few = Math.log10(max / min) <= 3;
  const ticks: number[] = [];
  for (let e = low; e <= high; e++) {
    for (const m of few ? [1, 3] : [1]) {
      const v = m * Math.pow(10, e);
      if (v >= min && v <= max) {
        ticks.push(v);
      }
    }
  }
  return ticks;
}

/**
 * Ticks of the time axis: the clock every few hours over a day, else the company's days ("Day 3"), every 1, 2, 5,
 * 10... days so there are at most about seven
 */
export function timeTicks(
  from: number,
  to: number,
  founded: number,
  range: StatsRange,
): { t: number; label: string }[] {
  const span = to - from;
  if (range === "day" || span <= 1.5 * DAY) {
    const step = [1, 2, 3, 6].map((h) => h * HOUR).find((s) => span / s <= 8) ?? 6 * HOUR;
    const start = new Date(from);
    start.setMinutes(0, 0, 0);
    const ticks: { t: number; label: string }[] = [];
    for (let t = start.getTime(); t <= to; t += HOUR) {
      const hour = new Date(t).getHours();
      if (t >= from && hour % (step / HOUR) === 0) {
        ticks.push({ t, label: `${String(hour).padStart(2, "0")}:00` });
      }
    }
    return ticks;
  }
  const every = [1, 2, 5, 10, 20, 50, 100].find((d) => span / (d * DAY) <= 7) ?? 100;
  const ticks: { t: number; label: string }[] = [];
  for (let day = Math.max(1, dayOf(from, founded)); founded + (day - 1) * DAY <= to; day++) {
    const t = founded + (day - 1) * DAY;
    if (t >= from && (day - 1) % every === 0) {
      ticks.push({ t, label: `Day ${day}` });
    }
  }
  return ticks;
}

/** A moment of a graph, as a time and a label: "Tue 21:30 · day 3" */
export function whenLabel(t: number, founded: number): string {
  const date = new Date(t);
  const weekday = date.toLocaleDateString("en-GB", { weekday: "short" });
  const clock = date.toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" });
  return `${weekday} ${clock} · day ${dayOf(t, founded)}`;
}

/** Moments close together on the graph share one marker (at the first of them) */
export function clusterMoments(
  moments: MomentDto[],
  x: (t: number) => number,
  gap: number,
): { x: number; moments: MomentDto[] }[] {
  const clusters: { x: number; moments: MomentDto[] }[] = [];
  for (const moment of [...moments].sort((a, b) => time(a.at) - time(b.at))) {
    const at = x(time(moment.at));
    const open = clusters[clusters.length - 1];
    if (open !== undefined && at - open.x < gap) {
      open.moments.push(moment);
    } else {
      clusters.push({ x: at, moments: [moment] });
    }
  }
  return clusters;
}

const MOMENT_ICONS: Record<string, string> = {
  founded: "🏁",
  level: "⭐",
  first: "✨",
  land: "🗺️",
  era: "🚀",
  wonder: "🏛️",
  crew: "🏘️",
  partner: "🤝",
  move: "🎈",
  track: "🔬",
  research: "📘",
  achievement: "🏅",
  monument: "🗿",
  event: "📅",
};

export const momentIcon = (moment: MomentDto | undefined) => (moment && MOMENT_ICONS[moment.kind]) ?? "•";

/** The words of a range: "in the last 24 hours", "in these 7 days" */
export const RANGE_WORDS: Record<StatsRange, string> = {
  day: "in the last 24 hours",
  week: "in these 7 days",
  month: "in these 30 days",
  all: "since you founded your company",
};

/**
 * A few sentences about a range: how income grew, the good that sold best, what came while you were away, the best
 * stretch, the levels reached (only what the numbers say)
 */
export function insights(stats: StatisticsDto, name: (good: string) => string): string[] {
  const lines: string[] = [];
  const range = RANGE_WORDS[stats.range];
  const income = column(stats, "income");
  const grew = change(income);
  if (grew.startsWith("×") || grew.startsWith("+")) {
    lines.push(`Income ${grew} ${range}.`);
  }
  const sold = goods(stats, "sold:");
  const total = sold.reduce((a, g) => a + (growth(column(stats, `sold:${g}`)) ?? 0), 0);
  const top = sold[0];
  if (top !== undefined && sold.length > 1 && total > 0) {
    const share = (growth(column(stats, `sold:${top}`)) ?? 0) / total;
    lines.push(`${Math.round(share * 100)}% of your sales were ${name(top)}.`);
  }
  const earned = column(stats, "earned");
  const away = awayStretches(stats);
  const all = growth(earned) ?? 0;
  if (away.length > 0 && all > 0) {
    let whileAway = 0;
    for (const [a, b] of pairs(earned)) {
      if (a.v !== null && b.v !== null && away.some((s) => a.t >= s.from && b.t <= s.to)) {
        whileAway += b.v - a.v;
      }
    }
    if (whileAway >= 0.05 * all) {
      lines.push(`${Math.round((whileAway / all) * 100)}% of your gold came while you were away.`);
    }
  }
  const rates = rate(earned);
  const best = rates.reduce<Point | null>(
    (b, p) => (p.v !== null && (b === null || p.v > (b.v ?? 0)) ? p : b),
    null,
  );
  if (best?.v != null && best.v > 0) {
    lines.push(`Best: ${formatAmount(best.v)}/min earned, ${whenLabel(best.t, time(stats.founded))}.`);
  }
  const levels = growth(column(stats, "level"));
  if (levels !== null && levels > 0) {
    lines.push(`${levels} Company ${levels === 1 ? "Level" : "Levels"} ${range}.`);
  }
  return lines.slice(0, 4);
}

/** Whether a log scale suits a series: positive, and over more than a factor 100 */
export function wantsLog(series: Series[]): boolean {
  const values = series
    .flatMap((s) => s.points.map((p) => p.v))
    .filter((v): v is number => v !== null && v > 0);
  if (values.length < 2) {
    return false;
  }
  return Math.max(...values) / Math.min(...values) > 100;
}

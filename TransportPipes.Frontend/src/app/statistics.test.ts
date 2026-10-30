import { describe, expect, it } from "vitest";
import type { StatisticsDto } from "../api/contracts";
import {
  DAY,
  HOUR,
  MINUTE,
  awayStretches,
  change,
  clusterMoments,
  column,
  insights,
  logTicks,
  metricsOf,
  niceTicks,
  rate,
  seriesOf,
  timeTicks,
  METRICS,
} from "./statistics";

const T0 = Date.UTC(2026, 9, 26, 12, 0);
const iso = (minutes: number) => new Date(T0 + minutes * MINUTE).toISOString();

/** Statistics of four samples a quarter hour apart (and the series given) */
function stats(series: Record<string, (number | null)[]>, extra: Partial<StatisticsDto> = {}): StatisticsDto {
  const keys = Object.keys(series);
  const count = Object.values(series)[0]?.length ?? 0;
  const at = Array.from({ length: count }, (_, i) => iso(15 * i));
  return {
    company: 1,
    name: "Graphs & Co",
    color: "#336699",
    founded: iso(0),
    own: true,
    range: "day",
    from: iso(0),
    now: at[at.length - 1] ?? iso(0),
    keys,
    at,
    rows: at.map((_, i) => keys.map((k) => series[k]?.[i] ?? null)),
    moments: [],
    town: { name: "Stonebridge", keys: [], at: [], rows: [] },
    peers: [],
    ...extra,
  };
}

const name = (good: string) => good.charAt(0).toUpperCase() + good.slice(1);
const color = () => "#123456";

describe("rate", () => {
  it("is a counter's growth per minute, never below zero", () => {
    const points = [0, 150, 450, 300].map((v, i) => ({ t: T0 + 15 * i * MINUTE, v }));
    expect(rate(points).map((p) => p.v)).toEqual([null, 10, 20, 0]);
  });

  it("leaves out a point right after the last sample (the live one)", () => {
    const points = [
      { t: T0, v: 0 },
      { t: T0 + 2 * MINUTE, v: 100 },
    ];
    expect(rate(points)[1]?.v).toBeNull();
  });
});

describe("change", () => {
  const points = (a: number, b: number) => [
    { t: 0, v: a },
    { t: 1, v: b },
  ];

  it("says a small change in percent and a big one as a factor", () => {
    expect(change(points(100, 138))).toBe("+38%");
    expect(change(points(100, 88))).toBe("−12%");
    expect(change(points(100, 320))).toBe("×3.2");
    expect(change(points(100, 4200))).toBe("×42");
    expect(change(points(100, 100))).toBe("");
    expect(change(points(0, 5))).toBe("new");
  });
});

describe("the stacked graphs", () => {
  it("draw what is spent on buildings and levels as the total less the other kinds", () => {
    const s = stats({
      spent: [0, 600, 1200, 1500],
      "spent:land": [0, 300, 300, 300],
      "spent:research": [0, 0, 150, 150],
    });
    const spent = METRICS.find((m) => m.id === "spentBy")!;
    const series = seriesOf(s, spent, color, name);
    expect(series.map((t) => t.label)).toEqual(["Buildings and levels", "Land", "Research"]);
    expect(series[0]?.points.map((p) => p.v)).toEqual([null, 20, 30, 20]);
    expect(series[1]?.points.map((p) => p.v)).toEqual([null, 20, 0, 0]);
  });

  it("show the goods that sold best", () => {
    const s = stats({ "sold:iron": [0, 10, 20, 30], "sold:copper": [0, 150, 300, 450] });
    const sales = METRICS.find((m) => m.id === "salesBy")!;
    expect(seriesOf(s, sales, color, name).map((t) => t.label)).toEqual(["Copper", "Iron"]);
  });
});

describe("metricsOf", () => {
  it("lists another company's public graphs only, and a production graph per good", () => {
    const own = stats({ income: [1, 2, 3, 4], gold: [5, 5, 5, 5], "made:iron": [0, 1, 2, 3] });
    expect(metricsOf(own, name).map((m) => m.id)).toEqual(["income", "gold", "made:iron"]);
    const theirs = stats({ income: [1, 2, 3, 4] }, { own: false });
    expect(metricsOf(theirs, name).map((m) => m.id)).toEqual(["income"]);
  });
});

describe("awayStretches", () => {
  it("are the samples apart without a minute played, joined", () => {
    const s = stats({ played: [15, 30, 30, 30, 45] });
    expect(awayStretches(s)).toEqual([{ from: T0 + 15 * MINUTE, to: T0 + 45 * MINUTE }]);
  });
});

describe("insights", () => {
  it("say how income grew, what sold best and what came while away", () => {
    const s = stats({
      income: [100, 150, 250, 400],
      earned: [0, 1500, 4500, 10500],
      played: [15, 15, 15, 30],
      "sold:iron": [0, 500, 1500, 3500],
      "sold:copper": [0, 1000, 3000, 7000],
    });
    const lines = insights(s, name);
    expect(lines[0]).toBe("Income ×4.0 in the last 24 hours.");
    expect(lines[1]).toBe("67% of your sales were Copper.");
    expect(lines[2]).toBe("43% of your gold came while you were away.");
    expect(lines[3]).toMatch(/^Best: 400\/min earned, /);
  });
});

describe("axes", () => {
  it("put linear ticks on 1, 2 and 5", () => {
    expect(niceTicks(0, 1000)).toEqual([0, 200, 400, 600, 800, 1000]);
    expect(niceTicks(0, 7)).toEqual([0, 2, 4, 6]);
  });

  it("put log ticks on powers of ten", () => {
    expect(logTicks(1, 1e6)).toEqual([1, 10, 100, 1000, 1e4, 1e5, 1e6]);
    expect(logTicks(5, 900)).toEqual([10, 30, 100, 300]);
  });

  it("count the company's days, every so many", () => {
    const founded = T0;
    expect(timeTicks(founded, founded + 3 * DAY, founded, "all").map((t) => t.label)).toEqual([
      "Day 1",
      "Day 2",
      "Day 3",
      "Day 4",
    ]);
    expect(timeTicks(founded, founded + 30 * DAY, founded, "all").map((t) => t.label)).toEqual([
      "Day 1",
      "Day 6",
      "Day 11",
      "Day 16",
      "Day 21",
      "Day 26",
      "Day 31",
    ]);
  });

  it("show the clock over a day", () => {
    const ticks = timeTicks(T0, T0 + DAY, T0, "day");
    expect(ticks.length).toBeGreaterThanOrEqual(4);
    expect(ticks.length).toBeLessThanOrEqual(8);
    expect(ticks.every((t) => /^\d\d:00$/.test(t.label))).toBe(true);
    expect((ticks[1]?.t ?? 0) - (ticks[0]?.t ?? 0)).toBeGreaterThanOrEqual(HOUR);
  });
});

describe("clusterMoments", () => {
  it("puts moments close together under one marker", () => {
    const moment = (minutes: number) => ({
      at: iso(minutes),
      kind: "level",
      text: `${minutes}`,
      big: false,
      ref: null,
    });
    const clusters = clusterMoments([moment(0), moment(1), moment(30)], (t) => (t - T0) / MINUTE, 10);
    expect(clusters.map((c) => c.moments.length)).toEqual([2, 1]);
  });
});

describe("column", () => {
  it("is null where a sample lacks the series", () => {
    const s = stats({ income: [1, null, 3, 4] });
    expect(column(s, "income").map((p) => p.v)).toEqual([1, null, 3, 4]);
    expect(column(s, "gold").every((p) => p.v === null)).toBe(true);
  });
});

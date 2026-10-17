import { describe, expect, it } from "vitest";
import { formatGold, formatGoldPerSecond, formatPerHour, goldAt } from "./gold";

describe("goldAt", () => {
  it("is 0 before the server sent anything", () => {
    expect(goldAt(null, 1000)).toBe(0);
  });

  it("extrapolates with the gold per second since the snapshot", () => {
    const snapshot = { currentGold: 500, goldPerSecond: 2.5, receivedAt: 10_000 };
    expect(goldAt(snapshot, 10_000)).toBe(500);
    expect(goldAt(snapshot, 14_000)).toBe(510);
    expect(goldAt(snapshot, 10_500)).toBeCloseTo(501.25);
  });

  it("goes down with a negative income", () => {
    expect(goldAt({ currentGold: 100, goldPerSecond: -1, receivedAt: 0 }, 30_000)).toBe(70);
  });
});

describe("formatting", () => {
  it("rounds gold like the old HUD", () => {
    expect(formatGold(1234.6)).toBe("1,235");
    expect(formatGoldPerSecond(0.0027777777)).toBe("0.0028");
    expect(formatGoldPerSecond(3)).toBe("3");
  });

  it("shows per second values per hour", () => {
    expect(formatPerHour(5 / 3600)).toBe("5");
    expect(formatPerHour(1000)).toBe("3600000");
    expect(formatPerHour(0.001)).toBe("3.60");
  });
});

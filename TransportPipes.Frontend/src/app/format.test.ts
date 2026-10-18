import { describe, expect, it } from "vitest";
import { displayName, formatAmount, formatDuration, formatRate, formatVersion, plural } from "./format";

describe("formatVersion", () => {
  it("shows the version with the short commit", () => {
    expect(formatVersion({ version: "1.0.5-beta", commit: "0307205a1b2c3d4e5f" })).toBe(
      "v1.0.5-beta · 0307205",
    );
  });

  it("leaves out an unknown commit", () => {
    expect(formatVersion({ version: "1.0.5", commit: null })).toBe("v1.0.5");
    expect(formatVersion({ version: "1.0.5", commit: "" })).toBe("v1.0.5");
  });
});

describe("displayName", () => {
  it("splits the type name into words", () => {
    expect(displayName("FactorySmall")).toBe("Factory Small");
  });
});

describe("formatDuration", () => {
  it("formats build times", () => {
    expect(formatDuration(0)).toBe("instant");
    expect(formatDuration(30)).toBe("30 s");
    expect(formatDuration(120)).toBe("2 min");
    expect(formatDuration(90)).toBe("1 min 30 s");
    expect(formatDuration(3600 * 2 + 600)).toBe("2 h 10 min");
  });
});

describe("formatAmount", () => {
  it("keeps small numbers exact and shortens big ones", () => {
    expect(formatAmount(1.25)).toBe("1.3");
    expect(formatAmount(950)).toBe("950");
    expect(formatAmount(1240)).toBe("1,240");
    expect(formatAmount(12_400)).toBe("12.4k");
    expect(formatAmount(3_210_000)).toBe("3.2M");
    expect(formatAmount(-45_000)).toBe("-45.0k");
    expect(formatAmount(450_000)).toBe("450k");
  });

  it("shows rates with a sign", () => {
    expect(formatRate(39.6)).toBe("+40/min");
    expect(formatRate(-17.5)).toBe("-18/min");
  });
});

describe("plural", () => {
  it("uses the singular for one", () => {
    expect(plural(1, "building")).toBe("1 building");
    expect(plural(3, "building")).toBe("3 buildings");
    expect(plural(2, "company", "companies")).toBe("2 companies");
  });
});

describe("formatDuration in hours", () => {
  it("never shows 60 minutes", () => {
    expect(formatDuration(2 * 3600 - 1)).toBe("2 h");
    expect(formatDuration(3600 + 90 * 60 - 20)).toBe("2 h 30 min");
    expect(formatDuration(20 * 86400 + 5 * 3600)).toBe("20 d 5 h");
  });
});

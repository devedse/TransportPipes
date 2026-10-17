import { describe, expect, it } from "vitest";
import { displayName, formatDuration, formatVersion } from "./format";

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
  });
});

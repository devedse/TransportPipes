import { describe, expect, it } from "vitest";
import type { BuildingDefinition, GameContentDto } from "../api/contracts";
import { Content, roundHalfEven } from "./content";

/** Just what the prices read: a copy 7% more than the last, three parcel prices */
const content = new Content({
  buildings: [],
  resourceList: [],
  pipeTiers: [],
  research: [],
  tracks: [],
  progression: { costPerCopy: 1.07, parcelCosts: [10_000, 25_000, 62_500] },
} as unknown as GameContentDto);
const moily = { id: "windMoily", cost: { gold: 150 } } as unknown as BuildingDefinition;

describe("the prices shown", () => {
  it("round a half as the server does, to the even number", () => {
    expect([22.5, 23.5, 22.4, 22.6, 0.5, 1.5].map(roundHalfEven)).toEqual([22, 24, 22, 23, 0, 2]);
  });

  it("are the build price, a copy dearer than the last", () => {
    expect(content.buildCost(moily, 0)).toBe(150);
    expect(content.buildCost(moily, 3)).toBe(184);
  });

  it("take off the town wonder's discount, as the server takes the gold", () => {
    // 184 × 0.97 = 178.48: the build menu showed 184 and the server took 178
    expect(content.buildCost(moily, 3, 0.97)).toBe(178);
    expect(content.parcelCost(1, 0.97)).toBe(24_250);
    expect(content.parcelCost(1)).toBe(25_000);
  });
});

import { describe, expect, it } from "vitest";
import type { ConductorDto, FlowDto, NetworkFlowsDto } from "../api/contracts";
import { insidePipe } from "./inside";

const pipe = (x: number, y: number, tier = 1): ConductorDto => ({
  x,
  y,
  kind: "pipe",
  tier,
  owner: 1,
  network: 9,
});
const flow = (resource: string, sent: number, from = 1, to = 2): FlowDto => ({
  from,
  to,
  resource,
  sent,
  delivered: sent,
  kind: "internal",
  price: 1,
  path: [0, 0, 1, 0, 2, 0],
});

function lookup(conductors: ConductorDto[], flows: FlowDto[]) {
  const network: NetworkFlowsDto = { id: 9, rates: {}, flows };
  return {
    conductorAt: (x: number, y: number) => conductors.find((t) => t.x === x && t.y === y),
    flows: new Map([[9, network]]),
  };
}

const content = {
  resource: (id: string) => ({ kind: id === "energy" ? "power" : id === "water" ? "fluid" : "solid" }),
  pipeTiers: [
    { tier: 1, throughput: 100 },
    { tier: 2, throughput: 5000 },
  ],
};

describe("looking inside a pipe", () => {
  it("lists what runs in each lane and how full the pair's pipe is", () => {
    const world = lookup([pipe(0, 0), pipe(1, 0), pipe(2, 0)], [flow("ironOre", 30), flow("energy", 50)]);
    const inside = insidePipe(world, content, 1, 0);
    expect(inside.map((t) => [t.flow.resource, t.kind, t.full])).toEqual([
      ["energy", "power", 0.8],
      ["ironOre", "solid", 0.8],
    ]);
  });

  it("counts a pipe as full as its lowest tier on the way", () => {
    const world = lookup([pipe(0, 0, 2), pipe(1, 0, 1), pipe(2, 0, 2)], [flow("water", 100)]);
    expect(insidePipe(world, content, 0, 0)[0]!.full).toBe(1);
  });

  it("is empty off the flows' paths and off pipes", () => {
    const world = lookup([pipe(0, 0), pipe(1, 0), pipe(2, 0), pipe(5, 5)], [flow("ironOre", 30)]);
    expect(insidePipe(world, content, 5, 5)).toEqual([]);
    expect(insidePipe(world, content, 7, 7)).toEqual([]);
  });
});

import type { ConductorDto, FlowDto, NetworkFlowsDto } from "../api/contracts";

/** The three lanes of a pipe (1.4 §9): a belt for solid goods, a slim pipe for fluids, cables for energy */
export type LaneKind = "solid" | "fluid" | "power";

export const LANE_NAMES: Record<LaneKind, string> = { solid: "belt", fluid: "slim pipe", power: "cables" };

/** What runs inside a pipe tile: one good between two buildings, and how full their pipe is */
export interface InsideFlow {
  flow: FlowDto;
  kind: LaneKind;
  /** What the pair's pipe carries of its tier's throughput (all the pair's goods together), 0 to 1 */
  full: number;
}

interface Lookup {
  conductorAt(x: number, y: number): ConductorDto | undefined;
  readonly flows: ReadonlyMap<number, NetworkFlowsDto>;
}

interface Kinds {
  resource(id: string): { kind?: string } | undefined;
  readonly pipeTiers: ReadonlyArray<{ tier: number; throughput: number }>;
}

const passes = (flow: FlowDto, x: number, y: number) => {
  for (let i = 0; i + 1 < flow.path.length; i += 2) {
    if (flow.path[i] === x && flow.path[i + 1] === y) {
      return true;
    }
  }
  return false;
};

/**
 * How full each flow's pipe is: a pipe carries its tier's throughput between two buildings, all their goods together,
 * and as much as its lowest tier on the way
 */
export function fullness(world: Lookup, content: Kinds, network: NetworkFlowsDto): (flow: FlowDto) => number {
  const throughput = (tier: number) => content.pipeTiers.find((t) => t.tier === tier)?.throughput ?? 100;
  const pairKey = (t: FlowDto) => `${Math.min(t.from, t.to)}:${Math.max(t.from, t.to)}`;
  const carried = new Map<string, number>();
  for (const flow of network.flows) {
    carried.set(pairKey(flow), (carried.get(pairKey(flow)) ?? 0) + flow.sent);
  }
  return (flow) => {
    let lowest = Number.POSITIVE_INFINITY;
    for (let i = 0; i + 1 < flow.path.length; i += 2) {
      const tile = world.conductorAt(flow.path[i]!, flow.path[i + 1]!);
      if (tile !== undefined && tile.kind === "pipe") {
        lowest = Math.min(lowest, throughput(tile.tier));
      }
    }
    return Number.isFinite(lowest) ? Math.min(1, (carried.get(pairKey(flow)) ?? 0) / lowest) : 0;
  };
}

/** The lane a good runs in */
export const laneOf = (content: Kinds, resource: string): LaneKind =>
  (content.resource(resource)?.kind ?? "solid") as LaneKind;

/** The flows through a pipe tile, lane by lane, the biggest first (look inside, 1.4 §9) */
export function insidePipe(world: Lookup, content: Kinds, x: number, y: number): InsideFlow[] {
  const conductor = world.conductorAt(x, y);
  if (conductor === undefined || conductor.kind !== "pipe" || conductor.network === null) {
    return [];
  }
  const network = world.flows.get(conductor.network);
  if (network === undefined) {
    return [];
  }
  const full = fullness(world, content, network);
  return network.flows
    .filter((t) => t.delivered > 0 && passes(t, x, y))
    .map((flow) => ({ flow, kind: laneOf(content, flow.resource), full: full(flow) }))
    .sort((a, b) => b.flow.delivered - a.flow.delivered);
}

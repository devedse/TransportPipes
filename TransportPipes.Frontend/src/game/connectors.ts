import type { FlowDto, NetworkFlowsDto } from "../api/contracts";
import type { LaneKind } from "./inside";

export type PipeSide = "u" | "r" | "d" | "l";
export const PIPE_SIDES: Array<[PipeSide, number, number]> = [
  ["u", 0, -1],
  ["r", 1, 0],
  ["d", 0, 1],
  ["l", -1, 0],
];
export const LANE_KINDS: LaneKind[] = ["solid", "fluid", "power"];
export const LANE_SLOTS: Record<number, number[]> = { 1: [1], 2: [0, 2], 3: [0, 1, 2] };
export const LANE_ACROSS = [0.27, 0.5, 0.73];

export interface ConnectorFlow {
  resource: string;
  outgoing: boolean;
  rate: number;
}

export const connectorKey = (x: number, y: number, building: number) => `${x},${y}:${building}`;

// Conservative projected silhouette of art/pipes.mjs's receiving box: half-width .2, height .444.
const inletHeight = 0.444 * 117.58;
const inletHull = [
  [96, -48 - 19.2 - inletHeight],
  [134.4, -48 - inletHeight],
  [134.4, -48],
  [96, -28.8],
  [57.6, -48],
  [57.6, -48 - inletHeight],
];

/** The raised inlet belongs to the gate tile, including its roof above the ground diamond. */
export function gateInletContains(x: number, y: number, tolerance = 0): boolean {
  return inletHull.every((a, i) => {
    const b = inletHull[(i + 1) % inletHull.length]!;
    const dx = b[0]! - a[0]!;
    const dy = b[1]! - a[1]!;
    return dx * (y - a[1]!) - dy * (x - a[0]!) >= -tolerance * Math.hypot(dx, dy);
  });
}

/** Actual traffic at the two building ends of each flow. Outgoing uses sent; incoming uses delivered after losses. */
export function connectorTraffic(networks: Iterable<NetworkFlowsDto>): Map<string, ConnectorFlow[]> {
  const result = new Map<string, ConnectorFlow[]>();
  const add = (flow: FlowDto, building: number, at: number, outgoing: boolean) => {
    const rate = outgoing ? flow.sent : flow.delivered;
    if (building <= 0 || rate <= 1e-6) return;
    const key = connectorKey(flow.path[at]!, flow.path[at + 1]!, building);
    let rows = result.get(key);
    if (rows === undefined) result.set(key, (rows = []));
    const existing = rows.find((r) => r.resource === flow.resource && r.outgoing === outgoing);
    if (existing !== undefined) existing.rate += rate;
    else rows.push({ resource: flow.resource, outgoing, rate });
  };
  for (const network of networks)
    for (const flow of network.flows) {
      if (flow.path.length < 2 || flow.path.length % 2 !== 0) continue;
      add(flow, flow.from, 0, true);
      add(flow, flow.to, flow.path.length - 2, false);
    }
  return result;
}

export interface ConnectorPoint {
  x: number;
  y: number;
}

/** The connector art's exact geometry, relative to its pipe tile (art/pipes.mjs). Offset separates the two directions. */
export function connectorPath(side: PipeSide, slot: number, kind: LaneKind, offset = 0): ConnectorPoint[] {
  const across = LANE_ACROSS[slot]! + offset;
  const positive = side === "r" || side === "d";
  const at = (distance: number, z: number) => {
    const along = (positive ? 1 : 0) + (positive ? -distance : distance);
    const [u, v] = side === "r" || side === "l" ? [along, across] : [across, along];
    return { x: 96 + (u! - v!) * 96, y: (u! + v!) * 48 - 96 - z * 117.58 };
  };
  if (kind === "power") return [at(0.3936, 0.175), at(0.22, 0.2), at(0.012, 0.15)];
  const z = kind === "solid" ? 0.12 : 0.165;
  return [at(0.4036, z), at(0.012, z)];
}

/** A fixed, small sprite budget. More traffic adds packets; nothing moves on an idle connector. */
export const connectorDensity = (rate: number) =>
  rate <= 1e-6 ? 0 : Math.min(3, Math.max(1, Math.ceil(Math.log2(1 + rate) / 3)));

/** Deterministic weighted choice: a busier resource appears more often, without counting displayed packets as units. */
export function packetResource(rows: ConnectorFlow[], cycle: number): string {
  const total = rows.reduce((sum, row) => sum + row.rate, 0);
  let pick = ((((cycle * 0.61803398875) % 1) + 1) % 1) * total;
  for (const row of rows) {
    pick -= row.rate;
    if (pick < 0) return row.resource;
  }
  return rows[rows.length - 1]?.resource ?? "";
}

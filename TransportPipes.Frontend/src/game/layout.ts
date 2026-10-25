// The fixed geometry of the world (1.4 §4), the same as TransportPipes.Game/World/WorldLayout.cs. A town is 196×164
// tiles: four Yard groups of 96×80 around the hub, two roads four tiles wide crossing in the middle, and the outskirts in
// the corners of the groups; 128 tiles of wilderness lie between towns. A Yard group is a Yard of 32×16 with a plot above
// and below it and one at each end.

export const ESTATE_SIZE = 32;
export const PARCEL_SIZE = 8;
export const PARCELS_PER_SIDE = ESTATE_SIZE / PARCEL_SIZE;
export const PARCEL_COUNT = PARCELS_PER_SIDE * PARCELS_PER_SIDE;
export const CHUNK_SIZE = 32;

export const YARD_WIDTH = 32;
export const YARD_HEIGHT = 16;
export const ROAD_WIDTH = 4;
export const GROUP_WIDTH = 96;
export const GROUP_HEIGHT = 80;

export const DISTRICT_WIDTH = 2 * GROUP_WIDTH + ROAD_WIDTH;
export const DISTRICT_HEIGHT = 2 * GROUP_HEIGHT + ROAD_WIDTH;
export const WILDERNESS_WIDTH = 128;
export const DISTRICT_PITCH_X = DISTRICT_WIDTH + WILDERNESS_WIDTH;
export const DISTRICT_PITCH_Y = DISTRICT_HEIGHT + WILDERNESS_WIDTH;

/** Town-local: the vertical road's columns and the horizontal road's rows */
export const ROAD_X = GROUP_WIDTH;
export const ROAD_Y = GROUP_HEIGHT;
export const ORIGIN_OFFSET_X = ROAD_X + ROAD_WIDTH / 2;
export const ORIGIN_OFFSET_Y = ROAD_Y + ROAD_WIDTH / 2;

export const STATIONS_PER_DISTRICT = 4;
export const ESTATES_PER_DISTRICT = STATIONS_PER_DISTRICT * 4;

export interface TileRect {
  x0: number;
  y0: number;
  x1: number;
  y1: number;
}

/** The hub (town-local): the market, the Wonder site and the statues, across the road crossing */
export const HUB_LOCAL: TileRect = { x0: 64, y0: 56, x1: 131, y1: 107 };

/** The Deep Level lies far away in the same coordinates: a deep tile is its surface tile shifted down by this */
export const DEEP_OFFSET = DISTRICT_PITCH_Y * CHUNK_SIZE * 100;

export function isDeep(y: number): boolean {
  return y >= DEEP_OFFSET / 2;
}

export type ZoneKind = "estate" | "hub" | "corridor" | "wilderness" | "yard" | "outskirts";

/** Which side of its Yard a plot lies on */
export type PlotSide = "north" | "west" | "east" | "south";
const SIDES: PlotSide[] = ["north", "west", "east", "south"];

export interface Zone {
  /** On a plot its slot, in a Yard its group's index, in the outskirts the plot it is set aside for */
  kind: ZoneKind;
  districtX: number;
  districtY: number;
  slot: number;
  /** Plot-, hub- or Yard-local; town-local on roads, the outskirts and the wilderness */
  localX: number;
  localY: number;
}

export function floorDiv(a: number, b: number): number {
  return Math.floor(a / b);
}

export function districtOriginX(districtX: number): number {
  return districtX * DISTRICT_PITCH_X - ORIGIN_OFFSET_X;
}

export function districtOriginY(districtY: number): number {
  return districtY * DISTRICT_PITCH_Y - ORIGIN_OFFSET_Y;
}

export function districtOf(x: number, y: number): { x: number; y: number } {
  return {
    x: floorDiv(x + ORIGIN_OFFSET_X, DISTRICT_PITCH_X),
    y: floorDiv(y + ORIGIN_OFFSET_Y, DISTRICT_PITCH_Y),
  };
}

// ---------------------------------------------------------------- Yard groups

/** A group's corner, town-local (0 NW, 1 NE, 2 SW, 3 SE) */
export function groupOrigin(station: number): { x: number; y: number } {
  return {
    x: station % 2 === 0 ? 0 : GROUP_WIDTH + ROAD_WIDTH,
    y: station < 2 ? 0 : GROUP_HEIGHT + ROAD_WIDTH,
  };
}

const SIDE_ORIGIN: Record<PlotSide, [number, number]> = {
  north: [32, 0],
  west: [0, 24],
  east: [64, 24],
  south: [32, 48],
};

/** The Yard within its group */
const YARD_IN_GROUP: TileRect = { x0: 32, y0: 32, x1: 32 + YARD_WIDTH - 1, y1: 32 + YARD_HEIGHT - 1 };

export function sideOf(slot: number): PlotSide {
  return SIDES[((slot % 4) + 4) % 4]!;
}

export function stationOfSlot(slot: number): number {
  return Math.floor(slot / 4);
}

/** The plots of a Yard group, in slot order (north, west, east, south) */
export function stationSlots(station: number): number[] {
  return [station * 4, station * 4 + 1, station * 4 + 2, station * 4 + 3];
}

const STATION_NAMES = ["NW", "NE", "SW", "SE"];

export function stationName(station: number): string {
  return STATION_NAMES[station] ?? "";
}

/** A plot's corner, town-local */
export function estateLocal(slot: number): { x: number; y: number } {
  const group = groupOrigin(stationOfSlot(slot));
  const [sx, sy] = SIDE_ORIGIN[sideOf(slot)];
  return { x: group.x + sx, y: group.y + sy };
}

export function estateOrigin(districtX: number, districtY: number, slot: number): { x: number; y: number } {
  const local = estateLocal(slot);
  return { x: districtOriginX(districtX) + local.x, y: districtOriginY(districtY) + local.y };
}

function offset(rect: TileRect, dx: number, dy: number): TileRect {
  return { x0: rect.x0 + dx, y0: rect.y0 + dy, x1: rect.x1 + dx, y1: rect.y1 + dy };
}

function contains(rect: TileRect, x: number, y: number): boolean {
  return x >= rect.x0 && x <= rect.x1 && y >= rect.y0 && y <= rect.y1;
}

/** A Yard, town-local */
export function yardLocal(station: number): TileRect {
  const group = groupOrigin(station);
  return offset(YARD_IN_GROUP, group.x, group.y);
}

/** A Yard in world coordinates */
export function yardRect(districtX: number, districtY: number, station: number): TileRect {
  return offset(yardLocal(station), districtOriginX(districtX), districtOriginY(districtY));
}

/** The hub in world coordinates */
export function hubRect(districtX: number, districtY: number): TileRect {
  return offset(HUB_LOCAL, districtOriginX(districtX), districtOriginY(districtY));
}

// ---------------------------------------------------------------- A plot's own places

/** A plot's Yard gate (plot-local): the middle of the edge it shares with its Yard */
export function yardGateLocal(slot: number): { x: number; y: number } {
  switch (sideOf(slot)) {
    case "north":
      return { x: 16, y: ESTATE_SIZE - 1 };
    case "west":
      return { x: ESTATE_SIZE - 1, y: 16 };
    case "east":
      return { x: 0, y: 16 };
    default:
      return { x: 16, y: 0 };
  }
}

/** A plot's Yard gate in world coordinates */
export function yardGate(estateX: number, estateY: number, slot: number): { x: number; y: number } {
  const local = yardGateLocal(slot);
  return { x: estateX + local.x, y: estateY + local.y };
}

/** A tile in a plot's own frame: as far along its Yard edge and as deep from it as it lies */
export function toYardFrame(side: PlotSide, x: number, y: number, size = ESTATE_SIZE): [number, number] {
  switch (side) {
    case "north":
      return [x, size - 1 - y];
    case "south":
      return [x, y];
    case "west":
      return [y, size - 1 - x];
    default:
      return [y, x];
  }
}

export function fromYardFrame(
  side: PlotSide,
  along: number,
  depth: number,
  size = ESTATE_SIZE,
): [number, number] {
  switch (side) {
    case "north":
      return [along, size - 1 - depth];
    case "south":
      return [along, depth];
    case "west":
      return [size - 1 - depth, along];
    default:
      return [depth, along];
  }
}

/** A footprint's corner on another side's plot (plot-local) */
export function moveFootprint(
  from: PlotSide,
  to: PlotSide,
  x: number,
  y: number,
  size: number,
): [number, number] {
  let minX = Infinity;
  let minY = Infinity;
  for (const [cx, cy] of [
    [x, y],
    [x + size - 1, y + size - 1],
  ] as const) {
    const [along, depth] = toYardFrame(from, cx, cy);
    const [tx, ty] = fromYardFrame(to, along, depth);
    minX = Math.min(minX, tx);
    minY = Math.min(minY, ty);
  }
  return [minX, minY];
}

/** A plot's start area: the four parcels against the Yard, around the gate */
export function startParcels(slot: number): number[] {
  switch (sideOf(slot)) {
    case "north":
      return [9, 10, 13, 14];
    case "west":
      return [6, 7, 10, 11];
    case "east":
      return [4, 5, 8, 9];
    default:
      return [1, 2, 5, 6];
  }
}

export function startMask(slot: number): number {
  return startParcels(slot).reduce((mask, parcel) => mask | (1 << parcel), 0);
}

/** Where the HQ (2×2) stands on a plot: next to the Yard gate */
export function hqLocal(slot: number): { x: number; y: number } {
  const side = sideOf(slot);
  const gate = yardGateLocal(slot);
  const [along] = toYardFrame(side, gate.x, gate.y);
  const [x, y] = moveFootprint("south", side, along - 3, 1, 2);
  return { x, y };
}

/** Where the trading post stands: the other side of the gate */
export function tradingPostLocal(slot: number): { x: number; y: number } {
  const side = sideOf(slot);
  const gate = yardGateLocal(slot);
  const [along] = toYardFrame(side, gate.x, gate.y);
  const [x, y] = fromYardFrame(side, along + 2, 1);
  return { x, y };
}

export function parcelOf(localX: number, localY: number): number {
  return Math.floor(localY / PARCEL_SIZE) * PARCELS_PER_SIDE + Math.floor(localX / PARCEL_SIZE);
}

export function parcelOrigin(parcel: number): [number, number] {
  return [(parcel % PARCELS_PER_SIDE) * PARCEL_SIZE, Math.floor(parcel / PARCELS_PER_SIDE) * PARCEL_SIZE];
}

export function isParcelUnlocked(mask: number, parcel: number): boolean {
  return (mask & (1 << parcel)) !== 0;
}

/** Parcels next to the ones you own that are not yours yet */
export function buyableParcels(mask: number): number[] {
  const result: number[] = [];
  for (let parcel = 0; parcel < PARCEL_COUNT; parcel++) {
    if (isParcelUnlocked(mask, parcel)) {
      continue;
    }
    const px = parcel % PARCELS_PER_SIDE;
    const py = Math.floor(parcel / PARCELS_PER_SIDE);
    const neighbours: Array<[number, number]> = [
      [px - 1, py],
      [px + 1, py],
      [px, py - 1],
      [px, py + 1],
    ];
    const inside = neighbours.filter(
      ([x, y]) => x >= 0 && y >= 0 && x < PARCELS_PER_SIDE && y < PARCELS_PER_SIDE,
    );
    if (inside.some(([x, y]) => isParcelUnlocked(mask, y * PARCELS_PER_SIDE + x))) {
      result.push(parcel);
    }
  }
  return result;
}

// ---------------------------------------------------------------- The outskirts

/** The outskirts parcels a plot has set aside (town-local corners), in the order they are bought */
export function outskirtsParcels(slot: number): Array<{ x: number; y: number }> {
  return OUTSKIRTS_OF[slot] ?? [];
}

const OUTSKIRTS_OF = buildOutskirts();

function buildOutskirts(): Array<Array<{ x: number; y: number }>> {
  // The NW group's allotment, group-local, by side; the other groups are its mirror images
  const nw: Record<PlotSide, Array<[number, number]>> = { north: [], west: [], east: [], south: [] };
  for (let row = 0; row < 3; row++) {
    for (let col = 0; col < 4; col++) {
      nw[col >= 2 ? "north" : "west"].push([col * 8, row * 8]);
      nw[col === 0 ? "north" : "east"].push([64 + col * 8, row * 8]);
      nw[col === 0 ? "west" : "south"].push([col * 8, 56 + row * 8]);
    }
  }
  const result: Array<Array<{ x: number; y: number }>> = [];
  for (let station = 0; station < STATIONS_PER_DISTRICT; station++) {
    const mirrorX = station % 2 === 1;
    const mirrorY = station >= 2;
    const group = groupOrigin(station);
    for (const side of SIDES) {
      const mirrored: PlotSide =
        side === "west" && mirrorX
          ? "east"
          : side === "east" && mirrorX
            ? "west"
            : side === "north" && mirrorY
              ? "south"
              : side === "south" && mirrorY
                ? "north"
                : side;
      result[station * 4 + SIDES.indexOf(mirrored)] = nw[side].map(([x, y]) => ({
        x: group.x + (mirrorX ? GROUP_WIDTH - PARCEL_SIZE - x : x),
        y: group.y + (mirrorY ? GROUP_HEIGHT - PARCEL_SIZE - y : y),
      }));
    }
  }
  return result;
}

/** The outskirts parcels by their town-local corner: (slot, its index 0..8) */
const OUTSKIRTS_AT = new Map<string, { slot: number; index: number }>(
  OUTSKIRTS_OF.flatMap((parcels, slot) =>
    parcels.map((t, index) => [`${t.x},${t.y}`, { slot, index }] as const),
  ),
);

/** Who an outskirts tile (town-local) is set aside for, or null */
export function outskirtsOwner(localX: number, localY: number): { slot: number; index: number } | null {
  if (localX < 0 || localY < 0 || localX >= DISTRICT_WIDTH || localY >= DISTRICT_HEIGHT) {
    return null;
  }
  const station = (localY < ROAD_Y ? 0 : 2) + (localX < ROAD_X ? 0 : 1);
  const group = groupOrigin(station);
  const gx = localX - group.x;
  const gy = localY - group.y;
  if (gx < 0 || gy < 0) {
    return null;
  }
  const cornerX = group.x + Math.floor(gx / PARCEL_SIZE) * PARCEL_SIZE;
  const cornerY = group.y + Math.floor(gy / PARCEL_SIZE) * PARCEL_SIZE;
  return OUTSKIRTS_AT.get(`${cornerX},${cornerY}`) ?? null;
}

// ---------------------------------------------------------------- Town gates

/** The four town gates (town-local): where the roads leave the town (top, bottom, left, right) */
export const TOWN_GATES_LOCAL: TileRect[] = [
  { x0: ROAD_X - 2, y0: 0, x1: ROAD_X + ROAD_WIDTH + 1, y1: 1 },
  { x0: ROAD_X - 2, y0: DISTRICT_HEIGHT - 2, x1: ROAD_X + ROAD_WIDTH + 1, y1: DISTRICT_HEIGHT - 1 },
  { x0: 0, y0: ROAD_Y - 2, x1: 1, y1: ROAD_Y + ROAD_WIDTH + 1 },
  { x0: DISTRICT_WIDTH - 2, y0: ROAD_Y - 2, x1: DISTRICT_WIDTH - 1, y1: ROAD_Y + ROAD_WIDTH + 1 },
];

// ---------------------------------------------------------------- Zones

export function zoneAt(x: number, y: number): Zone {
  const district = districtOf(x, y);
  const lx = x - districtOriginX(district.x);
  const ly = y - districtOriginY(district.y);
  const base = { districtX: district.x, districtY: district.y };
  if (lx >= DISTRICT_WIDTH || ly >= DISTRICT_HEIGHT) {
    return { kind: "wilderness", ...base, slot: -1, localX: lx, localY: ly };
  }
  if (contains(HUB_LOCAL, lx, ly)) {
    return { kind: "hub", ...base, slot: -1, localX: lx - HUB_LOCAL.x0, localY: ly - HUB_LOCAL.y0 };
  }
  if ((lx >= ROAD_X && lx < ROAD_X + ROAD_WIDTH) || (ly >= ROAD_Y && ly < ROAD_Y + ROAD_WIDTH)) {
    return { kind: "corridor", ...base, slot: -1, localX: lx, localY: ly };
  }
  const station = (ly < ROAD_Y ? 0 : 2) + (lx < ROAD_X ? 0 : 1);
  const group = groupOrigin(station);
  const gx = lx - group.x;
  const gy = ly - group.y;
  for (let side = 0; side < 4; side++) {
    const [sx, sy] = SIDE_ORIGIN[SIDES[side]!];
    if (gx >= sx && gx < sx + ESTATE_SIZE && gy >= sy && gy < sy + ESTATE_SIZE) {
      return { kind: "estate", ...base, slot: station * 4 + side, localX: gx - sx, localY: gy - sy };
    }
  }
  if (contains(YARD_IN_GROUP, gx, gy)) {
    return {
      kind: "yard",
      ...base,
      slot: station,
      localX: gx - YARD_IN_GROUP.x0,
      localY: gy - YARD_IN_GROUP.y0,
    };
  }
  const owner = outskirtsOwner(lx, ly);
  return owner !== null
    ? { kind: "outskirts", ...base, slot: owner.slot, localX: lx, localY: ly }
    : { kind: "wilderness", ...base, slot: -1, localX: lx, localY: ly };
}

export function chunkOf(x: number, y: number): { x: number; y: number } {
  return { x: floorDiv(x, CHUNK_SIZE), y: floorDiv(y, CHUNK_SIZE) };
}

export function tileKey(x: number, y: number): number {
  // Unique for |x|, |y| < 2^20
  return (x + 1048576) * 2097152 + (y + 1048576);
}

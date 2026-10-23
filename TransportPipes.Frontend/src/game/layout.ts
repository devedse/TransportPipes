// The fixed geometry of the world, the same as TransportPipes.Game/World/WorldLayout.cs.

export const ESTATE_SIZE = 32;
export const CORRIDOR_WIDTH = 2;
export const BLOCKS_PER_SIDE = 5;
export const PARCEL_SIZE = 8;
export const PARCELS_PER_SIDE = ESTATE_SIZE / PARCEL_SIZE;
export const WILDERNESS_WIDTH = 30;
export const CHUNK_SIZE = 32;
export const BLOCK_PITCH = ESTATE_SIZE + CORRIDOR_WIDTH;
export const DISTRICT_SIZE = CORRIDOR_WIDTH + BLOCKS_PER_SIDE * BLOCK_PITCH;
export const DISTRICT_PITCH = DISTRICT_SIZE + WILDERNESS_WIDTH;
export const HUB_BLOCK = Math.floor(BLOCKS_PER_SIDE / 2);
export const ORIGIN_OFFSET = CORRIDOR_WIDTH + HUB_BLOCK * BLOCK_PITCH + ESTATE_SIZE / 2;

/** The Deep Level lies far away in the same coordinates: a deep tile is its surface tile shifted down by this */
export const DEEP_OFFSET = DISTRICT_PITCH * CHUNK_SIZE * 150;

export function isDeep(y: number): boolean {
  return y >= DEEP_OFFSET / 2;
}

export type ZoneKind = "estate" | "hub" | "corridor" | "wilderness" | "yard";

export interface Zone {
  /** In a Yard, slot is the station's index */
  kind: ZoneKind;
  districtX: number;
  districtY: number;
  slot: number;
  localX: number;
  localY: number;
}

export function floorDiv(a: number, b: number): number {
  return Math.floor(a / b);
}

export function districtOriginX(districtX: number): number {
  return districtX * DISTRICT_PITCH - ORIGIN_OFFSET;
}

export function districtOriginY(districtY: number): number {
  return districtY * DISTRICT_PITCH - ORIGIN_OFFSET;
}

export function districtOf(x: number, y: number): { x: number; y: number } {
  return { x: floorDiv(x + ORIGIN_OFFSET, DISTRICT_PITCH), y: floorDiv(y + ORIGIN_OFFSET, DISTRICT_PITCH) };
}

function blockAxis(local: number): [number, number] {
  const inner = local - CORRIDOR_WIDTH;
  if (inner < 0) {
    return [-1, 0];
  }
  const block = Math.floor(inner / BLOCK_PITCH);
  const offset = inner % BLOCK_PITCH;
  return offset >= ESTATE_SIZE || block >= BLOCKS_PER_SIDE ? [-1, 0] : [block, offset];
}

export function slotOf(blockX: number, blockY: number): number {
  const raw = blockY * BLOCKS_PER_SIDE + blockX;
  const hubRaw = HUB_BLOCK * BLOCKS_PER_SIDE + HUB_BLOCK;
  return raw > hubRaw ? raw - 1 : raw;
}

export function blockOfSlot(slot: number): [number, number] {
  const hubRaw = HUB_BLOCK * BLOCKS_PER_SIDE + HUB_BLOCK;
  const raw = slot >= hubRaw ? slot + 1 : slot;
  return [raw % BLOCKS_PER_SIDE, Math.floor(raw / BLOCKS_PER_SIDE)];
}

export function zoneAt(x: number, y: number): Zone {
  const district = districtOf(x, y);
  const lx = x - districtOriginX(district.x);
  const ly = y - districtOriginY(district.y);
  const base = { districtX: district.x, districtY: district.y };
  if (lx >= DISTRICT_SIZE || ly >= DISTRICT_SIZE) {
    return { kind: "wilderness", ...base, slot: -1, localX: lx, localY: ly };
  }
  const station = yardAtLocal(lx, ly);
  if (station >= 0) {
    const yard = YARDS_LOCAL[station]!;
    return { kind: "yard", ...base, slot: station, localX: lx - yard.x0, localY: ly - yard.y0 };
  }
  const [bx, ox] = blockAxis(lx);
  const [by, oy] = blockAxis(ly);
  if (bx < 0 || by < 0) {
    return { kind: "corridor", ...base, slot: -1, localX: lx, localY: ly };
  }
  if (bx === HUB_BLOCK && by === HUB_BLOCK) {
    return { kind: "hub", ...base, slot: -1, localX: ox, localY: oy };
  }
  return { kind: "estate", ...base, slot: slotOf(bx, by), localX: ox, localY: oy };
}

export function blockOrigin(
  districtX: number,
  districtY: number,
  blockX: number,
  blockY: number,
): { x: number; y: number } {
  return {
    x: districtOriginX(districtX) + CORRIDOR_WIDTH + blockX * BLOCK_PITCH,
    y: districtOriginY(districtY) + CORRIDOR_WIDTH + blockY * BLOCK_PITCH,
  };
}

export function estateOrigin(districtX: number, districtY: number, slot: number): { x: number; y: number } {
  const [bx, by] = blockOfSlot(slot);
  return blockOrigin(districtX, districtY, bx, by);
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
  for (let parcel = 0; parcel < PARCELS_PER_SIDE * PARCELS_PER_SIDE; parcel++) {
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

export function chunkOf(x: number, y: number): { x: number; y: number } {
  return { x: floorDiv(x, CHUNK_SIZE), y: floorDiv(y, CHUNK_SIZE) };
}

export function tileKey(x: number, y: number): number {
  // Unique for |x|, |y| < 2^20
  return (x + 1048576) * 2097152 + (y + 1048576);
}

// ---------------------------------------------------------------- Stations and Yards (v1.2 §4)

export const STATIONS_PER_DISTRICT = 8;
export const YARD_SIZE = 10;
const YARD_DEPTH = (YARD_SIZE - CORRIDOR_WIDTH) / 2;
const SMALL_YARD_OFFSET = 20;
const STATION_NAMES = ["NW", "NE", "SW", "SE", "N", "S", "W", "E"];

/** Stations by their blocks: 0 NW, 1 NE, 2 SW, 3 SE (2×2 estates each), 4 N, 5 S, 6 W, 7 E (two estates on the hub's arms) */
const STATION_BLOCKS: Array<[number, number, number, number]> = [
  [0, 0, 1, 1],
  [3, 0, 4, 1],
  [0, 3, 1, 4],
  [3, 3, 4, 4],
  [2, 0, 2, 1],
  [2, 3, 2, 4],
  [0, 2, 1, 2],
  [3, 2, 4, 2],
];

export interface TileRect {
  x0: number;
  y0: number;
  x1: number;
  y1: number;
}

export function stationOfSlot(slot: number): number {
  const [bx, by] = blockOfSlot(slot);
  return STATION_BLOCKS.findIndex(([x0, y0, x1, y1]) => bx >= x0 && bx <= x1 && by >= y0 && by <= y1);
}

export function stationSlots(station: number): number[] {
  const [x0, y0, x1, y1] = STATION_BLOCKS[station]!;
  const slots: number[] = [];
  for (let by = y0; by <= y1; by++) {
    for (let bx = x0; bx <= x1; bx++) {
      slots.push(slotOf(bx, by));
    }
  }
  return slots;
}

export function stationName(station: number): string {
  return STATION_NAMES[station] ?? "";
}

function corridorAfter(block: number): number {
  return CORRIDOR_WIDTH + block * BLOCK_PITCH + ESTATE_SIZE;
}

/** A station's Yard in district-local coordinates */
export function yardLocal(station: number): TileRect {
  const [x0, y0, x1, y1] = STATION_BLOCKS[station]!;
  let yx: number;
  let yy: number;
  if (x0 !== x1 && y0 !== y1) {
    yx = corridorAfter(x0) - YARD_DEPTH;
    yy = corridorAfter(y0) - YARD_DEPTH;
  } else if (x0 === x1) {
    yx = CORRIDOR_WIDTH + x0 * BLOCK_PITCH + SMALL_YARD_OFFSET;
    yy = corridorAfter(y0) - YARD_DEPTH;
  } else {
    yx = corridorAfter(x0) - YARD_DEPTH;
    yy = CORRIDOR_WIDTH + y0 * BLOCK_PITCH + SMALL_YARD_OFFSET;
  }
  return { x0: yx, y0: yy, x1: yx + YARD_SIZE - 1, y1: yy + YARD_SIZE - 1 };
}

const YARDS_LOCAL = Array.from({ length: STATIONS_PER_DISTRICT }, (_, i) => yardLocal(i));

function yardAtLocal(lx: number, ly: number): number {
  return YARDS_LOCAL.findIndex((t) => lx >= t.x0 && lx <= t.x1 && ly >= t.y0 && ly <= t.y1);
}

/** A station's Yard in world coordinates */
export function yardRect(districtX: number, districtY: number, station: number): TileRect {
  const local = YARDS_LOCAL[station]!;
  const ox = districtOriginX(districtX);
  const oy = districtOriginY(districtY);
  return { x0: local.x0 + ox, y0: local.y0 + oy, x1: local.x1 + ox, y1: local.y1 + oy };
}

/** The part of an estate the Yard takes (estate-local) */
export function yardPart(slot: number): TileRect {
  const yard = YARDS_LOCAL[stationOfSlot(slot)]!;
  const [bx, by] = blockOfSlot(slot);
  const ox = CORRIDOR_WIDTH + bx * BLOCK_PITCH;
  const oy = CORRIDOR_WIDTH + by * BLOCK_PITCH;
  return {
    x0: Math.max(yard.x0, ox) - ox,
    y0: Math.max(yard.y0, oy) - oy,
    x1: Math.min(yard.x1, ox + ESTATE_SIZE - 1) - ox,
    y1: Math.min(yard.y1, oy + ESTATE_SIZE - 1) - oy,
  };
}

/** An estate's Yard gate (estate-local): right next to the Yard's part of the estate, on the side of its middle */
export function yardGateLocal(slot: number): { x: number; y: number } {
  const part = yardPart(slot);
  const middle = ESTATE_SIZE / 2;
  if (part.x1 - part.x0 + 1 > YARD_DEPTH) {
    return { x: Math.floor((part.x0 + part.x1) / 2), y: part.y0 > middle ? part.y0 - 1 : part.y1 + 1 };
  }
  if (part.y1 - part.y0 + 1 > YARD_DEPTH) {
    return { x: part.x0 > middle ? part.x0 - 1 : part.x1 + 1, y: Math.floor((part.y0 + part.y1) / 2) };
  }
  return { x: part.x0 > middle ? part.x0 - 1 : part.x1 + 1, y: part.y0 > middle ? part.y0 : part.y1 };
}

/**
 * The outline of an estate without its part of the Yard, as corners of the tile grid (estate-local, clockwise from the
 * estate's top corner): the estate's borders go around the Yard
 */
export function estateOutline(slot: number): Array<[number, number]> {
  const p = yardPart(slot);
  const s = ESTATE_SIZE;
  const [x0, y0, x1, y1] = [p.x0, p.y0, p.x1 + 1, p.y1 + 1];
  const top = p.y0 === 0;
  const bottom = p.y1 === s - 1;
  const left = p.x0 === 0;
  const right = p.x1 === s - 1;
  if (bottom && right) {
    return [
      [0, 0],
      [s, 0],
      [s, y0],
      [x0, y0],
      [x0, s],
      [0, s],
    ];
  }
  if (bottom && left) {
    return [
      [0, 0],
      [s, 0],
      [s, s],
      [x1, s],
      [x1, y0],
      [0, y0],
    ];
  }
  if (top && right) {
    return [
      [0, 0],
      [x0, 0],
      [x0, y1],
      [s, y1],
      [s, s],
      [0, s],
    ];
  }
  if (top && left) {
    return [
      [x1, 0],
      [s, 0],
      [s, s],
      [0, s],
      [0, y1],
      [x1, y1],
    ];
  }
  if (bottom) {
    return [
      [0, 0],
      [s, 0],
      [s, s],
      [x1, s],
      [x1, y0],
      [x0, y0],
      [x0, s],
      [0, s],
    ];
  }
  if (top) {
    return [
      [0, 0],
      [x0, 0],
      [x0, y1],
      [x1, y1],
      [x1, 0],
      [s, 0],
      [s, s],
      [0, s],
    ];
  }
  if (right) {
    return [
      [0, 0],
      [s, 0],
      [s, y0],
      [x0, y0],
      [x0, y1],
      [s, y1],
      [s, s],
      [0, s],
    ];
  }
  return [
    [0, 0],
    [s, 0],
    [s, s],
    [0, s],
    [0, y1],
    [x1, y1],
    [x1, y0],
    [0, y0],
  ];
}

/** An estate's Yard gate in world coordinates */
export function yardGate(estateX: number, estateY: number, slot: number): { x: number; y: number } {
  const local = yardGateLocal(slot);
  return { x: estateX + local.x, y: estateY + local.y };
}

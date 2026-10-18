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

export type ZoneKind = "estate" | "hub" | "corridor" | "wilderness";

export interface Zone {
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

export const GATE_SIDES = ["top", "right", "bottom", "left"] as const;

const GATE_LOCAL: Record<(typeof GATE_SIDES)[number], [number, number, number, number]> = {
  // x, y within the estate, and the step to the corridor tile outside it
  top: [ESTATE_SIZE / 2, 0, 0, -1],
  right: [ESTATE_SIZE - 1, ESTATE_SIZE / 2, 1, 0],
  bottom: [ESTATE_SIZE / 2, ESTATE_SIZE - 1, 0, 1],
  left: [0, ESTATE_SIZE / 2, -1, 0],
};

/** The gate of an estate on a side, and the corridor tile right outside it */
export function gateTiles(estateX: number, estateY: number, side: (typeof GATE_SIDES)[number]) {
  const [x, y, dx, dy] = GATE_LOCAL[side];
  return { gate: { x: estateX + x, y: estateY + y }, outside: { x: estateX + x + dx, y: estateY + y + dy } };
}

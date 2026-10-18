import type { BuildingDefinition, CompanyDto, EstateDto } from "../api/contracts";
import type { Content } from "./content";
import { ESTATE_SIZE, isParcelUnlocked, parcelOf, zoneAt } from "./layout";
import type { WorldModel } from "./world";

// The placement rules of the server (GameWorld.CanPlace, PlacePipes), so the build ghost and the pipe tool can show
// what is allowed before asking.

export interface Placement {
  ok: boolean;
  reason: string | null;
  /** How well the building produces at this spot (wind, sun, deposit, forest) */
  multiplier: number;
}

export function depositMatches(wanted: string, actual: string | null): boolean {
  return (
    actual !== null &&
    (wanted === actual ||
      (wanted === "anyMetal" && (actual === "ironOre" || actual === "copperOre" || actual === "coal")))
  );
}

/** The top-left tile of a building's footprint when the pointer is at a tile (bigger buildings are centred) */
export function footprintOrigin(pointerX: number, pointerY: number, size: number): { x: number; y: number } {
  const offset = Math.floor((size - 1) / 2);
  return { x: pointerX - offset, y: pointerY - offset };
}

export function ownEstate(world: WorldModel, company: CompanyDto): EstateDto | undefined {
  return world.district(company.districtX, company.districtY)?.estates.find((t) => t.slot === company.slot);
}

function insideEstate(company: CompanyDto, x: number, y: number): boolean {
  return (
    x >= company.estateX &&
    y >= company.estateY &&
    x < company.estateX + ESTATE_SIZE &&
    y < company.estateY + ESTATE_SIZE
  );
}

export function forestAround(world: WorldModel, x: number, y: number, size: number): number {
  let count = 0;
  for (let ty = y - 2; ty < y + size + 2; ty++) {
    for (let tx = x - 2; tx < x + size + 2; tx++) {
      if (world.terrain(tx, ty) === "forest" && world.buildingAt(tx, ty) === undefined) {
        count++;
      }
    }
  }
  return count;
}

export function locationMultiplier(
  content: Content,
  world: WorldModel,
  definition: BuildingDefinition,
  x: number,
  y: number,
): number {
  const tiles = [];
  for (let dy = 0; dy < definition.size; dy++) {
    for (let dx = 0; dx < definition.size; dx++) {
      const tile = world.tile(x + dx, y + dy);
      if (tile !== null) {
        tiles.push(tile);
      }
    }
  }
  if (tiles.length === 0) {
    return 1;
  }
  switch (definition.modifier) {
    case "wind":
      return tiles.reduce((sum, t) => sum + t.wind, 0) / tiles.length;
    case "sun":
      return tiles.reduce((sum, t) => sum + t.sun, 0) / tiles.length;
    case "richness": {
      const wanted = definition.placement.deposit;
      const richness = Math.max(
        0,
        ...tiles.filter((t) => wanted === null || depositMatches(wanted, t.deposit)).map((t) => t.richness),
      );
      const table = content.dto.layout.richnessMultiplier;
      return table[Math.min(richness, table.length - 1)] ?? 0;
    }
    case "forest":
      return Math.min(1.5, Math.max(0.25, forestAround(world, x, y, definition.size) / 12));
    default:
      return 1;
  }
}

/** Whether a company can put this building with its top-left tile at (x, y) */
export function canPlace(
  content: Content,
  world: WorldModel,
  company: CompanyDto,
  era: number,
  definition: BuildingDefinition,
  x: number,
  y: number,
  ignoreBuilding: number | null = null,
): Placement {
  const fail = (reason: string): Placement => ({ ok: false, reason, multiplier: 0 });
  const lock = content.lockReason(definition, company.level, era);
  if (lock !== null) {
    return fail(`Needs ${lock}`);
  }
  for (let dy = 0; dy < definition.size; dy++) {
    for (let dx = 0; dx < definition.size; dx++) {
      const tx = x + dx;
      const ty = y + dy;
      if (!insideEstate(company, tx, ty)) {
        return fail("Only on your own estate");
      }
      if (!isParcelUnlocked(company.unlockedParcels, parcelOf(tx - company.estateX, ty - company.estateY))) {
        return fail("Buy this parcel first");
      }
      const building = world.buildingAt(tx, ty);
      if (
        (building !== undefined && building.id !== ignoreBuilding) ||
        world.conductorAt(tx, ty) !== undefined
      ) {
        return fail("Something is in the way");
      }
      const terrain = world.terrain(tx, ty);
      if (terrain === null) {
        return fail("Loading…");
      }
      if (!definition.placement.terrain.includes(terrain)) {
        return fail(`Not on ${terrain}`);
      }
    }
  }
  const deposit = definition.placement.deposit;
  if (deposit !== null) {
    let best = 0;
    for (let dy = 0; dy < definition.size; dy++) {
      for (let dx = 0; dx < definition.size; dx++) {
        const tile = world.tile(x + dx, y + dy);
        if (tile !== null && depositMatches(deposit, tile.deposit)) {
          best = Math.max(best, tile.richness);
        }
      }
    }
    if (best === 0) {
      return fail(
        `Needs a ${deposit === "anyMetal" ? "metal" : content.resourceName(deposit).toLowerCase()} deposit`,
      );
    }
    if (best < definition.placement.minRichness) {
      return fail(`Needs a ★${definition.placement.minRichness} deposit`);
    }
  }
  if (definition.placement.shore) {
    let water = false;
    for (let i = 0; i < definition.size && !water; i++) {
      water =
        world.terrain(x + i, y - 1) === "water" ||
        world.terrain(x + i, y + definition.size) === "water" ||
        world.terrain(x - 1, y + i) === "water" ||
        world.terrain(x + definition.size, y + i) === "water";
    }
    if (!water) {
      return fail("Must stand next to water");
    }
  }
  if (definition.placement.forest && forestAround(world, x, y, definition.size) < 3) {
    return fail("Needs a forest nearby");
  }
  return { ok: true, reason: null, multiplier: locationMultiplier(content, world, definition, x, y) };
}

/** Whether a pipe may go on this tile for this company, and what it costs relative to plain land */
export function pipeCost(world: WorldModel, company: CompanyDto, x: number, y: number): number | null {
  const conductor = world.conductorAt(x, y);
  if (conductor !== undefined) {
    // Own pipes and gates are already there; other companies' corridor pipes can't be crossed
    return conductor.owner === company.id ? 0.15 : null;
  }
  if (world.buildingAt(x, y) !== undefined) {
    return null;
  }
  const zone = zoneAt(x, y);
  if (zone.kind === "estate" ? !insideEstate(company, x, y) : zone.kind !== "corridor") {
    return null;
  }
  const terrain = world.terrain(x, y);
  if (terrain === null || terrain === "mountain") {
    return null;
  }
  return terrain === "water" ? 3 : 1;
}

/** The cheapest pipe route between two tiles (A*), or null when there is none within the search limit */
export function routePipe(
  world: WorldModel,
  company: CompanyDto,
  from: { x: number; y: number },
  to: { x: number; y: number },
  limit = 4000,
): Array<{ x: number; y: number }> | null {
  if (pipeCost(world, company, from.x, from.y) === null || pipeCost(world, company, to.x, to.y) === null) {
    return null;
  }
  const key = (x: number, y: number) => `${x},${y}`;
  const open: Array<{ x: number; y: number; f: number }> = [{ ...from, f: 0 }];
  const cost = new Map<string, number>([[key(from.x, from.y), 0]]);
  const previous = new Map<string, { x: number; y: number }>();
  const heuristic = (x: number, y: number) => (Math.abs(x - to.x) + Math.abs(y - to.y)) * 0.15;
  let visited = 0;
  while (open.length > 0 && visited < limit) {
    let best = 0;
    for (let i = 1; i < open.length; i++) {
      if (open[i]!.f < open[best]!.f) {
        best = i;
      }
    }
    const current = open.splice(best, 1)[0]!;
    visited++;
    if (current.x === to.x && current.y === to.y) {
      const path = [{ x: current.x, y: current.y }];
      let k = key(current.x, current.y);
      while (previous.has(k)) {
        const p = previous.get(k)!;
        path.push(p);
        k = key(p.x, p.y);
      }
      return path.reverse();
    }
    const base = cost.get(key(current.x, current.y))!;
    // Prefer going straight: neighbours in a fixed order, ties broken by the heuristic
    for (const [dx, dy] of [
      [1, 0],
      [-1, 0],
      [0, 1],
      [0, -1],
    ] as const) {
      const nx = current.x + dx;
      const ny = current.y + dy;
      const step = pipeCost(world, company, nx, ny);
      if (step === null) {
        continue;
      }
      const next = base + step + 0.001;
      const k = key(nx, ny);
      if (next < (cost.get(k) ?? Infinity)) {
        cost.set(k, next);
        previous.set(k, { x: current.x, y: current.y });
        open.push({ x: nx, y: ny, f: next + heuristic(nx, ny) });
      }
    }
  }
  return null;
}

/** The tiles of a route that need a new pipe (own pipes and gates are already there) */
export function newPipeTiles(
  world: WorldModel,
  company: CompanyDto,
  route: Array<{ x: number; y: number }>,
): Array<{ x: number; y: number }> {
  return route.filter((t) => world.conductorAt(t.x, t.y)?.owner !== company.id);
}

/** Gold for laying pipes on these tiles (bridges over water cost three times as much) */
export function pipeGold(
  world: WorldModel,
  tiles: Array<{ x: number; y: number }>,
  goldPerTile: number,
): number {
  return tiles.reduce((sum, t) => sum + goldPerTile * (world.terrain(t.x, t.y) === "water" ? 3 : 1), 0);
}

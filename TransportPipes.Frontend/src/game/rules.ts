import type { BuildingDefinition, BuildingDto, CompanyDto, EstateDto } from "../api/contracts";
import type { Content } from "./content";
import { DEEP_OFFSET, ESTATE_SIZE, isDeep, isParcelUnlocked, parcelOf, zoneAt } from "./layout";
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

/** One of the company's estates: the first (index 0) or a branch */
export interface OwnEstate {
  index: number;
  x: number;
  y: number;
  unlockedParcels: number;
  nextParcelCost: number | null;
  nextParcelLevel: number | null;
}

export function ownEstates(company: CompanyDto): OwnEstate[] {
  return [
    {
      index: 0,
      x: company.estateX,
      y: company.estateY,
      unlockedParcels: company.unlockedParcels,
      nextParcelCost: company.nextParcelCost,
      nextParcelLevel: company.nextParcelLevel,
    },
    ...(company.branches ?? []).map((t, i) => ({
      index: i + 1,
      x: t.x,
      y: t.y,
      unlockedParcels: t.unlockedParcels,
      nextParcelCost: t.nextParcelCost,
      nextParcelLevel: t.nextParcelLevel,
    })),
  ];
}

/** The company's estate a surface tile lies on */
export function ownEstateAt(company: CompanyDto, x: number, y: number): OwnEstate | undefined {
  return ownEstates(company).find(
    (t) => x >= t.x && y >= t.y && x < t.x + ESTATE_SIZE && y < t.y + ESTATE_SIZE,
  );
}

export function forestAround(world: WorldModel, x: number, y: number, size: number): number {
  let count = 0;
  for (let ty = y - 2; ty < y + size + 2; ty++) {
    for (let tx = x - 2; tx < x + size + 2; tx++) {
      if (
        world.terrain(tx, ty) === "forest" &&
        (!world.dealtLand || world.tile(tx, ty)?.deposit === "wood") &&
        world.buildingAt(tx, ty) === undefined
      ) {
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
  /** Buildings that exist already (moved, or from a crate) need no unlock */
  ignoreLock = false,
): Placement {
  const fail = (reason: string): Placement => ({ ok: false, reason, multiplier: 0 });
  const lock = ignoreLock ? null : content.lockReason(definition, company, era);
  if (lock !== null) {
    return fail(`Needs ${lock}`);
  }
  const deep = isDeep(y);
  if (deep && definition.layer === "surface") {
    return fail(`${definition.name} can't be built in the Deep Level`);
  }
  if (!deep && definition.layer === "deep") {
    return fail(`${definition.name} is built in the Deep Level`);
  }
  for (let dy = 0; dy < definition.size; dy++) {
    for (let dx = 0; dx < definition.size; dx++) {
      const tx = x + dx;
      const ty = y + dy;
      if (deep) {
        // The caverns under your own estate
        if (!insideEstate(company, tx, ty - DEEP_OFFSET)) {
          return fail("Only under your own estate");
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
        if (terrain !== "cavern") {
          return fail("Dig out this parcel first");
        }
        continue;
      }
      const outpost = world.outpostAt(tx, ty);
      if (outpost !== undefined) {
        if (outpost.owner !== company.id) {
          return fail("Someone else's outpost");
        }
      } else {
        const estate = ownEstateAt(company, tx, ty);
        if (estate === undefined) {
          return fail("Only on your own estates and outposts");
        }
        if (!isParcelUnlocked(estate.unlockedParcels, parcelOf(tx - estate.x, ty - estate.y))) {
          return fail("Buy this parcel first");
        }
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
    return fail(world.dealtLand ? "Needs the old forest's marked trees nearby" : "Needs a forest nearby");
  }
  return { ok: true, reason: null, multiplier: locationMultiplier(content, world, definition, x, y) };
}

/** Building kinds that sit in a pipe and carry resources (a pipe route may go through your own) */
const CONDUCTOR_KINDS = new Set(["valve", "priorityValve"]);

/** Whether this tile already carries your resources: your pipe, gate, valve */
export function isOwnConductor(
  world: WorldModel,
  company: CompanyDto,
  x: number,
  y: number,
  content?: Content,
): boolean {
  if (world.conductorAt(x, y)?.owner === company.id) {
    return true;
  }
  const building = world.buildingAt(x, y);
  return (
    building !== undefined &&
    building.owner === company.id &&
    building.constructionEndsAt === null &&
    content !== undefined &&
    CONDUCTOR_KINDS.has(content.building(building.type)?.kind ?? "")
  );
}

/** Whether a pipe may go on this tile for this company, and what it costs relative to plain land */
export function pipeCost(
  world: WorldModel,
  company: CompanyDto,
  x: number,
  y: number,
  content?: Content,
): number | null {
  if (isOwnConductor(world, company, x, y, content)) {
    return 0.15;
  }
  if (world.conductorAt(x, y) !== undefined || world.buildingAt(x, y) !== undefined) {
    return null;
  }
  if (isDeep(y)) {
    // Down in the Deep Level: only in your own caverns, and no losses
    const terrain = world.terrain(x, y);
    return insideEstate(company, x, y - DEEP_OFFSET) && terrain === "cavern" ? 1 : null;
  }
  // Pipes stay on your own land (the corridors carry links, which the game lays); and around your outposts
  const zone = zoneAt(x, y);
  if (zone.kind === "estate" ? ownEstateAt(company, x, y) === undefined : zone.kind !== "wilderness") {
    return null;
  }
  // The wilderness is open to pipes, except someone else's outpost
  const outpost = zone.kind === "wilderness" ? world.outpostAt(x, y) : undefined;
  if (outpost !== undefined && outpost.owner !== company.id) {
    return null;
  }
  const terrain = world.terrain(x, y);
  if (terrain === null || terrain === "mountain") {
    return null;
  }
  return terrain === "water" ? 3 : 1;
}

/** A building a pipe can't go through (not one of your own valves or gates), at this tile */
function blockingBuilding(
  world: WorldModel,
  company: CompanyDto,
  x: number,
  y: number,
  content?: Content,
): BuildingDto | undefined {
  const building = world.buildingAt(x, y);
  return building !== undefined && !isOwnConductor(world, company, x, y, content) ? building : undefined;
}

/** Whether a tile is next to a building's footprint (a pipe there connects it) */
function touches(building: BuildingDto, x: number, y: number): boolean {
  const insideX = x >= building.x && x < building.x + building.size;
  const insideY = y >= building.y && y < building.y + building.size;
  return (
    (insideX && (y === building.y - 1 || y === building.y + building.size)) ||
    (insideY && (x === building.x - 1 || x === building.x + building.size))
  );
}

function around(building: BuildingDto): Array<{ x: number; y: number }> {
  const tiles: Array<{ x: number; y: number }> = [];
  for (let i = 0; i < building.size; i++) {
    tiles.push(
      { x: building.x + i, y: building.y - 1 },
      { x: building.x + i, y: building.y + building.size },
      { x: building.x - 1, y: building.y + i },
      { x: building.x + building.size, y: building.y + i },
    );
  }
  return tiles;
}

/**
 * The cheapest pipe route between two tiles (A*), or null when there is none within the search limit. A route that
 * starts or ends on a building ("drag from the panel to your Trading Post") goes from or to the free tiles around it.
 */
export function routePipe(
  world: WorldModel,
  company: CompanyDto,
  from: { x: number; y: number },
  to: { x: number; y: number },
  content?: Content,
  limit = 4000,
): Array<{ x: number; y: number }> | null {
  const startBuilding = blockingBuilding(world, company, from.x, from.y, content);
  const endBuilding = blockingBuilding(world, company, to.x, to.y, content);
  if (startBuilding !== undefined && startBuilding.id === endBuilding?.id) {
    return null;
  }
  const starts =
    startBuilding !== undefined
      ? around(startBuilding).filter((t) => pipeCost(world, company, t.x, t.y, content) !== null)
      : pipeCost(world, company, from.x, from.y, content) !== null
        ? [from]
        : [];
  if (
    starts.length === 0 ||
    (endBuilding === undefined && pipeCost(world, company, to.x, to.y, content) === null)
  ) {
    return null;
  }
  const isGoal = (x: number, y: number) =>
    endBuilding !== undefined ? touches(endBuilding, x, y) : x === to.x && y === to.y;
  const key = (x: number, y: number) => `${x},${y}`;
  const open: Array<{ x: number; y: number; f: number }> = starts.map((t) => ({ x: t.x, y: t.y, f: 0 }));
  const cost = new Map<string, number>(starts.map((t) => [key(t.x, t.y), 0]));
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
    if (isGoal(current.x, current.y)) {
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
      const step = pipeCost(world, company, nx, ny, content);
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

/** The tiles of a route that need a new pipe (own pipes, gates and valves are already there) */
export function newPipeTiles(
  world: WorldModel,
  company: CompanyDto,
  route: Array<{ x: number; y: number }>,
  content?: Content,
): Array<{ x: number; y: number }> {
  return route.filter((t) => !isOwnConductor(world, company, t.x, t.y, content));
}

/** The player's own pipes on a route that are of a lower tier: drawing a better pipe over them upgrades them */
export function upgradablePipeTiles(
  world: WorldModel,
  company: CompanyDto,
  route: Array<{ x: number; y: number }>,
  tier: number,
): Array<{ x: number; y: number }> {
  return route.filter((t) => {
    const conductor = world.conductorAt(t.x, t.y);
    return (
      conductor !== undefined &&
      conductor.kind === "pipe" &&
      conductor.owner === company.id &&
      conductor.tier < tier
    );
  });
}

/** Gold for laying pipes on these tiles (bridges over water cost three times as much) */
export function pipeGold(
  world: WorldModel,
  tiles: Array<{ x: number; y: number }>,
  goldPerTile: number,
): number {
  return tiles.reduce((sum, t) => sum + goldPerTile * (world.terrain(t.x, t.y) === "water" ? 3 : 1), 0);
}

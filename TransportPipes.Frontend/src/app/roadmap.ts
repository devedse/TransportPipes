import type { CompanyDto } from "../api/contracts";
import type { Content } from "../game/content";
import { ESTATE_SIZE, isParcelUnlocked, parcelOf } from "../game/layout";
import { depositMatches } from "../game/rules";
import type { WorldModel } from "../game/world";

/** The Company Level from which a corporation can be founded or joined (GameWorld.CorporationFoundLevel) */
const CORPORATION_LEVEL = 8;

/** Company Levels that bring a manager slot (progression.yaml managerSlots) */
const MANAGER_LEVELS = [10, 20, 30, 45, 60];

/** Something a Company Level brings; ok: whether the company can use it now (null: not yet known, e.g. a parcel has it) */
export interface RoadmapItem {
  name: string;
  note: string | null;
  ok: boolean | null;
}

export interface RoadmapLevel {
  level: number;
  items: RoadmapItem[];
}

/** What the company's estate holds: deposits and terrain on its land, and in the parcels it can still buy */
function holdings(world: WorldModel | null, company: CompanyDto) {
  const owned = new Set<string>();
  const buyable = new Set<string>();
  const terrain = new Set<string>();
  const buyableTerrain = new Set<string>();
  for (let y = 0; y < ESTATE_SIZE; y++) {
    for (let x = 0; x < ESTATE_SIZE; x++) {
      const tile = world?.tile(company.estateX + x, company.estateY + y);
      if (tile == null) {
        continue;
      }
      const mine = isParcelUnlocked(company.unlockedParcels, parcelOf(x, y));
      if (tile.deposit !== null) {
        (mine ? owned : buyable).add(tile.deposit);
      }
      (mine ? terrain : buyableTerrain).add(tile.terrain);
    }
  }
  return { owned, buyable, terrain, buyableTerrain };
}

/** Whether the land has a kind of terrain the building needs: [note, ok] */
function terrainNote(
  land: ReturnType<typeof holdings>,
  kinds: string[],
  name: string,
): [string, boolean | null] {
  return kinds.some((t) => land.terrain.has(t))
    ? [`you have ${name}`, true]
    : kinds.some((t) => land.buyableTerrain.has(t))
      ? [`a parcel you can buy has ${name}`, null]
      : [`needs ${name}`, false];
}

/** What a Company Level brings: buildings (and whether the land and the company's chains can use them), a parcel, corporations, a manager */
function unlocksAt(
  level: number,
  content: Content,
  company: CompanyDto,
  era: number,
  land: ReturnType<typeof holdings>,
  made: Set<string>,
): RoadmapItem[] {
  const items: RoadmapItem[] = [];
  for (const building of content.buildings) {
    if (!building.buildable || building.unlock.level !== level || building.unlock.era > era) {
      continue;
    }
    const deposit = building.placement.deposit;
    const missing = Object.keys(building.in).filter((t) => t !== "energy" && !made.has(t));
    let note: string | null = null;
    let ok: boolean | null = true;
    if (deposit !== null) {
      const name = content.resourceName(deposit).toLowerCase();
      const has = (set: Set<string>) => [...set].some((t) => depositMatches(deposit, t));
      [note, ok] = has(land.owned)
        ? [`you have ${name}`, true]
        : has(land.buyable)
          ? [`a parcel you can buy has ${name}`, null]
          : [`no ${name} on your land`, false];
    } else if (building.placement.shore) {
      [note, ok] = terrainNote(land, ["water"], "a shore");
    } else if (building.placement.forest || building.modifier === "forest") {
      [note, ok] = terrainNote(land, ["forest"], "forest");
    } else if (building.placement.terrain.length > 0 && !building.placement.terrain.includes("grass")) {
      [note, ok] = terrainNote(land, building.placement.terrain, building.placement.terrain.join(" or "));
    } else if (missing.length > 0) {
      [note, ok] = [`uses ${missing.map((t) => content.resourceName(t).toLowerCase()).join(" and ")}`, null];
    } else if (Object.keys(building.in).length > 0) {
      note = "you make all it needs";
    }
    if (building.unlock.research !== null && !company.researched.includes(building.unlock.research)) {
      const tech = content.research(building.unlock.research)?.name ?? building.unlock.research;
      note = `${note !== null ? `${note} · ` : ""}+ research ${tech}`;
      ok = ok === false ? false : null;
    }
    items.push({ name: building.name, note, ok });
  }
  const parcels = content.progression.parcelLevels;
  const parcel = parcels.indexOf(level);
  if (parcel >= 0) {
    items.push({
      name: "A parcel",
      note: `${(content.progression.parcelCosts[parcel] ?? 0).toLocaleString()} g`,
      ok: true,
    });
  }
  if (level === CORPORATION_LEVEL) {
    items.push({ name: "Corporations", note: "found or join one", ok: true });
  }
  if (MANAGER_LEVELS.includes(level)) {
    items.push({ name: "A manager", note: "hire one in the Company panel", ok: true });
  }
  return items;
}

/** The next Company Levels that bring something, up to count of them, each with what it brings */
export function levelRoadmap(
  content: Content,
  company: CompanyDto,
  world: WorldModel | null,
  era: number,
  count = 4,
): RoadmapLevel[] {
  const land = holdings(world, company);
  const made = new Set<string>();
  for (const building of world?.buildings.values() ?? []) {
    if (building.owner === company.id) {
      Object.keys(content.building(building.type)?.out ?? {}).forEach((t) => made.add(t));
    }
  }
  const levels: RoadmapLevel[] = [];
  for (
    let level = company.level + 1;
    level <= content.progression.maxCompanyLevel && levels.length < count;
    level++
  ) {
    const items = unlocksAt(level, content, company, era, land, made);
    if (items.length > 0) {
      levels.push({ level, items });
    }
  }
  return levels;
}

/** What the level just reached brings (for the level-up card) */
export function levelUnlocks(
  content: Content,
  company: CompanyDto,
  world: WorldModel | null,
  era: number,
  level: number,
) {
  const made = new Set<string>();
  for (const building of world?.buildings.values() ?? []) {
    if (building.owner === company.id) {
      Object.keys(content.building(building.type)?.out ?? {}).forEach((t) => made.add(t));
    }
  }
  return unlocksAt(level, content, company, era, holdings(world, company), made);
}

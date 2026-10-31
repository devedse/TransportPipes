import type {
  BuildingDto,
  ChunkDto,
  ChunkObjectsDto,
  ConductorDto,
  DistrictDto,
  EstateDto,
  NetworkFlowsDto,
  OutpostDto,
  TerrainType,
} from "../api/contracts";
import {
  CHUNK_SIZE,
  DEEP_OFFSET,
  ESTATE_SIZE,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
  chunkOf,
  districtOf,
  tileKey,
  zoneAt,
} from "./layout";

export interface TileInfo {
  terrain: TerrainType;
  deposit: string | null;
  richness: number;
  wind: number;
  sun: number;
}

export interface ChunkTerrain {
  cx: number;
  cy: number;
  terrain: Uint8Array;
  deposits: Uint8Array;
  richness: Uint8Array;
  wind: Uint8Array;
  sun: Uint8Array;
  depositNames: string[];
}

export function chunkKey(cx: number, cy: number): string {
  return `${cx},${cy}`;
}

function decode(base64: string): Uint8Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

/** groundChunks: of the chunks, those whose ground changed (their terrain or deposits): the others' objects only */
export type WorldListener = (change: {
  chunks: string[];
  flows: boolean;
  districts: boolean;
  groundChunks: string[];
}) => void;

/** Everything the client knows about the world: the chunks it has loaded, the districts and the flows */
export class WorldModel {
  readonly chunks = new Map<string, ChunkTerrain>();
  readonly buildings = new Map<number, BuildingDto>();
  readonly conductors = new Map<number, ConductorDto>();
  readonly districts = new Map<string, DistrictDto>();
  readonly flows = new Map<number, NetworkFlowsDto>();
  private readonly buildingsByChunk = new Map<string, Set<number>>();
  /** The chunk whose update registered a building (and its tiles): only that chunk's next update clears them */
  private readonly chunkOfBuilding = new Map<number, string>();
  private readonly buildingTilesByChunk = new Map<string, number[]>();
  private readonly tileOwner = new Map<number, string>();
  private readonly conductorsByChunk = new Map<string, Set<number>>();
  private readonly overridesByChunk = new Map<string, Set<number>>();
  private readonly tileBuilding = new Map<number, number>();
  private readonly overrides = new Map<number, TerrainType>();
  readonly outposts = new Map<number, OutpostDto>();
  private readonly outpostsByChunk = new Map<string, Set<number>>();
  private readonly deposits = new Map<number, { deposit: string; richness: number }>();
  private readonly depositsByChunk = new Map<string, Set<number>>();
  private readonly listeners = new Set<WorldListener>();

  private readonly terrainNames: TerrainType[];
  /** The player's own Deep Level: where its estate lies down there and which parcels are dug out */
  private deep: { x: number; y: number; parcels: number } | null = null;

  constructor(terrainNames: TerrainType[]) {
    this.terrainNames = terrainNames;
  }

  /** The dug out parcels of the player's Deep Level (the server sends solid rock; the caverns are the player's) */
  setDeepLevel(estateX: number, estateY: number, parcels: number) {
    const deep = { x: estateX, y: estateY + DEEP_OFFSET, parcels };
    const before = this.deep;
    if (before !== null && before.x === deep.x && before.y === deep.y && before.parcels === deep.parcels) {
      return;
    }
    this.deep = deep;
    const chunks = new Set<string>();
    for (const area of before === null ? [deep] : [before, deep]) {
      for (let y = area.y; y < area.y + ESTATE_SIZE; y += CHUNK_SIZE / 2) {
        for (let x = area.x; x < area.x + ESTATE_SIZE; x += CHUNK_SIZE / 2) {
          for (const [dx, dy] of [
            [0, 0],
            [CHUNK_SIZE / 2 - 1, 0],
            [0, CHUNK_SIZE / 2 - 1],
            [CHUNK_SIZE / 2 - 1, CHUNK_SIZE / 2 - 1],
          ] as const) {
            const c = chunkOf(x + dx, y + dy);
            chunks.add(chunkKey(c.x, c.y));
          }
        }
      }
    }
    this.emit([...chunks]);
  }

  /** Whether a deep tile is dug out: in a dug parcel of the player's Deep Level, or the pocket of a lift */
  isDug(x: number, y: number): boolean {
    const deep = this.deep;
    if (deep !== null && x >= deep.x && y >= deep.y && x < deep.x + ESTATE_SIZE && y < deep.y + ESTATE_SIZE) {
      const parcel =
        Math.floor((y - deep.y) / PARCEL_SIZE) * PARCELS_PER_SIDE + Math.floor((x - deep.x) / PARCEL_SIZE);
      if ((deep.parcels & (1 << parcel)) !== 0) {
        return true;
      }
    }
    const building = this.buildingAt(x, y);
    return building !== undefined && building.type === "liftShaft";
  }

  onChange(listener: WorldListener): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private emit(chunks: string[], flows = false, districts = false, groundChunks: string[] = chunks) {
    this.listeners.forEach((listener) => listener({ chunks, flows, districts, groundChunks }));
  }

  /** A chunk's terrain and deposit overrides, as a string to compare */
  private groundOf(key: string): string {
    const terrain = [...(this.overridesByChunk.get(key) ?? [])].map((k) => `${k}:${this.overrides.get(k)}`);
    const deposits = [...(this.depositsByChunk.get(key) ?? [])].map(
      (k) => `${k}:${this.deposits.get(k)?.deposit}:${this.deposits.get(k)?.richness}`,
    );
    return `${terrain.sort().join(",")}|${deposits.sort().join(",")}`;
  }

  setChunk(dto: ChunkDto) {
    const key = chunkKey(dto.cx, dto.cy);
    this.chunks.set(key, {
      cx: dto.cx,
      cy: dto.cy,
      terrain: decode(dto.terrain),
      deposits: decode(dto.deposits),
      richness: decode(dto.richness),
      wind: decode(dto.wind),
      sun: decode(dto.sun),
      depositNames: dto.depositNames,
    });
    this.applyObjectsSilently(dto.objects);
    this.emit([key]);
  }

  applyObjects(dto: ChunkObjectsDto) {
    const key = chunkKey(dto.cx, dto.cy);
    if (!this.chunks.has(key)) {
      return;
    }
    // Its ground is drawn again only when its terrain or deposits changed (the owner's profile, 2026-10-31: a
    // building's level redrew its whole chunk's ground)
    const ground = this.groundOf(key);
    this.applyObjectsSilently(dto);
    this.emit([key], false, false, this.groundOf(key) === ground ? [] : [key]);
  }

  private applyObjectsSilently(dto: ChunkObjectsDto) {
    const key = chunkKey(dto.cx, dto.cy);
    this.clearObjects(key);

    const buildingIds = new Set<number>();
    const buildingTiles: number[] = [];
    for (const building of dto.buildings) {
      this.buildings.set(building.id, building);
      this.chunkOfBuilding.set(building.id, key);
      buildingIds.add(building.id);
      for (let dy = 0; dy < building.size; dy++) {
        for (let dx = 0; dx < building.size; dx++) {
          const k = tileKey(building.x + dx, building.y + dy);
          this.tileBuilding.set(k, building.id);
          this.tileOwner.set(k, key);
          buildingTiles.push(k);
        }
      }
    }
    this.buildingsByChunk.set(key, buildingIds);
    this.buildingTilesByChunk.set(key, buildingTiles);

    const conductorKeys = new Set<number>();
    for (const conductor of dto.conductors) {
      const k = tileKey(conductor.x, conductor.y);
      this.conductors.set(k, conductor);
      conductorKeys.add(k);
    }
    this.conductorsByChunk.set(key, conductorKeys);

    const overrideKeys = new Set<number>();
    for (const override of dto.terrainOverrides) {
      const k = tileKey(override.x, override.y);
      this.overrides.set(k, override.terrain);
      overrideKeys.add(k);
    }
    this.overridesByChunk.set(key, overrideKeys);

    const outpostIds = new Set<number>();
    for (const outpost of dto.outposts) {
      this.outposts.set(outpost.id, outpost);
      outpostIds.add(outpost.id);
    }
    this.outpostsByChunk.set(key, outpostIds);

    const depositKeys = new Set<number>();
    for (const deposit of dto.deposits) {
      const k = tileKey(deposit.x, deposit.y);
      this.deposits.set(k, { deposit: deposit.deposit, richness: deposit.richness });
      depositKeys.add(k);
    }
    this.depositsByChunk.set(key, depositKeys);
  }

  /** The outpost a tile is in (when a chunk with it is loaded) */
  outpostAt(x: number, y: number): OutpostDto | undefined {
    for (const outpost of this.outposts.values()) {
      if (x >= outpost.x && y >= outpost.y && x < outpost.x + outpost.size && y < outpost.y + outpost.size) {
        return outpost;
      }
    }
    return undefined;
  }

  private clearObjects(key: string) {
    const outposts = this.outpostsByChunk.get(key) ?? new Set<number>();
    this.outpostsByChunk.delete(key);
    for (const id of outposts) {
      // An outpost spans chunks: it stays while another chunk still has it
      if (![...this.outpostsByChunk.values()].some((ids) => ids.has(id))) {
        this.outposts.delete(id);
      }
    }
    for (const k of this.depositsByChunk.get(key) ?? []) {
      this.deposits.delete(k);
    }
    this.depositsByChunk.delete(key);
    // Only what this chunk registered: a building that moved into another chunk may already be there (the chunks'
    // updates come in any order), with tiles of its own
    for (const k of this.buildingTilesByChunk.get(key) ?? []) {
      if (this.tileOwner.get(k) === key) {
        this.tileBuilding.delete(k);
        this.tileOwner.delete(k);
      }
    }
    this.buildingTilesByChunk.delete(key);
    for (const id of this.buildingsByChunk.get(key) ?? []) {
      if (this.chunkOfBuilding.get(id) === key) {
        this.buildings.delete(id);
        this.chunkOfBuilding.delete(id);
      }
    }
    for (const k of this.conductorsByChunk.get(key) ?? []) {
      this.conductors.delete(k);
    }
    for (const k of this.overridesByChunk.get(key) ?? []) {
      this.overrides.delete(k);
    }
    this.buildingsByChunk.delete(key);
    this.conductorsByChunk.delete(key);
    this.overridesByChunk.delete(key);
  }

  dropChunk(cx: number, cy: number) {
    const key = chunkKey(cx, cy);
    this.clearObjects(key);
    this.chunks.delete(key);
  }

  buildingIdsInChunk(cx: number, cy: number): Iterable<number> {
    return this.buildingsByChunk.get(chunkKey(cx, cy)) ?? [];
  }

  conductorKeysInChunk(cx: number, cy: number): Iterable<number> {
    return this.conductorsByChunk.get(chunkKey(cx, cy)) ?? [];
  }

  hasChunkAt(x: number, y: number): boolean {
    const c = chunkOf(x, y);
    return this.chunks.has(chunkKey(c.x, c.y));
  }

  tile(x: number, y: number): TileInfo | null {
    const c = chunkOf(x, y);
    const chunk = this.chunks.get(chunkKey(c.x, c.y));
    if (chunk === undefined) {
      return null;
    }
    const i = (y - c.y * CHUNK_SIZE) * CHUNK_SIZE + (x - c.x * CHUNK_SIZE);
    const depositIndex = chunk.deposits[i] ?? 0;
    const override = this.overrides.get(tileKey(x, y));
    // A Discovery's deposit
    const discovered = this.deposits.get(tileKey(x, y));
    let terrain = override ?? this.terrainNames[chunk.terrain[i] ?? 0] ?? "grass";
    if (terrain === "rock" && this.isDug(x, y)) {
      terrain = "cavern";
    }
    return {
      terrain,
      deposit:
        discovered?.deposit ?? (depositIndex > 0 ? (chunk.depositNames[depositIndex - 1] ?? null) : null),
      richness: discovered?.richness ?? chunk.richness[i] ?? 0,
      wind: (chunk.wind[i] ?? 100) / 100,
      sun: (chunk.sun[i] ?? 100) / 100,
    };
  }

  terrain(x: number, y: number): TerrainType | null {
    return this.tile(x, y)?.terrain ?? null;
  }

  buildingAt(x: number, y: number): BuildingDto | undefined {
    const id = this.tileBuilding.get(tileKey(x, y));
    return id === undefined ? undefined : this.buildings.get(id);
  }

  conductorAt(x: number, y: number): ConductorDto | undefined {
    return this.conductors.get(tileKey(x, y));
  }

  isOccupied(x: number, y: number): boolean {
    return this.tileBuilding.has(tileKey(x, y)) || this.conductors.has(tileKey(x, y));
  }

  setDistrict(district: DistrictDto) {
    this.districts.set(chunkKey(district.x, district.y), district);
    this.emit([], false, true);
  }

  district(x: number, y: number): DistrictDto | undefined {
    return this.districts.get(chunkKey(x, y));
  }

  /** The estate a tile is in (when its district is loaded) */
  estateAt(x: number, y: number): EstateDto | undefined {
    const zone = zoneAt(x, y);
    if (zone.kind !== "estate") {
      return undefined;
    }
    return this.district(zone.districtX, zone.districtY)?.estates.find((t) => t.slot === zone.slot);
  }

  districtAt(x: number, y: number): DistrictDto | undefined {
    const d = districtOf(x, y);
    return this.district(d.x, d.y);
  }

  setFlows(flows: NetworkFlowsDto[], removed: number[] = []) {
    for (const id of removed) {
      this.flows.delete(id);
    }
    for (const network of flows) {
      this.flows.set(network.id, network);
    }
    this.emit([], true);
  }

  /** The rate (0..1) a building runs at, from its network's flows */
  rate(building: BuildingDto): number | null {
    if (building.network === null) {
      return null;
    }
    const network = this.flows.get(building.network);
    return network === undefined ? null : (network.rates[String(building.id)] ?? null);
  }

  /** The networks of everything in the loaded chunks */
  visibleNetworks(): Set<number> {
    const result = new Set<number>();
    for (const building of this.buildings.values()) {
      if (building.network !== null) {
        result.add(building.network);
      }
    }
    return result;
  }
}

import type {
  BuildingDto,
  ChunkDto,
  ChunkObjectsDto,
  ConductorDto,
  DistrictDto,
  EstateDto,
  NetworkFlowsDto,
  TerrainType,
} from "../api/contracts";
import { CHUNK_SIZE, chunkOf, districtOf, tileKey, zoneAt } from "./layout";

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

export type WorldListener = (change: { chunks: string[]; flows: boolean; districts: boolean }) => void;

/** Everything the client knows about the world: the chunks it has loaded, the districts and the flows */
export class WorldModel {
  readonly chunks = new Map<string, ChunkTerrain>();
  readonly buildings = new Map<number, BuildingDto>();
  readonly conductors = new Map<number, ConductorDto>();
  readonly districts = new Map<string, DistrictDto>();
  readonly flows = new Map<number, NetworkFlowsDto>();
  private readonly buildingsByChunk = new Map<string, Set<number>>();
  private readonly conductorsByChunk = new Map<string, Set<number>>();
  private readonly overridesByChunk = new Map<string, Set<number>>();
  private readonly tileBuilding = new Map<number, number>();
  private readonly overrides = new Map<number, TerrainType>();
  private readonly listeners = new Set<WorldListener>();

  private readonly terrainNames: TerrainType[];

  constructor(terrainNames: TerrainType[]) {
    this.terrainNames = terrainNames;
  }

  onChange(listener: WorldListener): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  private emit(chunks: string[], flows = false, districts = false) {
    this.listeners.forEach((listener) => listener({ chunks, flows, districts }));
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
    this.applyObjectsSilently(dto);
    this.emit([key]);
  }

  private applyObjectsSilently(dto: ChunkObjectsDto) {
    const key = chunkKey(dto.cx, dto.cy);
    this.clearObjects(key);

    const buildingIds = new Set<number>();
    for (const building of dto.buildings) {
      this.buildings.set(building.id, building);
      buildingIds.add(building.id);
      for (let dy = 0; dy < building.size; dy++) {
        for (let dx = 0; dx < building.size; dx++) {
          this.tileBuilding.set(tileKey(building.x + dx, building.y + dy), building.id);
        }
      }
    }
    this.buildingsByChunk.set(key, buildingIds);

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
  }

  private clearObjects(key: string) {
    for (const id of this.buildingsByChunk.get(key) ?? []) {
      const building = this.buildings.get(id);
      if (building !== undefined) {
        for (let dy = 0; dy < building.size; dy++) {
          for (let dx = 0; dx < building.size; dx++) {
            const k = tileKey(building.x + dx, building.y + dy);
            if (this.tileBuilding.get(k) === id) {
              this.tileBuilding.delete(k);
            }
          }
        }
      }
      this.buildings.delete(id);
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
    return {
      terrain: override ?? this.terrainNames[chunk.terrain[i] ?? 0] ?? "grass",
      deposit: depositIndex > 0 ? (chunk.depositNames[depositIndex - 1] ?? null) : null,
      richness: chunk.richness[i] ?? 0,
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

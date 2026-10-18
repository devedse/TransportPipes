import { Assets, Texture, type Spritesheet } from "pixi.js";
import type { TerrainType } from "../../api/contracts";
import { resolveFrames } from "../spriteFrames";
import {
  drawBuilding,
  drawGate,
  drawHubStructure,
  type BuildingArt,
  type HubStructureLook,
} from "./buildings";
import { createCanvas, type Attachment } from "./iso";
import {
  drawDeposit,
  drawForestTrees,
  drawLantern,
  drawMagma,
  drawRocks,
  drawTerrainTile,
  drawVein,
} from "./terrain";

export const OBJECT_ATLAS_URL = "/images/CombinedGameObjects/combinedjson.json";
export const TERRAIN_ATLAS_URL = "/images/roadTiles/transportTiles.json";

/** Variants per terrain type, picked by a hash of the tile */
export const TERRAIN_VARIANTS = 4;

export interface BuildingTextures {
  frames: Texture[];
  size: number;
  attachments: Attachment[];
  /** Hand-drawn sprite of the original game */
  original: boolean;
}

/** Original sprites by building type */
const ORIGINAL_SPRITES: Record<string, string> = {
  solarPanel: "SolarPanel",
  windMoily: "WindMoily",
  ironMine: "IronMine",
  smallFactory: "FactorySmall",
  storage: "Storage",
  tradingPost: "TradingBuilding",
};

/** Smoke from the chimney of the original factory sprite */
const ORIGINAL_ATTACHMENTS: Record<string, Attachment[]> = {};

const ORIGINAL_TERRAIN: Partial<Record<TerrainType, string>> = {
  grass: "grass.png",
  beach: "beach.png",
  water: "water.png",
};

/** All textures of the game: the original atlases plus the procedural art, created once and cached */
export class TextureBank {
  private readonly atlas = new Map<string, Texture>();
  private readonly cache = new Map<string, Texture>();
  private readonly buildingCache = new Map<string, BuildingTextures>();
  private readonly thumbnailCache = new Map<string, string>();
  private atlasImage: HTMLImageElement | null = null;
  private atlasFrames: Record<string, { frame: { x: number; y: number; w: number; h: number } }> = {};

  static async load(): Promise<TextureBank> {
    const bank = new TextureBank();
    const [objects, terrain] = await Promise.all([
      Assets.load<Spritesheet>(OBJECT_ATLAS_URL),
      Assets.load<Spritesheet>(TERRAIN_ATLAS_URL),
    ]);
    for (const sheet of [objects, terrain]) {
      for (const [name, texture] of Object.entries(sheet.textures)) {
        bank.atlas.set(name, texture);
      }
    }
    // For DOM thumbnails
    try {
      const json = (await (await fetch(OBJECT_ATLAS_URL)).json()) as { frames: TextureBank["atlasFrames"] };
      bank.atlasFrames = json.frames;
      const image = new Image();
      image.src = OBJECT_ATLAS_URL.replace("combinedjson.json", "combinedimage.png");
      await image.decode();
      bank.atlasImage = image;
    } catch (error) {
      console.warn("Thumbnails of the original sprites are not available", error);
    }
    return bank;
  }

  private cached(key: string, create: () => HTMLCanvasElement): Texture {
    let texture = this.cache.get(key);
    if (texture === undefined) {
      texture = Texture.from(create());
      this.cache.set(key, texture);
    }
    return texture;
  }

  terrain(type: TerrainType, variant: number): Texture {
    const original = ORIGINAL_TERRAIN[type];
    if (original !== undefined) {
      const texture = this.atlas.get(original);
      if (texture !== undefined) {
        return texture;
      }
    }
    return this.cached(`terrain:${type}:${variant % TERRAIN_VARIANTS}`, () =>
      drawTerrainTile(type, variant % TERRAIN_VARIANTS),
    );
  }

  forest(variant: number): Texture {
    return this.cached(`forest:${variant % 6}`, () => drawForestTrees(variant % 6));
  }

  rocks(variant: number): Texture {
    return this.cached(`rocks:${variant % 4}`, () => drawRocks(variant % 4));
  }

  deposit(color: string, richness: number, variant: number): Texture {
    return this.cached(`deposit:${color}:${richness}:${variant % 3}`, () =>
      drawDeposit(color, richness, variant % 3),
    );
  }

  vein(color: string, richness: number, variant: number): Texture {
    return this.cached(`vein:${color}:${richness}:${variant % 3}`, () =>
      drawVein(color, richness, variant % 3),
    );
  }

  magma(richness: number, variant: number): Texture {
    return this.cached(`magma:${richness}:${variant % 3}`, () => drawMagma(richness, variant % 3));
  }

  lantern(): Texture {
    return this.cached("lantern", () => drawLantern());
  }

  building(type: string, size: number): BuildingTextures {
    let result = this.buildingCache.get(type);
    if (result === undefined) {
      result = this.createBuilding(type, size);
      this.buildingCache.set(type, result);
    }
    return result;
  }

  private createBuilding(type: string, size: number): BuildingTextures {
    const original = ORIGINAL_SPRITES[type];
    if (original !== undefined) {
      const resolved = resolveFrames(original, (frame) => this.atlas.has(frame));
      if (resolved !== null) {
        return {
          frames: resolved.frames.map((frame) => this.atlas.get(frame)!),
          size: 1,
          attachments: ORIGINAL_ATTACHMENTS[type] ?? [],
          original: true,
        };
      }
    }
    const art = drawBuilding(type, size);
    return {
      frames: [Texture.from(art.canvas)],
      size: art.size,
      attachments: art.attachments,
      original: false,
    };
  }

  /** The animated construction site of the original game (1×1) */
  underConstruction(): Texture[] {
    const resolved = resolveFrames("UnderConstruction", (frame) => this.atlas.has(frame));
    return resolved?.frames.map((frame) => this.atlas.get(frame)!) ?? [];
  }

  pipe(shape: string): Texture | undefined {
    return this.atlas.get(`GreyPipe${shape}`);
  }

  gate(policy: "open" | "closed" | "contract" | "corporation", alongU: boolean): BuildingTextures {
    const key = `gate:${policy}:${alongU}`;
    let result = this.buildingCache.get(key);
    if (result === undefined) {
      const art = drawGate(policy, alongU);
      result = { frames: [Texture.from(art.canvas)], size: 1, attachments: art.attachments, original: false };
      this.buildingCache.set(key, result);
    }
    return result;
  }

  hub(type: string, size: number, look?: HubStructureLook): BuildingTextures {
    const key = `hub:${type}:${size}:${look?.label ?? ""}:${look?.color ?? ""}:${look?.level ?? 0}:${look?.variant ?? ""}:${look?.stages ?? 0}`;
    let result = this.buildingCache.get(key);
    if (result === undefined) {
      const art: BuildingArt = drawHubStructure(type, size, look);
      result = { frames: [Texture.from(art.canvas)], size, attachments: art.attachments, original: false };
      this.buildingCache.set(key, result);
    }
    return result;
  }

  /** A data URL picture of a building for the menus */
  thumbnail(type: string, size: number): string {
    let url = this.thumbnailCache.get(type);
    if (url !== undefined) {
      return url;
    }
    const original = ORIGINAL_SPRITES[type];
    let source: HTMLCanvasElement;
    if (original !== undefined && this.atlasImage !== null) {
      const resolved = resolveFrames(original, (frame) => frame in this.atlasFrames);
      const frame = resolved === null ? undefined : this.atlasFrames[resolved.frames[0]!]?.frame;
      source = createCanvas(frame?.w ?? 1, frame?.h ?? 1);
      if (frame !== undefined) {
        source
          .getContext("2d")!
          .drawImage(this.atlasImage, frame.x, frame.y, frame.w, frame.h, 0, 0, frame.w, frame.h);
      }
    } else {
      source = drawBuilding(type, size, true).canvas;
    }
    // Square, the bottom (the base of the building) stays visible
    const side = Math.max(source.width, Math.min(source.height, source.width * 1.1));
    const canvas = createCanvas(96, 96);
    const g = canvas.getContext("2d")!;
    const scale = 96 / side;
    g.drawImage(
      source,
      (96 - source.width * scale) / 2,
      96 - source.height * scale,
      source.width * scale,
      source.height * scale,
    );
    url = canvas.toDataURL();
    this.thumbnailCache.set(type, url);
    return url;
  }
}

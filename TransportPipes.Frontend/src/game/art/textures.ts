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
import { TILE_HEIGHT, TILE_WIDTH } from "../constants";
import { createCanvas, hash01, type Attachment } from "./iso";
import { drawPipe } from "./pipes";
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
  /** Generated art: the sprite's anchor is the bottom corner of its ground plate, scaled down from its resolution */
  placement?: { anchorX: number; anchorY: number; scale: number };
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

/** What art/process.mjs made of the generated art (public/art/manifest.json) */
interface ArtManifest {
  resolution: number;
  buildings: Record<
    string,
    {
      w: number;
      h: number;
      ax: number;
      ay: number;
      attachments?: Array<{ kind: Attachment["kind"]; x: number; y: number; size?: number; color?: string }>;
    }
  >;
  rotors: Record<string, { size: number }>;
  terrain: Record<string, { size: number }>;
  props?: Record<string, { w: number; h: number; ax: number; ay: number; decal?: boolean }>;
}

/** A generated prop or decal ready to place: anchored at its foot (props) or middle (decals) */
export interface PropArt {
  texture: Texture;
  anchorX: number;
  anchorY: number;
  scale: number;
}

const TREES = ["treeOak", "treeBirch", "treePine", "treePines", "treeBig"];

const ART_URL = "/art/";

/** A seamless terrain texture covers this many tiles in each direction */
const TERRAIN_CELLS = 4;

function mod(a: number, b: number): number {
  return ((a % b) + b) % b;
}

/** All textures of the game: generated art, the original atlases and procedural art, created once and cached */
export class TextureBank {
  private art: ArtManifest | null = null;
  private readonly artTextures = new Map<string, Texture>();
  private readonly terrainImages = new Map<string, HTMLImageElement>();
  private readonly propImages = new Map<string, HTMLImageElement>();
  private readonly terrainColors = new Map<string, string>();
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
    await bank.loadArt();
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

  /** The generated art, when it has been made (the procedural art stands in for everything else) */
  private async loadArt() {
    try {
      const response = await fetch(`${ART_URL}manifest.json`);
      if (!response.ok) {
        return;
      }
      const art = (await response.json()) as ArtManifest;
      const urls: Array<[string, string]> = [
        ...Object.keys(art.buildings).map((id): [string, string] => [id, `${ART_URL}buildings/${id}.webp`]),
        ...Object.keys(art.rotors).map((id): [string, string] => [
          `${id}-rotor`,
          `${ART_URL}buildings/${id}-rotor.webp`,
        ]),
      ];
      const textures = await Promise.all(urls.map(([, url]) => Assets.load<Texture>(url).catch(() => null)));
      urls.forEach(([key], i) => {
        if (textures[i] !== null && textures[i] !== undefined) {
          this.artTextures.set(key, textures[i]);
        }
      });
      await Promise.all(
        Object.keys(art.terrain).map(async (id) => {
          const image = new Image();
          image.src = `${ART_URL}terrain/${id}.webp`;
          await image.decode();
          this.terrainImages.set(id, image);
        }),
      );
      await Promise.all(
        Object.keys(art.props ?? {}).map(async (id) => {
          const image = new Image();
          image.src = `${ART_URL}props/${id}.webp`;
          await image.decode();
          this.propImages.set(id, image);
        }),
      );
      this.art = art;
    } catch (error) {
      console.warn("The generated art could not be loaded", error);
    }
  }

  /** The generated sprite of a building: anchored at its plate's bottom corner, attachments around it */
  private generatedBuilding(type: string, size: number): BuildingTextures | null {
    const entry = this.art?.buildings[type];
    const texture = this.artTextures.get(type);
    if (this.art === null || entry === undefined || texture === undefined) {
      return null;
    }
    const scale = 1 / this.art.resolution;
    // Attachments are relative to the footprint's bottom-left corner, like the procedural art
    const at = (x: number, y: number) => ({
      x: TILE_WIDTH * size + (x - entry.ax) * scale,
      y: (y - entry.ay) * scale,
    });
    const attachments: Attachment[] = (entry.attachments ?? []).map((a) => ({
      ...at(a.x, a.y),
      kind: a.kind,
      size: a.size ?? 1,
      color: a.color,
    }));
    const rotor = this.artTextures.get(`${type}-rotor`);
    if (rotor !== undefined) {
      for (const a of attachments.filter((t) => t.kind === "rotor")) {
        a.texture = rotor;
        a.scale = scale;
      }
    }
    return {
      frames: [texture],
      size,
      attachments,
      original: false,
      placement: { anchorX: entry.ax / entry.w, anchorY: entry.ay / entry.h, scale },
    };
  }

  /** A generated prop or decal by id (null without generated art) */
  prop(id: string): PropArt | null {
    const entry = this.art?.props?.[id];
    const image = this.propImages.get(id);
    if (entry === undefined || image === undefined || this.art === null) {
      return null;
    }
    const resolution = this.art.resolution;
    return {
      texture: this.cached(`prop:${id}`, () => {
        const canvas = createCanvas(entry.w, entry.h);
        canvas.getContext("2d")!.drawImage(image, 0, 0);
        return canvas;
      }),
      anchorX: entry.ax / entry.w,
      anchorY: entry.ay / entry.h,
      scale: 1 / resolution,
    };
  }

  /** Whether generated trees are there (forests then get them) */
  get hasTrees(): boolean {
    return TREES.every((t) => this.propImages.has(t));
  }

  /**
   * A forest tile: two or three generated trees on it, composed into one sprite (one per tile keeps big forests
   * light). Anchored like a tile: the bottom-left corner of the tile's box.
   */
  forestTile(variant: number): Texture | null {
    if (!this.hasTrees || this.art === null) {
      return null;
    }
    const resolution = this.art.resolution;
    return this.cached(`forestTile:${variant % 12}`, () => {
      const extra = 260;
      const canvas = createCanvas(TILE_WIDTH * 2, TILE_HEIGHT + extra);
      const g = canvas.getContext("2d")!;
      const spots: Array<[number, number]> = [
        [0.5, 0.25],
        [0.28, 0.5],
        [0.72, 0.5],
        [0.5, 0.78],
      ];
      // Back to front: the spots are sorted by depth already
      spots.forEach(([u, v], i) => {
        if (hash01(variant * 31 + i * 7) < 0.3 && i !== 3) {
          return;
        }
        const tree = TREES[Math.floor(hash01(variant * 13 + i * 5) * TREES.length)]!;
        const entry = this.art!.props![tree]!;
        const image = this.propImages.get(tree)!;
        const scale = (0.75 + hash01(variant + i) * 0.35) / resolution;
        const footX = TILE_WIDTH * 2 * u;
        const footY = extra + TILE_HEIGHT * v;
        g.drawImage(
          image,
          footX - entry.ax * scale,
          footY - entry.ay * scale,
          entry.w * scale,
          entry.h * scale,
        );
      });
      return canvas;
    });
  }

  /**
   * A terrain tile cut out of its seamless texture where it lies: a texture covers TERRAIN_CELLS × TERRAIN_CELLS tiles,
   * and each tile maps its square cell of the texture onto its diamond (a true isometric projection), so tiles join
   * without seams and patterns follow the grid.
   */
  terrainAt(type: TerrainType, x: number, y: number, variant: number): Texture {
    const image = this.terrainImages.get(type);
    if (image === undefined) {
      return this.terrain(type, variant);
    }
    const cellX = mod(x, TERRAIN_CELLS);
    const cellY = mod(y, TERRAIN_CELLS);
    return this.cached(`terrainAt:${type}:${cellX}:${cellY}`, () => {
      const canvas = createCanvas(TILE_WIDTH * 2, TILE_HEIGHT);
      const g = canvas.getContext("2d")!;
      g.beginPath();
      g.moveTo(0, TILE_HEIGHT / 2);
      g.lineTo(TILE_WIDTH, 0);
      g.lineTo(TILE_WIDTH * 2, TILE_HEIGHT / 2);
      g.lineTo(TILE_WIDTH, TILE_HEIGHT);
      g.closePath();
      g.clip();
      // Texture pixel → screen: the cell's x axis runs down to the right, its y axis down to the left
      const cell = image.width / TERRAIN_CELLS;
      const a = TILE_WIDTH / cell;
      const b = TILE_HEIGHT / 2 / cell;
      g.setTransform(
        a,
        b,
        -a,
        b,
        TILE_WIDTH - a * cellX * cell + a * cellY * cell,
        -b * cellX * cell - b * cellY * cell,
      );
      // A little bleed over the diamond's edges hides hairline seams
      g.drawImage(image, 0, 0);
      return canvas;
    });
  }

  /** The average colour of a terrain's generated texture (for the far view), or null */
  terrainColor(type: TerrainType): string | null {
    const cached = this.terrainColors.get(type);
    if (cached !== undefined) {
      return cached;
    }
    const image = this.terrainImages.get(type);
    if (image === undefined) {
      return null;
    }
    const canvas = createCanvas(1, 1);
    const g = canvas.getContext("2d")!;
    g.drawImage(image, 0, 0, 1, 1);
    const [r, gr, b] = g.getImageData(0, 0, 1, 1).data;
    const color = `rgb(${r}, ${gr}, ${b})`;
    this.terrainColors.set(type, color);
    return color;
  }

  /** Whether there is a generated texture for a terrain */
  hasTerrainArt(type: TerrainType): boolean {
    return this.terrainImages.has(type);
  }

  /**
   * A soft transition: the neighbour's terrain fading in from one edge of a tile (edge 0: the tile at x-1, 1: x+1,
   * 2: y-1, 3: y+1), drawn over the tile's own terrain
   */
  terrainEdge(type: TerrainType, x: number, y: number, edge: number): Texture | null {
    const image = this.terrainImages.get(type);
    if (image === undefined) {
      return null;
    }
    const cellX = mod(x, TERRAIN_CELLS);
    const cellY = mod(y, TERRAIN_CELLS);
    return this.cached(`terrainEdge:${type}:${cellX}:${cellY}:${edge}`, () => {
      const canvas = createCanvas(TILE_WIDTH * 2, TILE_HEIGHT);
      const g = canvas.getContext("2d")!;
      const cell = image.width / TERRAIN_CELLS;
      const a = TILE_WIDTH / cell;
      const b = TILE_HEIGHT / 2 / cell;
      g.setTransform(
        a,
        b,
        -a,
        b,
        TILE_WIDTH - a * cellX * cell + a * cellY * cell,
        -b * cellX * cell - b * cellY * cell,
      );
      const x0 = cellX * cell;
      const y0 = cellY * cell;
      g.drawImage(image, 0, 0);
      // Keep only a band along the edge, fading out towards the middle (in texture coordinates)
      const reach = cell * 0.42;
      const bands: Array<[number, number, number, number]> = [
        [x0, y0, x0 + reach, y0],
        [x0 + cell, y0, x0 + cell - reach, y0],
        [x0, y0, x0, y0 + reach],
        [x0, y0 + cell, x0, y0 + cell - reach],
      ];
      const [gx0, gy0, gx1, gy1] = bands[edge]!;
      const gradient = g.createLinearGradient(gx0, gy0, gx1, gy1);
      gradient.addColorStop(0, "rgba(0,0,0,0.92)");
      gradient.addColorStop(0.55, "rgba(0,0,0,0.35)");
      gradient.addColorStop(1, "rgba(0,0,0,0)");
      g.globalCompositeOperation = "destination-in";
      g.fillStyle = gradient;
      g.fillRect(x0, y0, cell, cell);
      return canvas;
    });
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
    const generated = this.generatedBuilding(type, size);
    if (generated !== null) {
      return generated;
    }
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

  /** A pipe tile in the style of the generated art, for a tier (anchored like a tile, PIPE_LIFT taller) */
  styledPipe(shape: string, tier: number): Texture {
    return this.cached(`styledPipe:${shape}:${tier}`, () => drawPipe(shape, tier));
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
    if (this.artTextures.has(type)) {
      return `${ART_URL}buildings/${type}.webp`;
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

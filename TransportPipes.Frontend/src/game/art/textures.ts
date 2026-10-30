import { Assets, Rectangle, Texture, type Spritesheet } from "pixi.js";
import type { TerrainType } from "../../api/contracts";
import { resolveFrames } from "../spriteFrames";
import { gateInletContains } from "../connectors";
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
  /** A gate's ground pad, below its inlet and posts. */
  ground?: Texture;
  size: number;
  /** Generated art: the sprite's anchor is the bottom corner of its ground plate, scaled down from its resolution */
  placement?: { anchorX: number; anchorY: number; scale: number };
  attachments: Attachment[];
  /** Hand-drawn sprite of the original game */
  original: boolean;
  /** A bare ground plate while the generated picture is still loading */
  pending?: boolean;
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
      /** Its own resolution (big sprites are stored smaller) */
      r?: number;
      attachments?: Array<{ kind: Attachment["kind"]; x: number; y: number; size?: number; color?: string }>;
    }
  >;
  rotors: Record<string, { size: number }>;
  terrain: Record<string, { size: number }>;
  props?: Record<string, { w: number; h: number; ax: number; ay: number; decal?: boolean }>;
  /** A content hash per sprite (art/process.mjs): asked for as `<path>?v=<hash>`, so browsers can keep it */
  hashes?: Record<string, string>;
  /**
   * The pipe pieces (art/pipes.mjs): one atlas per tier; a frame is a piece trimmed out of its tile box (TILE_WIDTH ×
   * (TILE_HEIGHT + top) world pixels, at the resolution), with where it sat in the box
   */
  pipes?: {
    resolution: number;
    top: number;
    /** The tube's centre above the ground, in world pixels */
    z: number;
    tiers: number[];
    frames: Record<string, { x: number; y: number; w: number; h: number; ox: number; oy: number }>;
  };
}

/** A pipe piece ready to place: relative to the tile's anchor (the bottom-left corner of its box) */
export interface PipeArt {
  texture: Texture;
  x: number;
  y: number;
  scale: number;
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

/** Building pictures load when they are first needed, this many at a time */
const ART_CONCURRENCY = 6;

let shared: Promise<TextureBank> | null = null;

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
  private readonly resourceIcons = new Map<string, Texture | null>();

  /** Cargo on the little belts uses the same painted resource icons as the UI, loaded once when first visible. */
  resourceIcon(resource: string): Texture | null {
    if (!this.resourceIcons.has(resource)) {
      this.resourceIcons.set(resource, null);
      void Assets.load<Texture>(`/art/ui/res-${resource}.webp`)
        .then((texture) => {
          this.resourceIcons.set(resource, texture);
        })
        .catch(() => undefined);
    }
    return this.resourceIcons.get(resource) ?? null;
  }
  private atlasImage: HTMLImageElement | null = null;
  private readonly artRequested = new Set<string>();
  private readonly artFailed = new Set<string>();
  /** Pictures drawn as a placeholder (or left out) so far: the map is drawn again when they arrive */
  private readonly awaited = new Set<string>();
  /** Companies' buildings among them (not the hub's or decorations): the map waits a moment for these */
  private readonly awaitedBuildings = new Set<string>();
  private readonly artQueue: Array<{ id: string; job: () => Promise<void> }> = [];
  private artLoading = 0;
  private warming = false;
  private readonly artListeners = new Set<() => void>();
  private atlasFrames: Record<string, { frame: { x: number; y: number; w: number; h: number } }> = {};

  /**
   * The textures, loaded once for the whole session: started early (on the title screen), so the map is ready sooner.
   * Only the map's own art is waited for; the buildings' pictures come when they are first needed.
   */
  static load(): Promise<TextureBank> {
    shared ??= TextureBank.create().catch((error: unknown) => {
      shared = null;
      throw error;
    });
    return shared;
  }

  private static async create(): Promise<TextureBank> {
    const bank = new TextureBank();
    const [objects, terrain] = await Promise.all([
      Assets.load<Spritesheet>(OBJECT_ATLAS_URL),
      Assets.load<Spritesheet>(TERRAIN_ATLAS_URL),
      bank.loadArt(),
      bank.loadThumbnailAtlas(),
    ]);
    for (const sheet of [objects, terrain]) {
      for (const [name, texture] of Object.entries(sheet.textures)) {
        bank.atlas.set(name, texture);
      }
    }
    return bank;
  }

  /** The original sprites as an image, for DOM thumbnails */
  private async loadThumbnailAtlas() {
    try {
      const json = (await (await fetch(OBJECT_ATLAS_URL)).json()) as { frames: TextureBank["atlasFrames"] };
      this.atlasFrames = json.frames;
      const image = new Image();
      image.src = OBJECT_ATLAS_URL.replace("combinedjson.json", "combinedimage.png");
      await image.decode();
      this.atlasImage = image;
    } catch (error) {
      console.warn("Thumbnails of the original sprites are not available", error);
    }
  }

  /** The generated art's manifest and the map's own art; the buildings' pictures load when they are needed */
  private async loadArt() {
    try {
      const response = await fetch(`${ART_URL}manifest.json`);
      if (!response.ok) {
        return;
      }
      this.art = (await response.json()) as ArtManifest;
      const image = async (path: string) => {
        const element = new Image();
        element.src = this.artUrl(path);
        await element.decode();
        return element;
      };
      await Promise.all([
        // Every map has pipes: the first tier comes with the map's art, the others when they are first needed
        this.loadPipeTier(1),
        ...Object.keys(this.art.terrain).map(async (id) => {
          this.terrainImages.set(id, await image(`terrain/${id}.webp`));
        }),
        ...Object.keys(this.art.props ?? {}).map(async (id) => {
          this.propImages.set(id, await image(`props/${id}.webp`));
        }),
      ]);
    } catch (error) {
      console.warn("The generated art could not be loaded", error);
      this.art = null;
    }
  }

  /** A sprite's URL with its content hash */
  private artUrl(path: string): string {
    const hash = this.art?.hashes?.[path];
    return hash === undefined ? `${ART_URL}${path}` : `${ART_URL}${path}?v=${hash}`;
  }

  /**
   * Loads a building's picture (and its rotor) the first time it is needed. Companies' buildings (urgent) go first,
   * the latest first; the hub's buildings and the decorations after them.
   */
  private requestArt(id: string, urgent: boolean): void {
    const art = this.art;
    if (art === null) {
      return;
    }
    if (this.artRequested.has(id)) {
      // Still waiting in the queue: to the front now that it is urgent
      const queued = this.artQueue.findIndex((t) => t.id === id);
      if (urgent && queued > 0) {
        this.artQueue.unshift(...this.artQueue.splice(queued, 1));
      }
      return;
    }
    const job = async () => {
      const paths: Array<[string, string]> = [[id, `buildings/${id}.webp`]];
      if (art.rotors[id] !== undefined) {
        paths.push([`${id}-rotor`, `buildings/${id}-rotor.webp`]);
      }
      const textures = await Promise.all(
        paths.map(([, path]) => Assets.load<Texture>(this.artUrl(path)).catch(() => null)),
      );
      paths.forEach(([key], i) => {
        const texture = textures[i];
        if (texture !== null && texture !== undefined) {
          this.artTextures.set(key, texture);
        }
      });
      if (!this.artTextures.has(id)) {
        this.artFailed.add(id);
      }
      this.awaitedBuildings.delete(id);
      if (this.awaited.delete(id)) {
        this.artListeners.forEach((listener) => listener());
      }
    };
    this.artRequested.add(id);
    if (urgent) {
      this.artQueue.unshift({ id, job });
    } else {
      this.artQueue.push({ id, job });
    }
    this.pumpArt();
  }

  /**
   * Calls the listener when pictures the map waited for have arrived; returns how to stop. One bank serves every
   * engine of the session (a new one after a reconnect), so each engine removes only its own listener.
   */
  onArtArrived(listener: () => void): () => void {
    this.artListeners.add(listener);
    return () => this.artListeners.delete(listener);
  }

  /** Whether pictures of companies' buildings the map shows are still loading */
  get loadingBuildings(): boolean {
    return this.awaitedBuildings.size > 0;
  }

  private pumpArt() {
    while (this.artLoading < ART_CONCURRENCY && this.artQueue.length > 0) {
      const { job } = this.artQueue.shift()!;
      this.artLoading++;
      void job().finally(() => {
        this.artLoading--;
        this.pumpArt();
      });
    }
  }

  /** The state of a building's generated picture: there, still loading (it is asked for now), or none */
  private artState(id: string, urgent: boolean): Texture | "loading" | null {
    if (this.art?.buildings[id] === undefined || this.artFailed.has(id)) {
      return null;
    }
    const texture = this.artTextures.get(id);
    if (texture !== undefined) {
      return texture;
    }
    this.awaited.add(id);
    if (urgent) {
      this.awaitedBuildings.add(id);
    }
    this.requestArt(id, urgent);
    return "loading";
  }

  /**
   * Once the map is up: the other building pictures go into the browser's cache, one by one and only while nothing
   * else loads, so they are there without a wait later (not on a data saver)
   */
  async warmCache() {
    const art = this.art;
    const connection = (navigator as Navigator & { connection?: { saveData?: boolean } }).connection;
    if (art === null || this.warming || connection?.saveData === true) {
      return;
    }
    this.warming = true;
    const paths = [
      ...Object.keys(art.buildings).map((id) => [id, `buildings/${id}.webp`]),
      ...Object.keys(art.rotors).map((id) => [id, `buildings/${id}-rotor.webp`]),
    ];
    for (const [id, path] of paths) {
      while (this.artLoading > 0) {
        await new Promise((resolve) => setTimeout(resolve, 400));
      }
      if (!this.artRequested.has(id!)) {
        await fetch(this.artUrl(path!), { priority: "low" } as RequestInit).catch(() => undefined);
      }
    }
  }

  /** A bare ground plate in the colours of the generated art, while a building's picture loads */
  private placeholder(size: number): BuildingTextures {
    const texture = this.cached(`placeholder:${size}`, () => {
      const width = TILE_WIDTH * 2 * size;
      const height = TILE_HEIGHT * size;
      const edge = 8;
      const canvas = createCanvas(width, height + edge);
      const g = canvas.getContext("2d")!;
      const top = () => {
        g.beginPath();
        g.moveTo(0, height / 2);
        g.lineTo(width / 2, 0);
        g.lineTo(width, height / 2);
        g.lineTo(width / 2, height);
        g.closePath();
      };
      // The plate's sides, then its top
      g.fillStyle = "#9c8a68";
      g.beginPath();
      g.moveTo(0, height / 2);
      g.lineTo(width / 2, height);
      g.lineTo(width, height / 2);
      g.lineTo(width, height / 2 + edge);
      g.lineTo(width / 2, height + edge);
      g.lineTo(0, height / 2 + edge);
      g.closePath();
      g.fill();
      top();
      g.fillStyle = "#d8c9a6";
      g.fill();
      g.lineWidth = 2;
      g.strokeStyle = "rgba(90, 70, 40, 0.55)";
      g.stroke();
      return canvas;
    });
    return {
      frames: [texture],
      size,
      attachments: [],
      original: false,
      placement: { anchorX: 0.5, anchorY: 1, scale: 1 },
      pending: true,
    };
  }

  /** The generated sprite of a building: anchored at its plate's bottom corner, attachments around it */
  private generatedBuilding(type: string, size: number, urgent = false): BuildingTextures | null {
    const entry = this.art?.buildings[type];
    const texture = this.artState(type, urgent);
    if (this.art === null || entry === undefined || texture === null) {
      return null;
    }
    if (texture === "loading") {
      return this.placeholder(size);
    }
    const scale = 1 / (entry.r ?? this.art.resolution);
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
   * light). Anchored at the bottom middle of the tile's box; drawn at the art's resolution, with room for the crowns
   * that reach past the tile.
   */
  forestTile(variant: number): PropArt | null {
    if (!this.hasTrees || this.art === null) {
      return null;
    }
    const resolution = this.art.resolution;
    const side = 80;
    const extra = 260;
    const width = TILE_WIDTH * 2 + side * 2;
    const height = TILE_HEIGHT + extra;
    const texture = this.cached(`forestTile:${variant % 12}`, () => {
      const canvas = createCanvas(Math.ceil(width * resolution), Math.ceil(height * resolution));
      const g = canvas.getContext("2d")!;
      g.scale(resolution, resolution);
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
        const footX = side + TILE_WIDTH * 2 * u;
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
    return { texture, anchorX: 0.5, anchorY: 1, scale: 1 / resolution };
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

  /** A generated sprite by its art id (town fillers and other decorations), or null (also while it loads) */
  generated(id: string, size: number): BuildingTextures | null {
    const art = this.generatedBuilding(id, size);
    return art?.pending ? null : art;
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
      if (result.pending !== true) {
        this.buildingCache.set(type, result);
      }
    }
    return result;
  }

  private createBuilding(type: string, size: number): BuildingTextures {
    const generated = this.generatedBuilding(type, size, true);
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

  private readonly pipeAtlases = new Map<number, Texture>();
  private readonly pipeTiersLoading = new Set<number>();
  private readonly pipePieces = new Map<string, Texture>();

  private async loadPipeTier(tier: number) {
    const pipes = this.art?.pipes;
    if (pipes === undefined || !pipes.tiers.includes(tier) || this.pipeTiersLoading.has(tier)) {
      return;
    }
    this.pipeTiersLoading.add(tier);
    try {
      this.pipeAtlases.set(tier, await Assets.load<Texture>(this.artUrl(`pipes/tier${tier}.webp`)));
      this.artListeners.forEach((listener) => listener());
    } catch (error) {
      console.warn(`The pipes of tier ${tier} could not be loaded`, error);
    }
  }

  /**
   * A generated pipe piece: the sides it joins in u, r, d, l order ("" alone, "ur" a bend, "urdl" a cross), or
   * "socket-<side>" for where it plugs into a building. A tier that is still loading shows as the first tier.
   */
  pipePiece(shape: string, tier: number): PipeArt | null {
    const pipes = this.art?.pipes;
    const frame = pipes?.frames[shape];
    if (pipes === undefined || frame === undefined) {
      return null;
    }
    if (!this.pipeAtlases.has(tier)) {
      void this.loadPipeTier(tier);
      tier = 1;
    }
    const atlas = this.pipeAtlases.get(tier);
    if (atlas === undefined) {
      return null;
    }
    const key = `${tier}:${shape}`;
    let texture = this.pipePieces.get(key);
    if (texture === undefined) {
      texture = new Texture({
        source: atlas.source,
        frame: new Rectangle(frame.x, frame.y, frame.w, frame.h),
      });
      this.pipePieces.set(key, texture);
    }
    return {
      texture,
      x: frame.ox / pipes.resolution,
      y: frame.oy / pipes.resolution - (TILE_HEIGHT + pipes.top),
      scale: 1 / pipes.resolution,
    };
  }

  /** How high the generated pipes' tubes run above the ground (world pixels), for the flowing dots */
  get pipeHeight(): number {
    return this.art?.pipes?.z ?? 22;
  }

  private readonly gateMasks = new Map<string, ImageData>();

  /** Hit the inlet, posts and barrier above the ground. Coordinates use the sprite's bottom-left anchor. */
  gateContains(alongU: boolean, x: number, y: number, tolerance: number): boolean {
    if (gateInletContains(x, y, tolerance)) return true;
    this.gate("open", alongU);
    const mask = this.gateMasks.get(`gate:open:${alongU}`)!;
    const px = Math.round(x);
    const py = Math.round(y + mask.height);
    const radius = Math.ceil(tolerance);
    if (px < -radius || px >= mask.width + radius || py < -radius || py >= mask.height + radius) {
      return false;
    }
    for (let dy = -radius; dy <= radius; dy++) {
      for (let dx = -radius; dx <= radius; dx++) {
        const sx = px + dx;
        const sy = py + dy;
        if (
          dx * dx + dy * dy <= radius * radius &&
          sx >= 0 &&
          sx < mask.width &&
          sy >= 0 &&
          sy < mask.height &&
          mask.data[(sy * mask.width + sx) * 4 + 3]! > 128
        ) {
          return true;
        }
      }
    }
    return false;
  }

  gate(policy: "open" | "closed" | "contract" | "corporation", alongU: boolean): BuildingTextures {
    const key = `gate:${policy}:${alongU}`;
    let result = this.buildingCache.get(key);
    if (result === undefined) {
      const art = drawGate(policy, alongU);
      this.gateMasks.set(
        key,
        art.canvas.getContext("2d")!.getImageData(0, 0, art.canvas.width, art.canvas.height),
      );
      result = {
        frames: [Texture.from(art.canvas)],
        ground: art.ground === undefined ? undefined : Texture.from(art.ground),
        size: 1,
        attachments: art.attachments,
        original: false,
      };
      this.buildingCache.set(key, result);
    }
    return result;
  }

  hub(type: string, size: number, look?: HubStructureLook): BuildingTextures {
    // Generated art: the hub's buildings, and the Wonders (they rise stage by stage, see Engine)
    const wonder =
      (type === "wonderSite" || type === "eraWonderSite") && look?.variant ? `wonder-${look.variant}` : null;
    // A statue: one of three looks
    const statue = type === "statue" && look?.variant ? `hub-statue-${look.variant}` : null;
    const generated = this.generatedBuilding(wonder ?? statue ?? `hub-${type}`, size);
    if (generated !== null) {
      return generated;
    }
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
    if (this.art?.buildings[type] !== undefined && !this.artFailed.has(type)) {
      return this.artUrl(`buildings/${type}.webp`);
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

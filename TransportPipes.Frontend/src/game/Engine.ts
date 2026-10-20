import { AnimatedSprite, Application, Container, Graphics, Sprite, Text, Texture } from "pixi.js";
import type {
  BlueprintDto,
  BuildingDto,
  CompanyDto,
  ConductorDto,
  DistrictDto,
  OutpostDto,
  TerrainType,
} from "../api/contracts";
import { createCanvas, hash01 } from "./art/iso";
import { TERRAIN_MAP_COLORS } from "./art/terrain";
import { TextureBank, TERRAIN_VARIANTS, type BuildingTextures } from "./art/textures";
import { Camera, chunkIntersectsView } from "./camera";
import { CLICK_TOLERANCE_PX, FAR_ZOOM, TILE_HEIGHT, TILE_WIDTH } from "./constants";
import type { Content } from "./content";
import { diagonalToScreen, worldToTile, type TilePosition } from "./isometric";
import {
  CHUNK_SIZE,
  DEEP_OFFSET,
  DISTRICT_SIZE,
  ESTATE_SIZE,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
  chunkOf,
  districtOf,
  districtOriginX,
  districtOriginY,
  isDeep,
  isParcelUnlocked,
  tileKey,
  zoneAt,
} from "./layout";
import { BuildingEffects, dotTexture } from "./render/effects";
import { canPlace, footprintOrigin, routePipe, type Placement } from "./rules";
import { chunkKey, type WorldModel } from "./world";

export type Tool =
  | { kind: "select" }
  | { kind: "build"; type: string; crate?: number }
  | { kind: "pipe"; tier: number }
  | { kind: "bulldoze" }
  | { kind: "move"; building: number }
  | { kind: "terraform"; action: TerraformAction; resource?: string }
  /** Drag a rectangle of the estate to save it as a blueprint */
  | { kind: "blueprintArea" }
  | { kind: "blueprint"; blueprint: BlueprintDto };

/** The top-left tile of a blueprint when the pointer is at a tile (the pointer is in its middle) */
export function blueprintOrigin(pointerX: number, pointerY: number, blueprint: BlueprintDto) {
  return {
    x: pointerX - Math.floor((blueprint.width - 1) / 2),
    y: pointerY - Math.floor((blueprint.height - 1) / 2),
  };
}

export type TerraformAction = "drain" | "lake" | "raise" | "flatten" | "forest" | "seedDeposit";

export type Overlay = "none" | "owners" | "wind" | "sun" | "deposits";

export interface EngineState {
  content: Content;
  company: CompanyDto | null;
  era: number;
  tool: Tool;
  overlay: Overlay;
  /** A pipe route to show (Connect), not built yet */
  plannedRoute: TilePosition[] | null;
  selectedBuilding: number | null;
  /** The network shown in the inspector, with the paths of its full links */
  selectedNetwork: { id: number; bottlenecks: number[][] } | null;
  /** Milliseconds to add to Date.now() to get the server's clock */
  serverOffsetMs: number;
  /** A wilderness tile the player looks at (an outpost or a claim), and whether that claim is possible */
  wildTile: TilePosition | null;
  hintTile: { x: number; y: number; size: number } | null;
  outpostCheck: { error: string | null; x: number; y: number } | null;
}

export interface EngineCallbacks {
  getState(): EngineState;
  /** False while there is no connection: nothing is requested */
  canRequest(): boolean;
  requestChunks(pairs: number[]): void;
  unwatchChunks(pairs: number[]): void;
  requestDistrict(x: number, y: number): void;
  onHover(tile: TilePosition | null, placement: Placement | null): void;
  onClick(tile: TilePosition, building: BuildingDto | undefined): void;
  onPipeRoute(route: TilePosition[] | null): void;
  onPlacePipes(route: TilePosition[]): void;
  onRemovePipes(tiles: TilePosition[]): void;
  onPlaceBuilding(type: string, x: number, y: number): void;
  onMoveBuilding(id: number, x: number, y: number): void;
  onSelectArea(x: number, y: number, width: number, height: number): void;
  /** The camera went down into the Deep Level, or back up */
  onLayer(deep: boolean): void;
  onPlaceBlueprint(blueprint: BlueprintDto, x: number, y: number): void;
  onZoom(scale: number): void;
}

const CHUNK_MARGIN = 1;
const PIPE_TINTS: Record<number, number> = {
  1: 0xffffff,
  2: 0xf0a060,
  3: 0x8f9bb3,
  4: 0xb5f0ff,
  5: 0xc7a6ff,
};

/** Bottom-left corner (sprite anchor) of a footprint of size×size tiles with its top-left tile at (x, y) */
export function footprintAnchor(x: number, y: number, size: number): { x: number; y: number } {
  return { x: TILE_WIDTH * (x - y - size + 1), y: (TILE_HEIGHT / 2) * (x + y + 2 * size - 2) };
}

/** Centre of a tile's diamond */
export function tileCenter(x: number, y: number): { x: number; y: number } {
  const anchor = diagonalToScreen(x, y);
  return { x: anchor.x + TILE_WIDTH, y: anchor.y - TILE_HEIGHT / 2 };
}

/** Blue (0) to red (1) */
function heat(t: number): number {
  const r = Math.round(40 + 215 * t);
  const g = Math.round(120 + 80 * Math.sin(Math.PI * t));
  const b = Math.round(255 - 215 * t);
  return (r << 16) | (g << 8) | b;
}

/** Dim grey-blue (0) to bright yellow (1) */
function sunHeat(t: number): number {
  const r = Math.round(70 + 185 * t);
  const g = Math.round(80 + 150 * t);
  const b = Math.round(120 - 90 * t);
  return (r << 16) | (g << 8) | b;
}

/** The pipe sprite for a tile: the sides it connects to ("urdl"); a straight piece when it connects to one side */
function pipeShapeOf(x: number, y: number, connects: (x: number, y: number) => boolean): string {
  let shape = "";
  let horizontal = 0;
  let vertical = 0;
  if (connects(x, y - 1)) {
    shape += "u";
    vertical++;
  }
  if (connects(x + 1, y)) {
    shape += "r";
    horizontal++;
  }
  if (connects(x, y + 1)) {
    shape += "d";
    vertical++;
  }
  if (connects(x - 1, y)) {
    shape += "l";
    horizontal++;
  }
  if (shape === "") {
    return "urdl";
  }
  if (horizontal + vertical < 2) {
    return horizontal > vertical ? "rl" : "ud";
  }
  return shape;
}

/** Puts a building sprite on its footprint (the bottom-left corner of the footprint's box is at `anchor`) */
function placeArt(sprite: Sprite, art: BuildingTextures, anchor: { x: number; y: number }, size: number) {
  if (art.placement !== undefined) {
    sprite.anchor.set(art.placement.anchorX, art.placement.anchorY);
    sprite.scale.set(art.placement.scale);
    sprite.position.set(anchor.x + TILE_WIDTH * size, anchor.y);
  } else {
    sprite.anchor.set(0, 1);
    sprite.position.set(anchor.x, anchor.y);
  }
}

/** Which terrain bleeds over which at a border (man-made ground keeps a crisp edge) */
const BLEND_PRIORITY: Partial<Record<TerrainType, number>> = {
  water: 0,
  beach: 1,
  desert: 2,
  grass: 3,
  hills: 4,
  forest: 5,
  mountain: 6,
};

/** The four neighbours in the order of TextureBank.terrainEdge's edges */
const NEIGHBOURS: ReadonlyArray<readonly [number, number]> = [
  [-1, 0],
  [1, 0],
  [0, -1],
  [0, 1],
];

/** The generated decal of a deposit: depositIronOre for ironOre */
function depositArt(deposit: string): string {
  return `deposit${deposit[0]!.toUpperCase()}${deposit.slice(1)}`;
}

function traceFootprint(g: Graphics, x: number, y: number, size: number, height = size) {
  const top = diagonalToScreen(x, y);
  const right = diagonalToScreen(x + size - 1, y);
  const bottom = diagonalToScreen(x + size - 1, y + height - 1);
  const left = diagonalToScreen(x, y + height - 1);
  g.moveTo(top.x + TILE_WIDTH, top.y - TILE_HEIGHT);
  g.lineTo(right.x + TILE_WIDTH * 2, right.y - TILE_HEIGHT / 2);
  g.lineTo(bottom.x + TILE_WIDTH, bottom.y);
  g.lineTo(left.x, left.y - TILE_HEIGHT / 2);
  g.closePath();
}

interface ObjectView {
  display: Container;
  effects: BuildingEffects | null;
  building: BuildingDto | null;
  machine: AnimatedSprite | null;
  /** The other animated members of a complex */
  machines?: AnimatedSprite[];
  idle: Container | null;
  progress: Graphics | null;
}

interface DistrictView {
  graphics: Graphics;
  labels: Container;
  hub: ObjectView[];
}

/** What is drawn for one chunk */
class ChunkView {
  readonly cx: number;
  readonly cy: number;
  ground: Container | null = null;
  overlay: Graphics | null = null;
  mini: Sprite | null = null;
  objects: ObjectView[] = [];
  loaded = false;
  requested = false;

  constructor(cx: number, cy: number) {
    this.cx = cx;
    this.cy = cy;
  }
}

/**
 * The map: camera, chunk streaming, terrain at two levels of detail, buildings and pipes in depth order, estates and
 * parcels, flows, animated details and the tools. Framework-agnostic; React talks to it through the callbacks.
 */
export class Engine {
  private readonly camera = new Camera();
  private readonly worldLayer = new Container();
  private readonly groundLayer = new Container();
  private readonly miniLayer = new Container();
  private readonly estateLayer = new Container();
  /** The player's Deep Level: its outline, parcels and the dig */
  private readonly deepGraphics = new Graphics();
  private readonly deepLabels = new Container();
  private deep = false;
  private readonly overlayLayer = new Container();
  private overlay: Overlay = "none";
  private readonly highlightLayer = new Graphics();
  private readonly objectLayer = new Container({ sortableChildren: true });
  private readonly flowLayer = new Container();
  private readonly ghostLayer = new Container();
  private readonly labelLayer = new Container();
  private readonly chunks = new Map<string, ChunkView>();
  private readonly districtViews = new Map<string, DistrictView>();
  private readonly effects = new Set<ObjectView>();
  private readonly dirtyChunks = new Set<string>();
  private flowsDirty = true;
  private districtsDirty = true;
  private flowDots: Array<{
    sprite: Sprite;
    points: Array<{ x: number; y: number }>;
    length: number;
    offset: number;
    speed: number;
  }> = [];
  private far = false;
  private time = 0;

  // Input
  private pointerX = 0;
  private pointerY = 0;
  private pointerInside = false;
  private dragging = false;
  private dragButton = 0;
  private dragPointerId: number | null = null;
  private dragDeltaX = 0;
  /** Two fingers on the map (touch screens) */
  private pinching = false;
  private dragDeltaY = 0;
  private pressX = 0;
  private pressY = 0;
  private movedBeyondClick = false;
  private pipeStart: TilePosition | null = null;
  /** The first corner of a blueprint area being dragged */
  private areaStart: TilePosition | null = null;
  private pipeRoute: TilePosition[] | null = null;
  private bulldozeTiles = new Map<number, TilePosition>();
  private hovered: TilePosition | null = null;
  private lastToolKey = "";
  private readonly keys = new Set<string>();
  private readonly removeListeners: Array<() => void> = [];
  private destroyed = false;

  private readonly app: Application;
  private readonly textures: TextureBank;
  private readonly world: WorldModel;
  private readonly callbacks: EngineCallbacks;

  private constructor(
    app: Application,
    textures: TextureBank,
    world: WorldModel,
    callbacks: EngineCallbacks,
  ) {
    this.app = app;
    this.textures = textures;
    this.world = world;
    this.callbacks = callbacks;
    this.worldLayer.addChild(
      this.groundLayer,
      this.miniLayer,
      this.overlayLayer,
      this.estateLayer,
      this.highlightLayer,
      this.objectLayer,
      this.flowLayer,
      this.ghostLayer,
      this.labelLayer,
    );
    app.stage.addChild(this.worldLayer);
    // The far view starts hidden (it is switched when the zoom crosses FAR_ZOOM)
    this.miniLayer.visible = false;
    this.registerInput(app.canvas);
    app.ticker.add(this.update, this);
    this.removeListeners.push(
      world.onChange((change) => {
        change.chunks.forEach((key) => this.dirtyChunks.add(key));
        if (change.flows) {
          this.flowsDirty = true;
        }
        if (change.districts) {
          this.districtsDirty = true;
          // Gates show their policy, owners' colours their flags
          for (const key of this.chunks.keys()) {
            this.dirtyChunks.add(key);
          }
        }
      }),
    );
  }

  static async create(host: HTMLElement, world: WorldModel, callbacks: EngineCallbacks): Promise<Engine> {
    const app = new Application();
    await app.init({
      resizeTo: host,
      background: "#0d2b33",
      antialias: true,
      autoDensity: true,
      resolution: window.devicePixelRatio || 1,
    });
    let textures: TextureBank;
    try {
      textures = await TextureBank.load();
    } catch (error) {
      app.destroy(true);
      throw error;
    }
    app.canvas.classList.add("game-canvas");
    host.appendChild(app.canvas);
    return new Engine(app, textures, world, callbacks);
  }

  get textureBank(): TextureBank {
    return this.textures;
  }

  get scale(): number {
    return this.camera.scale;
  }

  /** Centres the view on a tile */
  lookAt(x: number, y: number, scale?: number) {
    if (scale !== undefined) {
      this.camera.zoomTo(scale, 0, 0);
      this.callbacks.onZoom(this.camera.scale);
    }
    const center = tileCenter(x, y);
    this.camera.centerOn(center.x, center.y, this.app.screen.width, this.app.screen.height);
  }

  zoomBy(steps: number) {
    if (this.camera.zoomBy(steps, this.app.screen.width / 2, this.app.screen.height / 2)) {
      this.callbacks.onZoom(this.camera.scale);
    }
  }

  /** Everything is drawn again (after a reconnect, a new company) */
  invalidate() {
    for (const key of this.chunks.keys()) {
      this.dirtyChunks.add(key);
    }
    this.districtsDirty = true;
    this.flowsDirty = true;
  }

  /** Chunks the client should fetch again (e.g. after a reconnect) */
  loadedChunkPairs(): number[] {
    const pairs: number[] = [];
    for (const chunk of this.chunks.values()) {
      pairs.push(chunk.cx, chunk.cy);
    }
    return pairs;
  }

  destroy() {
    if (this.destroyed) {
      return;
    }
    this.destroyed = true;
    this.app.ticker.remove(this.update, this);
    this.removeListeners.forEach((remove) => remove());
    this.app.destroy(true, { children: true });
  }

  // ---------------------------------------------------------------- Input

  private registerInput(canvas: HTMLCanvasElement) {
    const toLocal = (event: PointerEvent | WheelEvent) => {
      const bounds = canvas.getBoundingClientRect();
      return { x: event.clientX - bounds.left, y: event.clientY - bounds.top };
    };

    // Two fingers: pinch to zoom (and move the map with their middle)
    const touches = new Map<number, { x: number; y: number }>();
    let pinch: { distance: number; x: number; y: number } | null = null;
    const pinchState = () => {
      const [a, b] = [...touches.values()];
      return { distance: Math.hypot(a!.x - b!.x, a!.y - b!.y), x: (a!.x + b!.x) / 2, y: (a!.y + b!.y) / 2 };
    };
    const releaseTouch = (event: PointerEvent) => {
      touches.delete(event.pointerId);
      if (touches.size < 2) {
        pinch = null;
        this.pinching = false;
      }
    };

    const onPointerDown = (event: PointerEvent) => {
      if (event.pointerType === "touch") {
        touches.set(event.pointerId, toLocal(event));
        if (touches.size === 2) {
          // The first finger's drag (or tap) becomes a pinch
          if (this.dragging) {
            this.dragging = false;
            this.dragPointerId = null;
            this.cancelToolDrag();
          }
          pinch = pinchState();
          this.pinching = true;
          return;
        }
      }
      if (this.dragging || pinch !== null) {
        return;
      }
      const local = toLocal(event);
      this.pointerX = this.pressX = local.x;
      this.pointerY = this.pressY = local.y;
      this.pointerInside = true;
      this.dragging = true;
      this.dragButton = event.button;
      this.dragPointerId = event.pointerId;
      this.movedBeyondClick = false;
      canvas.setPointerCapture(event.pointerId);
      const tool = this.callbacks.getState().tool;
      if (event.button === 0 && tool.kind === "pipe") {
        this.pipeStart = this.camera.screenToTile(local.x, local.y);
        this.updatePipeRoute();
      }
      if (event.button === 0 && tool.kind === "bulldoze") {
        this.bulldozeTiles.clear();
        this.addBulldozeTile();
      }
      if (event.button === 0 && tool.kind === "blueprintArea") {
        this.areaStart = this.camera.screenToTile(local.x, local.y);
      }
    };

    const onPointerMove = (event: PointerEvent) => {
      const local = toLocal(event);
      if (event.pointerType === "touch" && touches.has(event.pointerId)) {
        touches.set(event.pointerId, local);
        if (pinch !== null && touches.size === 2) {
          const now = pinchState();
          if (
            pinch.distance > 0 &&
            this.camera.zoomTo(this.camera.scale * (now.distance / pinch.distance), now.x, now.y)
          ) {
            this.callbacks.onZoom(this.camera.scale);
          }
          this.dragDeltaX += now.x - pinch.x;
          this.dragDeltaY += now.y - pinch.y;
          pinch = now;
          return;
        }
      }
      if (this.dragging && event.pointerId === this.dragPointerId) {
        if (Math.hypot(local.x - this.pressX, local.y - this.pressY) > CLICK_TOLERANCE_PX) {
          this.movedBeyondClick = true;
        }
        if (!this.isToolDrag()) {
          this.dragDeltaX += local.x - this.pointerX;
          this.dragDeltaY += local.y - this.pointerY;
        }
      }
      this.pointerX = local.x;
      this.pointerY = local.y;
      this.pointerInside = true;
    };

    const endDrag = (event: PointerEvent, completed: boolean) => {
      if (!this.dragging || event.pointerId !== this.dragPointerId) {
        return;
      }
      const wasToolDrag = this.isToolDrag();
      this.dragging = false;
      this.dragPointerId = null;
      if (canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
      if (!completed) {
        this.cancelToolDrag();
        return;
      }
      if (wasToolDrag) {
        this.finishToolDrag();
      } else if (!this.movedBeyondClick && this.dragButton === 0) {
        this.click(this.camera.screenToTile(this.pointerX, this.pointerY));
      }
    };

    const onWheel = (event: WheelEvent) => {
      event.preventDefault();
      if (event.deltaY === 0) {
        return;
      }
      const local = toLocal(event);
      const steps = Math.max(-3, Math.min(3, -event.deltaY / 100)) || (event.deltaY < 0 ? 1 : -1);
      if (this.camera.zoomBy(steps, local.x, local.y)) {
        this.callbacks.onZoom(this.camera.scale);
      }
    };

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }
      this.keys.add(event.key.toLowerCase());
    };
    const onKeyUp = (event: KeyboardEvent) => this.keys.delete(event.key.toLowerCase());
    const onBlur = () => this.keys.clear();

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", (event) => {
      releaseTouch(event);
      endDrag(event, true);
    });
    canvas.addEventListener("pointercancel", (event) => {
      releaseTouch(event);
      endDrag(event, false);
    });
    canvas.addEventListener("pointerleave", () => {
      if (!this.dragging) {
        this.pointerInside = false;
      }
    });
    canvas.addEventListener("contextmenu", (event) => event.preventDefault());
    canvas.addEventListener("wheel", onWheel, { passive: false });
    window.addEventListener("keydown", onKeyDown);
    window.addEventListener("keyup", onKeyUp);
    window.addEventListener("blur", onBlur);
    this.removeListeners.push(() => {
      window.removeEventListener("keydown", onKeyDown);
      window.removeEventListener("keyup", onKeyUp);
      window.removeEventListener("blur", onBlur);
    });
  }

  /** Left-dragging with the pipe, bulldoze or blueprint area tool draws instead of panning */
  private isToolDrag(): boolean {
    const tool = this.callbacks.getState().tool;
    return (
      this.dragButton === 0 &&
      (tool.kind === "pipe" || tool.kind === "bulldoze" || tool.kind === "blueprintArea")
    );
  }

  /** The rectangle between the first corner of a blueprint area and the pointer */
  private areaRect(): { x: number; y: number; width: number; height: number } | null {
    if (this.areaStart === null) {
      return null;
    }
    const tile = this.camera.screenToTile(this.pointerX, this.pointerY);
    return {
      x: Math.min(this.areaStart.x, tile.x),
      y: Math.min(this.areaStart.y, tile.y),
      width: Math.abs(tile.x - this.areaStart.x) + 1,
      height: Math.abs(tile.y - this.areaStart.y) + 1,
    };
  }

  private cancelToolDrag() {
    this.areaStart = null;
    this.pipeStart = null;
    this.pipeRoute = null;
    this.bulldozeTiles.clear();
    this.callbacks.onPipeRoute(null);
  }

  private finishToolDrag() {
    const tool = this.callbacks.getState().tool;
    if (tool.kind === "pipe" && this.pipeRoute !== null && this.pipeRoute.length > 0) {
      this.callbacks.onPlacePipes(this.pipeRoute);
    }
    if (tool.kind === "bulldoze" && this.bulldozeTiles.size > 0) {
      const tiles = [...this.bulldozeTiles.values()];
      if (tiles.length === 1 && this.world.buildingAt(tiles[0]!.x, tiles[0]!.y) !== undefined) {
        this.click(tiles[0]!);
      } else {
        this.callbacks.onRemovePipes(tiles.filter((t) => this.world.conductorAt(t.x, t.y)?.kind === "pipe"));
      }
    }
    const area = tool.kind === "blueprintArea" ? this.areaRect() : null;
    if (area !== null) {
      this.callbacks.onSelectArea(area.x, area.y, area.width, area.height);
    }
    this.cancelToolDrag();
  }

  private addBulldozeTile() {
    const tile = this.camera.screenToTile(this.pointerX, this.pointerY);
    this.bulldozeTiles.set(tileKey(tile.x, tile.y), tile);
  }

  private click(tile: TilePosition) {
    const state = this.callbacks.getState();
    const tool = state.tool;
    if (tool.kind === "build" && state.company !== null) {
      const definition = state.content.building(tool.type);
      if (definition !== undefined) {
        const origin = footprintOrigin(tile.x, tile.y, definition.size);
        this.callbacks.onPlaceBuilding(tool.type, origin.x, origin.y);
      }
      return;
    }
    if (tool.kind === "move") {
      const building = this.world.buildings.get(tool.building);
      if (building !== undefined) {
        const origin = footprintOrigin(tile.x, tile.y, building.size);
        this.callbacks.onMoveBuilding(building.id, origin.x, origin.y);
      }
      return;
    }
    if (tool.kind === "blueprint") {
      const origin = blueprintOrigin(tile.x, tile.y, tool.blueprint);
      this.callbacks.onPlaceBlueprint(tool.blueprint, origin.x, origin.y);
      return;
    }
    this.callbacks.onClick(tile, this.world.buildingAt(tile.x, tile.y));
  }

  private updatePipeRoute() {
    const state = this.callbacks.getState();
    if (this.pipeStart === null || state.company === null) {
      return;
    }
    const end = this.camera.screenToTile(this.pointerX, this.pointerY);
    const route = routePipe(this.world, state.company, this.pipeStart, end, state.content);
    this.pipeRoute = route;
    this.callbacks.onPipeRoute(route);
  }

  // ---------------------------------------------------------------- Frame

  private update() {
    const deltaSeconds = this.app.ticker.deltaMS / 1000;
    this.time += deltaSeconds;
    this.panWithKeys(deltaSeconds);
    this.camera.update(
      (this.dragging && !this.isToolDrag()) || this.pinching,
      this.dragDeltaX,
      this.dragDeltaY,
    );
    this.dragDeltaX = 0;
    this.dragDeltaY = 0;
    this.worldLayer.scale.set(this.camera.scale);
    this.worldLayer.position.set(
      this.camera.offsetX * this.camera.scale,
      this.camera.offsetY * this.camera.scale,
    );

    const far = this.camera.scale < FAR_ZOOM;
    if (far !== this.far) {
      this.far = far;
      this.groundLayer.visible = !far;
      this.objectLayer.visible = !far;
      this.flowLayer.visible = !far;
      this.miniLayer.visible = far;
      this.districtsDirty = true;
    }

    this.streamChunks();
    const overlay = this.callbacks.getState().overlay;
    if (overlay !== this.overlay) {
      this.overlay = overlay;
      this.flowLayer.alpha = overlay === "none" ? 1 : 0.35;
      for (const chunk of this.chunks.values()) {
        this.drawOverlay(chunk);
      }
    }
    for (const key of this.dirtyChunks) {
      const chunk = this.chunks.get(key);
      if (chunk !== undefined) {
        this.drawChunk(chunk);
      }
    }
    this.dirtyChunks.clear();
    if (this.districtsDirty) {
      this.districtsDirty = false;
      this.drawDistricts();
    }
    if (this.flowsDirty) {
      this.flowsDirty = false;
      this.buildFlows();
      this.refreshRunning();
    }

    // Which layer the camera looks at
    const center = this.camera.screenToTile(this.app.screen.width / 2, this.app.screen.height / 2);
    if (isDeep(center.y) !== this.deep) {
      this.deep = isDeep(center.y);
      this.callbacks.onLayer(this.deep);
    }

    const tile = this.camera.screenToTile(this.pointerX, this.pointerY);
    const tool = this.callbacks.getState().tool;
    // A blueprint tool carries its whole layout: its id is enough
    const toolKey =
      (tool.kind === "blueprint" ? `blueprint:${tool.blueprint.id}` : JSON.stringify(tool)) +
      (this.callbacks.getState().plannedRoute?.length ?? 0);
    const changed =
      this.hovered === null ||
      tile.x !== this.hovered.x ||
      tile.y !== this.hovered.y ||
      toolKey !== this.lastToolKey;
    if (changed || !this.pointerInside) {
      this.hovered = this.pointerInside ? tile : null;
      this.lastToolKey = toolKey;
      if (this.dragging && this.isToolDrag()) {
        if (tool.kind === "pipe") {
          this.updatePipeRoute();
        } else if (tool.kind === "bulldoze") {
          this.addBulldozeTile();
        }
      }
      this.drawGhost();
    }
    this.drawHighlights();
    this.animate(deltaSeconds);
    this.scaleLabels();
  }

  private panWithKeys(deltaSeconds: number) {
    const speed = (900 / this.camera.scale) * deltaSeconds;
    let dx = 0;
    let dy = 0;
    if (this.keys.has("arrowleft") || this.keys.has("a")) dx += speed;
    if (this.keys.has("arrowright") || this.keys.has("d")) dx -= speed;
    if (this.keys.has("arrowup") || this.keys.has("w")) dy += speed;
    if (this.keys.has("arrowdown") || this.keys.has("s")) dy -= speed;
    if (dx !== 0 || dy !== 0) {
      this.camera.offsetX += dx;
      this.camera.offsetY += dy;
    }
  }

  // ---------------------------------------------------------------- Chunks

  private streamChunks() {
    const { width, height } = this.app.screen;
    const range = this.camera.visibleChunks(width, height, CHUNK_MARGIN);
    const view = this.camera.viewRect(width, height);
    const wanted = new Set<string>();
    const request: number[] = [];
    const districts = new Set<string>();
    const canRequest = this.callbacks.canRequest();

    for (let cy = range.minY; cy <= range.maxY; cy++) {
      for (let cx = range.minX; cx <= range.maxX; cx++) {
        if (!chunkIntersectsView(cx, cy, view)) {
          continue;
        }
        const key = chunkKey(cx, cy);
        wanted.add(key);
        let chunk = this.chunks.get(key);
        if (chunk === undefined) {
          chunk = new ChunkView(cx, cy);
          this.chunks.set(key, chunk);
        }
        if (!chunk.requested && canRequest) {
          chunk.requested = true;
          request.push(cx, cy);
        }
        // The districts of the chunk's corners
        for (const [tx, ty] of [
          [cx * CHUNK_SIZE, cy * CHUNK_SIZE],
          [cx * CHUNK_SIZE + CHUNK_SIZE - 1, cy * CHUNK_SIZE + CHUNK_SIZE - 1],
        ] as const) {
          const d = districtOf(tx, ty);
          // Down in the Deep Level there are no districts, only the player's own caverns
          if (!isDeep(ty)) {
            districts.add(`${d.x},${d.y}`);
          }
        }
      }
    }
    for (let i = 0; i < request.length; i += 32) {
      this.callbacks.requestChunks(request.slice(i, i + 32));
    }

    const unwatch: number[] = [];
    for (const [key, chunk] of this.chunks) {
      if (!wanted.has(key)) {
        this.clearChunk(chunk);
        this.chunks.delete(key);
        this.world.dropChunk(chunk.cx, chunk.cy);
        if (chunk.requested) {
          unwatch.push(chunk.cx, chunk.cy);
        }
      }
    }
    if (unwatch.length > 0) {
      this.callbacks.unwatchChunks(unwatch);
    }

    for (const key of districts) {
      if (canRequest && !this.districtViews.has(key)) {
        const [x, y] = key.split(",").map(Number) as [number, number];
        this.districtViews.set(key, { graphics: new Graphics(), labels: new Container(), hub: [] });
        this.callbacks.requestDistrict(x, y);
      }
    }
  }

  /** Requests everything on screen again (e.g. after a reconnect) */
  reloadChunks() {
    for (const chunk of this.chunks.values()) {
      chunk.requested = false;
    }
    for (const view of this.districtViews.values()) {
      view.graphics.destroy();
      view.labels.destroy({ children: true });
      for (const hub of view.hub) {
        this.effects.delete(hub);
        hub.display.destroy({ children: true });
      }
    }
    this.districtViews.clear();
  }

  private clearChunk(chunk: ChunkView) {
    chunk.ground?.destroy({ children: true });
    chunk.ground = null;
    chunk.overlay?.destroy();
    chunk.overlay = null;
    if (chunk.mini !== null) {
      chunk.mini.texture.destroy(true);
      chunk.mini.destroy();
      chunk.mini = null;
    }
    this.clearObjects(chunk);
  }

  private clearObjects(chunk: ChunkView) {
    for (const view of chunk.objects) {
      this.effects.delete(view);
      view.effects?.destroy();
      view.display.destroy({ children: true });
    }
    chunk.objects = [];
  }

  private drawChunk(chunk: ChunkView) {
    const terrain = this.world.chunks.get(chunkKey(chunk.cx, chunk.cy));
    if (terrain === undefined) {
      return;
    }
    const firstTime = !chunk.loaded;
    chunk.loaded = true;
    this.clearChunk(chunk);
    this.drawGround(chunk);
    this.drawMini(chunk);
    this.drawOverlay(chunk);
    this.drawObjects(chunk);
    if (firstTime) {
      // Pipes on the edge of the neighbours connect to this chunk's objects
      for (const [dx, dy] of [
        [1, 0],
        [-1, 0],
        [0, 1],
        [0, -1],
      ] as const) {
        const neighbour = this.chunks.get(chunkKey(chunk.cx + dx, chunk.cy + dy));
        if (neighbour?.loaded) {
          this.clearObjects(neighbour);
          this.drawObjects(neighbour);
        }
      }
    }
  }

  private drawGround(chunk: ChunkView) {
    const ground = new Container();
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;
    for (let y = y0; y < y0 + CHUNK_SIZE; y++) {
      for (let x = x0; x < x0 + CHUNK_SIZE; x++) {
        const tile = this.world.tile(x, y);
        if (tile === null) {
          continue;
        }
        const variant = Math.floor(hash01((x * 73856093) ^ (y * 19349663)) * TERRAIN_VARIANTS);
        const sprite = new Sprite(this.textures.terrainAt(tile.terrain, x, y, variant));
        const anchor = diagonalToScreen(x, y);
        sprite.anchor.set(0, 1);
        sprite.position.set(anchor.x, anchor.y);
        ground.addChild(sprite);
        // Soft edges where a stronger terrain borders this one (grass over sand, sand over water)
        const own = BLEND_PRIORITY[tile.terrain];
        if (own !== undefined) {
          NEIGHBOURS.forEach(([dx, dy], edge) => {
            const other = this.world.terrain(x + dx, y + dy);
            const priority = other === null ? undefined : BLEND_PRIORITY[other];
            if (other === null || priority === undefined || priority <= own) {
              return;
            }
            const texture = this.textures.terrainEdge(other, x, y, edge);
            if (texture !== null) {
              const blend = new Sprite(texture);
              blend.anchor.set(0, 1);
              blend.position.set(anchor.x, anchor.y);
              ground.addChild(blend);
            }
          });
        }
        if (
          tile.deposit !== null &&
          tile.richness > 0 &&
          (tile.terrain === "rock" || tile.deposit === "magma")
        ) {
          // Down in the Deep Level: veins in the rock, glowing magma once dug out
          const texture =
            tile.terrain === "rock"
              ? this.textures.vein(
                  this.callbacks.getState().content.resourceColor(tile.deposit),
                  tile.richness,
                  variant,
                )
              : this.textures.magma(tile.richness, variant);
          const deposit = new Sprite(texture);
          deposit.anchor.set(0, 1);
          deposit.position.set(anchor.x, anchor.y);
          ground.addChild(deposit);
        } else if (
          tile.deposit !== null &&
          tile.richness > 0 &&
          this.textures.prop(depositArt(tile.deposit)) !== null
        ) {
          // Generated art: the deposit lies on the tile, bigger when richer
          const art = this.textures.prop(depositArt(tile.deposit))!;
          const deposit = new Sprite(art.texture);
          deposit.anchor.set(art.anchorX, art.anchorY);
          deposit.scale.set(art.scale * (0.62 + tile.richness * 0.09));
          deposit.position.set(anchor.x + TILE_WIDTH, anchor.y - TILE_HEIGHT / 2);
          ground.addChild(deposit);
        } else if (tile.deposit !== null && tile.richness > 0) {
          const deposit = new Sprite(
            this.textures.deposit(
              this.callbacks.getState().content.resourceColor(tile.deposit),
              tile.richness,
              variant,
            ),
          );
          deposit.anchor.set(0, 1);
          deposit.position.set(anchor.x, anchor.y);
          ground.addChild(deposit);
        }
      }
    }
    ground.cullable = true;
    chunk.ground = ground;
    this.groundLayer.addChild(ground);
  }

  /** Colours over the terrain: who owns what, wind, sun or deposits */
  private drawOverlay(chunk: ChunkView) {
    chunk.overlay?.destroy();
    chunk.overlay = null;
    if (this.overlay === "none" || !chunk.loaded) {
      return;
    }
    const g = new Graphics();
    const content = this.callbacks.getState().content;
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;
    const ownerColors = new Map<number, string>();
    for (const district of this.world.districts.values()) {
      for (const estate of district.estates) {
        if (estate.ownerId !== null && estate.ownerColor !== null) {
          ownerColors.set(estate.ownerId, estate.ownerColor);
        }
      }
    }
    for (let y = y0; y < y0 + CHUNK_SIZE; y++) {
      for (let x = x0; x < x0 + CHUNK_SIZE; x++) {
        const tile = this.world.tile(x, y);
        if (tile === null) {
          continue;
        }
        let color: string | number | null = null;
        let alpha = 0.5;
        switch (this.overlay) {
          case "wind":
          case "sun": {
            if (tile.terrain === "water" || tile.terrain === "road" || tile.terrain === "city") {
              break;
            }
            // Most estates are between 0.8 and 1.4: that range gets the whole scale
            const value = this.overlay === "wind" ? tile.wind : tile.sun;
            const t = Math.max(0, Math.min(1, (value - 0.8) / 0.6));
            color = this.overlay === "wind" ? heat(t) : sunHeat(t);
            alpha = 0.68;
            break;
          }
          case "deposits":
            if (tile.deposit !== null) {
              color = content.resourceColor(tile.deposit);
              alpha = 0.45 + tile.richness * 0.1;
            } else {
              color = 0x0b1d24;
              alpha = 0.6;
            }
            break;
          case "owners": {
            const building = this.world.buildingAt(x, y);
            const owner = building?.owner ?? this.world.conductorAt(x, y)?.owner;
            if (owner !== undefined) {
              color = ownerColors.get(owner) ?? "#ffffff";
              alpha = 0.7;
            }
            break;
          }
        }
        if (color !== null) {
          traceFootprint(g, x, y, 1);
          g.fill({ color, alpha });
        }
      }
    }
    chunk.overlay = g;
    this.overlayLayer.addChild(g);
  }

  /** The far view of a chunk: one small picture of its terrain and buildings */
  private drawMini(chunk: ChunkView) {
    const scale = 6;
    const canvas = createCanvas(CHUNK_SIZE * 2 * scale, CHUNK_SIZE * scale);
    const g = canvas.getContext("2d")!;
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;
    const offset = CHUNK_SIZE * scale;
    for (let ly = 0; ly < CHUNK_SIZE; ly++) {
      for (let lx = 0; lx < CHUNK_SIZE; lx++) {
        const x = x0 + lx;
        const y = y0 + ly;
        const terrain: TerrainType = this.world.terrain(x, y) ?? "grass";
        const occupied = this.world.isOccupied(x, y);
        const px = offset + (lx - ly) * scale;
        const py = (lx + ly) * (scale / 2);
        g.fillStyle = occupied ? "#2b2b30" : TERRAIN_MAP_COLORS[terrain];
        g.beginPath();
        g.moveTo(px, py);
        g.lineTo(px + scale, py + scale / 2);
        g.lineTo(px, py + scale);
        g.lineTo(px - scale, py + scale / 2);
        g.closePath();
        g.fill();
      }
    }
    const sprite = new Sprite(Texture.from(canvas));
    // The canvas' top corner is tile (x0, y0)'s top corner
    const top = diagonalToScreen(x0, y0);
    sprite.position.set(top.x + TILE_WIDTH - offset * (TILE_WIDTH / scale), top.y - TILE_HEIGHT);
    sprite.scale.set(TILE_WIDTH / scale);
    chunk.mini = sprite;
    this.miniLayer.addChild(sprite);
  }

  /** What grows or lies on a free tile: trees, peaks, lanterns, and now and then a bush, flowers or reeds */
  private terrainProp(terrain: TerrainType, x: number, y: number): Sprite | null {
    const anchor = diagonalToScreen(x, y);
    const variant = Math.floor(hash01((x * 92821) ^ (y * 68917)) * 24);
    const depth = (x + y) * 4 + 1;
    // A tile sprite: anchored at the bottom-left corner of the tile's box
    const tileSprite = (texture: Texture) => {
      const sprite = new Sprite(texture);
      sprite.anchor.set(0, 1);
      sprite.position.set(anchor.x, anchor.y);
      sprite.zIndex = depth;
      return sprite;
    };
    // A generated prop: its foot somewhere on the tile
    const propSprite = (id: string, u = 0.5, v = 0.5, size = 1) => {
      const art = this.textures.prop(id);
      if (art === null) {
        return null;
      }
      const sprite = new Sprite(art.texture);
      sprite.anchor.set(art.anchorX, art.anchorY);
      sprite.scale.set(art.scale * size);
      // (u, v) inside the tile's diamond
      sprite.position.set(
        anchor.x + TILE_WIDTH * (1 + u - v),
        anchor.y - TILE_HEIGHT + (TILE_HEIGHT / 2) * (u + v),
      );
      sprite.zIndex = depth;
      return sprite;
    };
    const roll = hash01((x * 7121) ^ (y * 4271));
    switch (terrain) {
      case "cavern":
        return roll < 0.03 ? tileSprite(this.textures.lantern()) : null;
      case "forest":
        return tileSprite(this.textures.forestTile(variant) ?? this.textures.forest(variant));
      case "mountain":
        return (
          propSprite(variant % 2 === 0 ? "peak1" : "peak2", 0.5, 0.55) ??
          tileSprite(this.textures.rocks(variant))
        );
      case "grass":
      case "hills": {
        const wild = zoneAt(x, y).kind === "wilderness";
        if (wild && roll < 0.05) {
          return propSprite(
            roll < 0.025 ? "bush" : "boulder",
            0.3 + hash01(x + y * 3) * 0.4,
            0.3 + hash01(x * 3 + y) * 0.4,
          );
        }
        return roll > 0.94 ? propSprite("flowers", 0.5, 0.5, 0.8) : null;
      }
      case "beach": {
        const shore = [
          [1, 0],
          [-1, 0],
          [0, 1],
          [0, -1],
        ].some(([dx, dy]) => this.world.terrain(x + dx!, y + dy!) === "water");
        return shore && roll < 0.25
          ? propSprite("reeds", 0.4 + hash01(x * 5) * 0.2, 0.4 + hash01(y * 5) * 0.2)
          : null;
      }
      default:
        return null;
    }
  }

  private drawObjects(chunk: ChunkView) {
    const state = this.callbacks.getState();
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;

    // Trees and rocks of the terrain
    for (let y = y0; y < y0 + CHUNK_SIZE; y++) {
      for (let x = x0; x < x0 + CHUNK_SIZE; x++) {
        const terrain = this.world.terrain(x, y);
        if (terrain === null || this.world.isOccupied(x, y)) {
          continue;
        }
        const sprite = this.terrainProp(terrain, x, y);
        if (sprite === null) {
          continue;
        }
        this.objectLayer.addChild(sprite);
        chunk.objects.push({
          display: sprite,
          effects: null,
          building: null,
          machine: null,
          idle: null,
          progress: null,
        });
      }
    }

    // Pipes and gates
    for (const key of this.world.conductorKeysInChunk(chunk.cx, chunk.cy)) {
      const conductor = this.world.conductors.get(key);
      if (conductor !== undefined) {
        chunk.objects.push(this.conductorView(conductor));
      }
    }

    // Outpost claims (drawn by the chunk with their corner)
    for (const outpost of this.world.outposts.values()) {
      const corner = chunkOf(outpost.x, outpost.y);
      if (corner.x === chunk.cx && corner.y === chunk.cy) {
        chunk.objects.push({
          display: this.outpostView(outpost),
          effects: null,
          building: null,
          machine: null,
          idle: null,
          progress: null,
        });
      }
    }

    // Buildings
    for (const id of this.world.buildingIdsInChunk(chunk.cx, chunk.cy)) {
      const building = this.world.buildings.get(id);
      if (building !== undefined) {
        chunk.objects.push(this.buildingView(building, state));
        const cables = this.cables(building, state) ?? this.teleportArc(building);
        if (cables !== null) {
          chunk.objects.push({
            display: cables,
            effects: null,
            building: null,
            machine: null,
            idle: null,
            progress: null,
          });
        }
      }
    }
  }

  /** The border of an outpost claim in its owner's colour, with a flag and the owner's name */
  private outpostView(outpost: OutpostDto): Container {
    const container = new Container();
    const color = parseInt(outpost.ownerColor.replace("#", ""), 16);
    const g = new Graphics();
    traceFootprint(g, outpost.x, outpost.y, outpost.size);
    g.fill({ color, alpha: 0.1 });
    traceFootprint(g, outpost.x, outpost.y, outpost.size);
    g.stroke({ color, width: 6, alpha: 0.9 });
    container.addChild(g);
    const label = new Text({
      text: `⛺ ${outpost.ownerName}`,
      style: {
        fontFamily: "system-ui",
        fontSize: 30,
        fontWeight: "800",
        fill: "#ffffff",
        stroke: { color: "#0b1d24", width: 6 },
      },
    });
    // At the front corner, where buildings don't hide it
    const front = tileCenter(outpost.x + outpost.size - 1, outpost.y + outpost.size - 1);
    label.anchor.set(0.5, 0);
    label.position.set(front.x, front.y + 30);
    container.addChild(label);
    container.zIndex = (outpost.x + outpost.y) * 4 - 6;
    this.objectLayer.addChild(container);
    return container;
  }

  /** A teleporter's link: a dotted arc of capsules (or light) from the sending station to the receiving one */
  private teleportArc(sender: BuildingDto): Graphics | null {
    const receiver = sender.link != null ? this.world.buildings.get(sender.link) : undefined;
    if (receiver === undefined) {
      return null;
    }
    const from = tileCenter(sender.x + (sender.size - 1) / 2, sender.y + (sender.size - 1) / 2);
    const to = tileCenter(receiver.x + (receiver.size - 1) / 2, receiver.y + (receiver.size - 1) / 2);
    const lift = Math.min(900, Math.hypot(to.x - from.x, to.y - from.y) * 0.35) + 120;
    const cx = (from.x + to.x) / 2;
    const cy = Math.min(from.y, to.y) - lift;
    const quantum = sender.type === "quantumLink";
    const g = new Graphics();
    const steps = 28;
    for (let i = 1; i < steps; i++) {
      const t = i / steps;
      const x = (1 - t) * (1 - t) * from.x + 2 * (1 - t) * t * cx + t * t * to.x;
      const y = (1 - t) * (1 - t) * (from.y - 60) + 2 * (1 - t) * t * cy + t * t * (to.y - 60);
      g.circle(x, y, quantum ? 7 : 6);
    }
    g.fill({ color: quantum ? 0x9ad8ff : 0xd8a24a, alpha: 0.75 });
    g.zIndex = (Math.max(sender.x + sender.y, receiver.x + receiver.y) + 2) * 4 + 3;
    this.objectLayer.addChild(g);
    return g;
  }

  /** Power line cables from a pylon to its owner's pylons in reach (each cable once, from the pylon with the lower id) */
  private cables(pylon: BuildingDto, state: EngineState): Graphics | null {
    const reach = state.content.building(pylon.type)?.reach ?? 0;
    if (reach <= 0 || pylon.constructionEndsAt !== null) {
      return null;
    }
    const g = new Graphics();
    const from = tileCenter(pylon.x, pylon.y);
    let z = (pylon.x + pylon.y) * 4 + 3;
    let any = false;
    for (const other of this.world.buildings.values()) {
      if (
        other.id <= pylon.id ||
        other.type !== pylon.type ||
        other.owner !== pylon.owner ||
        other.constructionEndsAt !== null ||
        (other.x - pylon.x) ** 2 + (other.y - pylon.y) ** 2 > reach * reach
      ) {
        continue;
      }
      const to = tileCenter(other.x, other.y);
      for (const offset of [-14, 14]) {
        const ax = from.x + offset;
        const ay = from.y - 170;
        const bx = to.x + offset;
        const by = to.y - 170;
        // A cable sags in the middle
        g.moveTo(ax, ay).quadraticCurveTo((ax + bx) / 2, (ay + by) / 2 + 50, bx, by);
      }
      z = Math.max(z, (other.x + other.y) * 4 + 3);
      any = true;
    }
    if (!any) {
      return null;
    }
    g.stroke({ color: 0x2a2d33, width: 3, alpha: 0.85 });
    g.zIndex = z + 1;
    this.objectLayer.addChild(g);
    return g;
  }

  private pipeShape(x: number, y: number): string {
    return pipeShapeOf(
      x,
      y,
      (tx, ty) => this.world.conductorAt(tx, ty) !== undefined || this.world.buildingAt(tx, ty) !== undefined,
    );
  }

  private conductorView(conductor: ConductorDto): ObjectView {
    const anchor = diagonalToScreen(conductor.x, conductor.y);
    if (conductor.kind === "gate") {
      const estate = this.world.estateAt(conductor.x, conductor.y);
      const policy = estate?.gatePolicies[conductor.side ?? "top"] ?? "open";
      const art = this.textures.gate(policy, conductor.side === "top" || conductor.side === "bottom");
      const sprite = new Sprite(art.frames[0]);
      sprite.anchor.set(0, 1);
      const container = new Container();
      container.position.set(anchor.x, anchor.y);
      container.addChild(sprite);
      const effects = new BuildingEffects(art.attachments, "#ffffff");
      container.addChild(effects.container);
      container.zIndex = (conductor.x + conductor.y) * 4 + 2;
      this.objectLayer.addChild(container);
      const view: ObjectView = {
        display: container,
        effects,
        building: null,
        machine: null,
        idle: null,
        progress: null,
      };
      this.effects.add(view);
      return view;
    }
    const texture = this.textures.pipe(this.pipeShape(conductor.x, conductor.y)) ?? Texture.WHITE;
    const sprite = new Sprite(texture);
    sprite.anchor.set(0, 1);
    sprite.position.set(anchor.x, anchor.y);
    sprite.tint = PIPE_TINTS[conductor.tier] ?? 0xffffff;
    sprite.zIndex = (conductor.x + conductor.y) * 4 + 2;
    this.objectLayer.addChild(sprite);
    return { display: sprite, effects: null, building: null, machine: null, idle: null, progress: null };
  }

  private buildingView(building: BuildingDto, state: EngineState): ObjectView {
    const definition = state.content.building(building.type);
    const size = building.size;
    const anchor = footprintAnchor(building.x, building.y, size);
    const container = new Container();
    container.position.set(anchor.x, anchor.y);
    container.zIndex = (building.x + building.y + 2 * (size - 1)) * 4 + 3;
    const owner = this.world.estateAt(building.x, building.y);
    const view: ObjectView = {
      display: container,
      effects: null,
      building,
      machine: null,
      idle: null,
      progress: null,
    };

    if (building.constructionEndsAt !== null) {
      this.drawConstruction(view, size);
    } else {
      const art = this.textures.building(building.type, definition?.size ?? size);
      // A complex: its members side by side on one footprint (back to front), with a plaque
      const cells = definition !== undefined && definition.size === 1 && size > 1 ? size : 1;
      if (cells > 1) {
        const pad = new Graphics();
        pad
          .moveTo(0, (-TILE_HEIGHT / 2) * size)
          .lineTo(TILE_WIDTH * size, -TILE_HEIGHT * size)
          .lineTo(TILE_WIDTH * 2 * size, (-TILE_HEIGHT / 2) * size)
          .lineTo(TILE_WIDTH * size, 0)
          .closePath();
        pad.fill({ color: 0xc9a227, alpha: 0.35 });
        pad.stroke({ color: 0xf6c744, width: 6 });
        container.addChild(pad);
      }
      for (let depth = 0; depth <= 2 * (cells - 1); depth++) {
        for (let i = 0; i < cells; i++) {
          const j = depth - i;
          if (j < 0 || j >= cells) {
            continue;
          }
          // A complex's members are 1×1; any other building is one sprite over its whole footprint
          const cell =
            cells > 1
              ? footprintAnchor(building.x + i, building.y + j, 1)
              : footprintAnchor(building.x, building.y, size);
          const sprite = art.frames.length > 1 ? new AnimatedSprite(art.frames) : new Sprite(art.frames[0]);
          if (art.placement !== undefined) {
            // Generated art: anchored at the bottom corner of its plate
            const cellSize = cells > 1 ? 1 : size;
            sprite.anchor.set(art.placement.anchorX, art.placement.anchorY);
            sprite.scale.set(art.placement.scale);
            sprite.position.set(cell.x - anchor.x + TILE_WIDTH * cellSize, cell.y - anchor.y);
          } else {
            sprite.anchor.set(0, 1);
            sprite.position.set(cell.x - anchor.x, cell.y - anchor.y);
          }
          container.addChild(sprite);
          if (sprite instanceof AnimatedSprite) {
            sprite.animationSpeed = 0.2;
            sprite.play();
            if (view.machine === null) {
              view.machine = sprite;
            } else {
              (view.machines ??= []).push(sprite);
            }
          }
        }
      }
      if (cells > 1) {
        const plaque = new Text({
          text: `COMPLEX ×${cells === 2 ? 5 : 12}`,
          style: {
            fontFamily: "system-ui",
            fontSize: 26,
            fontWeight: "900",
            fill: "#f6c744",
            stroke: { color: "#17181c", width: 6 },
          },
        });
        plaque.anchor.set(0.5, 0);
        plaque.position.set(TILE_WIDTH * size, -TILE_HEIGHT / 2);
        container.addChild(plaque);
      }
      if (art.attachments.length > 0) {
        view.effects = new BuildingEffects(art.attachments, owner?.ownerColor ?? "#e74c3c");
        container.addChild(view.effects.container);
        this.effects.add(view);
      }
      if (
        definition !== undefined &&
        (Object.keys(definition.in).length > 0 || Object.keys(definition.out).length > 0)
      ) {
        // A small speech bubble: this one waits for a pipe (or for what it needs)
        const badge = new Container();
        const bubble = new Graphics();
        bubble
          .roundRect(-30, -26, 60, 44, 16)
          .fill({ color: 0xfbf3df })
          .stroke({ color: 0xc9963a, width: 4 });
        bubble.poly([-8, 16, 8, 16, 0, 30]).fill({ color: 0xfbf3df }).stroke({ color: 0xc9963a, width: 4 });
        bubble.rect(-9, 12, 18, 6).fill({ color: 0xfbf3df });
        const icon = new Text({
          text: "zz",
          style: {
            fontFamily: "Nunito Variable, system-ui",
            fontSize: 26,
            fontWeight: "900",
            fill: "#7a6a4e",
          },
        });
        icon.anchor.set(0.5);
        icon.position.set(0, -4);
        badge.addChild(bubble, icon);
        badge.position.set(TILE_WIDTH * size, -TILE_HEIGHT * size - 30);
        badge.visible = false;
        container.addChild(badge);
        view.idle = badge;
        this.effects.add(view);
      }
      if (building.damaged) {
        container.addChild(damageBadge(size));
      }
      // Away for a while: a sign on the HQ (and ivy when a Caretaker looks after the estate)
      if (definition?.kind === "hq" && owner?.activity != null && owner.activity !== "active") {
        const sign = new Text({
          text: owner.activity === "resting" ? "zz resting" : "Caretaker",
          style: {
            fontFamily: "system-ui",
            fontSize: 26,
            fontWeight: "800",
            fill: owner.activity === "resting" ? "#bfe9ff" : "#b7e4a0",
            stroke: { color: "#0b1d24", width: 6 },
          },
        });
        sign.anchor.set(0.5, 1);
        sign.position.set(TILE_WIDTH * size, -TILE_HEIGHT * size - 150);
        container.addChild(sign);
      }
      if (owner?.activity === "dormant") {
        for (const child of container.children) {
          if (child instanceof Sprite) {
            child.tint = 0xb8d8a0;
          }
        }
      }
    }
    this.objectLayer.addChild(container);
    this.applyRunning(view);
    return view;
  }

  private drawConstruction(view: ObjectView, size: number) {
    const container = view.display;
    if (size === 1) {
      const frames = this.textures.underConstruction();
      if (frames.length > 0) {
        const animated = new AnimatedSprite(frames);
        animated.anchor.set(0, 1);
        animated.animationSpeed = 0.25;
        animated.play();
        container.addChild(animated);
      }
    } else {
      // Scaffolding over the whole footprint
      const g = new Graphics();
      const w = TILE_WIDTH * size;
      for (let i = 0; i <= size * 2; i++) {
        const x = (w * 2 * i) / (size * 2);
        g.moveTo(x, (-TILE_HEIGHT / 2) * size).lineTo(x, (-TILE_HEIGHT / 2) * size - 110);
      }
      g.stroke({ color: 0x8a5a32, width: 6 });
      g.moveTo(0, (-TILE_HEIGHT / 2) * size - 40).lineTo(w * 2, (-TILE_HEIGHT / 2) * size - 40);
      g.moveTo(0, (-TILE_HEIGHT / 2) * size - 90).lineTo(w * 2, (-TILE_HEIGHT / 2) * size - 90);
      g.stroke({ color: 0xc9a227, width: 5 });
      const pad = new Graphics();
      pad
        .moveTo(0, (-TILE_HEIGHT / 2) * size)
        .lineTo(w, -TILE_HEIGHT * size)
        .lineTo(w * 2, (-TILE_HEIGHT / 2) * size)
        .lineTo(w, 0)
        .closePath();
      pad.fill({ color: 0x8a7a5a, alpha: 0.9 });
      pad.stroke({ color: 0x17181c, width: 3 });
      container.addChild(pad, g);
    }
    const progress = new Graphics();
    progress.position.set(TILE_WIDTH * size - 60, -TILE_HEIGHT * size - 40);
    container.addChild(progress);
    view.progress = progress;
    this.effects.add(view);
  }

  private applyRunning(view: ObjectView) {
    const building = view.building;
    if (building === null || building.constructionEndsAt !== null) {
      return;
    }
    const rate = this.world.rate(building);
    const running = rate !== null && rate > 0.005;
    view.effects?.setRunning(running);
    for (const machine of [view.machine, ...(view.machines ?? [])]) {
      if (machine === null) {
        continue;
      }
      if (running && !machine.playing) {
        machine.play();
      } else if (!running && machine.playing) {
        machine.gotoAndStop(0);
      }
    }
    if (view.idle !== null) {
      view.idle.visible = !running;
    }
  }

  private refreshRunning() {
    for (const chunk of this.chunks.values()) {
      for (const view of chunk.objects) {
        this.applyRunning(view);
      }
    }
  }

  private animate(deltaSeconds: number) {
    if (this.far) {
      return;
    }
    const now = Date.now() + this.callbacks.getState().serverOffsetMs;
    const content = this.callbacks.getState().content;
    for (const view of this.effects) {
      view.effects?.update(deltaSeconds, this.time);
      if (view.idle !== null && view.idle.visible) {
        view.idle.alpha = 0.6 + Math.sin(this.time * 2) * 0.3;
        view.idle.y = -TILE_HEIGHT * (view.building?.size ?? 1) - 40 - Math.sin(this.time * 1.5) * 6;
      }
      if (view.progress !== null && view.building?.constructionEndsAt) {
        const total = (content.building(view.building.type)?.buildSeconds ?? 60) * 1000;
        const left = new Date(view.building.constructionEndsAt).getTime() - now;
        const done = Math.max(0, Math.min(1, 1 - left / total));
        view.progress.clear();
        view.progress.roundRect(0, 0, 120, 16, 8).fill({ color: 0x17181c, alpha: 0.85 });
        view.progress.roundRect(3, 3, 114 * done, 10, 5).fill({ color: 0x2ec4a6 });
      }
    }
    for (const dot of this.flowDots) {
      if (dot.length <= 0) {
        continue;
      }
      let distance = (dot.offset + this.time * dot.speed) % dot.length;
      let i = 0;
      while (i < dot.points.length - 2) {
        const a = dot.points[i]!;
        const b = dot.points[i + 1]!;
        const segment = Math.hypot(b.x - a.x, b.y - a.y);
        if (distance <= segment) {
          break;
        }
        distance -= segment;
        i++;
      }
      const a = dot.points[i]!;
      const b = dot.points[i + 1] ?? a;
      const segment = Math.hypot(b.x - a.x, b.y - a.y) || 1;
      const t = Math.min(1, distance / segment);
      dot.sprite.position.set(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }
  }

  // ---------------------------------------------------------------- Flows

  private buildFlows() {
    for (const dot of this.flowDots) {
      dot.sprite.destroy();
    }
    this.flowDots = [];
    const content = this.callbacks.getState().content;
    for (const network of this.world.flows.values()) {
      for (const flow of network.flows) {
        if (flow.path.length < 2 || flow.delivered <= 0) {
          continue;
        }
        // The path in pieces: a Cargo Depot route jumps far (trucks, no dots on the way)
        const pieces: Array<Array<{ x: number; y: number }>> = [[]];
        const from = this.world.buildings.get(flow.from);
        const to = this.world.buildings.get(flow.to);
        if (from !== undefined) {
          pieces[0]!.push(this.buildingCenter(from));
        }
        for (let i = 0; i < flow.path.length; i += 2) {
          const x = flow.path[i]!;
          const y = flow.path[i + 1]!;
          if (i >= 2 && Math.max(Math.abs(x - flow.path[i - 2]!), Math.abs(y - flow.path[i - 1]!)) > 9) {
            pieces.push([]);
          }
          const center = tileCenter(x, y);
          pieces[pieces.length - 1]!.push({ x: center.x, y: center.y - 22 });
        }
        if (to !== undefined) {
          pieces[pieces.length - 1]!.push(this.buildingCenter(to));
        }
        const color = content.resourceColor(flow.resource);
        for (const points of pieces) {
          let length = 0;
          for (let i = 1; i < points.length; i++) {
            length += Math.hypot(points[i]!.x - points[i - 1]!.x, points[i]!.y - points[i - 1]!.y);
          }
          if (length <= 0) {
            continue;
          }
          // More dots for bigger flows, spaced along the path
          const spacing = Math.max(70, 260 / Math.log2(2 + flow.delivered));
          const count = Math.max(1, Math.min(40, Math.floor(length / spacing)));
          for (let i = 0; i < count; i++) {
            const sprite = new Sprite(dotTexture());
            sprite.anchor.set(0.5);
            sprite.scale.set(flow.kind === "sale" || flow.kind === "trade" ? 0.75 : 0.6);
            sprite.tint = color;
            this.flowLayer.addChild(sprite);
            this.flowDots.push({ sprite, points, length, offset: (length / count) * i, speed: 190 });
          }
        }
      }
    }
  }

  private buildingCenter(building: BuildingDto): { x: number; y: number } {
    const center = tileCenter(building.x + (building.size - 1) / 2, building.y + (building.size - 1) / 2);
    return { x: center.x, y: center.y - 22 };
  }

  // ---------------------------------------------------------------- Estates and districts

  private drawDistricts() {
    const state = this.callbacks.getState();
    this.drawDeepLevel(state.company);
    for (const [key, view] of this.districtViews) {
      const [dx, dy] = key.split(",").map(Number) as [number, number];
      const district = this.world.district(dx, dy);
      view.graphics.clear();
      view.labels.removeChildren().forEach((child) => child.destroy());
      for (const hub of view.hub) {
        this.effects.delete(hub);
        hub.effects?.destroy();
        hub.display.destroy({ children: true });
      }
      view.hub = [];
      if (district === undefined) {
        continue;
      }
      if (view.graphics.parent === null) {
        this.estateLayer.addChild(view.graphics);
        this.labelLayer.addChild(view.labels);
      }
      this.drawDistrict(view, district, state.company);
    }
  }

  /** The player's Deep Level: the outline, the parcels (dug, diggable, being dug) */
  private drawDeepLevel(company: CompanyDto | null) {
    const g = this.deepGraphics;
    g.clear();
    this.deepLabels.removeChildren().forEach((child) => child.destroy());
    if (g.parent === null) {
      this.estateLayer.addChild(g);
      this.labelLayer.addChild(this.deepLabels);
    }
    if (company === null || !company.deep.open) {
      return;
    }
    const x0 = company.estateX;
    const y0 = company.estateY + DEEP_OFFSET;
    const amber = 0xf2b134;
    for (let parcel = 0; parcel < PARCELS_PER_SIDE * PARCELS_PER_SIDE; parcel++) {
      const px = x0 + (parcel % PARCELS_PER_SIDE) * PARCEL_SIZE;
      const py = y0 + Math.floor(parcel / PARCELS_PER_SIDE) * PARCEL_SIZE;
      traceFootprint(g, px, py, PARCEL_SIZE);
      if (company.deep.digParcel === parcel) {
        g.fill({ color: amber, alpha: 0.22 });
        traceFootprint(g, px, py, PARCEL_SIZE);
        const label = new Text({
          text: "⛏ Digging…",
          style: {
            fontFamily: "system-ui",
            fontSize: 30,
            fontWeight: "700",
            fill: "#ffd77a",
            stroke: { color: "#1a120c", width: 7 },
          },
        });
        label.anchor.set(0.5);
        const center = tileCenter(px + PARCEL_SIZE / 2 - 0.5, py + PARCEL_SIZE / 2 - 0.5);
        label.position.set(center.x, center.y);
        label.label = "estate";
        this.deepLabels.addChild(label);
      }
      g.stroke({ color: amber, width: 4, alpha: (company.deep.parcels & (1 << parcel)) !== 0 ? 0.3 : 0.55 });
    }
    traceFootprint(g, x0, y0, ESTATE_SIZE);
    g.stroke({ color: amber, width: 12, alpha: 0.85 });
    const title = new Text({
      text: `${company.name} · Deep Level`,
      style: {
        fontFamily: "system-ui",
        fontSize: 30,
        fontWeight: "700",
        fill: "#ffd77a",
        stroke: { color: "#1a120c", width: 7 },
      },
    });
    title.anchor.set(0.5, 1);
    const top = tileCenter(x0 + ESTATE_SIZE / 2, y0 + ESTATE_SIZE / 2);
    title.position.set(top.x, tileCenter(x0, y0).y - 40);
    title.label = "estate";
    this.deepLabels.addChild(title);
  }

  private drawDistrict(view: DistrictView, district: DistrictDto, company: CompanyDto | null) {
    const g = view.graphics;
    const labels = view.labels;
    const far = this.far;
    for (const estate of district.estates) {
      const mine = company !== null && estate.ownerId === company.id;
      const color = estate.ownerColor ?? "#ffffff";
      if (estate.ownerId !== null) {
        if (far) {
          traceFootprint(g, estate.x, estate.y, ESTATE_SIZE);
          g.fill({ color, alpha: 0.35 });
        }
        traceFootprint(g, estate.x, estate.y, ESTATE_SIZE);
        g.stroke({ color, width: far ? 60 : mine ? 7 : 5, alpha: far ? 0.95 : 0.75 });
      } else {
        traceFootprint(g, estate.x, estate.y, ESTATE_SIZE);
        g.stroke({ color: 0xffffff, width: far ? 30 : 3, alpha: 0.22 });
      }

      if (!far && mine) {
        // Your locked parcels: a light veil and a thin outline
        for (let parcel = 0; parcel < PARCELS_PER_SIDE * PARCELS_PER_SIDE; parcel++) {
          if (isParcelUnlocked(estate.unlockedParcels, parcel)) {
            continue;
          }
          const px = estate.x + (parcel % PARCELS_PER_SIDE) * PARCEL_SIZE;
          const py = estate.y + Math.floor(parcel / PARCELS_PER_SIDE) * PARCEL_SIZE;
          traceFootprint(g, px, py, PARCEL_SIZE);
          g.fill({ color: 0x1f2a1c, alpha: 0.2 });
          traceFootprint(g, px, py, PARCEL_SIZE);
          g.stroke({ color: 0xfff2c8, width: 2, alpha: 0.35 });
        }
      }

      // The name of the company at the top of its estate
      // Free estates are only named up close, far away the map would be full of labels
      if (estate.ownerName !== null || (!far && this.camera.scale < 0.3)) {
        const text = estate.ownerName ?? "Free estate";
        const label = new Text({
          text: estate.ownerName !== null ? `${text}  ·  Lv ${estate.ownerLevel}` : text,
          style: {
            fontFamily: "system-ui",
            fontSize: 30,
            fontWeight: "700",
            fill: estate.ownerName !== null ? "#ffffff" : "#d6e6e3",
            stroke: { color: "#0b1d24", width: 7 },
          },
        });
        label.anchor.set(0.5, 1);
        const top = tileCenter(estate.x + ESTATE_SIZE / 2, estate.y + ESTATE_SIZE / 2);
        const corner = tileCenter(estate.x, estate.y);
        label.position.set(top.x, far ? top.y : corner.y - 40);
        label.label = "estate";
        labels.addChild(label);
      }
    }
    if (far) {
      const name = new Text({
        text: district.name,
        style: {
          fontFamily: "Georgia, serif",
          fontSize: 44,
          fontWeight: "700",
          fill: "#f6c744",
          stroke: { color: "#0b1d24", width: 9 },
        },
      });
      name.anchor.set(0.5);
      const center = tileCenter(
        districtOriginX(district.x) + DISTRICT_SIZE / 2,
        districtOriginY(district.y) + DISTRICT_SIZE / 2,
      );
      name.position.set(center.x, center.y);
      name.label = "district";
      labels.addChild(name);
    } else {
      // The hub city
      const hubX = district.originX + 2 + 2 * 34;
      const hubY = district.originY + 2 + 2 * 34;
      for (const structure of district.hub) {
        const art = this.textures.hub(structure.type, structure.size, structure);
        const sprite = new Sprite(art.frames[0]);
        sprite.anchor.set(0, 1);
        const x = hubX + structure.x;
        const y = hubY + structure.y;
        const anchor = footprintAnchor(x, y, structure.size);
        const container = new Container();
        container.position.set(anchor.x, anchor.y);
        container.addChild(sprite);
        const hub: ObjectView = {
          display: container,
          effects: null,
          building: null,
          machine: null,
          idle: null,
          progress: null,
        };
        if (art.attachments.length > 0) {
          hub.effects = new BuildingEffects(art.attachments, "#2e86de");
          container.addChild(hub.effects.container);
          this.effects.add(hub);
        }
        // The corporation quarter is a flat plaza under the HQs that stand on it
        container.zIndex =
          structure.type === "corporationQuarter"
            ? (x + y) * 4 - 8
            : (x + y + 2 * (structure.size - 1)) * 4 + 3;
        this.objectLayer.addChild(container);
        view.hub.push(hub);
      }
    }
  }

  /** Labels keep a readable size on screen at every zoom */
  private scaleLabels() {
    const scale = this.camera.scale;
    for (const view of this.districtViews.values()) {
      for (const label of view.labels.children) {
        const factor =
          label.label === "district" ? Math.min(6, 0.9 / scale) : Math.min(5, Math.max(1, 0.55 / scale));
        label.scale.set(factor);
      }
    }
  }

  // ---------------------------------------------------------------- Ghost and highlights

  private drawGhost() {
    for (const child of this.ghostLayer.removeChildren()) {
      child.destroy({ children: true });
    }
    const state = this.callbacks.getState();
    const tool = state.tool;
    const tile = this.hovered;
    if (state.plannedRoute !== null && state.company !== null) {
      this.drawRoute(state.plannedRoute, 1, state.company);
    }
    if (tile === null || state.company === null || this.far) {
      this.callbacks.onHover(tile, null);
      return;
    }

    if (tool.kind === "build" || tool.kind === "move") {
      const building = tool.kind === "move" ? this.world.buildings.get(tool.building) : undefined;
      const type = tool.kind === "build" ? tool.type : building?.type;
      const definition = type === undefined ? undefined : state.content.building(type);
      if (definition === undefined) {
        return;
      }
      const origin = footprintOrigin(tile.x, tile.y, definition.size);
      const placement = canPlace(
        state.content,
        this.world,
        state.company,
        state.era,
        definition,
        origin.x,
        origin.y,
        building?.id ?? null,
        tool.kind === "move" || tool.crate !== undefined,
      );
      const g = new Graphics();
      for (let dy = 0; dy < definition.size; dy++) {
        for (let dx = 0; dx < definition.size; dx++) {
          traceFootprint(g, origin.x + dx, origin.y + dy, 1);
        }
      }
      g.fill({ color: placement.ok ? 0x2ec4a6 : 0xef6f6c, alpha: 0.45 });
      traceFootprint(g, origin.x, origin.y, definition.size);
      g.stroke({ color: placement.ok ? 0x2ec4a6 : 0xef6f6c, width: 6 });
      this.ghostLayer.addChild(g);
      const art = this.textures.building(definition.id, definition.size);
      const sprite = new Sprite(art.frames[0]);
      placeArt(sprite, art, footprintAnchor(origin.x, origin.y, definition.size), definition.size);
      sprite.alpha = 0.72;
      if (!placement.ok) {
        sprite.tint = 0xff9090;
      }
      this.ghostLayer.addChild(sprite);
      this.callbacks.onHover(tile, placement);
      return;
    }

    if (tool.kind === "pipe") {
      const g = new Graphics();
      const route = this.dragging && this.pipeRoute !== null ? this.pipeRoute : [tile];
      for (const t of route) {
        const own = this.world.conductorAt(t.x, t.y)?.owner === state.company.id;
        traceFootprint(g, t.x, t.y, 1);
        g.fill({ color: own ? 0x6cb4ee : 0x2ec4a6, alpha: 0.35 });
        if (!own) {
          const texture = this.textures.pipe("urdl");
          if (texture !== undefined) {
            const sprite = new Sprite(texture);
            sprite.anchor.set(0, 1);
            const anchor = diagonalToScreen(t.x, t.y);
            sprite.position.set(anchor.x, anchor.y);
            sprite.alpha = 0.55;
            sprite.tint = PIPE_TINTS[tool.tier] ?? 0xffffff;
            this.ghostLayer.addChild(sprite);
          }
        }
      }
      if (this.dragging && this.pipeRoute === null && this.pipeStart !== null) {
        traceFootprint(g, tile.x, tile.y, 1);
        g.fill({ color: 0xef6f6c, alpha: 0.45 });
      }
      this.ghostLayer.addChildAt(g, 0);
      this.callbacks.onHover(tile, null);
      return;
    }

    if (tool.kind === "blueprintArea") {
      const area = this.dragging ? this.areaRect() : { x: tile.x, y: tile.y, width: 1, height: 1 };
      if (area !== null) {
        const g = new Graphics();
        traceFootprint(g, area.x, area.y, area.width, area.height);
        g.fill({ color: 0x6cb4ee, alpha: 0.3 });
        traceFootprint(g, area.x, area.y, area.width, area.height);
        g.stroke({ color: 0x6cb4ee, width: 6 });
        this.ghostLayer.addChild(g);
        if (this.dragging) {
          const label = new Text({
            text: `${area.width} × ${area.height}`,
            style: { fill: 0xffffff, fontSize: 36, fontWeight: "700", stroke: { color: 0x1d3b5a, width: 6 } },
          });
          label.anchor.set(0.5);
          const center = tileCenter(area.x + (area.width - 1) / 2, area.y + (area.height - 1) / 2);
          label.position.set(center.x, center.y);
          this.ghostLayer.addChild(label);
        }
      }
      this.callbacks.onHover(tile, null);
      return;
    }

    if (tool.kind === "blueprint") {
      this.drawBlueprintGhost(tool.blueprint, tile, state.company, state);
      return;
    }

    if (tool.kind === "bulldoze") {
      const g = new Graphics();
      const tiles = this.dragging ? [...this.bulldozeTiles.values()] : [tile];
      for (const t of tiles) {
        const building = this.world.buildingAt(t.x, t.y);
        if (building !== undefined) {
          traceFootprint(g, building.x, building.y, building.size);
        } else {
          traceFootprint(g, t.x, t.y, 1);
        }
        g.fill({ color: 0xef6f6c, alpha: 0.4 });
      }
      this.ghostLayer.addChild(g);
    }
    this.callbacks.onHover(tile, null);
  }

  /** Every building of a blueprint where it would go: green when it can be built, red when not */
  private drawBlueprintGhost(
    blueprint: BlueprintDto,
    tile: TilePosition,
    company: CompanyDto,
    state: EngineState,
  ) {
    const origin = blueprintOrigin(tile.x, tile.y, blueprint);
    const g = new Graphics();
    traceFootprint(g, origin.x, origin.y, blueprint.width, blueprint.height);
    g.stroke({ color: 0x6cb4ee, width: 5, alpha: 0.9 });
    const sprites: Sprite[] = [];
    // The pipes as they will look: connected to each other and to the blueprint's buildings
    const taken = new Set(blueprint.pipes.map((t) => tileKey(t.dx, t.dy)));
    for (const item of blueprint.buildings) {
      const size = state.content.building(item.type)?.size ?? 1;
      for (let dy = 0; dy < size; dy++) {
        for (let dx = 0; dx < size; dx++) {
          taken.add(tileKey(item.dx + dx, item.dy + dy));
        }
      }
    }
    for (const pipe of blueprint.pipes) {
      const texture = this.textures.pipe(pipeShapeOf(pipe.dx, pipe.dy, (x, y) => taken.has(tileKey(x, y))));
      if (texture !== undefined) {
        const sprite = new Sprite(texture);
        sprite.anchor.set(0, 1);
        const anchor = diagonalToScreen(origin.x + pipe.dx, origin.y + pipe.dy);
        sprite.position.set(anchor.x, anchor.y);
        sprite.alpha = 0.6;
        sprite.tint = PIPE_TINTS[pipe.tier] ?? 0xffffff;
        sprites.push(sprite);
      }
    }
    let fits = 0;
    let firstReason: string | null = null;
    for (const item of blueprint.buildings) {
      const definition = state.content.building(item.type);
      if (definition === undefined) {
        continue;
      }
      const x = origin.x + item.dx;
      const y = origin.y + item.dy;
      const placement = canPlace(state.content, this.world, company, state.era, definition, x, y);
      if (placement.ok) {
        fits++;
      } else {
        firstReason ??= placement.reason;
      }
      traceFootprint(g, x, y, definition.size);
      g.fill({ color: placement.ok ? 0x2ec4a6 : 0xef6f6c, alpha: 0.4 });
      const art = this.textures.building(definition.id, definition.size);
      const sprite = new Sprite(art.frames[0]);
      placeArt(sprite, art, footprintAnchor(x, y, definition.size), definition.size);
      sprite.alpha = 0.65;
      if (!placement.ok) {
        sprite.tint = 0xff9090;
      }
      sprites.push(sprite);
    }
    this.ghostLayer.addChild(g);
    // Back to front, like the buildings themselves
    sprites.sort((a, b) => a.y - b.y);
    for (const sprite of sprites) {
      this.ghostLayer.addChild(sprite);
    }
    const all = blueprint.buildings.length;
    this.callbacks.onHover(tile, {
      ok: fits > 0,
      reason:
        fits === all
          ? null
          : `${all - fits} of ${all} don't fit here${firstReason ? ` (${firstReason})` : ""}`,
      multiplier: fits / Math.max(1, all),
    });
  }

  /** Ghost pipes along a route (the ones that are not there yet) */
  private drawRoute(route: TilePosition[], tier: number, company: CompanyDto) {
    const g = new Graphics();
    for (const t of route) {
      const own = this.world.conductorAt(t.x, t.y)?.owner === company.id;
      traceFootprint(g, t.x, t.y, 1);
      g.fill({ color: own ? 0x6cb4ee : 0x2ec4a6, alpha: 0.35 });
      if (!own) {
        const texture = this.textures.pipe("urdl");
        if (texture !== undefined) {
          const sprite = new Sprite(texture);
          sprite.anchor.set(0, 1);
          const anchor = diagonalToScreen(t.x, t.y);
          sprite.position.set(anchor.x, anchor.y);
          sprite.alpha = 0.55;
          sprite.tint = PIPE_TINTS[tier] ?? 0xffffff;
          this.ghostLayer.addChild(sprite);
        }
      }
    }
    this.ghostLayer.addChildAt(g, 0);
  }

  private drawHighlights() {
    const g = this.highlightLayer;
    g.clear();
    if (this.far) {
      return;
    }
    const state = this.callbacks.getState();
    if (state.hintTile !== null) {
      // Loady's suggestion: a pulsing golden footprint
      const pulse = 0.5 + 0.5 * Math.sin(performance.now() / 260);
      traceFootprint(g, state.hintTile.x, state.hintTile.y, state.hintTile.size);
      g.fill({ color: 0xf6c744, alpha: 0.18 + 0.22 * pulse });
      traceFootprint(g, state.hintTile.x, state.hintTile.y, state.hintTile.size);
      g.stroke({ color: 0xffe08a, width: 6 + 4 * pulse, alpha: 0.95 });
    }
    if (state.selectedNetwork !== null) {
      for (const conductor of this.world.conductors.values()) {
        if (conductor.network === state.selectedNetwork.id) {
          traceFootprint(g, conductor.x, conductor.y, 1);
        }
      }
      g.fill({ color: 0x6cb4ee, alpha: 0.35 + Math.sin(this.time * 3) * 0.1 });
      for (const path of state.selectedNetwork.bottlenecks) {
        for (let i = 0; i < path.length; i += 2) {
          traceFootprint(g, path[i]!, path[i + 1]!, 1);
        }
      }
      g.fill({ color: 0xef6f6c, alpha: 0.55 });
    }
    if (state.selectedBuilding !== null) {
      const building = this.world.buildings.get(state.selectedBuilding);
      if (building !== undefined) {
        traceFootprint(g, building.x, building.y, building.size);
        g.fill({ color: 0xf6c744, alpha: 0.25 + Math.sin(this.time * 4) * 0.08 });
        traceFootprint(g, building.x, building.y, building.size);
        g.stroke({ color: 0xf6c744, width: 6 });
      }
    }
    if (state.wildTile !== null) {
      // An outpost, or the claim a click would make
      const outpost = this.world.outpostAt(state.wildTile.x, state.wildTile.y);
      const x = outpost?.x ?? state.outpostCheck?.x ?? state.wildTile.x - 3;
      const y = outpost?.y ?? state.outpostCheck?.y ?? state.wildTile.y - 3;
      const ok = outpost !== undefined || (state.outpostCheck !== null && state.outpostCheck.error === null);
      traceFootprint(g, x, y, 8);
      g.fill({ color: ok ? 0x2ec4a6 : 0xef6f6c, alpha: 0.18 + Math.sin(this.time * 3) * 0.06 });
      traceFootprint(g, x, y, 8);
      g.stroke({ color: ok ? 0x2ec4a6 : 0xef6f6c, width: 6 });
    }
    if (this.hovered !== null && (state.tool.kind === "select" || state.tool.kind === "terraform")) {
      const building = this.world.buildingAt(this.hovered.x, this.hovered.y);
      if (building !== undefined) {
        traceFootprint(g, building.x, building.y, building.size);
      } else {
        traceFootprint(g, this.hovered.x, this.hovered.y, 1);
      }
      g.stroke({ color: 0xffffff, width: 4, alpha: 0.8 });
    }
  }

  /** Where a tile's centre is on the screen (for tests and tooltips) */
  tileToScreen(x: number, y: number): { x: number; y: number } {
    const center = tileCenter(x, y);
    return {
      x: (center.x + this.camera.offsetX) * this.camera.scale,
      y: (center.y + this.camera.offsetY) * this.camera.scale,
    };
  }

  /** The tile under a screen point (for the minimap and tests) */
  tileAtScreen(x: number, y: number): TilePosition {
    return this.camera.screenToTile(x, y);
  }

  worldToTile(x: number, y: number): TilePosition {
    return worldToTile(x, y);
  }
}

/** A warning sign over a building that a storm damaged */
function damageBadge(size: number): Container {
  const badge = new Container();
  const sign = new Graphics();
  sign.moveTo(0, -34).lineTo(32, 22).lineTo(-32, 22).closePath();
  sign.fill({ color: 0xf4a261 });
  sign.stroke({ color: 0x17181c, width: 6 });
  const mark = new Text({
    text: "!",
    style: { fontFamily: "system-ui", fontSize: 40, fontWeight: "900", fill: "#17181c" },
  });
  mark.anchor.set(0.5);
  mark.position.set(0, 2);
  badge.addChild(sign, mark);
  badge.position.set(TILE_WIDTH * size - 70, -TILE_HEIGHT * size - 20);
  badge.label = "damaged";
  return badge;
}

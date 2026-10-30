import {
  AnimatedSprite,
  Application,
  ColorMatrixFilter,
  Container,
  Graphics,
  Rectangle,
  Sprite,
  Text,
  Texture,
} from "pixi.js";
import type {
  BlueprintDto,
  BuildingDefinition,
  BuildingDto,
  CompanyDto,
  ConductorDto,
  DistrictDto,
  FlowDto,
  MapLinkDto,
  OutpostDto,
  SeatState,
  StallDto,
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
  DISTRICT_HEIGHT,
  DISTRICT_WIDTH,
  ESTATE_SIZE,
  HUB_LOCAL,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
  ROAD_WIDTH,
  ROAD_X,
  ROAD_Y,
  TOWN_GATES_LOCAL,
  chunkOf,
  districtOf,
  districtOriginX,
  districtOriginY,
  estateOrigin,
  isDeep,
  isParcelUnlocked,
  outskirtsParcels,
  sideOf,
  stallLocal,
  tileKey,
  yardRect,
  yardGate,
  zoneAt,
  MONUMENT_SITES,
} from "./layout";
import { BuildingEffects, pelletTexture } from "./render/effects";
import { PIPE_COLORS } from "./art/pipes";
import { Juice } from "./render/juice";
import { levelTier, LEVEL_TIERS, tierIndex } from "./levels";
import { emblemIcon } from "../app/crews";
import { sounds } from "../app/sound";
import { canPlace, footprintOrigin, routePipe, type Placement } from "./rules";
import { chunkKey, type WorldModel } from "./world";
import { fullness, insidePipe, laneOf, type InsideFlow, type LaneKind } from "./inside";
import {
  connectorKey,
  connectorPath,
  connectorTraffic,
  LANE_KINDS,
  LANE_SLOTS,
  PIPE_SIDES,
  type ConnectorFlow,
  type PipeSide,
} from "./connectors";
import { ConnectorAnimation } from "./render/connectors";

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
  /** Several buildings selected together (a box, Shift, Ctrl or a double click) */
  selection: number[];
  /** The network shown in the inspector, with the paths of its full links */
  selectedNetwork: { id: number; bottlenecks: number[][] } | null;
  /** Milliseconds to add to Date.now() to get the server's clock */
  serverOffsetMs: number;
  /** A wilderness tile the player looks at (an outpost or a claim), and whether that claim is possible */
  wildTile: TilePosition | null;
  hintTile: { x: number; y: number; size: number } | null;
  /** The building just placed: build mode shows no ghost over it (after run 4: a red ghost sat on it, "a red tile over the wind moily") */
  justBuilt: { x: number; y: number; size: number } | null;
  outpostCheck: { error: string | null; x: number; y: number } | null;
  /** Links between stations (v1.2 §7), drawn as covered channels along the corridors, and the one the Links tab shows */
  mapLinks: MapLinkDto[];
  focusLink: number | null;
  /** Picking a plot (1.4 §6): the plots it shows, by "x,y,slot", and the chosen one */
  picker: {
    seats: Record<string, SeatState>;
    chosen: { districtX: number; districtY: number; slot: number } | null;
  } | null;
}

export interface EngineCallbacks {
  getState(): EngineState;
  /** False while there is no connection: nothing is requested */
  canRequest(): boolean;
  requestChunks(pairs: number[]): void;
  unwatchChunks(pairs: number[]): void;
  requestDistrict(x: number, y: number): void;
  onHover(tile: TilePosition | null, placement: Placement | null): void;
  onClick(tile: TilePosition, building: BuildingDto | undefined, modifiers?: ClickModifiers): void;
  /** A box dragged with the Select tool: the player's buildings in it (Shift: added to the selection) */
  onSelectBuildings(ids: number[], add: boolean): void;
  onPipeRoute(route: TilePosition[] | null): void;
  onPlacePipes(route: TilePosition[]): void;
  onRemovePipes(tiles: TilePosition[]): void;
  onPlaceBuilding(type: string, x: number, y: number): void;
  /** A painted row or area of the build tool's building */
  onPlaceBuildings(type: string, spots: TilePosition[]): void;
  onMoveBuilding(id: number, x: number, y: number): void;
  onSelectArea(x: number, y: number, width: number, height: number): void;
  /** The camera went down into the Deep Level, or back up */
  onLayer(deep: boolean): void;
  onPlaceBlueprint(blueprint: BlueprintDto, x: number, y: number): void;
  onZoom(scale: number): void;
  /** A right-click without dragging: one step back (cancel the tool, deselect) */
  onBack(): void;
  /** The map where the camera looks has been drawn for the first time (until then a loading picture covers it) */
  onMapShown(): void;
}

const CHUNK_MARGIN = 1;

/** The keys held with a click, and whether it was the second click of a double click */
export interface ClickModifiers {
  shift: boolean;
  ctrl: boolean;
  double: boolean;
}

/** Two clicks on the same tile within this many milliseconds are a double click */
const DOUBLE_CLICK_MS = 400;

/** A finger held this long without moving starts a box selection (or, with the build tool, painting) */
const LONG_PRESS_MS = 450;

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

/** Buildings a pipe plugs into: what takes or gives resources (not decorations, the HQ or the offices) */
const PIPE_KINDS = new Set<string>([
  "storage",
  "tradingPost",
  "lab",
  "valve",
  "priorityValve",
  "pump",
  "tollMeter",
  "cargoDepot",
  "teleporter",
  "deepLift",
  "powerLine",
]);

export function takesPipes(definition: BuildingDefinition | undefined): boolean {
  return (
    definition !== undefined &&
    (PIPE_KINDS.has(definition.kind) ||
      Object.keys(definition.in).length > 0 ||
      Object.keys(definition.out).length > 0)
  );
}

/** How a pipe looks at a tile: the sides it joins (in u, r, d, l order) and the ones where it plugs into a building */
export interface PipeLook {
  shape: string;
  sockets: string[];
}

/** How high each lane carries its goods, as a share of the big pipe's height */
const LANE_HEIGHT: Record<LaneKind, number> = { solid: 0.7, fluid: 0.83, power: 1.05 };

const PIPE_DX: Record<string, number> = { u: 0, r: 1, d: 0, l: -1 };
const PIPE_DY: Record<string, number> = { u: -1, r: 0, d: 1, l: 0 };

function pipeLookOf(
  x: number,
  y: number,
  isPipe: (x: number, y: number) => boolean,
  isBuilding: (x: number, y: number) => boolean,
): PipeLook {
  let shape = "";
  const sockets: string[] = [];
  for (const [side, dx, dy] of PIPE_SIDES) {
    if (isPipe(x + dx, y + dy)) {
      shape += side;
    } else if (isBuilding(x + dx, y + dy)) {
      shape += side;
      sockets.push(side);
    }
  }
  return { shape, sockets };
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

/** How a building looks at its level milestones: the colour of its pad's edge */

/** Buildings that sit in a pipe line (their art shows the pipe along x) */
const IN_PIPE_KINDS = new Set(["valve", "priorityValve", "tollMeter"]);

/** The generated decal of a deposit: depositIronOre for ironOre */
function depositArt(deposit: string): string {
  return `deposit${deposit[0]!.toUpperCase()}${deposit.slice(1)}`;
}

const badgeTextures = new Map<number, Texture>();

/** A building's level badge: a shield in its tier's colour with the level on it (one texture per level) */
function levelBadge(level: number): Texture {
  let texture = badgeTextures.get(level);
  if (texture !== undefined) {
    return texture;
  }
  const tier = levelTier(level) ?? LEVEL_TIERS[0]!;
  const hex = (c: number) => `#${c.toString(16).padStart(6, "0")}`;
  const w = 62;
  const h = 70;
  const canvas = createCanvas(w + 8, h + 8);
  const g = canvas.getContext("2d")!;
  g.translate(4, 4);
  const shield = () => {
    g.beginPath();
    g.moveTo(8, 0);
    g.lineTo(w - 8, 0);
    g.quadraticCurveTo(w, 0, w, 8);
    g.lineTo(w, h * 0.5);
    g.quadraticCurveTo(w, h * 0.78, w / 2, h);
    g.quadraticCurveTo(0, h * 0.78, 0, h * 0.5);
    g.lineTo(0, 8);
    g.quadraticCurveTo(0, 0, 8, 0);
    g.closePath();
  };
  shield();
  const fill = g.createLinearGradient(0, 0, 0, h);
  fill.addColorStop(0, hex(tier.light));
  fill.addColorStop(0.45, hex(tier.color));
  fill.addColorStop(1, hex(tier.color));
  g.fillStyle = fill;
  g.fill();
  g.lineWidth = 5;
  g.strokeStyle = "#241a0c";
  g.stroke();
  // A bright inner rim
  g.save();
  g.translate(w * 0.09, h * 0.08);
  g.scale(0.82, 0.84);
  shield();
  g.restore();
  g.lineWidth = 2;
  g.strokeStyle = "rgba(255,255,255,0.55)";
  g.stroke();
  const text = String(level);
  g.font = `900 ${text.length >= 3 ? 24 : 32}px "Nunito Variable", system-ui, sans-serif`;
  g.textAlign = "center";
  g.textBaseline = "middle";
  g.lineJoin = "round";
  g.lineWidth = 6;
  g.strokeStyle = "#241a0c";
  g.strokeText(text, w / 2, h * 0.43);
  g.fillStyle = "#ffffff";
  g.fillText(text, w / 2, h * 0.43);
  texture = Texture.from(canvas);
  badgeTextures.set(level, texture);
  return texture;
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

/** A footprint grown outwards by a share of a tile on every side: a ring that shows around a building's plate */
function traceRing(g: Graphics, x: number, y: number, size: number, grow: number) {
  const top = diagonalToScreen(x, y);
  const bottom = diagonalToScreen(x + size - 1, y + size - 1);
  const corners = [
    { x: top.x + TILE_WIDTH, y: top.y - TILE_HEIGHT },
    {
      x: diagonalToScreen(x + size - 1, y).x + TILE_WIDTH * 2,
      y: diagonalToScreen(x + size - 1, y).y - TILE_HEIGHT / 2,
    },
    { x: bottom.x + TILE_WIDTH, y: bottom.y },
    { x: diagonalToScreen(x, y + size - 1).x, y: diagonalToScreen(x, y + size - 1).y - TILE_HEIGHT / 2 },
  ];
  const cx = (corners[0]!.x + corners[2]!.x) / 2;
  const cy = (corners[0]!.y + corners[2]!.y) / 2;
  const scale = 1 + grow / size;
  corners.forEach((c, i) => {
    const px = cx + (c.x - cx) * scale;
    const py = cy + (c.y - cy) * scale;
    if (i === 0) {
      g.moveTo(px, py);
    } else {
      g.lineTo(px, py);
    }
  });
  g.closePath();
}

/** A selected building: a golden glow on its footprint and a bright ring just outside its plate */
function drawSelected(g: Graphics, building: BuildingDto, time: number) {
  traceFootprint(g, building.x, building.y, building.size);
  g.fill({ color: 0xf6c744, alpha: 0.22 + Math.sin(time * 4) * 0.07 });
  traceRing(g, building.x, building.y, building.size, 0.2);
  g.stroke({ color: 0xf6c744, width: 14, alpha: 0.28 });
  traceRing(g, building.x, building.y, building.size, 0.2);
  g.stroke({ color: 0xffe08a, width: 4, alpha: 0.95 });
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
  /** Its trunk lines, over the fog of the wilderness they cross */
  lines: Graphics;
  labels: Container;
  hub: ObjectView[];
}

/** What is drawn for one chunk */
class ChunkView {
  readonly cx: number;
  readonly cy: number;
  ground: Container | null = null;
  overlay: Container | null = null;
  /** The Deposits overlay's seat labels, over the buildings */
  overlayLabels: Container | null = null;
  /** The fog over wilderness the player hasn't mapped (1.4 §20) */
  fog: Graphics | null = null;
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
  /** The fog over the wilderness nobody of yours mapped yet, over its trees and deposits */
  private readonly fogLayer = new Container();
  /** Trunk lines: over the fog they cross */
  private readonly lineLayer = new Container();
  private overlay: Overlay = "none";
  private readonly highlightLayer = new Graphics();
  /** Picking a plot: the free plots glow (pulsing), the closed ones are amber, the chosen one has a gold border */
  private readonly pickGlow = new Graphics();
  private readonly pickMarks = new Graphics();
  private pickKey = "";
  /** Links: their channels (drawn when they change) and the goods moving along the flowing ones */
  private readonly linkLayer = new Graphics();
  private readonly linkPulse = new Graphics();
  private drawnLinks: MapLinkDto[] | null = null;
  private drawnFocus: number | null = null;
  private drawnWidth = 0;
  private linkPaths: Array<{
    points: Array<{ x: number; y: number }>;
    length: number;
    color: string;
    mine: boolean;
  }> = [];
  private linkTime = 0;
  private readonly objectLayer = new Container({ sortableChildren: true });
  private readonly flowLayer = new Container();
  private readonly ghostLayer = new Container();
  private readonly labelLayer = new Container();
  private readonly juice = new Juice();
  /** Seconds until the next round of coin pops */
  private coinTimer = 2;
  private readonly chunks = new Map<string, ChunkView>();
  private readonly districtViews = new Map<string, DistrictView>();
  private readonly effects = new Set<ObjectView>();
  private readonly dirtyChunks = new Set<string>();
  private flowsDirty = true;
  private districtsDirty = true;
  /** Whether the camera was sent somewhere, and the map there has been drawn */
  private lookedAt = false;
  private shown = false;
  private drawnAt: number | null = null;
  /** The box of a selection drag (in screen pixels), drawn over the map */
  private readonly boxLayer = new Graphics();
  private dragPointerType = "mouse";
  private dragShift = false;
  private dragCtrl = false;
  private lastClick: { x: number; y: number; at: number } | null = null;
  /** The level tier each building was last drawn with: an upgrade into a new one flashes */
  private readonly shownTiers = new Map<number, number>();
  /** Building pictures that were still loading have arrived */
  private artArrived = false;
  private lastArtRedraw = 0;
  /** Every flow's way along its pipes as drawn (look inside draws over it) */
  private flowPaths: Array<{ flow: FlowDto; network: number; points: Array<{ x: number; y: number }> }> = [];
  /** Look inside (1.4 §9): the pipe under the pointer, or every pipe while Alt is held, cut open */
  private readonly insideLayer = new Graphics();
  private insideKey = "";
  private flowsVersion = 0;
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
  /** Where a drag with the build tool started: it paints a row (with Shift an area) */
  private paintStart: TilePosition | null = null;
  /** A finger was held still: its drag selects or paints instead of moving the map */
  private longPressed = false;
  private longPressTimer: number | null = null;
  private pipeRoute: TilePosition[] | null = null;
  private bulldozeTiles = new Map<number, TilePosition>();
  private hovered: TilePosition | null = null;
  private lastToolKey = "";
  private readonly keys = new Set<string>();
  /** The pipes' lane connectors: they fade as the map zooms out (1.4 §9: the map stays calm) */
  private readonly laneSprites = new Set<Sprite>();
  private laneScale = 0;
  private readonly connectorViews = new Set<ConnectorAnimation>();
  private connectorFlows = new Map<string, ConnectorFlow[]>();
  private readonly dynamicLanes = new Map<number, string>();
  private readonly reducedMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)");
  private connectorSoundAt = 0;
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
      this.linkLayer,
      this.linkPulse,
      this.highlightLayer,
      this.objectLayer,
      this.fogLayer,
      this.lineLayer,
      this.pickGlow,
      this.pickMarks,
      this.insideLayer,
      this.flowLayer,
      this.ghostLayer,
      this.juice.layer,
      this.labelLayer,
    );
    app.stage.addChild(this.worldLayer, this.boxLayer);
    // The far view starts hidden (it is switched when the zoom crosses FAR_ZOOM)
    this.miniLayer.visible = false;
    this.registerInput(app.canvas);
    app.ticker.add(this.update, this);
    this.removeListeners.push(
      textures.onArtArrived(() => {
        this.artArrived = true;
      }),
    );
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
    const engine = new Engine(app, textures, world, callbacks);
    setTimeout(() => void textures.warmCache(), 4000);
    return engine;
  }

  get textureBank(): TextureBank {
    return this.textures;
  }

  get scale(): number {
    return this.camera.scale;
  }

  /** Shows a rectangle of tiles whole (a link's route): centred, zoomed to fit with a margin */
  lookAtArea(x0: number, y0: number, x1: number, y1: number) {
    const span = Math.max(1, x1 - x0 + (y1 - y0));
    const scale = Math.min(
      0.42,
      this.app.screen.width / (span * TILE_WIDTH * 1.3),
      this.app.screen.height / (span * (TILE_HEIGHT / 2) * 1.3),
    );
    this.lookAt((x0 + x1) / 2, (y0 + y1) / 2, scale);
  }

  /** Centres the view on a tile */
  lookAt(x: number, y: number, scale?: number) {
    if (scale !== undefined) {
      this.camera.zoomTo(scale, 0, 0);
      this.callbacks.onZoom(this.camera.scale);
    }
    const center = tileCenter(x, y);
    this.camera.centerOn(center.x, center.y, this.app.screen.width, this.app.screen.height);
    this.lookedAt = true;
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
      this.dragPointerType = event.pointerType;
      this.dragShift = event.shiftKey;
      this.dragCtrl = event.ctrlKey || event.metaKey;
      this.movedBeyondClick = false;
      canvas.setPointerCapture(event.pointerId);
      const tool = this.callbacks.getState().tool;
      if (event.button === 0 && tool.kind === "pipe") {
        this.pipeStart = this.pipeTargetAt(local.x, local.y);
        this.updatePipeRoute();
      }
      if (event.button === 0 && tool.kind === "bulldoze") {
        this.bulldozeTiles.clear();
        this.addBulldozeTile();
      }
      if (event.button === 0 && tool.kind === "blueprintArea") {
        this.areaStart = this.camera.screenToTile(local.x, local.y);
      }
      if (event.button === 0 && tool.kind === "build" && tool.crate === undefined) {
        this.paintStart = this.camera.screenToTile(local.x, local.y);
      }
      this.longPressed = false;
      if (
        event.pointerType === "touch" &&
        (tool.kind === "select" || (tool.kind === "build" && tool.crate === undefined))
      ) {
        this.clearLongPress();
        this.longPressTimer = window.setTimeout(() => {
          this.longPressTimer = null;
          if (this.dragging && !this.movedBeyondClick && touches.size === 1) {
            this.longPressed = true;
            navigator.vibrate?.(12);
          }
        }, LONG_PRESS_MS);
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
          // Moved before the long press: it moves the map
          this.clearLongPress();
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
      this.clearLongPress();
      this.dragging = false;
      this.dragPointerId = null;
      if (canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
      if (!completed) {
        this.cancelToolDrag();
        return;
      }
      const local = toLocal(event);
      this.pointerX = local.x;
      this.pointerY = local.y;
      if (wasToolDrag && this.isBoxDrag()) {
        if (this.movedBeyondClick) {
          this.finishBox();
        } else {
          this.boxLayer.clear();
          this.click(this.camera.screenToTile(this.pointerX, this.pointerY));
        }
      } else if (wasToolDrag) {
        this.finishToolDrag();
      } else if (!this.movedBeyondClick && this.dragButton === 0) {
        this.click(this.camera.screenToTile(this.pointerX, this.pointerY));
      } else if (!this.movedBeyondClick && this.dragButton === 2) {
        // Right-click: back (a right-drag pans the map)
        this.callbacks.onBack();
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
      // Alt looks inside the pipes: it doesn't open the browser's menu
      if (event.key === "Alt") {
        event.preventDefault();
      }
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
      (tool.kind === "pipe" ||
        tool.kind === "bulldoze" ||
        tool.kind === "blueprintArea" ||
        this.isBoxDrag() ||
        this.isPaintDrag())
    );
  }

  /** The left button with the build tool paints a row of the building (a click builds one); a finger after a long press */
  private isPaintDrag(): boolean {
    const tool = this.callbacks.getState().tool;
    return (
      this.dragButton === 0 &&
      (this.dragPointerType !== "touch" || this.longPressed) &&
      tool.kind === "build" &&
      tool.crate === undefined &&
      this.paintStart !== null
    );
  }

  private clearLongPress() {
    if (this.longPressTimer !== null) {
      clearTimeout(this.longPressTimer);
      this.longPressTimer = null;
    }
  }

  /**
   * The spots of a painted row of the build tool's building (with Shift an area), a building's width apart: where it
   * fits, as far as the gold goes. Blocked: the ones left out.
   */
  private paintSpots(): {
    spots: TilePosition[];
    blocked: TilePosition[];
    cost: number;
    short: boolean;
  } | null {
    const state = this.callbacks.getState();
    const tool = state.tool;
    if (tool.kind !== "build" || this.paintStart === null || state.company === null) {
      return null;
    }
    const definition = state.content.building(tool.type);
    if (definition === undefined) {
      return null;
    }
    const size = definition.size;
    const start = footprintOrigin(this.paintStart.x, this.paintStart.y, size);
    const pointer = this.camera.screenToTile(this.pointerX, this.pointerY);
    const end = footprintOrigin(pointer.x, pointer.y, size);
    const stepX = end.x >= start.x ? size : -size;
    const stepY = end.y >= start.y ? size : -size;
    const countX = Math.floor(Math.abs(end.x - start.x) / size) + 1;
    const countY = Math.floor(Math.abs(end.y - start.y) / size) + 1;
    const candidates: TilePosition[] = [];
    if (this.dragShift || this.keys.has("shift")) {
      for (let j = 0; j < countY; j++) {
        for (let i = 0; i < countX; i++) {
          candidates.push({ x: start.x + i * stepX, y: start.y + j * stepY });
        }
      }
    } else if (countX >= countY) {
      for (let i = 0; i < countX; i++) {
        candidates.push({ x: start.x + i * stepX, y: start.y });
      }
    } else {
      for (let j = 0; j < countY; j++) {
        candidates.push({ x: start.x, y: start.y + j * stepY });
      }
    }
    let copies = [...this.world.buildings.values()].filter(
      (t) => t.owner === state.company!.id && t.type === definition.id,
    ).length;
    const spots: TilePosition[] = [];
    const blocked: TilePosition[] = [];
    let cost = 0;
    let short = false;
    for (const spot of candidates.slice(0, 100)) {
      const placement = canPlace(
        state.content,
        this.world,
        state.company,
        state.era,
        definition,
        spot.x,
        spot.y,
      );
      const price = state.content.buildCost(definition, copies);
      if (!placement.ok || short || cost + price > state.company.gold) {
        short ||= placement.ok;
        blocked.push(spot);
        continue;
      }
      spots.push(spot);
      cost += price;
      copies++;
    }
    return { spots, blocked, cost, short };
  }

  /** The left button with the Select tool draws a selection box (the right button pans); a finger after a long press */
  private isBoxDrag(): boolean {
    return (
      this.dragButton === 0 &&
      (this.dragPointerType !== "touch" || this.longPressed) &&
      this.callbacks.getState().tool.kind === "select"
    );
  }

  /** The selection box between where the drag started and the pointer, in screen pixels */
  private drawBox() {
    const g = this.boxLayer;
    g.clear();
    if (!this.dragging || !this.isBoxDrag() || !this.movedBeyondClick) {
      return;
    }
    const x = Math.min(this.pressX, this.pointerX);
    const y = Math.min(this.pressY, this.pointerY);
    const w = Math.abs(this.pointerX - this.pressX);
    const h = Math.abs(this.pointerY - this.pressY);
    g.rect(x, y, w, h)
      .fill({ color: 0x2ec4a6, alpha: 0.14 })
      .stroke({ color: 0xffffff, width: 2, alpha: 0.9 });
  }

  /** The player's buildings whose middle is inside the selection box */
  private finishBox() {
    this.boxLayer.clear();
    const ids = this.ownBuildingsIn(
      Math.min(this.pressX, this.pointerX),
      Math.min(this.pressY, this.pointerY),
      Math.max(this.pressX, this.pointerX),
      Math.max(this.pressY, this.pointerY),
    );
    this.callbacks.onSelectBuildings(ids, this.dragShift);
  }

  /** The player's buildings on screen (Ctrl+A) */
  ownBuildingsInView(): number[] {
    return this.ownBuildingsIn(0, 0, this.app.screen.width, this.app.screen.height);
  }

  /** The player's buildings whose middle is inside a rectangle in screen pixels */
  private ownBuildingsIn(left: number, top: number, right: number, bottom: number): number[] {
    const company = this.callbacks.getState().company;
    const ids: number[] = [];
    for (const building of this.world.buildings.values()) {
      if (company === null || building.owner !== company.id) {
        continue;
      }
      const middle = this.tileToScreen(
        building.x + (building.size - 1) / 2,
        building.y + (building.size - 1) / 2,
      );
      if (middle.x >= left && middle.x <= right && middle.y >= top && middle.y <= bottom) {
        ids.push(building.id);
      }
    }
    return ids;
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
    this.paintStart = null;
    this.pipeStart = null;
    this.pipeRoute = null;
    this.bulldozeTiles.clear();
    this.callbacks.onPipeRoute(null);
  }

  private finishToolDrag() {
    const tool = this.callbacks.getState().tool;
    if (tool.kind === "build") {
      const paint = this.movedBeyondClick ? this.paintSpots() : null;
      if (paint === null) {
        this.click(this.camera.screenToTile(this.pointerX, this.pointerY));
      } else if (paint.spots.length === 1) {
        this.callbacks.onPlaceBuilding(tool.type, paint.spots[0]!.x, paint.spots[0]!.y);
      } else if (paint.spots.length > 1) {
        this.callbacks.onPlaceBuildings(tool.type, paint.spots);
      }
    }
    if (tool.kind === "pipe") {
      this.updatePipeRoute();
      if (this.pipeRoute !== null && this.pipeRoute.length > 0) {
        this.callbacks.onPlacePipes(this.pipeRoute);
      }
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
    const now = performance.now();
    const double =
      this.lastClick !== null &&
      this.lastClick.x === tile.x &&
      this.lastClick.y === tile.y &&
      now - this.lastClick.at < DOUBLE_CLICK_MS;
    this.lastClick = double ? null : { x: tile.x, y: tile.y, at: now };
    this.callbacks.onClick(tile, this.world.buildingAt(tile.x, tile.y), {
      shift: this.dragShift,
      ctrl: this.dragCtrl,
      double,
    });
  }

  private updatePipeRoute() {
    const state = this.callbacks.getState();
    if (this.pipeStart === null || state.company === null) {
      return;
    }
    const end = this.pipeTargetAt(this.pointerX, this.pointerY);
    const route = routePipe(this.world, state.company, this.pipeStart, end, state.content);
    this.pipeRoute = route;
    this.callbacks.onPipeRoute(route);
  }

  /** The inlet box, posts and barrier all belong to the gate tile. */
  private pipeTargetAt(screenX: number, screenY: number): TilePosition {
    const company = this.callbacks.getState().company;
    if (company !== null) {
      const gate = yardGate(company.estateX, company.estateY, company.slot);
      if (this.world.conductorAt(gate.x, gate.y)?.kind === "gate") {
        const anchor = diagonalToScreen(gate.x, gate.y);
        const point = this.camera.screenToWorld(screenX, screenY);
        const side = sideOf(company.slot);
        const alongU = side === "north" || side === "south";
        const tolerance = Math.min(16, (this.dragPointerType === "touch" ? 8 : 3) / this.camera.scale);
        if (this.textures.gateContains(alongU, point.x - anchor.x, point.y - anchor.y, tolerance)) {
          return gate;
        }
      }
    }
    return this.camera.screenToTile(screenX, screenY);
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
    this.drawBox();
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
    if (this.lookedAt && !this.shown) {
      const tile = this.camera.screenToTile(this.app.screen.width / 2, this.app.screen.height / 2);
      const chunk = chunkOf(tile.x, tile.y);
      if (this.chunks.get(chunkKey(chunk.x, chunk.y))?.loaded === true) {
        // Drawn: a moment more for the pictures of the buildings in it
        this.drawnAt ??= this.time;
        if (!this.textures.loadingBuildings || this.time - this.drawnAt > 3) {
          this.shown = true;
          this.callbacks.onMapShown();
        }
      }
    }
    // Pictures that were still loading are there: the objects are drawn again (a few times a second at most)
    if (this.artArrived && this.time - this.lastArtRedraw > 0.25) {
      this.artArrived = false;
      this.lastArtRedraw = this.time;
      for (const chunk of this.chunks.values()) {
        if (chunk.loaded) {
          this.clearObjects(chunk);
          this.drawObjects(chunk);
        }
      }
      this.districtsDirty = true;
    }
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

    const tool = this.callbacks.getState().tool;
    const tile =
      tool.kind === "pipe"
        ? this.pipeTargetAt(this.pointerX, this.pointerY)
        : this.camera.screenToTile(this.pointerX, this.pointerY);
    // A blueprint tool carries its whole layout: its id is enough
    const toolKey =
      (tool.kind === "blueprint" ? `blueprint:${tool.blueprint.id}` : JSON.stringify(tool)) +
      (this.callbacks.getState().plannedRoute?.length ?? 0) +
      // A building just placed under the pointer: its ghost goes at once
      JSON.stringify(this.callbacks.getState().justBuilt);
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
    this.fadeLanes();
    this.drawInside();
    this.drawPicker();
    this.drawLinks(deltaSeconds);
    this.animate(deltaSeconds);
    this.juice.update(deltaSeconds);
    this.popCoins(deltaSeconds);
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
        this.districtViews.set(key, {
          graphics: new Graphics(),
          lines: new Graphics(),
          labels: new Container(),
          hub: [],
        });
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
      view.lines.destroy();
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
    chunk.overlay?.destroy({ children: true });
    chunk.overlay = null;
    chunk.overlayLabels?.destroy({ children: true });
    chunk.overlayLabels = null;
    chunk.fog?.destroy();
    chunk.fog = null;
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
    this.drawFog(chunk);
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

  /**
   * Fog over the wilderness of a chunk the player's expeditions haven't mapped (1.4 §20): its runs of wilderness tiles
   * row by row, the same runs merged into rectangles
   */
  private drawFog(chunk: ChunkView) {
    chunk.fog?.destroy();
    chunk.fog = null;
    const company = this.callbacks.getState().company;
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;
    if (company === null || isDeep(y0) || (company.explored ?? []).includes(`${chunk.cx},${chunk.cy}`)) {
      return;
    }
    const rects: Array<{ x: number; y: number; w: number; h: number }> = [];
    let open: Array<{ x: number; y: number; w: number; h: number }> = [];
    let previous = "";
    for (let y = y0; y < y0 + CHUNK_SIZE; y++) {
      const runs: Array<[number, number]> = [];
      for (let x = x0; x < x0 + CHUNK_SIZE; x++) {
        if (zoneAt(x, y).kind !== "wilderness") {
          continue;
        }
        const last = runs[runs.length - 1];
        if (last !== undefined && last[1] === x) {
          last[1] = x + 1;
        } else {
          runs.push([x, x + 1]);
        }
      }
      const key = runs.map((t) => t.join("-")).join(",");
      if (key === previous) {
        open.forEach((t) => t.h++);
        continue;
      }
      rects.push(...open);
      open = runs.map(([a, b]) => ({ x: a, y, w: b - a, h: 1 }));
      previous = key;
    }
    rects.push(...open);
    if (rects.length === 0) {
      return;
    }
    const g = new Graphics();
    for (const rect of rects) {
      traceFootprint(g, rect.x, rect.y, rect.w, rect.h);
      g.fill({ color: 0xdfe4e2, alpha: 0.82 });
    }
    this.fogLayer.addChild(g);
    chunk.fog = g;
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
    chunk.overlay?.destroy({ children: true });
    chunk.overlay = null;
    chunk.overlayLabels?.destroy({ children: true });
    chunk.overlayLabels = null;
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
            // Only where something can be built: forests and mountains would be hard dark squares under their trees
            if (["water", "road", "city", "forest", "mountain"].includes(tile.terrain)) {
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
            } else if (tile.terrain === "beach") {
              // A beach is sand: the Sand Dredger's ground
              color = content.resourceColor("sand");
              alpha = 0.7;
            } else if (tile.terrain === "forest") {
              // Wood: a Lumber Camp's ground
              color = content.resourceColor("wood");
              alpha = 0.55;
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
    const overlay = new Container();
    overlay.addChild(g);
    // Dealt land (step 5): every seat's goods on it, so a player sees who in the station holds what (over the buildings)
    const labels = new Container();
    if (this.overlay === "deposits") {
      for (const district of this.world.districts.values()) {
        for (const estate of district.estates) {
          const cx = estate.x + ESTATE_SIZE / 2;
          const cy = estate.y + ESTATE_SIZE / 2;
          if (
            estate.goods === null ||
            estate.goods.length === 0 ||
            cx < x0 ||
            cx >= x0 + CHUNK_SIZE ||
            cy < y0 ||
            cy >= y0 + CHUNK_SIZE
          ) {
            continue;
          }
          // Smaller, and on the estate's upper edge rather than over its HQ, with the iron every estate holds (run 2:
          // huge words over the town hall, clipped at the screen's edge, and Mia-B couldn't find her iron)
          const label = new Text({
            text: `${estate.goods.map((t) => content.resourceName(t).toLowerCase()).join(" · ")} (+ iron ore)`,
            style: {
              fontFamily: "system-ui",
              fontSize: 40,
              fontWeight: "800",
              fill: "#ffffff",
              stroke: { color: "#0b1d24", width: 9 },
            },
          });
          label.anchor.set(0.5);
          const at = tileCenter(cx, estate.y + 3);
          label.position.set(at.x, at.y);
          labels.addChild(label);
        }
      }
    }
    chunk.overlay = overlay;
    this.overlayLayer.addChild(overlay);
    chunk.overlayLabels = labels;
    this.labelLayer.addChild(labels);
  }

  /** The far view of a chunk: one small picture of its terrain and buildings */
  private drawMini(chunk: ChunkView) {
    const scale = 6;
    const canvas = createCanvas(CHUNK_SIZE * 2 * scale, CHUNK_SIZE * scale);
    const g = canvas.getContext("2d")!;
    const x0 = chunk.cx * CHUNK_SIZE;
    const y0 = chunk.cy * CHUNK_SIZE;
    const offset = CHUNK_SIZE * scale;
    const diamond = (px: number, py: number, lift = 0) => {
      g.beginPath();
      g.moveTo(px, py - lift);
      g.lineTo(px + scale, py + scale / 2 - lift);
      g.lineTo(px, py + scale - lift);
      g.lineTo(px - scale, py + scale / 2 - lift);
      g.closePath();
    };
    const ROOFS = ["#9b4a34", "#2f6f72", "#5f6b75", "#b5762f"];
    // Back to front, so trees and roofs overlap the tiles behind them
    for (let ly = 0; ly < CHUNK_SIZE; ly++) {
      for (let lx = 0; lx < CHUNK_SIZE; lx++) {
        const x = x0 + lx;
        const y = y0 + ly;
        const terrain: TerrainType = this.world.terrain(x, y) ?? "grass";
        const px = offset + (lx - ly) * scale;
        const py = (lx + ly) * (scale / 2);
        // A painted look: every tile a little lighter or darker
        const base = this.textures.terrainColor(terrain) ?? TERRAIN_MAP_COLORS[terrain];
        g.fillStyle = base;
        diamond(px, py);
        g.fill();
        const jitter = hash01((x * 4931) ^ (y * 7703)) - 0.5;
        g.fillStyle = jitter > 0 ? `rgba(255,255,240,${jitter * 0.16})` : `rgba(20,30,10,${-jitter * 0.2})`;
        g.fill();
        const building = this.world.buildingAt(x, y);
        if (building !== undefined) {
          // A small roofed block
          g.fillStyle = "rgba(0,0,0,0.35)";
          diamond(px, py);
          g.fill();
          g.fillStyle = ROOFS[building.id % ROOFS.length]!;
          diamond(px, py, 3);
          g.fill();
          g.fillStyle = "rgba(255,255,255,0.18)";
          g.fillRect(px - 1, py - 2, 2, 2);
        } else if (this.world.conductorAt(x, y) !== undefined) {
          g.fillStyle = "#8f969e";
          diamond(px, py);
          g.fill();
        } else if (terrain === "forest" && hash01((x * 131) ^ (y * 197)) > 0.25) {
          g.fillStyle = "#1f4f22";
          g.beginPath();
          g.arc(px, py + scale * 0.2, scale * 0.62, 0, Math.PI * 2);
          g.fill();
          g.fillStyle = "#3f8a35";
          g.beginPath();
          g.arc(px - 1, py, scale * 0.42, 0, Math.PI * 2);
          g.fill();
        } else if (terrain === "mountain") {
          g.fillStyle = "#7d8288";
          g.beginPath();
          g.moveTo(px - scale * 0.9, py + scale * 0.7);
          g.lineTo(px, py - scale * 0.9);
          g.lineTo(px + scale * 0.9, py + scale * 0.7);
          g.closePath();
          g.fill();
          g.fillStyle = "#eef2f4";
          g.beginPath();
          g.moveTo(px - scale * 0.25, py - scale * 0.3);
          g.lineTo(px, py - scale * 0.9);
          g.lineTo(px + scale * 0.25, py - scale * 0.3);
          g.closePath();
          g.fill();
        }
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
        if (roll < 0.03) {
          return tileSprite(this.textures.lantern());
        }
        return roll < 0.07
          ? propSprite("stalagmites", 0.3 + hash01(x * 7) * 0.4, 0.3 + hash01(y * 7) * 0.4, 0.8)
          : null;
      case "forest": {
        const trees = this.textures.forestTile(variant);
        if (trees === null) {
          return tileSprite(this.textures.forest(variant));
        }
        const sprite = new Sprite(trees.texture);
        sprite.anchor.set(trees.anchorX, trees.anchorY);
        sprite.scale.set(trees.scale);
        sprite.position.set(anchor.x + TILE_WIDTH, anchor.y);
        sprite.zIndex = depth;
        return sprite;
      }
      case "mountain":
        return (
          propSprite(variant % 2 === 0 ? "peak1" : "peak2", 0.5, 0.55) ??
          tileSprite(this.textures.rocks(variant))
        );
      case "grass":
      case "hills":
      case "redHills": {
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
      case "city": {
        // The hub town: houses, stalls and trees on the free squares (streets every 8 tiles stay open)
        const frontage = this.freeHubTile(x, y);
        if (frontage === null || roll > (frontage === "street" ? 0.85 : 0.08)) {
          return null;
        }
        // Houses and shops face the streets; a tree or a stall now and then inside the blocks
        const town =
          frontage === "street"
            ? ["town-house1", "town-house2", "town-house3", "town-house1", "town-house2", "town-stall"]
            : ["town-tree", "town-stall"];
        const art = this.textures.generated(town[Math.floor(hash01((x * 37) ^ (y * 59)) * town.length)]!, 1);
        if (art === null) {
          return null;
        }
        const sprite = new Sprite(art.frames[0]);
        placeArt(sprite, art, footprintAnchor(x, y, 1), 1);
        sprite.zIndex = depth + 2;
        return sprite;
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

  /**
   * A free hub tile for the town around the structures: "street" when it lies along one of the streets (every 8
   * tiles, and the two roads that cross the hub), "inner" inside a block, null when it is a street, next to a structure
   * or outside the hub
   */
  private freeHubTile(x: number, y: number): "street" | "inner" | null {
    const d = districtOf(x, y);
    const district = this.world.district(d.x, d.y);
    if (district === undefined) {
      return null;
    }
    const lx = x - (district.originX + HUB_LOCAL.x0);
    const ly = y - (district.originY + HUB_LOCAL.y0);
    const roadX = ROAD_X - HUB_LOCAL.x0;
    const roadY = ROAD_Y - HUB_LOCAL.y0;
    const onRoad = (lx >= roadX && lx < roadX + ROAD_WIDTH) || (ly >= roadY && ly < roadY + ROAD_WIDTH);
    if (
      lx < 1 ||
      ly < 1 ||
      lx > HUB_LOCAL.x1 - HUB_LOCAL.x0 - 1 ||
      ly > HUB_LOCAL.y1 - HUB_LOCAL.y0 - 1 ||
      lx % 8 === 0 ||
      ly % 8 === 0 ||
      onRoad
    ) {
      return null;
    }
    if (
      district.hub.some((t) => lx >= t.x - 1 && ly >= t.y - 1 && lx <= t.x + t.size && ly <= t.y + t.size)
    ) {
      return null;
    }
    return lx % 8 === 1 || lx % 8 === 7 || ly % 8 === 1 || ly % 8 === 7 ? "street" : "inner";
  }

  /** Whether a tall prop on a tile stands in front of a pipe or a building (it covers up to two tiles behind it) */
  private hidesSomething(x: number, y: number): boolean {
    for (let dy = 0; dy <= 2; dy++) {
      for (let dx = 0; dx <= 2; dx++) {
        if ((dx !== 0 || dy !== 0) && this.world.isOccupied(x - dx, y - dy)) {
          return true;
        }
      }
    }
    return false;
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
        // Trees and peaks in front of a pipe or a building are see-through, so nothing hides behind them
        if ((terrain === "forest" || terrain === "mountain") && this.hidesSomething(x, y)) {
          sprite.alpha = 0.42;
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

  /**
   * The kinds of goods a building at a tile takes or makes, in the order its lanes stand (a belt, a slim pipe, cables);
   * a Storage or a trading post shows the kinds moving through it now
   */
  private laneKinds(x: number, y: number): LaneKind[] {
    const building = this.world.buildingAt(x, y);
    const content = this.callbacks.getState().content;
    const definition = building !== undefined ? content.building(building.type) : undefined;
    if (definition === undefined) {
      return [];
    }
    let goods = [...Object.keys(definition.in), ...Object.keys(definition.out)];
    if (goods.length === 0 && building !== undefined && building.network !== null) {
      // A Storage or a trading post: the kinds of goods moving through it right now (none: the inlet box)
      goods = (this.world.flows.get(building.network)?.flows ?? [])
        .filter((t) => (t.from === building.id || t.to === building.id) && t.delivered > 1e-6)
        .map((t) => t.resource);
    }
    const kinds = new Set(goods.map((t) => content.resource(t)?.kind ?? "solid"));
    return LANE_KINDS.filter((t) => kinds.has(t));
  }

  /** Whether the building at a tile takes pipes */
  private pipeBuildingAt(x: number, y: number): boolean {
    const building = this.world.buildingAt(x, y);
    return building !== undefined && takesPipes(this.callbacks.getState().content.building(building.type));
  }

  private pipeLook(x: number, y: number, extra?: (x: number, y: number) => boolean): PipeLook {
    return pipeLookOf(
      x,
      y,
      (tx, ty) => this.world.conductorAt(tx, ty) !== undefined || (extra?.(tx, ty) ?? false),
      (tx, ty) => this.pipeBuildingAt(tx, ty),
    );
  }

  /** A pipe tile at its anchor: the generated piece with its sockets, or the procedural pipe without generated art */
  private pipeDisplay(x: number, y: number, tier: number, look: PipeLook): Container {
    const anchor = diagonalToScreen(x, y);
    const container = new Container();
    container.position.set(anchor.x, anchor.y);
    const piece = this.textures.pipePiece(look.shape, tier);
    if (piece === null) {
      const legacy =
        look.shape.length === 0
          ? "urdl"
          : look.shape.length === 1
            ? /[ud]/.test(look.shape)
              ? "ud"
              : "rl"
            : look.shape;
      const sprite = new Sprite(this.textures.styledPipe(legacy, tier));
      sprite.anchor.set(0, 1);
      container.addChild(sprite);
      return container;
    }
    // Over the piece: a foot under every other straight tube, a flange at every third joint and where the tier
    // changes, the sockets into buildings
    const overlays: string[] = [];
    if ((look.shape === "rl" || look.shape === "ud") && (x + y) % 2 === 0) {
      overlays.push(look.shape === "rl" ? "foot-x" : "foot-y");
    }
    for (const [side, dx, dy] of PIPE_SIDES) {
      if (!look.shape.includes(side) || look.sockets.includes(side)) {
        continue;
      }
      const ex = dx < 0 ? x - 1 : x;
      const ey = dy < 0 ? y - 1 : y;
      const neighbour = this.world.conductorAt(x + dx, y + dy);
      if (
        (((ex + ey) % 3) + 3) % 3 === 0 ||
        (neighbour !== undefined && neighbour.kind === "pipe" && neighbour.tier !== tier)
      ) {
        overlays.push(`flange-${side}`);
      }
    }
    // A building inlet is one connected piece: its crossbar extends into the neighbouring pipes. Layering a bend or
    // a capped end beneath a separate manifold leaves overlapping tubes and false caps across a continuous run.
    const side = look.sockets.length === 1 ? look.sockets[0]! : null;
    const lanes = side !== null ? this.laneKinds(x + PIPE_DX[side]!, y + PIPE_DY[side]!) : [];
    const feed = side !== null ? `feed-${side}-${look.shape.replace(side, "") || "none"}` : null;
    if (side !== null && lanes.length > 0 && feed !== null && this.textures.pipePiece(feed, tier) !== null) {
      // The lanes, and the goods moving on them (conductorView), share one layer. On the far sides (u, l) the
      // building stands behind the pipe and the whole tube is in front of its connectors: they go under it.
      const lanesLayer = new Container({ label: "lanes" });
      const body: Sprite[] = [];
      for (const shape of [feed, ...overlays.filter((s) => !s.startsWith("foot-"))]) {
        const art = this.textures.pipePiece(shape, tier);
        if (art !== null) {
          const sprite = new Sprite(art.texture);
          sprite.position.set(art.x, art.y);
          sprite.scale.set(art.scale);
          if (shape === feed) sprite.label = "pipe-body";
          body.push(sprite);
        }
      }
      lanes.forEach((kind, i) => {
        const art = this.textures.pipePiece(`lane-${kind}-${side}-${LANE_SLOTS[lanes.length]![i]}`, tier);
        if (art !== null) {
          const sprite = new Sprite(art.texture);
          sprite.position.set(art.x, art.y);
          sprite.scale.set(art.scale);
          sprite.alpha = this.laneAlpha();
          this.laneSprites.add(sprite);
          lanesLayer.addChild(sprite);
        }
      });
      if (side === "u" || side === "l") container.addChild(lanesLayer, ...body);
      else container.addChild(...body, lanesLayer);
      return container;
    }
    // A straight run past a building taps into it with a tee (a hub is for pipes meeting)
    const through = look.shape.replace(look.sockets[0] ?? "", "");
    const tee =
      look.shape.length === 3 && look.sockets.length === 1 && (through === "ud" || through === "rl");
    const main = tee ? `tee-${look.sockets[0]}` : look.shape;
    for (const shape of [main, ...overlays, ...look.sockets.map((t) => `socket-${t}`)]) {
      const art = this.textures.pipePiece(shape, tier);
      if (art !== null) {
        const sprite = new Sprite(art.texture);
        sprite.position.set(art.x, art.y);
        sprite.scale.set(art.scale);
        container.addChild(sprite);
      }
    }
    return container;
  }

  private conductorView(conductor: ConductorDto): ObjectView {
    const anchor = diagonalToScreen(conductor.x, conductor.y);
    if (conductor.kind === "gate") {
      // The Yard gate: its barrier faces the Yard (the pipe runs on towards it); the plots above and below a Yard face it
      // along y
      const zone = zoneAt(conductor.x, conductor.y);
      const side = zone.kind === "estate" ? sideOf(zone.slot) : null;
      const towardsY = side === "north" || side === "south";
      const art = this.textures.gate("open", towardsY);
      const sprite = new Sprite(art.frames[0]);
      sprite.anchor.set(0, 1);
      const container = new Container();
      container.position.set(anchor.x, anchor.y);
      // The pad is ground. The pipes enter a receiving box above it; the posts stand in front.
      if (art.ground !== undefined) {
        const base = new Sprite(art.ground);
        base.anchor.set(0, 1);
        container.addChild(base);
      }
      const look = this.pipeLook(conductor.x, conductor.y);
      const inlet = this.textures.pipePiece(`gate-${look.shape || "none"}`, conductor.tier);
      if (inlet !== null) {
        const intake = new Sprite(inlet.texture);
        intake.position.set(inlet.x, inlet.y);
        intake.scale.set(inlet.scale);
        container.addChild(intake);
      }
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
    const pipe = this.pipeDisplay(
      conductor.x,
      conductor.y,
      conductor.tier,
      this.pipeLook(conductor.x, conductor.y),
    );
    const look = this.pipeLook(conductor.x, conductor.y);
    if (look.sockets.length === 1) {
      const side = look.sockets[0] as PipeSide;
      const building = this.world.buildingAt(conductor.x + PIPE_DX[side]!, conductor.y + PIPE_DY[side]!);
      const kinds = this.laneKinds(conductor.x + PIPE_DX[side]!, conductor.y + PIPE_DY[side]!);
      if (building !== undefined && kinds.length > 0) {
        const key = connectorKey(conductor.x, conductor.y, building.id);
        const animation = new ConnectorAnimation(
          key,
          conductor.x,
          conductor.y,
          side,
          kinds,
          this.callbacks.getState().content,
          this.textures,
          hash01(conductor.x * 73856093 + conductor.y * 19349663 + building.id),
        );
        animation.setFlows(this.connectorFlows.get(key) ?? []);
        (pipe.getChildByLabel("lanes") ?? pipe).addChild(animation.container);
        this.connectorViews.add(animation);
      }
    }
    pipe.zIndex = (conductor.x + conductor.y) * 4 + 2;
    this.objectLayer.addChild(pipe);
    return { display: pipe, effects: null, building: null, machine: null, idle: null, progress: null };
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
            // A valve or toll meter is drawn in a pipe along x: mirrored when its pipe runs along y
            if (
              IN_PIPE_KINDS.has(definition?.kind ?? "") &&
              (this.world.conductorAt(building.x, building.y - 1) !== undefined ||
                this.world.conductorAt(building.x, building.y + 1) !== undefined) &&
              this.world.conductorAt(building.x - 1, building.y) === undefined &&
              this.world.conductorAt(building.x + 1, building.y) === undefined
            ) {
              sprite.scale.x *= -1;
            }
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
      // Its level at a glance (GDD §17.3): a badge with the level in the colour of its tier, from Upgraded (grey)
      // to Mythic (gold); from Fine (level 10) a rim around its footprint in that colour, an Epic one flies the
      // owner's pennant, a Mythic one glows
      const attachments = [...art.attachments];
      const tier = levelTier(building.level);
      if (tier !== null) {
        const index = tierIndex(building.level);
        if (index >= 1) {
          const rim = (width: number, color: number, alpha: number) =>
            new Graphics()
              .moveTo(0, (-TILE_HEIGHT / 2) * size)
              .lineTo(TILE_WIDTH * size, -TILE_HEIGHT * size)
              .lineTo(TILE_WIDTH * 2 * size, (-TILE_HEIGHT / 2) * size)
              .lineTo(TILE_WIDTH * size, 0)
              .closePath()
              .stroke({ color, width, alpha, join: "round" });
          container.addChildAt(rim(10 + index * 2, tier.light, 0.4), 0);
          container.addChildAt(rim(6, tier.color, 0.95), 1);
        }
        const badge = new Sprite(levelBadge(building.level));
        badge.anchor.set(0.5, 1);
        badge.scale.set(size === 1 ? 0.85 : size === 2 ? 1.15 : 1.4);
        badge.position.set(TILE_WIDTH * size, -6);
        container.addChild(badge);
        // Into a new tier since it was drawn last: a ring in the new colour
        const before = this.shownTiers.get(building.id);
        if (before !== undefined && index > before && !this.far) {
          const center = tileCenter(building.x + (size - 1) / 2, building.y + (size - 1) / 2);
          this.juice.ring(center.x, center.y, tier.color, size);
        }
        this.shownTiers.set(building.id, index);
        if (building.level >= 50) {
          // The owner's pennant on a pole at the pad's left corner
          const pole = new Graphics()
            .moveTo(TILE_WIDTH * 0.35, -TILE_HEIGHT * size * 0.5)
            .lineTo(TILE_WIDTH * 0.35, -TILE_HEIGHT * size * 0.5 - 150)
            .stroke({ color: 0x3a2a12, width: 5 });
          container.addChild(pole);
          attachments.push({
            kind: "flag",
            x: TILE_WIDTH * 0.35,
            y: -TILE_HEIGHT * size * 0.5 - 150,
            size: 1.4,
          });
        }
        if (building.level >= 100) {
          attachments.push(
            {
              kind: "glow",
              x: TILE_WIDTH * size,
              y: -TILE_HEIGHT * size * 0.9,
              size: 2.2 * size,
              color: "#ffd75a",
            },
            {
              kind: "spark",
              x: TILE_WIDTH * size,
              y: -TILE_HEIGHT * size * 1.2,
              size: 1.2,
              color: "#fff3b0",
            },
          );
        }
      }
      if (attachments.length > 0) {
        view.effects = new BuildingEffects(attachments, owner?.ownerColor ?? "#e74c3c");
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
        // With no pipe at all it shows a pipe, not "zz" (run 2: Ben's new Solar Panel "looks broken")
        const pipe = new Graphics();
        pipe.roundRect(-22, -11, 44, 14, 6).fill({ color: 0x8d97a3 }).stroke({ color: 0x5b636c, width: 2 });
        pipe.rect(-4, -14, 8, 20).fill({ color: 0xc9963a });
        pipe.label = "needs-pipe";
        pipe.visible = false;
        icon.label = "waits";
        badge.addChild(bubble, icon, pipe);
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
      view.idle.visible = !running && !this.photo;
      const unpiped = building.network === null;
      const pipe = view.idle.getChildByLabel("needs-pipe");
      const waits = view.idle.getChildByLabel("waits");
      if (pipe !== null && waits !== null) {
        pipe.visible = unpiped;
        waits.visible = !unpiped;
      }
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
    const area = this.camera.viewRect(this.app.screen.width, this.app.screen.height);
    const alpha = this.laneAlpha() * (this.overlay === "none" ? 1 : 0.35);
    let audible: ConnectorAnimation | undefined;
    for (const connector of this.connectorViews) {
      const point = tileCenter(connector.x, connector.y);
      const visible =
        point.x >= area.left - 192 &&
        point.x <= area.right + 192 &&
        point.y >= area.top - 150 &&
        point.y <= area.bottom + 100;
      connector.update(this.time, visible ? alpha : 0, this.reducedMotion?.matches === true);
      if (visible && connector.active && this.hovered?.x === connector.x && this.hovered.y === connector.y)
        audible = connector;
    }
    // A quiet inspection sound, bounded for the whole map and governed by the existing mute control.
    if (
      audible !== undefined &&
      this.pointerInside &&
      !this.dragging &&
      this.callbacks.getState().tool.kind === "select" &&
      this.camera.scale > 0.75 &&
      this.time > this.connectorSoundAt &&
      !this.reducedMotion?.matches &&
      document.visibilityState === "visible"
    ) {
      const kinds = audible.activeKinds;
      if (kinds.includes("fluid") && (!kinds.includes("power") || Math.floor(this.time / 3) % 2 === 1))
        sounds.water();
      else if (kinds.includes("power")) sounds.electric();
      this.connectorSoundAt = this.time + 3;
    }
    const now = Date.now() + this.callbacks.getState().serverOffsetMs;
    const content = this.callbacks.getState().content;
    for (const view of this.effects) {
      // A view whose display went with its chunk (or a redrawn hub) no longer animates
      if (view.display.destroyed) {
        this.effects.delete(view);
        continue;
      }
      view.effects?.update(deltaSeconds, this.time);
      if (view.idle !== null && view.idle.visible) {
        view.idle.alpha = 0.6 + Math.sin(this.time * 2) * 0.3;
        view.idle.y = -TILE_HEIGHT * (view.building?.size ?? 1) - 40 - Math.sin(this.time * 1.5) * 6;
      }
      if (view.progress !== null && view.building?.constructionEndsAt) {
        // A scaffold waiting for its materials fills in gold with them (after run 3, step 2); a building site by its clock
        const site = view.building.siteShare;
        const total = (content.building(view.building.type)?.buildSeconds ?? 60) * 1000;
        const left = new Date(view.building.constructionEndsAt).getTime() - now;
        const done = site != null ? site : Math.max(0, Math.min(1, 1 - left / total));
        view.progress.clear();
        view.progress.roundRect(0, 0, 120, 16, 8).fill({ color: 0x17181c, alpha: 0.85 });
        view.progress.roundRect(3, 3, 114 * done, 10, 5).fill({ color: site != null ? 0xf2c14e : 0x2ec4a6 });
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

  /** How much of the lane connectors shows at this zoom: all of them close up, none far out */
  private laneAlpha(): number {
    return Math.max(0, Math.min(1, (this.camera.scale - 0.22) / 0.18));
  }

  private fadeLanes() {
    for (const connector of this.connectorViews)
      if (connector.container.destroyed) this.connectorViews.delete(connector);
    if (Math.abs(this.camera.scale - this.laneScale) < 0.005) {
      return;
    }
    this.laneScale = this.camera.scale;
    const alpha = this.laneAlpha();
    for (const sprite of this.laneSprites) {
      if (sprite.destroyed) {
        this.laneSprites.delete(sprite);
      } else {
        sprite.alpha = alpha;
      }
    }
  }

  // ---------------------------------------------------------------- Look inside

  /**
   * Look inside (1.4 §9): hovering a pipe with the select tool, or holding Alt, turns it see-through like the cutaway:
   * a glass tube along the way of each flow in it, and in it a core in the good's colour at its lane's height (a belt
   * low, the slim pipe in the middle, the cables high), thicker the fuller the pair's pipe is
   */
  private drawInside() {
    const state = this.callbacks.getState();
    const alt = this.keys.has("alt");
    const tile = this.hovered;
    const onPipe =
      !alt &&
      tile !== null &&
      state.tool.kind === "select" &&
      this.world.conductorAt(tile.x, tile.y)?.kind === "pipe" &&
      this.world.buildingAt(tile.x, tile.y) === undefined;
    const key = this.far
      ? ""
      : alt
        ? `alt:${this.flowsVersion}`
        : onPipe
          ? `${tile.x},${tile.y}:${this.flowsVersion}`
          : "";
    if (key === this.insideKey) {
      return;
    }
    this.insideKey = key;
    const g = this.insideLayer;
    g.clear();
    if (key === "" || state.content === null) {
      return;
    }
    const content = state.content;
    let shown: Array<{
      points: Array<{ x: number; y: number }>;
      flow: FlowDto;
      kind: LaneKind;
      full: number;
    }>;
    if (alt) {
      const full = new Map<number, (flow: FlowDto) => number>();
      shown = this.flowPaths.slice(0, 600).map(({ flow, network, points }) => {
        let of = full.get(network);
        if (of === undefined) {
          const flows = this.world.flows.get(network);
          of = flows !== undefined ? fullness(this.world, content, flows) : () => 0;
          full.set(network, of);
        }
        return { points, flow, kind: laneOf(content, flow.resource), full: of(flow) };
      });
    } else {
      const inside = new Map<FlowDto, InsideFlow>(
        insidePipe(this.world, content, tile!.x, tile!.y).map((t) => [t.flow, t]),
      );
      shown = this.flowPaths
        .filter((t) => inside.has(t.flow))
        .map((t) => ({
          points: t.points,
          flow: t.flow,
          kind: inside.get(t.flow)!.kind,
          full: inside.get(t.flow)!.full,
        }));
    }
    const lift = this.textures.pipeHeight;
    const trace = (points: Array<{ x: number; y: number }>, dy: number) => {
      g.moveTo(points[0]!.x, points[0]!.y + dy);
      for (const point of points.slice(1)) {
        g.lineTo(point.x, point.y + dy);
      }
    };
    // The tube cut open: its glass rim, then the dark inside the lanes run in
    for (const { points } of shown) {
      if (points.length >= 2) {
        trace(points, 0);
        g.stroke({ width: lift * 0.95, color: 0xe8f7ff, alpha: 0.4, cap: "round", join: "round" });
      }
    }
    for (const { points } of shown) {
      if (points.length >= 2) {
        trace(points, 0);
        g.stroke({ width: lift * 0.72, color: 0x1f2b33, alpha: 0.62, cap: "round", join: "round" });
      }
    }
    for (const { points, flow, kind, full } of shown) {
      if (points.length >= 2) {
        trace(points, (0.86 - LANE_HEIGHT[kind]) * lift * 0.6);
        g.stroke({
          width: 3 + 9 * full,
          color: content.resourceColor(flow.resource),
          alpha: 0.9,
          cap: "round",
          join: "round",
        });
      }
    }
  }

  // ---------------------------------------------------------------- Flows

  private buildFlows() {
    for (const dot of this.flowDots) {
      dot.sprite.destroy();
    }
    this.flowDots = [];
    this.flowPaths = [];
    this.flowsVersion++;
    const content = this.callbacks.getState().content;
    this.connectorFlows = connectorTraffic(this.world.flows.values());
    for (const id of this.dynamicLanes.keys())
      if (!this.world.buildings.has(id)) this.dynamicLanes.delete(id);
    for (const connector of this.connectorViews) {
      if (!connector.container.destroyed) connector.setFlows(this.connectorFlows.get(connector.key) ?? []);
    }
    // Storage and Trading Post ports reflect their actual traffic, including its first arrival and last departure.
    for (const building of this.world.buildings.values()) {
      if (building.type !== "storage" && building.type !== "tradingPost") continue;
      const kinds = this.laneKinds(building.x, building.y).join(",");
      if (this.dynamicLanes.get(building.id) === kinds) continue;
      this.dynamicLanes.set(building.id, kinds);
      for (let i = 0; i < building.size; i++)
        for (const [x, y] of [
          [building.x + i, building.y - 1],
          [building.x + i, building.y + building.size],
          [building.x - 1, building.y + i],
          [building.x + building.size, building.y + i],
        ]) {
          const chunk = chunkOf(x!, y!);
          this.dirtyChunks.add(chunkKey(chunk.x, chunk.y));
        }
    }
    for (const network of this.world.flows.values()) {
      const sharing = Math.max(1, network.flows.filter((t) => t.path.length >= 2 && t.delivered > 0).length);
      for (const flow of network.flows) {
        if (flow.path.length < 2 || flow.delivered <= 0) {
          continue;
        }
        // The path in pieces: a Cargo Depot route jumps far (trucks, no dots on the way)
        const tiles: Array<Array<{ x: number; y: number }>> = [[]];
        for (let i = 0; i < flow.path.length; i += 2) {
          const x = flow.path[i]!;
          const y = flow.path[i + 1]!;
          if (i >= 2 && Math.max(Math.abs(x - flow.path[i - 2]!), Math.abs(y - flow.path[i - 1]!)) > 9) {
            tiles.push([]);
          }
          tiles[tiles.length - 1]!.push({ x, y });
        }
        const from = this.world.buildings.get(flow.from);
        const to = this.world.buildings.get(flow.to);
        const pieces = tiles.map((run, index) =>
          this.flowPoints(
            run,
            index === 0 ? from : undefined,
            index === tiles.length - 1 ? to : undefined,
            flow.resource,
          ),
        );
        const color = content.resourceColor(flow.resource);
        for (const points of pieces) {
          this.flowPaths.push({ flow, network: network.id, points });
          let length = 0;
          for (let i = 1; i < points.length; i++) {
            length += Math.hypot(points[i]!.x - points[i - 1]!.x, points[i]!.y - points[i - 1]!.y);
          }
          if (length <= 0) {
            continue;
          }
          // Glowing pellets riding in the pipe: more for bigger flows, fewer where many flows share the pipes, spaced so
          // the pipe stays visible
          const spacing = Math.max(150, 420 / Math.log2(2 + flow.delivered)) * Math.sqrt(sharing);
          const count = Math.max(1, Math.min(20, Math.floor(length / spacing)));
          for (let i = 0; i < count; i++) {
            const sprite = new Sprite(pelletTexture());
            sprite.anchor.set(0.5);
            sprite.scale.set(flow.kind === "sale" || flow.kind === "trade" ? 0.66 : 0.56);
            sprite.tint = color;
            this.flowLayer.addChild(sprite);
            this.flowDots.push({ sprite, points, length, offset: (length / count) * i, speed: 190 });
          }
        }
      }
    }
  }

  /** Every few seconds your trading posts on screen pop what they earned meanwhile */
  private popCoins(deltaSeconds: number) {
    this.coinTimer -= deltaSeconds;
    if (this.coinTimer > 0) {
      return;
    }
    const period = 3;
    this.coinTimer = period;
    const company = this.callbacks.getState().company;
    if (company === null || this.far) {
      return;
    }
    const earned = new Map<number, number>();
    for (const network of this.world.flows.values()) {
      for (const flow of network.flows) {
        if (flow.kind === "sale" || flow.kind === "trade") {
          const seller = this.world.buildings.get(flow.from);
          // Sales pop at the trading post, trades at the building that sold
          const at = flow.kind === "sale" ? flow.to : flow.from;
          if (seller?.owner === company.id) {
            earned.set(at, (earned.get(at) ?? 0) + (flow.delivered * flow.price * period) / 60);
          }
        }
      }
    }
    const { width, height } = this.app.screen;
    const view = this.camera.viewRect(width, height);
    let popped = false;
    for (const [id, amount] of earned) {
      const building = this.world.buildings.get(id);
      if (building === undefined || amount < 1) {
        continue;
      }
      const top = tileCenter(building.x + (building.size - 1) / 2, building.y + (building.size - 1) / 2);
      if (top.x < view.left || top.x > view.right || top.y < view.top || top.y > view.bottom) {
        continue;
      }
      this.juice.coinPop(top.x, top.y - TILE_HEIGHT * building.size - 40, amount);
      popped = true;
    }
    if (popped) {
      sounds.coin();
    }
  }

  private photo = false;

  /** Photo mode: no idle bubbles and no labels on the map */
  setPhotoMode(on: boolean) {
    this.photo = on;
    this.labelLayer.visible = !on;
    for (const view of this.effects) {
      if (view.idle !== null && on) {
        view.idle.visible = false;
      }
    }
    if (!on) {
      this.invalidate();
    }
  }

  /** Photo mode's time of day: a colour grade over the world */
  setTimeOfDay(time: "day" | "golden" | "dusk" | "night") {
    if (time === "day") {
      this.worldLayer.filters = [];
      return;
    }
    const filter = new ColorMatrixFilter();
    if (time === "golden") {
      filter.tint(0xffd9a0, false);
      filter.saturate(0.15, true);
      filter.brightness(1.05, true);
    } else if (time === "dusk") {
      filter.tint(0xd8a0c8, false);
      filter.brightness(0.82, true);
    } else {
      // Moonlight: darker, cooler, a little less colour; warm lights still read
      filter.matrix = [
        0.5, 0.06, 0.04, 0, 0.02, 0.04, 0.55, 0.06, 0, 0.03, 0.05, 0.1, 0.72, 0, 0.08, 0, 0, 0, 1, 0,
      ];
    }
    this.worldLayer.filters = [filter];
  }

  /** The world as a picture (photo mode): a PNG data URL of what is on screen */
  async snapshot(): Promise<string> {
    return this.app.renderer.extract.base64({ target: this.app.stage, format: "png" });
  }

  /** A building is done: a burst and a little sound */
  celebrate(buildingId: number) {
    const building = this.world.buildings.get(buildingId);
    if (building === undefined || this.far) {
      return;
    }
    const center = tileCenter(building.x + (building.size - 1) / 2, building.y + (building.size - 1) / 2);
    this.juice.burst(center.x, center.y - 30, building.size);
    sounds.built();
  }

  /** An upgrade: a twinkle */
  twinkle(buildingId: number) {
    const building = this.world.buildings.get(buildingId);
    if (building !== undefined) {
      const center = tileCenter(building.x + (building.size - 1) / 2, building.y + (building.size - 1) / 2);
      this.juice.sparkle(center.x, center.y);
    }
  }

  /**
   * Where the dots of a flow run along pipe tiles (ground coordinates: a tile's centre is x + 0.5): through the
   * middle of straight pieces, around the bends like the pipes, from and to the socket of a building next to the ends;
   * into a building with lanes along its good's lane (1.4 §9: on the belt, through the slim pipe, along the cables)
   */
  private flowPoints(
    run: Array<{ x: number; y: number }>,
    from: BuildingDto | undefined,
    to: BuildingDto | undefined,
    resource?: string,
  ): Array<{ x: number; y: number }> {
    const lift = this.textures.pipeHeight;
    const screen = (gx: number, gy: number, height = 1) => {
      const c = tileCenter(gx - 0.5, gy - 0.5);
      return { x: c.x, y: c.y - lift * height };
    };
    const content = this.callbacks.getState().content;
    const kind = (resource !== undefined ? content.resource(resource)?.kind : undefined) as
      LaneKind | undefined;
    // The edge of the end tile toward the building (its socket, or its good's lane and the manifold it leaves from,
    // building first), or the building's middle when it isn't next to it
    const socket = (tile: { x: number; y: number }, building: BuildingDto) => {
      for (const [side, dx, dy] of PIPE_SIDES) {
        const nx = tile.x + dx;
        const ny = tile.y + dy;
        if (
          nx >= building.x &&
          ny >= building.y &&
          nx < building.x + building.size &&
          ny < building.y + building.size
        ) {
          const lanes = this.laneKinds(nx, ny);
          const sockets = this.pipeLook(tile.x, tile.y).sockets;
          const index =
            kind !== undefined && sockets.length === 1 && sockets[0] === side ? lanes.indexOf(kind) : -1;
          if (index >= 0) {
            // The short lane has its own cargo / sparks / fluid animation. Pipe pellets stay inside the big pipe.
            const start = connectorPath(side, LANE_SLOTS[lanes.length]![index]!, kind!)[0]!;
            const anchor = diagonalToScreen(tile.x, tile.y);
            return [{ x: anchor.x + start.x, y: anchor.y + start.y }];
          }
          return [screen(tile.x + 0.5 + dx / 2, tile.y + 0.5 + dy / 2)];
        }
      }
      return [this.buildingCenter(building)];
    };
    const points: Array<{ x: number; y: number }> = [];
    if (from !== undefined && run.length > 0) {
      points.push(...socket(run[0]!, from));
    }
    run.forEach((tile, i) => {
      const before = run[i - 1];
      const after = run[i + 1];
      if (
        before !== undefined &&
        after !== undefined &&
        before.x !== after.x &&
        before.y !== after.y &&
        this.pipeLook(tile.x, tile.y).shape.length === 2
      ) {
        // A bend: around the corner between the two edges, a quarter circle of half a tile
        const cx = tile.x + 0.5 + (before.x - tile.x + after.x - tile.x) / 2;
        const cy = tile.y + 0.5 + (before.y - tile.y + after.y - tile.y) / 2;
        const a0 = Math.atan2((tile.y + before.y) / 2 + 0.5 - cy, (tile.x + before.x) / 2 + 0.5 - cx);
        let a1 = Math.atan2((tile.y + after.y) / 2 + 0.5 - cy, (tile.x + after.x) / 2 + 0.5 - cx);
        if (a1 - a0 > Math.PI) {
          a1 -= 2 * Math.PI;
        } else if (a0 - a1 > Math.PI) {
          a1 += 2 * Math.PI;
        }
        for (let k = 0; k <= 4; k++) {
          const a = a0 + ((a1 - a0) * k) / 4;
          points.push(screen(cx + 0.5 * Math.cos(a), cy + 0.5 * Math.sin(a)));
        }
      } else {
        // Inlet manifolds and junctions meet at the middle, even when this flow turns there.
        points.push(screen(tile.x + 0.5, tile.y + 0.5));
      }
    });
    if (to !== undefined && run.length > 0) {
      points.push(...socket(run[run.length - 1]!, to).reverse());
    }
    return points;
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
      view.lines.clear();
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
        this.lineLayer.addChild(view.lines);
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
          g.fill({ color, alpha: 0.12 });
        }
        traceFootprint(g, estate.x, estate.y, ESTATE_SIZE);
        g.stroke({ color, width: far ? (mine ? 40 : 26) : mine ? 7 : 5, alpha: far ? 0.85 : 0.75 });
      } else {
        traceFootprint(g, estate.x, estate.y, ESTATE_SIZE);
        g.stroke({ color: 0xffffff, width: far ? 14 : 3, alpha: 0.22 });
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
      // Free plots are only named up close, far away the map would be full of labels
      if (estate.ownerName !== null || (!far && this.camera.scale < 0.3)) {
        const text = estate.ownerName ?? "Free plot";
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
      // The race between towns (1.4 §13): its Era, its Launch, its stars
      const name = new Text({
        text: `${district.name}\nEra ${district.era ?? 1}${district.launched ? ` · 🚀${district.stars > 0 ? ` ${district.stars} ★` : ""}` : ""}`,
        style: {
          fontFamily: "Georgia, serif",
          align: "center",
          fontSize: 44,
          fontWeight: "700",
          fill: "#f6c744",
          stroke: { color: "#0b1d24", width: 9 },
        },
      });
      name.anchor.set(0.5);
      const center = tileCenter(
        districtOriginX(district.x) + DISTRICT_WIDTH / 2,
        districtOriginY(district.y) + DISTRICT_HEIGHT / 2,
      );
      name.position.set(center.x, center.y);
      name.label = "district";
      labels.addChild(name);
    } else {
      // The hub city
      const hubX = district.originX + HUB_LOCAL.x0;
      const hubY = district.originY + HUB_LOCAL.y0;
      for (const structure of district.hub.filter((t) => t.type !== "townGate")) {
        const art = this.textures.hub(structure.type, structure.size, structure);
        const sprite = new Sprite(art.frames[0]);
        const x = hubX + structure.x;
        const y = hubY + structure.y;
        const anchor = footprintAnchor(x, y, structure.size);
        const container = new Container();
        container.position.set(anchor.x, anchor.y);
        placeArt(sprite, art, { x: 0, y: 0 }, structure.size);
        container.addChild(sprite);
        if (structure.type === "statue" && structure.label !== null) {
          // A Diamond builder's statue (1.4 §13): a sash in their colour on the plinth, their name on a plaque
          const sash = new Graphics()
            .roundRect(TILE_WIDTH - 26, -TILE_HEIGHT * 0.9, 52, 12, 4)
            .fill({ color: structure.color ?? "#c9a227" })
            .stroke({ color: 0x2a1c10, width: 2 });
          const plaque = new Text({
            text: structure.label,
            style: {
              fontFamily: "Georgia, serif",
              fontSize: 18,
              fontWeight: "700",
              fill: "#fff4d6",
              stroke: { color: "#2a1c10", width: 5 },
            },
          });
          // Over the trees and houses in front of it
          plaque.anchor.set(0.5, 0);
          plaque.position.set(anchor.x + TILE_WIDTH, anchor.y + 4);
          container.addChild(sash);
          labels.addChild(plaque);
        }
        // Wonders and the stations' projects rise stage by stage
        const wonder =
          structure.type === "wonderSite" ||
          structure.type === "eraWonderSite" ||
          structure.type.startsWith("project-");
        if (
          wonder &&
          art.placement !== undefined &&
          structure.stages > 0 &&
          structure.level < structure.stages
        ) {
          // A Wonder rises stage by stage: the finished part solid, the rest a faint plan with scaffolding
          sprite.alpha = 0.42;
          sprite.tint = 0x9ec4e8;
          const done = (structure.level + 0.25) / (structure.stages + 0.25);
          const source = sprite.texture;
          const visible = Math.max(1, Math.round(source.height * Math.min(1, 0.18 + done * 0.82)));
          const solid = new Sprite(
            new Texture({
              source: source.source,
              frame: new Rectangle(
                source.frame.x,
                source.frame.y + source.height - visible,
                source.width,
                visible,
              ),
            }),
          );
          solid.anchor.set(
            art.placement.anchorX,
            (art.placement.anchorY * source.height - (source.height - visible)) / visible,
          );
          solid.scale.set(art.placement.scale);
          solid.position.copyFrom(sprite.position);
          container.addChild(solid);
          const scaffold = new Graphics();
          const topY = -(visible * art.placement.scale) + 10;
          const w = TILE_WIDTH * structure.size * 0.55;
          for (let i = 0; i <= 6; i++) {
            const px = TILE_WIDTH * structure.size - w + (2 * w * i) / 6;
            scaffold.moveTo(px, topY + 40).lineTo(px, topY - 70);
          }
          scaffold
            .moveTo(TILE_WIDTH * structure.size - w, topY - 30)
            .lineTo(TILE_WIDTH * structure.size + w, topY - 30);
          scaffold
            .moveTo(TILE_WIDTH * structure.size - w, topY - 70)
            .lineTo(TILE_WIDTH * structure.size + w, topY - 70);
          scaffold.stroke({ color: 0xc9963a, width: 5, alpha: 0.9 });
          container.addChild(scaffold);
        }
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
        container.zIndex = (x + y + 2 * (structure.size - 1)) * 4 + 3;
        this.objectLayer.addChild(container);
        view.hub.push(hub);
      }
    }
    this.drawTownParts(view, district);
  }

  /**
   * A town's own places (1.4 §4): each Yard with its crew's banner and name (a lock when it's closed), the town gates
   * where the roads leave the town, and up close the outskirts set aside for the plots (they open with Era 2)
   */
  private drawTownParts(view: DistrictView, district: DistrictDto) {
    const g = view.graphics;
    const far = this.far;
    const ox = districtOriginX(district.x);
    const oy = districtOriginY(district.y);
    for (const yard of district.yards ?? []) {
      const rect = yardRect(district.x, district.y, yard.index);
      const width = rect.x1 - rect.x0 + 1;
      const height = rect.y1 - rect.y0 + 1;
      if (yard.color !== null) {
        // The crew's banner colours around its Yard
        traceFootprint(g, rect.x0, rect.y0, width, height);
        g.stroke({ color: yard.color, width: far ? 22 : 6, alpha: 0.85 });
        traceFootprint(g, rect.x0 + 1, rect.y0 + 1, width - 2, height - 2);
        g.stroke({ color: yard.color2 ?? yard.color, width: far ? 12 : 4, alpha: 0.7 });
      }
      if (yard.emblem === null && !far) {
        continue;
      }
      const label = new Text({
        text: `${yard.emblem !== null ? `${emblemIcon(yard.emblem)} ` : ""}${yard.name}${yard.open ? "" : "  🔒"}`,
        style: {
          fontFamily: "Georgia, serif",
          fontSize: 32,
          fontWeight: "700",
          fill: yard.emblem !== null ? "#fff4d6" : "#e8dcc0",
          stroke: { color: "#2a1c10", width: 8 },
        },
      });
      label.anchor.set(0.5);
      const center = tileCenter(rect.x0 + width / 2 - 0.5, rect.y0 + height / 2 - 0.5);
      label.position.set(center.x, center.y);
      label.label = "yard";
      view.labels.addChild(label);
    }
    if (!far) {
      for (const yard of district.yards ?? []) {
        for (const stall of yard.stalls) {
          this.drawStall(view, district, stall);
        }
        // The Yard's Monuments on their sites, bigger as their stages grow (1.4 §13)
        const rect = yardRect(district.x, district.y, yard.index);
        for (const monument of yard.monuments ?? []) {
          const site = MONUMENT_SITES[monument.era - 1];
          const look = monument.stages >= 10 ? 3 : monument.stages >= 3 ? 2 : 1;
          const art = this.textures.generated(`monument-${monument.kind}-${look}`, 3);
          if (site === undefined || art === null) {
            continue;
          }
          const sprite = new Sprite(art.frames[0]);
          placeArt(sprite, art, footprintAnchor(rect.x0 + site.x, rect.y0 + site.y, 3), 3);
          sprite.zIndex = (rect.x0 + site.x + rect.y0 + site.y + 3) * 4 + 3;
          this.objectLayer.addChild(sprite);
          view.hub.push({
            display: sprite,
            effects: null,
            building: null,
            machine: null,
            idle: null,
            progress: null,
          });
        }
      }
    }
    // The trunk lines that leave the town (1.4 §20): a big pipe through the wilderness in its tier's colour, its
    // carrier's colour down the middle
    const lines = view.lines;
    for (const line of district.lines ?? []) {
      const points: Array<{ x: number; y: number }> = [];
      for (let i = 0; i + 1 < line.route.length; i += 2) {
        points.push(tileCenter(line.route[i]!, line.route[i + 1]!));
      }
      if (points.length < 2) {
        continue;
      }
      lines.moveTo(points[0]!.x, points[0]!.y);
      points.slice(1).forEach((p) => lines.lineTo(p.x, p.y));
      lines.stroke({ color: PIPE_COLORS[line.tier] ?? "#8f969e", width: far ? 40 : 28, alpha: 0.95 });
      lines.moveTo(points[0]!.x, points[0]!.y);
      points.slice(1).forEach((p) => lines.lineTo(p.x, p.y));
      lines.stroke({ color: line.color, width: far ? 10 : 6, alpha: 0.9 });
    }
    const tower = this.textures.generated("hub-gateTower", 2);
    for (const gate of TOWN_GATES_LOCAL) {
      // The town gate: a paved apron where the road leaves the town, a gate tower on each side of the road
      traceFootprint(g, ox + gate.x0, oy + gate.y0, gate.x1 - gate.x0 + 1, gate.y1 - gate.y0 + 1);
      g.fill({ color: 0xcdb48a, alpha: far ? 0.6 : 0.45 });
      traceFootprint(g, ox + gate.x0, oy + gate.y0, gate.x1 - gate.x0 + 1, gate.y1 - gate.y0 + 1);
      g.stroke({ color: 0x5a4630, width: far ? 10 : 4, alpha: 0.8 });
      if (far || tower === null) {
        continue;
      }
      const across = gate.x1 - gate.x0 > gate.y1 - gate.y0;
      for (const [x, y] of across
        ? [
            [gate.x0, gate.y0],
            [gate.x1 - 1, gate.y0],
          ]
        : [
            [gate.x0, gate.y0],
            [gate.x0, gate.y1 - 1],
          ]) {
        const sprite = new Sprite(tower.frames[0]);
        placeArt(sprite, tower, footprintAnchor(ox + x!, oy + y!, 2), 2);
        sprite.zIndex = (ox + x! + oy + y! + 2) * 4 + 3;
        this.objectLayer.addChild(sprite);
        view.hub.push({
          display: sprite,
          effects: null,
          building: null,
          machine: null,
          idle: null,
          progress: null,
        });
      }
    }
    if (far) {
      return;
    }
    for (const estate of district.estates) {
      outskirtsParcels(estate.slot).forEach((parcel, index) => {
        // A bought one is the owner's land: drawn like it (1.4 §5)
        const bought = (estate.outskirtsParcels & (1 << index)) !== 0;
        traceFootprint(g, ox + parcel.x, oy + parcel.y, PARCEL_SIZE);
        g.stroke({
          color: estate.ownerColor ?? 0xfff2c8,
          width: bought ? 3 : 2,
          alpha: bought ? 0.85 : estate.ownerId !== null ? 0.3 : 0.14,
        });
      });
    }
  }

  /**
   * A resident's stall at its Yard gate (1.4 §7): a market stall, and over it a sign with what it sells the crew (a dot in
   * each good's colour), its crew price and a red ring for each good it wants
   */
  private drawStall(view: DistrictView, district: DistrictDto, stall: StallDto) {
    const content = this.callbacks.getState().content;
    const origin = estateOrigin(district.x, district.y, stall.slot);
    const local = stallLocal(stall.slot);
    const x = origin.x + local.x;
    const y = origin.y + local.y;
    const art = this.textures.generated("town-stall", 1);
    if (art !== null) {
      const sprite = new Sprite(art.frames[0]);
      placeArt(sprite, art, footprintAnchor(x, y, 1), 1);
      sprite.zIndex = (x + y) * 4 + 3;
      this.objectLayer.addChild(sprite);
      view.hub.push({
        display: sprite,
        effects: null,
        building: null,
        machine: null,
        idle: null,
        progress: null,
      });
    }
    const sign = new Container();
    const price = new Text({
      text: `${stall.crewPrice.toFixed(2).replace(/0$/, "")}×`,
      style: { fontFamily: "system-ui", fontSize: 34, fontWeight: "800", fill: "#3b2a14" },
    });
    const dots = stall.sells.length + stall.wants.length;
    const width = 24 + dots * 32 + (dots > 0 ? 8 : 0) + price.width;
    const board = new Graphics()
      .roundRect(-width / 2, -27, width, 54, 14)
      .fill({ color: 0xfff4d6, alpha: 0.95 })
      .stroke({ color: 0x5a4630, width: 4 });
    sign.addChild(board);
    let cursor = -width / 2 + 24;
    for (const good of stall.sells) {
      board
        .circle(cursor, 0, 12)
        .fill({ color: content.resourceColor(good) })
        .stroke({ color: 0x2b1d0e, width: 3 });
      cursor += 32;
    }
    for (let i = 0; i < stall.wants.length; i++) {
      // A good it wants: a hollow ring
      board.circle(cursor, 0, 10).stroke({ color: 0xc0392b, width: 5 });
      cursor += 32;
    }
    price.anchor.set(0, 0.5);
    price.position.set(cursor + (dots > 0 ? 8 : 0) - 12, 0);
    sign.addChild(price);
    const top = tileCenter(x, y);
    sign.position.set(top.x, top.y - 170);
    sign.label = "stall";
    view.labels.addChild(sign);
  }

  /** Labels keep a readable size on screen at every zoom */
  private scaleLabels() {
    const scale = this.camera.scale;
    for (const view of this.districtViews.values()) {
      for (const label of view.labels.children) {
        // A town's name, its Yards' names and its companies' names stay readable when the whole town is in view
        const factor =
          label.label === "district"
            ? Math.min(16, 0.9 / scale)
            : label.label === "stall"
              ? Math.min(2.5, Math.max(1, 0.5 / scale))
              : label.label === "yard"
                ? Math.min(14, Math.max(1, 0.6 / scale))
                : Math.min(9, Math.max(1, 0.55 / scale));
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

    const paint = this.dragging && this.movedBeyondClick && this.isPaintDrag() ? this.paintSpots() : null;
    if (paint !== null && tool.kind === "build") {
      this.drawPaintGhost(tool.type, paint);
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
      const just = state.justBuilt;
      if (
        tool.kind === "build" &&
        just !== null &&
        origin.x < just.x + just.size &&
        just.x < origin.x + definition.size &&
        origin.y < just.y + just.size &&
        just.y < origin.y + definition.size
      ) {
        // Over the one just placed: its tip says so, and the ghost waits till the pointer moves on
        this.callbacks.onHover(tile, null);
        return;
      }
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
      this.drawRouteGhost(g, route, tool.tier, state.company);
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

  /** A painted row or area: a ghost on every spot it will build, the left-out ones in red, and the count and cost */
  private drawPaintGhost(
    type: string,
    paint: { spots: TilePosition[]; blocked: TilePosition[]; cost: number; short: boolean },
  ) {
    const state = this.callbacks.getState();
    const definition = state.content.building(type);
    if (definition === undefined) {
      return;
    }
    const size = definition.size;
    const g = new Graphics();
    for (const spot of paint.blocked) {
      traceFootprint(g, spot.x, spot.y, size);
    }
    g.fill({ color: 0xef6f6c, alpha: 0.3 });
    for (const spot of paint.spots) {
      traceFootprint(g, spot.x, spot.y, size);
    }
    g.fill({ color: 0x2ec4a6, alpha: 0.4 });
    for (const spot of paint.spots) {
      traceFootprint(g, spot.x, spot.y, size);
      g.stroke({ color: 0x2ec4a6, width: 4 });
    }
    this.ghostLayer.addChild(g);
    const art = this.textures.building(definition.id, size);
    for (const spot of [...paint.spots].sort((a, b) => a.x + a.y - (b.x + b.y))) {
      const sprite = new Sprite(art.frames[0]);
      placeArt(sprite, art, footprintAnchor(spot.x, spot.y, size), size);
      sprite.alpha = 0.72;
      this.ghostLayer.addChild(sprite);
    }
    const last = paint.spots.at(-1) ?? paint.blocked.at(-1);
    if (last !== undefined) {
      const label = new Text({
        text: `${paint.spots.length} × ${definition.name} · ${Math.round(paint.cost).toLocaleString("en-US")} g${paint.short ? " · no gold for more" : ""}`,
        style: { fill: 0xffffff, fontSize: 30, fontWeight: "700", stroke: { color: 0x1d3b5a, width: 6 } },
      });
      label.anchor.set(0.5, 1);
      const top = tileCenter(last.x + (size - 1) / 2, last.y + (size - 1) / 2);
      label.position.set(top.x, top.y - 90);
      this.ghostLayer.addChild(label);
    }
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
    const sprites: Container[] = [];
    // The pipes as they will look: connected to each other and plugged into the blueprint's buildings
    const pipes = new Set(blueprint.pipes.map((t) => tileKey(t.dx, t.dy)));
    const plugs = new Set<number>();
    for (const item of blueprint.buildings) {
      const definition = state.content.building(item.type);
      if (!takesPipes(definition)) {
        continue;
      }
      for (let dy = 0; dy < (definition?.size ?? 1); dy++) {
        for (let dx = 0; dx < (definition?.size ?? 1); dx++) {
          plugs.add(tileKey(item.dx + dx, item.dy + dy));
        }
      }
    }
    for (const pipe of blueprint.pipes) {
      const look = pipeLookOf(
        pipe.dx,
        pipe.dy,
        (x, y) => pipes.has(tileKey(x, y)),
        (x, y) => plugs.has(tileKey(x, y)),
      );
      const ghost = this.pipeDisplay(origin.x + pipe.dx, origin.y + pipe.dy, pipe.tier, look);
      ghost.alpha = 0.7;
      sprites.push(ghost);
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
    this.drawRouteGhost(g, route, tier, company);
    this.ghostLayer.addChildAt(g, 0);
  }

  /**
   * The route's tiles, and the pipes it adds as they will look: joined to each other, to the pipes there and plugged
   * into the buildings along it
   */
  private drawRouteGhost(g: Graphics, route: TilePosition[], tier: number, company: CompanyDto) {
    const onRoute = new Set(route.map((t) => tileKey(t.x, t.y)));
    for (const t of route) {
      const own = this.world.conductorAt(t.x, t.y)?.owner === company.id;
      traceFootprint(g, t.x, t.y, 1);
      g.fill({ color: own ? 0x6cb4ee : 0x2ec4a6, alpha: 0.35 });
      if (!own) {
        const ghost = this.pipeDisplay(
          t.x,
          t.y,
          tier,
          this.pipeLook(t.x, t.y, (x, y) => onRoute.has(tileKey(x, y))),
        );
        ghost.alpha = 0.75;
        ghost.zIndex = (t.x + t.y) * 4 + 2;
        this.ghostLayer.addChild(ghost);
      }
    }
  }

  /**
   * Links between stations: a covered channel along the corridors in the colour of the good (side by side where several
   * share a corridor), yours brighter, and the goods moving along the ones that flow
   */
  private drawLinks(deltaSeconds: number) {
    const state = this.callbacks.getState();
    // Zoomed out the channels get wider, so they stay visible on the far map
    const width = Math.max(1, 0.25 / this.camera.scale);
    const widthStep = Math.round(Math.log2(width) * 2);
    if (
      state.mapLinks !== this.drawnLinks ||
      state.focusLink !== this.drawnFocus ||
      widthStep !== this.drawnWidth
    ) {
      this.drawnLinks = state.mapLinks;
      this.drawnFocus = state.focusLink;
      this.drawnWidth = widthStep;
      const w = 2 ** (widthStep / 2);
      const g = this.linkLayer;
      g.clear();
      this.linkPaths = [];
      // Side by side: each link gets a lane on the segments it shares with others
      const lanes = new Map<string, number>();
      const myId = state.company?.id ?? -1;
      const ordered = [...state.mapLinks].sort((a, b) => a.id - b.id);
      for (const link of ordered) {
        const corners: Array<{ x: number; y: number }> = [];
        for (let i = 0; i + 1 < link.route.length; i += 2) {
          corners.push({ x: link.route[i]!, y: link.route[i + 1]! });
        }
        const points: Array<{ x: number; y: number }> = [];
        corners.forEach((c, i) => {
          // Corridor corners are the first tile of a two-wide corridor: the channel runs in its middle, in its own lane.
          // The gates are tiles of their own (their middle), and the turn under the Yard keeps its gate's line.
          const last = corners.length - 1;
          const gate = i <= 1 ? corners[0]! : i >= last - 1 ? corners[last]! : null;
          const next = corners[Math.min(last, i + 1)]!;
          const before = corners[Math.max(0, i - 1)]!;
          const along = c.x === next.x && c.y !== next.y ? next : before;
          const key = `${Math.min(c.x, along.x)},${Math.min(c.y, along.y)},${Math.max(c.x, along.x)},${Math.max(c.y, along.y)}`;
          const lane = lanes.get(key) ?? 0;
          lanes.set(key, lane + 1);
          const offset = i === 0 || i === last ? 0 : [0, -0.45, 0.45, -0.8, 0.8][lane % 5]!;
          const u = gate !== null && c.x === gate.x ? c.x + 0.5 : c.x + 1 + (c.x === along.x ? offset : 0);
          const v = gate !== null && c.y === gate.y ? c.y + 0.5 : c.y + 1 + (c.y === along.y ? offset : 0);
          const p = diagonalToScreen(u, v);
          points.push({ x: p.x + TILE_WIDTH, y: p.y - TILE_HEIGHT });
        });
        const length = points.reduce(
          (sum, p, i) => (i === 0 ? 0 : sum + Math.hypot(p.x - points[i - 1]!.x, p.y - points[i - 1]!.y)),
          0,
        );
        const mine = link.seller === myId || link.buyer === myId;
        const focus = link.id === state.focusLink;
        const color = state.content.resourceColor(link.resource);
        this.linkPaths.push({ points, length, color, mine: mine && link.flowing });
        const trace = () => points.forEach((p, i) => (i === 0 ? g.moveTo(p.x, p.y) : g.lineTo(p.x, p.y)));
        if (focus) {
          trace();
          g.stroke({ color: 0xffe08a, width: 64 * w, alpha: 0.55, cap: "round", join: "round" });
        }
        // The channel's cover (dark stone), then the good's colour in its slot
        trace();
        g.stroke({
          color: 0x2b2a26,
          width: 34 * w,
          alpha: mine || focus ? 0.85 : 0.45,
          cap: "round",
          join: "round",
        });
        trace();
        g.stroke({ color, width: 14 * w, alpha: mine || focus ? 1 : 0.55, cap: "round", join: "round" });
      }
    }
    // The goods on their way: a few bright beads gliding from seller to buyer
    this.linkTime += deltaSeconds;
    const pulse = this.linkPulse;
    pulse.clear();
    if (this.far) {
      return;
    }
    for (const path of this.linkPaths) {
      if (!path.mine || path.length <= 0) {
        continue;
      }
      const spacing = 420;
      const shift = (this.linkTime * 160) % spacing;
      for (let d = shift; d < path.length; d += spacing) {
        let rest = d;
        for (let i = 1; i < path.points.length; i++) {
          const a = path.points[i - 1]!;
          const b = path.points[i]!;
          const segment = Math.hypot(b.x - a.x, b.y - a.y);
          if (rest <= segment) {
            const t = segment === 0 ? 0 : rest / segment;
            pulse.circle(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, 11);
            break;
          }
          rest -= segment;
        }
      }
      pulse.fill({ color: 0xfff6d8, alpha: 0.95 });
    }
  }

  /**
   * The plots a picker shows: drawn again when they change or the zoom does (the lines keep their width on screen), the
   * free ones' glow pulses
   */
  private drawPicker() {
    const picker = this.callbacks.getState().picker;
    const zoom = Math.round(Math.log(this.camera.scale) * 6);
    const key = picker === null ? "" : `${JSON.stringify(picker)}|${zoom}`;
    if (key !== this.pickKey) {
      this.pickKey = key;
      const glow = this.pickGlow;
      const marks = this.pickMarks;
      glow.clear();
      marks.clear();
      // Pixels on screen, whatever the zoom
      const px = (pixels: number) => pixels / this.camera.scale;
      if (picker !== null) {
        for (const [seat, state] of Object.entries(picker.seats)) {
          const [dx, dy, slot] = seat.split(",").map(Number) as [number, number, number];
          const origin = estateOrigin(dx, dy, slot);
          if (state === "free") {
            // A soft halo, wide and faint, around a bright line, over a light veil
            traceFootprint(glow, origin.x, origin.y, ESTATE_SIZE);
            glow.fill({ color: 0xfffbe0, alpha: 0.16 });
            for (const [width, alpha] of [
              [22, 0.12],
              [12, 0.22],
              [6, 0.45],
            ] as const) {
              traceFootprint(glow, origin.x, origin.y, ESTATE_SIZE);
              glow.stroke({ color: 0xeaffc8, width: px(width), alpha });
            }
            traceFootprint(glow, origin.x, origin.y, ESTATE_SIZE);
            glow.stroke({ color: 0xffffff, width: px(2.5), alpha: 1 });
          } else if (state === "closed") {
            traceFootprint(marks, origin.x, origin.y, ESTATE_SIZE);
            marks.fill({ color: 0xf2b134, alpha: 0.18 });
            traceFootprint(marks, origin.x, origin.y, ESTATE_SIZE);
            marks.stroke({ color: 0xf2b134, width: px(3), alpha: 0.9 });
          }
        }
        const chosen = picker.chosen;
        if (chosen !== null) {
          const origin = estateOrigin(chosen.districtX, chosen.districtY, chosen.slot);
          traceFootprint(marks, origin.x, origin.y, ESTATE_SIZE);
          marks.stroke({ color: 0x2b1d0e, width: px(8), alpha: 0.6 });
          traceFootprint(marks, origin.x, origin.y, ESTATE_SIZE);
          marks.stroke({ color: 0xffd34d, width: px(4), alpha: 1 });
        }
      }
    }
    if (picker !== null) {
      this.pickGlow.alpha = 0.6 + 0.4 * Math.sin(performance.now() / 380);
    }
  }

  private drawHighlights() {
    const g = this.highlightLayer;
    g.clear();
    if (this.far) {
      return;
    }
    const state = this.callbacks.getState();
    if (
      state.tool.kind === "pipe" &&
      this.hovered !== null &&
      this.world.conductorAt(this.hovered.x, this.hovered.y)?.kind === "gate"
    ) {
      traceFootprint(g, this.hovered.x, this.hovered.y, 1);
      g.fill({ color: 0x2ec4a6, alpha: 0.3 });
      g.stroke({ color: 0x2ec4a6, width: 5 });
    }
    if (state.hintTile !== null) {
      // Loady's suggestion: a pulsing golden footprint
      const pulse = 0.5 + 0.5 * Math.sin(performance.now() / 260);
      traceFootprint(g, state.hintTile.x, state.hintTile.y, state.hintTile.size);
      g.fill({ color: 0xf6c744, alpha: 0.18 + 0.22 * pulse });
      // A dark rim under the gold, so it shows on the Wind and Sun overlays too (run 2: "an orange area, not a tile")
      traceFootprint(g, state.hintTile.x, state.hintTile.y, state.hintTile.size);
      g.stroke({ color: 0x2b1d0e, width: 12 + 4 * pulse, alpha: 0.55 });
      traceFootprint(g, state.hintTile.x, state.hintTile.y, state.hintTile.size);
      g.stroke({ color: 0xffe08a, width: 6 + 4 * pulse, alpha: 0.95 });
      // And a golden arrow bobbing over it, so it can't be missed (run 2's replay: Ana didn't see the tile at all)
      const top = tileCenter(
        state.hintTile.x + (state.hintTile.size - 1) / 2,
        state.hintTile.y + (state.hintTile.size - 1) / 2,
      );
      const lift = 70 + 14 * state.hintTile.size + 10 * pulse;
      g.poly([top.x - 26, top.y - lift - 34, top.x + 26, top.y - lift - 34, top.x, top.y - lift])
        .fill({ color: 0xf6c744, alpha: 0.95 })
        .stroke({ color: 0x2b1d0e, width: 5, alpha: 0.7 });
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
    for (const id of state.selectedBuilding !== null
      ? [...state.selection, state.selectedBuilding]
      : state.selection) {
      const building = this.world.buildings.get(id);
      if (building !== undefined) {
        drawSelected(g, building, this.time);
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

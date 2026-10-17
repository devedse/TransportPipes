import { Application, ColorMatrixFilter, Container, type Filter, type Sprite } from "pixi.js";
import type { ChunkData } from "../api/contracts";
import { Camera, chunkIntersectsView } from "./camera";
import { ChunkView } from "./ChunkView";
import { CHUNK_SIZE, CLICK_TOLERANCE_PX, TILE_HEIGHT, TILE_WIDTH } from "./constants";
import { chunkKey, diagonalToScreen, tileToChunk, type TilePosition } from "./isometric";
import { SpriteFactory } from "./SpriteFactory";

export interface GameEngineCallbacks {
  requestChunk(chunkX: number, chunkY: number): Promise<ChunkData>;
  /** False while disconnected: no new chunk requests are made. */
  canRequestChunks(): boolean;
  getCurrentUserId(): number | null;
  onTileClicked(tile: TilePosition): void;
  /** The tile under the pointer, or null when the pointer is not over the map. */
  onHoveredTileChanged(tile: TilePosition | null): void;
  onZoomChanged(scale: number): void;
}

const CHUNK_RETRY_DELAY_MS = 3000;
/** Extra ring of chunks around the view, so tall buildings just below the screen are loaded too. */
const CHUNK_MARGIN = 1;
const GHOST_ALPHA = 0.75;

interface Highlight {
  hovered: boolean;
  selected: boolean;
}

/**
 * The isometric map: camera, chunk streaming, hover/selection highlighting and the build ghost.
 * Framework-agnostic; the React app talks to it through the callbacks and a few methods.
 */
export class GameEngine {
  private readonly app: Application;
  private readonly factory: SpriteFactory;
  private readonly callbacks: GameEngineCallbacks;
  private readonly camera = new Camera();
  private readonly world = new Container();
  private readonly terrainLayer = new Container();
  private readonly objectsLayer = new Container({ sortableChildren: true });
  private readonly ghostLayer = new Container();
  private readonly chunks = new Map<string, ChunkView>();

  private readonly hoverFilter = new ColorMatrixFilter();
  private readonly selectFilter = new ColorMatrixFilter();
  private highlights = new Map<Sprite, Highlight>();

  private pointerX = 0;
  private pointerY = 0;
  private pointerInside = false;
  private reportedPointerInside = false;
  private dragging = false;
  private dragPointerId: number | null = null;
  private dragDeltaX = 0;
  private dragDeltaY = 0;
  private pressX = 0;
  private pressY = 0;
  private movedBeyondClick = false;
  private selectOnNextFrame = false;

  private hoveredTile: TilePosition = { x: 0, y: 0 };
  private selectedTile: TilePosition | null = null;
  private ghost: Sprite | null = null;
  private ghostName: string | null = null;
  private lastUserId: number | null = null;
  private readonly removeListeners: Array<() => void> = [];
  private destroyed = false;

  private constructor(app: Application, factory: SpriteFactory, callbacks: GameEngineCallbacks) {
    this.app = app;
    this.factory = factory;
    this.callbacks = callbacks;
    this.hoverFilter.brightness(2, false);
    this.selectFilter.sepia(false);

    this.world.addChild(this.terrainLayer, this.objectsLayer, this.ghostLayer);
    app.stage.addChild(this.world);

    // Start with the origin of the map in the middle of the screen.
    const origin = diagonalToScreen(0, 0);
    this.camera.centerOn(
      origin.x + TILE_WIDTH,
      origin.y - TILE_HEIGHT / 2,
      app.screen.width,
      app.screen.height,
    );

    this.registerInput(app.canvas);
    app.ticker.add(this.update, this);
  }

  static async create(host: HTMLElement, callbacks: GameEngineCallbacks): Promise<GameEngine> {
    const app = new Application();
    await app.init({
      resizeTo: host,
      backgroundAlpha: 0,
      antialias: true,
      autoDensity: true,
      resolution: window.devicePixelRatio || 1,
    });
    let factory: SpriteFactory;
    try {
      factory = await SpriteFactory.load();
    } catch (error) {
      app.destroy(true);
      throw error;
    }
    app.canvas.classList.add("game-canvas");
    host.appendChild(app.canvas);
    return new GameEngine(app, factory, callbacks);
  }

  get scale(): number {
    return this.camera.scale;
  }

  /** Zoom from the HUD buttons, anchored at the middle of the screen. */
  zoomBy(steps: number) {
    if (this.camera.zoomBy(steps, this.app.screen.width / 2, this.app.screen.height / 2)) {
      this.callbacks.onZoomChanged(this.camera.scale);
    }
  }

  /** Shows (or hides with null) a translucent preview of a building at the hovered tile. */
  setGhost(gameObjectName: string | null) {
    if (gameObjectName === this.ghostName) {
      return;
    }
    this.ghost?.destroy();
    this.ghost = null;
    this.ghostName = gameObjectName;
    if (gameObjectName !== null) {
      this.ghost = this.factory.createGameObject(gameObjectName);
      if (this.ghost !== null) {
        this.ghost.anchor.set(0, 1);
        this.ghost.alpha = GHOST_ALPHA;
        this.ghostLayer.addChild(this.ghost);
      }
    }
  }

  /** New data for a chunk (a pushed update or a response); ignored when the chunk is not on screen. */
  applyChunk(data: ChunkData) {
    const view = this.chunks.get(chunkKey(data.chunkX, data.chunkY));
    view?.apply(data, this.callbacks.getCurrentUserId());
  }

  /** Requests every loaded chunk again, e.g. after a reconnect. */
  refreshLoadedChunks() {
    for (const view of this.chunks.values()) {
      this.requestChunk(view);
    }
  }

  /** The last tile the pointer was over (also when it has since moved onto a panel). */
  getHoveredTile(): TilePosition {
    return this.hoveredTile;
  }

  destroy() {
    if (this.destroyed) {
      return;
    }
    this.destroyed = true;
    this.app.ticker.remove(this.update, this);
    this.removeListeners.forEach((remove) => remove());
    for (const view of this.chunks.values()) {
      view.destroy();
    }
    this.chunks.clear();
    this.app.destroy(true, { children: true });
  }

  private registerInput(canvas: HTMLCanvasElement) {
    const toLocal = (event: PointerEvent | WheelEvent) => {
      const bounds = canvas.getBoundingClientRect();
      return { x: event.clientX - bounds.left, y: event.clientY - bounds.top };
    };

    const onPointerDown = (event: PointerEvent) => {
      if (event.button !== 0 || this.dragging) {
        return;
      }
      const local = toLocal(event);
      this.pointerX = this.pressX = local.x;
      this.pointerY = this.pressY = local.y;
      this.pointerInside = true;
      this.dragging = true;
      this.dragPointerId = event.pointerId;
      this.movedBeyondClick = false;
      this.selectOnNextFrame = true;
      canvas.setPointerCapture(event.pointerId);
    };

    const onPointerMove = (event: PointerEvent) => {
      const local = toLocal(event);
      if (this.dragging && event.pointerId === this.dragPointerId) {
        this.dragDeltaX += local.x - this.pointerX;
        this.dragDeltaY += local.y - this.pointerY;
        if (Math.hypot(local.x - this.pressX, local.y - this.pressY) > CLICK_TOLERANCE_PX) {
          this.movedBeyondClick = true;
        }
      }
      this.pointerX = local.x;
      this.pointerY = local.y;
      this.pointerInside = true;
    };

    const endDrag = (event: PointerEvent, isClick: boolean) => {
      if (!this.dragging || event.pointerId !== this.dragPointerId) {
        return;
      }
      this.dragging = false;
      this.dragPointerId = null;
      if (canvas.hasPointerCapture(event.pointerId)) {
        canvas.releasePointerCapture(event.pointerId);
      }
      if (isClick && !this.movedBeyondClick) {
        this.callbacks.onTileClicked(this.camera.screenToTile(this.pointerX, this.pointerY));
      }
    };
    const onPointerUp = (event: PointerEvent) => endDrag(event, true);
    const onPointerCancel = (event: PointerEvent) => endDrag(event, false);

    const onPointerLeave = () => {
      if (!this.dragging) {
        this.pointerInside = false;
      }
    };

    const onWheel = (event: WheelEvent) => {
      event.preventDefault();
      if (event.deltaY === 0) {
        return;
      }
      const local = toLocal(event);
      if (this.camera.zoomBy(event.deltaY < 0 ? 1 : -1, local.x, local.y)) {
        this.callbacks.onZoomChanged(this.camera.scale);
      }
    };

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerCancel);
    canvas.addEventListener("pointerleave", onPointerLeave);
    canvas.addEventListener("wheel", onWheel, { passive: false });
    this.removeListeners.push(() => {
      canvas.removeEventListener("pointerdown", onPointerDown);
      canvas.removeEventListener("pointermove", onPointerMove);
      canvas.removeEventListener("pointerup", onPointerUp);
      canvas.removeEventListener("pointercancel", onPointerCancel);
      canvas.removeEventListener("pointerleave", onPointerLeave);
      canvas.removeEventListener("wheel", onWheel);
    });
  }

  private update() {
    this.camera.update(this.dragging, this.dragDeltaX, this.dragDeltaY);
    this.dragDeltaX = 0;
    this.dragDeltaY = 0;
    this.world.scale.set(this.camera.scale);
    this.world.position.set(this.camera.offsetX * this.camera.scale, this.camera.offsetY * this.camera.scale);

    this.streamChunks();
    this.updateOwnership();

    const tile = this.camera.screenToTile(this.pointerX, this.pointerY);
    const tileChanged = tile.x !== this.hoveredTile.x || tile.y !== this.hoveredTile.y;
    if (tileChanged || this.pointerInside !== this.reportedPointerInside) {
      this.hoveredTile = tile;
      this.reportedPointerInside = this.pointerInside;
      this.callbacks.onHoveredTileChanged(this.pointerInside ? tile : null);
    }
    if (this.selectOnNextFrame) {
      this.selectOnNextFrame = false;
      this.selectedTile = tile;
    }

    if (this.ghost !== null) {
      const position = diagonalToScreen(tile.x, tile.y);
      this.ghost.position.set(position.x, position.y);
      this.ghost.zIndex = tile.x + tile.y;
      this.ghost.visible = this.pointerInside;
    }

    this.updateHighlights();
  }

  private streamChunks() {
    const { width, height } = this.app.screen;
    const range = this.camera.visibleChunks(width, height, CHUNK_MARGIN);
    const view = this.camera.viewRect(width, height);
    const canRequest = this.callbacks.canRequestChunks();
    const now = performance.now();
    const wanted = new Set<string>();

    for (let chunkY = range.minY; chunkY <= range.maxY; chunkY++) {
      for (let chunkX = range.minX; chunkX <= range.maxX; chunkX++) {
        if (!chunkIntersectsView(chunkX, chunkY, view)) {
          continue;
        }
        const key = chunkKey(chunkX, chunkY);
        wanted.add(key);
        const existing = this.chunks.get(key);
        if (existing === undefined) {
          if (canRequest) {
            const created = new ChunkView(chunkX, chunkY, this.factory, this.terrainLayer, this.objectsLayer);
            this.chunks.set(key, created);
            this.requestChunk(created);
          }
        } else if (existing.state === "failed" && canRequest && now >= existing.retryAt) {
          this.requestChunk(existing);
        }
      }
    }

    for (const [key, chunk] of this.chunks) {
      if (!wanted.has(key)) {
        chunk.destroy();
        this.chunks.delete(key);
      }
    }
  }

  private requestChunk(view: ChunkView) {
    if (view.state === "failed") {
      view.state = "loading";
    }
    this.callbacks.requestChunk(view.chunkX, view.chunkY).then(
      (data) => view.apply(data, this.callbacks.getCurrentUserId()),
      (error: unknown) => {
        if (!view.isDestroyed) {
          console.warn(`Loading chunk ${view.chunkX},${view.chunkY} failed`, error);
          view.state = "failed";
          view.retryAt = performance.now() + CHUNK_RETRY_DELAY_MS;
        }
      },
    );
  }

  private updateOwnership() {
    const userId = this.callbacks.getCurrentUserId();
    if (userId !== this.lastUserId) {
      this.lastUserId = userId;
      for (const view of this.chunks.values()) {
        view.applyOwnership(userId);
      }
    }
  }

  private spritesAt(tile: TilePosition): Sprite[] {
    const chunk = tileToChunk(tile.x, tile.y);
    const view = this.chunks.get(chunkKey(chunk.x, chunk.y));
    if (view === undefined) {
      return [];
    }
    const localX = tile.x - chunk.x * CHUNK_SIZE;
    const localY = tile.y - chunk.y * CHUNK_SIZE;
    return [view.tileSprite(localX, localY), view.objectSprite(localX, localY)].filter(
      (sprite): sprite is Sprite => sprite !== undefined,
    );
  }

  /** Brightens the tile and object under the cursor and sepia-tints the ones selected on press. */
  private updateHighlights() {
    const next = new Map<Sprite, Highlight>();
    const mark = (sprite: Sprite, key: keyof Highlight) => {
      const highlight = next.get(sprite) ?? { hovered: false, selected: false };
      highlight[key] = true;
      next.set(sprite, highlight);
    };
    if (this.pointerInside) {
      this.spritesAt(this.hoveredTile).forEach((sprite) => mark(sprite, "hovered"));
    }
    if (this.selectedTile !== null) {
      this.spritesAt(this.selectedTile).forEach((sprite) => mark(sprite, "selected"));
    }

    for (const [sprite] of this.highlights) {
      if (!next.has(sprite) && !sprite.destroyed) {
        sprite.filters = null;
      }
    }
    for (const [sprite, highlight] of next) {
      const previous = this.highlights.get(sprite);
      if (previous?.hovered !== highlight.hovered || previous.selected !== highlight.selected) {
        const filters: Filter[] = [];
        if (highlight.selected) {
          filters.push(this.selectFilter);
        }
        if (highlight.hovered) {
          filters.push(this.hoverFilter);
        }
        sprite.filters = filters;
      }
    }
    this.highlights = next;
  }
}

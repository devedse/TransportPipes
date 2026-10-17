import { CHUNK_SIZE, MAX_ZOOM, MIN_ZOOM, TILE_WIDTH, ZOOM_STEP } from "./constants";
import { diagonalToScreen, worldToChunk, worldToTile, type Point, type TilePosition } from "./isometric";

export interface ChunkRange {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
}

export interface WorldRect {
  left: number;
  top: number;
  right: number;
  bottom: number;
}

/** Tallest sprite in the atlas (the construction site); buildings stick out this far above their tile. */
const MAX_SPRITE_HEIGHT = 500;

/**
 * Whether any sprite of a chunk can be inside the world rectangle. The chunk range from the
 * screen corners is a rectangle in chunk space, i.e. a diamond on screen; this drops the chunks
 * in its corners that are not actually visible.
 */
export function chunkIntersectsView(chunkX: number, chunkY: number, view: WorldRect): boolean {
  const x0 = chunkX * CHUNK_SIZE;
  const y0 = chunkY * CHUNK_SIZE;
  const x1 = x0 + CHUNK_SIZE - 1;
  const y1 = y0 + CHUNK_SIZE - 1;
  // Leftmost tile anchor is (x0, y1), rightmost (x1, y0), topmost (x0, y0), bottommost (x1, y1).
  const left = diagonalToScreen(x0, y1).x;
  const right = diagonalToScreen(x1, y0).x + TILE_WIDTH * 2;
  const top = diagonalToScreen(x0, y0).y - MAX_SPRITE_HEIGHT;
  const bottom = diagonalToScreen(x1, y1).y;
  return right >= view.left && left <= view.right && bottom >= view.top && top <= view.bottom;
}

const INERTIA_DECAY = 1.1;
const INERTIA_STOP = 0.1;

export function clampZoom(scale: number): number {
  // Round to one decimal like the original game, so repeated steps don't drift.
  return Math.round(Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, scale)) * 10) / 10;
}

/**
 * Camera state: world = screen / scale - offset.
 * The world container is drawn at (offset * scale) with the camera scale.
 */
export class Camera {
  offsetX = 0;
  offsetY = 0;
  scale = 1;
  private speedX = 0;
  private speedY = 0;
  private lastOffsetX = 0;
  private lastOffsetY = 0;
  private zoomedSinceLastUpdate = false;

  screenToWorld(screenX: number, screenY: number): Point {
    return { x: screenX / this.scale - this.offsetX, y: screenY / this.scale - this.offsetY };
  }

  screenToTile(screenX: number, screenY: number): TilePosition {
    const world = this.screenToWorld(screenX, screenY);
    return worldToTile(world.x, world.y);
  }

  /** Zooms to a new scale while keeping the world point under (anchorX, anchorY) in place. */
  zoomTo(requestedScale: number, anchorX: number, anchorY: number): boolean {
    const newScale = clampZoom(requestedScale);
    if (newScale === this.scale) {
      return false;
    }
    this.offsetX += anchorX * (1 / newScale - 1 / this.scale);
    this.offsetY += anchorY * (1 / newScale - 1 / this.scale);
    this.scale = newScale;
    this.zoomedSinceLastUpdate = true;
    return true;
  }

  zoomBy(steps: number, anchorX: number, anchorY: number): boolean {
    return this.zoomTo(this.scale + steps * ZOOM_STEP, anchorX, anchorY);
  }

  /** Centers the camera on a world point for a viewport of the given size. */
  centerOn(worldX: number, worldY: number, viewportWidth: number, viewportHeight: number) {
    this.offsetX = viewportWidth / 2 / this.scale - worldX;
    this.offsetY = viewportHeight / 2 / this.scale - worldY;
    this.lastOffsetX = this.offsetX;
    this.lastOffsetY = this.offsetY;
    this.speedX = 0;
    this.speedY = 0;
  }

  /**
   * Advances one frame. While dragging the camera follows the pointer (delta in screen px since
   * the previous frame); after release it keeps gliding with the last speed, decaying per frame.
   */
  update(dragging: boolean, dragDeltaX: number, dragDeltaY: number) {
    if (dragging) {
      this.offsetX += dragDeltaX / this.scale;
      this.offsetY += dragDeltaY / this.scale;
    } else {
      this.speedX /= INERTIA_DECAY;
      this.speedY /= INERTIA_DECAY;
      if (Math.abs(this.speedX) < INERTIA_STOP) {
        this.speedX = 0;
      }
      if (Math.abs(this.speedY) < INERTIA_STOP) {
        this.speedY = 0;
      }
      this.offsetX += this.speedX;
      this.offsetY += this.speedY;
    }

    if (!this.zoomedSinceLastUpdate) {
      this.speedX = this.offsetX - this.lastOffsetX;
      this.speedY = this.offsetY - this.lastOffsetY;
    }
    this.zoomedSinceLastUpdate = false;
    this.lastOffsetX = this.offsetX;
    this.lastOffsetY = this.offsetY;
  }

  /** The world rectangle that is visible in a viewport. */
  viewRect(viewportWidth: number, viewportHeight: number): WorldRect {
    const topLeft = this.screenToWorld(0, 0);
    const bottomRight = this.screenToWorld(viewportWidth, viewportHeight);
    return { left: topLeft.x, top: topLeft.y, right: bottomRight.x, bottom: bottomRight.y };
  }

  /** The chunks that intersect a viewport, from its four corners (plus an optional margin). */
  visibleChunks(viewportWidth: number, viewportHeight: number, margin = 0): ChunkRange {
    const topLeft = this.screenToWorld(0, 0);
    const bottomRight = this.screenToWorld(viewportWidth, viewportHeight);
    const chunkTopLeft = worldToChunk(topLeft.x, topLeft.y);
    const chunkTopRight = worldToChunk(bottomRight.x, topLeft.y);
    const chunkBottomLeft = worldToChunk(topLeft.x, bottomRight.y);
    const chunkBottomRight = worldToChunk(bottomRight.x, bottomRight.y);
    return {
      minX: chunkTopLeft.x - margin,
      maxX: chunkBottomRight.x + margin,
      minY: chunkTopRight.y - margin,
      maxY: chunkBottomLeft.y + margin,
    };
  }
}

import { CHUNK_SIZE, TILE_HEIGHT, TILE_WIDTH } from "./constants";

export interface Point {
  x: number;
  y: number;
}

export interface TilePosition {
  /** Global tile x. */
  x: number;
  /** Global tile y. */
  y: number;
}

/** Diagonal (tile) coordinates to world pixels: the bottom-left anchor of the tile sprite. */
export function diagonalToScreen(x: number, y: number): Point {
  return {
    x: x * TILE_WIDTH - y * TILE_WIDTH,
    y: x * (TILE_HEIGHT / 2) + y * (TILE_HEIGHT / 2),
  };
}

/** World pixels to (fractional) diagonal coordinates; the exact inverse of diagonalToScreen. */
export function screenToDiagonal(x: number, y: number): Point {
  return {
    x: x / (TILE_WIDTH * 2) + y / TILE_HEIGHT,
    y: y / TILE_WIDTH - x / (TILE_HEIGHT * 2),
  };
}

/** The tile under a world-space point (sprites are anchored bottom-left, hence the offsets). */
export function worldToTile(worldX: number, worldY: number): TilePosition {
  const diagonal = screenToDiagonal(worldX - TILE_WIDTH, worldY + TILE_HEIGHT);
  return { x: Math.floor(diagonal.x), y: Math.floor(diagonal.y) };
}

/** The chunk containing a world-space point. */
export function worldToChunk(worldX: number, worldY: number): Point {
  const tile = worldToTile(worldX, worldY);
  return tileToChunk(tile.x, tile.y);
}

export function tileToChunk(x: number, y: number): Point {
  return { x: Math.floor(x / CHUNK_SIZE), y: Math.floor(y / CHUNK_SIZE) };
}

export function chunkKey(chunkX: number, chunkY: number): string {
  return `${chunkX}_${chunkY}`;
}

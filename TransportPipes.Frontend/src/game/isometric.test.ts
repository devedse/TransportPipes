import { describe, expect, it } from "vitest";
import { chunkKey, diagonalToScreen, screenToDiagonal, tileToChunk, worldToTile } from "./isometric";

describe("diagonalToScreen / screenToDiagonal", () => {
  it("uses the original 96px isometric projection", () => {
    expect(diagonalToScreen(0, 0)).toEqual({ x: 0, y: 0 });
    expect(diagonalToScreen(1, 0)).toEqual({ x: 96, y: 48 });
    expect(diagonalToScreen(0, 1)).toEqual({ x: -96, y: 48 });
    expect(diagonalToScreen(3, 2)).toEqual({ x: 96, y: 240 });
  });

  it("round trips for integer and fractional coordinates", () => {
    for (const [x, y] of [
      [0, 0],
      [5, -3],
      [-12, 40],
      [2.25, 7.5],
    ] as const) {
      const screen = diagonalToScreen(x, y);
      const back = screenToDiagonal(screen.x, screen.y);
      expect(back.x).toBeCloseTo(x, 10);
      expect(back.y).toBeCloseTo(y, 10);
    }
  });
});

describe("worldToTile", () => {
  it("finds the tile whose diamond contains the point", () => {
    for (const [x, y] of [
      [0, 0],
      [4, 9],
      [-3, -7],
      [15, -2],
    ] as const) {
      // The sprite is anchored bottom-left; the diamond centre is 96 px right and 48 px up from that.
      const anchor = diagonalToScreen(x, y);
      expect(worldToTile(anchor.x + 96, anchor.y - 48)).toEqual({ x, y });
      // Points well inside the diamond, near its four corners.
      expect(worldToTile(anchor.x + 10, anchor.y - 48)).toEqual({ x, y });
      expect(worldToTile(anchor.x + 182, anchor.y - 48)).toEqual({ x, y });
      expect(worldToTile(anchor.x + 96, anchor.y - 90)).toEqual({ x, y });
      expect(worldToTile(anchor.x + 96, anchor.y - 6)).toEqual({ x, y });
    }
  });

  it("matches the old getMousePosOnTile formula", () => {
    // Old: MouseToDiagonal(mouse/scale - offset - TILE_WIDTH, mouse/scale - offset + TILE_HEIGHT), floored.
    const worldX = 431.5;
    const worldY = -77.25;
    const old = screenToDiagonal(worldX - 96, worldY + 96);
    expect(worldToTile(worldX, worldY)).toEqual({ x: Math.floor(old.x), y: Math.floor(old.y) });
  });
});

describe("tileToChunk", () => {
  it("floors towards negative infinity", () => {
    expect(tileToChunk(0, 7)).toEqual({ x: 0, y: 0 });
    expect(tileToChunk(8, 15)).toEqual({ x: 1, y: 1 });
    expect(tileToChunk(-1, -8)).toEqual({ x: -1, y: -1 });
    expect(tileToChunk(-9, 3)).toEqual({ x: -2, y: 0 });
  });

  it("builds stable chunk keys", () => {
    expect(chunkKey(-1, 2)).toBe("-1_2");
  });
});

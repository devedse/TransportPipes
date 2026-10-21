import { describe, expect, it } from "vitest";
import { Camera, chunkIntersectsView, clampZoom } from "./camera";
import { diagonalToScreen } from "./isometric";

describe("clampZoom", () => {
  it("clamps to 0.3..2 and rounds to one decimal", () => {
    expect(clampZoom(0.001)).toBe(0.04);
    expect(clampZoom(5)).toBe(1.5);
    expect(clampZoom(1.1000000001)).toBe(1.1);
    expect(clampZoom(0.7 + 0.1)).toBe(0.8);
  });
});

describe("Camera zoom", () => {
  it("keeps the world point under the cursor in place", () => {
    const camera = new Camera();
    camera.offsetX = 120;
    camera.offsetY = -40;
    const anchor = { x: 300, y: 200 };
    const before = camera.screenToWorld(anchor.x, anchor.y);

    expect(camera.zoomBy(1, anchor.x, anchor.y)).toBe(true);
    expect(camera.scale).toBe(1.18);
    const after = camera.screenToWorld(anchor.x, anchor.y);
    expect(after.x).toBeCloseTo(before.x, 10);
    expect(after.y).toBeCloseTo(before.y, 10);
  });

  it("uses the original offset formula", () => {
    const camera = new Camera();
    camera.zoomTo(0.5, 400, 100);
    // offset += mouse * (1 / newScale - 1 / oldScale)
    expect(camera.offsetX).toBeCloseTo(400 * (1 / 0.5 - 1), 10);
    expect(camera.offsetY).toBeCloseTo(100 * (1 / 0.5 - 1), 10);
  });

  it("does nothing beyond the limits", () => {
    const camera = new Camera();
    camera.zoomTo(2, 0, 0);
    expect(camera.zoomBy(1, 10, 10)).toBe(false);
    expect(camera.scale).toBe(1.5);
  });
});

describe("Camera panning", () => {
  it("follows the drag in world units", () => {
    const camera = new Camera();
    camera.zoomTo(0.5, 0, 0);
    camera.update(true, 40, -20);
    expect(camera.offsetX).toBeCloseTo(80);
    expect(camera.offsetY).toBeCloseTo(-40);
  });

  it("keeps gliding after release and slows down until it stops", () => {
    const camera = new Camera();
    camera.update(true, 30, 0);
    const afterDrag = camera.offsetX;

    camera.update(false, 0, 0);
    const firstGlide = camera.offsetX - afterDrag;
    expect(firstGlide).toBeCloseTo(30 / 1.1);

    let previous = camera.offsetX;
    let steps = 0;
    while (steps < 500) {
      camera.update(false, 0, 0);
      if (camera.offsetX === previous) {
        break;
      }
      expect(camera.offsetX - previous).toBeLessThan(firstGlide);
      previous = camera.offsetX;
      steps++;
    }
    expect(steps).toBeLessThan(500);
  });
});

describe("visible chunks", () => {
  it("covers the chunk under every corner of the viewport", () => {
    const camera = new Camera();
    camera.centerOn(0, 0, 1280, 720);
    const range = camera.visibleChunks(1280, 720);
    for (const [sx, sy] of [
      [0, 0],
      [1280, 0],
      [0, 720],
      [1280, 720],
      [640, 360],
    ] as const) {
      const tile = camera.screenToTile(sx, sy);
      const chunkX = Math.floor(tile.x / 32);
      const chunkY = Math.floor(tile.y / 32);
      expect(chunkX).toBeGreaterThanOrEqual(range.minX);
      expect(chunkX).toBeLessThanOrEqual(range.maxX);
      expect(chunkY).toBeGreaterThanOrEqual(range.minY);
      expect(chunkY).toBeLessThanOrEqual(range.maxY);
    }
  });

  it("grows when zooming out and adds the margin", () => {
    const camera = new Camera();
    camera.centerOn(0, 0, 1000, 600);
    const near = camera.visibleChunks(1000, 600);
    const withMargin = camera.visibleChunks(1000, 600, 1);
    expect(withMargin).toEqual({
      minX: near.minX - 1,
      maxX: near.maxX + 1,
      minY: near.minY - 1,
      maxY: near.maxY + 1,
    });
    camera.zoomTo(0.05, 500, 300);
    const far = camera.visibleChunks(1000, 600);
    expect(far.maxX - far.minX).toBeGreaterThan(near.maxX - near.minX);
  });
});

describe("chunkIntersectsView", () => {
  it("accepts the chunk at the centre and rejects far away chunks", () => {
    const centre = diagonalToScreen(16, 16);
    const view = { left: centre.x - 300, right: centre.x + 300, top: centre.y - 200, bottom: centre.y + 200 };
    expect(chunkIntersectsView(0, 0, view)).toBe(true);
    expect(chunkIntersectsView(10, 10, view)).toBe(false);
    expect(chunkIntersectsView(-10, 10, view)).toBe(false);
  });

  it("keeps chunks just below the view because their buildings stick out upwards", () => {
    // Chunk (1, 1) starts at tile (32, 32), whose anchor is at y = 3072.
    const view = { left: -200, right: 200, top: 0, bottom: 2900 };
    expect(chunkIntersectsView(1, 1, view)).toBe(true);
    expect(chunkIntersectsView(3, 3, view)).toBe(false);
  });
});

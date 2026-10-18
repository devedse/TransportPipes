import type { TerrainType } from "../../api/contracts";
import { createCanvas, hash01, Iso, OUTLINE, shade, TILE_H, TILE_W } from "./iso";

// Terrain tiles (192×96 diamonds) in the style of the original grass/beach/water tiles: a flat colour with a noisy
// texture and a thin dark edge. The original tiles are used for grass, beach and water; the rest is drawn here.

export const TERRAIN_COLORS: Record<TerrainType, string> = {
  grass: "#1f8a2a",
  hills: "#5a9a2c",
  forest: "#1b6a24",
  beach: "#d8b62a",
  water: "#1e90ff",
  desert: "#e2c06a",
  mountain: "#8c9096",
  road: "#7d7a74",
  city: "#b7ae9f",
  rock: "#3d342d",
  cavern: "#8a6a48",
};

/** Colours for the small overview map and far zoom */
export const TERRAIN_MAP_COLORS: Record<TerrainType, string> = {
  grass: "#2f9a35",
  hills: "#6aa63a",
  forest: "#1e6b2a",
  beach: "#e0c040",
  water: "#2b8fe8",
  desert: "#e8c878",
  mountain: "#9a9ea6",
  road: "#8a867e",
  city: "#c4bba9",
  rock: "#3a312a",
  cavern: "#9a7650",
};

function diamond(g: CanvasRenderingContext2D) {
  g.beginPath();
  g.moveTo(0, TILE_H / 2);
  g.lineTo(TILE_W / 2, 0);
  g.lineTo(TILE_W, TILE_H / 2);
  g.lineTo(TILE_W / 2, TILE_H);
  g.closePath();
}

function speckle(
  g: CanvasRenderingContext2D,
  seed: number,
  count: number,
  colors: string[],
  size: [number, number],
) {
  for (let i = 0; i < count; i++) {
    const x = hash01(seed * 7919 + i * 31) * TILE_W;
    const y = hash01(seed * 104729 + i * 17) * TILE_H;
    const w = size[0] + hash01(seed + i * 13) * (size[1] - size[0]);
    g.fillStyle = colors[i % colors.length]!;
    g.fillRect(x, y, w, Math.max(1, w * 0.5));
  }
}

export function drawTerrainTile(type: TerrainType, variant: number): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_H);
  const g = canvas.getContext("2d")!;
  const base = TERRAIN_COLORS[type];
  diamond(g);
  g.save();
  g.clip();
  g.fillStyle = base;
  g.fillRect(0, 0, TILE_W, TILE_H);
  const seed = variant * 97 + type.length * 13;

  switch (type) {
    case "hills": {
      speckle(g, seed, 160, [shade(base, 0.85), shade(base, 1.15)], [2, 5]);
      g.strokeStyle = shade(base, 1.35);
      g.lineWidth = 3;
      for (let i = 0; i < 3; i++) {
        const x = 30 + hash01(seed + i) * 110;
        const y = 30 + hash01(seed + i * 5) * 40;
        g.beginPath();
        g.arc(x, y + 12, 18, Math.PI * 1.1, Math.PI * 1.9);
        g.stroke();
      }
      g.strokeStyle = shade(base, 0.7);
      g.lineWidth = 2;
      for (let i = 0; i < 3; i++) {
        const x = 30 + hash01(seed + i) * 110;
        const y = 30 + hash01(seed + i * 5) * 40;
        g.beginPath();
        g.arc(x + 4, y + 14, 18, Math.PI * 0.05, Math.PI * 0.6);
        g.stroke();
      }
      break;
    }
    case "forest":
      speckle(g, seed, 220, [shade(base, 0.75), shade(base, 1.2), "#2a4a1a"], [2, 6]);
      break;
    case "desert": {
      speckle(g, seed, 200, [shade(base, 0.9), shade(base, 1.1)], [1, 3]);
      g.strokeStyle = shade(base, 0.8);
      g.lineWidth = 2;
      for (let i = 0; i < 4; i++) {
        const y = 20 + i * 18 + hash01(seed + i) * 8;
        g.beginPath();
        g.moveTo(20, y);
        g.bezierCurveTo(60, y - 10, 110, y + 10, 170, y - 4);
        g.stroke();
      }
      break;
    }
    case "mountain":
      speckle(g, seed, 260, [shade(base, 0.7), shade(base, 1.25), "#5f636a"], [2, 6]);
      break;
    case "road": {
      speckle(g, seed, 120, [shade(base, 0.9), shade(base, 1.08)], [2, 4]);
      g.strokeStyle = "rgba(0,0,0,0.18)";
      g.lineWidth = 1;
      for (let i = 1; i < 6; i++) {
        g.beginPath();
        g.moveTo((TILE_W / 6) * i - 48, 0);
        g.lineTo((TILE_W / 6) * i + 48, TILE_H);
        g.stroke();
        g.beginPath();
        g.moveTo((TILE_W / 6) * i + 48, 0);
        g.lineTo((TILE_W / 6) * i - 48, TILE_H);
        g.stroke();
      }
      break;
    }
    case "city": {
      g.strokeStyle = "rgba(60,50,40,0.25)";
      g.lineWidth = 1.5;
      for (let i = -4; i < 8; i++) {
        g.beginPath();
        g.moveTo(i * 32, 0);
        g.lineTo(i * 32 + 192, 96);
        g.stroke();
        g.beginPath();
        g.moveTo(i * 32 + 96, 0);
        g.lineTo(i * 32 - 96, 96);
        g.stroke();
      }
      break;
    }
    case "rock": {
      // Solid stone of the Deep Level: grains, and a few cracks
      speckle(g, seed, 240, [shade(base, 0.7), shade(base, 1.3), "#2a231e"], [2, 6]);
      g.strokeStyle = "rgba(15,10,8,0.6)";
      g.lineWidth = 2;
      for (let i = 0; i < 2; i++) {
        let x = 30 + hash01(seed + i * 41) * 130;
        let y = 20 + hash01(seed + i * 43) * 50;
        g.beginPath();
        g.moveTo(x, y);
        for (let step = 0; step < 4; step++) {
          x += 8 + hash01(seed + i * 7 + step) * 16;
          y += (hash01(seed + i * 11 + step) - 0.5) * 18;
          g.lineTo(x, y);
        }
        g.stroke();
      }
      break;
    }
    case "cavern": {
      // A dug out floor, lit warm by the lanterns: packed earth and pebbles
      const light = g.createRadialGradient(TILE_W / 2, TILE_H / 2, 10, TILE_W / 2, TILE_H / 2, TILE_W / 2);
      light.addColorStop(0, shade(base, 1.12));
      light.addColorStop(1, shade(base, 0.88));
      g.fillStyle = light;
      g.fillRect(0, 0, TILE_W, TILE_H);
      speckle(g, seed, 120, [shade(base, 0.75), shade(base, 1.2), "#5c4430"], [2, 5]);
      for (let i = 0; i < 5; i++) {
        const x = 30 + hash01(seed * 3 + i * 19) * 130;
        const y = 22 + hash01(seed * 5 + i * 23) * 52;
        g.fillStyle = "#6b5a4a";
        g.beginPath();
        g.ellipse(x, y, 4 + hash01(i + seed) * 4, 2.5, 0, 0, Math.PI * 2);
        g.fill();
      }
      break;
    }
    default:
      speckle(g, seed, 200, [shade(base, 0.8), shade(base, 1.2)], [2, 5]);
  }
  g.restore();
  diamond(g);
  g.strokeStyle = "rgba(0,0,0,0.55)";
  g.lineWidth = 1;
  g.stroke();
  return canvas;
}

/** Trees for forest tiles (several per tile, drawn as one sprite) */
export function drawForestTrees(variant: number): HTMLCanvasElement {
  const k = new Iso(1, 70);
  const spots: Array<[number, number]> = [
    [0.3, 0.25],
    [0.7, 0.3],
    [0.25, 0.7],
    [0.55, 0.6],
    [0.8, 0.75],
  ];
  const sorted = spots
    .filter((_, i) => hash01(variant * 31 + i) > 0.25)
    .sort((a, b) => a[0] + a[1] - (b[0] + b[1]));
  sorted.forEach(([u, v], i) => {
    const conifer = hash01(variant * 7 + i) > 0.5;
    const scale = 0.7 + hash01(variant * 13 + i) * 0.45;
    if (conifer) {
      const [x, y] = k.pt(u, v);
      const g = k.g;
      g.fillStyle = "#5a3a1a";
      g.fillRect(x - 3, y - 12 * scale, 6, 12 * scale);
      g.beginPath();
      g.moveTo(x, y - 70 * scale);
      g.lineTo(x + 20 * scale, y - 10 * scale);
      g.lineTo(x - 20 * scale, y - 10 * scale);
      g.closePath();
      const gradient = g.createLinearGradient(x - 20 * scale, 0, x + 20 * scale, 0);
      gradient.addColorStop(0, "#2f7d3a");
      gradient.addColorStop(1, "#174f24");
      g.fillStyle = gradient;
      g.fill();
      g.strokeStyle = OUTLINE;
      g.lineWidth = 2;
      g.stroke();
    } else {
      k.tree(u, v, scale * 0.85, hash01(variant + i) > 0.5 ? "#2f8a3a" : "#3c9a2e");
    }
  });
  return k.canvas;
}

/** Rocks and peaks for mountain tiles */
export function drawRocks(variant: number): HTMLCanvasElement {
  const k = new Iso(1, 90);
  const g = k.g;
  const peaks = 1 + Math.floor(hash01(variant * 3) * 2);
  for (let i = 0; i < peaks; i++) {
    const u = 0.35 + hash01(variant * 11 + i) * 0.35;
    const v = 0.35 + hash01(variant * 17 + i) * 0.35;
    const h = 50 + hash01(variant * 23 + i) * 50;
    const w = 45 + hash01(variant * 29 + i) * 25;
    const [x, y] = k.pt(u, v);
    g.beginPath();
    g.moveTo(x - w, y + 10);
    g.lineTo(x - w * 0.15, y - h);
    g.lineTo(x + w, y + 10);
    g.closePath();
    const gradient = g.createLinearGradient(x - w, 0, x + w, 0);
    gradient.addColorStop(0, "#a9adb3");
    gradient.addColorStop(0.45, "#8a8f96");
    gradient.addColorStop(0.46, "#6b7077");
    gradient.addColorStop(1, "#555a61");
    g.fillStyle = gradient;
    g.fill();
    g.strokeStyle = OUTLINE;
    g.lineWidth = 2.5;
    g.stroke();
    // Snow cap
    g.beginPath();
    g.moveTo(x - w * 0.15, y - h);
    g.lineTo(x - w * 0.15 - w * 0.25, y - h + h * 0.3);
    g.lineTo(x - w * 0.05, y - h + h * 0.22);
    g.lineTo(x + w * 0.15, y - h + h * 0.32);
    g.closePath();
    g.fillStyle = "#f4f6f8";
    g.fill();
    g.stroke();
  }
  return k.canvas;
}

/** Ore chunks for a deposit tile: more and bigger with more stars, so a rich deposit is easy to spot */
export function drawDeposit(color: string, richness: number, variant: number): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_H);
  const g = canvas.getContext("2d")!;
  diamond(g);
  g.save();
  g.clip();
  // A darker, disturbed patch of ground under the ore
  g.fillStyle = "rgba(60, 35, 20, 0.28)";
  g.beginPath();
  g.ellipse(TILE_W / 2, TILE_H / 2, 70, 32, 0, 0, Math.PI * 2);
  g.fill();
  const count = 4 + richness * 3;
  for (let i = 0; i < count; i++) {
    const x = 36 + hash01(variant * 131 + i * 7) * 120;
    const y = 24 + hash01(variant * 71 + i * 11) * 48;
    const r = 6 + richness * 1.6 + hash01(i * 5 + variant) * 5;
    g.beginPath();
    g.moveTo(x - r, y + r * 0.2);
    g.lineTo(x - r * 0.4, y - r * 0.6);
    g.lineTo(x + r * 0.5, y - r * 0.7);
    g.lineTo(x + r, y);
    g.lineTo(x + r * 0.3, y + r * 0.55);
    g.lineTo(x - r * 0.5, y + r * 0.5);
    g.closePath();
    g.fillStyle = shade(color, 0.85 + hash01(i + variant * 3) * 0.45);
    g.fill();
    g.strokeStyle = OUTLINE;
    g.lineWidth = 2;
    g.stroke();
    // A glint
    g.fillStyle = "rgba(255,255,255,0.55)";
    g.fillRect(x - r * 0.3, y - r * 0.4, r * 0.35, r * 0.18);
  }
  g.restore();
  return canvas;
}

/** A vein of ore in solid rock: a hint of what digging will uncover */
export function drawVein(color: string, richness: number, variant: number): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_H);
  const g = canvas.getContext("2d")!;
  diamond(g);
  g.save();
  g.clip();
  g.lineCap = "round";
  for (let i = 0; i < richness - 1; i++) {
    let x = 20 + hash01(variant * 19 + i * 5) * 60;
    let y = 30 + hash01(variant * 23 + i * 7) * 40;
    g.beginPath();
    g.moveTo(x, y);
    for (let step = 0; step < 6; step++) {
      x += 14 + hash01(variant + i * 3 + step) * 14;
      y += (hash01(variant * 5 + i + step * 3) - 0.5) * 20;
      g.lineTo(x, y);
    }
    g.strokeStyle = "rgba(10,8,6,0.7)";
    g.lineWidth = 7;
    g.stroke();
    g.strokeStyle = shade(color, 1.1);
    g.lineWidth = 3.5;
    g.stroke();
  }
  g.restore();
  return canvas;
}

/** A glowing pool of magma (Magma Taps stand on these) */
export function drawMagma(richness: number, variant: number): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_H);
  const g = canvas.getContext("2d")!;
  diamond(g);
  g.save();
  g.clip();
  const rx = 52 + richness * 6;
  const ry = rx / 2;
  const glow = g.createRadialGradient(TILE_W / 2, TILE_H / 2, 4, TILE_W / 2, TILE_H / 2, rx);
  glow.addColorStop(0, "#fff3a0");
  glow.addColorStop(0.35, "#ffb02e");
  glow.addColorStop(0.75, "#e2461b");
  glow.addColorStop(1, "#5a1a0c");
  g.fillStyle = glow;
  g.beginPath();
  g.ellipse(TILE_W / 2, TILE_H / 2, rx, ry, 0, 0, Math.PI * 2);
  g.fill();
  // A darker crust floating on it
  g.strokeStyle = "rgba(60,15,5,0.65)";
  g.lineWidth = 2.5;
  for (let i = 0; i < 4; i++) {
    const x = TILE_W / 2 + (hash01(variant * 7 + i) - 0.5) * rx;
    const y = TILE_H / 2 + (hash01(variant * 11 + i) - 0.5) * ry;
    g.beginPath();
    g.moveTo(x - 10, y);
    g.lineTo(x, y - 4);
    g.lineTo(x + 12, y + 2);
    g.stroke();
  }
  g.beginPath();
  g.ellipse(TILE_W / 2, TILE_H / 2, rx, ry, 0, 0, Math.PI * 2);
  g.strokeStyle = OUTLINE;
  g.lineWidth = 2.5;
  g.stroke();
  g.restore();
  return canvas;
}

/** A lantern on a post with its warm glow, for the caverns */
export function drawLantern(): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_W);
  const g = canvas.getContext("2d")!;
  const cx = TILE_W / 2;
  const floor = TILE_W - TILE_H / 2;
  const glow = g.createRadialGradient(cx, floor - 30, 4, cx, floor - 10, TILE_W / 2);
  glow.addColorStop(0, "rgba(255,214,140,0.55)");
  glow.addColorStop(0.5, "rgba(255,170,80,0.18)");
  glow.addColorStop(1, "rgba(255,150,60,0)");
  g.fillStyle = glow;
  g.fillRect(0, 0, TILE_W, TILE_W);
  // The post
  g.fillStyle = "#3a2a1c";
  g.fillRect(cx - 3, floor - 58, 6, 58);
  g.strokeStyle = OUTLINE;
  g.lineWidth = 2;
  g.strokeRect(cx - 3, floor - 58, 6, 58);
  g.fillRect(cx - 3, floor - 60, 18, 4);
  // The lantern
  g.fillStyle = "#ffd77a";
  g.fillRect(cx + 8, floor - 56, 12, 16);
  g.strokeRect(cx + 8, floor - 56, 12, 16);
  g.fillStyle = "#3a2a1c";
  g.fillRect(cx + 6, floor - 60, 16, 5);
  return canvas;
}

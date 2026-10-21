// Isometric drawing on a 2D canvas, in the style of the original sprites: flat colours, a light top, a darker right
// side and thick dark outlines. A tile is 192×96 pixels; u runs to the bottom right, v to the bottom left, z up.

export const TILE_W = 192;
export const TILE_H = 96;
export const OUTLINE = "#17181c";

/** Screen pixels per tile unit of a circle's radius (an iso circle is an ellipse) */
const CIRCLE_RX = 96 * Math.SQRT2;
const CIRCLE_RY = 48 * Math.SQRT2;

export type V3 = [number, number, number];

export interface Attachment {
  kind: "smoke" | "rotor" | "blink" | "flag" | "glow" | "wheel" | "spark";
  /** Position relative to the sprite's bottom-left corner (y is negative upwards) */
  x: number;
  y: number;
  size: number;
  color?: string;
  /** Generated art: the picture of the moving part (a rotor) and its scale */
  texture?: import("pixi.js").Texture;
  scale?: number;
}

export function shade(hex: string, factor: number): string {
  const value = parseInt(hex.slice(1), 16);
  const channel = (shift: number) => {
    const c = (value >> shift) & 255;
    const next = factor >= 1 ? c + (255 - c) * (factor - 1) : c * factor;
    return Math.max(0, Math.min(255, Math.round(next)));
  };
  return `#${((channel(16) << 16) | (channel(8) << 8) | channel(0)).toString(16).padStart(6, "0")}`;
}

export function withAlpha(hex: string, alpha: number): string {
  const value = parseInt(hex.slice(1), 16);
  return `rgba(${(value >> 16) & 255}, ${(value >> 8) & 255}, ${value & 255}, ${alpha})`;
}

/** A deterministic pseudo random number for a seed (0..1) */
export function hash01(seed: number): number {
  let x = Math.imul(seed ^ 0x9e3779b9, 0x85ebca6b);
  x ^= x >>> 13;
  x = Math.imul(x, 0xc2b2ae35);
  x ^= x >>> 16;
  return (x >>> 0) / 4294967296;
}

export function createCanvas(width: number, height: number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = Math.max(1, Math.ceil(width));
  canvas.height = Math.max(1, Math.ceil(height));
  return canvas;
}

export interface BoxColors {
  top: string;
  left: string;
  right: string;
}

export function boxColors(color: string): BoxColors {
  return { top: shade(color, 1.18), left: color, right: shade(color, 0.72) };
}

/** A sprite canvas for a footprint of size×size tiles with extra room above it */
export class Iso {
  readonly canvas: HTMLCanvasElement;
  readonly g: CanvasRenderingContext2D;
  readonly attachments: Attachment[] = [];
  readonly width: number;
  readonly height: number;
  private readonly ox: number;
  private readonly oy: number;

  readonly size: number;
  readonly extra: number;

  constructor(size: number, extra: number) {
    this.size = size;
    this.extra = extra;
    this.width = TILE_W * size;
    this.height = TILE_H * size + extra;
    this.canvas = createCanvas(this.width, this.height);
    this.g = this.canvas.getContext("2d")!;
    this.g.lineJoin = "round";
    this.g.lineCap = "round";
    this.ox = (TILE_W / 2) * size;
    this.oy = extra;
  }

  pt(u: number, v: number, z = 0): [number, number] {
    return [this.ox + (u - v) * (TILE_W / 2), this.oy + (u + v) * (TILE_H / 2) - z];
  }

  /** Records an animated detail at a point of the sprite */
  attach(kind: Attachment["kind"], u: number, v: number, z: number, size: number, color?: string) {
    const [x, y] = this.pt(u, v, z);
    this.attachments.push({ kind, x, y: y - this.height, size, color });
  }

  private trace(points: V3[]) {
    const g = this.g;
    g.beginPath();
    points.forEach(([u, v, z], i) => {
      const [x, y] = this.pt(u, v, z);
      if (i === 0) {
        g.moveTo(x, y);
      } else {
        g.lineTo(x, y);
      }
    });
    g.closePath();
  }

  poly(points: V3[], fill: string | CanvasGradient | null, outline: string | null = OUTLINE, lineWidth = 3) {
    this.trace(points);
    if (fill !== null) {
      this.g.fillStyle = fill;
      this.g.fill();
    }
    if (outline !== null) {
      this.g.strokeStyle = outline;
      this.g.lineWidth = lineWidth;
      this.g.stroke();
    }
  }

  line(a: V3, b: V3, color = OUTLINE, width = 3) {
    const g = this.g;
    const [x0, y0] = this.pt(...a);
    const [x1, y1] = this.pt(...b);
    g.beginPath();
    g.moveTo(x0, y0);
    g.lineTo(x1, y1);
    g.strokeStyle = color;
    g.lineWidth = width;
    g.stroke();
  }

  /** The flat ground under a building (a pad, a field, an excavation) */
  pad(u: number, v: number, w: number, d: number, fill: string, outline: string | null = OUTLINE, z = 0) {
    this.poly(
      [
        [u, v, z],
        [u + w, v, z],
        [u + w, v + d, z],
        [u, v + d, z],
      ],
      fill,
      outline,
      2.5,
    );
  }

  box(
    u: number,
    v: number,
    w: number,
    d: number,
    h: number,
    color: string | BoxColors,
    z = 0,
    outline = true,
  ) {
    const c = typeof color === "string" ? boxColors(color) : color;
    const o = outline ? OUTLINE : null;
    const u1 = u + w;
    const v1 = v + d;
    // Front left face (v = v1), front right face (u = u1), top
    this.poly(
      [
        [u, v1, z],
        [u1, v1, z],
        [u1, v1, z + h],
        [u, v1, z + h],
      ],
      c.left,
      o,
    );
    this.poly(
      [
        [u1, v, z],
        [u1, v1, z],
        [u1, v1, z + h],
        [u1, v, z + h],
      ],
      c.right,
      o,
    );
    this.poly(
      [
        [u, v, z + h],
        [u1, v, z + h],
        [u1, v1, z + h],
        [u, v1, z + h],
      ],
      c.top,
      o,
    );
  }

  /** Windows on the front left face of a box (along u) */
  windowsLeft(
    u: number,
    v: number,
    w: number,
    d: number,
    z: number,
    h: number,
    rows: number,
    cols: number,
    color = "#2f7fd0",
    frame = "#5b1a1a",
  ) {
    const v1 = v + d;
    const mu = (w * 0.12) / cols;
    const mz = (h * 0.18) / rows;
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        const cu = u + (w / cols) * c + mu;
        const cw = w / cols - mu * 2;
        const cz = z + (h / rows) * r + mz;
        const ch = h / rows - mz * 2;
        this.poly(
          [
            [cu, v1, cz],
            [cu + cw, v1, cz],
            [cu + cw, v1, cz + ch],
            [cu, v1, cz + ch],
          ],
          color,
          frame,
          2,
        );
        this.gridLines([cu, v1, cz], [cw, 0, 0], [0, 0, ch], 3, 2);
      }
    }
  }

  /** Windows on the front right face of a box (along v) */
  windowsRight(
    u: number,
    v: number,
    w: number,
    d: number,
    z: number,
    h: number,
    rows: number,
    cols: number,
    color = "#2a6db3",
    frame = "#5b1a1a",
  ) {
    const u1 = u + w;
    const mv = (d * 0.12) / cols;
    const mz = (h * 0.18) / rows;
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        const cv = v + (d / cols) * c + mv;
        const cd = d / cols - mv * 2;
        const cz = z + (h / rows) * r + mz;
        const ch = h / rows - mz * 2;
        this.poly(
          [
            [u1, cv, cz],
            [u1, cv + cd, cz],
            [u1, cv + cd, cz + ch],
            [u1, cv, cz + ch],
          ],
          color,
          frame,
          2,
        );
        this.gridLines([u1, cv, cz], [0, cd, 0], [0, 0, ch], 3, 2);
      }
    }
  }

  /** Thin grid lines inside a parallelogram (window panes) */
  private gridLines(origin: V3, a: V3, b: V3, na: number, nb: number) {
    const color = "rgba(10, 30, 60, 0.55)";
    for (let i = 1; i < na; i++) {
      const f = i / na;
      this.line(
        [origin[0] + a[0] * f, origin[1] + a[1] * f, origin[2] + a[2] * f],
        [origin[0] + a[0] * f + b[0], origin[1] + a[1] * f + b[1], origin[2] + a[2] * f + b[2]],
        color,
        1,
      );
    }
    for (let i = 1; i < nb; i++) {
      const f = i / nb;
      this.line(
        [origin[0] + b[0] * f, origin[1] + b[1] * f, origin[2] + b[2] * f],
        [origin[0] + b[0] * f + a[0], origin[1] + b[1] * f + a[1], origin[2] + b[2] * f + a[2]],
        color,
        1,
      );
    }
  }

  /** Plank lines on a flat roof (the brown wooden tops of the original buildings) */
  planks(u: number, v: number, w: number, d: number, z: number, color = "#7a3f1d") {
    this.poly(
      [
        [u, v, z],
        [u + w, v, z],
        [u + w, v + d, z],
        [u, v + d, z],
      ],
      color,
      OUTLINE,
      3,
    );
    const inset = Math.min(w, d) * 0.08;
    this.poly(
      [
        [u + inset, v + inset, z],
        [u + w - inset, v + inset, z],
        [u + w - inset, v + d - inset, z],
        [u + inset, v + d - inset, z],
      ],
      shade(color, 0.85),
      "rgba(0,0,0,0.45)",
      1.5,
    );
    const lines = Math.max(4, Math.round(w * 9));
    for (let i = 1; i < lines; i++) {
      const uu = u + inset + ((w - inset * 2) * i) / lines;
      this.line([uu, v + inset, z], [uu, v + d - inset, z], "rgba(40, 15, 5, 0.35)", 1);
    }
  }

  /** A gable roof over a box, the ridge along u */
  gable(u: number, v: number, w: number, d: number, z: number, h: number, color: string) {
    const mid = v + d / 2;
    // Front left slope
    this.poly(
      [
        [u, v + d, z],
        [u + w, v + d, z],
        [u + w, mid, z + h],
        [u, mid, z + h],
      ],
      shade(color, 0.95),
    );
    // Gable end on the right
    this.poly(
      [
        [u + w, v, z],
        [u + w, v + d, z],
        [u + w, mid, z + h],
      ],
      shade(color, 0.7),
    );
    // Back slope (visible as a sliver)
    this.poly(
      [
        [u, v, z],
        [u + w, v, z],
        [u + w, mid, z + h],
        [u, mid, z + h],
      ],
      shade(color, 1.2),
    );
  }

  /** A saw-tooth factory roof */
  sawtooth(
    u: number,
    v: number,
    w: number,
    d: number,
    z: number,
    h: number,
    teeth: number,
    color: string,
    glass = "#8fc7ee",
  ) {
    const step = w / teeth;
    for (let i = 0; i < teeth; i++) {
      const a = u + step * i;
      this.poly(
        [
          [a, v, z],
          [a, v + d, z],
          [a + step, v + d, z + h],
          [a + step, v, z + h],
        ],
        shade(color, 1.1),
      );
      this.poly(
        [
          [a + step, v, z],
          [a + step, v + d, z],
          [a + step, v + d, z + h],
          [a + step, v, z + h],
        ],
        glass,
      );
    }
  }

  ellipse(
    u: number,
    v: number,
    z: number,
    r: number,
    fill: string | null,
    outline: string | null = OUTLINE,
    lineWidth = 3,
  ) {
    const [x, y] = this.pt(u, v, z);
    const g = this.g;
    g.beginPath();
    g.ellipse(x, y, r * CIRCLE_RX, r * CIRCLE_RY, 0, 0, Math.PI * 2);
    if (fill !== null) {
      g.fillStyle = fill;
      g.fill();
    }
    if (outline !== null) {
      g.strokeStyle = outline;
      g.lineWidth = lineWidth;
      g.stroke();
    }
  }

  /** A vertical cylinder (tank, chimney, tower) standing at (u, v) */
  cylinder(
    u: number,
    v: number,
    r: number,
    h: number,
    color: string,
    z = 0,
    top: "flat" | "dome" | "cone" | "open" = "flat",
    topColor?: string,
  ) {
    const g = this.g;
    const [x, y0] = this.pt(u, v, z);
    const y1 = y0 - h;
    const rx = r * CIRCLE_RX;
    const ry = r * CIRCLE_RY;
    const gradient = g.createLinearGradient(x - rx, 0, x + rx, 0);
    gradient.addColorStop(0, shade(color, 1.05));
    gradient.addColorStop(0.35, shade(color, 1.15));
    gradient.addColorStop(1, shade(color, 0.62));
    g.beginPath();
    g.moveTo(x - rx, y1);
    g.lineTo(x - rx, y0);
    g.ellipse(x, y0, rx, ry, 0, Math.PI, 0, true);
    g.lineTo(x + rx, y1);
    g.ellipse(x, y1, rx, ry, 0, 0, Math.PI, false);
    g.closePath();
    g.fillStyle = gradient;
    g.fill();
    g.strokeStyle = OUTLINE;
    g.lineWidth = 3;
    g.stroke();

    const capColor = topColor ?? shade(color, 1.2);
    if (top === "dome") {
      g.beginPath();
      g.ellipse(x, y1, rx, ry, 0, 0, Math.PI, false);
      g.ellipse(x, y1, rx, rx * 0.85, 0, Math.PI, 0, false);
      g.closePath();
      const dome = g.createLinearGradient(x - rx, 0, x + rx, 0);
      dome.addColorStop(0, shade(capColor, 1.1));
      dome.addColorStop(1, shade(capColor, 0.7));
      g.fillStyle = dome;
      g.fill();
      g.stroke();
    } else if (top === "cone") {
      g.beginPath();
      g.moveTo(x - rx, y1);
      g.ellipse(x, y1, rx, ry, 0, Math.PI, 0, true);
      g.lineTo(x, y1 - rx * 0.9);
      g.closePath();
      g.fillStyle = capColor;
      g.fill();
      g.stroke();
    } else {
      g.beginPath();
      g.ellipse(x, y1, rx, ry, 0, 0, Math.PI * 2);
      g.fillStyle = top === "open" ? "#222" : capColor;
      g.fill();
      g.stroke();
      if (top === "open") {
        g.beginPath();
        g.ellipse(x, y1 + ry * 0.15, rx * 0.75, ry * 0.7, 0, 0, Math.PI * 2);
        g.fillStyle = "#0d0d0d";
        g.fill();
      }
    }
  }

  /** A brick chimney with smoke */
  chimney(u: number, v: number, h: number, z = 0, r = 0.09, color = "#9b3b2f") {
    this.cylinder(u, v, r, h, color, z, "open");
    const [x, y] = this.pt(u, v, z);
    // Brick rings
    const g = this.g;
    g.strokeStyle = "rgba(30,0,0,0.35)";
    g.lineWidth = 1;
    for (let i = 1; i < h / 14; i++) {
      g.beginPath();
      g.ellipse(x, y - i * 14, r * CIRCLE_RX, r * CIRCLE_RY, 0, 0, Math.PI, false);
      g.stroke();
    }
    this.attach("smoke", u, v, z + h + 6, 1);
  }

  /** A dark rectangular door on the front left face */
  doorLeft(u: number, v: number, z: number, w: number, h: number, color = "#3a3a3a") {
    this.poly(
      [
        [u, v, z],
        [u + w, v, z],
        [u + w, v, z + h],
        [u, v, z + h],
      ],
      color,
      OUTLINE,
      2,
    );
  }

  doorRight(u: number, v: number, z: number, d: number, h: number, color = "#3a3a3a") {
    this.poly(
      [
        [u, v, z],
        [u, v + d, z],
        [u, v + d, z + h],
        [u, v, z + h],
      ],
      color,
      OUTLINE,
      2,
    );
  }

  /** Small stacked blocks (crates, bricks, logs) */
  pile(u: number, v: number, w: number, d: number, h: number, color: string, z = 0) {
    this.box(u, v, w, d, h, color, z);
  }

  /** A simple round tree */
  tree(u: number, v: number, scale = 1, color = "#2f8a3a") {
    const [x, y] = this.pt(u, v, 0);
    const g = this.g;
    g.fillStyle = "#6b4423";
    g.strokeStyle = OUTLINE;
    g.lineWidth = 2;
    g.beginPath();
    g.rect(x - 4 * scale, y - 26 * scale, 8 * scale, 26 * scale);
    g.fill();
    g.stroke();
    g.beginPath();
    g.ellipse(x, y - 42 * scale, 24 * scale, 26 * scale, 0, 0, Math.PI * 2);
    const gradient = g.createRadialGradient(x - 8 * scale, y - 52 * scale, 4, x, y - 42 * scale, 28 * scale);
    gradient.addColorStop(0, shade(color, 1.35));
    gradient.addColorStop(1, shade(color, 0.8));
    g.fillStyle = gradient;
    g.fill();
    g.lineWidth = 2.5;
    g.stroke();
  }

  /** A lattice mast (pylons, antennas, headframes) between two points */
  lattice(u: number, v: number, z0: number, z1: number, width: number, color = "#4a4f57") {
    const [x, ya] = this.pt(u, v, z0);
    const [, yb] = this.pt(u, v, z1);
    const g = this.g;
    const wb = width;
    const wt = width * 0.35;
    g.strokeStyle = color;
    g.lineWidth = 3;
    g.beginPath();
    g.moveTo(x - wb, ya);
    g.lineTo(x - wt, yb);
    g.moveTo(x + wb, ya);
    g.lineTo(x + wt, yb);
    const steps = Math.max(3, Math.round((ya - yb) / 18));
    for (let i = 0; i < steps; i++) {
      const f0 = i / steps;
      const f1 = (i + 1) / steps;
      const w0 = wb + (wt - wb) * f0;
      const w1 = wb + (wt - wb) * f1;
      const y0 = ya + (yb - ya) * f0;
      const y1 = ya + (yb - ya) * f1;
      g.moveTo(x - w0, y0);
      g.lineTo(x + w1, y1);
      g.moveTo(x + w0, y0);
      g.lineTo(x - w1, y1);
    }
    g.stroke();
  }

  text(u: number, v: number, z: number, text: string, color: string, size = 28) {
    const [x, y] = this.pt(u, v, z);
    const g = this.g;
    g.font = `bold ${size}px Georgia, serif`;
    g.textAlign = "center";
    g.textBaseline = "middle";
    g.fillStyle = color;
    g.fillText(text, x, y);
  }
}

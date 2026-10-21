import { createCanvas, OUTLINE, shade, TILE_H, TILE_W } from "./iso";

// Pipes in the style of the generated art: chunky shaded tubes lying on the ground, with brass collars where they
// meet the next tile, coloured by tier. A shape is the sides it connects to: u (x, y-1), r (x+1), d (y+1), l (x-1).

export const PIPE_COLORS: Record<number, string> = {
  1: "#8f969e",
  2: "#c8793c",
  3: "#5f7286",
  4: "#8fd3f0",
  5: "#a77bff",
};

const CENTRE: [number, number] = [TILE_W / 2, TILE_H / 2];

/** Where a side's edge midpoint is in the tile's 192×96 box (the tube ends just past it, into the next tile) */
const EDGES: Record<string, [number, number]> = {
  u: [TILE_W * 0.75 + 2, TILE_H * 0.25 - 1],
  r: [TILE_W * 0.75 + 2, TILE_H * 0.75 + 1],
  d: [TILE_W * 0.25 - 2, TILE_H * 0.75 + 1],
  l: [TILE_W * 0.25 - 2, TILE_H * 0.25 - 1],
};

/** The canvas is taller than the tile so the tube can sit on the ground with its shadow: TILE_H plus this on top */
export const PIPE_LIFT = 16;

export function drawPipe(shape: string, tier: number): HTMLCanvasElement {
  const canvas = createCanvas(TILE_W, TILE_H + PIPE_LIFT);
  const g = canvas.getContext("2d")!;
  const base = PIPE_COLORS[tier] ?? PIPE_COLORS[1]!;
  const sides = [...shape].filter((t) => t in EDGES);
  const [cx, cy] = [CENTRE[0], CENTRE[1] + PIPE_LIFT];
  const ends = sides.map((t) => [EDGES[t]![0], EDGES[t]![1] + PIPE_LIFT] as [number, number]);
  const raise = 9;
  g.lineCap = "round";
  g.lineJoin = "round";

  const stroke = (width: number, color: string, dy = 0) => {
    g.strokeStyle = color;
    g.lineWidth = width;
    for (const [x, y] of ends) {
      g.beginPath();
      g.moveTo(cx, cy - raise + dy);
      g.lineTo(x, y - raise + dy);
      g.stroke();
    }
    if (ends.length === 0) {
      g.beginPath();
      g.moveTo(cx - 0.1, cy - raise + dy);
      g.lineTo(cx + 0.1, cy - raise + dy);
      g.stroke();
    }
  };

  // A soft shadow on the ground, the outline, the tube, its top lit from above and a glint
  g.save();
  g.filter = "blur(3px)";
  stroke(24, "rgba(20, 24, 18, 0.32)", raise + 3);
  g.restore();
  stroke(25, OUTLINE);
  stroke(19, shade(base, 0.72));
  stroke(14, base, -2);
  stroke(6, shade(base, 1.35), -6);
  stroke(2.5, "rgba(255,255,255,0.65)", -8);

  // Brass collars where the pipe enters the next tile, a fitting where three or four meet
  for (const [x, y] of ends) {
    const t = 0.78;
    const px = cx + (x - cx) * t;
    const py = cy - raise + (y - cy) * t;
    const angle = Math.atan2(y - cy, x - cx);
    g.save();
    g.translate(px, py);
    g.rotate(angle);
    g.fillStyle = OUTLINE;
    g.fillRect(-4.5, -14, 9, 28);
    const brass = g.createLinearGradient(0, -12, 0, 12);
    brass.addColorStop(0, "#f2d27a");
    brass.addColorStop(0.5, "#c9963a");
    brass.addColorStop(1, "#7d5a1f");
    g.fillStyle = brass;
    g.fillRect(-2.5, -12, 5, 24);
    g.restore();
  }
  if (ends.length >= 3 || ends.length === 0) {
    const r = 14;
    const fitting = g.createRadialGradient(cx - 4, cy - raise - 6, 2, cx, cy - raise, r);
    fitting.addColorStop(0, shade(base, 1.4));
    fitting.addColorStop(1, shade(base, 0.7));
    g.beginPath();
    g.ellipse(cx, cy - raise, r, r * 0.8, 0, 0, Math.PI * 2);
    g.fillStyle = fitting;
    g.fill();
    g.strokeStyle = OUTLINE;
    g.lineWidth = 3;
    g.stroke();
    g.beginPath();
    g.arc(cx, cy - raise - 1, 3, 0, Math.PI * 2);
    g.fillStyle = "#c9963a";
    g.fill();
  }
  if (tier >= 4) {
    // Glass and superconductors glow a little
    g.globalCompositeOperation = "lighter";
    stroke(10, tier === 5 ? "rgba(167,123,255,0.25)" : "rgba(143,211,240,0.25)", -2);
  }
  return canvas;
}

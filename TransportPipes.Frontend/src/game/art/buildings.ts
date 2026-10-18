import { Iso, OUTLINE, shade, type Attachment } from "./iso";

// Procedural building art in the style of the original sprites: charcoal walls, brown plank roofs, blue gridded
// windows, red brick chimneys, thick outlines. Animated details (smoke, rotors, lights) are attachments the renderer
// animates.

export interface BuildingArt {
  canvas: HTMLCanvasElement;
  size: number;
  attachments: Attachment[];
}

const WALL = "#2e2f33";
const WALL_LIGHT = "#5d6068";
const ROOF = "#7a3f1d";
const BRICK = "#9b3b2f";
const STEEL = "#8c939c";
const CONCRETE = "#a7a39a";
const WHITE = "#e8e8e4";
const COPPER = "#d27a3c";
const GOLD = "#f2c230";

type Recipe = { size: number; extra: number; draw: (k: Iso) => void };

/** A dark office/factory block with windows and a plank roof, the look of the original Storage and Factory */
function block(
  k: Iso,
  u: number,
  v: number,
  w: number,
  d: number,
  h: number,
  opts: { rows?: number; cols?: number; wall?: string; roof?: string; z?: number; windows?: boolean } = {},
) {
  const z = opts.z ?? 0;
  const wall = opts.wall ?? WALL;
  k.box(u, v, w, d, h, { top: shade(wall, 1.3), left: wall, right: shade(wall, 0.8) }, z);
  if (opts.windows !== false) {
    k.windowsLeft(
      u,
      v,
      w,
      d,
      z + h * 0.12,
      h * 0.76,
      opts.rows ?? 1,
      opts.cols ?? Math.max(1, Math.round(w * 2)),
    );
    k.windowsRight(
      u,
      v,
      w,
      d,
      z + h * 0.12,
      h * 0.76,
      opts.rows ?? 1,
      opts.cols ?? Math.max(1, Math.round(d * 2)),
    );
  }
  k.planks(u, v, w, d, z + h, opts.roof ?? ROOF);
}

function tank(k: Iso, u: number, v: number, r: number, h: number, color: string, z = 0) {
  k.cylinder(u, v, r, h, color, z, "dome");
}

function smokeStack(k: Iso, u: number, v: number, h: number, z = 0) {
  k.chimney(u, v, h, z);
}

function rotor(k: Iso, u: number, v: number, z: number, size: number) {
  k.attach("rotor", u, v, z, size);
}

function solarRow(k: Iso, u: number, v: number, w: number, d: number, z: number) {
  // A tilted panel: front edge low, back edge high
  k.poly(
    [
      [u, v + d, z],
      [u + w, v + d, z],
      [u + w, v, z + 26],
      [u, v, z + 26],
    ],
    "#1f4fb8",
  );
  const cells = Math.max(2, Math.round(w * 6));
  for (let i = 1; i < cells; i++) {
    const uu = u + (w * i) / cells;
    k.line([uu, v + d, z], [uu, v, z + 26], "#9cc4ff", 1);
  }
  k.line([u, v + d / 2, z + 13], [u + w, v + d / 2, z + 13], "#9cc4ff", 1);
  k.line([u + w * 0.5, v + d * 0.5, 0], [u + w * 0.5, v + d * 0.5, z + 10], OUTLINE, 3);
}

function pumpjack(k: Iso, u: number, v: number) {
  k.pad(u - 0.3, v - 0.2, 0.6, 0.4, "#555");
  k.lattice(u, v, 0, 70, 12, "#3d3d3d");
  const [x, y] = k.pt(u, v, 72);
  const g = k.g;
  g.fillStyle = "#c9a227";
  g.strokeStyle = OUTLINE;
  g.lineWidth = 3;
  g.beginPath();
  g.moveTo(x - 60, y + 6);
  g.lineTo(x + 50, y - 10);
  g.lineTo(x + 52, y - 2);
  g.lineTo(x - 58, y + 14);
  g.closePath();
  g.fill();
  g.stroke();
  g.beginPath();
  g.ellipse(x - 64, y + 14, 12, 18, 0.2, 0, Math.PI * 2);
  g.fillStyle = "#444";
  g.fill();
  g.stroke();
}

function headframe(k: Iso, u: number, v: number, h: number) {
  k.lattice(u, v, 0, h, 20, "#5a4632");
  const [x, y] = k.pt(u, v, h);
  const g = k.g;
  g.beginPath();
  g.ellipse(x, y, 14, 14, 0, 0, Math.PI * 2);
  g.fillStyle = "#777";
  g.fill();
  g.strokeStyle = OUTLINE;
  g.lineWidth = 3;
  g.stroke();
  k.attach("wheel", u, v, h, 0.6);
}

function pit(k: Iso, u: number, v: number, w: number, d: number, ground: string, ore: string) {
  k.pad(u, v, w, d, shade(ground, 0.75));
  k.pad(u + w * 0.12, v + d * 0.12, w * 0.76, d * 0.76, shade(ground, 0.5), "rgba(0,0,0,0.5)");
  k.pad(u + w * 0.25, v + d * 0.25, w * 0.5, d * 0.5, shade(ground, 0.35), "rgba(0,0,0,0.5)");
  for (let i = 0; i < 6; i++) {
    const a = (i * 2.39) % 1;
    const b = (i * 0.618 + 0.3) % 1;
    k.ellipse(u + w * (0.25 + a * 0.5), v + d * (0.25 + b * 0.5), 0, 0.035, ore, OUTLINE, 1.5);
  }
}

function orePile(k: Iso, u: number, v: number, r: number, color: string) {
  const [x, y] = k.pt(u, v, 0);
  const g = k.g;
  g.beginPath();
  g.moveTo(x - r * 135, y);
  g.quadraticCurveTo(x - r * 40, y - r * 150, x, y - r * 110);
  g.quadraticCurveTo(x + r * 40, y - r * 150, x + r * 135, y);
  g.ellipse(x, y, r * 135, r * 60, 0, 0, Math.PI, false);
  g.closePath();
  const gradient = g.createLinearGradient(x - r * 135, 0, x + r * 135, 0);
  gradient.addColorStop(0, shade(color, 1.2));
  gradient.addColorStop(1, shade(color, 0.6));
  g.fillStyle = gradient;
  g.fill();
  g.strokeStyle = OUTLINE;
  g.lineWidth = 2.5;
  g.stroke();
}

function dome(k: Iso, u: number, v: number, r: number, color: string, z = 0) {
  k.cylinder(u, v, r, 8, shade(color, 0.85), z, "dome", color);
}

function sign(k: Iso, u: number, v: number, z: number, text: string, color: string, size = 26) {
  k.text(u, v, z, text, color, size);
}

function lights(k: Iso, u: number, v: number, z: number, color = "#ff4040") {
  k.attach("blink", u, v, z, 1, color);
}

const recipes: Record<string, Recipe> = {
  // ---------------------------------------------------------------- Energy
  hydroWheel: {
    size: 1,
    extra: 120,
    draw: (k) => {
      block(k, 0.15, 0.25, 0.5, 0.5, 70, { cols: 1, roof: "#6b4a2a" });
      k.attach("wheel", 0.78, 0.55, 55, 1, "#6b4a2a");
    },
  },
  coalPlant: {
    size: 2,
    extra: 230,
    draw: (k) => {
      orePile(k, 0.45, 1.55, 0.35, "#2d2d33");
      block(k, 0.3, 0.2, 1.1, 1.1, 95, { wall: "#5a2a24", cols: 3, roof: "#4a4a4a" });
      smokeStack(k, 1.55, 0.45, 190);
      smokeStack(k, 1.7, 0.9, 160);
    },
  },
  bigTurbine: {
    size: 2,
    extra: 420,
    draw: (k) => {
      k.pad(0.6, 0.6, 0.8, 0.8, CONCRETE);
      k.cylinder(1, 1, 0.07, 330, WHITE, 0, "flat");
      k.box(0.88, 0.93, 0.3, 0.14, 28, WHITE, 322);
      rotor(k, 0.86, 1.04, 336, 1.6);
    },
  },
  steamTurbine: {
    size: 2,
    extra: 200,
    draw: (k) => {
      tank(k, 0.45, 0.45, 0.28, 110, "#b8b4ac");
      block(k, 0.75, 0.3, 1.0, 1.2, 80, { wall: "#4b4f55", cols: 2 });
      k.cylinder(0.5, 1.35, 0.22, 60, "#c0392b", 0, "dome");
      k.attach("smoke", 0.45, 0.45, 160, 1.4);
    },
  },
  solarFarm: {
    size: 3,
    extra: 60,
    draw: (k) => {
      k.pad(0.1, 0.1, 2.8, 2.8, "#7c8a5a");
      for (let r = 0; r < 4; r++) {
        for (let c = 0; c < 2; c++) {
          solarRow(k, 0.25 + c * 1.35, 0.3 + r * 0.66, 1.15, 0.38, 14);
        }
      }
    },
  },
  geothermalPlant: {
    size: 2,
    extra: 170,
    draw: (k) => {
      k.pad(0.2, 0.2, 1.6, 1.6, "#8a5a3a");
      dome(k, 0.7, 0.7, 0.45, "#d0d6dc");
      k.cylinder(1.45, 0.6, 0.15, 90, STEEL, 0, "flat");
      k.box(0.9, 1.3, 0.8, 0.4, 50, "#6c7a89");
      k.attach("smoke", 1.45, 0.6, 100, 1.5);
      k.attach("glow", 0.7, 0.7, 30, 1, "#ff7b2e");
    },
  },
  fusionReactor: {
    size: 3,
    extra: 230,
    draw: (k) => {
      k.pad(0.2, 0.2, 2.6, 2.6, "#5d6470");
      k.cylinder(1.5, 1.5, 0.95, 90, "#d9dde3", 0, "dome", "#bfe4ff");
      k.cylinder(0.5, 2.5, 0.2, 60, STEEL, 0, "dome");
      k.cylinder(2.5, 0.5, 0.2, 60, STEEL, 0, "dome");
      k.attach("glow", 1.5, 1.5, 120, 2, "#7fe6ff");
      lights(k, 1.5, 1.5, 230, "#7fe6ff");
    },
  },

  // ---------------------------------------------------------------- Extraction
  copperMine: {
    size: 1,
    extra: 140,
    draw: (k) => {
      pit(k, 0.05, 0.05, 0.9, 0.9, "#b4622a", COPPER);
      headframe(k, 0.75, 0.3, 110);
    },
  },
  coalPit: {
    size: 1,
    extra: 90,
    draw: (k) => {
      pit(k, 0.05, 0.05, 0.9, 0.9, "#3c3530", "#111");
      k.box(0.65, 0.6, 0.3, 0.3, 30, "#5b4a3a");
      orePile(k, 0.3, 0.75, 0.15, "#1e1e22");
    },
  },
  quarry: {
    size: 2,
    extra: 160,
    draw: (k) => {
      pit(k, 0.1, 0.1, 1.8, 1.8, "#9aa1a8", "#cfd3d6");
      k.lattice(1.6, 0.4, 0, 130, 10, "#c9a227");
      k.line([1.6, 0.4, 130], [0.8, 0.9, 110], "#c9a227", 5);
      k.box(1.3, 1.3, 0.45, 0.45, 25, "#d3d6d9");
    },
  },
  sandDredger: {
    size: 1,
    extra: 110,
    draw: (k) => {
      k.box(0.15, 0.2, 0.7, 0.6, 20, "#c79a3b");
      k.box(0.3, 0.3, 0.35, 0.3, 40, WALL_LIGHT, 20);
      k.line([0.7, 0.4, 40], [1.0, 0.1, 95], "#c9a227", 5);
      orePile(k, 0.25, 0.85, 0.12, "#e6cf8f");
    },
  },
  lumberCamp: {
    size: 1,
    extra: 100,
    draw: (k) => {
      k.box(0.2, 0.15, 0.55, 0.45, 45, "#7a5230");
      k.gable(0.2, 0.15, 0.55, 0.45, 45, 30, "#5a3a20");
      for (let i = 0; i < 3; i++) {
        k.cylinder(0.3 + i * 0.16, 0.85, 0.07, 10, "#8a5a32", 0, "flat", "#d9b07a");
      }
    },
  },
  waterPump: {
    size: 1,
    extra: 120,
    draw: (k) => {
      k.box(0.15, 0.35, 0.4, 0.4, 45, "#5c7a96");
      k.planks(0.15, 0.35, 0.4, 0.4, 45, "#3d556b");
      k.cylinder(0.7, 0.3, 0.2, 80, "#6ec6ff", 0, "dome", "#9fdcff");
    },
  },
  deepMine: {
    size: 2,
    extra: 260,
    draw: (k) => {
      pit(k, 0.1, 0.9, 1.0, 1.0, "#6c5a48", "#c0603a");
      block(k, 1.0, 0.25, 0.75, 0.9, 70, { wall: "#4b3b2b", cols: 2 });
      headframe(k, 0.6, 0.5, 230);
    },
  },
  oilDerrick: {
    size: 1,
    extra: 120,
    draw: (k) => {
      k.pad(0.1, 0.1, 0.8, 0.8, "#3a3530");
      k.cylinder(0.75, 0.75, 0.13, 40, "#e2e2e2", 0, "flat");
      pumpjack(k, 0.4, 0.4);
    },
  },
  rareEarthExcavator: {
    size: 2,
    extra: 140,
    draw: (k) => {
      pit(k, 0.1, 0.1, 1.8, 1.8, "#6b4f7a", "#c08bff");
      k.box(0.9, 0.8, 0.6, 0.4, 35, "#e0b030");
      k.box(1.05, 0.85, 0.3, 0.3, 30, "#2a2a2a", 35);
      k.line([1.0, 0.9, 50], [0.5, 0.5, 90], "#e0b030", 7);
      k.line([0.5, 0.5, 90], [0.35, 0.6, 30], "#e0b030", 6);
      orePile(k, 1.55, 1.55, 0.2, "#9f6bd8");
    },
  },

  // ---------------------------------------------------------------- Processing
  smelter: {
    size: 1,
    extra: 170,
    draw: (k) => {
      block(k, 0.1, 0.2, 0.6, 0.65, 70, { wall: "#3a2a26", cols: 1 });
      k.attach("glow", 0.4, 0.85, 20, 0.6, "#ff8a2a");
      smokeStack(k, 0.82, 0.3, 130);
    },
  },
  brickworks: {
    size: 1,
    extra: 140,
    draw: (k) => {
      k.cylinder(0.4, 0.4, 0.3, 50, BRICK, 0, "dome", "#b8513f");
      smokeStack(k, 0.75, 0.25, 100);
      k.pile(0.55, 0.65, 0.3, 0.25, 22, "#b5523a");
      k.pile(0.15, 0.7, 0.25, 0.2, 15, "#b5523a");
    },
  },
  glassworks: {
    size: 1,
    extra: 170,
    draw: (k) => {
      block(k, 0.1, 0.3, 0.6, 0.6, 60, { wall: "#3b4a57", cols: 1 });
      k.cylinder(0.75, 0.3, 0.17, 110, "#8b6b4a", 0, "cone", "#6e5238");
      k.attach("glow", 0.4, 0.9, 25, 0.6, "#ffb347");
      k.attach("smoke", 0.75, 0.3, 150, 0.8);
    },
  },
  sawmill: {
    size: 1,
    extra: 100,
    draw: (k) => {
      k.box(0.1, 0.15, 0.7, 0.5, 40, "#8a5a32");
      k.gable(0.1, 0.15, 0.7, 0.5, 40, 26, "#5a3a20");
      k.attach("wheel", 0.45, 0.72, 25, 0.5, "#c0c0c0");
      for (let i = 0; i < 3; i++) {
        k.box(0.15 + i * 0.22, 0.75, 0.18, 0.18, 10 + i * 4, "#d9b07a");
      }
    },
  },
  bigFactory: {
    size: 2,
    extra: 200,
    draw: (k) => {
      k.box(0.15, 0.2, 1.6, 1.4, 70, { top: "#555", left: WALL, right: shade(WALL, 0.8) });
      k.windowsLeft(0.15, 0.2, 1.6, 1.4, 12, 46, 1, 5);
      k.windowsRight(0.15, 0.2, 1.6, 1.4, 12, 46, 1, 4);
      k.sawtooth(0.15, 0.2, 1.6, 1.4, 70, 30, 4, "#6b6b6b");
      smokeStack(k, 1.6, 0.35, 120, 70);
      smokeStack(k, 1.25, 0.3, 100, 70);
    },
  },
  steelMill: {
    size: 2,
    extra: 260,
    draw: (k) => {
      block(k, 0.9, 0.2, 0.95, 1.5, 75, { wall: "#3b3b40", cols: 2 });
      k.cylinder(0.5, 0.6, 0.3, 160, "#6d4c3d", 0, "cone", "#4a3328");
      k.attach("glow", 0.5, 0.95, 40, 1, "#ff7a1a");
      smokeStack(k, 0.3, 1.4, 200);
    },
  },
  wireMill: {
    size: 1,
    extra: 110,
    draw: (k) => {
      block(k, 0.1, 0.1, 0.65, 0.55, 55, { wall: "#3d3f45", cols: 1 });
      for (let i = 0; i < 2; i++) {
        k.cylinder(0.3 + i * 0.35, 0.82, 0.12, 22, COPPER, 0, "flat", "#f0a46a");
      }
    },
  },
  cementKiln: {
    size: 2,
    extra: 170,
    draw: (k) => {
      tank(k, 0.45, 0.45, 0.3, 140, CONCRETE);
      // A long rotary kiln lying along u on two supports, slightly sloped
      k.box(0.85, 1.2, 0.1, 0.25, 22, "#555");
      k.box(1.35, 1.2, 0.1, 0.25, 16, "#555");
      k.box(0.6, 1.18, 1.0, 0.3, 30, { top: "#d4a373", left: "#b9875a", right: "#8a6040" }, 18);
      for (let i = 1; i < 5; i++) {
        k.line([0.6 + i * 0.2, 1.48, 18], [0.6 + i * 0.2, 1.48, 48], "#6b4a2a", 3);
      }
      k.box(1.5, 1.3, 0.35, 0.35, 45, "#7b7369");
      k.attach("smoke", 1.67, 1.47, 60, 1);
    },
  },
  machineShop: {
    size: 2,
    extra: 140,
    draw: (k) => {
      block(k, 0.15, 0.2, 1.6, 1.4, 70, { wall: "#3a4048", cols: 4 });
      k.attach("wheel", 1.0, 1.0, 110, 0.8, "#c9a227");
    },
  },
  refinery: {
    size: 3,
    extra: 280,
    draw: (k) => {
      k.pad(0.15, 0.15, 2.7, 2.7, "#5e5a54");
      k.cylinder(0.7, 0.7, 0.18, 230, "#d9d4c8", 0, "dome");
      k.cylinder(1.25, 0.6, 0.14, 180, "#c9c2b4", 0, "dome");
      tank(k, 2.2, 0.8, 0.45, 70, WHITE);
      tank(k, 0.8, 2.1, 0.45, 70, WHITE);
      k.lattice(2.3, 2.2, 0, 150, 8, "#5a5a5a");
      k.attach("glow", 2.3, 2.2, 160, 0.8, "#ff9a2e");
      block(k, 1.6, 1.6, 0.7, 0.6, 45, { wall: "#4a4f55", cols: 1 });
    },
  },
  circuitFab: {
    size: 2,
    extra: 140,
    draw: (k) => {
      k.box(0.15, 0.15, 1.7, 1.6, 80, { top: "#f0f2f4", left: "#dfe3e8", right: "#b9c0c8" });
      k.windowsLeft(0.15, 0.15, 1.7, 1.6, 30, 30, 1, 6, "#3fa9f5", "#1b4f72");
      k.windowsRight(0.15, 0.15, 1.7, 1.6, 30, 30, 1, 6, "#3fa9f5", "#1b4f72");
      k.box(0.5, 0.5, 0.4, 0.4, 20, "#c8cdd2", 80);
      k.box(1.1, 0.6, 0.4, 0.4, 20, "#c8cdd2", 80);
      lights(k, 1.8, 0.2, 90, "#3fe07a");
    },
  },
  assemblyPlant: {
    size: 3,
    extra: 170,
    draw: (k) => {
      k.box(0.15, 0.15, 2.7, 2.4, 85, { top: "#666", left: "#3b4652", right: "#2c343d" });
      k.sawtooth(0.15, 0.15, 2.7, 2.4, 85, 32, 6, "#6f7680");
      for (let i = 0; i < 4; i++) {
        k.doorLeft(0.35 + i * 0.6, 2.55, 0, 0.4, 50, "#c9a227");
      }
      smokeStack(k, 2.6, 0.4, 100, 85);
    },
  },
  robotWorks: {
    size: 3,
    extra: 190,
    draw: (k) => {
      k.box(0.2, 0.2, 2.5, 2.5, 70, { top: "#e9ecef", left: "#cfd5db", right: "#a7b0ba" });
      k.windowsLeft(0.2, 0.2, 2.5, 2.5, 20, 34, 1, 6, "#47c3ff", "#174a66");
      k.windowsRight(0.2, 0.2, 2.5, 2.5, 20, 34, 1, 6, "#47c3ff", "#174a66");
      dome(k, 1.45, 1.45, 0.6, "#bfe9ff", 70);
      k.line([2.2, 0.6, 70], [2.0, 0.5, 140], "#f39c12", 8);
      k.line([2.0, 0.5, 140], [1.7, 0.7, 120], "#f39c12", 7);
      lights(k, 1.45, 1.45, 160, "#47c3ff");
    },
  },
  superconductorLab: {
    size: 2,
    extra: 170,
    draw: (k) => {
      block(k, 0.2, 0.9, 1.0, 0.85, 55, { wall: "#e1e6ea", cols: 2 });
      dome(k, 0.75, 0.55, 0.4, "#d6f3ff");
      k.cylinder(1.5, 0.5, 0.18, 90, "#9fe2ff", 0, "dome");
      k.cylinder(1.55, 1.2, 0.18, 70, "#9fe2ff", 0, "dome");
      k.attach("glow", 1.5, 0.5, 60, 0.7, "#9fe2ff");
    },
  },

  // ---------------------------------------------------------------- Logistics
  valve: {
    size: 1,
    extra: 70,
    draw: (k) => {
      k.cylinder(0.5, 0.5, 0.16, 34, STEEL, 0, "flat");
      k.ellipse(0.5, 0.5, 52, 0.17, null, "#c0392b", 6);
    },
  },
  priorityValve: {
    size: 1,
    extra: 80,
    draw: (k) => {
      k.cylinder(0.5, 0.5, 0.16, 34, STEEL, 0, "flat");
      k.ellipse(0.5, 0.5, 52, 0.17, null, "#2e86de", 6);
      sign(k, 0.5, 0.5, 70, "1", "#2e86de", 24);
    },
  },
  pumpStation: {
    size: 1,
    extra: 110,
    draw: (k) => {
      k.box(0.2, 0.2, 0.6, 0.6, 45, "#3f6e8c");
      k.planks(0.2, 0.2, 0.6, 0.6, 45, "#2f5168");
      k.cylinder(0.5, 0.5, 0.12, 30, STEEL, 45, "dome");
      k.attach("wheel", 0.5, 0.5, 85, 0.4, "#c9a227");
    },
  },
  tollMeter: {
    size: 1,
    extra: 110,
    draw: (k) => {
      k.cylinder(0.5, 0.5, 0.15, 30, STEEL, 0, "flat");
      k.box(0.4, 0.4, 0.2, 0.2, 50, "#c9a227", 30);
      sign(k, 0.5, 0.5, 105, "%", OUTLINE, 22);
    },
  },
  warehouse: {
    size: 2,
    extra: 170,
    draw: (k) => {
      block(k, 0.15, 0.15, 1.7, 1.6, 110, { rows: 2, cols: 4 });
      k.doorLeft(0.6, 1.75, 0, 0.5, 45, "#6b6b6b");
    },
  },
  silo: {
    size: 2,
    extra: 260,
    draw: (k) => {
      k.cylinder(1.4, 0.6, 0.4, 200, CONCRETE, 0, "cone", "#8e8a82");
      k.cylinder(0.6, 1.4, 0.4, 200, CONCRETE, 0, "cone", "#8e8a82");
      k.line([0.6, 1.4, 200], [1.4, 0.6, 200], "#5a5a5a", 5);
    },
  },
  cargoDepot: {
    size: 2,
    extra: 140,
    draw: (k) => {
      k.pad(0.1, 0.1, 1.8, 1.8, "#6b6b6b");
      block(k, 0.2, 0.2, 0.9, 0.8, 70, { cols: 2 });
      const colors = ["#c0392b", "#2e86de", "#27ae60", "#f39c12"];
      colors.forEach((color, i) =>
        k.box(1.2 + (i % 2) * 0.35, 0.3 + Math.floor(i / 2) * 0.55, 0.3, 0.5, 28, color),
      );
      k.box(0.4, 1.3, 0.7, 0.35, 30, "#e2e2e2");
      k.box(1.1, 1.3, 0.25, 0.35, 32, "#c0392b");
    },
  },
  powerLinePylon: {
    size: 1,
    extra: 220,
    draw: (k) => {
      k.lattice(0.5, 0.5, 0, 200, 22, "#5a5f68");
      k.line([0.25, 0.75, 170], [0.75, 0.25, 170], "#5a5f68", 4);
      k.line([0.3, 0.7, 140], [0.7, 0.3, 140], "#5a5f68", 4);
    },
  },

  // ---------------------------------------------------------------- Commerce
  marketHall: {
    size: 2,
    extra: 170,
    draw: (k) => {
      block(k, 0.2, 0.2, 1.6, 1.5, 70, { wall: "#6b4e2e", cols: 4 });
      k.gable(0.2, 0.2, 1.6, 1.5, 70, 50, "#b83b2c");
      for (let i = 0; i < 4; i++) {
        k.poly(
          [
            [0.25 + i * 0.4, 1.7, 40],
            [0.6 + i * 0.4, 1.7, 40],
            [0.6 + i * 0.4, 1.9, 25],
            [0.25 + i * 0.4, 1.9, 25],
          ],
          i % 2 === 0 ? "#f2f2f2" : "#e74c3c",
          OUTLINE,
          2,
        );
      }
    },
  },
  exchangeOffice: {
    size: 1,
    extra: 140,
    draw: (k) => {
      block(k, 0.15, 0.15, 0.7, 0.7, 90, { rows: 2, cols: 1, wall: "#2c3e50" });
      k.box(0.2, 0.86, 0.6, 0.05, 18, "#111", 60);
      sign(k, 0.5, 0.88, 69, "▲▼", "#3fe07a", 14);
    },
  },
  showroom: {
    size: 2,
    extra: 140,
    draw: (k) => {
      k.box(0.2, 0.2, 1.6, 1.6, 75, { top: "#dfe6ec", left: "#9fd6f2", right: "#7bb8d8" });
      k.windowsLeft(0.2, 0.2, 1.6, 1.6, 6, 62, 1, 4, "#bfe9ff", "#4a6a7a");
      k.windowsRight(0.2, 0.2, 1.6, 1.6, 6, 62, 1, 4, "#bfe9ff", "#4a6a7a");
      sign(k, 1.0, 1.0, 105, "★", GOLD, 40);
    },
  },
  bank: {
    size: 2,
    extra: 180,
    draw: (k) => {
      k.box(0.2, 0.2, 1.6, 1.6, 20, "#d8d2c4");
      k.box(0.3, 0.3, 1.4, 1.2, 90, "#ece6d8", 20);
      for (let i = 0; i < 4; i++) {
        k.cylinder(0.45 + i * 0.36, 1.62, 0.06, 80, "#f5f0e4", 20, "flat");
      }
      k.gable(0.3, 0.3, 1.4, 1.4, 110, 40, "#c9c2b0");
      sign(k, 1.0, 1.0, 175, "$", GOLD, 34);
    },
  },

  // ---------------------------------------------------------------- Company
  hq: {
    size: 2,
    extra: 300,
    draw: (k) => {
      k.pad(0.1, 0.1, 1.8, 1.8, "#b9b2a5");
      block(k, 0.3, 0.3, 1.4, 1.4, 90, { rows: 2, cols: 3 });
      block(k, 0.55, 0.55, 0.9, 0.9, 110, { rows: 2, cols: 2, z: 90 });
      k.line([1.0, 1.0, 200], [1.0, 1.0, 280], OUTLINE, 4);
      k.attach("flag", 1.0, 1.0, 278, 1);
      k.doorLeft(0.85, 1.7, 0, 0.3, 40, "#c9a227");
    },
  },
  lab: {
    size: 1,
    extra: 150,
    draw: (k) => {
      block(k, 0.15, 0.3, 0.7, 0.55, 55, { wall: "#e3e7ea", cols: 1 });
      dome(k, 0.45, 0.55, 0.22, "#cfe9ff", 55);
      k.lattice(0.8, 0.25, 0, 120, 6, "#777");
      lights(k, 0.8, 0.25, 120);
    },
  },
  researchInstitute: {
    size: 2,
    extra: 200,
    draw: (k) => {
      block(k, 0.2, 0.6, 1.6, 1.1, 80, { wall: "#d9dde1", rows: 2, cols: 4 });
      k.cylinder(1.0, 0.4, 0.35, 60, "#cfd3d6", 0, "dome", "#e9f1f7");
      k.line([1.0, 0.4, 120], [1.25, 0.2, 165], "#555", 8);
    },
  },
  engineerWorkshop: {
    size: 1,
    extra: 110,
    draw: (k) => {
      k.box(0.15, 0.2, 0.7, 0.55, 45, "#7b8a99");
      k.gable(0.15, 0.2, 0.7, 0.55, 45, 28, "#4b5a69");
      k.attach("wheel", 0.5, 0.75, 25, 0.4, "#c9a227");
    },
  },
  weatherStation: {
    size: 1,
    extra: 190,
    draw: (k) => {
      k.box(0.2, 0.45, 0.45, 0.4, 35, WHITE);
      k.planks(0.2, 0.45, 0.45, 0.4, 35, "#9aa3ab");
      k.line([0.75, 0.3, 0], [0.75, 0.3, 160], "#555", 4);
      rotor(k, 0.75, 0.3, 165, 0.35);
    },
  },
  recycler: {
    size: 1,
    extra: 110,
    draw: (k) => {
      block(k, 0.15, 0.15, 0.6, 0.55, 50, { wall: "#2f6f4f", cols: 1 });
      k.box(0.2, 0.78, 0.2, 0.15, 22, "#27ae60");
      k.box(0.5, 0.78, 0.2, 0.15, 22, "#2e86de");
      sign(k, 0.45, 0.42, 85, "♻", "#3fe07a", 30);
    },
  },
  expeditionOffice: {
    size: 1,
    extra: 140,
    draw: (k) => {
      block(k, 0.2, 0.25, 0.6, 0.6, 60, { wall: "#6b4e2e", cols: 1 });
      k.line([0.8, 0.2, 0], [0.8, 0.2, 120], OUTLINE, 3);
      k.attach("flag", 0.8, 0.2, 118, 0.8, "#e67e22");
    },
  },
  blueprintOffice: {
    size: 1,
    extra: 120,
    draw: (k) => {
      block(k, 0.15, 0.15, 0.7, 0.7, 70, { rows: 2, cols: 1, roof: "#2e5c9a" });
    },
  },
  deepLift: {
    size: 2,
    extra: 240,
    draw: (k) => {
      k.pad(0.2, 0.2, 1.6, 1.6, "#4a4a4a");
      k.box(0.7, 0.7, 0.6, 0.6, 190, { top: "#777", left: "#5d646d", right: "#454b52" });
      k.windowsLeft(0.7, 0.7, 0.6, 0.6, 20, 160, 4, 1, "#f6c744", "#333");
      lights(k, 1.0, 1.0, 200, "#f6c744");
    },
  },
  magmaTap: {
    size: 2,
    extra: 160,
    draw: (k) => {
      k.pad(0.2, 0.2, 1.6, 1.6, "#3a2a24");
      k.ellipse(1.0, 1.0, 0, 0.5, "#ff6a00");
      k.cylinder(1.0, 1.0, 0.25, 90, "#4a4a4a", 0, "flat");
      k.attach("glow", 1.0, 1.0, 20, 1.5, "#ff6a00");
    },
  },
  vault: {
    size: 2,
    extra: 140,
    draw: (k) => {
      k.box(0.2, 0.2, 1.6, 1.6, 80, { top: "#9a9a9a", left: "#7a7a7a", right: "#5a5a5a" });
      k.ellipse(1.0, 1.8, 40, 0.3, "#c9a227");
    },
  },
  pneumaticPost: {
    size: 1,
    extra: 200,
    draw: (k) => {
      k.box(0.2, 0.2, 0.6, 0.6, 40, "#4a6070");
      k.cylinder(0.5, 0.5, 0.14, 140, "#bfe9ff", 40, "dome", "#e8f8ff");
      k.attach("spark", 0.5, 0.5, 120, 0.6, "#bfe9ff");
    },
  },
  terraformer: {
    size: 2,
    extra: 200,
    draw: (k) => {
      k.box(0.3, 0.4, 1.4, 1.2, 50, "#d9a43a");
      k.box(0.5, 0.5, 0.8, 0.6, 40, "#3a3a3a", 50);
      k.ellipse(1.2, 0.6, 140, 0.35, "#e0e6ea");
      k.line([1.2, 0.6, 90], [1.2, 0.6, 140], "#555", 6);
    },
  },
  quantumLink: {
    size: 2,
    extra: 230,
    draw: (k) => {
      k.pad(0.2, 0.2, 1.6, 1.6, "#3a3f4a");
      const [x, y] = k.pt(1.0, 1.0, 110);
      const g = k.g;
      g.beginPath();
      g.ellipse(x, y, 95, 110, 0, 0, Math.PI * 2);
      g.lineWidth = 22;
      g.strokeStyle = "#6c5ce7";
      g.stroke();
      g.lineWidth = 3;
      g.strokeStyle = OUTLINE;
      g.stroke();
      k.attach("glow", 1.0, 1.0, 110, 2, "#a29bfe");
    },
  },

  // ---------------------------------------------------------------- Decorations
  goldenMoily: {
    size: 1,
    extra: 190,
    draw: (k) => {
      k.box(0.25, 0.25, 0.5, 0.5, 20, "#c9c2b4");
      k.cylinder(0.5, 0.5, 0.09, 150, "#f6c744", 20, "dome", "#ffe08a");
      k.attach("rotor", 0.5, 0.5, 165, 1, "#f6c744");
      k.attach("spark", 0.5, 0.5, 120, 0.8, "#fff2b0");
    },
  },
  crystalObelisk: {
    size: 1,
    extra: 170,
    draw: (k) => {
      k.box(0.2, 0.2, 0.6, 0.6, 16, "#5d6670");
      k.box(0.36, 0.36, 0.28, 0.28, 120, { top: "#d6b8ff", left: "#9a6bdc", right: "#7a4fbf" }, 16);
      k.cylinder(0.5, 0.5, 0.2, 4, "#b08ae8", 136, "cone", "#e6d4ff");
      k.attach("glow", 0.5, 0.5, 90, 0.9, "#d6b8ff");
    },
  },
  goldenGear: {
    size: 1,
    extra: 140,
    draw: (k) => {
      k.box(0.25, 0.4, 0.5, 0.2, 18, "#c9c2b4");
      const [x, y] = k.pt(0.5, 0.5, 70);
      const g = k.g;
      g.fillStyle = "#f6c744";
      g.strokeStyle = OUTLINE;
      g.lineWidth = 3;
      g.beginPath();
      for (let i = 0; i < 24; i++) {
        const angle = (i / 24) * Math.PI * 2;
        const r = i % 2 === 0 ? 50 : 40;
        g.lineTo(x + Math.cos(angle) * r, y + Math.sin(angle) * r);
      }
      g.closePath();
      g.fill();
      g.stroke();
      g.fillStyle = "#b8862f";
      g.beginPath();
      g.arc(x, y, 14, 0, Math.PI * 2);
      g.fill();
      g.stroke();
      k.attach("spark", 0.5, 0.5, 110, 0.7, "#fff2b0");
    },
  },
  tree: {
    size: 1,
    extra: 60,
    draw: (k) => k.tree(0.5, 0.5, 1.3),
  },
  flowerBed: {
    size: 1,
    extra: 20,
    draw: (k) => {
      k.box(0.15, 0.15, 0.7, 0.7, 10, "#6b4423");
      const colors = ["#e74c3c", "#f1c40f", "#9b59b6", "#ff7eb6", "#ffffff"];
      for (let i = 0; i < 14; i++) {
        const u = 0.22 + ((i * 0.37) % 0.56);
        const v = 0.22 + ((i * 0.61) % 0.56);
        k.ellipse(u, v, 14, 0.04, colors[i % colors.length]!, OUTLINE, 1);
      }
    },
  },
  lamp: {
    size: 1,
    extra: 120,
    draw: (k) => {
      k.line([0.5, 0.5, 0], [0.5, 0.5, 100], "#2f2f2f", 6);
      k.ellipse(0.5, 0.5, 104, 0.08, "#fff3b0");
      k.attach("glow", 0.5, 0.5, 104, 0.5, "#fff3b0");
    },
  },
  fountain: {
    size: 1,
    extra: 80,
    draw: (k) => {
      k.cylinder(0.5, 0.5, 0.4, 16, "#c9c2b4", 0, "flat", "#6ec6ff");
      k.cylinder(0.5, 0.5, 0.08, 40, "#c9c2b4", 16, "flat");
      k.attach("spark", 0.5, 0.5, 60, 0.8, "#bfe9ff");
    },
  },
  statue: {
    size: 1,
    extra: 140,
    draw: (k) => {
      k.box(0.3, 0.3, 0.4, 0.4, 35, "#c9c2b4");
      const [x, y] = k.pt(0.5, 0.5, 35);
      const g = k.g;
      g.fillStyle = "#b08d57";
      g.strokeStyle = OUTLINE;
      g.lineWidth = 2.5;
      g.beginPath();
      g.ellipse(x, y - 78, 10, 11, 0, 0, Math.PI * 2);
      g.fill();
      g.stroke();
      g.beginPath();
      g.moveTo(x - 14, y - 66);
      g.lineTo(x + 14, y - 66);
      g.lineTo(x + 10, y - 20);
      g.lineTo(x - 10, y - 20);
      g.closePath();
      g.fill();
      g.stroke();
      g.beginPath();
      g.moveTo(x + 12, y - 62);
      g.lineTo(x + 30, y - 92);
      g.stroke();
    },
  },
};

/** A generic building for anything without its own recipe */
function generic(size: number): Recipe {
  return {
    size,
    extra: 120,
    draw: (k) => block(k, 0.15, 0.15, size - 0.3, size - 0.3, 70, { cols: 2 }),
  };
}

export function hasRecipe(type: string): boolean {
  return type in recipes;
}

/**
 * Draws the moving parts in a resting pose. In the game the renderer animates them on top (rotors, wheels, flags),
 * for thumbnails they are part of the picture.
 */
export function paintAttachments(
  g: CanvasRenderingContext2D,
  attachments: Attachment[],
  height: number,
  flagColor = "#e74c3c",
) {
  for (const a of attachments) {
    const x = a.x;
    const y = a.y + height;
    g.save();
    g.translate(x, y);
    g.strokeStyle = OUTLINE;
    g.lineWidth = 2.5;
    switch (a.kind) {
      case "rotor":
        drawRotor(g, a.size * 70, 0.3);
        break;
      case "wheel":
        drawWheel(g, a.size * 40, a.color ?? "#6b4a2a", 0);
        break;
      case "flag":
        drawFlag(g, a.size * 34, a.color ?? flagColor, 0);
        break;
      case "glow":
      case "blink":
        g.beginPath();
        g.arc(0, 0, a.kind === "glow" ? 6 * a.size : 4, 0, Math.PI * 2);
        g.fillStyle = a.color ?? "#ff4040";
        g.fill();
        break;
    }
    g.restore();
  }
}

/** Three white blades around the origin */
export function drawRotor(g: CanvasRenderingContext2D, length: number, angle: number) {
  for (let i = 0; i < 3; i++) {
    const a = angle + (i * Math.PI * 2) / 3;
    g.save();
    g.rotate(a);
    g.beginPath();
    g.moveTo(0, -4);
    g.quadraticCurveTo(length * 0.5, -10, length, -2);
    g.lineTo(length, 2);
    g.quadraticCurveTo(length * 0.5, 6, 0, 4);
    g.closePath();
    g.fillStyle = "#f4f4f2";
    g.fill();
    g.strokeStyle = OUTLINE;
    g.lineWidth = 2;
    g.stroke();
    g.restore();
  }
  g.beginPath();
  g.arc(0, 0, 6, 0, Math.PI * 2);
  g.fillStyle = "#cfd3d6";
  g.fill();
  g.stroke();
}

/** A spoked wheel (water wheel, saw, gear) seen from the side */
export function drawWheel(g: CanvasRenderingContext2D, radius: number, color: string, angle: number) {
  g.beginPath();
  g.ellipse(0, 0, radius * 0.55, radius, 0, 0, Math.PI * 2);
  g.fillStyle = shade(color, 0.8);
  g.fill();
  g.lineWidth = 3;
  g.strokeStyle = OUTLINE;
  g.stroke();
  g.lineWidth = 2;
  for (let i = 0; i < 8; i++) {
    const a = angle + (i * Math.PI) / 4;
    g.beginPath();
    g.moveTo(0, 0);
    g.lineTo(Math.cos(a) * radius * 0.55, Math.sin(a) * radius);
    g.strokeStyle = shade(color, 1.3);
    g.stroke();
  }
  g.beginPath();
  g.arc(0, 0, 4, 0, Math.PI * 2);
  g.fillStyle = OUTLINE;
  g.fill();
}

/** A waving flag on a pole top at the origin */
export function drawFlag(g: CanvasRenderingContext2D, width: number, color: string, phase: number) {
  const h = width * 0.6;
  g.beginPath();
  g.moveTo(0, 0);
  for (let i = 0; i <= 8; i++) {
    const x = (width * i) / 8;
    g.lineTo(x, Math.sin(phase + i * 0.8) * 3 * (i / 8));
  }
  for (let i = 8; i >= 0; i--) {
    const x = (width * i) / 8;
    g.lineTo(x, h + Math.sin(phase + i * 0.8) * 3 * (i / 8));
  }
  g.closePath();
  g.fillStyle = color;
  g.fill();
  g.strokeStyle = OUTLINE;
  g.lineWidth = 2;
  g.stroke();
}

export function drawBuilding(type: string, size: number, withMovingParts = false): BuildingArt {
  const recipe = recipes[type] ?? generic(size);
  const iso = new Iso(recipe.size, recipe.extra);
  recipe.draw(iso);
  if (withMovingParts) {
    paintAttachments(iso.g, iso.attachments, iso.height);
  }
  return { canvas: iso.canvas, size: recipe.size, attachments: iso.attachments };
}

/**
 * The gate of an estate: two posts along the estate's edge with a barrier arm, raised when the gate is open.
 * alongU: the edge runs along u (the top and bottom sides of an estate), otherwise along v.
 */
export function drawGate(
  policy: "open" | "closed" | "contract" | "corporation",
  alongU: boolean,
): BuildingArt {
  const open = policy !== "closed";
  const k = new Iso(1, 110);
  const a: [number, number] = alongU ? [0.08, 0.5] : [0.5, 0.08];
  const b: [number, number] = alongU ? [0.92, 0.5] : [0.5, 0.92];
  k.pad(alongU ? 0 : 0.3, alongU ? 0.3 : 0, alongU ? 1 : 0.4, alongU ? 0.4 : 1, "#6d6a64");
  for (let i = 0; i < 4; i++) {
    // Zebra marking on the ground between the posts
    const f = 0.15 + i * 0.2;
    const u = alongU ? f : 0.32;
    const v = alongU ? 0.32 : f;
    k.pad(u, v, alongU ? 0.1 : 0.36, alongU ? 0.36 : 0.1, i % 2 === 0 ? "#e8c547" : "#2a2a2a", null);
  }
  k.cylinder(a[0], a[1], 0.06, 64, "#c9a227", 0, "flat", "#e0be4a");
  if (open) {
    k.line([a[0], a[1], 58], [a[0] + (alongU ? 0.08 : 0), a[1] + (alongU ? 0 : 0.08), 150], "#e74c3c", 7);
    k.line(
      [a[0] + (alongU ? 0.02 : 0), a[1] + (alongU ? 0 : 0.02), 85],
      [a[0] + (alongU ? 0.05 : 0), a[1] + (alongU ? 0 : 0.05), 115],
      "#ffffff",
      7,
    );
  } else {
    k.line([a[0], a[1], 55], [b[0], b[1], 55], "#e74c3c", 7);
    k.line(
      [a[0] + (b[0] - a[0]) * 0.3, a[1] + (b[1] - a[1]) * 0.3, 55],
      [a[0] + (b[0] - a[0]) * 0.45, a[1] + (b[1] - a[1]) * 0.45, 55],
      "#ffffff",
      7,
    );
    k.line(
      [a[0] + (b[0] - a[0]) * 0.65, a[1] + (b[1] - a[1]) * 0.65, 55],
      [a[0] + (b[0] - a[0]) * 0.8, a[1] + (b[1] - a[1]) * 0.8, 55],
      "#ffffff",
      7,
    );
  }
  k.cylinder(b[0], b[1], 0.06, 64, "#c9a227", 0, "flat", "#e0be4a");
  const light = { open: "#3fe07a", contract: "#f6c744", corporation: "#6cb4ee", closed: "#ff4040" }[policy];
  k.attach("blink", b[0], b[1], 72, 0.6, light);
  return { canvas: k.canvas, size: 1, attachments: k.attachments };
}

/** The buildings of the hub city */
/** What a corporation HQ or a Wonder shows: tag, colour and level, or the Wonder and its stages */
export interface HubStructureLook {
  label: string | null;
  color: string | null;
  level: number;
  variant?: string | null;
  stages?: number;
}

/** Scaffolding around the part of a Wonder that is being built */
function scaffold(k: Iso, u: number, v: number, w: number, d: number, z: number, h: number) {
  for (const [pu, pv] of [
    [u, v + d],
    [u + w, v + d],
    [u + w, v],
  ] as const) {
    k.lattice(pu, pv, z, z + h, 10, "#b8862f");
  }
  k.line([u, v + d, z + h], [u + w, v + d, z + h], "#b8862f", 4);
  k.line([u + w, v + d, z + h], [u + w, v, z + h], "#b8862f", 4);
  k.line([u, v + d, z + h / 2], [u + w, v + d, z + h / 2], "#b8862f", 3);
  k.line([u + w, v + d, z + h / 2], [u + w, v, z + h / 2], "#b8862f", 3);
}

/** A hub billboard with a company's name (the Brand tech) */
function drawBillboard(look: HubStructureLook): BuildingArt {
  const k = new Iso(2, 2 * 90 + 200);
  const color = look.color ?? "#2e86de";
  k.pad(0.2, 0.2, 1.6, 1.6, "#c7b79a");
  k.line([0.5, 1.2, 0], [0.5, 1.2, 120], "#3a3f45", 6);
  k.line([1.5, 1.2, 0], [1.5, 1.2, 120], "#3a3f45", 6);
  k.box(0.2, 1.1, 1.6, 0.12, 90, { top: shade(color, 1.3), left: color, right: shade(color, 0.7) }, 110);
  k.box(0.2, 1.22, 1.6, 0.03, 6, "#f6c744", 196);
  if (look.label !== null) {
    k.text(1.0, 1.3, 160, look.label.length > 12 ? `${look.label.slice(0, 11)}…` : look.label, "#ffffff", 22);
  }
  k.attach("glow", 1.0, 1.25, 205, 0.5, "#fff2b0");
  return { canvas: k.canvas, size: 2, attachments: k.attachments };
}

/** A crane next to a Wonder under construction */
function crane(k: Iso, u: number, v: number, h: number) {
  k.lattice(u, v, 0, h, 16, "#e0a020");
  k.line([u, v, h], [u - 2.2, v + 0.2, h + 10], "#e0a020", 6);
  k.line([u, v, h], [u + 0.8, v - 0.1, h + 4], "#e0a020", 6);
  k.line([u - 1.8, v + 0.2, h + 8], [u - 1.8, v + 0.2, h - 60], "#333", 2);
  k.attach("blink", u, v, h + 14, 0.6, "#ff5a4f");
}

/**
 * A Wonder (GDD §9.3, §9.4): every stage adds a part; the part being built has scaffolding and a crane stands next to
 * it until the Wonder is finished.
 */
function drawWonder(look: HubStructureLook): BuildingArt {
  const stages = Math.max(1, look.stages ?? 3);
  const built = Math.min(stages, look.level);
  const finished = built >= stages;
  const k = new Iso(6, 6 * 90 + 520);
  k.pad(0.1, 0.1, 5.8, 5.8, "#b5ab96");
  k.pad(0.6, 0.6, 4.8, 4.8, "#d6cfc0");
  // How much of the part in progress stands (a third, as a promise)
  const part = (stage: number) => (stage < built ? 1 : stage === built && !finished ? 0.35 : 0);
  const stone = { top: "#efe7d6", left: "#cbbfa5", right: "#ab9f86" };
  switch (look.variant) {
    case "clockTower": {
      k.box(1.6, 1.6, 2.8, 2.8, 60 * Math.max(0.3, part(0)), stone);
      if (part(1) > 0) {
        k.box(2.1, 2.1, 1.8, 1.8, 220 * part(1), stone, 60);
        k.windowsLeft(2.1, 2.1, 1.8, 1.8, 80, 200 * part(1) - 30, 3, 2, "#6aa8d8");
        k.windowsRight(2.1, 2.1, 1.8, 1.8, 80, 200 * part(1) - 30, 3, 2, "#5a98c8");
      }
      if (part(2) > 0) {
        const z = 280;
        k.box(2.0, 2.0, 2.0, 2.0, 70 * part(2), { top: "#efe7d6", left: "#f4ecda", right: "#d8ccb2" }, z);
        if (part(2) === 1) {
          // A pointed roof with a golden finial
          k.cylinder(3, 3, 0.95, 6, "#7a3b2e", z + 70, "cone", "#8f4535");
          k.line([3, 3, z + 150], [3, 3, z + 190], "#c9a227", 5);
          k.attach("flag", 3, 3, z + 190, 1, "#f6c744");
          k.attach("glow", 3, 3, z + 40, 0.7, "#fff2b0");
        }
      }
      break;
    }
    case "grandFountain": {
      const radii = [2.4, 1.6, 0.9];
      let z = 0;
      radii.forEach((r, i) => {
        if (part(i) > 0) {
          k.cylinder(3, 3, r, 40 * part(i), "#d9d2c3", z, "flat", part(i) === 1 ? "#6ec6ff" : "#cfc8b8");
          z += 40 * part(i);
        }
      });
      if (finished) {
        k.cylinder(3, 3, 0.25, 90, "#d9d2c3", z, "dome", "#e8e2d4");
        k.attach("spark", 3, 3, z + 100, 1.4, "#bfe9ff");
        k.attach("spark", 2.2, 3.4, 50, 1, "#bfe9ff");
        k.attach("spark", 3.8, 2.6, 50, 1, "#bfe9ff");
      }
      break;
    }
    case "observatory": {
      k.box(1.2, 1.2, 3.6, 3.6, 80 * Math.max(0.3, part(0)), {
        top: "#e7e0d0",
        left: "#b9ad93",
        right: "#998d74",
      });
      if (part(1) > 0) {
        k.cylinder(3, 3, 1.4, 90 * part(1), "#d8d0be", 80, "flat", "#cfc6b2");
      }
      if (part(2) > 0) {
        k.cylinder(3, 3, 1.4, 1, "#8fa3b8", 170, "dome", part(2) === 1 ? "#9fb7cf" : "#c9c2b4");
        if (part(2) === 1) {
          k.line([3, 3, 230], [3.9, 2.4, 300], "#2a3440", 10);
          k.attach("glow", 3, 3, 240, 0.8, "#cfe8ff");
        }
      }
      break;
    }
    case "lighthouse": {
      k.cylinder(3, 3, 1.5, 60 * Math.max(0.3, part(0)), "#c7bfae", 0, "flat", "#d8d0be");
      const colors = ["#e8e2d4", "#c0392b"];
      for (let i = 0; i < 6; i++) {
        const p = i < 3 ? part(1) : part(2);
        if (p > 0 && (i % 3) / 3 < p) {
          k.cylinder(3, 3, 0.85 - i * 0.05, 50, colors[i % 2]!, 60 + i * 50, "flat", colors[i % 2]);
        }
      }
      if (finished) {
        k.cylinder(3, 3, 0.6, 50, "#f6e8a0", 360, "dome", "#2c3e50");
        k.attach("glow", 3, 3, 385, 1.6, "#fff2b0");
      }
      break;
    }
    case "greatDynamo": {
      // The copper coils stand behind the turbine hall: drawn first
      if (part(1) > 0) {
        for (let i = 0; i < 3; i++) {
          k.cylinder(1.6 + i * 1.4, 1.0, 0.45, 200 * part(1), "#b87333", 0, "dome", "#d4894a");
        }
      }
      k.box(0.8, 1.6, 4.4, 3.0, 110 * Math.max(0.3, part(0)), {
        top: "#6b4e2e",
        left: "#8a5a32",
        right: "#6f4628",
      });
      if (part(0) === 1) {
        k.windowsLeft(0.8, 1.6, 4.4, 3.0, 20, 80, 2, 6, "#f6c744", "#3a2614");
        k.gable(0.8, 1.6, 4.4, 3.0, 110, 50, "#4a5560");
      }
      if (part(2) > 0) {
        k.cylinder(4.8, 4.6, 0.5, 320 * part(2), "#7a3b2e", 0, "open");
        if (part(2) === 1) {
          k.attach("smoke", 4.8, 4.6, 330, 1.4, "#cccccc");
          k.attach("spark", 2.9, 1.2, 180, 1.5, "#9fe0ff");
        }
      }
      break;
    }
    case "copperCathedral": {
      const stoneColors = { top: "#e8dcc4", left: "#c9b48e", right: "#a99670" };
      k.box(1.2, 1.8, 3.6, 2.4, 120 * Math.max(0.3, part(0)), stoneColors);
      if (part(0) === 1) {
        k.windowsLeft(1.2, 1.8, 3.6, 2.4, 25, 80, 1, 5, "#7fb3e0", "#5a4630");
        k.gable(1.2, 1.8, 3.6, 2.4, 120, 60, "#4a9a82");
      }
      if (part(2) > 0) {
        // The copper dome over the crossing
        k.cylinder(3, 3, 1.0, 50 * part(2), "#e8dcc4", 150, part(2) === 1 ? "dome" : "flat", "#5fb39a");
        if (part(2) === 1) {
          k.attach("flag", 3, 3, 300, 1, "#f6c744");
        }
      }
      if (part(1) > 0) {
        // Twin towers at the front
        for (const u of [1.0, 4.0]) {
          k.box(u, 4.2, 1.0, 1.0, 220 * part(1), stoneColors);
          if (part(1) === 1) {
            k.cylinder(u + 0.5, 4.7, 0.55, 4, "#4a9a82", 220, "cone", "#5fb39a");
          }
        }
      }
      break;
    }
    default: {
      // The later Era Wonders: a tower of glass and steel that grows with every stage
      const height = 110 * (built + (finished ? 0 : 0.35));
      k.box(1.5, 1.5, 3, 3, Math.max(40, height), { top: "#9fb7cf", left: "#5d7d99", right: "#4a6680" });
      k.windowsLeft(
        1.5,
        1.5,
        3,
        3,
        20,
        Math.max(30, height - 30),
        Math.max(1, Math.round(height / 40)),
        5,
        "#cfe8ff",
      );
      k.windowsRight(
        1.5,
        1.5,
        3,
        3,
        20,
        Math.max(30, height - 30),
        Math.max(1, Math.round(height / 40)),
        5,
        "#bfe0ff",
      );
      if (finished) {
        k.lattice(3, 3, height, height + 200, 20, "#d9d9d9");
        k.attach("blink", 3, 3, height + 205, 0.8, "#ff5a4f");
      }
    }
  }
  if (!finished) {
    const z = [0, 60, 220][Math.min(2, built)] ?? 0;
    scaffold(k, 1.5, 1.5, 3, 3, z, 160);
    crane(k, 5.2, 1, 330);
  }
  if (look.label !== null) {
    k.text(3, 5.7, 6, look.label.toUpperCase(), "#f2c230", 30);
  }
  return { canvas: k.canvas, size: 6, attachments: k.attachments };
}

/** The HQ tower grows a floor per HQ level */
const HQ_FLOOR = 38;

function drawCorporationHq(look: HubStructureLook): BuildingArt {
  const level = Math.max(1, look.level);
  const color = look.color ?? "#2e86de";
  const k = new Iso(3, 3 * 90 + 140 + level * HQ_FLOOR);
  k.pad(0.1, 0.1, 2.8, 2.8, "#d6cfc0");
  // Podium with the entrance, then the tower, then a crown that gets fancier with the level
  k.box(0.25, 0.25, 2.5, 2.5, 34, { top: "#cfc8b8", left: "#a39c8e", right: "#8c8579" });
  k.doorLeft(1.2, 2.75, 0, 0.6, 26, "#26323a");
  const tower = 60 + level * HQ_FLOOR;
  const wall = { top: shade(color, 1.35), left: shade(color, 0.85), right: shade(color, 0.65) };
  k.box(0.55, 0.55, 1.9, 1.9, tower, wall, 34);
  const rows = Math.max(2, Math.round(tower / 32));
  k.windowsLeft(0.55, 0.55, 1.9, 1.9, 44, tower - 20, rows, 4, "#bfe3ff", shade(color, 0.5));
  k.windowsRight(0.55, 0.55, 1.9, 1.9, 44, tower - 20, rows, 4, "#9fd0f5", shade(color, 0.45));
  const top = 34 + tower;
  if (level >= 6) {
    // Setback with a second, slimmer tower
    k.box(0.85, 0.85, 1.3, 1.3, 40 + (level - 6) * 8, wall, top);
    k.attach("glow", 1.5, 1.5, top + 50 + (level - 6) * 8, 0.8, "#fff2b0");
  }
  const crown = level >= 6 ? top + 40 + (level - 6) * 8 : top;
  k.lattice(1.5, 1.5, crown, crown + 50 + Math.min(level, 5) * 6, 14, "#d9d9d9");
  k.attach("flag", 1.5, 1.5, crown + 50 + Math.min(level, 5) * 6, 1, color);
  k.attach("blink", 1.5, 1.5, crown + 54 + Math.min(level, 5) * 6, 0.6, "#ff5a4f");
  // The tag on a sign at the front edge of the roof, readable from afar
  if (look.label !== null) {
    k.box(0.6, 2.3, 1.8, 0.08, 34, { top: "#222", left: "#1b1b1b", right: "#111" }, top);
    k.text(1.5, 2.38, top + 17, look.label, "#ffffff", look.label.length > 4 ? 22 : 28);
  }
  return { canvas: k.canvas, size: 3, attachments: k.attachments };
}

export function drawHubStructure(type: string, size: number, look?: HubStructureLook): BuildingArt {
  if (type === "corporationHq") {
    return drawCorporationHq(look ?? { label: null, color: null, level: 1 });
  }
  if ((type === "wonderSite" || type === "eraWonderSite") && look?.variant) {
    return drawWonder(look);
  }
  if (type === "billboard") {
    return drawBillboard(look ?? { label: null, color: null, level: 0 });
  }
  const k = new Iso(size, size * 90 + 60);
  const s = size;
  switch (type) {
    case "marketHall": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#c7b79a");
      block(k, 0.3, 0.3, s - 0.6, s - 0.6, 110, { wall: "#6b4e2e", rows: 2, cols: 5 });
      k.gable(0.3, 0.3, s - 0.6, s - 0.6, 110, 70, "#b83b2c");
      k.text(s / 2, s - 0.3, 150, "MARKET", "#f2c230", 34);
      break;
    }
    case "townHall": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#c7b79a");
      k.box(0.3, 0.3, s - 0.6, s - 0.6, 120, "#e3dccb");
      k.windowsLeft(0.3, 0.3, s - 0.6, s - 0.6, 20, 90, 2, 4, "#2f7fd0");
      k.windowsRight(0.3, 0.3, s - 0.6, s - 0.6, 20, 90, 2, 4, "#2f7fd0");
      k.cylinder(s / 2, s / 2, 0.3, 90, "#e3dccb", 120, "dome", "#7fb3c9");
      k.attach("flag", s / 2, s / 2, 260, 1, "#2e86de");
      break;
    }
    case "exchange": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#b5ada0");
      k.box(0.3, 0.3, s - 0.6, s - 0.6, 160, { top: "#41505e", left: "#2c3e50", right: "#1f2d3a" });
      k.windowsLeft(0.3, 0.3, s - 0.6, s - 0.6, 15, 130, 4, 4, "#58b4f0", "#123");
      k.windowsRight(0.3, 0.3, s - 0.6, s - 0.6, 15, 130, 4, 4, "#58b4f0", "#123");
      lights(k, s / 2, s / 2, 175, "#3fe07a");
      break;
    }
    case "contractBoard": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#c7b79a");
      k.box(0.4, 0.8, s - 0.8, 0.15, 90, "#8a5a32");
      for (let i = 0; i < 5; i++) {
        k.box(0.5 + i * 0.2, 0.96, 0.12, 0.01, 20, "#f2f2f2", 40 + (i % 2) * 25);
      }
      break;
    }
    case "corporationQuarter": {
      // A plaza with four lots; the corporations' HQs stand on them (drawn separately)
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#b9b2a5");
      for (let i = 0; i < 4; i++) {
        const u = (i % 2) * 4;
        const v = Math.floor(i / 2) * 4;
        k.pad(u + 0.15, v + 0.15, 2.7, 2.7, "#9d968a", "#7d776c");
      }
      k.pad(3.2, 0.2, 0.6, s - 0.4, "#cbc4b6", null);
      k.pad(0.2, 3.2, s - 0.4, 0.6, "#cbc4b6", null);
      k.cylinder(3.5, 3.5, 0.35, 10, "#c9c2b4", 0, "flat", "#6ec6ff");
      break;
    }
    case "tradingHouse": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#c7b79a");
      block(k, 0.25, 0.25, s - 0.5, s - 0.5, 90, { rows: 2, cols: 3, wall: "#5c4033" });
      k.text(s / 2, s - 0.25, 50, "$", "#f2c230", 32);
      break;
    }
    case "park": {
      k.pad(0.05, 0.05, s - 0.1, s - 0.1, "#4f9a3e");
      k.cylinder(s / 2, s / 2, 0.35, 12, "#c9c2b4", 0, "flat", "#6ec6ff");
      k.attach("spark", s / 2, s / 2, 30, 0.8, "#bfe9ff");
      for (let i = 0; i < 6; i++) {
        const a = (i / 6) * Math.PI * 2;
        k.tree(s / 2 + Math.cos(a) * s * 0.33, s / 2 + Math.sin(a) * s * 0.33, 1.1);
      }
      break;
    }
    case "wonderSite": {
      k.pad(0.1, 0.1, s - 0.2, s - 0.2, "#8d8471");
      k.box(s / 2 - 1.2, s / 2 - 1.2, 2.4, 2.4, 40, "#c9c2b4");
      k.lattice(s / 2, s / 2, 40, 300, 40, "#c9a227");
      k.text(s / 2, s - 0.4, 40, "WONDER SITE", "#f2c230", 40);
      break;
    }
    default:
      block(k, 0.2, 0.2, s - 0.4, s - 0.4, 90, { rows: 2, cols: 3 });
  }
  return { canvas: k.canvas, size, attachments: k.attachments };
}

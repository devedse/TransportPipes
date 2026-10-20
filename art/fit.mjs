// Checks that every building stands on its footprint: each sprite drawn at its anchor over the exact tile diamond of
// its footprint (red). The top surface of the plate must fill the diamond; nothing of the plate may stick out below
// it, or the building looks lifted. Run after every `node process.mjs buildings` and look at every cell.
//   node fit.mjs <out.png> [id...]      (no ids: all buildings)
import sharp from "sharp";
import { readFileSync } from "node:fs";
import { parse as yaml } from "yaml";

const ART = new URL("../TransportPipes.Frontend/public/art/", import.meta.url).pathname;
const manifest = JSON.parse(readFileSync(ART + "manifest.json", "utf8"));
const sizes = Object.fromEntries(yaml(readFileSync(new URL("./buildings.yaml", import.meta.url), "utf8")).map((t) => [t.id, t.size]));
const [out, ...only] = process.argv.slice(2);
const ids = (only.length > 0 ? only : Object.keys(manifest.buildings)).sort();
const CELL = Number(process.env.CELL ?? 300);
const COLUMNS = Number(process.env.COLUMNS ?? 8);
const layers = [];
for (const [i, id] of ids.entries()) {
  const entry = manifest.buildings[id];
  const size = id.startsWith("wonder-") ? 6 : (sizes[id] ?? 1);
  const resolution = entry.r ?? manifest.resolution;
  // The footprint diamond in sprite pixels, the sprite shrunk to fit its cell
  const diamondW = size * 192 * resolution;
  const fit = Math.min(1, (CELL - 40) / Math.max(entry.w, diamondW), (CELL - 50) / entry.h);
  const ox = (i % COLUMNS) * CELL;
  const oy = Math.floor(i / COLUMNS) * CELL;
  const ax = ox + CELL / 2;
  const ay = oy + CELL - 16;
  const sprite = await sharp(`${ART}buildings/${id}.webp`).resize(Math.max(1, Math.round(entry.w * fit)), Math.max(1, Math.round(entry.h * fit))).toBuffer();
  layers.push({ input: sprite, left: Math.round(ax - entry.ax * fit), top: Math.round(ay - entry.ay * fit) });
  const w = (diamondW / 2) * fit;
  const h = (diamondW / 4) * fit;
  const svg = `<svg width="${CELL}" height="${CELL}" xmlns="http://www.w3.org/2000/svg">
    <polygon points="${CELL / 2},${CELL - 16} ${CELL / 2 + w},${CELL - 16 - h} ${CELL / 2},${CELL - 16 - 2 * h} ${CELL / 2 - w},${CELL - 16 - h}" fill="none" stroke="#ff2d55" stroke-width="1.5"/>
    <text x="6" y="16" font-family="sans-serif" font-size="13" fill="#fff">${id} (${size}×${size})</text></svg>`;
  layers.push({ input: Buffer.from(svg), left: ox, top: oy });
}
await sharp({ create: { width: CELL * COLUMNS, height: CELL * Math.ceil(ids.length / COLUMNS), channels: 4, background: "#5d8a3a" } })
  .composite(layers)
  .png()
  .toFile(out);
console.log(`${ids.length} buildings → ${out}`);

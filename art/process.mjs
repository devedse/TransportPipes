// Turns generated art into game sprites (docs/ArtDirection.md):
//   node process.mjs buildings <source dir>   ground plate → footprint, WebP, manifest
//   node process.mjs terrain <source dir>     seamless textures, WebP
//   node process.mjs derived                  terrain made from another one's texture (red hills)
// Output goes to TransportPipes.Frontend/public/art, the manifest to public/art/manifest.json.
import sharp from "sharp";
import { existsSync, mkdirSync, readFileSync, readdirSync } from "node:fs";
import { basename, join } from "node:path";
import { OUT, manifest as load, save } from "./manifest.mjs";
import { parse as yaml } from "yaml";

/** Sprites are stored at 1.5× the game's world scale, so they stay sharp when zoomed in */
const RESOLUTION = 1.5;
/** A tile's diamond is 192 world units wide */
const TILE = 192;

function manifest() {
  return load({ resolution: RESOLUTION, buildings: {}, rotors: {}, terrain: {} });
}

async function rgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, width: info.width, height: info.height };
}

/** The opaque bounding box and the ground plate: its left/right corners and its bottom corner */
function measure({ data, width, height }) {
  const opaque = (x, y) => data[(y * width + x) * 4 + 3] > 40;
  let minX = width, maxX = -1, minY = height, maxY = -1;
  const rows = [];
  for (let y = 0; y < height; y++) {
    let left = -1, right = -1;
    for (let x = 0; x < width; x++) {
      if (opaque(x, y)) {
        if (left < 0) left = x;
        right = x;
      }
    }
    rows.push([left, right]);
    if (left >= 0) {
      minX = Math.min(minX, left);
      maxX = Math.max(maxX, right);
      minY = Math.min(minY, y);
      maxY = y;
    }
  }
  if (maxY < 0) throw new Error("empty image");
  // The plate: the widest rows in the lowest part of the picture
  const band = Math.max(8, Math.round((maxY - minY) * 0.45));
  let left = width, right = -1;
  for (let y = maxY - band; y <= maxY; y++) {
    const [l, r] = rows[y] ?? [-1, -1];
    if (l >= 0) {
      left = Math.min(left, l);
      right = Math.max(right, r);
    }
  }
  const [bl, br] = rows[maxY];
  // The slab's top surface: its left and right corners are the widest row of the lower part (the topmost of them;
  // below it the slab's side goes down)
  const widest = Math.max(...rows.slice(minY + Math.round((maxY - minY) * 0.3), maxY + 1).map(([l, r]) => (l < 0 ? 0 : r - l)));
  let topY = maxY;
  for (let y = maxY; y >= minY + Math.round((maxY - minY) * 0.3); y--) {
    const [l, r] = rows[y];
    if (l >= 0 && r - l >= widest - 2) topY = y;
  }
  const [tl, tr] = rows[topY];
  return { minX, maxX, minY, maxY, left, right, bottomX: (bl + br) / 2, top: { left: tl, right: tr, y: topY } };
}

async function buildings(source) {
  const list = yaml(readFileSync(new URL("./buildings.yaml", import.meta.url), "utf8"));
  const parts = yaml(readFileSync(new URL("./attachments.yaml", import.meta.url), "utf8")) ?? {};
  const sizes = Object.fromEntries(list.map((t) => [t.id, t.size]));
  // Wonders stand on the 6×6 Wonder sites of the hubs
  for (const file of readdirSync(source)) {
    if (file.startsWith("wonder-")) sizes[basename(file, ".png")] = 6;
  }
  const m = manifest();
  mkdirSync(join(OUT, "buildings"), { recursive: true });
  for (const file of readdirSync(source).filter((f) => f.endsWith(".png"))) {
    const id = basename(file, ".png");
    if (id.endsWith("-rotor")) continue;
    if (!(id in sizes)) {
      console.log(`skip ${file}: not in buildings.yaml`);
      continue;
    }
    const image = await rgba(join(source, file));
    const box = measure(image);
    // Every building: the top surface of its plate lies on the footprint (the plate's side sinks into the ground),
    // else the building looks lifted. docs/ArtDirection.md, check with art/fit.mjs
    const onTop = true;
    if (onTop) {
      box.left = box.top.left;
      box.right = box.top.right;
    }
    const plate = box.right - box.left;
    // Big sprites (Wonders) are stored at a lower resolution: they would only be upscaled
    const resolution = sizes[id] >= 4 ? 0.75 : RESOLUTION;
    const scale = (sizes[id] * TILE * resolution) / plate;
    const cx = (box.left + box.right) / 2;
    const w = Math.round((box.maxX - box.minX + 1) * scale);
    const h = Math.round((box.maxY - box.minY + 1) * scale);
    const resized = await sharp(join(source, file))
      .extract({ left: box.minX, top: box.minY, width: box.maxX - box.minX + 1, height: box.maxY - box.minY + 1 })
      .resize(w, h, { kernel: "lanczos3" })
      .ensureAlpha()
      .raw()
      .toBuffer();
    // Nothing below the footprint's two front edges: the plate's side would stick out over the ground in front
    const anchorX = (cx - box.minX) * scale;
    const anchorY = (box.top.y - box.minY) * scale + (sizes[id] * TILE * resolution) / 4;
    const half = (sizes[id] * TILE * resolution) / 2;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        // The front edges: from the bottom corner up to the left and right corners (and level beyond them)
        const edge = anchorY - Math.min(half, Math.abs(x - anchorX)) / 2;
        const below = y - edge;
        if (below > 2.5) {
          resized[(y * w + x) * 4 + 3] = 0;
        } else if (below > 1) {
          resized[(y * w + x) * 4 + 3] = Math.round(resized[(y * w + x) * 4 + 3] * (2.5 - below) / 1.5);
        }
      }
    }
    await sharp(resized, { raw: { width: w, height: h, channels: 4 } })
      .webp({ quality: 88, alphaQuality: 95 })
      .toFile(join(OUT, "buildings", `${id}.webp`));
    // Where the plate's bottom corner is, in the stored sprite
    m.buildings[id] = {
      ...(resolution !== RESOLUTION ? { r: resolution } : {}),
      w,
      h,
      ax: Math.round((cx - box.minX) * scale),
      // The bottom corner of the plate's top surface: half a footprint below its left and right corners
      ay: onTop
        ? Math.round((box.top.y - box.minY) * scale + (sizes[id] * TILE * resolution) / 4)
        : Math.round((box.maxY - box.minY) * scale),
      attachments: (parts[id] ?? []).map((a) => ({ ...a, x: Math.round((a.x - box.minX) * scale), y: Math.round((a.y - box.minY) * scale) })),
    };
    const skew = Math.abs(box.bottomX - cx) / plate;
    console.log(`${id}: plate ${plate}px → ${w}×${h}${skew > 0.06 ? `  ⚠ bottom corner off centre by ${Math.round(skew * 100)}%` : ""}`);

    const rotor = join(source, `${id}-rotor.png`);
    if (existsSync(rotor)) {
      const r = await rgba(rotor);
      const rb = measure(r);
      // Rotor diameter: as wide as the footprint
      const size = Math.round(sizes[id] * TILE * RESOLUTION * 0.8);
      const span = Math.max(rb.maxX - rb.minX, rb.maxY - rb.minY) + 1;
      const half = Math.ceil(span / 2);
      const centre = { x: r.width / 2, y: r.height / 2 };
      const left = Math.max(0, Math.round(centre.x - half));
      const top = Math.max(0, Math.round(centre.y - half));
      await sharp(rotor)
        .extract({ left, top, width: Math.min(r.width - left, half * 2), height: Math.min(r.height - top, half * 2) })
        .resize(size, size)
        .webp({ quality: 88, alphaQuality: 95 })
        .toFile(join(OUT, "buildings", `${id}-rotor.webp`));
      m.rotors[id] = { size };
      console.log(`${id}-rotor: ${size}px`);
    }
  }
  save(m);
}

/** Toning per texture, so the ground stays calmer than the buildings on it */
const TERRAIN_TONE = {
  grass: { saturation: 0.78, brightness: 0.9 },
  hills: { saturation: 0.62, brightness: 0.92, hue: 14 },
  forest: { saturation: 0.85, brightness: 0.85 },
  water: { saturation: 0.8, brightness: 0.78 },
  beach: { saturation: 0.85 },
  desert: { saturation: 0.72, brightness: 0.96 },
  road: { saturation: 0.85 },
  city: { saturation: 0.8 },
};

/** Seamless: blend the texture with itself shifted by half, the shifted copy at the edges */
async function terrain(source) {
  const m = manifest();
  mkdirSync(join(OUT, "terrain"), { recursive: true });
  for (const file of readdirSync(source).filter((f) => f.endsWith(".png"))) {
    const id = basename(file, ".png");
    const size = 512;
    const tone = TERRAIN_TONE[id] ?? {};
    const { data } = await sharp(join(source, file)).resize(size, size).modulate(tone).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const out = Buffer.alloc(size * size * 4);
    for (let y = 0; y < size; y++) {
      for (let x = 0; x < size; x++) {
        const edge = Math.min(x, size - 1 - x, y, size - 1 - y) / (size / 2);
        const t = Math.min(1, edge / 0.45);
        const keep = t * t * (3 - 2 * t);
        const i = (y * size + x) * 4;
        const j = (((y + size / 2) % size) * size + ((x + size / 2) % size)) * 4;
        for (let c = 0; c < 3; c++) out[i + c] = Math.round(data[i + c] * keep + data[j + c] * (1 - keep));
        out[i + 3] = 255;
      }
    }
    await sharp(out, { raw: { width: size, height: size, channels: 4 } }).webp({ quality: 90 }).toFile(join(OUT, "terrain", `${id}.webp`));
    m.terrain[id] = { size };
    console.log(`${id}: seamless ${size}px`);
  }
  save(m);
}

/** Props stand on the ground (anchored at their foot), decals lie on it (anchored at their middle) */
async function props(source) {
  const list = yaml(readFileSync(new URL("./props.yaml", import.meta.url), "utf8"));
  const m = manifest();
  m.props ??= {};
  mkdirSync(join(OUT, "props"), { recursive: true });
  for (const prop of list) {
    const file = join(source, `${prop.id}.png`);
    if (!existsSync(file)) continue;
    const image = await rgba(file);
    const box = measure(image);
    const scale = (prop.width * TILE * RESOLUTION) / (box.maxX - box.minX + 1);
    const w = Math.round((box.maxX - box.minX + 1) * scale);
    const h = Math.round((box.maxY - box.minY + 1) * scale);
    await sharp(file)
      .extract({ left: box.minX, top: box.minY, width: box.maxX - box.minX + 1, height: box.maxY - box.minY + 1 })
      .resize(w, h, { kernel: "lanczos3" })
      .webp({ quality: 88, alphaQuality: 95 })
      .toFile(join(OUT, "props", `${prop.id}.webp`));
    m.props[prop.id] = prop.kind === "decal" ? { w, h, ax: Math.round(w / 2), ay: Math.round(h / 2), decal: true } : { w, h, ax: Math.round((box.bottomX - box.minX) * scale), ay: h };
    console.log(`${prop.id}: ${w}×${h}`);
  }
  save(m);
}

/** UI pictures: trimmed and scaled to a width (icons 112 px, portraits 192 px, the logo 960 px, key art 1920 px) */
async function ui(source) {
  mkdirSync(join(OUT, "ui"), { recursive: true });
  const widths = { icon: 112, tool: 112, res: 96, ui: 112, manager: 192, logo: 960, "key-art": 1920 };
  for (const file of readdirSync(source).filter((f) => f.endsWith(".png"))) {
    const id = basename(file, ".png");
    const width = Object.entries(widths).find(([prefix]) => id.startsWith(prefix))?.[1];
    if (width === undefined) continue;
    const image = sharp(join(source, file));
    const trimmed = id === "key-art" ? image : image.trim({ threshold: 8 });
    await trimmed.resize({ width, height: id === "key-art" ? undefined : width, fit: "inside" })
      .webp({ quality: id === "key-art" ? 82 : 90, alphaQuality: 95 })
      .toFile(join(OUT, "ui", `${id}.webp`));
    console.log(`ui/${id}.webp`);
  }
}

/** Loady's poses side by side in one picture: one trimmed sprite per pose */
async function loady(source) {
  mkdirSync(join(OUT, "ui"), { recursive: true });
  const names = ["wave", "point", "think", "cheer"];
  const meta = await sharp(source).metadata();
  const column = Math.floor(meta.width / names.length);
  for (const [i, name] of names.entries()) {
    const part = await sharp(source).extract({ left: i * column, top: 0, width: column, height: meta.height }).png().toBuffer();
    await sharp(part).trim({ threshold: 10 }).resize({ height: 220, fit: "inside" }).webp({ quality: 90, alphaQuality: 95 }).toFile(join(OUT, "ui", `loady-${name}.webp`));
    console.log(`loady-${name}`);
  }
}

const [kind, source] = process.argv.slice(2);
/** Terrain made from another one's texture: the red hills (bauxite, 1.4 §5) are the hills' dry grass on red earth */
const DERIVED = { redHills: { from: "hills", tone: { hue: -58, saturation: 0.8, brightness: 0.78 } } };

async function derived() {
  const m = manifest();
  for (const [id, { from, tone }] of Object.entries(DERIVED)) {
    await sharp(join(OUT, "terrain", `${from}.webp`)).modulate(tone).webp({ quality: 90 }).toFile(join(OUT, "terrain", `${id}.webp`));
    m.terrain[id] = { size: m.terrain[from].size };
    console.log(`${id}: from ${from}`);
  }
  save(m);
}

// `node process.mjs hashes` only stamps the manifest
await { buildings, terrain, props, loady, ui, derived, hashes: async () => save(manifest()) }[kind](source);

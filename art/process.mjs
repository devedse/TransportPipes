// Turns generated art into game sprites (docs/ArtDirection.md):
//   node process.mjs buildings <source dir>   ground plate → footprint, WebP, manifest
//   node process.mjs terrain <source dir>     seamless textures, WebP
// Output goes to TransportPipes.Frontend/public/art, the manifest to public/art/manifest.json.
import sharp from "sharp";
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { basename, join } from "node:path";
import { parse as yaml } from "yaml";

const OUT = new URL("../TransportPipes.Frontend/public/art/", import.meta.url).pathname;
const MANIFEST = join(OUT, "manifest.json");
/** Sprites are stored at 1.5× the game's world scale, so they stay sharp when zoomed in */
const RESOLUTION = 1.5;
/** A tile's diamond is 192 world units wide */
const TILE = 192;

function manifest() {
  return existsSync(MANIFEST) ? JSON.parse(readFileSync(MANIFEST, "utf8")) : { resolution: RESOLUTION, buildings: {}, rotors: {}, terrain: {} };
}

/**
 * Writes the manifest with a content hash of every sprite: the game asks for `<path>?v=<hash>`, so browsers can keep
 * the art for good and still get a new picture when it changes.
 */
function save(m) {
  m.hashes = {};
  for (const dir of ["buildings", "terrain", "props"]) {
    if (!existsSync(join(OUT, dir))) continue;
    for (const file of readdirSync(join(OUT, dir)).filter((f) => f.endsWith(".webp")).sort()) {
      m.hashes[`${dir}/${file}`] = createHash("sha256").update(readFileSync(join(OUT, dir, file))).digest("hex").slice(0, 10);
    }
  }
  writeFileSync(MANIFEST, JSON.stringify(m, null, 1));
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
  return { minX, maxX, minY, maxY, left, right, bottomX: (bl + br) / 2 };
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
    const plate = box.right - box.left;
    // Big sprites (Wonders) are stored at a lower resolution: they would only be upscaled
    const resolution = sizes[id] >= 4 ? 0.75 : RESOLUTION;
    const scale = (sizes[id] * TILE * resolution) / plate;
    const cx = (box.left + box.right) / 2;
    const w = Math.round((box.maxX - box.minX + 1) * scale);
    const h = Math.round((box.maxY - box.minY + 1) * scale);
    await sharp(join(source, file))
      .extract({ left: box.minX, top: box.minY, width: box.maxX - box.minX + 1, height: box.maxY - box.minY + 1 })
      .resize(w, h, { kernel: "lanczos3" })
      .webp({ quality: 88, alphaQuality: 95 })
      .toFile(join(OUT, "buildings", `${id}.webp`));
    // Where the plate's bottom corner is, in the stored sprite
    m.buildings[id] = {
      ...(resolution !== RESOLUTION ? { r: resolution } : {}),
      w,
      h,
      ax: Math.round((cx - box.minX) * scale),
      ay: Math.round((box.maxY - box.minY) * scale),
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
// `node process.mjs hashes` only stamps the manifest
await { buildings, terrain, props, loady, ui, hashes: async () => save(manifest()) }[kind](source);

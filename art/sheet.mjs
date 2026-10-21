// A contact sheet of images for reviewing: node sheet.mjs <out.png> <tile px> <files...>
import sharp from "sharp";
import { basename } from "node:path";
const [out, tilePx, ...files] = process.argv.slice(2);
const size = Number(tilePx);
const cols = Math.min(4, files.length);
const rows = Math.ceil(files.length / cols);
const label = 28;
const tiles = await Promise.all(files.map(async (f, i) => ({
  input: await sharp(f).resize(size, size, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer(),
  left: (i % cols) * size,
  top: Math.floor(i / cols) * (size + label),
})));
const labels = files.map((f, i) => ({
  input: Buffer.from(`<svg width="${size}" height="${label}"><text x="6" y="20" font-size="18" font-family="sans-serif" fill="#fff">${basename(f)}</text></svg>`),
  left: (i % cols) * size,
  top: Math.floor(i / cols) * (size + label) + size,
}));
await sharp({ create: { width: cols * size, height: rows * (size + label), channels: 4, background: { r: 52, g: 120, b: 56, alpha: 1 } } })
  .composite([...tiles, ...labels]).png().toFile(out);
console.log(out);

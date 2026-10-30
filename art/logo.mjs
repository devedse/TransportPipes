// The TransportPipes logo, in the owner's MS Paint style (like their other projects' logos): flat fills, thick black
// outlines, aliased stair-stepped edges, a plain bold dark red title. Drawn as SVG with slightly wobbly hand-drawn
// edges, rasterised small, then snapped to the palette with hard alpha (that makes the MS Paint jaggies).
//
//   node logo.mjs   → logo/logo-title.png (300 px) and logo/logo-icon.png (256 px), each also at 4x;
//                     TransportPipes.Frontend/public/favicon.ico (16, 32, 48 px) and apple-touch-icon.png (180 px)
import sharp from "sharp";
import { mkdirSync, writeFileSync } from "node:fs";

const here = (path) => new URL(path, import.meta.url).pathname;
const logoDir = here("./logo/");
const publicDir = here("../TransportPipes.Frontend/public/");
mkdirSync(logoDir, { recursive: true });

const C = {
  k: "#000000", w: "#FFFFFF", g: "#9A9A9A", gd: "#5E5E5E", gl: "#D4D4D4",
  r: "#E8201A", rd: "#8B0000", y: "#FFE020", yd: "#D9A400", b: "#0040FF", bl: "#9FE0FF",
  o: "#FF8C00", od: "#8B4513", lime: "#22FF22", gr: "#119911",
};
const PALETTE = Object.values(C).map((h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16)));

// A seeded wobble, so each run draws the same "hand-drawn" lines
let seed = 7;
const rnd = () => ((seed = (seed * 16807) % 2147483647) / 2147483647) * 2 - 1;
function wobbly(points, closed = true, step = 10, amount = 0.9) {
  const pts = [];
  const n = closed ? points.length : points.length - 1;
  for (let i = 0; i < n; i++) {
    const [x0, y0] = points[i];
    const [x1, y1] = points[(i + 1) % points.length];
    const parts = Math.max(1, Math.round(Math.hypot(x1 - x0, y1 - y0) / step));
    for (let k = 0; k < parts; k++) {
      const t = k / parts;
      const j = k === 0 ? 0.3 : 1;
      pts.push([x0 + (x1 - x0) * t + rnd() * amount * j, y0 + (y1 - y0) * t + rnd() * amount * j]);
    }
  }
  if (!closed) pts.push(points[points.length - 1]);
  return pts.map(([x, y]) => `${x.toFixed(1)},${y.toFixed(1)}`).join(" ");
}
const poly = (points, fill, sw = 3) =>
  `<polygon points="${wobbly(points)}" fill="${fill}" stroke="#000" stroke-width="${sw}" stroke-linejoin="round"/>`;
const line = (points, color = "#000", sw = 3) =>
  `<polyline points="${wobbly(points, false, 8, 0.6)}" fill="none" stroke="${color}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round"/>`;
const rect = (x0, y0, x1, y1, fill, sw = 3) => poly([[x0, y0], [x1, y0], [x1, y1], [x0, y1]], fill, sw);
const circlePts = (cx, cy, rx, ry = rx, a0 = 0, a1 = Math.PI * 2, n = 28) =>
  Array.from({ length: n + 1 }, (_, i) => {
    const a = a0 + ((a1 - a0) * i) / n;
    return [cx + Math.cos(a) * rx, cy + Math.sin(a) * ry];
  });
const circle = (cx, cy, r, fill, sw = 3) => poly(circlePts(cx, cy, r).slice(0, -1), fill, sw);
// A blue motion arc with a thin black edge, like the signal arcs in the streamer logo
const arc = (cx, cy, r, a0, a1) => {
  const pts = circlePts(cx, cy, r, r, a0, a1, 10);
  return line(pts, "#000", 8) + line(pts, C.b, 4.5);
};

function drawing() {
  const s = [];
  // Grass under it all
  s.push(poly(circlePts(128, 222, 118, 22).slice(0, -1), C.lime));
  // Smoke drifting up and left from the chimney
  s.push(circle(40, 16, 12, C.w), circle(58, 27, 10, C.w), circle(74, 38, 7, C.w));
  // The factory: red brick, a sawtooth roof, a chimney, warm windows, a door
  s.push(rect(74, 52, 90, 104, C.r), rect(71, 48, 93, 58, C.gd));
  s.push(poly([[16, 118], [16, 92], [44, 118], [44, 92], [72, 118], [72, 92], [100, 118]], C.g));
  s.push(rect(16, 118, 100, 212, C.r));
  for (const [x0, y0, x1, y1] of [[22, 164, 34, 164], [50, 172, 64, 172], [78, 162, 92, 162], [24, 196, 36, 196], [84, 190, 94, 190], [28, 126, 42, 126]])
    s.push(line([[x0, y0], [x1, y1]], C.rd, 2.5));
  s.push(rect(26, 134, 46, 154, C.y), rect(62, 134, 82, 154, C.y));
  s.push(line([[36, 135], [36, 153]], "#000", 2.5), line([[72, 135], [72, 153]], "#000", 2.5));
  s.push(rect(46, 176, 66, 212, C.gd));
  // The pipe: out of the factory to the right, a bend, up to an open end
  const top = 150, bot = 184, cy = 129, cx = 172, ri = 21, ro = 55;
  const bend = [
    [100, top], [cx, top],
    ...circlePts(cx, cy, ri, ri, Math.PI / 2, 0, 8).slice(1),
    [cx + ri, 80], [cx + ro, 80],
    ...circlePts(cx, cy, ro, ro, 0, Math.PI / 2, 12).slice(0, -1),
    [cx, bot], [100, bot],
  ];
  s.push(poly(bend, C.g, 3.5));
  // A flat darker underside and a white shine stripe
  s.push(line([[104, 178], [168, 178]], C.gd, 6), line([[106, 157], [166, 157]], C.w, 3));
  s.push(line(circlePts(cx, cy, ro - 6, ro - 6, 0.25, 1.25, 6), C.gd, 6));
  s.push(line([[cx + ri + 6, 90], [cx + ri + 6, 124]], C.w, 3));
  // A cut-away window with the goods inside: ore, water, energy
  s.push(rect(118, 159, 162, 177, C.bl, 2.5));
  s.push(poly([[123, 172], [124, 164], [131, 162], [134, 168], [130, 174]], C.o, 2));
  s.push(poly([[144, 162], [149, 169], [148, 174], [140, 174], [139, 169]], C.b, 2));
  s.push(poly([[158, 161], [152, 168], [156, 168], [151, 175], [160, 166], [156, 166], [159, 161]], C.y, 1.5));
  // Gold flanges at the joints, and the open end with its dark mouth
  s.push(rect(103, 143, 113, 191, C.y), rect(166, 143, 176, 191, C.y));
  s.push(rect(186, 120, 234, 130, C.y), rect(186, 74, 234, 86, C.y));
  s.push(poly(circlePts(210, 75, 18, 5).slice(0, -1), C.k, 2));
  // Out of the open end: goods flying up and over into a big gold coin
  s.push(poly([[208, 62], [210, 52], [218, 50], [222, 58], [217, 65]], C.o, 2.5));
  s.push(line([[212, 56], [215, 56]], C.od, 2));
  s.push(poly([[196, 30], [203, 41], [202, 47], [191, 47], [189, 41]], C.b, 2.5));
  s.push(line([[194, 41], [195, 44]], C.w, 2));
  s.push(poly([[220, 14], [210, 28], [217, 28], [209, 42], [226, 23], [219, 23], [226, 14]], C.y, 2));
  s.push(arc(150, 38, 34, Math.PI * 0.62, Math.PI * 0.98), arc(150, 38, 43, Math.PI * 0.66, Math.PI * 0.94));
  s.push(circle(150, 38, 25, C.y, 3.5));
  s.push(line(circlePts(150, 38, 17, 17, 0, Math.PI * 2, 20), C.yd, 3));
  s.push(`<text x="150" y="46" font-family="Liberation Sans" font-weight="bold" font-size="22" fill="${C.yd}" text-anchor="middle">G</text>`);
  s.push(line([[134, 30], [139, 22]], C.w, 3));
  return s.join("\n");
}

/**
 * The favicon: the same factory, pipe bend and gold coin, bolder and with fewer parts so it reads in a browser tab.
 * Drawn on a 64 grid; the outline is kept at least a pixel and a bit wide at every size.
 */
function mark(size) {
  const tiny = size <= 16;
  const sw = (tiny ? 1.05 : Math.max(1.3, size / 22)) * (64 / size);
  const flat = (points, fill, w = sw) =>
    `<polygon points="${points.map((p) => p.join(",")).join(" ")}" fill="${fill}" stroke="#000" stroke-width="${w}" stroke-linejoin="round"/>`;
  const box = (x0, y0, x1, y1, fill) => flat([[x0, y0], [x1, y0], [x1, y1], [x0, y1]], fill);
  const s = [];
  // The factory with a sawtooth roof and a lit window
  s.push(flat([[3, 29], [3, 17], [16, 29], [16, 17], [29, 29]], C.g));
  s.push(box(3, 29, 29, 61, C.r));
  if (!tiny) s.push(box(9, 37, 23, 47, C.y));
  // The pipe: out of the factory, a bend, up to an open end with a gold flange
  const top = 40, bot = 56, cx = 40, cy = 36, ri = top - cy, ro = bot - cy;
  s.push(flat([
    [29, top], [cx, top], ...circlePts(cx, cy, ri, ri, Math.PI / 2, 0, 4).slice(1),
    [cx + ri, 21], [cx + ro, 21], ...circlePts(cx, cy, ro, ro, 0, Math.PI / 2, 8).slice(0, -1), [cx, bot], [29, bot],
  ], C.g));
  if (!tiny) s.push(box(33, 37, 38, 59, C.y));
  s.push(box(41, 14, 63, 22, C.y));
  // The gold coin the goods turn into
  s.push(flat(circlePts(30, 10, 9, 9, 0, Math.PI * 2, 20).slice(0, -1), C.y));
  if (size >= 32) {
    s.push(`<polyline points="${circlePts(30, 10, 5, 5, 0, Math.PI * 2, 16).map((p) => p.join(",")).join(" ")}" fill="none" stroke="${C.yd}" stroke-width="${sw * 0.8}"/>`);
    s.push(`<ellipse cx="52" cy="15" rx="7" ry="2.2" fill="#000"/>`);
  }
  return s.join("\n");
}

const title = (x, y, size) =>
  `<text x="${x}" y="${y}" font-family="Liberation Sans" font-weight="bold" font-size="${size}" fill="${C.rd}" text-anchor="middle">TransportPipes</text>`;
const svg = (w, h, body, view = `0 0 ${w} ${h}`) =>
  `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="${view}">${body}</svg>`;

/** Rasterise, then MS Paint it: hard alpha, every pixel one of the palette's colours */
async function paint(source, w, h, palette = PALETTE) {
  const { data } = await sharp(Buffer.from(source)).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < w * h; i++) {
    const k = i * 4;
    if (data[k + 3] < 128) {
      data[k] = data[k + 1] = data[k + 2] = data[k + 3] = 0;
      continue;
    }
    let best = palette[0];
    let dist = Infinity;
    for (const p of palette) {
      const d = (p[0] - data[k]) ** 2 + (p[1] - data[k + 1]) ** 2 + (p[2] - data[k + 2]) ** 2;
      if (d < dist) {
        dist = d;
        best = p;
      }
    }
    [data[k], data[k + 1], data[k + 2], data[k + 3]] = [...best, 255];
  }
  return sharp(data, { raw: { width: w, height: h, channels: 4 } });
}

async function logo(name, w, h, body) {
  const image = await paint(svg(w, h, body), w, h);
  await image.clone().png().toFile(`${logoDir}${name}.png`);
  await image.clone().resize(w * 4, h * 4, { kernel: "nearest" }).png().toFile(`${logoDir}${name}-x4.png`);
}

/** An .ico holding PNG images (every browser reads those), the best size picked per screen */
function ico(images) {
  const header = Buffer.alloc(6 + 16 * images.length);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(images.length, 4);
  let offset = header.length;
  images.forEach(({ size, png }, i) => {
    const at = 6 + 16 * i;
    header.writeUInt8(size, at);
    header.writeUInt8(size, at + 1);
    header.writeUInt16LE(1, at + 4);
    header.writeUInt16LE(32, at + 6);
    header.writeUInt32LE(png.length, at + 8);
    header.writeUInt32LE(offset, at + 12);
    offset += png.length;
  });
  return Buffer.concat([header, ...images.map((t) => t.png)]);
}

seed = 7;
await logo("logo-icon", 256, 256, drawing());
seed = 7;
await logo("logo-title", 300, 300, `${title(150, 34, 34)}<g transform="translate(22 44)">${drawing()}</g>`);
/**
 * A small icon without stray edge colours: painted at 4x, then each 4x4 block takes its most common colour, black where
 * the outline covers enough of the block (so outlines stay whole), clear where most of it is
 */
async function small(size) {
  const big = size * 4;
  const colours = [C.k, C.w, C.g, C.r, C.y, C.yd].map((h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16)));
  const marked = await paint(svg(big, big, mark(size), "0 0 64 64"), big, big, colours);
  const { data } = await marked.raw().toBuffer({ resolveWithObject: true });
  const outData = Buffer.alloc(size * size * 4);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const counts = new Map();
      for (let dy = 0; dy < 4; dy++) {
        for (let dx = 0; dx < 4; dx++) {
          const k = ((y * 4 + dy) * big + x * 4 + dx) * 4;
          const key = data[k + 3] === 0 ? "clear" : `${data[k]},${data[k + 1]},${data[k + 2]}`;
          counts.set(key, (counts.get(key) ?? 0) + 1);
        }
      }
      const k = (y * size + x) * 4;
      if ((counts.get("clear") ?? 0) >= 9) continue;
      let pick = (counts.get("0,0,0") ?? 0) >= (size <= 16 ? 7 : 5) ? "0,0,0" : null;
      if (pick === null) {
        let most = 0;
        for (const [key, n] of counts) if (key !== "clear" && n > most) [pick, most] = [key, n];
      }
      const [r, g, b] = pick.split(",").map(Number);
      [outData[k], outData[k + 1], outData[k + 2], outData[k + 3]] = [r, g, b, 255];
    }
  }
  return sharp(outData, { raw: { width: size, height: size, channels: 4 } }).png().toBuffer();
}

const icons = [];
for (const size of [16, 32, 48]) {
  const png = await small(size);
  icons.push({ size, png });
  writeFileSync(`${logoDir}favicon-${size}.png`, png);
}
writeFileSync(`${publicDir}favicon.ico`, ico(icons));
// The home screen icon (iOS shows transparency as black): the full drawing on white
seed = 7;
await (
  await paint(svg(180, 180, `<rect width="180" height="180" fill="#fff"/><g transform="translate(12 12) scale(0.61)">${drawing()}</g>`), 180, 180)
)
  .png()
  .toFile(`${publicDir}apple-touch-icon.png`);
console.log("logo/, public/favicon.ico, public/apple-touch-icon.png");

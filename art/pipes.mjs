// Renders the pipes (docs/ArtDirection.md): real 3D pipe pieces, so every tile joins its neighbours exactly and corners,
// junctions, ends and building sockets look like fittings, not lines. A small signed-distance raymarcher draws each
// piece with the camera and the light of the generated art, then shades it once per tier (the material).
//
//   node pipes.mjs [shape]     → TransportPipes.Frontend/public/art/pipes/tier<N>.webp and manifest.json "pipes"
//   (with a shape, e.g. "ur", only that piece, as scratch/pipe-<shape>.png for a quick look; "d+manifold-u+lane-solid-u-1"
//   joins pieces into one scene)
//
// A tile is the ground square [0,1]×[0,1]; x runs to the bottom right of the screen, y to the bottom left, z up. A
// shape is the sides a piece connects to: u (y-1), r (x+1), d (y+1), l (x-1); "socket-<side>" is the bulkhead where a
// pipe plugs into a building, drawn over the pipe piece.
import sharp from "sharp";
import { mkdirSync } from "node:fs";
import { join } from "node:path";
import { OUT, manifest, save } from "./manifest.mjs";

/** Sprites at 1.5× the world scale (like the buildings), supersampled 2×2 */
const RES = 1.5;
const SS = 2;
/** A tile's box in world pixels, with room above it for the fittings */
const TILE_W = 192;
const TILE_H = 96;
const TOP = 56;
const W = TILE_W * RES;
const H = (TILE_H + TOP) * RES;
const SW = W * SS;
const SH = H * SS;
/** World pixels per unit of height (a 30° camera over the 2:1 tiles) */
const ZS = 117.58;

// The camera looks along D (30° down, from the bottom of the screen); the light comes from the top left
const D = [-0.61237, -0.61237, -0.5];
const V = [0.61237, 0.61237, 0.5];
const L = normalize([-0.62, -0.2, 0.76]);
const HALF = normalize([L[0] + V[0], L[1] + V[1], L[2] + V[2]]);

// The pipe
const R = 0.108; // tube radius
const Z = 0.175; // its centre above the ground
const RF = 0.15; // flange radius
const WF = 0.024; // flange half thickness
const EXT = 0.3; // how far the neighbours' tubes are drawn (for outlines and shadows only)

function normalize(v) {
  const l = Math.hypot(v[0], v[1], v[2]);
  return [v[0] / l, v[1] / l, v[2] / l];
}

const clamp = (v, a = 0, b = 1) => (v < a ? a : v > b ? b : v);

// ------------------------------------------------------------------ Signed distances

/** A tube along x (y, z fixed) from a to b, flat ends */
function tubeX(y0, z0, r, a, b) {
  return (x, y, z) => Math.max(Math.hypot(y - y0, z - z0) - r, Math.abs(x - (a + b) / 2) - (b - a) / 2);
}
function tubeY(x0, z0, r, a, b) {
  return (x, y, z) => Math.max(Math.hypot(x - x0, z - z0) - r, Math.abs(y - (a + b) / 2) - (b - a) / 2);
}
/** A tube bent along a horizontal arc around (cx, cy) with radius rr, between two angles */
function arc(cx, cy, rr, a0, a1, z0, r) {
  return (x, y, z) => {
    let a = Math.atan2(y - cy, x - cx);
    // Into [a0, a0 + 2π), then the nearest angle within the arc
    a = a0 + ((((a - a0) % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI));
    if (a > a1) {
      const d0 = Math.abs(Math.atan2(Math.sin(a - a0), Math.cos(a - a0)));
      const d1 = Math.abs(Math.atan2(Math.sin(a - a1), Math.cos(a - a1)));
      a = d0 < d1 ? a0 : a1;
    }
    const qx = cx + rr * Math.cos(a);
    const qy = cy + rr * Math.sin(a);
    return Math.hypot(x - qx, y - qy, z - z0) - r;
  };
}
/** A disc across x at x0 (a flange), slightly rounded */
function discX(x0, y0, z0, r, w) {
  return (x, y, z) => {
    const dr = Math.hypot(y - y0, z - z0) - r + 0.006;
    const dx = Math.abs(x - x0) - w + 0.006;
    return Math.min(Math.max(dr, dx), 0) + Math.hypot(Math.max(dr, 0), Math.max(dx, 0)) - 0.006;
  };
}
function discY(x0, y0, z0, r, w) {
  const f = discX(y0, x0, z0, r, w);
  return (x, y, z) => f(y, x, z);
}
/** A vertical cylinder (a post, the hub), rounded */
function post(cx, cy, r, z0, z1, round = 0.012) {
  return (x, y, z) => {
    const dr = Math.hypot(x - cx, y - cy) - r + round;
    const dz = Math.abs(z - (z0 + z1) / 2) - (z1 - z0) / 2 + round;
    return Math.min(Math.max(dr, dz), 0) + Math.hypot(Math.max(dr, 0), Math.max(dz, 0)) - round;
  };
}
/** A rounded box */
function box(cx, cy, cz, hx, hy, hz, round = 0.01) {
  return (x, y, z) => {
    const qx = Math.abs(x - cx) - hx + round;
    const qy = Math.abs(y - cy) - hy + round;
    const qz = Math.abs(z - cz) - hz + round;
    return Math.hypot(Math.max(qx, 0), Math.max(qy, 0), Math.max(qz, 0)) + Math.min(Math.max(qx, qy, qz), 0) - round;
  };
}
/** A ring (torus) around an axis through c: the axis is t (unit), the ring lies across it */
function ring(c, t, rr, r) {
  return (x, y, z) => {
    const qx = x - c[0];
    const qy = y - c[1];
    const qz = z - c[2];
    const along = qx * t[0] + qy * t[1] + qz * t[2];
    const px = qx - along * t[0];
    const py = qy - along * t[1];
    const pz = qz - along * t[2];
    return Math.hypot(Math.hypot(px, py, pz) - rr, along) - r;
  };
}
/** A horizontal ring (the valve wheel) */
function wheel(cx, cy, cz, rr, r) {
  return (x, y, z) => Math.hypot(Math.hypot(x - cx, y - cy) - rr, z - cz) - r;
}
/** A capsule between two points */
function capsule(a, b, r) {
  const bax = b[0] - a[0];
  const bay = b[1] - a[1];
  const baz = b[2] - a[2];
  const dd = bax * bax + bay * bay + baz * baz;
  return (x, y, z) => {
    const pax = x - a[0];
    const pay = y - a[1];
    const paz = z - a[2];
    const h = clamp((pax * bax + pay * bay + paz * baz) / dd);
    return Math.hypot(pax - bax * h, pay - bay * h, paz - baz * h) - r;
  };
}

// ------------------------------------------------------------------ Pieces

const SIDES = {
  u: { mid: [0.5, 0], axis: "y", out: -1 },
  r: { mid: [1, 0.5], axis: "x", out: 1 },
  d: { mid: [0.5, 1], axis: "y", out: 1 },
  l: { mid: [0, 0.5], axis: "x", out: -1 },
};

/** Bolts around a flange across the axis at a point */
function bolts(prims, axis, at, count, radius, w, material, phase = Math.PI / count) {
  for (let i = 0; i < count; i++) {
    const a = phase + (i * 2 * Math.PI) / count;
    const off = radius * Math.cos(a);
    const zz = Z + radius * Math.sin(a);
    if (axis === "x") {
      prims.push({ d: tubeX(at[1] + off, zz, 0.011, at[0] - w - 0.009, at[0] + w + 0.009), m: material });
    } else {
      prims.push({ d: tubeY(at[0] + off, zz, 0.011, at[1] - w - 0.009, at[1] + w + 0.009), m: material });
    }
  }
}

function flange(prims, side, at, r = RF, w = WF, count = 6) {
  const axis = SIDES[side].axis;
  prims.push({ d: axis === "x" ? discX(at[0], at[1], Z, r, w) : discY(at[0], at[1], Z, r, w), m: "flange" });
  bolts(prims, axis, at, count, r - 0.024, w, "bolt");
}

/** A tube from the tile's centre (or from `from` along the axis) out over the side's edge */
function arm(prims, side, from = 0.5, beyond = EXT) {
  const s = SIDES[side];
  const [a, b] = s.out > 0 ? [from, 1 + beyond] : [-beyond, 1 - from];
  prims.push(
    s.axis === "x"
      ? { d: tubeX(0.5, Z, R, a, b), m: "pipe", u: (x) => x }
      : { d: tubeY(0.5, Z, R, a, b), m: "pipe", u: (_, y) => y },
  );
}

/** A concrete foot with an iron clamp around the tube, at a point of it with the tube's direction */
function support(prims, at, dir, clamp = true) {
  prims.push({ d: post(at[0], at[1], 0.066, 0, Z - R * 0.55, 0.016), m: "foot" });
  if (clamp) {
    prims.push({ d: ring([at[0], at[1], Z], dir, R + 0.005, 0.0085), m: "clamp" });
  }
}

/** The tube along an axis through the whole tile: hides what is behind it in an overlay, without being drawn */
function occluder(prims, axis) {
  prims.push(axis === "x" ? { d: tubeX(0.5, Z, R, -EXT, 1 + EXT), m: "hidden" } : { d: tubeY(0.5, Z, R, -EXT, 1 + EXT), m: "hidden" });
}

/** Where a building's connectors leave the pipe, across the side (1, 2 or 3 of them: one per kind of good, §9) */
const LANE_SLOTS = [0.27, 0.5, 0.73];

/**
 * The manifold where a pipe meets a building on a side: a short tube across the tile's middle, parallel to the
 * building's edge, that the connectors leave from (a pipe running past the building is its own manifold)
 */
function manifold(prims, side, connections = "") {
  const s = SIDES[side];
  const [a, b] = [LANE_SLOTS[0] - 0.09, LANE_SLOTS[2] + 0.09];
  const [low, high] = s.axis === "x" ? ["u", "d"] : ["l", "r"];
  const start = connections.includes(low) ? -EXT : a;
  const stop = connections.includes(high) ? 1 + EXT : b;
  // This is the actual pipe, including its incoming arms. Never layer a capped end or a bend over it.
  prims.push(s.axis === "x" ? { d: tubeY(0.5, Z, R, start, stop), m: "pipe", u: (_, y) => y } : { d: tubeX(0.5, Z, R, start, stop), m: "pipe", u: (x) => x });
  for (const end of [connections.includes(low) ? null : a, connections.includes(high) ? null : b]) {
    if (end === null) continue;
    prims.push(s.axis === "x" ? { d: discY(0.5, end, Z, R * 1.05, 0.02), m: "cap" } : { d: discX(end, 0.5, Z, R * 1.05, 0.02), m: "cap" });
  }
  for (const incoming of connections) {
    if (incoming !== low && incoming !== high) arm(prims, incoming, 0.5);
  }
  prims.push(s.axis === "x" ? { d: tubeY(0.5, Z, R * 1.12, 0.46, 0.54), m: "collar" } : { d: tubeX(0.5, Z, R * 1.12, 0.46, 0.54), m: "collar" });
  support(prims, [0.5, 0.5], s.axis === "x" ? [0, 1, 0] : [1, 0, 0], false);
}

/**
 * A connector from the pipe into a building on a side, at a slot across it: energy on two copper cables over a little
 * post, solids on a short belt on legs, fluids in a slim pipe with a red valve wheel; a brass hatch on the pipe where
 * it leaves (its trim is the tier's)
 */
function lane(prims, kind, side, slot) {
  const s = SIDES[side];
  const edge = s.axis === "x" ? s.mid[0] : s.mid[1];
  const lat = LANE_SLOTS[slot];
  // A point at a distance from the building's edge into the tile, across it at l
  const pt = (t, l = lat) => (s.axis === "x" ? [edge - s.out * t, l] : [l, edge - s.out * t]);
  const from = 0.5 - R * 0.8;
  const end = 0.012;
  const across = (half, alongHalf) => (s.axis === "x" ? [alongHalf, half] : [half, alongHalf]);
  // The hatch on the pipe's side
  const hatch = pt(from - 0.012);
  const [hx, hy] = across(0.05, 0.022);
  prims.push({ d: box(hatch[0], hatch[1], Z, hx, hy, 0.05, 0.01), m: "flange" });
  if (kind === "power") {
    for (const k of [-1, 1]) {
      const l = lat + k * 0.024;
      const a = pt(from - 0.02, l);
      const m = pt(0.22, l);
      const b = pt(end, l);
      prims.push({ d: capsule([a[0], a[1], Z], [m[0], m[1], 0.2], 0.013), m: "cable" });
      prims.push({ d: capsule([m[0], m[1], 0.2], [b[0], b[1], 0.15], 0.013), m: "cable" });
    }
    const p = pt(0.22);
    prims.push({ d: post(p[0], p[1], 0.011, 0, 0.19, 0.004), m: "clamp" });
    const [cx, cy] = across(0.04, 0.012);
    prims.push({ d: box(p[0], p[1], 0.195, cx, cy, 0.01, 0.004), m: "clamp" });
  } else if (kind === "solid") {
    const mid = pt((from + end) / 2);
    const half = (from - end) / 2;
    const [bx, by] = across(0.045, half);
    prims.push({ d: box(mid[0], mid[1], 0.105, bx, by, 0.012, 0.006), m: "rubber" });
    for (const k of [-1, 1]) {
      const r = pt((from + end) / 2, lat + k * 0.052);
      const [rx, ry] = across(0.008, half);
      prims.push({ d: box(r[0], r[1], 0.115, rx, ry, 0.018, 0.004), m: "collar" });
    }
    for (const t of [end + 0.05, from - 0.04]) {
      for (const k of [-1, 1]) {
        const leg = pt(t, lat + k * 0.04);
        prims.push({ d: post(leg[0], leg[1], 0.008, 0, 0.1, 0.003), m: "clamp" });
      }
    }
  } else {
    const a = pt(from - 0.01);
    const b = pt(end);
    const z = Z - 0.03;
    prims.push({ d: capsule([a[0], a[1], z], [b[0], b[1], z], 0.032), m: "slim" });
    const w = pt(0.2);
    prims.push({ d: capsule([w[0], w[1], z], [w[0], w[1], z + 0.075], 0.009), m: "clamp" });
    prims.push({ d: wheel(w[0], w[1], z + 0.08, 0.04, 0.009), m: "valve" });
    for (const t of [end + 0.03, from - 0.03]) {
      const c = pt(t);
      prims.push({ d: ring([c[0], c[1], z], s.axis === "x" ? [1, 0, 0] : [0, 1, 0], 0.036, 0.008), m: "collar" });
    }
  }
}

function piece(shape) {
  const prims = [];
  if (shape.startsWith("feed-")) {
    const [, side, connections] = shape.split("-");
    manifold(prims, side, connections === "none" ? "" : connections);
    return prims;
  }
  if (shape.startsWith("manifold-")) {
    manifold(prims, shape.slice(9));
    return prims;
  }
  if (shape.startsWith("lane-")) {
    const [, kind, side, slot] = shape.split("-");
    lane(prims, kind, side, Number(slot));
    return prims;
  }
  // Overlays, drawn over a piece where the game wants them: a flange at an edge (every few tiles), a foot under a
  // straight tube (every other tile)
  if (shape.startsWith("flange-")) {
    const side = shape.slice(7);
    flange(prims, side, SIDES[side].mid);
    occluder(prims, SIDES[side].axis);
    return prims;
  }
  if (shape.startsWith("foot-")) {
    const axis = shape.slice(5);
    support(prims, [0.5, 0.5], axis === "x" ? [1, 0, 0] : [0, 1, 0]);
    occluder(prims, axis);
    return prims;
  }
  // A tee: a straight tube with a short branch to one side (where a building beside it plugs in), no hub
  if (shape.startsWith("tee-")) {
    const side = shape.slice(4);
    const s = SIDES[side];
    const through = s.axis === "x" ? "y" : "x";
    prims.push(through === "x" ? { d: tubeX(0.5, Z, R, -EXT, 1 + EXT), m: "pipe", u: (x) => x } : { d: tubeY(0.5, Z, R, -EXT, 1 + EXT), m: "pipe", u: (_, y) => y });
    arm(prims, side, 0.5);
    // A welded collar where the branch leaves the tube
    const [a, b] = s.out > 0 ? [0.5 + R * 0.7, 0.5 + R * 1.45] : [0.5 - R * 1.45, 0.5 - R * 0.7];
    prims.push(s.axis === "x" ? { d: tubeX(0.5, Z, R * 1.2, a, b), m: "collar" } : { d: tubeY(0.5, Z, R * 1.2, a, b), m: "collar" });
    support(prims, [0.5, 0.5], through === "x" ? [1, 0, 0] : [0, 1, 0]);
    return prims;
  }
  // A building socket: an inlet box at the building's edge that the pipe runs into, all inside the pipe's tile (a
  // neighbouring building completes nothing beyond the edge)
  if (shape.startsWith("socket-")) {
    const side = shape.slice(7);
    const s = SIDES[side];
    const edge = s.axis === "x" ? s.mid[0] : s.mid[1];
    const along = (t) => edge - s.out * t;
    const at = (t) => (s.axis === "x" ? [along(t), 0.5] : [0.5, along(t)]);
    const [hx, hy] = s.axis === "x" ? [0.065, 0.15] : [0.15, 0.065];
    const top = Z + R + 0.055;
    const c = at(0.08);
    prims.push({ d: box(c[0], c[1], top / 2, hx, hy, top / 2, 0.022), m: "collar" });
    prims.push({ d: box(c[0], c[1], top + 0.012, hx + 0.012, hy + 0.012, 0.014, 0.01), m: "cap" });
    for (const k of [-1, 1]) {
      // Two bolts on the lid
      const b = s.axis === "x" ? [c[0], 0.5 + k * 0.09] : [0.5 + k * 0.09, c[1]];
      prims.push({ d: post(b[0], b[1], 0.013, top + 0.02, top + 0.036, 0.005), m: "bolt" });
    }
    flange(prims, side, at(0.155), RF, 0.018, 6);
    // The tube up to the box, hiding what is behind it
    const [a, b] = s.out > 0 ? [-EXT, along(0.08)] : [along(0.08), 1 + EXT];
    prims.push(s.axis === "x" ? { d: tubeX(0.5, Z, R, a, b), m: "hidden" } : { d: tubeY(0.5, Z, R, a, b), m: "hidden" });
    return prims;
  }
  const sides = [...shape];
  const opposite = (a, b) => (a === "u" && b === "d") || (a === "d" && b === "u") || (a === "r" && b === "l") || (a === "l" && b === "r");
  if (sides.length === 2 && opposite(sides[0], sides[1])) {
    // Straight: one tube (its flanges and its foot are overlays)
    const axis = SIDES[sides[0]].axis;
    prims.push(axis === "x" ? { d: tubeX(0.5, Z, R, -EXT, 1 + EXT), m: "pipe", u: (x) => x } : { d: tubeY(0.5, Z, R, -EXT, 1 + EXT), m: "pipe", u: (_, y) => y });
    return prims;
  }
  if (sides.length === 2) {
    // A bend around the tile corner between the two sides
    const has = (t) => sides.includes(t);
    const corner = [has("r") ? 1 : 0, has("d") ? 1 : 0];
    const angles = sides.map((t) => Math.atan2(SIDES[t].mid[1] - corner[1], SIDES[t].mid[0] - corner[0]));
    let [a0, a1] = angles.sort((p, q) => p - q);
    // The quarter inside the tile (u+l around (0,0) spans 0..π/2; others may wrap)
    if (a1 - a0 > Math.PI) {
      [a0, a1] = [a1, a0 + 2 * Math.PI];
    }
    const arcD = arc(corner[0], corner[1], 0.5, a0, a1, Z, R);
    prims.push({ d: arcD, m: "pipe", u: (x, y) => Math.atan2(y - corner[1], x - corner[0]) * 0.5 });
    for (const side of sides) {
      arm(prims, side, 1, EXT);
    }
    const mid = (a0 + a1) / 2;
    const at = [corner[0] + 0.5 * Math.cos(mid), corner[1] + 0.5 * Math.sin(mid)];
    // A foot under the middle of the bend (a clamp would look like a seam across it)
    support(prims, at, [-Math.sin(mid), Math.cos(mid), 0], false);
    return prims;
  }
  // An end, a junction or a lone piece: a cast hub (or a blind flange) in the middle
  if (sides.length === 1) {
    const side = sides[0];
    arm(prims, side, 0.5);
    // The blind flange that closes it
    const s = SIDES[side];
    const at = s.axis === "x" ? [0.5 - s.out * 0.0, 0.5] : [0.5, 0.5];
    flange(prims, side, at, RF * 1.05, 0.03, 6);
    prims.push(s.axis === "x" ? { d: discX(at[0] - s.out * 0.035, 0.5, Z, R * 0.55, 0.012), m: "cap" } : { d: discY(0.5, at[1] - s.out * 0.035, Z, R * 0.55, 0.012), m: "cap" });
    support(prims, s.axis === "x" ? [0.5 + s.out * 0.2, 0.5] : [0.5, 0.5 + s.out * 0.2], s.axis === "x" ? [1, 0, 0] : [0, 1, 0]);
    return prims;
  }
  const hubR = 0.165;
  const top = Z + 0.125;
  prims.push({ d: post(0.5, 0.5, hubR, 0.02, top, 0.02), m: "hub" });
  prims.push({ d: box(0.5, 0.5, 0.012, 0.2, 0.2, 0.012, 0.012), m: "pad" });
  prims.push({ d: post(0.5, 0.5, hubR + 0.018, top - 0.012, top + 0.022, 0.01), m: "cap" });
  for (let i = 0; i < 8; i++) {
    const a = Math.PI / 8 + (i * Math.PI) / 4;
    prims.push({ d: post(0.5 + (hubR + 0.002) * Math.cos(a), 0.5 + (hubR + 0.002) * Math.sin(a), 0.011, top + 0.018, top + 0.034, 0.005), m: "bolt" });
  }
  for (const side of sides) {
    arm(prims, side, 0.5 + hubR * 0.6);
    const s = SIDES[side];
    const at = s.axis === "x" ? [0.5 + s.out * (hubR + 0.012), 0.5] : [0.5, 0.5 + s.out * (hubR + 0.012)];
    flange(prims, side, at, RF * 0.98, 0.016, 6);
  }
  if (sides.length === 4 || sides.length === 0) {
    // The valve: a stem and a red wheel with three spokes
    const wz = top + 0.1;
    prims.push({ d: capsule([0.5, 0.5, top + 0.02], [0.5, 0.5, wz], 0.016), m: "clamp" });
    prims.push({ d: wheel(0.5, 0.5, wz, 0.082, 0.014), m: "valve" });
    for (let i = 0; i < 3; i++) {
      const a = (i * 2 * Math.PI) / 3 + 0.3;
      prims.push({ d: capsule([0.5, 0.5, wz], [0.5 + 0.08 * Math.cos(a), 0.5 + 0.08 * Math.sin(a), wz], 0.009), m: "valve" });
    }
    prims.push({ d: post(0.5, 0.5, 0.022, wz - 0.012, wz + 0.014, 0.008), m: "valve" });
  } else {
    // A pressure gauge on a T
    const gz = top + 0.06;
    prims.push({ d: capsule([0.5, 0.5, top + 0.02], [0.5, 0.5, gz - 0.02], 0.013), m: "clamp" });
    prims.push({ d: box(0.5, 0.5, gz + 0.015, 0.045, 0.045, 0.035, 0.03), m: "gauge" });
  }
  return prims;
}

/** The core of a glass pipe: a thin glowing line along the tube's path */
function core(shape) {
  const prims = [];
  if (shape.includes("-") && !shape.startsWith("tee-") && !shape.startsWith("feed-")) {
    return prims;
  }
  for (const p of piece(shape)) {
    if (p.m === "pipe") {
      prims.push(p);
    }
  }
  return prims.map((p) => ({ d: (x, y, z) => p.d(x, y, z) + R - 0.042, m: "core", u: p.u }));
}

// ------------------------------------------------------------------ Raymarching

function makeScene(prims) {
  const n = prims.length;
  const ds = prims.map((p) => p.d);
  const sdf = (x, y, z) => {
    let best = 1e9;
    for (let i = 0; i < n; i++) {
      const v = ds[i](x, y, z);
      if (v < best) {
        best = v;
      }
    }
    return best;
  };
  const nearest = (x, y, z) => {
    let best = 1e9;
    let index = -1;
    for (let i = 0; i < n; i++) {
      const v = ds[i](x, y, z);
      if (v < best) {
        best = v;
        index = i;
      }
    }
    return index;
  };
  return { sdf, nearest, prims };
}

const INSIDE = 0.003;
// Surfaces overlap their neighbours a hair (no gaps); the ground's shadow belongs to exactly one tile (no dark seams)
const inside = (x, y) => x >= -INSIDE && x <= 1 + INSIDE && y >= -INSIDE && y <= 1 + INSIDE;
const ownGround = (x, y) => x >= 0 && x < 1 && y >= 0 && y < 1;

/** The ray of a supersampled pixel: its start (high above) */
function rayStart(sx, sy) {
  const X = (sx + 0.5) / (RES * SS);
  const Y = (sy + 0.5) / (RES * SS) - TOP;
  const z0 = 0.75;
  const a = (X - 96) / 96;
  const b = (Y + z0 * ZS) / 48;
  return [(a + b) / 2, (b - a) / 2, z0];
}

function march(scene, o, maxT) {
  let t = 0;
  for (let i = 0; i < 160 && t < maxT; i++) {
    const x = o[0] + D[0] * t;
    const y = o[1] + D[1] * t;
    const z = o[2] + D[2] * t;
    const d = scene.sdf(x, y, z);
    if (d < 0.0004) {
      return t;
    }
    t += Math.max(d * 0.9, 0.0006);
  }
  return -1;
}

function normalAt(sdf, x, y, z) {
  const e = 0.0008;
  return normalize([
    sdf(x + e, y, z) - sdf(x - e, y, z),
    sdf(x, y + e, z) - sdf(x, y - e, z),
    sdf(x, y, z + e) - sdf(x, y, z - e),
  ]);
}

/** Soft shadow toward the light (1: lit) */
function shadow(sdf, x, y, z, k = 10) {
  let res = 1;
  let t = 0.012;
  for (let i = 0; i < 48 && t < 0.9; i++) {
    const d = sdf(x + L[0] * t, y + L[1] * t, z + L[2] * t);
    if (d < 0.0005) {
      return 0;
    }
    res = Math.min(res, (k * d) / t);
    t += clamp(d, 0.006, 0.08);
  }
  return clamp(res);
}

function occlusion(sdf, x, y, z, n) {
  let occ = 0;
  let scale = 1;
  for (let i = 1; i <= 5; i++) {
    const h = 0.012 * i * i * 0.6 + 0.004;
    const d = sdf(x + n[0] * h, y + n[1] * h, z + n[2] * h);
    occ += (h - d) * scale;
    scale *= 0.6;
  }
  return clamp(1 - occ * 3.2);
}

/**
 * The geometry of a piece at the supersampled resolution: for every pixel whether a surface is hit (in this tile or
 * a neighbour's, for the outlines), where, its normal, material, light and occlusion; for the ground the shadow
 */
function render(prims) {
  const scene = makeScene(prims);
  const count = SW * SH;
  const g = {
    any: new Uint8Array(count),
    mine: new Uint8Array(count),
    depth: new Float32Array(count),
    nx: new Float32Array(count),
    ny: new Float32Array(count),
    nz: new Float32Array(count),
    mat: new Int16Array(count).fill(-1),
    hidden: new Uint8Array(count),
    u: new Float32Array(count),
    light: new Float32Array(count),
    ao: new Float32Array(count),
    ground: new Float32Array(count),
    groundMine: new Uint8Array(count),
    pz: new Float32Array(count),
  };
  for (let sy = 0; sy < SH; sy++) {
    for (let sx = 0; sx < SW; sx++) {
      const i = sy * SW + sx;
      const o = rayStart(sx, sy);
      const maxT = o[2] / -D[2];
      const t = march(scene, o, maxT);
      if (t >= 0) {
        const x = o[0] + D[0] * t;
        const y = o[1] + D[1] * t;
        const z = o[2] + D[2] * t;
        g.any[i] = 1;
        g.depth[i] = t;
        if (!inside(x, y)) {
          continue;
        }
        g.mine[i] = 1;
        const n = normalAt(scene.sdf, x, y, z);
        g.nx[i] = n[0];
        g.ny[i] = n[1];
        g.nz[i] = n[2];
        const index = scene.nearest(x, y, z);
        g.mat[i] = index;
        const prim = prims[index];
        g.hidden[i] = prim.m === "hidden" ? 1 : 0;
        g.u[i] = prim.u ? prim.u(x, y, z) : 0;
        g.pz[i] = z;
        const lit = Math.max(0, n[0] * L[0] + n[1] * L[1] + n[2] * L[2]);
        g.light[i] = lit > 0 ? shadow(scene.sdf, x + n[0] * 0.003, y + n[1] * 0.003, z + n[2] * 0.003) : 0;
        g.ao[i] = occlusion(scene.sdf, x, y, z, n);
      } else {
        // The ground: the shadow and the contact darkness of the pipe
        const x = o[0] + D[0] * maxT;
        const y = o[1] + D[1] * maxT;
        g.depth[i] = maxT;
        if (ownGround(x, y)) {
          g.groundMine[i] = 1;
          const sh = shadow(scene.sdf, x, y, 0.001, 6);
          const near = clamp(scene.sdf(x, y, 0.02) / 0.09);
          g.ground[i] = clamp((1 - sh) * 0.42 + (1 - near) * 0.28, 0, 0.55);
        }
      }
    }
  }
  return { g, prims };
}

// ------------------------------------------------------------------ Shading per tier

const hex = (h) => [parseInt(h.slice(1, 3), 16) / 255, parseInt(h.slice(3, 5), 16) / 255, parseInt(h.slice(5, 7), 16) / 255];

const CONCRETE = { color: hex("#b8ad98"), spec: 0.05, shine: 8 };
const PAD = { color: hex("#a39a86"), spec: 0.03, shine: 6 };
const IRON = { color: hex("#3f4248"), spec: 0.35, shine: 30 };
const BRASS = { color: hex("#d2a24a"), spec: 0.75, shine: 50 };
const BOLT_DARK = { color: hex("#5c5040"), spec: 0.4, shine: 30 };
const RED = { color: hex("#c8402c"), spec: 0.55, shine: 40 };
const RUBBER = { color: hex("#2f2c2a"), spec: 0.12, shine: 10 };
const CABLE = { color: hex("#e08a45"), spec: 0.9, shine: 40 };
const SLIM = { color: hex("#3f8fcf"), spec: 0.7, shine: 50 };

/** The materials of each tier (pipe and hub follow the tier's colour, see PIPE_COLORS in the game): grey, copper, steel,
 * composite (charcoal carbon fibre, orange fittings), glass, superconductor */
const TIERS = {
  1: { pipe: { color: hex("#8e979f"), spec: 0.42, shine: 30 }, flange: BRASS, collar: { color: hex("#a5adb4"), spec: 0.45, shine: 30 }, bolt: BOLT_DARK, hub: { color: hex("#8a939b"), spec: 0.5, shine: 30 }, cap: BRASS, gauge: { color: hex("#f2ead8"), spec: 0.6, shine: 60 } },
  2: { pipe: { color: hex("#cf7d3e"), spec: 0.85, shine: 46 }, flange: { color: hex("#5b4636"), spec: 0.45, shine: 30 }, collar: { color: hex("#b8743e"), spec: 0.6, shine: 40 }, bolt: BRASS, hub: { color: hex("#b86a32"), spec: 0.8, shine: 44 }, cap: { color: hex("#5b4636"), spec: 0.45, shine: 30 }, gauge: { color: hex("#f2ead8"), spec: 0.6, shine: 60 } },
  3: { pipe: { color: hex("#5f7a92"), spec: 0.7, shine: 44 }, flange: BRASS, collar: { color: hex("#6f88a0"), spec: 0.55, shine: 40 }, bolt: BOLT_DARK, hub: { color: hex("#536b81"), spec: 0.65, shine: 40 }, cap: BRASS, gauge: { color: hex("#f2ead8"), spec: 0.6, shine: 60 } },
  4: { pipe: { color: hex("#3d4147"), spec: 0.7, shine: 55 }, flange: { color: hex("#e07b39"), spec: 0.55, shine: 40 }, collar: { color: hex("#4a4f56"), spec: 0.6, shine: 45 }, bolt: { color: hex("#2a2c30"), spec: 0.4, shine: 30 }, hub: { color: hex("#373a40"), spec: 0.65, shine: 50 }, cap: { color: hex("#e07b39"), spec: 0.55, shine: 40 }, gauge: { color: hex("#f2ead8"), spec: 0.6, shine: 60 } },
  5: { pipe: { color: hex("#a9e7f7"), spec: 1.0, shine: 80, glass: true }, flange: { color: hex("#8a949c"), spec: 0.6, shine: 40 }, collar: { color: hex("#9aa4ac"), spec: 0.55, shine: 40 }, bolt: BRASS, hub: { color: hex("#7f8b94"), spec: 0.6, shine: 40 }, cap: BRASS, gauge: { color: hex("#f2ead8"), spec: 0.6, shine: 60 }, core: hex("#6ff0ff") },
  6: { pipe: { color: hex("#3a2f5e"), spec: 0.9, shine: 60, bands: hex("#c49bff") }, flange: { color: hex("#c3ccd6"), spec: 0.9, shine: 70 }, collar: { color: hex("#4a3d78"), spec: 0.7, shine: 50 }, bolt: { color: hex("#2c2448"), spec: 0.5, shine: 30 }, hub: { color: hex("#2f2650"), spec: 0.85, shine: 60 }, cap: { color: hex("#c3ccd6"), spec: 0.9, shine: 70 }, gauge: { color: hex("#d9c8ff"), spec: 0.6, shine: 60 }, core: hex("#b98cff") },
};

function material(tier, name) {
  const t = TIERS[tier];
  switch (name) {
    case "foot":
      return CONCRETE;
    case "pad":
      return PAD;
    case "clamp":
      return IRON;
    case "valve":
      return RED;
    case "rubber":
      return RUBBER;
    case "cable":
      return CABLE;
    case "slim":
      return SLIM;
    default:
      return t[name] ?? t.pipe;
  }
}

const WARM = [1.0, 0.95, 0.84];
const SKY = [0.62, 0.72, 0.86];
const OUTLINE = hex("#1d1a16");

/** The colour of a surface pixel (straight alpha) */
function shadeSurface(g, i, prims, tier) {
  const name = prims[g.mat[i]].m;
  const m = material(tier, name);
  const n = [g.nx[i], g.ny[i], g.nz[i]];
  const ndl = n[0] * L[0] + n[1] * L[1] + n[2] * L[2];
  // Soft painted light: a wide wrap, a strong sky fill, gentle occlusion
  const wrap = clamp((ndl + 0.45) / 1.45);
  const lit = wrap * (0.35 + 0.65 * g.light[i]);
  const sky = 0.55 + 0.45 * n[2];
  const ao = 0.62 + 0.38 * g.ao[i];
  // Lower parts are a little darker, like the generated art's grounded look
  const ground = clamp(0.84 + g.pz[i] * 1.0, 0.84, 1);
  const ndh = Math.max(0, n[0] * HALF[0] + n[1] * HALF[1] + n[2] * HALF[2]);
  const spec = Math.pow(ndh, m.shine * 0.6) * m.spec * 0.75 * (0.25 + 0.75 * g.light[i]);
  const rim = Math.pow(1 - clamp(n[0] * V[0] + n[1] * V[1] + n[2] * V[2]), 3) * 0.12;
  let base = m.color;
  if (m.bands && name === "pipe") {
    // Superconductor: glowing bands along the tube
    const f = ((g.u[i] * 5) % 1 + 1) % 1;
    if (f < 0.16) {
      return [m.bands[0] * 1.15, m.bands[1] * 1.1, m.bands[2] * 1.05, 1];
    }
  }
  const c = [0, 0, 0];
  for (let k = 0; k < 3; k++) {
    const ambient = base[k] * (0.54 * ao + 0.24 * sky * SKY[k]) * ground;
    const diffuse = base[k] * lit * 0.52 * WARM[k] * ground;
    c[k] = ambient + diffuse + spec * WARM[k] + rim * SKY[k];
  }
  if (m.glass) {
    // Glass: see-through, a tint and bright reflections; the core glows inside (added in compose)
    const fres = Math.pow(1 - clamp(n[0] * V[0] + n[1] * V[1] + n[2] * V[2]), 2);
    return [c[0], c[1], c[2], clamp(0.32 + fres * 0.55 + spec * 0.6)];
  }
  return [c[0], c[1], c[2], 1];
}

/** Outline strength of a pixel: at a silhouette, or where a nearer surface starts next to it */
function outline(g, i, sx, sy) {
  if (!g.mine[i]) {
    return 0;
  }
  const radius = 2.6 * SS * RES * 0.68;
  const r = Math.ceil(radius);
  let edge = 0;
  for (let dy = -r; dy <= r; dy++) {
    for (let dx = -r; dx <= r; dx++) {
      const dist = Math.hypot(dx, dy);
      if (dist > radius || (dx === 0 && dy === 0)) {
        continue;
      }
      const x = sx + dx;
      const y = sy + dy;
      if (x < 0 || y < 0 || x >= SW || y >= SH) {
        continue;
      }
      const j = y * SW + x;
      const weight = clamp(radius - dist + 0.5);
      if (!g.any[j]) {
        edge = Math.max(edge, weight);
      } else if (g.depth[j] < g.depth[i] - 0.03) {
        edge = Math.max(edge, weight * 0.9);
      } else if (g.mine[j] && g.mat[j] !== g.mat[i] && Math.abs(g.depth[j] - g.depth[i]) < 0.03 && !g.hidden[j]) {
        // Between two parts at the same depth (a flange on its tube): a thin line
        const nd = g.nx[i] * g.nx[j] + g.ny[i] * g.ny[j] + g.nz[i] * g.nz[j];
        if (nd < 0.6 && dist < radius * 0.5) {
          edge = Math.max(edge, 0.55);
        }
      }
    }
  }
  return edge;
}

function compose(geometry, coreGeometry, tier, overlay) {
  const { g, prims } = geometry;
  const out = new Float32Array(SW * SH * 4);
  const t = TIERS[tier];
  for (let sy = 0; sy < SH; sy++) {
    for (let sx = 0; sx < SW; sx++) {
      const i = sy * SW + sx;
      let r = 0;
      let gg = 0;
      let b = 0;
      let a = 0;
      if (g.mine[i] && g.hidden[i]) {
        // Behind the occluder: the piece under the overlay shows here
      } else if (g.mine[i]) {
        let [cr, cg, cb, ca] = shadeSurface(g, i, prims, tier);
        const name = prims[g.mat[i]].m;
        if (t.core && name === "pipe" && coreGeometry && coreGeometry.g.mine[i]) {
          // The glowing core behind the glass (or under the superconductor's bands)
          const glow = t.core;
          if (TIERS[tier].pipe.glass) {
            const k = 1 - ca;
            cr = cr * ca + glow[0] * k;
            cg = cg * ca + glow[1] * k;
            cb = cb * ca + glow[2] * k;
            ca = 1;
          }
        } else if (TIERS[tier].pipe.glass && name === "pipe") {
          // Through the glass: a pale inside
          const k = 1 - ca;
          cr = cr * ca + 0.78 * k;
          cg = cg * ca + 0.93 * k;
          cb = cb * ca + 0.97 * k;
          ca = Math.max(ca, 0.72);
        }
        const o = outline(g, i, sx, sy);
        r = cr * (1 - o) + OUTLINE[0] * o;
        gg = cg * (1 - o) + OUTLINE[1] * o;
        b = cb * (1 - o) + OUTLINE[2] * o;
        a = ca * (1 - o) + o;
      } else if (g.groundMine[i] && g.ground[i] > 0.004 && !overlay) {
        r = 0.08;
        gg = 0.07;
        b = 0.05;
        a = g.ground[i];
      }
      out[i * 4] = r;
      out[i * 4 + 1] = gg;
      out[i * 4 + 2] = b;
      out[i * 4 + 3] = a;
    }
  }
  // Down to the sprite's resolution (premultiplied box filter), sRGB bytes
  const pixels = Buffer.alloc(W * H * 4);
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      let r = 0;
      let g2 = 0;
      let b = 0;
      let a = 0;
      for (let dy = 0; dy < SS; dy++) {
        for (let dx = 0; dx < SS; dx++) {
          const j = ((y * SS + dy) * SW + x * SS + dx) * 4;
          const aa = out[j + 3];
          r += out[j] * aa;
          g2 += out[j + 1] * aa;
          b += out[j + 2] * aa;
          a += aa;
        }
      }
      const k = (y * W + x) * 4;
      if (a > 0) {
        pixels[k] = Math.round(clamp(r / a) * 255);
        pixels[k + 1] = Math.round(clamp(g2 / a) * 255);
        pixels[k + 2] = Math.round(clamp(b / a) * 255);
      }
      pixels[k + 3] = Math.round(clamp(a / (SS * SS)) * 255);
    }
  }
  return pixels;
}

// ------------------------------------------------------------------ The atlas

function shapes() {
  const list = [];
  for (let mask = 0; mask < 16; mask++) {
    list.push(["u", "r", "d", "l"].filter((_, i) => mask & (1 << i)).join(""));
  }
  for (const side of ["u", "r", "d", "l"]) {
    list.push(`flange-${side}`, `socket-${side}`, `tee-${side}`, `manifold-${side}`);
    const other = ["u", "r", "d", "l"].filter((s) => s !== side);
    for (let mask = 0; mask < 8; mask++) {
      const connections = other.filter((_, i) => mask & (1 << i)).join("");
      list.push(`feed-${side}-${connections || "none"}`);
    }
    // The lanes into a building (1.4 §9): each kind of good at each of the three places across the side
    for (const kind of ["power", "solid", "fluid"]) {
      for (const slot of [0, 1, 2]) {
        list.push(`lane-${kind}-${side}-${slot}`);
      }
    }
  }
  return [...list, "foot-x", "foot-y"];
}

/** The opaque bounds of a frame */
function bounds(pixels) {
  let minX = W;
  let minY = H;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      if (pixels[(y * W + x) * 4 + 3] > 2) {
        minX = Math.min(minX, x);
        maxX = Math.max(maxX, x);
        minY = Math.min(minY, y);
        maxY = Math.max(maxY, y);
      }
    }
  }
  return maxX < 0 ? { x: 0, y: 0, w: 1, h: 1 } : { x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

async function main() {
  const only = process.argv[2];
  const list = only ? [only] : shapes();
  const frames = {};
  const rendered = [];
  for (const shape of list) {
    const started = Date.now();
    // A scratch look may join pieces with "+" (a pipe end, its manifold and lanes), rendered as one scene
    const parts = shape.split("+").map((t) => (t === "none" ? "" : t));
    const geometry = render(parts.flatMap(piece));
    const coreGeometry = render(parts.flatMap(core));
    const tiers = {};
    for (const tier of [1, 2, 3, 4, 5, 6]) {
      tiers[tier] = compose(geometry, coreGeometry, tier, shape.includes("-") && !shape.startsWith("tee-") && !shape.startsWith("feed-"));
    }
    rendered.push({ shape, tiers });
    console.log(`${shape || "(none)"}: ${Date.now() - started} ms`);
  }
  if (only) {
    mkdirSync(new URL("./scratch/", import.meta.url).pathname, { recursive: true });
    for (const tier of [1, 2, 3, 4, 5, 6]) {
      const file = new URL(`./scratch/pipe-${only}-${tier}.png`, import.meta.url).pathname;
      await sharp(rendered[0].tiers[tier], { raw: { width: W, height: H, channels: 4 } }).png().toFile(file);
      console.log(file);
    }
    return;
  }
  // One atlas per tier, the frames trimmed to what all tiers show of them, in rows
  const columns = 9;
  const boxes = rendered.map(({ tiers }) => {
    const all = Object.values(tiers).map(bounds);
    const x = Math.min(...all.map((t) => t.x));
    const y = Math.min(...all.map((t) => t.y));
    return { x, y, w: Math.max(...all.map((t) => t.x + t.w)) - x, h: Math.max(...all.map((t) => t.y + t.h)) - y };
  });
  const cellW = Math.max(...boxes.map((t) => t.w)) + 2;
  const cellH = Math.max(...boxes.map((t) => t.h)) + 2;
  const rows = Math.ceil(rendered.length / columns);
  rendered.forEach(({ shape }, index) => {
    const b = boxes[index];
    frames[shape] = {
      x: (index % columns) * cellW + 1,
      y: Math.floor(index / columns) * cellH + 1,
      w: b.w,
      h: b.h,
      ox: b.x,
      oy: b.y,
    };
  });
  mkdirSync(join(OUT, "pipes"), { recursive: true });
  for (const tier of [1, 2, 3, 4, 5, 6]) {
    const atlas = Buffer.alloc(columns * cellW * rows * cellH * 4);
    rendered.forEach(({ shape, tiers }) => {
      const f = frames[shape];
      const pixels = tiers[tier];
      for (let y = 0; y < f.h; y++) {
        for (let x = 0; x < f.w; x++) {
          const from = ((f.oy + y) * W + f.ox + x) * 4;
          const to = ((f.y + y) * columns * cellW + f.x + x) * 4;
          pixels.copy(atlas, to, from, from + 4);
        }
      }
    });
    await sharp(atlas, { raw: { width: columns * cellW, height: rows * cellH, channels: 4 } })
      .webp({ quality: 90, alphaQuality: 100 })
      .toFile(join(OUT, "pipes", `tier${tier}.webp`));
  }
  const m = manifest({});
  m.pipes = { resolution: RES, top: TOP, z: Z * ZS, tiers: [1, 2, 3, 4, 5, 6], frames };
  save(m);
  console.log(`${rendered.length} pieces, atlas ${columns * cellW}×${rows * cellH} per tier`);
}

await main();

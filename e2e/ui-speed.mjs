// How the map draws on a big estate (the owner, 2026-10-31: "now the bases are becoming a bit bigger, the game runs a
// bit slow in the UI"; his Firefox profile: 38% of the time PixiJS rebuilt its draw list, as smoke puffs came and went
// every frame, and 12% rebuilding every flow dot when any network changed): the estate of big-estate.mjs, its flows
// changing once a second as on a busy server, and for 8 s: how often PixiJS rebuilds its draw list and what that costs,
// the time a frame's drawing takes, and a flow rebuild's time. As on the live server (the owner's third profile: what
// the test only flagged, the game got as real changes): every second some networks' flows come anew, and map areas
// change as other players build. trace: also which draw lists PixiJS rebuilt, and what made it.
//   node ui-speed.mjs <outdir> [spacing] [trace]
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/ui-speed", spacing = "3", trace = ""] = process.argv.slice(2);
const p = await start(out);
await newPlayer(p.page, "spd");
for (let i = 0; i < 6; i++) await p.cheat("level");
for (let i = 0; i < 4; i++) await p.cheat("earn");
await p.cheat("alltech");
const grown = await p.page.evaluate(async (spacing) => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const me = () => s().company;
  // A parcel is bought next to the land owned: in passes outwards from the start area against the Yard (1.4 §5)
  for (let pass = 0; pass < 4; pass++) for (let parcel = 0; parcel < 16; parcel++) await c.buyParcel(parcel, 0);
  await new Promise((r) => setTimeout(r, 1500));
  const ox = me().estateX, oy = me().estateY;
  const types = ["windMoily", "solarPanel", "smallFactory", "lab", "storage", "solarPanel"];
  let pipes = 0, buildings = 0;
  const refused = [];
  // Pipe rows every third row, joined at alternate ends; buildings on the row above each pipe
  for (let row = 0; row < 10; row++) {
    const y = oy + 2 + row * 3;
    if (y + 1 >= oy + 31) break;
    const run = [];
    for (let x = ox + 1; x < ox + 31; x++) if (!w.isOccupied(x, y + 1)) run.push(x, y + 1);
    const laid = await c.placePipes(run, 1);
    if (laid.success) pipes += run.length / 2;
    else refused.push(laid.error);
    const turnX = row % 2 === 0 ? ox + 30 : ox + 1;
    const turn = [turnX, y + 2, turnX, y + 3];
    if ((await c.placePipes(turn, 1)).success) pipes += 2;
    for (let x = ox + 2; x < ox + 30; x += spacing) {
      for (const type of types.slice((x + row) % types.length).concat(types)) {
        const definition = s().content.building(type);
        if (definition && tp.rules.canPlace(s().content, w, me(), s().era, definition, x, y).ok) {
          if ((await c.placeBuilding(type, x, y)).success) { buildings++; break; }
        }
      }
    }
  }
  await c.cheat("finish");
  return { pipes, buildings, refused: refused.slice(0, 3) };
}, Number(spacing));
console.log("grown", JSON.stringify(grown));
await p.page.waitForTimeout(6000);
await p.page.evaluate(() => {
  const me = tp.appStore.getState().company;
  tp.gameClient.engine.lookAt(me.estateX + 16, me.estateY + 16, 0.6);
});
await p.page.waitForTimeout(4000);
await p.shot("1-estate");
const result = await p.page.evaluate(async (trace) => {
  const e = tp.gameClient.engine;
  const w = tp.gameClient.world;
  const system = e.app.renderer.renderGroup;
  const stats = { rebuilds: 0, rebuildMs: 0, frames: 0, renderMs: 0, flows: 0, flowsMs: 0 };
  const build = system._buildInstructions.bind(system);
  system._buildInstructions = (...args) => { const t = performance.now(); build(...args); stats.rebuilds++; stats.rebuildMs += performance.now() - t; };
  const renderer = e.app.renderer;
  const render = renderer.render.bind(renderer);
  renderer.render = (...args) => { const t = performance.now(); render(...args); stats.frames++; stats.renderMs += performance.now() - t; };
  const flows = e.buildFlows.bind(e);
  e.buildFlows = () => { const t = performance.now(); flows(); stats.flows++; stats.flowsMs += performance.now() - t; };
  // A frame's own time (the engine's update and the drawing): the stutters are the long ones
  const frames = [];
  const update = e.update;
  e.app.ticker.remove(update, e);
  e.app.ticker.add(function timed(ticker) { const t = performance.now(); update.call(e, ticker); frames.push(performance.now() - t); }, e);
  const draw = renderer.render.bind(renderer);
  renderer.render = (...args) => { const t = performance.now(); draw(...args); if (frames.length > 0) frames[frames.length - 1] += performance.now() - t; };
  // What made PixiJS rebuild a draw list: the first lines of our code on the way there, by layer
  const causes = new Map();
  const layerOf = (group) => {
    const root = group.root;
    for (const name of ["groundLayer", "flowLayer", "badgeLayer", "labelLayer"]) if (root === e[name]) return name;
    if (root === e.juice.layer) return "juice";
    if (root === e.app.stage) return "stage";
    return root.label || "band";
  };
  const watch = () => {
    const groups = [];
    const walk = (g) => { groups.push(g); for (const c of g.renderGroupChildren) walk(c); };
    walk(e.app.stage.renderGroup);
    for (const group of groups) {
      if (group.__watched) continue;
      group.__watched = true;
      let value = group.structureDidChange;
      Object.defineProperty(group, "structureDidChange", {
        get: () => value,
        set: (next) => {
          if (next && !value) {
            const where = (new Error().stack ?? "").split("\n").slice(2).filter((l) => /src\/(game|app)/.test(l)).slice(0, 2)
              .map((l) => l.trim().replace(/^at /, "").replace(/\(?https?:\/\/[^/]+\/src\//, "").replace(/\?[^:]*/, "")).join(" < ");
            const key = `${layerOf(group)}: ${where || "(PixiJS itself)"}`;
            causes.set(key, (causes.get(key) ?? 0) + 1);
          }
          value = next;
        },
      });
    }
  };
  // A busy server: some networks' flows come anew every second, and three map areas on screen change twice a second
  // (other players), each as the world tells it: one gets a new pipe or building, two a level or a finished building
  const keys = [...e.chunks.keys()].filter((k) => e.chunks.get(k).loaded);
  let n = 0;
  let m = 0;
  const busy = setInterval(() => {
    const networks = [...w.flows.values()];
    const fresh = [];
    for (let i = 0; i < Math.min(3, networks.length); i++) {
      const network = networks[(m++) % networks.length];
      fresh.push({ ...network, flows: network.flows.map((f) => ({ ...f, delivered: f.delivered * (0.98 + 0.04 * Math.random()) })) });
    }
    w.setFlows(fresh);
  }, 1000);
  const others = setInterval(() => {
    for (let i = 0; i < 3; i++) {
      const key = keys[n++ % keys.length];
      w.emit([key], false, false, [], i === 0 ? [key] : []);
    }
  }, 500);
  const watcher = trace ? setInterval(watch, 200) : 0;
  if (trace) watch();
  // And inside a flow rebuild: the time of its parts
  const parts = {};
  if (trace) {
    for (const name of ["waysOf", "flowPoints", "laneKinds", "pipeLook"]) {
      const original = e[name];
      if (typeof original !== "function") continue;
      let depth = 0;
      e[name] = function (...args) {
        if (depth++ > 0) { try { return original.apply(this, args); } finally { depth--; } }
        const t = performance.now();
        try { return original.apply(this, args); } finally { depth--; const part = (parts[name] ??= { calls: 0, ms: 0 }); part.calls++; part.ms += performance.now() - t; }
      };
    }
  }
  const t0 = performance.now();
  await new Promise((r) => setTimeout(r, 8000));
  clearInterval(busy);
  clearInterval(others);
  clearInterval(watcher);
  const seconds = (performance.now() - t0) / 1000;
  const main = { ...stats };
  frames.sort((a, b) => a - b);
  main.p95 = frames[Math.floor(frames.length * 0.95)] ?? 0;
  main.max = frames[frames.length - 1] ?? 0;
  // The dots alone, the map still: the time a frame moves them, and draws them (with them and without), per thousand
  // moving dots
  const dots = e.flowDots.filter((t) => t.length > 0).length;
  let animateMs = 0;
  let animateFrames = 0;
  const animate = e.animate;
  e.animate = function (...args) { const t = performance.now(); animate.apply(this, args); animateMs += performance.now() - t; animateFrames++; };
  const drawFor = async (visible) => {
    e.flowLayer.visible = visible;
    const before = { frames: stats.frames, ms: stats.renderMs };
    await new Promise((r) => setTimeout(r, 3000));
    return (stats.renderMs - before.ms) / Math.max(1, stats.frames - before.frames);
  };
  const withDots = await drawFor(true);
  const animatePerFrame = animateMs / Math.max(1, animateFrames);
  const withoutDots = await drawFor(false);
  e.flowLayer.visible = true;
  main.dotsDrawMsPer1000 = ((withDots - withoutDots) * 1000) / Math.max(1, dots);
  main.dotsMoveMsPer1000 = (animatePerFrame * 1000) / Math.max(1, dots);
  let sprites = 0;
  const count = (c) => { sprites++; for (const k of c.children ?? []) count(k); };
  count(e.app.stage);
  return {
    seconds: +seconds.toFixed(1),
    objects: sprites,
    fps: +(main.frames / seconds).toFixed(1),
    renderMsPerFrame: +(main.renderMs / Math.max(1, main.frames)).toFixed(2),
    rebuildsPerSecond: +(main.rebuilds / seconds).toFixed(1),
    rebuildMsPerSecond: +(main.rebuildMs / seconds).toFixed(1),
    flowRebuilds: main.flows,
    flowRebuildMs: +(main.flowsMs / Math.max(1, main.flows)).toFixed(2),
    dots,
    dotsDrawMsPer1000: +main.dotsDrawMsPer1000.toFixed(2),
    dotsMoveMsPer1000: +main.dotsMoveMsPer1000.toFixed(2),
    frameMsP95: +main.p95.toFixed(1),
    frameMsMax: +main.max.toFixed(1),
    ...(trace
      ? {
          inFlowRebuilds: Object.fromEntries(Object.entries(parts).map(([k, v]) => [k, `${v.calls} calls, ${v.ms.toFixed(0)} ms`])),
          rebuiltBy: [...causes].sort((a, b) => b[1] - a[1]).slice(0, 12),
        }
      : {}),
  };
}, trace);
console.log(JSON.stringify(result));
await p.browser.close();

// How the map draws on a big estate (the owner, 2026-10-31: "now the bases are becoming a bit bigger, the game runs a
// bit slow in the UI"; his Firefox profile: 38% of the time PixiJS rebuilt its draw list, as smoke puffs came and went
// every frame, and 12% rebuilding every flow dot when any network changed): the estate of big-estate.mjs, its flows
// changing once a second as on a busy server, and for 8 s: how often PixiJS rebuilds its draw list and what that costs,
// the time a frame's drawing takes, and a flow rebuild's time.   node ui-speed.mjs <outdir> [spacing]
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/ui-speed", spacing = "3"] = process.argv.slice(2);
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
const result = await p.page.evaluate(async () => {
  const e = tp.gameClient.engine;
  const system = e.app.renderer.renderGroup;
  const stats = { rebuilds: 0, rebuildMs: 0, frames: 0, renderMs: 0, flows: 0, flowsMs: 0 };
  const build = system._buildInstructions.bind(system);
  system._buildInstructions = (...args) => { const t = performance.now(); build(...args); stats.rebuilds++; stats.rebuildMs += performance.now() - t; };
  const renderer = e.app.renderer;
  const render = renderer.render.bind(renderer);
  renderer.render = (...args) => { const t = performance.now(); render(...args); stats.frames++; stats.renderMs += performance.now() - t; };
  const flows = e.buildFlows.bind(e);
  e.buildFlows = () => { const t = performance.now(); flows(); stats.flows++; stats.flowsMs += performance.now() - t; };
  const busy = setInterval(() => { e.flowsDirty = true; }, 1000);
  const t0 = performance.now();
  await new Promise((r) => setTimeout(r, 8000));
  clearInterval(busy);
  const seconds = (performance.now() - t0) / 1000;
  let sprites = 0;
  const count = (c) => { sprites++; for (const k of c.children ?? []) count(k); };
  count(e.app.stage);
  return {
    seconds: +seconds.toFixed(1),
    objects: sprites,
    fps: +(stats.frames / seconds).toFixed(1),
    renderMsPerFrame: +(stats.renderMs / Math.max(1, stats.frames)).toFixed(2),
    rebuildsPerSecond: +(stats.rebuilds / seconds).toFixed(1),
    rebuildMsPerSecond: +(stats.rebuildMs / seconds).toFixed(1),
    flowRebuilds: stats.flows,
    flowRebuildMs: +(stats.flowsMs / Math.max(1, stats.flows)).toFixed(2),
    dots: e.flowDots.length,
  };
});
console.log(JSON.stringify(result));
await p.browser.close();

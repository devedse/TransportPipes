// How the map draws on a big estate (the owner, 2026-10-31: "now the bases are becoming a bit bigger, the game runs a
// bit slow in the UI"; his Firefox profile: 38% of the time PixiJS rebuilt its draw list, as smoke puffs came and went
// every frame, and 12% rebuilding every flow dot when any network changed): the estate of big-estate.mjs, its flows
// changing once a second as on a busy server, and for 8 s: how often PixiJS rebuilds its draw list and what that costs,
// the time a frame's drawing takes, and a flow rebuild's time.   node ui-speed.mjs <outdir> [spacing]
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/ui-profile", spacing = "3"] = process.argv.slice(2);
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

// A CPU profile of 6 s as on a busy server (flows changing once a second, three areas twice a second): self time by function
await p.page.evaluate(() => {
  const e = tp.gameClient.engine;
  const keys = [...e.chunks.keys()].filter((k) => e.chunks.get(k).loaded);
  let n = 0;
  window.__busy = [setInterval(() => { e.flowsDirty = true; }, 1000), setInterval(() => { for (let i = 0; i < 3; i++) e.dirtyChunks.add(keys[(n++) % keys.length]); }, 500)];
});
const cdp = await p.page.context().newCDPSession(p.page);
await cdp.send("Profiler.enable");
await cdp.send("Profiler.setSamplingInterval", { interval: 500 });
await cdp.send("Profiler.start");
await p.page.waitForTimeout(6000);
const { profile } = await cdp.send("Profiler.stop");
const byId = new Map(profile.nodes.map((t) => [t.id, t]));
const self = new Map();
const total = profile.samples.length;
for (const id of profile.samples) {
  const f = byId.get(id).callFrame;
  const key = `${f.functionName || "(anon)"} ${f.url.split("/").pop()}:${f.lineNumber}`;
  self.set(key, (self.get(key) ?? 0) + 1);
}
const parent = new Map();
for (const node of profile.nodes) for (const c of node.children ?? []) parent.set(c, node.id);
const incl = new Map();
for (const id of profile.samples) {
  const seen = new Set();
  for (let at = id; at !== undefined; at = parent.get(at)) {
    const f = byId.get(at).callFrame;
    const key = `${f.functionName || "(anon)"} ${f.url.split("/").pop()}:${f.lineNumber}`;
    if (seen.has(key)) continue;
    seen.add(key);
    incl.set(key, (incl.get(key) ?? 0) + 1);
  }
}
const top = (m, n) => [...m.entries()].sort((a, b) => b[1] - a[1]).slice(0, n).map(([k, v]) => `${(100 * v / total).toFixed(1)}% ${k}`);
console.log("SELF\n" + top(self, 25).join("\n"));
console.log("INCLUSIVE\n" + top(incl, 45).join("\n"));
await p.browser.close();

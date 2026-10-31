// A grown estate (the owner, after run 4: "even moderately sized networks took 10 seconds to build a new building in
// them"): a pipe snakes across a whole estate with a building every few tiles, then a building placed into that
// network is timed, from the command to the scaffold on the map: node big-estate.mjs <outdir> [spacing] [budget ms]
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/big-estate", spacing = "3", budget = "1500"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const p = await start(out);
await newPlayer(p.page, "big");
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

// The network: the one most of the estate's buildings are in
const network = await p.page.evaluate(() => {
  const w = tp.gameClient.world;
  const me = tp.appStore.getState().company;
  const counts = new Map();
  for (const b of w.buildings.values()) if (b.owner === me.id && b.network != null) counts.set(b.network, (counts.get(b.network) ?? 0) + 1);
  const [id, count] = [...counts.entries()].sort((a, b) => b[1] - a[1])[0];
  const pipes = [...w.conductors.values()].filter((t) => t.network === id).length;
  return { id, buildings: count, pipes };
});
console.log("network", JSON.stringify(network));
// (a mountain on the plot can cut the snake: 40 or more in one network is a big one)
if (network.buildings < 40) fail(`a grown network (${network.buildings} buildings)`);
await p.shot("1-grown");

// A slower laptop (THROTTLE=4: Chrome runs the page four times slower)
if (process.env.THROTTLE) {
  const cdp = await p.page.context().newCDPSession(p.page);
  await cdp.send("Emulation.setCPUThrottlingRate", { rate: Number(process.env.THROTTLE) });
  console.log("CPU throttled", process.env.THROTTLE, "times");
}
// With a building of the network open in the panel, as a player has it
await p.page.evaluate((network) => {
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.network === network && t.type === "smallFactory");
  if (b) tp.gameClient.select(b.id);
}, network.id);
await p.page.waitForTimeout(3000);

// A building placed beside its pipe, three times, the way the build tool does it
const times = [];
for (let i = 0; i < 3; i++) {
  const took = await p.page.evaluate(async () => {
    const w = tp.gameClient.world;
    const s = () => tp.appStore.getState();
    const me = s().company;
    // A build rebuilds the network under a new id: the biggest one again
    const counts = new Map();
    for (const b of w.buildings.values()) if (b.owner === me.id && b.network != null) counts.set(b.network, (counts.get(b.network) ?? 0) + 1);
    const network = [...counts.entries()].sort((a, b) => b[1] - a[1])[0][0];
    let spot = null;
    for (const type of ["solarPanel", "storage", "lab", "windMoily"]) {
      const definition = s().content.building(type);
      for (const pipe of w.conductors.values()) {
        if (pipe.network !== network || spot) continue;
        for (const [dx, dy] of [[0, 1], [0, -1], [1, 0], [-1, 0]]) {
          if (!spot && tp.rules.canPlace(s().content, w, me, s().era, definition, pipe.x + dx, pipe.y + dy).ok) spot = { type, x: pipe.x + dx, y: pipe.y + dy };
        }
      }
    }
    if (spot === null) {
      const reasons = new Map();
      const definition = s().content.building("solarPanel");
      for (const pipe of w.conductors.values()) {
        if (pipe.network !== network) continue;
        const why = tp.rules.canPlace(s().content, w, me, s().era, definition, pipe.x, pipe.y + 1).reason ?? "ok";
        reasons.set(why, (reasons.get(why) ?? 0) + 1);
      }
      return { error: "no spot", reasons: Object.fromEntries(reasons) };
    }
    const t0 = performance.now();
    await tp.gameClient.placeBuilding(spot.type, spot.x, spot.y);
    const command = performance.now() - t0;
    // Until the map has it
    while (![...w.buildings.values()].some((b) => b.x === spot.x && b.y === spot.y) && performance.now() - t0 < 30000) await new Promise((r) => setTimeout(r, 20));
    const onMap = performance.now() - t0;
    // And until the open panel and the flows came back (the page answers again)
    await new Promise((r) => setTimeout(r, 0));
    return { ...spot, command: Math.round(command), onMap: Math.round(onMap) };
  });
  console.log("build", i + 1, JSON.stringify(took));
  times.push(took.onMap ?? 99999);
  await p.page.waitForTimeout(3000);
}
await p.shot("2-built");
const worst = Math.max(...times);
if (worst > Number(budget)) fail(`a build shows within ${budget} ms (worst ${worst} ms)`);
if (p.errors.length > 0) fail(p.errors.join(" | "));
await p.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

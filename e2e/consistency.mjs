// After every kind of action, what the playing tab shows must match a freshly loaded tab: the network id of every pipe
// and building, which buildings run, and the idle bubbles. Catches updates the client misses (they used to need a
// reload). node consistency.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/consistency"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
const base = process.env.BASE_URL ?? "http://localhost:5173";
await newPlayer(page, "c");
for (const c of ["gold", "gold", "level", "level"]) await cheat(c);
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));

// An estate: two moilies and a solar panel piped to the trading post and out through a gate, and a storage
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState(); const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const ok = (type, x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building(type), x, y).ok;
  const near = (type, cx, cy) => { for (let r = 1; r < 9; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) if (ok(type, cx + dx, cy + dy)) return { x: cx + dx, y: cy + dy }; return null; };
  const built = {};
  for (const [name, type, dx, dy] of [["moilyA", "windMoily", 4, -3], ["moilyB", "windMoily", 6, 2], ["solar", "solarPanel", -3, 4], ["storage", "storage", 3, 4]]) {
    const at = near(type, post.x + dx, post.y + dy);
    await c.placeBuilding(type, at.x, at.y);
    built[name] = at;
  }
  await c.cheat("finish");
  const lay = async (b, to) => {
    for (const [fx, fy] of [[b.x - 1, b.y], [b.x + 1, b.y], [b.x, b.y - 1], [b.x, b.y + 1]]) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, to, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) return route;
    }
    return null;
  };
  const routes = {};
  for (const name of Object.keys(built)) routes[name] = await lay(built[name], { x: post.x - 1, y: post.y });
  const gate = [...w.conductors.values()].filter((t) => t.kind === "gate" && t.owner === co().id)
    .sort((a, b) => Math.abs(a.x - post.x) + Math.abs(a.y - post.y) - (Math.abs(b.x - post.x) + Math.abs(b.y - post.y)))[0];
  const inEstate = (x, y) => x >= co().estateX && y >= co().estateY && x < co().estateX + 32 && y < co().estateY + 32;
  const inside = [[1, 0], [-1, 0], [0, 1], [0, -1]].map(([dx, dy]) => ({ x: gate.x + dx, y: gate.y + dy })).find((t) => inEstate(t.x, t.y));
  routes.gate = await lay(built.moilyA, inside);
  tp.gameClient.engine.lookAt(post.x + 2, post.y, 0.4);
  window.__c = { post: { x: post.x, y: post.y }, built, routes, gateSide: gate.side };
  return { built, routes: Object.fromEntries(Object.entries(routes).map(([k, v]) => [k, v?.length ?? null])), gate: gate.side };
});
console.log("setup", JSON.stringify(setup));

// What a tab shows: network ids of the own pipes and buildings, which buildings run, the idle bubbles
const snapshot = (p) => p.evaluate(() => {
  const engine = tp.gameClient.engine; const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  const pipes = {}; const buildings = {}; const idle = [];
  for (const t of w.conductors.values()) if (t.owner === co.id) pipes[`${t.x},${t.y}`] = t.network;
  for (const b of w.buildings.values()) if (b.owner === co.id && b.constructionEndsAt === null) {
    const rate = w.rate(b);
    buildings[`${b.type}@${b.x},${b.y}`] = `${b.network}:${rate !== null && rate > 0.005 ? "runs" : "stops"}`;
  }
  for (const chunk of engine.chunks.values()) for (const view of chunk.objects) {
    if (view.building?.owner === co.id && view.idle?.visible) idle.push(`${view.building.type}@${view.building.x},${view.building.y}`);
  }
  return { pipes, buildings, idle: idle.sort() };
});
// A second browser context with the same login (the player's cookie): a fresh tab loads the true state
const context = await browser.newContext({ storageState: await page.context().storageState(), viewport: { width: 1600, height: 1000 } });
const compare = async (label) => {
  await page.waitForTimeout(3500);
  const other = await context.newPage();
  await other.goto(base + "/");
  await other.waitForSelector("body[data-ready='1']", { timeout: 60000 });
  await other.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x + 2, y, 0.4), await page.evaluate(() => window.__c.post));
  await other.waitForTimeout(4000);
  const truth = await snapshot(other);
  await other.close();
  // The playing tab at the same moment (a move or a construction may have finished meanwhile)
  const live = await snapshot(page);
  const diffs = [];
  for (const [key, id] of Object.entries(truth.pipes)) if (live.pipes[key] !== id) diffs.push(`pipe ${key}: shows ${live.pipes[key]}, is ${id}`);
  for (const key of Object.keys(live.pipes)) if (!(key in truth.pipes)) diffs.push(`pipe ${key}: shown but gone`);
  for (const [key, v] of Object.entries(truth.buildings)) if (live.buildings[key] !== v) diffs.push(`${key}: shows ${live.buildings[key]}, is ${v}`);
  for (const key of Object.keys(live.buildings)) if (!(key in truth.buildings)) diffs.push(`${key}: shown but gone`);
  if (live.idle.join() !== truth.idle.join()) diffs.push(`idle bubbles: shows [${live.idle}], is [${truth.idle}]`);
  console.log(`${diffs.length === 0 ? "ok  " : "FAIL"} ${label}${diffs.length ? "\n       " + diffs.slice(0, 6).join("\n       ") + (diffs.length > 6 ? `\n       … ${diffs.length - 6} more` : "") : ""}`);
  return diffs.length;
};

const act = (fn, arg) => page.evaluate(fn, arg);
let failures = 0;
failures += await compare("the estate as built");
failures += (await act(async () => {
  // A pipe stub next to the network
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  for (const t of window.__c.routes.moilyB) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const x = t.x + dx, y = t.y + dy;
    if (tp.rules.pipeCost(w, co, x, y, s.content) === 1 && w.buildingAt(x, y) === undefined) { window.__c.stub = { x, y }; return c.placePipes([x, y], 1); }
  }
}), await compare("a pipe added"));
failures += (await act(() => tp.gameClient.connection.removePipes([window.__c.stub.x, window.__c.stub.y])), await compare("that pipe removed"));
failures += (await act(() => { const r = window.__c.routes.moilyB; const t = r[Math.floor(r.length / 2)]; window.__c.cut = t; return tp.gameClient.connection.removePipes([t.x, t.y]); }), await compare("a pipe cut out of a run (the network splits)"));
failures += (await act(() => tp.gameClient.connection.placePipes([window.__c.cut.x, window.__c.cut.y], 1)), await compare("the cut mended (the networks merge)"));
// (Gates have no policy since v1.2: the Yard trades through every gate)
failures += (await act(() => { const w = tp.gameClient.world; const b = w.buildingAt(window.__c.built.moilyA.x, window.__c.built.moilyA.y); return tp.gameClient.connection.upgrade(b.id, 1); }), await compare("a moily upgraded"));
failures += (await act(async () => {
  // A second solar panel touching the moily's pipe
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  for (const t of window.__c.routes.moilyA) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    if (tp.rules.canPlace(s.content, w, co, s.era, s.content.building("solarPanel"), t.x + dx, t.y + dy).ok) {
      window.__c.solar2 = { x: t.x + dx, y: t.y + dy };
      await c.placeBuilding("solarPanel", t.x + dx, t.y + dy);
      return c.cheat("finish");
    }
  }
}), await compare("a building placed beside a pipe"));
failures += (await act(() => { const b = tp.gameClient.world.buildingAt(window.__c.solar2.x, window.__c.solar2.y); return tp.gameClient.connection.demolish(b.id); }), await compare("that building demolished"));
failures += (await act(async () => {
  // The storage moved one tile away from its pipe (it drops off the network)
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState();
  const b = w.buildingAt(window.__c.built.storage.x, window.__c.built.storage.y);
  for (const [dx, dy] of [[2, 0], [0, 2], [-2, 0], [0, -2]]) if ((await c.move(b.id, b.x + dx, b.y + dy)).success) return `moved by ${dx},${dy}`;
}), await compare("a building moved"));
await shot("1-end");
console.log(failures === 0 ? "all consistent" : `${failures} differences`);
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

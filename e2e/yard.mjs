// Two players in one station trade through their Yard (v1.2 §7): A's Wind Moily and B's Iron Mine, each piped to their
// Yard gate with the Yard panel's "Plan the pipe" button. B's mine should get A's energy at the player price, and the
// Yard panel should show it: node yard.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/yard"] = process.argv.slice(2);

const a = await start(out);
await newPlayer(a.page, "ya");
await a.cheat("gold");
const b = await start(out);
await newPlayer(b.page, "yb");
await b.cheat("gold");
await b.cheat("level");

/** Builds a building near the trading post (or on a deposit), pipes it to the post and plans and lays the pipe to the Yard gate */
async function settle(player, type, deposit) {
  return player.page.evaluate(async ({ type, deposit }) => {
    const c = tp.gameClient.connection;
    const w = tp.gameClient.world;
    const s = () => tp.appStore.getState();
    const co = () => s().company;
    const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
    const definition = s().content.building(type);
    const ok = (x, y) => tp.rules.canPlace(s().content, w, co(), s().era, definition, x, y).ok;
    let spot = null;
    for (let r = 2; r < 12 && !spot; r++)
      for (let dy = -r; dy <= r && !spot; dy++)
        for (let dx = -r; dx <= r && !spot; dx++)
          if (ok(post.x + dx, post.y + dy) && (!deposit || w.tile(post.x + dx, post.y + dy)?.deposit === deposit)) spot = { x: post.x + dx, y: post.y + dy };
    if (!spot) return `no spot for ${type}`;
    const placed = await c.placeBuilding(type, spot.x, spot.y);
    if (!placed.success) return placed.error;
    await c.cheat("finish");
    const border = (b) => [[b.x, b.y - 1], [b.x, b.y + 1], [b.x - 1, b.y], [b.x + 1, b.y]];
    let toPost = null;
    for (const [fx, fy] of border(spot)) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) { toPost = route.length; break; }
    }
    await new Promise((r) => setTimeout(r, 1500));
    // The Yard panel's button: plan the pipe to the gate, then build it
    await tp.gameClient.openYard();
    await tp.gameClient.planYardPipe();
    for (let i = 0; i < 30 && s().plannedRoute === null; i++) await new Promise((r) => setTimeout(r, 300));
    const planned = s().plannedRoute?.length ?? null;
    if (planned !== null) await tp.gameClient.buildPlannedRoute();
    await new Promise((r) => setTimeout(r, 1500));
    return JSON.stringify({ type, spot, toPost, planned, slot: co().slot });
  }, { type, deposit });
}

console.log("A", await settle(a, "windMoily"));
console.log("B", await settle(b, "ironMine", "ironOre"));
// The Yards' rounds (a minute each on the server): one matches, the next delivers
await a.cheat("yard");
await new Promise((r) => setTimeout(r, 1500));

for (const [name, player] of [["A", a], ["B", b]]) {
  const yard = await player.page.evaluate(async () => {
    await tp.gameClient.openYard();
    await new Promise((r) => setTimeout(r, 800));
    const y = tp.appStore.getState().yard;
    return y && JSON.stringify({ station: y.station, connected: y.connected, line: y.line, yours: y.yours, residents: y.residents.map((t) => [t.name, t.connected]) });
  });
  console.log(name, yard);
  const gate = await player.page.evaluate(() => { const y = tp.appStore.getState().yard; return y && { x: y.gateX, y: y.gateY }; });
  if (gate) await player.page.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x, y, 0.7), gate);
  await player.page.waitForTimeout(1500);
  await player.shot(`${name}-yard`);
}
for (const player of [a, b]) {
  console.log(player.errors.length ? player.errors.join("\n") : "no errors");
  await player.browser.close();
}

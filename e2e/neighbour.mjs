// Loady's neighbour step the way a player does it: a moily piped to the trading post and to the own gate, then
// Neighbours → Connect to a works next door. The step should be done a moment later: node neighbour.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/neighbour"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "n");
await cheat("gold");
await cheat("level");
for (let i = 0; i < 6; i++) await cheat("tutorial");
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstNeighbour");
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const ok = (x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building("windMoily"), x, y).ok;
  let spot = null;
  for (let r = 2; r < 8 && !spot; r++) for (let dy = -r; dy <= r && !spot; dy++) for (let dx = -r; dx <= r && !spot; dx++) if (ok(post.x + dx, post.y + dy)) spot = { x: post.x + dx, y: post.y + dy };
  await c.placeBuilding("windMoily", spot.x, spot.y);
  await c.cheat("finish");
  const border = (b, size = 1) => { const r = []; for (let i = 0; i < size; i++) r.push([b.x + i, b.y - 1], [b.x + i, b.y + size], [b.x - 1, b.y + i], [b.x + size, b.y + i]); return r; };
  const lay = async (from, to) => {
    for (const [fx, fy] of from) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, to, s().content);
      if (route) { const r = await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1); if (r.success) return route.length; }
    }
    return null;
  };
  const toPost = await lay(border(spot), { x: post.x - 1, y: post.y });
  // Neighbours → Connect, like the button
  await tp.gameClient.openNeighbours();
  await new Promise((r) => setTimeout(r, 800));
  // The nearest works (the list is nearest first), like the neighbour card suggests
  const works = s().neighbours.find((t) => t.isNpc && /Ironworks|Brickworks/.test(t.name) && !t.connected);
  if (!works) return "no ironworks";
  await tp.gameClient.planConnection(works.id);
  for (let i = 0; i < 30 && s().plannedRoute === null; i++) await new Promise((r) => setTimeout(r, 300));
  const route = s().plannedRoute;
  if (!route) return "no route to " + works.name;
  // Inside our gate: the tile on the other side of the gate the route starts next to
  const start = route[0];
  let inside = null;
  for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const gate = w.conductorAt(start.x + dx, start.y + dy);
    if (gate?.kind === "gate" && gate.owner === co().id) inside = { x: start.x + 2 * dx, y: start.y + 2 * dy };
  }
  await tp.gameClient.buildPlannedRoute();
  const toGate = inside ? await lay(border(spot), inside) : null;
  return JSON.stringify({ works: works.name, toPost, corridor: route.length, toGate });
}));
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id !== "firstNeighbour", null, { timeout: 30000 })
  .then(() => console.log("Loady's neighbour step done"), () => console.log("still on the neighbour step"));
console.log(await page.evaluate(() => JSON.stringify(tp.appStore.getState().company?.tutorial?.tip ?? null)));
await page.evaluate(async () => { await tp.gameClient.openNeighbours(); });
await page.waitForTimeout(1500);
await shot("1-neighbours");
await page.evaluate(async () => { await tp.gameClient.openTradeSettings(); });
await page.waitForTimeout(1500);
await page.locator(".neighbours-panel").screenshot({ path: `${out}/2-trade-settings.png` });
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

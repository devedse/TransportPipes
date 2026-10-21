// Closing and opening a gate: the buildings behind it keep running (no idle bubble) without a reload.
// node gate.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/gate"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "g");
await cheat("gold");
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState(); const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  // The own gate nearest to the trading post, and the tile inside it
  const inEstate = (x, y) => x >= co().estateX && y >= co().estateY && x < co().estateX + 32 && y < co().estateY + 32;
  const gate = [...w.conductors.values()].filter((t) => t.kind === "gate" && t.owner === co().id)
    .sort((a, b) => Math.abs(a.x - post.x) + Math.abs(a.y - post.y) - (Math.abs(b.x - post.x) + Math.abs(b.y - post.y)))[0];
  const inside = [[1, 0], [-1, 0], [0, 1], [0, -1]].map(([dx, dy]) => ({ x: gate.x + dx, y: gate.y + dy })).find((t) => inEstate(t.x, t.y));
  const ok = (x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building("windMoily"), x, y).ok;
  let spot = null;
  for (let r = 2; r < 8 && !spot; r++) for (let dy = -r; dy <= r && !spot; dy++) for (let dx = -r; dx <= r && !spot; dx++) if (ok(post.x + dx, post.y + dy)) spot = { x: post.x + dx, y: post.y + dy };
  await c.placeBuilding("windMoily", spot.x, spot.y);
  await c.cheat("finish");
  const lay = async (to) => {
    for (const [fx, fy] of [[spot.x - 1, spot.y], [spot.x + 1, spot.y], [spot.x, spot.y - 1], [spot.x, spot.y + 1]]) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, to, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) return route.length;
    }
    return null;
  };
  const toPost = await lay({ x: post.x - 1, y: post.y });
  const toGate = await lay(inside);
  tp.gameClient.engine.lookAt(spot.x, spot.y, 0.7);
  return { gate: { x: gate.x, y: gate.y, side: gate.side }, toPost, toGate };
});
console.log("setup", JSON.stringify(setup));
// What the map shows: an idle bubble over a finished building of the player
const look = (label) => page.evaluate((label) => {
  const engine = tp.gameClient.engine; const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  const idle = [];
  for (const chunk of engine.chunks.values()) for (const view of chunk.objects) {
    if (view.building?.owner === co.id && view.idle !== null && view.idle.visible) idle.push(view.building.type);
  }
  const moily = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  return `${label}: idle bubbles [${idle.join(", ")}], moily network ${moily.network} rate ${w.rate(moily)}`;
}, label);
await page.waitForTimeout(4000);
const results = [await look("running")];
for (const policy of ["closed", "open", "closed", "open"]) {
  await page.evaluate(([side, policy]) => tp.gameClient.setGatePolicy(side, policy), [setup.gate.side, policy]);
  await page.waitForTimeout(4000);
  results.push(await look(`gate ${policy}`));
}
await shot("1-after-gate");
console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

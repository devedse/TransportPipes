// The network view and trade settings on an estate that is short of energy and trades with a neighbour:
// node network-view.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/network-view"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "v");
for (const c of ["gold", "gold", "level"]) await cheat(c);
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const ids = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState(); const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const ok = (type, x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building(type), x, y).ok;
  const placed = [];
  const near = async (type, cx, cy) => {
    for (let r = 1; r < 24; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) === r && ok(type, cx + dx, cy + dy) && (await c.placeBuilding(type, cx + dx, cy + dy)).success) { placed.push({ type, x: cx + dx, y: cy + dy }); return; }
    }
  };
  // One moily for two mines and two factories: short of energy
  for (const [type, dx, dy] of [["windMoily", 4, -3], ["ironMine", 9, 1], ["ironMine", 9, 4], ["smallFactory", 6, 1], ["smallFactory", 6, 4]]) await near(type, post.x + dx, post.y + dy);
  await c.cheat("finish");
  for (const b of placed) {
    for (const [fx, fy] of [[b.x - 1, b.y], [b.x + 1, b.y], [b.x, b.y - 1], [b.x, b.y + 1]]) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) break;
    }
  }
  tp.gameClient.engine.lookAt(post.x + 4, post.y, 0.7);
  const pipe = [...w.conductors.values()].find((t) => t.owner === co().id && t.kind === "pipe");
  return { pipe: { x: pipe.x, y: pipe.y } };
});
await page.waitForTimeout(4000);
await page.evaluate(({ pipe }) => tp.gameClient.engineCallbacks.onClick(pipe, undefined), ids);
await page.waitForTimeout(2500);
await shot("1-network");
await page.locator(".network-panel").screenshot({ path: `${out}/2-network-panel.png` });
await page.evaluate(() => tp.gameClient.openTradeSettings());
await page.waitForTimeout(2000);
await page.locator(".neighbours-panel").screenshot({ path: `${out}/3-trade-settings.png` });
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

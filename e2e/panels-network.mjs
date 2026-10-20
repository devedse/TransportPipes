// The Trading Post's flows grouped per resource with its selling meter, and a building's network totals per company
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/panels-network"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "t");
for (const c of ["gold", "gold", "level"]) await cheat(c);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const ids = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState(); const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const ok = (type, x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building(type), x, y).ok;
  const placed = [];
  const near = async (type, cx, cy) => {
    for (let r = 1; r < 9; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) === r && ok(type, cx + dx, cy + dy) && (await c.placeBuilding(type, cx + dx, cy + dy)).success) { placed.push({ type, x: cx + dx, y: cy + dy }); return; }
    }
  };
  for (const [type, dx, dy] of [["windMoily", 4, -3], ["windMoily", 6, -3], ["windMoily", 8, -3], ["solarPanel", 4, 3], ["ironMine", 9, 1], ["ironMine", 9, 4], ["smallFactory", 6, 1]]) await near(type, post.x + dx, post.y + dy);
  await c.cheat("finish");
  for (const b of placed) {
    for (const [fx, fy] of [[b.x - 1, b.y], [b.x + 1, b.y], [b.x, b.y - 1], [b.x, b.y + 1]]) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) break;
    }
  }
  tp.gameClient.engine.lookAt(post.x + 3, post.y, 0.8);
  const moily = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "windMoily");
  return { post: post.id, moily: moily.id };
});
await page.waitForTimeout(4000);
await page.evaluate((id) => tp.gameClient.select(id), ids.post);
await page.waitForTimeout(2500);
await shot("1-trading-post");
// Open the first group
await page.click(".flow-groups summary");
await page.waitForTimeout(400);
await page.locator(".building-panel").screenshot({ path: `${out}/2-trading-post-open.png` });
await page.evaluate((id) => tp.gameClient.select(id), ids.moily);
await page.waitForTimeout(2500);
await page.locator(".building-panel").screenshot({ path: `${out}/3-moily-network.png` });
console.log(await page.evaluate(() => JSON.stringify(tp.appStore.getState().buildingNetwork?.owners.map((o) => [o.name, o.resources]))));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

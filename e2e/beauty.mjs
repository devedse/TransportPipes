// A working, decorated estate for screenshots: node beauty.mjs <outdir> [zoom]
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/beauty", zoom = "0.8"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "w");
for (const c of ["gold", "gold", "gold", "gold", "level", "level", "level", "level"]) await cheat(c);
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.waitForTimeout(800);
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const co = () => tp.appStore.getState().company;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, w, co(), tp.appStore.getState().era, s.content.building(t), x, y).ok;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const placed = [];
  const near = async (type, cx, cy, r0 = 1) => {
    for (let r = r0; r < 10; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;
      if (ok(type, cx + dx, cy + dy)) { const res = await c.placeBuilding(type, cx + dx, cy + dy); if (res.success) { placed.push({ type, x: cx + dx, y: cy + dy }); return; } }
    }
  };
  for (const [type, dx, dy] of [["windMoily", 5, -4], ["windMoily", 7, -4], ["windMoily", 9, -4], ["solarPanel", 4, 4], ["solarPanel", 5, 4], ["ironMine", 9, 1], ["smallFactory", 6, 1], ["storage", 3, 6], ["lab", -4, 4]]) {
    await near(type, post.x + dx, post.y + dy);
  }
  await c.cheat("finish"); await c.cheat("stock");
  // Pipe every building to the trading post
  for (const b of placed) {
    const def = s.content.building(b.type);
    const border = [];
    for (let i = 0; i < def.size; i++) border.push([b.x + i, b.y - 1], [b.x + i, b.y + def.size], [b.x - 1, b.y + i], [b.x + def.size, b.y + i]);
    let done = false;
    for (const [fx, fy] of border) {
      if (done) break;
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s.content);
      if (route) { const r = await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1); done = r.success; }
    }
  }
  // Decorations around the HQ
  const hq = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "hq");
  for (const [type, dx, dy] of [["tree", -2, -1], ["tree", -2, 1], ["flowerBed", -1, 3], ["lamp", 2, -1], ["fountain", -2, 3], ["tree", 3, 3]]) await near(type, hq.x + dx, hq.y + dy, 0);
  await c.cheat("finish");
  window.__look = () => tp.gameClient.engine.lookAt(post.x + 2, post.y, Number(window.__zoom));
  return placed.map((t) => t.type).join(",");
}));
await page.evaluate((z) => { window.__zoom = z; window.__look(); }, zoom);
await page.waitForTimeout(8000);
await page.evaluate(() => tp.gameClient.select(null));
await shot("1-estate");
await page.keyboard.press("f");
await page.evaluate(() => tp.gameClient.setTimeOfDay("golden"));
await page.waitForTimeout(2500);
await shot("2-golden");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

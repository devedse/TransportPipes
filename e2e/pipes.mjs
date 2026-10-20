// A small connected estate with pipes and flows: node pipes.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/pipes"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "q");
for (const c of ["gold", "gold", "level", "level", "level"]) await cheat(c);
await page.waitForTimeout(1000);
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, w, tp.appStore.getState().company, s.era, s.content.building(t), x, y).ok;
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  const placed = [];
  const near = async (type, cx, cy) => { for (let r = 1; r < 8; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) { if (ok(type, cx + dx, cy + dy)) { await c.placeBuilding(type, cx + dx, cy + dy); placed.push([type, cx + dx, cy + dy]); return { x: cx + dx, y: cy + dy }; } } };
  const items = [];
  for (const [type, dx, dy] of [["windMoily", 4, -3], ["windMoily", 6, -3], ["solarPanel", 4, 3], ["ironMine", 7, 2], ["smallFactory", 6, 0], ["storage", 3, 5]]) items.push(await near(type, post.x + dx, post.y + dy));
  await c.cheat("finish");
  const border = (b) => [[b.x + 1, b.y], [b.x - 1, b.y], [b.x, b.y + 1], [b.x, b.y - 1]];
  let laid = 0;
  for (const it of items.filter(Boolean)) {
    const target = [...w.buildings.values()].find((t) => t.x === it.x && t.y === it.y);
    for (const [fx, fy] of border(it)) {
      const route = tp.rules.routePipe(w, tp.appStore.getState().company, { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s.content);
      if (route) { const r = await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1); if (r.success) { laid += route.length; break; } }
    }
  }
  tp.gameClient.engine.lookAt(post.x + 3, post.y, 0.95);
  return `${placed.length} buildings, ${laid} pipe tiles`;
}));
await page.waitForTimeout(6000);
await shot("1-pipes");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

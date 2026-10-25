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
// Close up on the factory: its lanes (1.4 §9: a belt, cables) and the goods riding them
await page.evaluate(() => {
  const me = tp.appStore.getState().company.id;
  const w = tp.gameClient.world;
  const piped = (b) => [[b.x - 1, b.y], [b.x + b.size, b.y], [b.x, b.y - 1], [b.x, b.y + b.size]].some(([x, y]) => w.conductorAt(x, y) !== undefined);
  const mine = [...w.buildings.values()].filter((t) => t.owner === me && piped(t));
  const building = mine.find((t) => t.type === "smallFactory") ?? mine.find((t) => t.type === "ironMine") ?? mine.find((t) => t.type !== "tradingPost");
  if (building) tp.gameClient.engine.lookAt(building.x, building.y, 2.2);
});
await page.waitForTimeout(3000);
await shot("2-lanes");
// Look inside (1.4 §9): the pointer on a pipe tile that carries goods cuts it open, and the tip says what runs in which lane
const busy = await page.evaluate(() => {
  const w = tp.gameClient.world;
  const counts = new Map();
  for (const network of w.flows.values())
    for (const flow of network.flows)
      if (flow.delivered > 0)
        for (let i = 2; i + 3 < flow.path.length; i += 2) {
          const key = `${flow.path[i]},${flow.path[i + 1]}`;
          if (w.conductorAt(flow.path[i], flow.path[i + 1])?.kind === "pipe" && !w.buildingAt(flow.path[i], flow.path[i + 1])) counts.set(key, (counts.get(key) ?? 0) + 1);
        }
  const [key] = [...counts.entries()].sort((a, b) => b[1] - a[1])[0] ?? [];
  if (!key) return null;
  const [x, y] = key.split(",").map(Number);
  tp.gameClient.engine.lookAt(x, y, 1.6);
  return { x, y };
});
await page.waitForTimeout(2500);
let inside = "";
if (busy) {
  const at = await page.evaluate(({ x, y }) => tp.gameClient.engine.tileToScreen(x, y), busy);
  await page.mouse.move(at.x, at.y);
  await page.waitForTimeout(1200);
  inside = await page.locator(".hover-tip").innerText().catch(() => "");
}
console.log("look inside:", inside);
await shot("3-look-inside");
await page.keyboard.down("Alt");
await page.waitForTimeout(800);
await shot("4-alt-every-pipe");
await page.keyboard.up("Alt");
// Zoomed out the connectors fade, so the map stays calm
await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + 16, 0.3);
});
await page.waitForTimeout(2500);
await shot("5-zoomed-out");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

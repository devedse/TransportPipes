// The generated art in the game: node art-check.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/art"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "a");
for (const c of ["gold", "gold", "level", "level"]) await cheat(c);
await page.waitForTimeout(1000);
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const s = tp.appStore.getState();
  const co = s.company;
  const ok = (type, x, y) => tp.rules.canPlace(s.content, tp.gameClient.world, tp.appStore.getState().company, s.era, s.content.building(type), x, y).ok;
  const placed = [];
  const tryPlace = async (type, near, radius = 6) => {
    for (let r = 0; r <= radius; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) {
      const x = near.x + dx, y = near.y + dy;
      if (ok(type, x, y)) { const res = await c.placeBuilding(type, x, y); if (res.success) { placed.push(type); return { x, y }; } }
    }
    return null;
  };
  const hq = { x: co.estateX + 15, y: co.estateY + 15 };
  for (let i = 0; i < 3; i++) await tryPlace("windMoily", { x: hq.x - 4 + i * 2, y: hq.y - 4 });
  for (let i = 0; i < 2; i++) await tryPlace("solarPanel", { x: hq.x - 4 + i, y: hq.y + 4 });
  await tryPlace("smallFactory", { x: hq.x + 5, y: hq.y - 3 });
  await tryPlace("storage", { x: hq.x + 6, y: hq.y + 2 });
  await c.cheat("finish");
  tp.gameClient.engine.lookAt(hq.x + 1, hq.y + 1, 0.75);
  return placed.join(",");
}));
await page.waitForTimeout(4000);
await shot("1-estate");
await page.evaluate(() => { const co = tp.appStore.getState().company; tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + 16, 0.35); });
await page.waitForTimeout(4000);
await shot("2-estate-far");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

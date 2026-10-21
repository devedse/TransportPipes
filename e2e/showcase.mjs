// Many buildings on one estate: node showcase.mjs <outdir> <types,...>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/showcase", list = ""] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "s");
for (const c of ["gold", "gold", "gold", "gold", "gold", "gold", "gold", "gold", "level", "level", "level", "level", "level", "level", "level", "level", "alltech", "deep"]) await cheat(c);
while (await page.evaluate(() => tp.appStore.getState().era) < 5) { await cheat("era"); await page.waitForTimeout(200); }
for (let i = 0; i < 20; i++) await cheat("gold");
await page.waitForTimeout(1500);
// Buy all parcels, stock materials
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, tp.gameClient.world, tp.appStore.getState().company, tp.appStore.getState().era, s.content.building(t), x, y).ok;
  for (let y = co.estateY + 1; y < co.estateY + 31; y++) for (let x = co.estateX + 1; x < co.estateX + 31; x++) if (ok("storage", x, y)) { await c.placeBuilding("storage", x, y); await c.cheat("finish"); await c.cheat("stock"); return; }
});
await page.waitForTimeout(1500);
const types = list.split(",").filter(Boolean);
console.log(await page.evaluate(async (types) => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = tp.appStore.getState().company;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, tp.gameClient.world, tp.appStore.getState().company, tp.appStore.getState().era, s.content.building(t), x, y).ok;
  const placed = [];
  for (const type of types) {
    let done = false;
    // Spread out: scan from the middle outwards, a gap of one tile
    for (let r = 2; r < 16 && !done; r++) for (let dy = -r; dy <= r && !done; dy++) for (let dx = -r; dx <= r && !done; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;
      const x = co.estateX + 15 + dx, y = co.estateY + 15 + dy;
      if (ok(type, x, y)) { const res = await c.placeBuilding(type, x, y); if (res.success) { placed.push(type); done = true; } }
    }
    if (!done) placed.push(`!${type}`);
    await c.cheat("stock");
  }
  await c.cheat("finish");
  tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + 16, 0.42);
  return placed.join(" ");
}, types));
await page.waitForTimeout(5000);
await page.keyboard.press("Escape");
await shot("1-estate");
await page.evaluate(() => { const co = tp.appStore.getState().company; tp.gameClient.engine.lookAt(co.estateX + 12, co.estateY + 12, 0.8); });
await page.waitForTimeout(4000);
await shot("2-close");
await page.evaluate(() => { const co = tp.appStore.getState().company; tp.gameClient.engine.lookAt(co.estateX + 20, co.estateY + 20, 0.8); });
await page.waitForTimeout(4000);
await shot("3-close");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

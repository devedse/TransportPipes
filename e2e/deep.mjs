// The Deep Level dug out, with a lift and a few deep buildings: node deep.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/deep"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "d");
for (const c of ["gold", "gold", "gold", "gold", "gold", "gold", "gold", "gold", "level", "level", "level", "level", "level", "level", "alltech"]) await cheat(c);
await page.waitForTimeout(1000);
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, w, tp.appStore.getState().company, tp.appStore.getState().era, s.content.building(t), x, y).ok;
  for (let y = co.estateY + 8; y < co.estateY + 24; y++) for (let x = co.estateX + 8; x < co.estateX + 22; x++) {
    if (ok("deepLift", x, y) && ok("storage", x + 3, y)) {
      await c.placeBuilding("storage", x + 3, y); await c.cheat("finish"); await c.cheat("stock");
      await c.placeBuilding("deepLift", x, y); await c.cheat("finish"); await c.cheat("deep");
      return { x, y };
    }
  }
  return null;
}));
await page.waitForTimeout(1500);
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = tp.appStore.getState().company; const w = tp.gameClient.world;
  const ok = (t, x, y) => tp.rules.canPlace(s.content, w, tp.appStore.getState().company, tp.appStore.getState().era, s.content.building(t), x, y).ok;
  const off = co.deep.offset; const placed = [];
  for (const type of ["magmaTap", "vault", "crystalGrotto", "deepMine", "crystalGrotto", "storage"]) {
    let done = false;
    for (let y = co.estateY + off + 2; y < co.estateY + off + 30 && !done; y++) for (let x = co.estateX + 2; x < co.estateX + 30 && !done; x++) {
      if (ok(type, x, y)) { const r = await c.placeBuilding(type, x, y); if (r.success) { placed.push(type); done = true; } }
    }
    await c.cheat("stock");
  }
  await c.cheat("finish");
  tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + off + 16, 0.45);
  return placed.join(",");
}));
await page.waitForTimeout(5000);
await shot("1-deep");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

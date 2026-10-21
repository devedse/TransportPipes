// A new estate's starter kit (copper, coal, stone near the HQ, a pond in the first parcel) and a parcel's contents:
// node survey.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/survey"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out);
await newPlayer(page, "k");
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const found = await page.evaluate(() => {
  const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  const count = {};
  for (let y = co.estateY; y < co.estateY + 32; y++) for (let x = co.estateX; x < co.estateX + 32; x++) {
    const t = w.tile(x, y); if (t?.deposit) count[t.deposit] = (count[t.deposit] ?? 0) + 1;
    if (t?.terrain === "water") count.water = (count.water ?? 0) + 1;
  }
  tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + 14, 0.55);
  tp.appStore.setState({ overlay: "deposits" });
  return count;
});
console.log(JSON.stringify(found));
await page.waitForTimeout(3000);
await shot("1-deposits");
// The first parcel to buy (parcel 2 in the panel's numbering)
console.log(await page.evaluate(() => {
  tp.appStore.setState({ overlay: "none" });
  const co = tp.appStore.getState().company;
  // A locked parcel: parcel 1 is the first one to buy
  const tile = { x: co.estateX + 12, y: co.estateY + 3 };
  tp.gameClient.engineCallbacks.onClick(tile, tp.gameClient.world.buildingAt(tile.x, tile.y));
  const s = tp.appStore.getState();
  return JSON.stringify({ parcel: s.selectedParcel, unlocked: co.unlockedParcels, wild: s.wildTile, visiting: s.visiting });
}));
await page.waitForTimeout(1200);
const panel = await page.locator(".parcel-panel").count();
if (panel) await page.locator(".parcel-panel").screenshot({ path: `${out}/2-parcel.png` });
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

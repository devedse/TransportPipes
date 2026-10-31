// A building on a forest (playtest: "when you put a lumber camp on a forest it removes the forest, and when you remove
// it again it's gone"): it stands on the forest without felling it, and moved away the trees are there again.
//   node forest-regrow.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/forest-regrow"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "fr");
for (const c of ["level", "gold"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const placed = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const def = s().content.building("lumberCamp");
  for (let r = 1; r < 30; r++) for (let dx = -r; dx <= r; dx++) for (let dy = -r; dy <= r; dy++) {
    const x = post.x + dx, y = post.y + dy;
    if (w.terrain(x, y) === "forest" && tp.rules.canPlace(s().content, w, s().company, s().era, def, x, y).ok) {
      const result = await tp.gameClient.connection.placeBuilding("lumberCamp", x, y);
      if (result.success) { await tp.gameClient.connection.cheat("finish"); tp.gameClient.engine.lookAt(x, y, 1); return { x, y }; }
    }
  }
  return null;
});
console.log("placed on", JSON.stringify(placed));
check(placed !== null, "a Lumber Camp stands on a forest tile");
if (placed !== null) {
  await page.waitForTimeout(2500);
  check(await page.evaluate((p) => tp.gameClient.world.terrain(p.x, p.y), placed) === "forest", "the forest is still under it");
  await shot("1-on-forest");
  const moved = await page.evaluate(async (p) => {
    const s = () => tp.appStore.getState(); const w = tp.gameClient.world;
    const camp = w.buildingAt(p.x, p.y);
    const def = s().content.building("lumberCamp");
    for (let r = 2; r < 12; r++) for (let dx = -r; dx <= r; dx++) for (let dy = -r; dy <= r; dy++) {
      const x = p.x + dx, y = p.y + dy;
      if (w.terrain(x, y) !== "forest" && tp.rules.canPlace(s().content, w, s().company, s().era, def, x, y, camp.id, true).ok) {
        const result = await tp.gameClient.connection.move(camp.id, x, y);
        if (result.success) return { x, y };
      }
    }
    return null;
  }, placed);
  console.log("moved to", JSON.stringify(moved));
  await page.waitForTimeout(2500);
  check(moved !== null && await page.evaluate((p) => tp.gameClient.world.terrain(p.x, p.y), placed) === "forest", "moved away, the forest is there again");
  await page.evaluate((p) => tp.gameClient.engine.lookAt(p.x, p.y, 1), placed);
  await page.waitForTimeout(1500);
  await shot("2-moved-away");
}
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

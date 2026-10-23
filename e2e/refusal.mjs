// A refusal points at the nearest tile that works (after run 3, row 3): Ana clicked the pond three times for a Water
// Pump, read "Not on water" and gave up for nine days. A building in hand clicked on a tile where it can't stand puts
// the golden tile on the nearest one where it can, and says so; the pump on water says it stands beside the water.
//   node refusal.mjs shots/refusal
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/refusal";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "refusal");
await cheat("level");
await cheat("gold");
await page.waitForTimeout(1500);

// A Wind Moily in hand, clicked on the trading post
const post = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const p = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  tp.gameClient.setTool({ kind: "build", type: "windMoily" });
  tp.gameClient.engine.lookAt(p.x, p.y, 0.8);
  return { x: p.x, y: p.y };
});
await page.waitForTimeout(1200);
const at = await page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [post.x, post.y]);
await page.mouse.move(at.x, at.y);
await page.waitForTimeout(400);
await page.mouse.click(at.x, at.y);
await page.waitForTimeout(800);
const after = await page.evaluate(() => {
  const s = tp.appStore.getState();
  const hint = s.hintTile;
  const ok = hint === null ? false : tp.rules.canPlace(s.content, tp.gameClient.world, s.company, s.era, s.content.building("windMoily"), hint.x, hint.y).ok;
  return { hint, ok, toast: [...document.querySelectorAll(".toast")].map((t) => t.textContent).join(" | ") };
});
console.log("after the click:", JSON.stringify(after));
await shot("1-refused");

// The pump on water: the client's own words
const water = await page.evaluate(() => {
  const s = tp.appStore.getState();
  const co = s.company;
  const pump = s.content.building("waterPump");
  for (let y = co.estateY; y < co.estateY + 32; y++)
    for (let x = co.estateX; x < co.estateX + 32; x++)
      if (tp.gameClient.world.terrain(x, y) === "water") {
        // A pond on a parcel you don't own yet answers "Buy this parcel first": the water's own rule is the one to see
        const reason = tp.rules.canPlace(s.content, tp.gameClient.world, co, s.era, pump, x, y).reason;
        if (reason !== "Buy this parcel first") return reason;
      }
  return null;
});
console.log("a pump on water:", water);

const pass =
  after.hint !== null &&
  after.ok &&
  (Math.abs(after.hint.x - post.x) + Math.abs(after.hint.y - post.y)) <= 4 &&
  /the golden tile nearby works/.test(after.toast) &&
  (water === null || water === "Beside the water, not on it");
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

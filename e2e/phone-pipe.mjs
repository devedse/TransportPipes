// No drag on a phone (F3, the redesign §3.7): a newcomer on a phone builds Loady's first panel, then Show me on the pipe
// step plans the pipe to the trading post and one tap on "Lay it" lays it.
//   node phone-pipe.mjs shots/phone-pipe
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/phone-pipe";
const { browser, page, errors, shot } = await start(out, { width: 390, height: 844, touch: true });
await newPlayer(page, "ph");
await page.waitForSelector(".loady-card");
// Step 1, the panel: built where Show me points (as a script, the tap itself is touch.mjs's business)
const built = await page.evaluate(async () => {
  const s = tp.appStore.getState();
  const co = s.company;
  const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  for (let r = 4; r < 12; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const x = post.x + dx, y = post.y + dy;
        if (tp.rules.canPlace(s.content, w, co, s.era, s.content.building("solarPanel"), x, y).ok && !w.conductorAt(x + 1, y) && !w.conductorAt(x - 1, y) && !w.conductorAt(x, y + 1) && !w.conductorAt(x, y - 1)) {
          const placed = await tp.gameClient.connection.placeBuilding("solarPanel", x, y);
          await tp.gameClient.connection.cheat("finish");
          return placed.success ? { x, y } : placed.error;
        }
      }
  return "no spot";
});
console.log("panel", JSON.stringify(built));
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstSale", null, { timeout: 30000 });
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.pipe != null, null, { timeout: 30000 }).catch(() => console.log("no pipe planned"));
const text = await page.textContent(".loady-card .loady-bubble p");
console.log("step 2 text:", text);
await shot("1-pipe-step");
await page.tap(".loady-card button.primary");
await page.waitForSelector(".route-banner", { timeout: 10000 });
console.log("banner:", await page.textContent(".route-banner"));
await shot("2-planned");
await page.tap(".route-banner button.primary");
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id !== "firstSale", null, { timeout: 30000 }).catch(() => {});
const after = await page.evaluate(() => tp.appStore.getState().company?.tutorial?.id);
console.log("now on:", after);
await shot("3-after");
const pass = text?.includes("Show me plans the pipe") && after === "firstUpgrade";
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

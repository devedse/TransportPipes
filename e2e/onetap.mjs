// One tap lays the pipe (run 2's recommendation 4): a Wind Moily with no pipe is stuck, its panel's one fix says where
// the pipe goes and what it costs, and a single tap lays it: no preview to confirm, and the Moily runs.
//   node onetap.mjs shots/onetap
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/onetap";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "onetap");
await cheat("level");
await cheat("gold");

const placed = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const co = () => s().company;
  const content = s().content;
  const ok = (type, x, y) => tp.rules.canPlace(content, w, co(), s().era, content.building(type), x, y).ok;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  // A free tile 4 to 9 tiles from the post with no pipe or building around it, so the Moily's pipes reach nothing
  const lonely = (x, y) =>
    [-1, 0, 1].every((dy) => [-1, 0, 1].every((dx) => (dx === 0 && dy === 0) || (!w.buildingAt(x + dx, y + dy) && w.conductorAt(x + dx, y + dy)?.kind !== "pipe")));
  for (let r = 4; r < 10; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const x = post.x + dx, y = post.y + dy;
        if (ok("windMoily", x, y) && lonely(x, y)) {
          const result = await c.placeBuilding("windMoily", x, y);
          if (!result.success) throw new Error(result.error);
          await c.cheat("finish");
          await new Promise((res) => setTimeout(res, 1500));
          const id = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "windMoily" && t.x === x && t.y === y).id;
          tp.gameClient.showBuilding(id);
          return { x, y, id };
        }
      }
  throw new Error("no lonely tile for a Wind Moily");
});
console.log("placed", JSON.stringify(placed));
await page.waitForSelector(".building-panel .idle-status", { timeout: 15000 });
await page.waitForTimeout(1500);
const head = await page.evaluate(() => ({
  pill: document.querySelector(".building-panel .idle-status .status")?.textContent,
  fix: document.querySelector(".building-panel .idle-status .fix")?.textContent,
}));
console.log("panel", JSON.stringify(head));
await shot("1-stuck");

await page.click(".building-panel .idle-status .fix");
await page.waitForTimeout(1200);
const banner = await page.locator(".route-banner").count();
await page.waitForTimeout(3500);
const after = await page.evaluate(() => ({
  pill: document.querySelector(".building-panel .idle-status .status")?.textContent ?? null,
  status: document.querySelector(".building-panel .status")?.textContent ?? null,
}));
console.log("banner after the tap:", banner, "after", JSON.stringify(after));
await shot("2-laid");

// The fix lays a pipe: to your own building, or to the gate with a Send when the project or a neighbour wants it
const pass = head.pill?.includes("Stuck: energy can't leave") && /^Lay the pipe (to your|and send to)/.test(head.fix ?? "") && banner === 0 && after.pill === null;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

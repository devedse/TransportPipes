// Loady's later steps: the Storage (and what it keeps), the daily orders, a step that waits for a level
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/guide"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "g");
await cheat("gold");
// On to "firstStorage"
for (let i = 0; i < 7; i++) await cheat("tutorial");
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstStorage");
await page.click(".loady-card button.primary");
await page.waitForTimeout(1500);
await shot("1-storage-show-me");
console.log(await page.evaluate(async () => {
  const s = tp.appStore.getState(); const c = tp.gameClient.connection; const w = tp.gameClient.world; const co = s.company;
  const hint = s.hintTile; if (!hint) return "no hint";
  const placed = await c.placeBuilding("storage", hint.x, hint.y); if (!placed.success) return placed.error;
  await c.cheat("finish");
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  const route = tp.rules.routePipe(w, co, { x: hint.x + 1, y: hint.y }, { x: post.x - 1, y: post.y }, s.content)
    ?? tp.rules.routePipe(w, co, { x: hint.x, y: hint.y + 1 }, { x: post.x, y: post.y + 1 }, s.content);
  if (route) await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1);
  const storage = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "storage");
  tp.gameClient.select(storage.id);
  return `storage at ${hint.x},${hint.y}, pipe ${route?.length ?? "none"}`;
}));
await page.waitForTimeout(2500);
await shot("2-storage-panel");
await page.evaluate(() => tp.gameClient.select(null));
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstOrder", null, { timeout: 15000 }).catch(() => console.log("still not on firstOrder"));
await page.click(".loady-card button.primary");
await page.waitForTimeout(2000);
await shot("3-orders");
await page.keyboard.press("Escape");
await cheat("tutorial");
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstResearch");
await page.waitForTimeout(800);
await shot("4-waits-for-level");
console.log(await page.evaluate(() => JSON.stringify(tp.appStore.getState().company.tutorial)));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

// A new player with Loady: node onboarding.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/onboarding"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out);
await newPlayer(page, "o");
await page.waitForSelector(".loady-card");
await shot("1-welcome");
await page.click(".loady-card button.primary");
await page.waitForTimeout(2500);
await shot("2-show-me");
// Click where the ghost is (the middle of the screen, where Show me looked)
const box = await page.locator("canvas").first().boundingBox();
await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
await page.waitForTimeout(400);
await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
await page.waitForTimeout(4000);
await page.keyboard.press("Escape");
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id !== "firstPanel", null, { timeout: 30000 }).catch(() => console.log("still on step 1"));
await shot("3-step2");
// Lay the pipe the way Show me suggests: from the panel to the trading post
console.log(await page.evaluate(async () => {
  const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  // The player's own panel: the newest (a dealt kit has panels of its own)
  const panel = [...w.buildings.values()].filter((t) => t.owner === co.id && t.type === "solarPanel").sort((a, b) => b.id - a.id)[0];
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  if (!panel || !post) return "missing";
  await tp.gameClient.connection.cheat("finish");
  // The tiles beside a building (not its corners: a pipe there would not touch it)
  const border = (b) => { const r = []; for (let i = 0; i < b.size; i++) { r.push([b.x + i, b.y - 1], [b.x + i, b.y + b.size], [b.x - 1, b.y + i], [b.x + b.size, b.y + i]); } return r; };
  for (const [fx, fy] of border(panel)) for (const [tx, ty] of border(post)) {
    const route = tp.rules.routePipe(w, co, { x: fx, y: fy }, { x: tx, y: ty }, s.content);
    if (route) { const r = await tp.gameClient.connection.placePipes(route.flatMap((t) => [t.x, t.y]), 1); return r.error ?? `pipe ${route.length}`; }
  }
  return "no route";
}));
await page.waitForFunction(() => tp.appStore.getState().company?.tutorial?.id === "firstUpgrade", null, { timeout: 60000 }).catch(() => console.log("no sale yet"));
await page.waitForTimeout(1500);
await shot("4-step3");
console.log(await page.evaluate(() => JSON.stringify(tp.appStore.getState().company.tutorial)));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

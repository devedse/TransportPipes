// Coin pops, a finished building and a level-up: node juice.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/juice"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "j");
// A connected solar panel next to the trading post
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  for (const [dx, dy] of [[2, 0], [0, 2], [-2, 0], [0, -2]]) {
    const r = await c.placeBuilding("windMoily", post.x + dx, post.y + dy);
    if (r.success) { await c.placePipes([post.x + dx / 2, post.y + dy / 2], 1); tp.gameClient.engine.lookAt(post.x, post.y, 1.1); return "placed"; }
  }
  return "no spot";
}));
await page.waitForTimeout(800);
await cheat("finish");
await page.waitForTimeout(450);
await shot("1-finished-burst");
await page.waitForTimeout(6000);
await shot("2-coins");
await cheat("level");
await page.waitForTimeout(900);
await shot("3-level-up");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

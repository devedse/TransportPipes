// Drawing a better pipe over your own pipes upgrades them where they lie: node pipe-upgrade.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/pipe-upgrade"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "u");
for (const c of ["gold", "gold", "level", "level", "alltech"]) await cheat(c);
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const co = () => tp.appStore.getState().company;
  await c.skipTutorial();
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  // A storage with copper ingots (Copper Pipes need one per tile)
  for (let d = 3; d < 12; d++) {
    if ((await c.placeBuilding("storage", post.x - d, post.y)).success) break;
  }
  await c.cheat("finish");
  await c.cheat("stock");
  // A straight grey pipe from the trading post
  for (const [dx, dy] of [[0, 2], [2, 0], [0, -1], [-1, 0]]) {
    const tiles = [];
    for (let i = 0; i < 6; i++) tiles.push(post.x + (dx === 0 ? 0 : dx > 0 ? dx + i : dx - i), post.y + (dy === 0 ? 0 : dy > 0 ? dy + i : dy - i));
    if ((await c.placePipes(tiles, 1)).success) {
      await new Promise((r) => setTimeout(r, 1000));
      tp.gameClient.engine.lookAt(tiles[4], tiles[5], 1);
      return { tiles };
    }
  }
  return null;
});
console.log(JSON.stringify(setup));
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
await page.waitForTimeout(1200);
const tier = () => page.evaluate((t) => Array.from({ length: t.length / 2 }, (_, i) => tp.gameClient.world.conductorAt(t[i * 2], t[i * 2 + 1])?.tier), setup.tiles);
console.log("before", JSON.stringify(await tier()));
await shot("1-grey");
// The copper pipe tool, dragged with the mouse from the first tile to the last
await page.evaluate(() => tp.gameClient.setTool({ kind: "pipe", tier: 2 }));
const a = await page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [setup.tiles[0], setup.tiles[1]]);
const b = await page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [setup.tiles.at(-2), setup.tiles.at(-1)]);
await page.mouse.move(a.x, a.y);
await page.mouse.down();
for (let i = 1; i <= 12; i++) await page.mouse.move(a.x + ((b.x - a.x) * i) / 12, a.y + ((b.y - a.y) * i) / 12);
const tip = await page.locator(".hover-tip").textContent().catch(() => null);
console.log("tip while dragging:", tip);
await shot("2-dragging");
await page.mouse.up();
await page.waitForTimeout(2000);
const after = await tier();
console.log("after", JSON.stringify(after));
await shot("3-copper");
console.log(after.every((t) => t === 2) ? "ok   the grey pipes are copper now" : "FAIL not all upgraded");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

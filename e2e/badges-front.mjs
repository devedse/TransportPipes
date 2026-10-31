// Level badges over every building (the owner, 2026-10-31: "draw the building levels always in front": in a block of
// Iron Mines the ones in front hid the badges behind them): a block of Wind Moilys side by side, levels 2 to 10, every
// badge drawn on the badge layer, over the buildings' layer, and none left behind when the buildings are drawn again.
//   node badges-front.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/badges-front"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1280, height: 860 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "bf");
for (const c of ["gold", "level"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const built = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const w = tp.gameClient.world; const c = tp.gameClient.connection;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const definition = s().content.building("windMoily");
  const ids = [];
  // A block of 3×3 beside the post, tile against tile
  for (let ox = 3; ox < 12 && ids.length === 0; ox++)
    for (let oy = -4; oy < 4 && ids.length === 0; oy++) {
      const spots = [];
      for (let dx = 0; dx < 3; dx++) for (let dy = 0; dy < 3; dy++) spots.push([post.x + ox + dx, post.y + oy + dy]);
      if (!spots.every(([x, y]) => tp.rules.canPlace(s().content, w, s().company, s().era, definition, x, y).ok)) continue;
      for (const [x, y] of spots) {
        const placed = await c.placeBuilding("windMoily", x, y);
        if (placed.success) ids.push(placed.buildingId ?? null);
      }
    }
  await c.cheat("finish");
  await new Promise((r) => setTimeout(r, 1500));
  const mine = [...w.buildings.values()].filter((t) => t.owner === s().company.id && t.type === "windMoily");
  let level = 2;
  for (const b of mine) {
    await c.upgrade(b.id, level - b.level);
    level = level >= 10 ? 2 : level + 1;
  }
  await new Promise((r) => setTimeout(r, 2000));
  const first = mine[4] ?? mine[0];
  return { count: mine.length, at: first ? { x: first.x, y: first.y } : null };
});
console.log(JSON.stringify(built));
check(built.count >= 9, "a block of Wind Moilys", String(built.count));
if (built.at) await page.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x, y, 0.9), built.at);
await page.waitForTimeout(2500);
await shot("1-block");
const layers = await page.evaluate(() => {
  const e = tp.gameClient.engine;
  const order = e.worldLayer.children;
  const levelled = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === tp.appStore.getState().company.id && t.level >= 2 && t.constructionEndsAt === null).length;
  return { objects: order.indexOf(e.objectLayer), badges: order.indexOf(e.badgeLayer), badgeCount: e.badgeLayer.children.length, levelled };
});
console.log(JSON.stringify(layers));
check(layers.badges > layers.objects, "the badges are drawn over the buildings' layer", JSON.stringify(layers));
check(layers.badgeCount >= layers.levelled && layers.levelled >= 9, "every levelled building has its badge there", JSON.stringify(layers));
// Drawn again (a level more): the old badges go with their buildings
const again = await page.evaluate(async () => {
  const before = tp.gameClient.engine.badgeLayer.children.length;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === tp.appStore.getState().company.id && t.type === "windMoily" && t.level < 10);
  await tp.gameClient.connection.upgrade(b.id, 1);
  await new Promise((r) => setTimeout(r, 2500));
  return { before, after: tp.gameClient.engine.badgeLayer.children.length };
});
console.log(JSON.stringify(again));
check(again.after === again.before, "no badge left behind when a building is drawn again", JSON.stringify(again));
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

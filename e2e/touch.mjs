// Touch: one finger moves the map, a finger held still selects with a box (and paints with the build tool).
// Real touch events through Chromium's DevTools protocol. node touch.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/touch"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 390, height: 844, touch: true });
await newPlayer(page, "f");
await cheat("gold");
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.evaluate(async () => {
  await tp.gameClient.connection.skipTutorial();
  const co = tp.appStore.getState().company;
  const post = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  tp.gameClient.engine.lookAt(post.x, post.y, 0.7);
  tp.appStore.setState({ levelUp: null });
});
await page.waitForTimeout(1500);
const cdp = await page.context().newCDPSession(page);
const touch = (type, x, y) => cdp.send("Input.dispatchTouchEvent", { type, touchPoints: type === "touchEnd" ? [] : [{ x, y }] });
const drag = async (from, to, holdMs) => {
  await touch("touchStart", from.x, from.y);
  await page.waitForTimeout(holdMs);
  for (let i = 1; i <= 10; i++) await touch("touchMove", from.x + ((to.x - from.x) * i) / 10, from.y + ((to.y - from.y) * i) / 10);
  await touch("touchEnd");
  await page.waitForTimeout(600);
};
const results = [];
const expect = (label, ok, extra = "") => results.push(`${ok ? "ok  " : "FAIL"} ${label}${extra ? ` (${extra})` : ""}`);
const origin = () => page.evaluate(() => tp.gameClient.engine.tileToScreen(0, 0));
const selection = () => page.evaluate(() => tp.appStore.getState().selection.length);
// The HQ and the trading post on screen
const pts = await page.evaluate(() => { const co = tp.appStore.getState().company; return [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === co.id).map((t) => tp.gameClient.engine.tileToScreen(t.x, t.y)); });
const xs = pts.map((p) => p.x), ys = pts.map((p) => p.y);
const box = { from: { x: Math.max(5, Math.min(...xs) - 80), y: Math.max(150, Math.min(...ys) - 80) }, to: { x: Math.min(385, Math.max(...xs) + 80), y: Math.min(700, Math.max(...ys) + 80) } };

// A quick drag moves the map
const before = await origin();
await drag({ x: 200, y: 600 }, { x: 260, y: 650 }, 50);
const after = await origin();
expect("a quick drag moves the map", Math.abs(after.x - before.x) > 30, `${Math.round(after.x - before.x)} px`);
expect("and selects nothing", (await selection()) === 0);

// Held still, then dragged: a box
const pts2 = await page.evaluate(() => { const co = tp.appStore.getState().company; return [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === co.id).map((t) => tp.gameClient.engine.tileToScreen(t.x, t.y)); });
const xs2 = pts2.map((p) => p.x), ys2 = pts2.map((p) => p.y);
const from = { x: Math.max(5, Math.min(...xs2) - 70), y: Math.max(160, Math.min(...ys2) - 70) }, to = { x: Math.min(385, Math.max(...xs2) + 70), y: Math.min(700, Math.max(...ys2) + 70) };
const still = await origin();
await drag(from, to, 700);
expect("a long press and drag selects with a box", (await selection()) >= 2, `${await selection()} selected`);
const moved = (await origin()).x - still.x;
expect("and doesn't move the map", Math.abs(moved) < 2, `${Math.round(moved)} px`);
await shot("1-touch-box");
console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

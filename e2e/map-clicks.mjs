// The map and clicks after run 4: a refusal goes once the ghost fits, no red ghost over the building just placed, the
// keep card never opens by itself, and nothing under the return card takes a click: node map-clicks.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/map-clicks"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "m");
for (const c of ["gold", "level"]) await cheat(c);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
await page.waitForTimeout(1500);
let failed = 0;
const check = (ok, what, detail = "") => {
  if (!ok) failed++;
  console.log(`${ok ? "ok  " : "FAIL"} ${what}${detail ? `  (${detail})` : ""}`);
};

// A tile a Wind Moily can't take (water on the estate, or a locked parcel) and one it can, near the trading post
const spots = await page.evaluate(() => {
  const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const def = s.content.building("windMoily");
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  let good = null, bad = null;
  for (let r = 2; r < 16 && (!good || !bad); r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const x = post.x + dx, y = post.y + dy;
        const p = tp.rules.canPlace(s.content, w, co, s.era, def, x, y);
        if (p.ok && !good) good = { x, y };
        if (!p.ok && !bad && w.buildingAt(x, y) === undefined && w.conductorAt(x, y) === undefined) bad = { x, y, reason: p.reason };
      }
  tp.gameClient.engine.lookAt(post.x, post.y, 0.8);
  return { good, bad };
});
console.log("spots", JSON.stringify(spots));
await page.waitForTimeout(800);
const screen = (t) => page.evaluate(({ x, y }) => tp.gameClient.engine.tileToScreen(x, y), t);
await page.evaluate(() => tp.gameClient.setTool({ kind: "build", type: "windMoily" }));
await page.waitForTimeout(300);

// 1. A refusal, then a tile that fits: the red toast goes
const bad = await screen(spots.bad);
await page.mouse.move(bad.x, bad.y);
await page.waitForTimeout(300);
await page.mouse.click(bad.x, bad.y);
await page.waitForTimeout(400);
const refusals = () => page.locator(".toast.error").count();
check((await refusals()) === 1, "a click where it doesn't fit says why", spots.bad.reason);
const good = await screen(spots.good);
await page.mouse.move(good.x, good.y, { steps: 4 });
await page.waitForTimeout(500);
check((await refusals()) === 0, "the refusal goes once the ghost fits");
await shot("1-ghost-fits");

// 2. Placed: no red ghost over it, and a second click there places nothing
await page.mouse.click(good.x, good.y);
await page.waitForTimeout(1500);
const ghost = await page.evaluate(() => tp.gameClient.engine.ghostLayer.children.length);
check(ghost === 0, "no ghost over the building just placed", `${ghost} ghost parts`);
await shot("2-placed");
await page.mouse.click(good.x, good.y);
await page.waitForTimeout(600);
const standing = await page.locator(".toast", { hasText: "already standing" }).count();
check(standing === 0 && (await refusals()) === 0, "a click on it places nothing and says nothing wrong");
await page.keyboard.press("Escape");

// 3. A guest at a keep level: the card waits for their own tap, the link glows
await page.evaluate(() => {
  const s = tp.appStore.getState();
  tp.appStore.setState({ user: { ...s.user, isGuest: true }, levelUp: 3 });
});
await page.waitForTimeout(6000);
const keep = await page.evaluate(() => ({ open: tp.appStore.getState().keepOpen, nudge: tp.appStore.getState().keepNudge }));
check(!keep.open && keep.nudge, "the keep card doesn't open by itself; the link glows", JSON.stringify(keep));
check((await page.locator(".keep-link.nudge").count()) === 1, "the top bar's Keep my estate glows");
await shot("3-keep-nudge");
await page.locator(".keep-link").click();
check(await page.evaluate(() => tp.appStore.getState().keepOpen), "one tap opens it");
await page.evaluate(() => tp.appStore.setState({ keepOpen: false, user: { ...tp.appStore.getState().user, isGuest: false } }));

// 4. The return card: what lies under it can't be reached, and its buttons have their own names
await cheat("gone");
await page.evaluate(() => tp.gameClient.hello());
await page.waitForSelector(".return-card", { timeout: 30000 });
await page.waitForTimeout(800);
const under = await page.evaluate(() => ({
  topBar: document.querySelector(".top-bar")?.closest("[inert]") !== null,
  steps: document.querySelector(".next-steps")?.closest("[inert]") !== null,
}));
check(under.topBar, "the top bar under the card is out of reach", JSON.stringify(under));
const names = await page.$$eval(".return-card .return-line button", (t) => t.map((b) => b.getAttribute("aria-label")));
check(names.length === 0 || new Set(names).size === names.length, "the card's buttons each have their own name", names.join(" | "));
await shot("4-return");
await page.click(".return-card button.primary");
await page.waitForTimeout(500);
check(await page.evaluate(() => document.querySelector(".top-bar")?.closest("[inert]") === null), "and back in reach after Continue");

console.log(errors.length ? `FAIL errors: ${errors.join("\n")}` : failed > 0 ? `FAIL ${failed} checks` : "PASS errors: none");
await browser.close();

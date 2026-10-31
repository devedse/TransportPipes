// The Next steps' first pipe says what it brings (the owner, 2026-10-31: "whenever something says x will happen, it
// actually does"; it said "would bring about +30 g/min", from the hub's price alone): a Wind Moily away from the
// Trading Post, its step "Pipe your Wind Moily to your Trading Post" with no "about", the pipe dragged as a player
// would, and two market minutes later the top bar is up by what the step said, within 2% (the player's route is the
// map's shortest, the step's the server's).   node pipe-tip.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/pipe-tip"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const rate = () => page.evaluate(() => tp.appStore.getState().company.goldPerMinute);

await newPlayer(page, "pt");
for (const c of ["level", "gold"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const moily = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const definition = s().content.building("windMoily");
  for (let r = 7; r < 14; r++)
    for (let dy = -r; dy <= r; dy++)
      for (const dx of [-r, r]) {
        const x = post.x + dx, y = post.y + dy;
        if (tp.rules.canPlace(s().content, w, s().company, s().era, definition, x, y).ok && (await tp.gameClient.connection.placeBuilding("windMoily", x, y)).success) {
          await tp.gameClient.connection.cheat("finish");
          return { x, y, post: { x: post.x, y: post.y } };
        }
      }
  return null;
});
console.log("moily", JSON.stringify(moily));
check(moily !== null, "a Wind Moily away from the Trading Post");
await page.evaluate(() => tp.gameClient.refreshNextSteps());
await page.waitForTimeout(2000);
const step = await page.evaluate(() => tp.appStore.getState().nextSteps.find((t) => t.title.startsWith("Pipe your Wind Moily")) ?? null);
console.log("step", JSON.stringify(step && { title: step.title, reason: step.reason, gain: step.gainPerMinute }));
check(step !== null, "the step: pipe it to your Trading Post");
check(step !== null && !/about/.test(step.reason) && /\+[\d.,]+ g\/min/.test(step.reason), "it says what the pipe brings, not about", step?.reason);
await shot("1-step");

const before = await rate();
const laid = await page.evaluate(async ({ x, y, post }) => {
  const s = tp.appStore.getState(); const w = tp.gameClient.world; const co = s.company;
  const sides = (b, size) => [[-1, 0], [size, 0], [0, -1], [0, size]].map(([dx, dy]) => ({ x: b.x + dx, y: b.y + dy }));
  let best = null;
  for (const from of sides({ x, y }, 1)) for (const to of sides(post, 2)) {
    const route = tp.rules.routePipe(w, co, from, to, s.content);
    if (route && (best === null || route.length < best.length)) best = route;
  }
  if (best === null) return null;
  const placed = await tp.gameClient.connection.placePipes(best.flatMap((t) => [t.x, t.y]), 1);
  return placed.success ? best.length : placed.error;
}, moily);
console.log("pipe", laid);
check(typeof laid === "number", "the pipe laid", String(laid));
// The market looks every minute: two of them
await page.waitForTimeout(130_000);
const got = (await rate()) - before;
console.log("before", before.toFixed(1), "said", step?.gainPerMinute, "got", got.toFixed(1));
check(step !== null && Math.abs(got - step.gainPerMinute) <= Math.max(0.5, Math.abs(got) * 0.02), "the top bar is up by what it said, within 2%", `said +${step?.gainPerMinute}, got +${got.toFixed(1)}`);
await shot("2-after");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// A level's goods coming in (playtest: "I pushed the grow button and now all buildings go ZZ for a few frames every few
// seconds"): a building that steps up keeps its network, and the running buildings on its pipes never show ZZ meanwhile.
//   node level-flicker.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/level-flicker"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "lf");
for (const c of ["level", "level", "level", "gold"]) await cheat(c);
await kit(page, ["windMoily", "windMoily", "windMoily"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// Three moilies on the post: one bought up to level 11, its goods still to come
const ids = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const co = tp.appStore.getState().company; const w = tp.gameClient.world;
  await c.setWantsFromMarket(false);
  const moilies = [...w.buildings.values()].filter((t) => t.owner === co.id && t.type === "windMoily").map((t) => t.id);
  await c.upgrade(moilies[0], 11 - w.buildings.get(moilies[0]).level);
  return moilies;
});
const on = ids.slice(1);
await page.evaluate((id) => tp.gameClient.engine.lookAt(tp.gameClient.world.buildings.get(id).x, tp.gameClient.world.buildings.get(id).y, 0.8), ids[0]);
await page.waitForTimeout(4000);
const idleNow = () => page.evaluate((on) => {
  const views = [...tp.gameClient.engine.chunks.values()].flatMap((t) => t.objects);
  return on.map((id) => views.find((v) => v.building?.id === id)?.idle?.visible ?? null);
}, on);
console.log("idle before:", JSON.stringify(await idleNow()));
check((await idleNow()).every((t) => t === false), "the other moilies run");
const networkBefore = await page.evaluate((id) => tp.gameClient.world.buildings.get(id).network, ids[0]);

// Watch every frame while its goods come in and it steps up
await page.evaluate((on) => {
  window.__idle = 0; window.__frames = 0;
  const tick = () => {
    const views = [...tp.gameClient.engine.chunks.values()].flatMap((t) => t.objects);
    window.__frames++;
    if (on.some((id) => views.find((v) => v.building?.id === id)?.idle?.visible === true)) window.__idle++;
    if (!window.__stop) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
  // And at every map update: a running moily whose network's flows aren't here would draw ZZ
  window.__gaps = 0;
  const world = tp.gameClient.world;
  const apply = world.applyObjects.bind(world);
  world.applyObjects = (chunk) => {
    apply(chunk);
    if (on.some((id) => { const b = world.buildings.get(id); return b?.network != null && !world.flows.has(b.network); })) window.__gaps++;
  };
}, on);
const bought = await page.evaluate((id) => tp.gameClient.connection.orderRest(id, null), ids[0]);
await page.waitForTimeout(4000);
const seen = await page.evaluate(() => { window.__stop = true; return { idle: window.__idle, frames: window.__frames, gaps: window.__gaps }; });
const after = await page.evaluate((id) => ({ level: tp.gameClient.world.buildings.get(id).level, network: tp.gameClient.world.buildings.get(id).network }), ids[0]);
console.log("bought:", JSON.stringify(bought), "watched:", JSON.stringify(seen), "after:", JSON.stringify(after), "network before", networkBefore);
check(after.level === 11, "it stepped up to 11", String(after.level));
check(after.network === networkBefore, "it kept its network", `${networkBefore} → ${after.network}`);
check(seen.idle === 0 && seen.gaps === 0, "no ZZ on the running moilies meanwhile", `${seen.idle} of ${seen.frames} frames, ${seen.gaps} updates without their flows`);
await shot("1-after");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

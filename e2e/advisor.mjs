// The advisor's next steps on a real estate: a moily, a mine and a factory piped to the trading post, too little energy.
// Shows the card, Show me on the first step, and an upgrade step done from the card. node advisor.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/advisor"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "a");
for (const c of ["gold", "gold", "level", "level", "level", "level"]) await cheat(c);
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const co = () => tp.appStore.getState().company;
  await c.skipTutorial();
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const placed = [];
  const place = async (type, best) => {
    const def = s.content.building(type); const spots = [];
    for (let y = co().estateY; y < co().estateY + 32; y++) for (let x = co().estateX; x < co().estateX + 32; x++) {
      const p = tp.rules.canPlace(s.content, w, co(), tp.appStore.getState().era, def, x, y);
      if (p.ok) spots.push({ x, y, m: p.multiplier, d: Math.abs(x - post.x) + Math.abs(y - post.y) });
    }
    spots.sort((a, b) => (best ? b.m - a.m : 0) || a.d - b.d);
    for (const spot of spots.filter((t) => t.d >= 3).slice(0, 20)) {
      if ((await c.placeBuilding(type, spot.x, spot.y)).success) { placed.push({ type, ...spot }); return spot; }
    }
    return null;
  };
  const sides = (b, size = 1) => [[-1, 0], [size, 0], [0, -1], [0, size]].map(([dx, dy]) => ({ x: b.x + dx, y: b.y + dy }));
  const pipe = async (b) => {
    for (const from of sides(b)) for (const to of sides(post, 2)) {
      const route = tp.rules.routePipe(w, co(), from, to, s.content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) return route.length;
    }
    return null;
  };
  const moily = await place("windMoily", false);
  const mine = await place("ironMine", false);
  const factory = await place("smallFactory", false);
  const pipes = [];
  for (const b of [moily, mine, factory]) if (b) pipes.push(await pipe(b));
  await c.cheat("finish");
  await new Promise((r) => setTimeout(r, 2500));
  tp.gameClient.engine.lookAt(post.x, post.y, 0.7);
  await tp.gameClient.refreshNextSteps();
  return { placed, pipes, steps: tp.appStore.getState().nextSteps.map((t) => `${t.kind}: ${t.title} — ${t.reason} [${t.action} ${t.cost}]`) };
});
console.log(JSON.stringify(setup, null, 1));
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
await page.waitForTimeout(800);
await shot("1-next-steps");
const card = page.locator(".next-steps");
const results = [];
const expect = (label, ok, extra = "") => results.push(`${ok ? "ok  " : "FAIL"} ${label}${extra ? ` (${extra})` : ""}`);
expect("the card shows", await card.isVisible());
expect("three steps", (await page.locator(".next-step").count()) === 3, `${await page.locator(".next-step").count()}`);

// Show me on the first step
await page.locator(".next-step").first().getByRole("button", { name: /Show me|Open research|Plan the pipe/ }).click();
await page.waitForTimeout(1200);
const after = await page.evaluate(() => { const s = tp.appStore.getState(); return { tool: s.tool, hint: s.hintTile, overlay: s.overlay, building: s.selectedBuilding, research: s.researchOpen, parcel: s.selectedParcel }; });
console.log("after show me", JSON.stringify(after));
await shot("2-show-me");
await page.keyboard.press("Escape"); await page.keyboard.press("Escape");

// An upgrade step done from the card
const upgrade = page.locator(".next-step").filter({ has: page.getByRole("button", { name: /^Upgrade ·/ }) }).first();
if (await upgrade.count()) {
  const id = await page.evaluate(() => tp.appStore.getState().nextSteps.find((t) => t.action === "upgrade")?.building);
  const level = () => page.evaluate((i) => tp.gameClient.world.buildings.get(i)?.level, id);
  const before = await level();
  await upgrade.getByRole("button", { name: /^Upgrade ·/ }).click();
  await page.waitForTimeout(3000);
  expect("the upgrade button upgrades", (await level()) > before, `${before} → ${await level()}`);
  console.log("steps after", JSON.stringify(await page.evaluate(() => tp.appStore.getState().nextSteps.map((t) => t.title))));
  await shot("3-after-upgrade");
}
const time = await page.evaluate(async () => { const t = performance.now(); await tp.gameClient.connection.getNextSteps(); return Math.round(performance.now() - t); });
console.log(`GetNextSteps round trip ${time} ms`);
console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

// Widen (after run 3, the design team's step 9): a Wind Moily that makes more than its pipe carries offers the Copper
// Pipe first (Grey doesn't widen); once its pipe is copper and full, its panel's one fix is "Widen its pipe ×2 · … (1 hour
// of your income)", and one tap widens it.
//   node widen.mjs shots/widen
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/widen";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "widen");
for (const c of ["gold", "earn", "earn", "earn", "level", "level", "level", "level", "alltech"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

// A Storage with copper ingots, and the starter kit's first Wind Moily upgraded far past what a grey pipe carries
const moily = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const co = tp.appStore.getState().company;
  await c.skipTutorial();
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  for (let d = 3; d < 14; d++) {
    if ((await c.placeBuilding("storage", post.x - d, post.y + 1)).success) break;
  }
  await c.cheat("finish");
  await c.cheat("stock");
  const m = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  // Its trading post sells far more than a pipe brings
  await c.upgrade(post.id, 20);
  return m.id;
});
// Levels in steps of ten, with gold for each
const raise = (levels) =>
  page.evaluate(
    async ({ id, levels }) => {
      const c = tp.gameClient.connection;
      const done = [];
      for (let left = levels; left > 0; left -= 10) {
        for (let i = 0; i < 5; i++) await c.cheat("earn");
        done.push((await c.upgrade(id, Math.min(10, left))).success);
      }
      return done;
    },
    { id: moily, levels },
  );
console.log("to level 21:", JSON.stringify(await raise(20)));
await page.waitForTimeout(4000);
const fixOf = () => page.evaluate(() => tp.appStore.getState().buildingDetails?.status?.fix ?? null);
await page.evaluate((id) => tp.gameClient.showBuilding(id), moily);
await page.waitForSelector(".building-panel", { timeout: 15000 });
await page.waitForTimeout(2500);
let fix = await fixOf();
console.log("first fix:", fix?.kind, fix?.label);
expect("a full grey pipe offers copper, not Widen", fix?.kind === "pipeTier", fix?.label ?? "none");
await shot("1-grey");
if (fix?.kind === "pipeTier") {
  await page.click(".building-panel button.fix");
  await page.waitForTimeout(4000);
  console.log("to level 61:", JSON.stringify(await raise(40)));
  await page.waitForTimeout(4000);
  await page.evaluate((id) => tp.gameClient.showBuilding(id), moily);
  await page.waitForTimeout(3000);
}
fix = await fixOf();
console.log("then:", fix?.kind, fix?.label);
if (fix?.kind !== "widen") {
  // What the panel knows when no Widen shows
  console.log(JSON.stringify(await page.evaluate(() => {
    const d = tp.appStore.getState().buildingDetails;
    return { status: d.status, rate: d.rate, flows: d.flows.map((f) => `${f.direction} ${f.resource} ${f.partnerType} ${Math.round(f.amount)} ${f.kind}`), outputs: d.outputs };
  })));
}
expect("a full Copper Pipe offers Widen ×2 for an hour of income", fix?.kind === "widen" && /^Widen its pipe ×2 · .* \(1 hour of your income\) · carries [\d.,k]+ a minute$/.test(fix.label), fix?.label ?? "none");
await shot("2-copper");
const gold = await page.evaluate(() => tp.appStore.getState().company.gold);
if (fix?.kind === "widen") {
  await page.click(".building-panel button.fix");
  await page.waitForTimeout(5000);
  await page.evaluate((id) => tp.gameClient.showBuilding(id), moily);
  await page.waitForTimeout(3000);
}
const after = await fixOf();
const paid = gold - (await page.evaluate(() => tp.appStore.getState().company.gold));
console.log("after:", after?.kind, after?.label, "paid about", Math.round(paid));
expect("one tap widened it: next is ×4 for two more hours", after?.kind === "widen" && /×4 .*\(2 more hours of your income\)/.test(after.label), after?.label ?? "none");
await shot("3-widened");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

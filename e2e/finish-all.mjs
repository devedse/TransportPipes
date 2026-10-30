// Buying the rest in the Yard's "What you're waiting for" (playtest: "when trying to finish something, we get a lot of
// "That has what it needs."", and "there should be a button to finish all at the top"): levels bought together are one
// line per good, which buys what its button says, for the gold it says; Buy all the rest at the top buys every want's
// rest at once.   node finish-all.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/finish-all"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "fa");
for (const c of ["level", "level", "level", "gold"]) await cheat(c);
await kit(page, ["solarPanel", "solarPanel", "solarPanel"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// Three Solar Panels bought up to level 13: three steps' goods each, and nothing brings them by itself
const ids = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const co = tp.appStore.getState().company;
  await c.setWantsFromMarket(false);
  const ids = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === co.id && t.type === "solarPanel").map((t) => t.id);
  for (const id of ids) await c.upgrade(id, 13 - tp.gameClient.world.buildings.get(id).level);
  await tp.gameClient.refreshCompany();
  return ids;
});
await page.evaluate(() => tp.gameClient.openYard());
await page.waitForSelector(".yard-panel .yard-waiting", { timeout: 15000 });
await page.waitForTimeout(1000);
const lines = () => page.locator(".yard-panel .yard-waiting li");
const wants = await page.evaluate(() => tp.appStore.getState().company.wants);
console.log("wants:", wants.map((w) => `${w.target} ${w.resource} ${w.amount} ${w.orderCost} g`).join(" · "));
check(wants.length === 2 * ids.length, "one line per good of each building, its three steps together", String(wants.length));
await shot("1-waiting");

// One good of the first panel: it costs what its button says
const gold = () => page.evaluate(() => tp.appStore.getState().company.gold);
const first = lines().first();
const said = Number((await first.locator("button", { hasText: "Buy the rest" }).innerText()).replace(/[^\d]/g, ""));
const before = await gold();
await first.locator("button", { hasText: "Buy the rest" }).click();
await page.waitForTimeout(2500);
const paid = before - (await gold());
// The panels earn a little gold meanwhile (before: 44 g said, 144 g paid)
check(paid <= said + 0.5 && said - paid <= said * 0.03 + 2, "a good's Buy the rest costs what it says", `said ${said}, paid ${paid.toFixed(1)}`);
await shot("2-one-good");

// Then every line in turn, as the player clicked them: no "That has what it needs."
for (let i = 0; i < 3 && (await lines().count()) > 0; i++) {
  const button = lines().first().locator("button", { hasText: "Buy the rest" });
  if ((await button.count()) === 0) break;
  await button.click();
  await page.waitForTimeout(1500);
}
const toasts = await page.locator(".toast").allInnerTexts();
check(!toasts.some((t) => /has what it needs/.test(t)), "no \"has what it needs\" on the way", toasts.join(" | "));
check(await page.evaluate((id) => tp.gameClient.world.buildings.get(id)?.level, ids[0]) === 13, "the first panel stepped up to 13");

// Buy all the rest at the top: the rest of every want, for what it says
const all = page.locator(".yard-panel .yard-waiting-all");
check((await all.count()) === 1, "Buy all the rest at the top of the list");
if ((await all.count()) === 1) {
  const allSaid = Number((await all.innerText()).replace(/[^\d]/g, ""));
  const expected = await page.evaluate(() => tp.appStore.getState().company.wants.reduce((s, w) => s + (w.orderCost ?? 0), 0));
  check(Math.abs(allSaid - expected) <= 1, "it says what the lines add up to", `${allSaid} vs ${expected}`);
  const at = await gold();
  await all.click();
  await page.waitForTimeout(2500);
  const spent = at - (await gold());
  // Each line's price is rounded up, and the panels earn meanwhile: what it costs is a little less
  const count = await page.evaluate(() => tp.appStore.getState().company.wants.length);
  check(spent <= allSaid + 0.5 && allSaid - spent <= allSaid * 0.03 + count + 1, "and costs that", `said ${allSaid}, paid ${spent.toFixed(1)}`);
  const levels = await page.evaluate((ids) => ids.map((id) => tp.gameClient.world.buildings.get(id)?.level), ids);
  check(levels.every((l) => l === 13), "every panel stepped up to 13", JSON.stringify(levels));
  check((await page.locator(".yard-panel .yard-waiting").count()) === 0, "nothing waits any more");
}
await shot("3-all");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// A building short of a good says why the Yard doesn't bring it (playtest: "why are my buildings paused while my
// friends are producing coal?"): here a Smelter whose Yard has nobody making coal, and the hub as the other way.
// The Yard's own cases (a neighbour not at their gate, using all of it, nobody making it) are in WantFacts.
//   node yard-short.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/yard-short"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "ys");
for (const c of ["level", "level", "gold"]) await cheat(c);
console.log(await kit(page, ["windMoily", "smelter"]));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.waitForTimeout(3000);
await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "smelter");
  tp.gameClient.showBuilding(b.id);
});
await page.waitForSelector(".building-panel .idle-status", { timeout: 15000 });
await page.waitForTimeout(1500);
const status = (await page.locator(".building-panel .idle-status").innerText()).replace(/\s+/g, " ");
console.log(status);
check(/Its pipes bring 0 of the \d+ coal it needs\. (Nobody in your Yard makes coal|In your Yard, ).*buy it from the hub: tick Coal under Buy from the hub on your Trading Post/.test(status), "the Smelter says why the Yard brings no coal, and the hub", status);
await shot("1-smelter");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

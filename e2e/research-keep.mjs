// Research keeps going while you're away (run 2's recommendation 10): the Research panel has the switch, on by default,
// and turning it off sticks.
//   node research-keep.mjs shots/research-keep
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/research-keep";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "keep");
await cheat("level");
await page.waitForTimeout(1200);
await page.evaluate(() => tp.gameClient.openResearch());
await page.waitForTimeout(1500);
const box = page.locator(".keep-researching input");
const before = await box.isChecked();
await shot("1-research-panel");
await box.click();
await page.waitForTimeout(1500);
const after = await page.evaluate(() => tp.appStore.getState().company.research.keepResearching);
console.log("on by default:", before, "after the click:", after);
const pass = before === true && after === false;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

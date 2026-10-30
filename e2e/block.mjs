// Blocking a neighbour in the trade settings (playtest: "i cant click the buttons on other players traders to disable
// trading with them": a block waits an hour's notice and the switch snapped back): the switch goes off at once and says
// when the block counts; on again calls it off. node block.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/block"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "bl");
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.evaluate(() => tp.gameClient.openTradeSettings());
await page.waitForSelector(".trade-settings", { timeout: 15000 });
await page.waitForTimeout(1500);
const rows = page.locator(".trade-settings .partner-list:not(.price-list) li.partner");
const count = await rows.count();
check(count > 0, "a neighbour to block", String(count));
if (count > 0) {
  const row = rows.first();
  const input = row.locator(".switch input");
  check(await input.isChecked(), "trading with them at first");
  await row.locator(".switch").click();
  await page.waitForTimeout(1500);
  check(!(await input.isChecked()), "the switch is off once clicked");
  const text = (await row.innerText()).replace(/\s+/g, " ");
  check(/trading stops at \d\d:\d\d/.test(text), "and says when the block counts", text);
  await shot("1-blocking");
  await row.locator(".switch").click();
  await page.waitForTimeout(1500);
  check(await input.isChecked(), "on again calls it off");
  const pending = await page.evaluate(() => tp.appStore.getState().neighbours.some((t) => t.blockAt != null || t.blocked));
  check(!pending, "no block waits any more");
  await shot("2-off-again");
}
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

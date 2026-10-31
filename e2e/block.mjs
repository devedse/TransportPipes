// Blocking a neighbour in the trade settings (playtest: "i cant click the buttons on other players traders to disable
// trading with them": the switch snapped back): it goes off and the block counts at once (the owner, for the playtest:
// it waited an hour's notice); on again trades again. node block.mjs <outdir>
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
  check(!/trading stops at/.test(text), "nothing to wait for", text);
  check(await page.evaluate(() => tp.appStore.getState().neighbours.some((t) => t.blocked)), "the block counts at once");
  await shot("1-blocking");
  await row.locator(".switch").click();
  await page.waitForTimeout(1500);
  check(await input.isChecked(), "on again trades again");
  const pending = await page.evaluate(() => tp.appStore.getState().neighbours.some((t) => t.blockAt != null || t.blocked));
  check(!pending, "no block waits any more");
  await shot("2-off-again");
}
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

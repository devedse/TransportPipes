// Grow brings what it says (the owner, 2026-10-31: "whenever something says x will happen, it actually does, so for
// example the grow button should match exactly"): the gold panel's "+X g/min", then Grow, its levels' goods bought, and
// a couple of the market's minutes later the top bar's gold a minute is up by that, within 1%.   node grow-promise.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/grow-promise"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const rate = () => page.evaluate(() => tp.appStore.getState().company.goldPerMinute);

await newPlayer(page, "gp");
for (const c of ["level", "level", "level", "gold"]) await cheat(c);
console.log(await kit(page, ["windMoily", "windMoily", "windMoily", "solarPanel", "ironMine", "smallFactory", "storage"]));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// The market looks every minute: two of them, so the price has settled on what the plot sells
await page.waitForTimeout(130_000);
const before = await rate();
await page.locator(".top-bar button[aria-expanded]").first().click();
await page.waitForSelector(".gold-uses .gold-use.grow", { timeout: 20000 });
const line = (await page.locator(".gold-uses .gold-use.grow").innerText()).replace(/\s+/g, " ");
const said = await page.evaluate(() => tp.appStore.getState().goldUses?.grow?.gain ?? null);
console.log("before", before.toFixed(1), "said", said, "|", line.slice(0, 160));
check(said !== null && said > 0, "Grow says what it brings", line.slice(0, 160));
await page.locator(".gold-uses button", { hasText: /^Grow · / }).click();
await page.waitForTimeout(2000);
await page.evaluate(() => tp.gameClient.connection.orderAll());
await page.waitForTimeout(130_000);
const after = await rate();
const got = after - before;
console.log("after", after.toFixed(1), "got", got.toFixed(1));
check(Math.abs(got - said) <= Math.max(0.5, Math.abs(got) * 0.01), "the top bar is up by what it said, within 1%", `said +${said}, got +${got.toFixed(1)}`);
await shot("1-after");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

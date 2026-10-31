// Where your gold a minute comes from (playtest: "add a very detailed explanation what makes this total gold come to
// be ... we keep seeing it change all the time and I don't understand why"): the gold panel lists every line the top
// bar adds up, they add up to it, and it says what isn't in it and why it moves.   node income.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/income"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1100 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "in");
for (const c of ["level", "gold"]) await cheat(c);
console.log(await kit(page));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.waitForTimeout(4000);
await page.locator(".top-bar button[aria-expanded]").first().click();
await page.waitForSelector(".gold-uses .income", { timeout: 15000 });
await page.waitForTimeout(1000);
const income = await page.evaluate(() => tp.appStore.getState().goldUses?.income);
const sum = income.lines.reduce((s, t) => s + t.gold, 0);
console.log("lines:", income.lines.map((t) => `${t.kind} ${t.resource} ${t.gold}`).join(" · "), "total", income.total);
check(income.lines.length > 0 && Math.abs(sum - income.total) < 0.5 + income.lines.length * 0.05, "the lines add up to the total", `${sum} vs ${income.total}`);
const top = await page.evaluate(() => tp.appStore.getState().company.goldPerMinute);
check(Math.abs(top - income.total) <= Math.max(2, Math.abs(top) * 0.02), "which is the top bar's gold a minute", `${top} vs ${income.total}`);
const text = (await page.locator(".gold-uses .income").innerText()).replace(/\s+/g, " ");
check(/Where your \+[\d.,k]+ g\/min comes from/.test(text), "it says what it explains", text.slice(0, 120));
check(/Sold at the hub \(your Trading Post\)/.test(text) && /Energy · [\d.,]+\/min × [\d.]+ g/.test(text), "each good: how many a minute at what price", text.slice(0, 400));
check(/Your gold a minute \(the top bar\)/.test(text), "a total line");
check(/Not in this number/.test(text) && /Why it keeps changing/.test(text), "what isn't in it and why it moves");
await shot("1-income");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

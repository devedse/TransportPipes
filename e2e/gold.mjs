// Your gold can (after run 4: the players spent 0.2% of what they earned and banked 24–29 days of income): the top bar's
// gold reads "· N days ▸"; tapping it lists, shared first, the Era Wonder's goods to order (a Capital player: nobody here
// makes them yet), and "Grow everything that pays" with its parts; Grow buys them in one tap and says so; Undo is there
// for an hour.
//   node gold.mjs shots/gold
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/gold";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "gold");
for (const c of ["gold", "earn", "earn", "earn", "level", "level", "wonderopen"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(3000);

const label = await page.locator(".top-gold").innerText();
expect("the top bar's gold reads in days of income", /· [\d.]+ days ▸/.test(label), label.replace(/\s+/g, " "));
await page.click(".top-gold");
await page.waitForSelector(".gold-uses .gold-use", { timeout: 20000 }).catch(() => null);
await page.waitForTimeout(1500);
const list = await page.locator(".gold-uses").innerText().catch(() => "");
console.log(list.replace(/\n+/g, " | "));
expect("it lists the Era Wonder's goods first", /🏛 The Great Dynamo wants [\d.,k]+ .* you can order/.test(list), "");
expect("and Grow everything that pays, with its price and gain", /Grow everything that pays: about \+[\d.,k]+ g\/min for [\d.,kM]+ g/.test(list));
await shot("1-list");

const before = await page.evaluate(() => tp.appStore.getState().company.gold);
const grow = page.locator(".gold-uses button", { hasText: /^Grow · / });
if ((await grow.count()) > 0) {
  await grow.click();
  await page.waitForTimeout(3000);
}
const after = await page.evaluate(() => ({
  gold: tp.appStore.getState().company.gold,
  toast: [...document.querySelectorAll(".toast")].map((t) => t.textContent).join(" | "),
}));
expect("one tap grew them and said so", after.gold < before && /Grown: \d+ buildings? for /.test(after.toast), after.toast);
const undo = await page.locator(".gold-uses button", { hasText: "Undo the last Grow" }).count();
expect("Undo is there for an hour", undo > 0);
await shot("2-grown");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

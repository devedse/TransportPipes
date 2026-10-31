// What's new and the return card together (the owner, 2026-10-31: "if you don't close the release notes quick enough
// it's not clickable anymore"): after an update and some days away the page opens with both. The return card ("While
// you were away") came once the map was drawn and made everything beside it unclickable, What's new too, which stayed
// drawn on top. Now one card at a time: the return card first, What's new once it is closed, and its Got it closes it.
//   node whats-new-return.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/whats-new-return"] = process.argv.slice(2);
const BASE = process.env.BASE_URL ?? "http://localhost:5173";
const { browser, page, errors, shot, cheat } = await start(out, { width: 1280, height: 860 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const whatsNew = page.locator(".whats-new");
const returnCard = page.locator(".return-card");

await newPlayer(page, "wr");
console.log(await settle({ page }, "solarPanel"));
await cheat("level");
// Earning before going (a card with nothing to say isn't shown)
for (let i = 0; i < 30 && !(await page.evaluate(() => (tp.appStore.getState().company?.goldPerMinute ?? 0) > 0)); i++) await page.waitForTimeout(1000);
await page.waitForTimeout(5000);
const changes = await page.evaluate(() => fetch("/changes.json").then((r) => r.json()));
// This browser saw the game as it was three changes ago: What's new opens with the page
await page.evaluate((date) => localStorage.setItem("tp.whatsNewSeen", date), changes.changes[3].date);
await page.goto(`${BASE}/`);
await page.waitForSelector(".top-bar", { timeout: 30000 });
await whatsNew.waitFor({ timeout: 30000 }).catch(() => {});
check((await whatsNew.count()) === 1, "after an update: What's new");
// ...and the return card comes while it is open (the player was ten days away; a reload would start the absence
// now, so the client says hello again as it does when it comes back)
await cheat("gone");
await page.evaluate(() => tp.gameClient.hello());
await returnCard.waitFor({ timeout: 30000 }).catch(() => {});
// Slow to close: the map is drawn and the return card has come
await page.waitForTimeout(4000);
await shot("1-both");
const both = { whatsNew: await whatsNew.count(), returnCard: await returnCard.count() };
console.log(JSON.stringify(both));
check(both.returnCard === 1, "the return card came", JSON.stringify(both));
check(both.whatsNew === 0, "What's new waits while the return card is open", JSON.stringify(both));
// What's new's Got it, if it shows, takes the click
if (both.whatsNew === 1) {
  const clicked = await whatsNew.locator("button", { hasText: "Got it" }).click({ timeout: 3000 }).then(() => true, (e) => e.message.split("\n")[0]);
  check(clicked === true, "What's new's Got it takes the click", String(clicked));
}
if ((await returnCard.count()) === 1) {
  const clicked = await returnCard.locator("button.primary").last().click({ timeout: 3000 }).then(() => true, (e) => e.message.split("\n")[0]);
  check(clicked === true, "the return card's Continue takes the click", String(clicked));
}
await page.waitForTimeout(800);
await shot("2-after-return");
check((await returnCard.count()) === 0, "the return card closed");
check((await whatsNew.count()) === 1, "then What's new shows");
const items = await whatsNew.locator(".whats-new-list li").count();
check(items > 0, "with the changes since", String(items));
const clicked = await whatsNew.locator("button", { hasText: "Got it" }).click({ timeout: 3000 }).then(() => true, (e) => e.message.split("\n")[0]);
check(clicked === true, "its Got it takes the click", String(clicked));
await page.waitForTimeout(500);
check((await whatsNew.count()) === 0 && (await page.locator("[role=dialog]:visible").count()) === 0, "and the map is free");
check((await page.evaluate(() => localStorage.getItem("tp.whatsNewSeen"))) === changes.date, "it counts as seen");
await shot("3-free");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

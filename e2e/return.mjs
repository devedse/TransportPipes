// The way back (v1.2 §10): A plays, is "gone" for ten days (the dev cheat) and comes back to the three screens; then A
// sleeps until the Quiet Isles ("isles") and comes back to the landing cards, moving to another one: node return.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/return"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out, { width: 1280, height: 860 });
await newPlayer(a.page, "ra");
const before = await a.page.evaluate(() => tp.appStore.getState().company.slot);

// Ten days away: what happened, that the estate is safe
await a.cheat("gone");
// A reload would disconnect (and the absence would start now): the client says hello again instead
await a.page.evaluate(() => tp.gameClient.hello());
await a.page.waitForSelector(".return-dialog", { timeout: 30000 });
await a.page.waitForTimeout(800);
await a.shot("1-while-away");
await a.page.click(".return-dialog button.primary:has-text('Next')");
await a.page.waitForTimeout(300);
await a.shot("2-safe");
const safe = await a.page.$eval(".return-dialog", (t) => t.textContent);
if (!safe.includes("Where you left it")) fail("the estate is where A left it");
await a.page.click(".return-dialog button.primary");
await a.page.waitForTimeout(500);
if (await a.page.$(".return-dialog")) fail("two screens when the estate stayed");

// The Quiet Isles: the landing cards
await a.cheat("isles");
await a.page.evaluate(() => tp.gameClient.hello());
await a.page.waitForSelector(".return-dialog", { timeout: 30000 });
await a.page.click(".return-dialog button.primary:has-text('Next')");
await a.page.click(".return-dialog button.primary:has-text('Next')");
await a.page.waitForSelector(".landing-cards", { timeout: 15000 });
await a.page.waitForTimeout(500);
await a.shot("3-landing");
const cards = await a.page.$$eval(".landing-card", (t) => t.map((x) => x.textContent.replace(/\s+/g, " ").trim()));
console.log(cards);
const back = await a.page.evaluate(() => tp.appStore.getState().company.slot);
if (back !== before) fail(`A lands on its old seat (${before}, got ${back})`);
if (cards.length < 2) fail("other places to land");
const other = a.page.locator(".landing-card:not(.here)").first();
await other.click();
await a.page.waitForTimeout(2000);
const moving = await a.page.evaluate(() => tp.appStore.getState().company.moving.arrivesAt);
if (moving === null) fail("a move to the other card");
await a.shot("4-moving");

if (a.errors.length > 0) fail(a.errors.join(" | "));
await a.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

// The way back (v1.2 §10, F5): A plays, is "gone" for ten days (the dev cheat) and comes back to one card; then A sleeps
// until the Quiet Isles ("isles") and comes back to the card with the landing cards, moving to another one:
// node return.mjs <outdir>
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
await a.page.waitForSelector(".return-card", { timeout: 30000 });
await a.page.waitForTimeout(800);
await a.shot("1-while-away");
const card = await a.page.$eval(".return-card", (t) => t.textContent);
console.log(card.replace(/\s+/g, " ").slice(0, 300));
if (!/While you were away/.test(card) || !/days away/.test(card)) fail("one card: what happened, on the server's clock");
// One card, one tap
await a.page.click(".return-card button.primary");
await a.page.waitForTimeout(500);
if (await a.page.$("[role=dialog]")) fail("one card, then the map");

// The Quiet Isles: the card asks where to land
await a.cheat("isles");
await a.page.evaluate(() => tp.gameClient.hello());
await a.page.waitForSelector(".return-card .landing-cards", { timeout: 30000 });
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

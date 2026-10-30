// The way back (v1.2 §10, F5): A plays, is "gone" for ten days (the dev cheat) and comes back to one card:
// node return.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/return"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out, { width: 1280, height: 860 });
await newPlayer(a.page, "ra");
// A plot starts empty (1.4 §6): something that earns while A is away
console.log(await settle(a, "solarPanel"));
// Earning before A goes (a card that has nothing to say isn't shown)
for (let i = 0; i < 30 && !(await a.page.evaluate(() => (tp.appStore.getState().company?.goldPerMinute ?? 0) > 0)); i++) await a.page.waitForTimeout(1000);
await a.page.waitForTimeout(5000);

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

if (a.errors.length > 0) fail(a.errors.join(" | "));
await a.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

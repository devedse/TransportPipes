// Step 7's screens (F9–F11): the crew's own spot in the top bar, the research queue, chat day stamps and "new since
// you left", one toast at a time on a phone: node step7.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/step7"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

// A crew founder: the crew's spot
const a = await start(out);
await newPlayer(a.page, "s7", "Start with friends");
if (await a.page.$(".share-panel")) await a.page.click("text=Start playing");
await a.page.waitForTimeout(1500);
const crewSpot = await a.page.$eval(".top-crew", (t) => t.textContent).catch(() => null);
console.log("crew spot", crewSpot);
if (crewSpot === null) fail("the crew has its own spot in the top bar");
await a.shot("1-top-bar");

// The research queue: two techs, the first starts, the second waits in line
await a.cheat("research");
await a.page.evaluate(() => tp.gameClient.openResearch());
await a.page.waitForSelector(".research-panel", { timeout: 15000 });
await a.page.click("button[aria-label='Queue Ore Washing']");
await a.page.waitForTimeout(800);
await a.page.click("button[aria-label='Queue Double Furnace']");
await a.page.waitForTimeout(1200);
const queue = await a.page.evaluate(() => ({ current: tp.appStore.getState().company.currentResearch, queue: tp.appStore.getState().company.researchQueue }));
console.log("research", JSON.stringify(queue));
if (queue.current !== "oreWashing" || !queue.queue?.includes("doubleFurnace")) fail("the first starts, the second waits in the queue");
await a.shot("2-research-queue");
await a.page.keyboard.press("Escape");

// Chat: a day stamp over today's messages
await a.page.evaluate(() => tp.gameClient.toggleChat());
await a.page.waitForSelector(".chat-panel", { timeout: 10000 });
await a.page.fill(".chat-form input", "hello from step 7");
await a.page.press(".chat-form input", "Enter");
await a.page.waitForTimeout(1500);
const stamp = await a.page.$eval(".chat-day", (t) => t.textContent).catch(() => null);
console.log("day stamp", stamp);
if (stamp !== "Today") fail("a day stamp in the chat");
await a.shot("3-chat");

// A phone: one toast at a time
const p = await start(out, { width: 420, height: 860, touch: true });
await newPlayer(p.page, "s7p");
await p.page.evaluate(() => { for (let i = 0; i < 3; i++) tp.toast?.(`toast ${i}`); });
await p.page.evaluate(() => { const s = tp.appStore.getState(); tp.appStore.setState({ toasts: [...s.toasts, { id: 9001, text: "one", kind: "info" }, { id: 9002, text: "two", kind: "info" }, { id: 9003, text: "three", kind: "info" }] }); });
await p.page.waitForTimeout(300);
const shown = await p.page.$$eval(".toasts .toast", (t) => t.map((x) => x.textContent));
console.log("phone toasts", shown);
if (shown.length !== 1) fail("one toast at a time on a phone");
await p.shot("4-phone-toast");

if (a.errors.length + p.errors.length > 0) fail([...a.errors, ...p.errors].join(" | "));
await a.browser.close();
await p.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

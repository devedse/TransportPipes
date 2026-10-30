// What's new (playtest: an update came with "New update!" and no word of what changed): after an update the changes
// since the newest one seen on this browser, a day a list; seen once closed; a new company gets none; the version badge
// shows the latest. Needs TransportPipes.Frontend/public/changes.json (node scripts/changes.mjs). node whats-new.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/whats-new"] = process.argv.slice(2);
const BASE = process.env.BASE_URL ?? "http://localhost:5173";
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const card = page.locator(".whats-new");
const game = async () => { await page.goto(`${BASE}/`); await page.waitForSelector(".top-bar", { timeout: 30000 }); await page.waitForTimeout(3000); };

await newPlayer(page, "wn");
check((await card.count()) === 0, "a new company gets no catching up");
const changes = await page.evaluate(() => fetch("/changes.json").then((r) => r.json()));
// This browser saw the game as it was three changes ago
await cheat("level");
await page.evaluate((date) => localStorage.setItem("tp.whatsNewSeen", date), changes.changes[3].date);
await game();
check((await card.count()) === 1, "after an update: what's new");
const items = await card.locator(".whats-new-list li").allInnerTexts();
console.log(items);
check(items.length === 3 && items[0] === changes.changes[0].text[0], "the three changes since, newest first", JSON.stringify(items));
check((await card.locator("h4").first().innerText()).length > 0, "under their day");
await shot("1-new");
await page.keyboard.press("Escape");
await page.waitForTimeout(500);
check((await card.count()) === 0, "Esc closes it");
check((await page.evaluate(() => localStorage.getItem("tp.whatsNewSeen"))) === changes.date, "and it counts as seen");
await game();
check((await card.count()) === 0, "seen: not again");
await page.click(".version-badge");
await page.waitForTimeout(800);
check((await card.locator(".whats-new-list li").count()) >= 10, "the version badge shows the latest");
await shot("2-latest");
await card.locator("button", { hasText: "Got it" }).click();
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

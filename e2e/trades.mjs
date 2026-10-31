// Every trade in one place (the owner, 2026-10-31: "a separate button where we can clearly see these trades happening
// would make sense ... each menu has a very clear goal"): the toolbar's Trades button (and C) opens the Trades panel;
// its tabs Now (every line of your gold a minute, your links), Links, Settings and Town each say their goal and show
// theirs; the Yard panel has no Links tab any more, and its All your trades opens Trades.   node trades.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/trades"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "tr");
for (const c of ["level", "gold"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
console.log(await kit(page, ["windMoily", "windMoily"]));
await page.waitForTimeout(2500);

await page.locator(".toolbar button", { hasText: "Trades" }).click();
await page.waitForSelector(".trades-panel", { timeout: 10000 });
await page.waitForTimeout(2500);
const now = (await page.locator(".trades-panel").innerText()).replace(/\s+/g, " ");
console.log("Now:", now.slice(0, 240));
check(/What you sell and buy this minute/.test(now) && /Where your/.test(now) && /Your links/.test(now), "Now: its goal, every line of your gold a minute, your links", now.slice(0, 160));
await shot("1-now");
// Nothing in a tab sticks out past the panel (the owner, 2026-10-31: "some things don't fit in the trade menu now")
const fits = () => page.evaluate(() => {
  const panel = document.querySelector(".trades-panel");
  const right = panel.getBoundingClientRect().right;
  const out = [...panel.querySelectorAll("*")].filter((t) => {
    const r = t.getBoundingClientRect();
    return r.width > 0 && r.right > right + 1 && !t.closest(".trade-table-wrap");
  });
  return out.slice(0, 3).map((t) => `${t.tagName.toLowerCase()}.${t.className} ${Math.round(t.getBoundingClientRect().right - right)}px`);
});
check((await fits()).length === 0, "Now: nothing sticks out of the panel", JSON.stringify(await fits()));
for (const [tab, goal] of [["Links", /Pipes that send one good/], ["Settings", /Your prices/], ["Town", /Everyone in your town/]]) {
  await page.locator(".trades-panel .tab", { hasText: tab }).click();
  await page.waitForTimeout(1500);
  const text = (await page.locator(".trades-panel").innerText()).replace(/\s+/g, " ");
  check(goal.test(text), `${tab}: its goal and its contents`, text.slice(0, 160));
  const out = await fits();
  check(out.length === 0, `${tab}: nothing sticks out of the panel`, JSON.stringify(out));
  await shot(`2-${tab.toLowerCase()}`);
}
await page.keyboard.press("c");
await page.waitForTimeout(400);
check((await page.locator(".trades-panel").count()) === 0, "C closes it");
await page.keyboard.press("c");
await page.waitForTimeout(800);
check((await page.locator(".trades-panel").count()) === 1, "and opens it");

// The Yard: who lives around you, no Links tab; All your trades opens Trades
await page.locator(".toolbar button", { hasText: "Yard" }).click();
await page.waitForSelector(".yard-panel:not(.trades-panel)", { timeout: 10000 });
await page.waitForTimeout(1500);
check((await page.locator(".yard-panel:not(.trades-panel) .tab", { hasText: "Links" }).count()) === 0, "the Yard panel has no Links tab");
await page.locator(".yard-panel button", { hasText: "All your trades" }).click();
await page.waitForTimeout(800);
check((await page.locator(".trades-panel").count()) === 1 && (await page.locator(".yard-panel:not(.trades-panel)").count()) === 0, "All your trades opens Trades (one side panel at a time)");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

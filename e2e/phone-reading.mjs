// Phone and reading after run 4: the Chat button above an open panel, the panel's network folded, the inbox lines
// apart, and the Links tab in one sentence: node phone-reading.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/phone-reading"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 390, height: 844 });
await newPlayer(page, "p");
for (const c of ["gold", "level", "finish"]) await cheat(c);
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
await page.waitForTimeout(4000);
let failed = 0;
const check = (ok, what, detail = "") => {
  if (!ok) failed++;
  console.log(`${ok ? "ok  " : "FAIL"} ${what}${detail ? `  (${detail})` : ""}`);
};
/** Whether a button can be tapped where it shows: nothing lies over its middle */
const tappable = (selector) =>
  page.evaluate((selector) => {
    const button = document.querySelector(selector);
    if (button === null) return "missing";
    const box = button.getBoundingClientRect();
    if (box.width === 0 || box.bottom > window.innerHeight || box.top < 0) return "off screen";
    const top = document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2);
    return top !== null && (top === button || button.contains(top)) ? "ok" : `covered by ${top?.className ?? "?"}`;
  }, selector);

// 1. A building's panel open: Chat stays in reach, and the network's numbers fold
const moily = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  tp.gameClient.showBuilding(b.id);
  return b.id;
});
await page.waitForSelector(".building-panel", { timeout: 15000 });
await page.waitForTimeout(2000);
await shot("1-panel");
const chat = await tappable(".top-actions button[aria-label=Chat]");
check(chat === "ok", "Chat can be tapped with a building panel open", chat);
const panelHeight = await page.evaluate(() => Math.round(document.querySelector(".building-panel").getBoundingClientRect().height));
const folded = await page.evaluate(() => document.querySelector(".building-panel .net-summary")?.closest("details")?.open === false);
check(folded, "the panel's network numbers are folded on a phone", `panel ${panelHeight} px`);

// 2. The inbox: entries apart, each with its time
await page.evaluate(() => tp.gameClient.select(null));
await page.waitForTimeout(500);
await page.locator(".top-bar button[title*='nbox'], button[aria-label*='nbox'], .bell").first().click().catch(() => {});
await page.waitForTimeout(1500);
await shot("2-inbox");
const times = await page.locator(".inbox-time").allInnerTexts();
check(times.length > 0 && times.every((t) => !/instant/.test(t)), "inbox entries say when", times.join(" | "));

// 3. The Yard and its Links tab
await page.keyboard.press("Escape");
await page.evaluate(() => tp.gameClient.openYard());
await page.waitForTimeout(2000);
await shot("3-yard");
const yardLine = await page.locator(".yard-line").innerText().catch(() => "");
const lacks = await page.locator(".yard-residents").innerText().catch(() => "");
check(!(/Nobody here needs (\w+)/.test(yardLine) && new RegExp(`Lacks[^\n]*${yardLine.match(/needs (\w+)/)?.[1]}`).test(lacks)), "the Yard's line doesn't say nobody needs what a neighbour lacks", yardLine);
await page.evaluate(() => tp.gameClient.openLinks());
await page.waitForTimeout(2000);
await shot("4-links");
const links = await page.locator(".link-picture").count();
const trunks = await page.locator(".trunks").count();
check(links === 1 && trunks === 0, "Links: one sentence and a picture, no trunks before Era 2");
await page.keyboard.press("Escape");
await page.evaluate(() => tp.appStore.setState({ yardOpen: false }));
await page.waitForTimeout(500);
// The chat opens from the top bar
await page.locator(".top-actions button[aria-label=Chat]").click();
await page.waitForTimeout(1000);
check((await page.locator(".chat-panel").count()) === 1, "the top bar's Chat opens the chat");
await shot("5-chat");

console.log(errors.length ? `FAIL errors: ${errors.join("\n")}` : failed > 0 ? `FAIL ${failed} checks` : "PASS errors: none");
await browser.close();

// The network panel's numbers, traceable (playtest: "I don't get why the total is 488x and the pipe itself shows
// something different. I want you to make the numbers clearly traceable"): each good says where it goes in numbers,
// its hub sales as units × price = gold, and they add up to the hub's line under Money per minute; a pipe's hover says
// it is what runs through that tile.   node network-numbers.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/network-numbers"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1100 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "nn");
for (const c of ["level", "gold"]) await cheat(c);
console.log(await kit(page));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.waitForTimeout(4000);
await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const post = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  tp.gameClient.showBuilding(post.id);
});
await page.waitForSelector(".building-panel .net-where", { timeout: 15000 });
await page.waitForTimeout(1500);
const where = await page.locator(".building-panel .net-where").allInnerTexts();
console.log(where.join(" | "));
check(where.some((t) => /sold to the hub [\d.,]+ × [\d.]+ g = \+[\d.,]+ g/.test(t)), "each good: sold to the hub, units × price = gold");
const sum = where.reduce((s, t) => s + Number((t.match(/= \+([\d.,]+) g/)?.[1] ?? "0").replace(/,/g, "")), 0);
const hub = Number(((await page.locator(".building-panel .plus", { hasText: "hub +" }).first().innerText()).match(/hub \+([\d.,]+)/)?.[1] ?? "0").replace(/,/g, ""));
check(Math.abs(sum - hub) <= Math.max(2, where.length), "they add up to the hub's line", `${sum} vs ${hub}`);
await shot("1-network");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

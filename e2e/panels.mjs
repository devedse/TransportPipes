// Every panel under the theme: node panels.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/panels"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "p");
for (const c of ["gold", "gold", "level", "level", "level", "level", "level", "research"]) await cheat(c);
await page.waitForTimeout(1500);
for (const [key, name] of [["r", "research"], ["n", "neighbours"], ["c", "corporation"], ["l", "leaderboard"], ["t", "chat"], ["u", "blueprints"]]) {
  await page.keyboard.press(key);
  await page.waitForTimeout(1500);
  await shot(name);
  await page.keyboard.press("Escape");
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
await page.click(".top-actions button[title='Inbox']").catch(() => {});
await page.waitForTimeout(1200);
await shot("inbox");
await page.keyboard.press("Escape");
await page.keyboard.press("m");
await page.waitForTimeout(800);
for (const tab of ["Prices", "Calendar", "Wonders", "Exchange"]) {
  await page.click(`.tabs >> text=${tab}`).catch(() => {});
  await page.waitForTimeout(1000);
  await shot(`hub-${tab.toLowerCase()}`);
}
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

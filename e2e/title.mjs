// The title screen, toolbar and managers: node title.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/title"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await page.goto(`${process.env.BASE_URL ?? "http://localhost:5173"}/`);
await page.waitForSelector(".auth-panel", { timeout: 30000 });
await page.waitForTimeout(1500);
await shot("1-login");
await newPlayer(page, "t");
for (const c of ["gold", "level", "level", "level", "level", "level"]) await cheat(c);
await page.waitForTimeout(1500);
await page.keyboard.press("i");
await page.waitForTimeout(1200);
await page.click("text=Managers");
await page.waitForTimeout(1200);
await shot("2-managers");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

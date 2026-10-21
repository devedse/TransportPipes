// The zoomed-out world: node far.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/far"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out);
await newPlayer(page, "f");
await page.evaluate(() => tp.gameClient.engine.lookAt(0, 0, 0.12));
await page.waitForTimeout(6000);
await shot("1-district");
await page.evaluate(() => tp.gameClient.engine.lookAt(0, 0, 0.05));
await page.waitForTimeout(6000);
await shot("2-world");
await page.evaluate(() => tp.gameClient.engine.lookAt(0, 0, 0.22));
await page.waitForTimeout(6000);
await shot("3-hub");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

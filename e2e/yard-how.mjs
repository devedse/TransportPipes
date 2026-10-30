// The Yard's "what happens when" (playtest: "can we clearly explain if you click the yard what happens when?"): open
// until you are piped to your Yard gate, then folded; the Monument and the workshops say their Company Level.
//   node yard-how.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/yard-how"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "yh");
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const openYard = async () => {
  await page.evaluate(() => tp.gameClient.openYard());
  await page.waitForSelector(".yard-panel .yard-how", { timeout: 15000 });
  await page.waitForTimeout(800);
};
await openYard();
const how = page.locator(".yard-panel .yard-how");
check(await how.evaluate((e) => e.open), "open while you aren't piped to the gate");
const text = (await how.innerText()).replace(/\s+/g, " ");
console.log(text);
for (const [label, re] of [
  ["the pipe first", /First: Lay a pipe from your network to your Yard gate/],
  ["every minute, at your crew price", /Every minute, also while you're away: .* at your crew price \(1\.15× the base price\)/],
  ["what nobody needs sells at the Trading Post", /goes on to your Trading Post, which sells it to the town's market \(the hub\)/],
  ["building and levels", /When you build or level up: /],
  ["the Wonder switch: a minute or two", /within a minute or two/],
  ["the Monument's level", /From Company Level \d+: The Yard's Monument/],
  ["the workshops' level", /From Company Level \d+: Workshops/],
]) check(re.test(text), label, text.slice(0, 200));
check((await how.locator("li.locked").count()) >= 1, "what your level doesn't reach yet is locked");
await shot("1-new");

console.log("settle:", await settle({ page }, "windMoily"));
await page.waitForTimeout(3000);
await openYard();
const piped = await page.evaluate(() => tp.appStore.getState().yard?.connected);
console.log("piped:", piped);
if (piped) {
  check(!(await how.evaluate((e) => e.open)), "folded once you are piped");
  await how.locator("summary").click();
  await page.waitForTimeout(300);
  check(/^✓/.test((await how.locator("li").first().innerText()).trim()), "the pipe step is ticked");
}
await shot("2-piped");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

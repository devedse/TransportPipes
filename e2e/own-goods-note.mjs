// Your own goods going into your levels (playtest: after Grow, "I went from 3900 to 3400, why the hell did that
// happen?"): the Waits line and the Yard's What you're waiting for say what they would have sold for, until they're in.
// The number is the server's (WantFacts); here it is set to see both texts.   node own-goods-note.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/own-goods-note"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "og");
for (const c of ["level", "level", "level", "gold"]) await cheat(c);
await kit(page, ["solarPanel"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.evaluate(async () => {
  const c = tp.gameClient.connection; const co = tp.appStore.getState().company;
  await c.setWantsFromMarket(false);
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "solarPanel");
  await c.upgrade(b.id, 11 - b.level);
  await tp.gameClient.refreshCompany();
  await tp.gameClient.openYard();
});
await page.waitForSelector(".yard-panel .yard-waiting", { timeout: 15000 });
await page.evaluate(() => tp.appStore.setState({ company: { ...tp.appStore.getState().company, ownGoodsIntoWants: 512 } }));
await page.waitForTimeout(300);
const corner = await page.locator(".goals").innerText();
const yard = await page.locator(".yard-panel .yard-waiting").innerText();
check(/your own goods go in: −512 g\/min of sales until they're in/.test(corner), "the Waits line says what the levels take", corner.slice(0, 300));
check(/Your own goods go in first: they sell for 512 g\/min less until they're in, and then the levels pay/.test(yard), "and the Yard's list", yard.slice(0, 300));
await shot("1-note");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// The Move key (owner, 2026-10-30: "Can you add a hotkey for move?"): E with one of your buildings selected picks it up
// as its panel's Move does, E again puts it back; nothing selected, or someone else's: nothing. node move-key.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/move-key"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const tool = () => page.evaluate(() => tp.appStore.getState().tool);

await newPlayer(page, "mk");
await cheat("gold");
await kit(page, ["windMoily"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.mouse.click(5, 500);
await page.keyboard.press("e");
check((await tool()).kind === "select", "nothing selected: E does nothing", JSON.stringify(await tool()));

const id = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  tp.gameClient.showBuilding(b.id);
  return b.id;
});
await page.waitForSelector(".building-panel", { timeout: 15000 });
await page.waitForTimeout(1500);
check(/Move\s*E/.test(await page.locator(".building-panel .footer-row").innerText()), "the panel's Move says its key");
await page.keyboard.press("e");
await page.waitForTimeout(300);
const moving = await tool();
check(moving.kind === "move" && moving.building === id, "E picks the selected building up", JSON.stringify(moving));
await shot("1-moving");
await page.keyboard.press("e");
await page.waitForTimeout(300);
check((await tool()).kind === "select", "E again puts it back", JSON.stringify(await tool()));

// Someone else's building: nothing
const other = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner !== co.id && t.owner !== null && t.type !== "hq");
  if (b) tp.gameClient.showBuilding(b.id);
  return b?.id ?? null;
});
if (other !== null) {
  await page.waitForTimeout(1500);
  await page.keyboard.press("e");
  check((await tool()).kind === "select", "someone else's building: E does nothing", JSON.stringify(await tool()));
}
await page.keyboard.press("?");
await page.waitForTimeout(300);
check(/move the selected building/.test(await page.locator(".keys-help").innerText()), "the keys list has it");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

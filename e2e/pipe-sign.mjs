// A pipe on its way to a better one (playtest: "when you upgrade pipes, can we show some kind of thing so that it's clear
// it's being upgraded?"): its tiles glow in the new pipe's colour with an arrow, the hover says what it waits for, and
// the Waits line in the corner offers Buy all the rest when more than one thing waits.   node pipe-sign.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/pipe-sign"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "ps");
for (const c of ["level", "level", "level", "gold", "eratech"]) await cheat(c);
await kit(page, ["windMoily", "solarPanel"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// Its pipes asked to become copper with no ingots near, and a level that waits for goods
const tile = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const co = () => tp.appStore.getState().company;
  await c.setWantsFromMarket(false);
  const tiles = [...w.conductors.values()].filter((t) => t.owner === co().id && t.kind === "pipe" && t.tier === 1);
  await c.upgradePipes(tiles.flatMap((t) => [t.x, t.y]), 2);
  const panel = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "solarPanel");
  await c.upgrade(panel.id, 11 - panel.level);
  await tp.gameClient.refreshCompany();
  const want = co().wants.find((t) => t.pipeAt != null);
  return want ? { x: want.pipeAt[0], y: want.pipeAt[1], tier: want.pipeTier, at: want.pipeAt } : null;
});
console.log("waiting tile:", JSON.stringify(tile));
check(tile !== null && tile.tier === 2, "the pipe want says its tiles and the tier they become", JSON.stringify(tile));
if (tile !== null) {
  await page.evaluate((t) => tp.gameClient.engine.lookAt(t.x, t.y, 0.8), tile);
  await page.waitForTimeout(2000);
  // Over each of its tiles in turn, until the hover shows (a tile can sit under a panel)
  let hover = [];
  for (let i = 0; i < tile.at.length && !hover.some((t) => t.length > 0); i += 2) {
    const at = await page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [tile.at[i], tile.at[i + 1]]);
    await page.mouse.move(at.x, at.y);
    await page.waitForTimeout(800);
    hover = await page.locator(".hover-tip").allInnerTexts();
  }
  console.log("hover:", hover.join(" | "));
  check(hover.some((t) => /Becomes a Copper Pipe once its goods are in: .*copper ingot/.test(t)), "the hover says it waits for its copper ingots", hover.join(" | "));
  await shot("1-pipe-waits");
}
const all = page.locator(".goal-buy-all");
check((await all.count()) === 1, "the corner's Waits line offers Buy all the rest");
if ((await all.count()) === 1) {
  await all.click();
  await page.waitForTimeout(2500);
  const left = await page.evaluate(() => tp.appStore.getState().company.wants.length);
  check(left === 0, "and it buys everything", String(left));
  await shot("2-all-bought");
}
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

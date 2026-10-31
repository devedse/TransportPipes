// The prices shown are the gold taken, also with the Workshop wonder's discount (its perk makes buildings, levels and
// parcels 3% cheaper a stage; the Build menu went on showing the full price): pays a Workshop stage, reads the Wind
// Moily's price in the Build menu, builds one and compares the gold that went.
//   node discount-prices.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/discount-prices"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "dp");
for (let i = 0; i < 6; i++) await cheat("level");
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const paid = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const chose = await c.chooseMonument(1, "workshop");
  const pay = await c.payMonument(1, 100_000_000);
  return { chose: chose.error ?? "ok", pay: pay.error ?? "ok" };
});
console.log(JSON.stringify(paid));
await page.waitForFunction(() => (tp.appStore.getState().company?.costFactor ?? 1) < 1, null, { timeout: 15000 }).catch(() => {});
const factor = await page.evaluate(() => tp.appStore.getState().company.costFactor);
check(factor !== undefined && factor < 1, "the Workshop's discount reaches the browser", `costFactor ${factor} (${JSON.stringify(paid)})`);

await page.evaluate(() => tp.appStore.setState({ buildMenuOpen: true }));
await page.waitForSelector(".build-list", { timeout: 10000 });
const item = page.locator(".build-item", { hasText: "Wind Moily" }).first();
await item.scrollIntoViewIfNeeded();
const shown = Number((await item.locator(".build-cost .gold-text").innerText()).replace(/[^0-9.]/g, ""));
await shot("1-menu");
await page.keyboard.press("Escape");
const taken = await page.evaluate(async () => {
  const s = () => tp.appStore.getState();
  const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const definition = s().content.building("windMoily");
  for (let r = 2; r < 18; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++)
        if (tp.rules.canPlace(s().content, w, s().company, s().era, definition, post.x + dx, post.y + dy).ok) {
          const before = s().company.gold;
          const placed = await tp.gameClient.connection.placeBuilding("windMoily", post.x + dx, post.y + dy);
          if (!placed.success) return { error: placed.error };
          for (let i = 0; i < 50 && s().company.gold === before; i++) await new Promise((r) => setTimeout(r, 100));
          return { gold: before - s().company.gold };
        }
  return { error: "no spot" };
});
console.log(JSON.stringify({ factor, shown, taken }));
// The gold may move by what the estate earns in the moment between: within a gold a second of income
check(taken.gold !== undefined && Math.abs(taken.gold - shown) <= 1, "the Build menu's price is the gold taken", `shown ${shown} g, took ${JSON.stringify(taken)}`);
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

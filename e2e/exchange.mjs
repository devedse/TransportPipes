// The Exchange honest (after run 4: a typed price silently became the one player price, an order expired without a
// word, and a sell order was refused for goods that weren't stored). 1.4: the Exchange comes with Company Level 12 (no
// office), at a price of your own between the market's two: node exchange.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/exchange"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "x");
for (const c of ["gold", "gold", "level", "level", "level"]) await cheat(c);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
let failed = 0;
const check = (ok, what, detail = "") => {
  if (!ok) failed++;
  console.log(`${ok ? "ok  " : "FAIL"} ${what}${detail ? `  (${detail})` : ""}`);
};

// A Storage near the trading post
const built = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const co = () => s().company; const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const out = [];
  for (const type of ["storage"]) {
    const def = s().content.building(type);
    let done = false;
    for (let r = 2; r < 12 && !done; r++)
      for (let dy = -r; dy <= r && !done; dy++)
        for (let dx = -r; dx <= r && !done; dx++)
          if (tp.rules.canPlace(s().content, w, co(), s().era, def, post.x + dx, post.y + dy).ok) {
            const result = await tp.gameClient.connection.placeBuilding(type, post.x + dx, post.y + dy);
            out.push(`${type}: ${result.success ? "placed" : result.error}`);
            done = true;
          }
  }
  await tp.gameClient.connection.cheat("finish");
  return out;
});
console.log(built.join(" · "));
await page.waitForTimeout(1500);
await page.evaluate(() => tp.gameClient.openHub("exchange"));
await page.waitForSelector(".exchange-form", { timeout: 15000 });
await page.waitForTimeout(1000);

// Selling what isn't stored: said before the click, the button waits
await page.selectOption(".exchange-form select >> nth=0", "sell");
await page.selectOption(".exchange-form select >> nth=1", "stone");
const terms = await page.locator(".exchange-terms").innerText();
check(/^[\d.]+ g each \(between [\d.]+ and [\d.]+\)/.test(terms), "your own price, between the market's two, is said before the click", terms);
check(/in your Storages/.test(terms) && (await page.locator(".exchange-form button[type=submit]").isDisabled()), "selling what isn't stored: it says so, and Place waits");
await shot("1-sell-terms");

// Buying at a price typed: placed at that price, with what it holds and how long it waits
await page.selectOption(".exchange-form select >> nth=0", "buy");
await page.fill(".exchange-form input[type=number] >> nth=0", "3000");
await page.fill(".exchange-form input[type=number] >> nth=1", "2");
check(/^2\.00 g each/.test(await page.locator(".exchange-terms").innerText()), "a typed price stays what was typed");
await page.click(".exchange-form button[type=submit]");
await page.waitForTimeout(1200);
const toasts = await page.locator(".toast").allInnerTexts();
check(toasts.some((t) => /waits up to 7 days for a seller/.test(t)), "placing says what it holds and how long it waits", toasts.join(" | "));
const mine = await page.locator(".exchange .contract").first().innerText().catch(() => "");
check(/closes in/.test(mine), "your order says when it closes and what comes back", mine.replace(/\s+/g, " "));
await shot("2-order");

console.log(errors.length ? `FAIL errors: ${errors.join("\n")}` : failed > 0 ? `FAIL ${failed} checks` : "PASS errors: none");
await browser.close();

// What levels from 11 take, everywhere levels are bought (the owner, 2026-10-31: "when you upgrade a building above
// level 10 it should show 1100g, 100 wood, 3838 iron ingot and when you hover over it it should explain it clearly in a
// little popup"; "also in this menu", the selection; "next to the gold ... the buildings waiting for resources and a
// clear 1 button click"; "what we are paying currently for buying resources from the hub"; "a timer in the top for
// that 1 minute yard update"): a Wind Moily at level 10, its Level 11 button with its goods and their popup; two
// selected with theirs; after buying, the top bar's waiting goods with Buy all the rest, the gold panel's market lines,
// the Yard clock, and the Trading Post's bars saying sold and Hub Order.   node level-goods.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/level-goods"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const closeDialogs = async () => { for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); } };

await newPlayer(page, "lg");
for (const c of ["gold", "gold", "level", "level"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
await closeDialogs();
console.log(await kit(page, ["windMoily", "windMoily", "windMoily"]));
await page.evaluate(() => tp.gameClient.connection.setWantsFromMarket(true));
const ids = await page.evaluate(async () => {
  const s = tp.appStore.getState();
  const mine = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === s.company.id && t.type === "windMoily");
  for (const b of mine) await tp.gameClient.connection.upgrade(b.id, 10 - b.level);
  await new Promise((r) => setTimeout(r, 1500));
  return mine.map((t) => t.id);
});
check(ids.length >= 3, "three Wind Moilys at level 10", String(ids.length));

// The building panel: Level 11 with its goods, the popup on hover
await page.evaluate((id) => tp.gameClient.showBuilding(id), ids[0]);
await page.waitForSelector(".building-panel .upgrade button.primary", { timeout: 15000 });
await page.waitForTimeout(1500);
const level = page.locator(".building-panel .upgrade button.primary").first();
const levelText = (await level.innerText()).replace(/\s+/g, " ");
console.log("Level button:", levelText);
check(/Level 11/.test(levelText) && (await level.locator(".level-goods .resource-icon, .level-goods .flow-dot").count()) > 0, "Level 11 shows its gold and its goods with icons", levelText);
await level.hover();
await page.waitForTimeout(400);
const tip = page.locator(".goods-tip");
const tipText = (await tip.count()) > 0 ? (await tip.innerText()).replace(/\s+/g, " ") : "";
console.log("popup:", tipText.slice(0, 300));
check(/From level 11/.test(tipText) && /market/.test(tipText) && /g more/.test(tipText), "its popup says what the goods are and how they come", tipText.slice(0, 200));
await shot("1-level-11");
await page.mouse.move(10, 500);

// Two selected: All +1 with the goods of both
await page.evaluate((ids) => tp.appStore.setState({ selection: ids.slice(1, 3) }), ids);
await page.waitForSelector(".selection-panel .upgrade button.primary", { timeout: 15000 });
await page.waitForTimeout(1500);
const all = (await page.locator(".selection-panel .upgrade button.primary").innerText()).replace(/\s+/g, " ");
const spend = (await page.locator(".selection-panel .budget button").innerText()).replace(/\s+/g, " ");
console.log("All +1:", all, "| Spend:", spend);
check(/All \+1/.test(all) && (await page.locator(".selection-panel .upgrade button.primary .level-goods").count()) > 0, "the selection's All +1 shows the goods", all);
check(/levels for/.test(spend), "Spend says the levels it buys", spend);
await shot("2-selection");
await page.evaluate(() => tp.appStore.setState({ selection: [] }));

// Bought: the top bar says what waits, Buy all the rest, and the Yard's clock
await page.evaluate((ids) => Promise.all(ids.map((id) => tp.gameClient.connection.upgrade(id, 1))), ids);
await page.waitForTimeout(2500);
const waits = page.locator(".top-waits");
check((await waits.count()) === 1, "the top bar: what waits for goods");
await waits.hover();
await page.waitForTimeout(400);
const panelText = (await page.locator(".top-waits-panel").innerText().catch(() => "")).replace(/\s+/g, " ");
console.log("waits:", panelText.slice(0, 300));
check(/Wind Moily → level 11/.test(panelText) && /Buy all the rest/.test(panelText), "its list, each with its goods, and Buy all the rest", panelText.slice(0, 200));
const clock = (await page.locator(".top-yard").innerText().catch(() => "")).replace(/\s+/g, " ");
console.log("clock:", clock);
check(/Yard round/.test(clock) && /(\d+ s|min|now)/.test(clock), "the Yard's clock counts down", clock);
await shot("3-waiting");

// The gold panel: what the market brings for them a minute
await page.mouse.move(10, 500);
await page.click(".top-gold");
await page.waitForSelector(".gold-uses .income", { timeout: 15000 });
await page.waitForTimeout(1500);
const gold = (await page.locator(".gold-uses").innerText()).replace(/\s+/g, " ");
check(/Goods the market brings for your levels/.test(gold) && /Paid a minute now/.test(gold), "the gold panel: what the market brings for the levels, a minute", gold.slice(0, 200));
await page.locator(".gold-uses p", { hasText: "Goods the market brings" }).scrollIntoViewIfNeeded().catch(() => {});
await shot("4-gold");
await page.click(".top-gold");

// Buy all the rest: nothing waits
await waits.hover();
await page.waitForTimeout(300);
await page.locator(".top-waits-panel button.primary", { hasText: "Buy all the rest" }).click();
await page.waitForTimeout(2500);
check((await page.locator(".top-waits").count()) === 0, "Buy all the rest: nothing waits any more");

// The Trading Post: sold and the Hub Order, said
await page.mouse.move(10, 500);
const post = await page.evaluate(() => [...tp.gameClient.world.buildings.values()].find((t) => t.owner === tp.appStore.getState().company.id && t.type === "tradingPost").id);
await page.evaluate((id) => tp.gameClient.showBuilding(id), post);
await page.waitForSelector(".post-sales", { timeout: 15000 });
await page.waitForTimeout(1500);
const sales = (await page.locator(".post-sales").innerText()).replace(/\s+/g, " ");
console.log("sales:", sales.slice(0, 300));
check(/Hub Order \d/.test(sales) && /less for all of it/.test(sales), "the Trading Post: what it sells against its Hub Order, and how the price falls", sales.slice(0, 200));
await shot("5-post");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

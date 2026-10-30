// A level's goods bought one good at a time (playtest: the planks' "Buy the rest · 302 g" bought the bricks as well, for
// 609 g, and finished the level): each good's button buys that good, at its price; Buy all the rest buys the rest.
//   node buy-rest.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/buy-rest"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "br");
for (const c of ["level", "level", "level", "gold"]) await cheat(c);
await kit(page, ["windMoily"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// The market brings nothing by itself: the goods wait for the buttons
const id = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const co = tp.appStore.getState().company;
  await c.setWantsFromMarket(false);
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  await c.upgrade(b.id, 11 - b.level);
  tp.gameClient.showBuilding(b.id);
  return b.id;
});
await page.waitForSelector(".building-panel .level-wait", { timeout: 15000 });
await page.waitForTimeout(1500);
const wants = () => page.evaluate(() => tp.appStore.getState().buildingDetails.levelWait?.wants.map((t) => ({ resource: t.resource, left: t.amount - t.got, cost: t.orderCost })) ?? []);
const before = await wants();
console.log("wants:", JSON.stringify(before));
check(before.length >= 2, "the level waits for two goods", JSON.stringify(before));
await shot("1-waits");

const gold = await page.evaluate(() => tp.appStore.getState().company.gold);
await page.locator(".building-panel .level-wait .site-list li").first().locator("button", { hasText: "Buy the rest" }).click();
await page.waitForTimeout(2000);
const after = await wants();
const paid = gold - (await page.evaluate(() => tp.appStore.getState().company.gold));
console.log("after:", JSON.stringify(after), "paid", paid.toFixed(0));
const first = after.find((t) => t.resource === before[0].resource);
const second = after.find((t) => t.resource === before[1].resource);
check(first === undefined || first.left < 0.5, "the first good is in", JSON.stringify(first));
check(second !== undefined && second.left > 0.5, "the second still waits", JSON.stringify(second));
check(Math.abs(paid - before[0].cost) <= before[0].cost * 0.05 + 2, "it cost that good's price", `${paid.toFixed(0)} for ${before[0].cost}`);
const level = await page.evaluate((id) => tp.gameClient.world.buildings.get(id)?.level, id);
check(level === 10, "the level waits for the rest", String(level));
await shot("2-one-bought");

check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

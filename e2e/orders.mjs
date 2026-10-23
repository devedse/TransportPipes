// Hub orders as buyers (after run 3, the design team's step 7): the Hub's Daily orders tab has no Deliver and no
// Storage; each order says what the trading posts sell of its good and when it is full; the goal tracker and the
// toolbar don't ask for them (they show once filled).
//   node orders.mjs shots/orders
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/orders";
const { browser, page, errors, shot } = await start(out);
await newPlayer(page, "orders");
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

const orders = await page.evaluate(() => tp.appStore.getState().company.dailyOrders);
console.log("orders:", JSON.stringify(orders.map((t) => `${t.amount} ${t.resource} · sells ${t.selling}/min`)));
const goods = new Set(orders.map((t) => t.resource));
expect("an order a good the trading post sells", orders.length >= 1 && goods.size === orders.length && orders.every((t) => t.selling > 0), JSON.stringify(orders.map((t) => t.resource)));
const today = await page.locator(".goal-tracker li", { hasText: "Today" }).count();
expect("the tracker doesn't ask for them", today === 0);
const badge = await page.locator(".toolbar button", { hasText: "Hub" }).locator(".badge").count();
expect("nor the toolbar", badge === 0);
await shot("1-start");

await page.evaluate(() => tp.gameClient.openHub("orders"));
await page.waitForSelector(".orders .order", { timeout: 15000 });
await page.waitForTimeout(1000);
const tab = await page.locator(".orders").innerText();
expect("no Deliver, no Storage", !/Deliver\b|Build a Storage/.test(tab.replace("nothing to\ndeliver", "")), tab.replace(/\s+/g, " ").slice(0, 200));
expect("each says what the posts sell and when it is full", (tab.match(/Your trading posts sell [\d.,k]+ a minute: full in about/g) ?? []).length === orders.length, tab.replace(/\s+/g, " ").slice(0, 400));
await shot("2-orders");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

// Hub orders as buyers (after run 3, the design team's step 7; the playtest of 2026-10-30): one a good the trading
// posts sell, up to three, a good that starts selling during the day gets its own; each card says how far it is, what
// the posts sell and when it is full, what it pays over the hub; Deliver fills it from the Storages, only when they hold
// the good; Loady's order step says how far today's order is; the tracker and the toolbar don't ask for them.
//   node orders.mjs shots/orders
import { start, newPlayer, kit } from "./lib.mjs";

const out = process.argv[2] ?? "shots/orders";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "orders");
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};
const orders = () => page.evaluate(() => tp.appStore.getState().company.dailyOrders);
const first = await orders();
expect("a new plot gets its first order at once", first.length === 1, JSON.stringify(first.map((t) => t.resource)));

// 1.4: a plot starts empty: a small estate that sells energy, ore and ingots
console.log("kit:", JSON.stringify(await kit(page)));
// And an Iron Mine on the plot's ore (from Level 2), piped to the post: ore sells too
await cheat("level");
await cheat("level");
console.log("mine:", await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  const def = s.content.building("ironMine");
  const spots = [];
  for (let y = co.estateY; y < co.estateY + 32; y++) for (let x = co.estateX; x < co.estateX + 32; x++)
    if (tp.rules.canPlace(s.content, w, co, s.era, def, x, y).ok) spots.push({ x, y, d: Math.abs(x - post.x) + Math.abs(y - post.y) });
  spots.sort((a, b) => a.d - b.d);
  for (const spot of spots.slice(0, 10)) {
    if (!(await c.placeBuilding("ironMine", spot.x, spot.y)).success) continue;
    await c.cheat("finish");
    const sides = (x, y, size) => [[-1, 0], [size, 0], [0, -1], [0, size]].map(([dx, dy]) => ({ x: x + dx, y: y + dy }));
    for (const from of sides(spot.x, spot.y, def.size)) for (const to of sides(post.x, post.y, post.size)) {
      const route = tp.rules.routePipe(w, co, from, to, s.content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) return `ironMine@${spot.x},${spot.y} piped`;
    }
    return `ironMine@${spot.x},${spot.y} (no pipe)`;
  }
  return "no ore";
}));
// Iron ore sells too: its order comes during the day
for (let i = 0; i < 40 && (await orders()).length < 2; i++) await page.waitForTimeout(1000);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const today = await orders();
console.log("orders:", JSON.stringify(today.map((t) => `${t.amount} ${t.resource} · sells ${t.selling}/min`)));
const goods = new Set(today.map((t) => t.resource));
expect("one order a good", goods.size === today.length);
expect("the ore that started selling today got its own", today.some((t) => t.resource === "ironOre" && t.id !== first[0].id) && today.length <= 3, JSON.stringify(today.map((t) => t.resource)));
const tracked = await page.locator(".goal-tracker li", { hasText: "Today" }).count();
expect("the tracker doesn't ask for them", tracked === 0);
const badge = await page.locator(".toolbar button", { hasText: "Hub" }).locator(".badge").count();
expect("nor the toolbar", badge === 0);
await shot("1-start");

await page.evaluate(() => tp.gameClient.openHub("orders"));
await page.waitForSelector(".orders .order", { timeout: 15000 });
await page.waitForTimeout(1000);
const tab = await page.locator(".orders").innerText();
const flat = tab.replace(/\s+/g, " ");
expect("each says how far it is", (flat.match(/[\d.,k]+ \/ [\d.,k]+ delivered/g) ?? []).length === today.length, flat.slice(0, 300));
expect("and what it pays over the hub", (flat.match(/more than the hub would/g) ?? []).length === today.length, flat.slice(0, 300));
expect("each says what the posts sell and when it is full", (flat.match(/Your trading posts sell [\d.,k]+ a minute: full in about/g) ?? []).length === today.filter((t) => t.selling > 0).length, flat.slice(0, 500));
expect("no Deliver while the Storages hold none of it", (await page.locator(".order-deliver button").count()) === today.filter((t) => t.stored >= 1).length);
const track = await page.locator(".order-bar").first().evaluate((e) => getComputedStyle(e).backgroundColor);
expect("the bar's empty part shows", track !== "rgba(0, 0, 0, 0)" && !/, 0\.1\)$/.test(track), track);
await shot("2-orders");

// Goods in the Storage: Deliver fills an order at once, at its price
await cheat("stock");
for (let i = 0; i < 10 && (await page.locator(".order-deliver button").count()) === 0; i++) await page.waitForTimeout(1000);
const deliverable = (await orders()).find((t) => t.stored >= 1 && t.delivered < t.amount);
expect("Deliver shows for a good the Storages hold", deliverable !== undefined && (await page.locator(".order-deliver button").count()) >= 1, JSON.stringify(await orders()));
if (deliverable) {
  await shot("3-deliver");
  const gold = await page.evaluate(() => tp.appStore.getState().company.gold);
  const card = page.locator(".order", { hasText: new RegExp(`${deliverable.amount.toLocaleString("en-US")}`) }).filter({ has: page.locator(".order-deliver button") }).first();
  await card.locator(".order-deliver button").click();
  await page.waitForTimeout(2000);
  const after = (await orders()).find((t) => t.id === deliverable.id);
  const paid = (await page.evaluate(() => tp.appStore.getState().company.gold)) - gold;
  const units = after.delivered - deliverable.delivered;
  expect("it filled from the Storage", units >= Math.min(deliverable.stored, deliverable.amount - deliverable.delivered) - 1, `${deliverable.delivered} → ${after.delivered}`);
  expect("and paid the order's price at once", paid >= units * (deliverable.rewardGold / deliverable.amount) * 0.95, `+${paid.toFixed(0)} g for ${units}`);
  await shot("4-delivered");
}
await page.keyboard.press("Escape");

// Loady's order step says how far today's order is
for (let i = 0; i < 12; i++) {
  const step = await page.evaluate(() => tp.appStore.getState().company.tutorial?.id);
  if (step === "firstOrder" || step == null) break;
  await page.evaluate(() => tp.gameClient.connection.skipTutorial());
  await page.waitForTimeout(1200);
}
const step = await page.evaluate(() => tp.appStore.getState().company.tutorial);
if (step?.id === "firstOrder") {
  expect("Loady says up to three, one a good", /up to three orders a day, one for each good/.test(step.text), step.text);
  expect("and how far today's order is", /order: [\d,.]+ of [\d,.]+ delivered/.test(step.tip ?? ""), step.tip ?? "no tip");
  await shot("5-loady");
} else {
  console.log("(the order step was done already:", JSON.stringify(step), ")");
}

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

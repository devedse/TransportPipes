// The Era Wonder as the Capital's want (after run 3, the design team's step 5, and run 4's fixes): a Capital player's goal
// tracker has an "Era" row with the Wonder's stage and what it wants; it opens the Wonders tab (no District Wonder beside
// it in the Capital), where each good can be ordered by the hour or all of it (all, since nobody in the Capital makes it
// yet), who ordered shows, and once it's all ordered a line says so instead of a vanished button; the chain line says
// which building would make it.
//   node era-wonder.mjs shots/era-wonder
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/era-wonder";
const base = process.env.BASE_URL ?? "http://localhost:5173";
const { browser, page, errors, shot, cheat } = await start(out);
for (let i = 0; i < 40; i++) {
  try {
    await page.goto(`${base}/`);
    await page.waitForSelector(".auth-panel", { timeout: 15000 });
    break;
  } catch {
    await page.waitForTimeout(1000);
  }
}
await page.click("text=New player");
await page.fill("input[autocomplete=username]", `era${Date.now() % 100000}`);
await page.fill("input[type=password]", "secret123");
await page.click("button[type=submit]");
await page.waitForSelector(".picker-top", { timeout: 30000 });
// A free seat in the Capital (its Era Wonder is the Capital's want; on a fresh world the first players live there)
const seat = await page.evaluate(async () => {
  const stations = await tp.gameClient.connection.getStations(0, 0);
  for (const station of stations.stations.filter((t) => t.open)) {
    const free = station.seats.find((t) => t.state === "free");
    if (free) return { districtX: 0, districtY: 0, slot: free.slot };
  }
  return null;
});
await page.evaluate((s) => tp.gameClient.foundCompany(`Era ${Date.now() % 100000} Works`, "#8a5cd0", s, null), seat);
await page.waitForSelector(".top-bar", { timeout: 30000 });
await page.waitForTimeout(3000);
await cheat("gold");
// The stage waits for midnight on a fresh world: open it now
await cheat("wonderopen");
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(1500);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

const co = await page.evaluate(() => {
  const c = tp.appStore.getState().company;
  return { capital: c.districtX === 0 && c.districtY === 0, line: c.eraWonderLine?.text ?? null };
});
console.log("company:", JSON.stringify(co));
expect("a Capital player", co.capital);
const row = await page.locator(".goal-tracker li", { hasText: "🏛" }).innerText().catch(() => "");
expect("the tracker has an Era row with the Wonder", /🏛 The Great Dynamo .*% · .* left · (wants|your)/.test(row), row.replace(/\s+/g, " "));
await shot("1-tracker");

await page.locator(".goal-tracker li", { hasText: "🏛" }).locator("button.goal-text").click();
await page.waitForSelector(".wonder-card", { timeout: 15000 });
await page.waitForTimeout(1500);
const era = page.locator(".wonder-card", { hasText: "Era 1 Wonder" });
expect("the Era card has no Deliver button", (await era.locator("button", { hasText: "Deliver from storage" }).count()) === 0);
expect("no District Wonder beside it in the Capital", (await page.locator(".wonder-card", { hasText: "District Wonder" }).count()) === 0);
const need = era.locator(".wonder-need").first();
const all = need.locator(".wonder-order-buttons button").last();
const label = await all.innerText().catch(() => "");
expect("each good can be ordered, all of it since nobody here makes it", /^Order (all )?[\d.,k]+ · [\d.,kM]+ g$/.test(label), label);
const chain = await need.locator(".wonder-chain").innerText().catch(() => "");
expect("the chain line says what would make it", /^A .+ \([\d.,k]+ g\) makes /.test(chain), chain);
await shot("2-wonders");

const hour = need.locator(".wonder-order-buttons button", { hasText: "Order 1 h" });
if ((await hour.count()) > 0) {
  await hour.click();
  await page.waitForTimeout(2500);
  const ordered = await need.locator(".wonder-order span", { hasText: "Ordered:" }).innerText().catch(() => "");
  expect("an hour's order shows who ordered", /^Ordered: you [\d.,k]+/.test(ordered), ordered);
}
const before = await page.evaluate(() => tp.appStore.getState().company.gold);
await need.locator(".wonder-order-buttons button").last().click();
await page.waitForTimeout(2500);
const after = await page.evaluate(() => tp.appStore.getState().company.gold);
const why = await need.locator(".wonder-why").innerText().catch(() => "");
expect("ordering pays the hub, and a line says it is all ordered instead of a vanished button", after < before && /All of it is ordered/.test(why), `${before} → ${after}: ${why}`);
const onItsWay = await need.locator(".wonder-order span", { hasText: "on its way" }).count();
expect("the order shows on its way", onItsWay > 0);
await shot("3-ordered");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

// The Era Wonder as the Capital's want (after run 3, the design team's step 5): a Capital player's goal tracker has an
// "Era" row with the Wonder's stage and what it wants; it opens the Wonders tab, where each good can be ordered (half of
// it at most, at 2 × base) and what arrives by pipe shows as "/min coming".
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
await page.waitForSelector(".found-panel", { timeout: 30000 });
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
const order = era.locator(".wonder-order button").first();
const label = await order.innerText().catch(() => "");
expect("each good can be ordered", /^Order [\d.,k]+ · [\d.,kM]+ g$/.test(label), label);
await shot("2-wonders");

const before = await page.evaluate(() => tp.appStore.getState().company.gold);
await order.click();
await page.waitForTimeout(2500);
const after = await page.evaluate(() => ({
  gold: tp.appStore.getState().company.gold,
  buttons: [...document.querySelectorAll(".wonder-card .wonder-order button")].map((t) => t.textContent),
  toast: document.querySelector(".toast")?.textContent ?? null,
  onItsWay: [...document.querySelectorAll(".wonder-card .wonder-order span")].map((t) => t.textContent).find((t) => /ordered, on its way/.test(t)) ?? null,
}));
expect("ordering pays the hub and that good's offer is gone", after.gold < before && !after.buttons.includes(label), JSON.stringify(after));
expect("the order shows on its way", after.onItsWay !== null, after.onItsWay ?? "");
await shot("3-ordered");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

// Your gold can (after run 4: the players spent 0.2% of what they earned and banked 24–29 days of income): the top bar's
// gold reads "· N days ▸"; tapping it lists, shared first, the town's Era Wonder's stage to buy and give, and "Grow
// everything that pays" with its parts; Grow buys them in one tap and says so (no Undo: the owner, 2026-10-31). (1.4: a plot
// starts empty, so two Wind Moilys on the trading post earn first.)
//   node gold.mjs shots/gold
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/gold";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "gold");
for (const c of ["gold", "earn", "earn", "earn", "level", "level"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const moily = s().content.building("windMoily");
  let placed = 0;
  for (let r = 3; r < 9 && placed < 2; r++)
    for (let dy = -r; dy <= r && placed < 2; dy++)
      for (let dx = -r; dx <= r && placed < 2; dx++) {
        const x = post.x + dx;
        const y = post.y + dy;
        if (!tp.rules.canPlace(s().content, w, s().company, s().era, moily, x, y).ok || !(await c.placeBuilding("windMoily", x, y)).success) continue;
        await c.cheat("finish");
        for (const [fx, fy] of [[x, y - 1], [x, y + 1], [x - 1, y], [x + 1, y]]) {
          const route = tp.rules.routePipe(w, s().company, { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
          if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) break;
        }
        placed++;
      }
});
await page.waitForTimeout(4000);
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(3000);

// The first sales take a market round to count
for (let i = 0; i < 20 && !(await page.locator(".top-gold").innerText()).includes("days"); i++) await page.waitForTimeout(1000);
const label = await page.locator(".top-gold").innerText();
expect("the top bar's gold reads in days of income", /· [\d.]+ days ▸/.test(label), label.replace(/\s+/g, " "));
await page.click(".top-gold");
await page.waitForSelector(".gold-uses .gold-use", { timeout: 20000 }).catch(() => null);
await page.waitForTimeout(1500);
const list = await page.locator(".gold-uses").innerText().catch(() => "");
console.log(list.replace(/\n+/g, " | "));
const wonderAt = list.search(/🏛 The .+.s stage \d wants goods: buy and give them/);
expect("it lists the town's Era Wonder first", wonderAt >= 0 && wonderAt < list.indexOf("Grow everything that pays"), "");
expect("and Grow everything that pays, with its price and gain", /Grow everything that pays: about \+[\d.,k]+ g\/min for [\d.,kM]+ g/.test(list));
await shot("1-list");

const grow = page.locator(".gold-uses button", { hasText: /^Grow · / });
if ((await grow.count()) > 0) {
  await grow.click();
  await page.waitForTimeout(3000);
}
const after = await page.evaluate(() => ({
  gold: tp.appStore.getState().company.gold,
  toast: [...document.querySelectorAll(".toast")].map((t) => t.textContent).join(" | "),
}));
// (not the gold: achievements may pay out at the same moment)
expect("one tap grew them and said so", /Grown: .+ for [\d.,kM]+ g/.test(after.toast), after.toast);
const undo = await page.locator(".gold-uses button", { hasText: "Undo the last Grow" }).count();
expect("no Undo (the owner: \"let's remove it for all for now\")", undo === 0);
await shot("2-grown");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

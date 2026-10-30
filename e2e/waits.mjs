// A building that waits says so (after run 4: Dev's Big Factory and Ana's Cement Kiln read "is being built" and waited
// to the end for bricks nobody near made): placing a Market Hall without bricks says "stands as a scaffold. It waits for
// 500 bricks…"; the goal tracker has a Waits row with Buy the rest; its panel names the chain that would make bricks,
// with Show me, which puts that building in hand.
//   node waits.mjs shots/waits
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/waits";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "waits");
for (const c of ["gold", "earn", "earn", "level", "level", "level", "level"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

// A Market Hall where it fits, by the client's own placing (its toast)
const spot = await page.evaluate(async () => {
  await tp.gameClient.connection.skipTutorial();
  const s = tp.appStore.getState();
  const w = tp.gameClient.world;
  const co = s.company;
  const def = s.content.building("marketHall");
  for (let y = co.estateY + 4; y < co.estateY + 28; y++)
    for (let x = co.estateX + 4; x < co.estateX + 28; x++)
      if (tp.rules.canPlace(s.content, w, co, s.era, def, x, y).ok) return { x, y };
  return null;
});
expect("a spot for a Market Hall", spot !== null);
const toast = await page.evaluate(async (p) => {
  tp.gameClient.setTool({ kind: "build", type: "marketHall" });
  // The path a click on the map takes
  await tp.gameClient.placeBuilding("marketHall", p.x, p.y);
  await new Promise((r) => setTimeout(r, 800));
  return [...document.querySelectorAll(".toast")].map((t) => t.textContent).join(" | ");
}, spot);
expect("the toast says it stands as a scaffold and waits for its bricks", /Market Hall stands as a scaffold\. It waits for 500 bricks/.test(toast), toast);
await page.keyboard.press("Escape");
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(2000);
const row = await page.locator(".goal-tracker li", { hasText: "Your Market Hall waits" }).first().innerText().catch(() => "");
// (1.4: the market brings a little meanwhile, and the button is Buy the rest)
expect("the tracker has a Waits row with Buy the rest", /🏗 Your Market Hall waits for \d+ bricks/.test(row) && /Buy the rest · [\d.,k]+ g/.test(row), row.replace(/\s+/g, " "));
await shot("1-waits");

const site = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  return [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "marketHall")?.id ?? null;
});
await page.evaluate((id) => tp.gameClient.showBuilding(id), site);
await page.waitForSelector(".site-wants", { timeout: 15000 });
await page.waitForTimeout(1500);
const chain = await page.locator(".site-wants .site-list li").first().innerText().catch(() => "");
expect("its panel names the chain that would make bricks", /A Brickworks \([\d.,k]+ g\) makes bricks from coal and stone/.test(chain), chain.replace(/\s+/g, " "));
await shot("2-panel");
const show = page.locator(".site-wants button", { hasText: "Show me" });
if ((await show.count()) > 0) {
  await show.first().click();
  await page.waitForTimeout(1000);
  const tool = await page.evaluate(() => tp.appStore.getState().tool);
  expect("Show me puts the Brickworks in hand", tool.kind === "build" && tool.type === "brickworks", JSON.stringify(tool));
} else {
  expect("Show me is there when you may build it", false, "no button");
}

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

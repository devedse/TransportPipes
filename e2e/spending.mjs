// Spending is safe (after run 3, the design team's step 1): a building's panel says what the next level and Max will do
// to your income before the click (the price in gold only: the owner, 2026-10-30); no Undo after buying (the owner, 2026-10-31).
//   node spending.mjs shots/spending
import { start, newPlayer, kit } from "./lib.mjs";

const out = process.argv[2] ?? "shots/spending";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "spend");
await cheat("level");
await cheat("gold");
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.waitForTimeout(1500);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}

// A kit building that sells: its panel
const id = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  // A producer: a Storage's level has no gold preview
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily" && t.constructionEndsAt === null);
  tp.gameClient.showBuilding(b.id);
  return b.id;
});
await page.waitForSelector(".building-panel .upgrade", { timeout: 15000 });
await page.waitForTimeout(2500);
const before = await page.evaluate(() => ({
  preview: document.querySelector(".building-panel .spend-preview")?.textContent ?? null,
  button: document.querySelector(".building-panel .upgrade button.primary")?.textContent ?? null,
  level: tp.appStore.getState().buildingDetails.building.level,
  gold: tp.appStore.getState().company.gold,
}));
console.log("before:", JSON.stringify(before));
await shot("1-preview");

await page.click(".building-panel .upgrade button.primary");
await page.waitForTimeout(2500);
// No Undo (the owner, 2026-10-31: "let's remove it for all for now")
const undo = await page.evaluate(() => document.querySelector(".building-panel .undo-line") !== null);
await shot("2-bought");
const pass = /Level \d+: [+−]/.test(before.preview ?? "") && !undo;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none", "building", id);
await browser.close();
process.exit(pass ? 0 : 1);

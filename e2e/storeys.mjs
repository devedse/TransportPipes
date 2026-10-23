// The Landmark's storeys (after run 4: once the Landmark stood the crew had nothing to build together): with two storeys
// raised and the third open, the project tab shows the tower with its storeys, the purse and Give by the hour; a Give
// pays and says so; the map's Landmark stands taller; the tracker names the storey.
//   node storeys.mjs shots/storeys
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/storeys";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "storey");
for (const c of ["gold", "earn", "earn", "level", "level", "storey", "storey"]) await cheat(c);
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
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(2500);

const row = await page.locator(".goal-tracker li", { hasText: "storey" }).first().innerText().catch(() => "");
// For two hours after a storey stands the line says so; then it names the open one
expect("the tracker names the storey", /Landmark, storey 3 · \d+% · its purse wants [\d.,kM]+ g: Give|Landmark rose to storey 2: its pipes carry \+20%/.test(row), row.replace(/\s+/g, " "));
await page.evaluate(() => tp.gameClient.openProject());
await page.waitForSelector(".storeys", { timeout: 15000 });
await page.waitForTimeout(1500);
const bands = await page.locator(".landmark-tower .landmark-band").count();
expect("the tower shows its storeys and the rising one", bands === 4, `${bands} bands`);
const text = await page.locator(".storeys-text").innerText();
expect("the purse, the pipe perk and the plaque", /Storey 3 of the Landmark/.test(text) && /pipes carry \+20%/.test(text) && /Storey 2's plaque/.test(text), text.replace(/\s+/g, " "));
await shot("1-project");
const before = await page.evaluate(() => tp.appStore.getState().company.gold);
await page.locator(".storeys button", { hasText: "Give an hour" }).click();
await page.waitForTimeout(2000);
const after = await page.evaluate(() => ({ gold: tp.appStore.getState().company.gold, toast: [...document.querySelectorAll(".toast")].map((t) => t.textContent).join(" | ") }));
expect("a Give pays an hour of income and says so", after.gold < before && /Given [\d.,k]+ g to .*Landmark, storey 3 💛/.test(after.toast), after.toast);
await shot("2-given");

// The map: the Landmark in the Yard
await page.keyboard.press("Escape");
const at = await page.evaluate(() => {
  const district = [...tp.gameClient.world.districts.values()].find((d) => d.hub.some((h) => h.type === "project-landmark")) ?? null;
  return district === null ? null : (() => { const h = district.hub.find((t) => t.type === "project-landmark"); return { x: district.originX + h.x, y: district.originY + h.y, storeys: h.storeys }; })();
});
console.log("landmark:", JSON.stringify(at));
if (at !== null) {
  // The station's Yard, where its project stands
  await page.evaluate(() => {
    const co = tp.appStore.getState().company;
    const yard = tp.layout.yardRect(co.districtX, co.districtY, tp.layout.stationOfSlot(co.slot));
    tp.gameClient.engine?.lookAt(yard.x0 + 1, yard.y0 - 4, 0.6);
  });
  await page.waitForTimeout(2500);
  await shot("3-map");
}
expect("the map's Landmark knows its storeys", at !== null && at.storeys === 2, JSON.stringify(at));

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

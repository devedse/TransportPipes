// What a Storage holds above its line (playtest: "the energy is now above the limit, so how can I get rid of that? I
// want everything at 806, but it doesn't fit"): it goes on to the Trading Post on its pipes and is sold, down to the
// line, as fast as its pipe carries it; the panel says so.   node storage-drain.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/storage-drain"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "sd");
await cheat("gold");
console.log(await kit(page, ["storage"]));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
// Kept everything, it filled up with every good; then its line lowered to What's needed: iron ore is above it
const id = await page.evaluate(async () => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "storage");
  await tp.gameClient.connection.setBuildingSetting(b.id, 1);
  return b.id;
});
console.log("stock:", await cheat("stock"));
await page.evaluate(async (id) => {
  await tp.gameClient.connection.setBuildingSetting(id, 0);
  tp.gameClient.showBuilding(id);
}, id);
await page.waitForSelector(".building-panel", { timeout: 15000 });
await page.waitForTimeout(3000);
// All it holds (one good after another goes, as fast as its pipe to the post carries them)
const ore = async () => {
  await page.evaluate((id) => tp.gameClient.showBuilding(id), id);
  await page.waitForTimeout(1500);
  return page.evaluate(() => Object.values(tp.appStore.getState().buildingDetails?.contents ?? {}).reduce((a, b) => a + b, 0));
};
const first = await ore();
const panel = (await page.locator(".building-panel").innerText()).replace(/\s+/g, " ");
check(/what's above it goes on to your Trading Post and is sold, as fast as its pipe there carries it/.test(panel), "the panel says where what's above the line goes", panel.slice(0, 300));
await shot("1-above");
await page.waitForTimeout(30000);
const later = await ore();
console.log(`stored ${first.toFixed(0)} → ${later.toFixed(0)} in 30 s`);
check(later < first - 20, "it is sold down", `${first} → ${later}`);
check(later > 0, "about a tenth a minute, not at once", String(later));
await shot("2-later");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// What a building does, at the top of its panel (playtest: "wouldn't it make sense to show in a bar how much the trading
// building is being used? I think those kind of stats should be at the top for all buildings"): a producer's Needs and
// Makes, a Storage's Stored, the Trading Post's sales against your Hub Orders (with Buy from the hub), all above Upgrade.
//   node panel-tops.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/panel-tops"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1100 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "pt");
for (const c of ["level", "level", "gold"]) await cheat(c);
console.log(await kit(page, ["windMoily", "windMoily", "solarPanel", "ironMine", "smallFactory", "storage", "lab"]));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.waitForTimeout(3000);
const open = async (type) => {
  const id = await page.evaluate((type) => {
    const co = tp.appStore.getState().company;
    const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === type);
    if (b) tp.gameClient.showBuilding(b.id);
    return b?.id ?? null;
  }, type);
  if (id === null) return null;
  await page.waitForSelector(".building-panel", { timeout: 15000 });
  await page.waitForTimeout(2000);
  return id;
};
// Whether the first selector comes before the Upgrade section in the panel
const before = (selector) => page.evaluate((selector) => {
  const panel = document.querySelector(".building-panel");
  const a = panel?.querySelector(selector);
  const upgrade = panel?.querySelector(".upgrade");
  if (!a) return "missing";
  if (!upgrade) return "no upgrade";
  return a.compareDocumentPosition(upgrade) & Node.DOCUMENT_POSITION_FOLLOWING ? "before" : "after";
}, selector);
const heading = (text) => `.building-panel section:has(> h4:text-is("${text}"))`;

await open("tradingPost");
const sales = await page.locator(".building-panel .post-sales").innerText().catch(() => "");
console.log("sales:", sales.replace(/\s+/g, " ").slice(0, 300));
check(/Sells to the hub · [\d.,k]+ g\/min/i.test(sales), "the Trading Post says what it sells to the hub", sales.slice(0, 200));
check(/Energy · /.test(sales) && /The line is your Hub Order/.test(sales), "each good against the Hub Order line", sales.slice(0, 300));
check(/Buy from the hub/.test(sales), "and Buy from the hub is there again");
check(await before(".post-sales") === "before", "above Upgrade", await before(".post-sales"));
await shot("1-trading-post");

await open("windMoily");
check(await page.evaluate(() => [...document.querySelectorAll(".building-panel h4")].some((t) => t.textContent === "Makes")), "a producer's Makes");
check(await page.evaluate(() => {
  const panel = document.querySelector(".building-panel");
  const makes = [...panel.querySelectorAll("h4")].find((t) => t.textContent === "Makes");
  const upgrade = panel.querySelector(".upgrade");
  return makes && upgrade && (makes.compareDocumentPosition(upgrade) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
}), "above Upgrade");
await shot("2-producer");

await open("smallFactory");
await shot("3-factory");

await open("storage");
check(await page.evaluate(() => {
  const panel = document.querySelector(".building-panel");
  const stored = [...panel.querySelectorAll("h4")].find((t) => t.textContent.startsWith("Stored"));
  const upgrade = panel.querySelector(".upgrade");
  return stored && upgrade && (stored.compareDocumentPosition(upgrade) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
}), "a Storage's Stored above Upgrade");
await shot("4-storage");

if ((await open("lab")) !== null) await shot("5-lab");
if ((await open("hq")) !== null) await shot("6-hq");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// Nothing sticks out of a panel (the owner, 2026-10-31: "some things don't fit in the trade menu now. Can you check
// those kind of things too?"): every panel and tab opened on an estate with a Storage, a Trading Post and pipes, and
// any element whose right edge passes its panel's (not inside a box that scrolls on its own) is listed.
//   node panels-fit.mjs <outdir> [width]
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/panels-fit", width = "1400"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: Number(width), height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "pf");
for (const c of ["level", "level", "gold"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
console.log(await kit(page, ["windMoily", "windMoily", "solarPanel", "storage"]));
await page.waitForTimeout(2500);

const fits = (selector) => page.evaluate((selector) => {
  const panel = document.querySelector(selector);
  if (!panel) return ["(not open)"];
  const right = panel.getBoundingClientRect().right;
  return [...panel.querySelectorAll("*")].filter((t) => {
    const r = t.getBoundingClientRect();
    if (r.width <= 0 || r.right <= right + 1) return false;
    // Inside something that scrolls sideways on its own: fine
    for (let up = t.parentElement; up && up !== panel; up = up.parentElement) {
      const style = getComputedStyle(up);
      if ((style.overflowX === "auto" || style.overflowX === "scroll") && up.scrollWidth > up.clientWidth) return false;
    }
    return true;
  }).slice(0, 3).map((t) => `${t.tagName.toLowerCase()}.${String(t.className).split(" ")[0]} "${(t.textContent ?? "").trim().slice(0, 30)}" +${Math.round(t.getBoundingClientRect().right - right)}px`);
}, selector);
const look = async (name, open, selector) => {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(300);
  await open();
  await page.waitForTimeout(1800);
  const out = await fits(selector);
  check(out.length === 0, `${name}: nothing sticks out`, JSON.stringify(out));
  if (out.length > 0) await shot(`x-${name.replace(/\W+/g, "-")}`);
};
const ids = await page.evaluate(() => {
  const mine = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === tp.appStore.getState().company.id);
  const by = (type) => mine.find((t) => t.type === type)?.id;
  return { post: by("tradingPost"), storage: by("storage"), moily: by("windMoily"), hq: by("hq") };
});
await look("Yard", () => page.evaluate(() => tp.gameClient.openYard()), ".yard-panel");
for (const tab of ["now", "links", "settings", "town"]) await look(`Trades ${tab}`, () => page.evaluate((t) => tp.gameClient.openTrades(t), tab), ".trades-panel");
for (const [name, id] of Object.entries(ids)) if (id) await look(`building ${name}`, () => page.evaluate((id) => tp.gameClient.showBuilding(id), id), ".building-panel");
await look("network", async () => { await page.evaluate((id) => tp.gameClient.showBuilding(id), ids.moily); await page.waitForTimeout(800); const net = await page.evaluate(() => tp.appStore.getState().buildingDetails?.building.network); if (net) await page.evaluate((n) => tp.gameClient.selectNetwork(n), net); }, ".network-panel, .building-panel");
await look("gold", () => page.click(".top-gold"), ".gold-uses");
for (const tab of ["orders", "market", "calendar", "wonders", "exchange"]) await look(`Hub ${tab}`, () => page.evaluate((t) => tp.gameClient.openHub(t), tab), ".hub-panel");
await look("research", () => page.evaluate(() => tp.gameClient.openResearch()), ".research-panel");
await look("company", () => page.evaluate(() => tp.gameClient.openCompany()), ".company-panel");
await look("crew", () => page.evaluate(() => tp.gameClient.openCrew()), ".crew-panel");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

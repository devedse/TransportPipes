// Dealt land (step 5 of the redesign): the station's plots hold all five land goods between them, the Deposits overlay
// labels every seat's goods, and the Build menu says who makes a good the land lacks (1.4: a plot starts empty, no
// kit): node land.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/land"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out);
await newPlayer(a.page, "land");
// The card's Continue, if a card waits
if (await a.page.$(".return-card")) await a.page.click(".return-card button.primary");
const state = await a.page.evaluate(() => {
  const s = tp.appStore.getState();
  const kit = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === s.company.id).map((t) => t.type);
  return { land: s.company.land, kit };
});
console.log("land", JSON.stringify(state.land?.goods), "station", JSON.stringify(state.land?.station.map((t) => `${t.slot}:${t.goods.join("+")}${t.house ? "⚙" : ""}${t.you ? "*" : ""}`)), "kit", state.kit.join(","));
if (state.land === null) fail("dealt land");
if (state.kit.some((t) => t !== "tradingPost" && t !== "hq")) fail(`a plot starts empty: ${state.kit.join(", ")}`);
const station = state.land?.station.flatMap((t) => t.goods) ?? [];
if (["copperOre", "coal", "sand", "wood", "stone"].some((t) => !station.includes(t))) fail("the station holds all five");

// The Deposits overlay: the seats' goods
await a.page.keyboard.press("Escape");
await a.page.click("text=Deposits");
await a.page.evaluate(() => { const e = tp.appStore.getState().company; tp.gameClient.engine?.lookAt(e.estateX + 16, e.estateY + 16, 0.25); });
await a.page.waitForTimeout(2500);
await a.shot("1-deposits");

// The Build menu: a land good the seat doesn't hold
await a.page.click("text=Deposits");
await a.page.keyboard.press("b");
await a.page.waitForSelector(".build-menu", { timeout: 10000 });
await a.page.click(".build-menu .chip:has-text('Extraction')");
await a.page.waitForTimeout(800);
await a.shot("2-build-menu");
const lines = await a.page.$$eval(".build-menu .land-line", (t) => t.map((x) => x.textContent));
console.log(lines);
if (lines.length === 0 || !lines.every((t) => t.startsWith("There's no"))) fail("the Build menu says who makes the goods the land lacks");
if (a.errors.length > 0) fail(a.errors.join(" | "));
await a.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

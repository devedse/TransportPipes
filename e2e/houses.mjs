// Houses and stand-ins (v1.2 §9): on a small server Houses keep every seat warm; a newcomer who takes a House's seat
// gets its starter kit, piped to the Yard gate already; when a neighbour falls asleep a stand-in House keeps its trades
// going. A and B start; B is put to sleep (the dev cheat); A's Yard panel names the stand-in: node houses.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/houses"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out);
await newPlayer(a.page, "ha");
const kit = await a.page.evaluate(() => {
  const s = tp.appStore.getState();
  return [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === s.company.id && t.type !== "hq" && t.type !== "tradingPost").map((t) => `${t.type} L${t.level}`);
});
console.log("A's starter kit", kit);
if (kit.length === 0) fail("A gets a starter kit");
await a.shot("1-a-kit");

const b = await start(out);
await newPlayer(b.page, "hb");
const bName = await b.page.evaluate(() => tp.appStore.getState().company.name);

// A's Yard: B, and the Houses with what they do
await a.page.evaluate(() => tp.gameClient.openYard());
await a.page.waitForSelector(".yard-residents li", { timeout: 15000 });
await a.page.waitForTimeout(1000);
await a.shot("2-a-yard");
const residents = await a.page.$$eval(".yard-residents li .resident-name", (t) => t.map((x) => x.textContent.replace(/\s+/g, " ").trim()));
console.log(residents);
if (!residents.some((t) => t.includes("⚙ House"))) fail("Houses live in the station");
if (!residents.some((t) => t.includes(bName))) fail("B lives next to A");

// B falls asleep: a stand-in keeps B's trades
await b.cheat("sleep");
await a.page.waitForTimeout(1500);
await a.page.evaluate(() => tp.gameClient.refreshYard());
await a.page.waitForTimeout(800);
await a.shot("3-a-stand-in");
const asleep = await a.page.$$eval(".yard-residents li .resident-name", (t) => t.map((x) => x.textContent.replace(/\s+/g, " ").trim()).find((x) => x.includes("stands in")) ?? null);
console.log("B now:", asleep);
if (asleep === null) fail("A's Yard panel names B's stand-in");

for (const [name, player] of [["A", a], ["B", b]]) {
  if (player.errors.length > 0) fail(`${name}: ${player.errors.join(" | ")}`);
  await player.browser.close();
}
console.log(process.exitCode ? "done with failures" : "ok");

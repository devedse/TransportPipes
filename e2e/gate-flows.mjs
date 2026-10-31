// What goes through the Yard gate shows where the goods are (the audit, 2026-10-31: those pipes looked empty on the map
// and in the hover, and a building's Where it goes had no row for it): a Solar Panel piped to the Trading Post and to the
// Yard gate, the Yard's rounds run (the town's Wonder takes spare through the gate): the map's flows have the gate's,
// the building's panel a row to or from the Yard (a neighbour at the same Yard whose Iron Mine needs energy takes it).
//   node gate-flows.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/gate-flows"] = process.argv.slice(2);
const p = await start(out, { width: 1400, height: 1000 });
const { page, errors, shot, cheat } = p;
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "gf");
await cheat("gold");
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const settled = JSON.parse(await settle(p, "solarPanel"));
console.log(JSON.stringify(settled));
// A neighbour at the same Yard whose Iron Mine needs energy: a taker at the gate
const seat = await page.evaluate(async () => {
  const me = tp.appStore.getState().company;
  const town = await tp.gameClient.connection.getStations(me.districtX, me.districtY);
  const station = town.stations.find((t) => t.seats.some((s) => s.slot === me.slot));
  const free = station?.seats.find((s) => s.state === "free");
  return free ? { districtX: me.districtX, districtY: me.districtY, slot: free.slot } : null;
});
const q = await start(`${out}/b`, { width: 1200, height: 800 });
await newPlayer(q.page, "gn", seat);
await q.cheat("gold");
await q.cheat("level");
console.log("neighbour", seat?.slot, await settle(q, "ironMine", "ironOre"));
for (let i = 0; i < 3; i++) { await cheat("yard"); await page.waitForTimeout(1500); }
await page.waitForTimeout(3000);
await page.evaluate(() => tp.gameClient.refreshFlows());
await page.waitForTimeout(2000);
const gate = await page.evaluate(() => [...tp.gameClient.world.flows.values()].flatMap((n) => n.flows).filter((f) => f.kind === "toYard" || f.kind === "fromYard").map((f) => ({ kind: f.kind, resource: f.resource, sent: f.sent, tiles: f.path.length / 2 })));
console.log("gate flows:", JSON.stringify(gate));
check(gate.length > 0 && gate.every((f) => f.tiles >= 1), "the map's flows have what goes through the gate, with its pipe", JSON.stringify(gate));
const id = await page.evaluate(({ x, y }) => tp.gameClient.world.buildingAt(x, y)?.id, settled.spot);
await page.evaluate((id) => tp.gameClient.showBuilding(id), id);
await page.waitForSelector(".flow-groups", { timeout: 15000 });
// Its row once the round that sends through the gate has come
for (let i = 0; i < 20 && !(await page.evaluate(() => tp.appStore.getState().buildingDetails?.flows?.some((f) => f.kind === "toYard" || f.kind === "fromYard"))); i++) {
  await page.evaluate(() => tp.gameClient.refreshDetails());
  await page.waitForTimeout(1000);
}
const where = (await page.locator(".flow-groups").innerText()).replace(/\s+/g, " ");
console.log("where it goes:", where.slice(0, 240));
check(/your Yard gate/.test(where) && /through the Yard/.test(where), "the building's Where it goes has its row to the Yard", where.slice(0, 160));
await shot("1-panel");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await p.browser.close();
await q.browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

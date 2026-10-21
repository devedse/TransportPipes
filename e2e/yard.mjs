// Two players in one station trade through their Yard (v1.2 §7): A's Wind Moily and B's Iron Mine, each piped to their
// Yard gate with the Yard panel's "Plan the pipe" button. B's mine should get A's energy at the player price, and the
// Yard panel should show it: node yard.mjs <outdir>
import { start, newPlayer, settle, emptySeats, moveTo, clearKit } from "./lib.mjs";
const [out = "shots/yard"] = process.argv.slice(2);

const a = await start(out);
await newPlayer(a.page, "ya");
await a.cheat("gold");
const b = await start(out);
await newPlayer(b.page, "yb");
await b.cheat("gold");
await b.cheat("level");
// In a busy world they may land apart: both move to the two first seats of a station nobody lives in
const [seatA, seatB] = await emptySeats(a.page, 2);
console.log("moved to", await moveTo(a, seatA), await moveTo(b, seatB));
// A House's starter kit would give B's mine its energy: B starts from an empty estate
console.log("kit cleared", await clearKit(b));

console.log("A", await settle(a, "windMoily"));
console.log("B", await settle(b, "ironMine", "ironOre"));
// The Yards' rounds (a minute each on the server): one matches, the next delivers
await a.cheat("yard");
await new Promise((r) => setTimeout(r, 1500));

for (const [name, player] of [["A", a], ["B", b]]) {
  const yard = await player.page.evaluate(async () => {
    await tp.gameClient.openYard();
    await new Promise((r) => setTimeout(r, 800));
    const y = tp.appStore.getState().yard;
    return y && JSON.stringify({ station: y.station, connected: y.connected, line: y.line, yours: y.yours, residents: y.residents.map((t) => [t.name, t.connected]) });
  });
  console.log(name, yard);
  const gate = await player.page.evaluate(() => { const y = tp.appStore.getState().yard; return y && { x: y.gateX, y: y.gateY }; });
  if (gate) await player.page.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x, y, 0.7), gate);
  await player.page.waitForTimeout(1500);
  await player.shot(`${name}-yard`);
}
for (const player of [a, b]) {
  console.log(player.errors.length ? player.errors.join("\n") : "no errors");
  await player.browser.close();
}

// A new link says when its first goods come, and they come then (the owner, 2026-10-31: "I just setup a coal
// connection with Frib, but my smelters are not doing anything still ... I think there's a 1 minute delay"; the card
// said "You buy 0 coal/min" for a minute or two): the Yard moves links' goods at its rounds, a round a minute, the first
// round after Accept sets the link up and the next brings the goods. A (a Wind Moily) offers B (an Iron Mine) its
// energy, B accepts, no cheat runs the rounds: B's card says it is starting and counts down to the first energy, the
// energy comes when it said (within a few seconds), and the card shows it flowing by itself.
//   node link-start.mjs <outdir>
import { start, newPlayer, settle, clearKit } from "./lib.mjs";
const [out = "shots/link-start"] = process.argv.slice(2);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

const a = await start(`${out}/a`, { width: 1280, height: 860 });
await newPlayer(a.page, "sa");
await a.cheat("gold");
// B starts in A's town at a station nobody lives at (a link joins two stations of a town; between towns it needs a
// trunk line)
const seat = await a.page.evaluate(async () => {
  const me = tp.appStore.getState().company;
  const town = await tp.gameClient.connection.getStations(me.districtX, me.districtY);
  const station = town.stations.find((t) => t.seats.length > 0 && t.seats.every((s) => s.state === "free"));
  return station ? { districtX: me.districtX, districtY: me.districtY, slot: station.seats[0].slot } : null;
});
console.log("B's plot", JSON.stringify(seat));
const b = await start(`${out}/b`, { width: 1280, height: 860 });
await newPlayer(b.page, "sb", seat);
await b.cheat("gold");
await b.cheat("level");
const bName = await b.page.evaluate(() => tp.appStore.getState().company.name);
const partners = () => a.page.evaluate(async (bName) => (await tp.gameClient.connection.getLinks()).partners.some((t) => t.name === bName), bName);
check(seat !== null && (await partners()), "A and B live in two stations of a town");
console.log("kit cleared", await clearKit(b));
console.log("A", await settle(a, "windMoily"));
console.log("B", await settle(b, "ironMine", "ironOre"));
// A's Foundation stands, so A's energy is A's to link (see links.mjs); the rounds settle before the link
await a.cheat("project");
await a.cheat("yard");

const offered = await a.page.evaluate(async (bName) => {
  const c = tp.gameClient.connection;
  const links = await c.getLinks();
  const partner = links.partners.find((t) => t.name === bName);
  return partner ? await c.offerLink(partner.id, "energy", true, 1) : "B isn't a partner";
}, bName);
console.log("offered", JSON.stringify(offered));
await b.page.evaluate(() => tp.gameClient.openLinks());
await b.page.waitForSelector(".link-card button.primary:has-text('Accept')", { timeout: 20000 });
await b.page.click(".link-card button.primary:has-text('Accept')");
const accepted = Date.now();
await b.page.waitForTimeout(1500);
const card = b.page.locator(".link-card").first();
const read = () => b.page.evaluate(async () => (await tp.gameClient.connection.getLinks()).links.find((t) => t.resource === "energy") ?? null);
const first = await read();
const cardText = (await card.innerText()).replace(/\s+/g, " ");
console.log("after Accept:", first?.line, "| startsAt", first?.startsAt, "| card:", cardText.slice(0, 260));
await b.shot("1-starting");
const promised = first?.startsAt ? new Date(first.startsAt).getTime() : null;
check(!/ 0 energy\/min/.test(first?.line ?? ""), "the card doesn't say 0 energy a minute", first?.line);
check(/Starting/.test(first?.line ?? ""), "it says the link is starting", first?.line);
check(promised !== null && promised - accepted > 0 && promised - accepted <= 125_000, "it says when the first energy comes, within two minutes", `${promised === null ? "none" : `${((promised - accepted) / 1000).toFixed(0)} s`}`);
check(/First energy in \d/.test(cardText), "and counts down to it on the card", cardText.slice(0, 200));

// The rounds come by themselves: when does the first energy arrive, and when does the card show it?
let came = null;
let shown = null;
for (let i = 0; i < 100 && (came === null || shown === null); i++) {
  await b.page.waitForTimeout(2000);
  const link = await read();
  if (came === null && link?.rate > 0) came = Date.now();
  if (shown === null && /\/min at /.test(await card.innerText())) shown = Date.now();
}
console.log(`accepted +0 s · promised +${promised === null ? "?" : ((promised - accepted) / 1000).toFixed(0)} s · came +${came === null ? "never" : ((came - accepted) / 1000).toFixed(0)} s · card shows +${shown === null ? "never" : ((shown - accepted) / 1000).toFixed(0)} s`);
check(came !== null, "the energy comes");
check(came !== null && promised !== null && Math.abs(came - promised) <= 8000, "when the card said, within a few seconds", `promised ${promised} came ${came}`);
check(came !== null && shown !== null && shown - came <= 6000, "and the card shows it flowing by itself", `came ${came} shown ${shown}`);
await b.shot("2-flowing");
for (const [name, player] of [["A", a], ["B", b]]) {
  check(player.errors.length === 0, `${name}: browser errors: none`, player.errors.join(" | "));
  await player.browser.close();
}
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

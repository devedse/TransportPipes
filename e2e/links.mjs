// A link between two stations (v1.2 §7): A (a Wind Moily) and B (an Iron Mine that needs energy) live in two stations
// nobody else lives in. A's Links tab suggests B; A offers the link, B accepts it, the Yards' rounds run, and both see
// it flow, with the link drawn along the corridors on the map: node links.mjs <outdir>
import { start, newPlayer, settle, emptySeats, moveTo, clearKit } from "./lib.mjs";
const [out = "shots/links"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out);
await newPlayer(a.page, "la");
await a.cheat("gold");
const b = await start(out);
await newPlayer(b.page, "lb");
await b.cheat("gold");
await b.cheat("level");

// Two stations of their own
const [seatA] = await emptySeats(a.page, 1);
const [seatB] = await emptySeats(a.page, 1, [seatA.station]);
console.log("moved to", await moveTo(a, seatA), await moveTo(b, seatB));
// A House's starter kit would give B's mine its energy: B starts from an empty estate
console.log("kit cleared", await clearKit(b));
console.log("A", await settle(a, "windMoily"));
console.log("B", await settle(b, "ironMine", "ironOre"));
// A lives alone in its station: its Foundation would take all of A's spare energy (every stage stands by the Yard
// since run 2), so it stands now and A's energy is A's to link
await a.cheat("project");
await a.cheat("yard");

const links = (player) => player.page.evaluate(async () => { await tp.gameClient.refreshLinks(); return tp.appStore.getState().links; });

// A: the Links tab suggests B
await a.page.evaluate(() => tp.gameClient.openLinks());
await a.page.waitForSelector(".yard-panel .link-suggestions, .yard-panel .new-link", { timeout: 15000 });
await a.page.waitForTimeout(800);
await a.shot("1-a-links");
const suggested = (await links(a)).suggestions;
console.log("suggestions", suggested.map((t) => `${t.partnerName} ${t.resource} ${t.youSell ? "sell" : "buy"} ${t.perMinute.toFixed(2)}/min ${t.length} tiles`));
const bName = await b.page.evaluate(() => tp.appStore.getState().company.name);
const pick = suggested.find((t) => t.partnerName === bName && t.resource === "energy" && t.youSell);
if (pick === undefined) fail("A's Links tab suggests selling energy to B");

// A offers it (the suggestion fills the form)
if (pick !== undefined) {
  const row = a.page.locator(".link-suggestion", { hasText: bName }).first();
  await row.locator("button", { hasText: "Link" }).click();
} else {
  const id = (await links(a)).partners.find((t) => t.name === bName)?.id;
  await a.page.selectOption("select[aria-label=Partner]", String(id));
  await a.page.selectOption("select[aria-label=Good]", "energy");
}
await a.page.waitForSelector(".new-link p.small", { timeout: 15000 });
await a.page.waitForTimeout(500);
await a.shot("2-a-plan");
await a.page.click("text=Offer the link");
await a.page.waitForTimeout(1500);
await a.shot("3-a-offered");
const offered = (await links(a)).links.find((t) => t.partnerName === bName);
console.log("A's offer", offered?.state, offered?.line);
if (offered?.state !== "offered") fail("the offer waits for B");

// B: the offer waits on the Links tab, with a badge
await b.page.evaluate(() => tp.gameClient.openYard());
await b.page.waitForTimeout(1500);
await b.page.evaluate(() => tp.gameClient.openLinks());
await b.page.waitForSelector(".link-card", { timeout: 15000 });
await b.page.waitForTimeout(500);
await b.shot("4-b-offer");
await b.page.click(".link-card button.primary:has-text('Accept')");
await b.page.waitForTimeout(1500);

// The rounds: one matches, the next delivers
await a.cheat("yard");
await a.page.waitForTimeout(1500);
const bLink = (await links(b)).links.find((t) => t.resource === "energy");
const aLink = (await links(a)).links.find((t) => t.resource === "energy");
console.log("B", bLink?.state, bLink?.rate.toFixed(2), bLink?.line);
console.log("A", aLink?.state, aLink?.rate.toFixed(2), aLink?.line);
if (!(bLink?.rate > 0)) fail("energy flows to B's gate");
if (aLink?.tone !== "good") fail("A's state line says it flows");

for (const [name, player] of [["a", a], ["b", b]]) {
  await player.page.evaluate(() => tp.gameClient.openLinks());
  await player.page.waitForSelector(".link-card", { timeout: 15000 });
  await player.page.waitForTimeout(800);
  await player.shot(`5-${name}-live`);
  // The link on the map: "show" looks along its route
  await player.page.click(".link-card button.link:has-text('show')");
  await player.page.waitForTimeout(2500);
  await player.shot(`6-${name}-map`);
}

const mapLinks = await a.page.evaluate(() => tp.appStore.getState().mapLinks);
console.log("map links", mapLinks.length);
if (mapLinks.length === 0) fail("the map knows the link");
for (const [name, player] of [["A", a], ["B", b]]) {
  if (player.errors.length > 0) fail(`${name}: ${player.errors.join(" | ")}`);
  await player.browser.close();
}
console.log(process.exitCode ? "done with failures" : "ok");

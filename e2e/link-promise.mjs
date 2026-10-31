// A link's promise and cap (the owner, 2026-10-31: "25 guaranteed, but 100 on reserve"; "A: yes a real promise"):
// A offers B energy, B accepts; A's card says nothing is guaranteed, A promises 5 a minute up to 8 and the card says so;
// B asks for 6 up to 10, A's card asks A, A accepts, and both cards show the new numbers.
//   node link-promise.mjs <outdir>
import { start, newPlayer, settle, clearKit } from "./lib.mjs";
const [out = "shots/link-promise"] = process.argv.slice(2);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

const a = await start(`${out}/a`, { width: 1280, height: 860 });
await newPlayer(a.page, "pa");
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
await newPlayer(b.page, "pb", seat);
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
  return partner ? await c.offerLink(partner.id, "energy", true, 1, 0, 0) : "B isn't a partner";
}, bName);
console.log("offered", JSON.stringify(offered));
await b.page.evaluate(() => tp.gameClient.openLinks());
await b.page.waitForSelector(".link-card button.primary:has-text('Accept')", { timeout: 20000 });
await b.page.click(".link-card button.primary:has-text('Accept')");
await b.page.waitForTimeout(1500);
const terms = (player) => player.page.evaluate(async () => { await tp.gameClient.refreshLinks(); return tp.appStore.getState().links.links.find((t) => t.resource === "energy"); });

// A: nothing promised yet; A promises 5, up to 8
await a.page.evaluate(() => tp.gameClient.openLinks());
await a.page.waitForSelector(".link-card .link-terms", { timeout: 20000 });
const card = a.page.locator(".link-card").first();
check(/Guaranteed nothing/.test((await card.locator(".link-terms").innerText()).replace(/\s+/g, " ")), "a new link: nothing guaranteed");
await card.locator(".link-terms button", { hasText: "change" }).click();
const inputs = card.locator(".link-terms-inputs input");
await inputs.nth(0).fill("5");
await inputs.nth(1).fill("8");
await card.locator(".link-terms button.primary", { hasText: "Save" }).click();
await a.page.waitForTimeout(1500);
let link = await terms(a);
check(link.guaranteed === 5 && link.upTo === 8, "A promises 5 a minute, up to 8", JSON.stringify({ g: link.guaranteed, u: link.upTo }));
const shown = (await card.locator(".link-terms").innerText()).replace(/\s+/g, " ");
console.log("A's card:", shown.slice(0, 200));
check(/Guaranteed 5\/min · Up to 8\/min/.test(shown), "and its card says so", shown.slice(0, 120));
await a.shot("1-promised");

// B asks for more: 6 up to 10; A accepts
const asked = await b.page.evaluate(async () => {
  const l = tp.appStore.getState().links?.links.find((t) => t.resource === "energy") ?? (await tp.gameClient.connection.getLinks()).links.find((t) => t.resource === "energy");
  return tp.gameClient.connection.setLinkTerms(l.id, 6, 10);
});
check(asked.success, "B asks for 6, up to 10", JSON.stringify(asked));
await a.page.evaluate(() => tp.gameClient.refreshLinks());
await a.page.waitForSelector(".link-card .link-ask button.primary", { timeout: 15000 });
const ask = (await card.locator(".link-ask").innerText()).replace(/\s+/g, " ");
console.log("A sees:", ask);
check(/asks for 6\/min guaranteed, up to 10\/min/.test(ask), "A's card asks A", ask);
await a.shot("2-asked");
await card.locator(".link-ask button.primary", { hasText: "Accept" }).click();
await a.page.waitForTimeout(1500);
link = await terms(b);
check(link.guaranteed === 6 && link.upTo === 10 && link.askedGuaranteed == null, "accepted: both see 6, up to 10", JSON.stringify({ g: link.guaranteed, u: link.upTo, asked: link.askedGuaranteed }));
for (const [name, player] of [["A", a], ["B", b]]) {
  check(player.errors.length === 0, `${name}: browser errors: none`, player.errors.join(" | "));
  await player.browser.close();
}
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// Starting with friends (v1.2 §5): Ana founds a crew from the start screen and keeps three seats; Ben opens his seat's
// link and moves in as a guest with two taps; Cleo joins with the crew link; Ana's crew panel shows all of them, and
// Ben's Move tab picks a seat on the station picker: node crew.mjs <outdir>
import { start } from "./lib.mjs";
const [out = "shots/crew"] = process.argv.slice(2);
const base = process.env.BASE_URL ?? "http://localhost:5173";
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

// Ana: an account, "Start with friends", the crew and the company
const ana = await start(out);
const user = `ana${Date.now() % 100000}`;
const crewName = `Kettle ${user.slice(3)}`;
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".auth-panel", { timeout: 30000 });
await ana.page.click("text=New player");
await ana.page.fill("input[autocomplete=username]", user);
await ana.page.fill("input[type=password]", "secret123");
await ana.page.click(".auth-panel button[type=submit]");
await ana.page.waitForSelector(".start-ways", { timeout: 30000 });
await ana.shot("1-ways");
await ana.page.click("text=Start with friends");
await ana.page.waitForSelector(".seat-row.chosen", { timeout: 30000 });
await ana.page.fill(".crew-fields input", crewName);
await ana.page.click(".emblem[title=owl]");
await ana.page.getByLabel("Company name").fill(`${user} Works`);
await ana.shot("2-friends-form");
await ana.page.click(".found-panel button[type=submit]");
await ana.page.waitForSelector(".share-panel", { timeout: 30000 }).catch(async (e) => { await ana.shot("x-no-share"); console.log(await ana.page.$eval(".found-panel", (t) => t.innerText).catch(() => "no panel")); throw e; });
await ana.page.waitForTimeout(1500);
await ana.shot("3-share-codes");
const codes = await ana.page.$$eval(".held-seats .code", (t) => t.map((x) => x.textContent.trim()));
console.log("seat codes", codes);
if (codes.length !== 3) fail(`3 seat codes, got ${codes.length}`);
// Who the first seat is for (F9): its link becomes Ben's
await ana.page.locator(".held-seats input").first().fill("Ben");
await ana.page.locator(".held-seats input").first().press("Enter");
await ana.page.waitForTimeout(500);
if (!(await ana.page.locator("text=Copy Ben's link").isVisible())) fail("the link is Ben's");
await ana.shot("3b-named");
await ana.page.click("text=Start playing");
const crew = await ana.page.evaluate(() => tp.gameClient.connection.getCrew());
const anaSlot = await ana.page.evaluate(() => tp.appStore.getState().company.slot);
console.log("crew", crew?.crew?.name, "invite", crew?.crew?.inviteCode, "Ana's seat", anaSlot);

// Ben: the seat link, no account: "Move in", then "Done"
const ben = await start(out, { width: 420, height: 860, touch: true });
await ben.page.goto(`${base}/?seat=${codes[0]}`);
await ben.page.waitForSelector(".invite-panel", { timeout: 30000 });
await ben.page.waitForTimeout(800);
await ben.shot("4-ben-invite");
const benHeading = await ben.page.$eval(".invite-panel h2", (t) => t.textContent);
console.log("Ben reads", benHeading);
if (!/kept this seat for Ben/.test(benHeading)) fail("the move-in screen says the seat was kept for Ben");
await ben.page.click("text=Move in");
await ben.page.waitForSelector(".invite-panel form", { timeout: 30000 });
await ben.shot("5-ben-name");
await ben.page.click(".invite-panel button[type=submit]");
await ben.page.waitForSelector(".top-bar", { timeout: 30000 });
await ben.page.waitForTimeout(3000);
await ben.shot("6-ben-in");
const benCompany = await ben.page.evaluate(() => {
  const s = tp.appStore.getState();
  return { slot: s.company.slot, crew: s.company.crew?.name ?? null, guest: s.user.isGuest };
});
console.log("Ben", benCompany);
if (benCompany.crew !== crewName) fail("Ben is in the crew");
if (!benCompany.guest) fail("Ben is a guest");

// Cleo: the crew link (a seat of her own somewhere, then the crew)
const cleo = await start(out);
await cleo.page.goto(`${base}/?crew=${crew.crew.inviteCode}`);
await cleo.page.waitForSelector(".invite-panel", { timeout: 30000 });
await cleo.page.click("text=Move in");
await cleo.page.waitForSelector(".invite-panel form", { timeout: 30000 });
await cleo.page.click(".invite-panel button[type=submit]");
await cleo.page.waitForSelector(".top-bar", { timeout: 30000 });
await cleo.page.waitForTimeout(2500);
const cleoCrew = await cleo.page.evaluate(() => tp.appStore.getState().company.crew?.name ?? null);
console.log("Cleo's crew", cleoCrew);
if (cleoCrew !== crewName) fail("Cleo is in the crew");
await cleo.shot("6b-cleo-in");

// Ana's crew panel: three members, two seats still kept
await ana.page.keyboard.press("k");
await ana.page.waitForSelector(".crew-panel .crew-members", { timeout: 15000 });
await ana.page.waitForTimeout(800);
await ana.shot("7-ana-crew");
const members = await ana.page.$$eval(".crew-panel .crew-members li", (t) => t.length);
const held = await ana.page.$$eval(".crew-panel .held-seats li", (t) => t.length);
console.log("members", members, "held", held);
if (members !== 3 || held !== 2) fail(`3 members and 2 held seats, got ${members} and ${held}`);
const stations = await ana.page.$$eval(".crew-panel .crew-members li", (t) => t.map((x) => x.textContent));
console.log(stations);
if (!stations.some((t) => t.includes("(Ben)"))) fail("Ana's crew shows Ben by name");
// Cleo: the crew's own station is full (Ana, Ben and two kept seats), so the small station next to it
const home = / · (.+?) (NW|NE|SW|SE)\b/.exec(stations[0])?.[1];
if (home === undefined || !stations[2]?.includes(`${home} `) || stations[2].includes(stations[0].split(" · ")[1])) fail("Cleo lives next to the crew, in another station");

// Ben, a guest: the company panel asks him to keep his estate
await ben.page.evaluate(() => tp.gameClient.openCompany());
await ben.page.waitForSelector(".keep-note", { timeout: 15000 });
await ben.shot("8-ben-keep-note");

// Ben's Move tab: his own district and station on the picker; he moves to a free seat of another station
await ben.page.click(".tabs .tab:has-text(\"Move\")");
await ben.page.waitForSelector(".move-tab .station-picker .station-card", { timeout: 15000 });
await ben.page.waitForTimeout(800);
await ben.shot("9-ben-move");
const target = await ben.page.$("svg.district-map rect.seat.can");
if (target === null) fail("a free seat to move to");
else {
  await target.click();
  await ben.page.waitForTimeout(300);
  await ben.shot("10-ben-move-picked");
  await ben.page.click("text=Move to this seat");
  await ben.page.waitForTimeout(1500);
  const moving = await ben.page.evaluate(() => tp.appStore.getState().company.moving);
  console.log("Ben moving", moving.arrivesAt, moving.canMoveAt);
  if (moving.arrivesAt === null) fail("Ben's estate is on its way");
}

for (const [name, player] of [["Ana", ana], ["Ben", ben], ["Cleo", cleo]]) {
  if (player.errors.length > 0) fail(`${name}: ${player.errors.join(" | ")}`);
  await player.browser.close();
}
console.log(process.exitCode ? "done with failures" : "ok");

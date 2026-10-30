// Four players find each other on the map (1.4 §6–7, phase A's check). Ana starts in an empty Yard, names it and
// sends her "Play next to me" link; Ben opens it on a phone, as a guest, and lands next to her; Cleo finds Ana by name
// on the map and takes a free plot in her Yard; Ana closes the Yard, so Dev's Pick for me goes elsewhere and his tap on
// Ana's last plot becomes a request, which Ana answers yes: Dev's plot moves in for free. Four browsers, one a phone.
// node four-friends.mjs shots/four-friends
import { emptySeats, moveTo, settle, start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/four-friends";
const base = process.env.BASE_URL ?? "http://localhost:5173";
const stamp = Date.now() % 100000;
const state = (p, f, arg) => p.page.evaluate(f, arg);
const yardName = `Kettle ${stamp}`;
const check = (ok, text) => {
  console.log(ok ? "ok  " : "FAIL", text);
  if (!ok) process.exitCode = 1;
};

// Ana: an empty Yard (the next town, opened by the dev cheat, when the test world has none left), a name and a banner,
// a stall, her link
const ana = await start(`${out}/ana`);
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".auth-panel", { timeout: 60000 });
await ana.page.click("text=New player");
await ana.page.fill("input[autocomplete=username]", `ana${stamp}`);
await ana.page.fill("input[type=password]", "secret123");
await ana.page.click("button[type=submit]");
await ana.page.waitForSelector(".picker-top", { timeout: 30000 });
await ana.page.click("text=Pick for me");
await ana.page.waitForSelector(".plot-card .start-form", { timeout: 30000 });
await ana.page.waitForTimeout(3000);
await ana.shot("1-ana-picks");
await ana.page.fill(".start-form input[minlength='3']", `Ana ${stamp}`);
await ana.page.click(".start-form button[type=submit]");
await ana.page.waitForSelector(".top-bar", { timeout: 30000 });
let seats = await emptySeats(ana.page, 4);
if (seats.length === 0) {
  await ana.cheat("district");
  seats = await emptySeats(ana.page, 4);
}
check(seats.length === 4, `an empty Yard for the four (${seats[0]?.station})`);
console.log(await moveTo(ana, seats[0]));
await ana.page.waitForTimeout(2000);
const named = await state(ana, (yardName) =>
  tp.gameClient.connection.setYard(yardName, "kettle", "#c9963a", "#236b69").then((r) => r.success || r.error),
  yardName,
);
check(named === true, `Ana names her Yard (${named})`);
console.log(await settle(ana, "solarPanel"));
const link = await state(ana, () => tp.gameClient.nextToMeLink());
check(link !== null && link.includes("?near="), `Ana's link: ${link}`);

// Ben: a phone, the link, a guest
const ben = await start(`${out}/ben`, { width: 390, height: 844, touch: true });
await ben.page.goto(link);
await ben.page.waitForSelector(".invite-panel", { timeout: 60000 });
await ben.page.waitForTimeout(1000);
await ben.shot("2-ben-link");
await ben.page.click("button:has-text(\"Play next to\")");
await ben.page.waitForSelector(".plot-card .start-form", { timeout: 60000 });
await ben.page.waitForTimeout(5000);
await ben.shot("3-ben-picker");
const benSeat = await state(ben, () => tp.appStore.getState().picker?.chosen ?? null);
check(
  benSeat !== null && seats.some((t) => t.slot === benSeat.slot && t.districtX === benSeat.districtX),
  `Ben's picker opens at Ana's Yard (plot ${benSeat?.slot})`,
);
await ben.page.fill(".start-form input[minlength='3']", `Ben ${stamp}`);
await ben.page.click(".start-form button[type=submit]");
await ben.page.waitForSelector(".top-bar", { timeout: 30000 });
await ben.page.waitForTimeout(3000);
await ben.shot("4-ben-playing");

// Cleo: Find a player
const cleo = await start(`${out}/cleo`);
await cleo.page.goto(`${base}/`);
await cleo.page.waitForSelector(".auth-panel", { timeout: 60000 });
await cleo.page.click("text=New player");
await cleo.page.fill("input[autocomplete=username]", `cleo${stamp}`);
await cleo.page.fill("input[type=password]", "secret123");
await cleo.page.click("button[type=submit]");
await cleo.page.waitForSelector(".picker-top", { timeout: 30000 });
await cleo.page.fill(".find-player input", `Ana ${stamp}`);
await cleo.page.click(".find-player button[type=submit]");
await cleo.page.waitForSelector(".find-results button", { timeout: 15000 });
await cleo.shot("5-cleo-finds");
await cleo.page.click(".find-results button");
await cleo.page.waitForTimeout(3000);
await cleo.page.waitForSelector(".plot-card .start-form", { timeout: 30000 });
await cleo.page.waitForTimeout(4000);
await cleo.shot("6-cleo-card");
await cleo.page.fill(".start-form input[minlength='3']", `Cleo ${stamp}`);
await cleo.page.click(".start-form button[type=submit]");
await cleo.page.waitForSelector(".top-bar", { timeout: 30000 });
const cleoYard = await state(cleo, () => tp.appStore.getState().company.crew?.name ?? null);
check(cleoYard === yardName, `Cleo lives at ${cleoYard}`);

// Ana closes the Yard; Dev's Pick for me goes elsewhere, his tap on the last plot asks
const closed = await state(ana, () => tp.gameClient.connection.setYardOpen(false).then((r) => r.success || r.error));
check(closed === true, "Ana closes Kettle Yard");
const dev = await start(`${out}/dev`);
await dev.page.goto(`${base}/`);
await dev.page.waitForSelector(".auth-panel", { timeout: 60000 });
await dev.page.click("text=New player");
await dev.page.fill("input[autocomplete=username]", `dev${stamp}`);
await dev.page.fill("input[type=password]", "secret123");
await dev.page.click("button[type=submit]");
await dev.page.waitForSelector(".picker-top", { timeout: 30000 });
await dev.page.click("text=Pick for me");
await dev.page.waitForTimeout(1500);
const pick = await state(dev, () => tp.appStore.getState().picker.chosen);
check(!seats.some((t) => t.slot === pick.slot && t.districtX === pick.districtX && t.districtY === pick.districtY), `Pick for me keeps Dev out of the closed Yard (plot ${pick.slot})`);
// Dev looks for Ana: her Yard's last plot is closed to him, so it is a request
await dev.page.fill(".find-player input", `Ana ${stamp}`);
await dev.page.click(".find-player button[type=submit]");
await dev.page.waitForSelector(".find-results button", { timeout: 15000 });
await dev.page.click(".find-results button");
await dev.page.waitForTimeout(4000);
const free = await state(dev, (seats) => {
  const picker = tp.appStore.getState().picker;
  return seats.filter((t) => picker.seats[`${t.districtX},${t.districtY},${t.slot}`] === "closed");
}, seats);
check(free.length === 1, `one plot of Kettle Yard is left, and it is closed to Dev (${JSON.stringify(free.map((t) => t.slot))})`);
const devChosen = await state(dev, () => tp.appStore.getState().picker.chosen);
check(devChosen?.slot === free[0]?.slot, `Dev's picker offers that plot (${devChosen?.slot})`);
await dev.page.waitForSelector(".plot-card .start-form", { timeout: 30000 });
await dev.page.waitForTimeout(4000);
await dev.shot("7-dev-closed");
await dev.page.fill(".start-form input[minlength='3']", `Dev ${stamp}`);
await dev.page.click(".start-form button[type=submit]");
await dev.page.waitForSelector(".top-bar", { timeout: 30000 });
const devId = await state(dev, () => tp.appStore.getState().company.id);

// Ana: the request in the Crew panel, Yes
await ana.page.evaluate(() => tp.gameClient.openCrew());
await ana.page.waitForSelector(".crew-panel", { timeout: 15000 });
await ana.page.waitForTimeout(2000);
await ana.shot("8-ana-request");
await ana.page.click(".crew-panel button.primary.small");
await ana.page.waitForTimeout(2000);
await dev.page.evaluate(() => tp.gameClient.connection.cheat("arrive"));
await dev.page.waitForTimeout(3000);
const devYard = await state(dev, () => tp.gameClient.connection.getCompany().then((c) => c.crew?.name ?? null));
check(devYard === yardName, `Dev moved in: ${devYard}`);

// The Yard with its four residents
await ana.page.evaluate(() => tp.appStore.setState({ crewOpen: false }));
await ana.page.evaluate(() => tp.gameClient.openYard());
await ana.page.waitForTimeout(3000);
const residents = await state(ana, () => tp.appStore.getState().yard?.residents.length ?? 0);
check(residents === 4, `Kettle Yard has ${residents} residents`);
await ana.page.evaluate(() => {
  const c = tp.appStore.getState().company;
  const r = tp.layout.yardRect(c.districtX, c.districtY, Math.floor(c.slot / 4));
  tp.gameClient.engine.lookAt((r.x0 + r.x1) / 2, (r.y0 + r.y1) / 2, 0.12);
});
await ana.page.waitForTimeout(5000);
await ana.shot("9-ana-yard");
console.log("devId", devId);
for (const p of [ana, ben, cleo, dev]) {
  if (p.errors.length > 0) console.log("errors:", p.errors);
  await p.browser.close();
}

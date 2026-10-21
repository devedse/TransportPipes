// The first evening (v1.2 §5) with four browsers: Ana starts with friends and keeps three seats; Ben (on a phone) and
// Cleo open their codes and move in as guests; Houses kept their seats warm and leave them starter kits piped to the
// Yard; the pipes meet (Handshakes) and the Foundation buys from the station; Dev comes later with the last code and
// lands next to them. The "project" cheat finishes the Foundation: +3% for every seat and the Water Tower's card with
// its keystone: node evening.mjs <outdir>
import { start } from "./lib.mjs";
const [out = "shots/evening"] = process.argv.slice(2);
const base = process.env.BASE_URL ?? "http://localhost:5173";
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };
const events = async (player) => player.page.evaluate(() => [...document.querySelectorAll(".toast, .toasts *")].map((t) => t.textContent).join(" | "));

// Minute 0: Ana starts with friends
const ana = await start(out);
const user = `ev${Date.now() % 100000}`;
const crewName = `Kettle ${user.slice(2)}`;
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".auth-panel", { timeout: 30000 });
await ana.page.click("text=New player");
await ana.page.fill("input[autocomplete=username]", user);
await ana.page.fill("input[type=password]", "secret123");
await ana.page.click(".auth-panel button[type=submit]");
await ana.page.waitForSelector(".start-ways", { timeout: 30000 });
await ana.page.click("text=Start with friends");
await ana.page.waitForSelector(".seat-row.chosen", { timeout: 30000 });
await ana.page.fill(".crew-fields input", crewName);
await ana.page.getByLabel("Company name").fill(`${user} Works`);
await ana.page.click(".found-panel button[type=submit]");
await ana.page.waitForSelector(".share-panel", { timeout: 30000 });
const codes = await ana.page.$$eval(".held-seats .code", (t) => t.map((x) => x.textContent.trim()));
console.log("codes", codes);
await ana.shot("0-ana-codes");
await ana.page.click("text=Start playing");

/** A friend opens a code: "Move in", then "Done" */
async function friend(code, options) {
  const player = await start(out, options);
  await player.page.goto(`${base}/?seat=${code}`);
  await player.page.waitForSelector(".invite-panel", { timeout: 30000 });
  await player.page.click("text=Move in");
  await player.page.waitForSelector(".invite-panel form", { timeout: 30000 });
  await player.page.click(".invite-panel button[type=submit]");
  await player.page.waitForSelector(".top-bar", { timeout: 30000 });
  await player.page.waitForTimeout(2500);
  return player;
}

// Minutes 1–3: Ben and Cleo
const ben = await friend(codes[0], { width: 420, height: 860, touch: true });
await ben.shot("1-ben-in");
const cleo = await friend(codes[1]);
await cleo.shot("2-cleo-in");

const state = (player) => player.page.evaluate(() => {
  const s = tp.appStore.getState();
  const kit = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === s.company.id && t.type !== "hq" && t.type !== "tradingPost").length;
  return { name: s.company.name, slot: s.company.slot, crew: s.company.crew?.name ?? null, kit };
});
const people = [];
for (const player of [ana, ben, cleo]) people.push(await state(player));
console.log(people);
if (!people.every((t) => t.crew === crewName)) fail("all three in the crew");
if (people.slice(1).some((t) => t.kit === 0)) fail("Houses leave Ben and Cleo their starter kits");

// Minute 12: the Yard rounds; the pipes meet, the Foundation buys
await ana.cheat("yard");
await ana.cheat("yard");
await ana.page.waitForTimeout(2000);
const handshakes = await ana.page.evaluate(() => tp.appStore.getState().company.report?.notes ?? []);
const project = await ana.page.evaluate(async () => { await tp.gameClient.openProject(); return tp.appStore.getState().project; });
console.log("project", project.stage, project.basket.map((t) => `${t.resource} ${t.delivered.toFixed(1)}/${t.target}`), project.line);
await ana.page.waitForTimeout(800);
await ana.shot("3-ana-foundation");
if (!project.basket.some((t) => t.delivered > 0)) fail("the Foundation buys from the station's pipes at once");
const yard = await ana.page.evaluate(async () => { await tp.gameClient.openYard(); return tp.appStore.getState().yard; });
console.log("yard", yard.line, yard.residents.map((t) => `${t.name}${t.isNpc ? " ⚙" : ""}${t.connected ? "" : " (not piped)"}`));
await ana.page.waitForTimeout(800);
await ana.shot("4-ana-yard");

// About minute 20: the Foundation stands
await ana.cheat("project");
const tower = await ana.page.evaluate(async () => { await tp.gameClient.refreshProject(); return tp.appStore.getState().project; });
console.log("then", tower.stage, tower.line, "bonus", tower.bonus);
if (tower.stage !== "Water Tower") fail("the Water Tower comes next");
if (!tower.basket.some((t) => t.keystone)) fail("with its keystone: the crew's first decision");
await ana.page.evaluate(() => tp.gameClient.openProject());
await ana.page.waitForTimeout(800);
await ana.shot("5-ana-water-tower");

// Later: Dev opens the last code with nobody needing to be online for it
for (const player of [ben, cleo]) await player.browser.close();
const dev = await friend(codes[2]);
const devState = await state(dev);
console.log("Dev", devState);
if (devState.crew !== crewName) fail("Dev joins the crew");
await dev.cheat("yard");
await dev.page.waitForTimeout(1500);
await dev.shot("6-dev-in");
const notes = await dev.page.evaluate(() => tp.appStore.getState().company.report?.notes ?? []);
console.log("Dev's notes", notes.slice(-3));

// The map: the station with its Foundation and four friends
await ana.page.evaluate(() => tp.appStore.setState({ yardOpen: false }));
await ana.page.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x + 3, y + 3, 0.32), { x: yard.x0, y: yard.y0 });
await ana.page.waitForTimeout(2500);
await ana.shot("7-ana-station");

for (const [name, player] of [["Ana", ana], ["Dev", dev]]) {
  if (player.errors.length > 0) fail(`${name}: ${player.errors.join(" | ")}`);
  await player.browser.close();
}
console.log(handshakes.length ? `Ana's notes: ${handshakes.slice(-3).join(" / ")}` : "");
console.log(process.exitCode ? "done with failures" : "ok");

// 1.4 phase B, the economy, in the browser: the Company Level from gold earned on the top bar; the Lab's two lines
// (a track's tier and an Era tree item at once); the market's Hub Orders; the town's Wonder with a give switch; a level
// from 11 that waits for its goods; a workshop at the Yard's end and the Depot; an outskirts parcel bought in Era 2.
//   node economy.mjs shots/economy
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/economy";
const { browser, page, errors, shot, cheat } = await start(out);
const checks = [];
const check = (name, ok, detail = "") => {
  checks.push(ok);
  console.log(ok ? "ok  " : "FAIL", name, detail);
};
const state = () => page.evaluate(() => tp.appStore.getState().company);
const wait = (ms = 1200) => page.waitForTimeout(ms);

await newPlayer(page, "econ");
for (let i = 0; i < 2; i++) await cheat("level");
await cheat("earn");
await wait();
let company = await state();
check("the Company Level comes from the gold earned", company.level >= 11 && company.lifetimeEarnings >= company.levelEarned, `level ${company.level}, ${Math.round(company.lifetimeEarnings)} g earned`);
await shot("1-top-bar");

// The Lab: build one, then a track's tier and an Era tree item on its two lines
const lab = await page.evaluate(async () => {
  const s = () => tp.appStore.getState();
  const w = tp.gameClient.world;
  const co = s().company;
  const definition = s().content.building("lab");
  for (let r = 1; r < 20; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const x = co.estateX + 16 + dx;
        const y = co.estateY + 16 + dy;
        if (tp.rules.canPlace(s().content, w, co, s().era, definition, x, y).ok) {
          const result = await tp.gameClient.connection.placeBuilding("lab", x, y);
          return result.success ? "built" : result.error;
        }
      }
  return "no spot";
});
await cheat("finish");
await wait(2000);
await page.evaluate(() => tp.gameClient.openResearch());
await wait(1500);
await shot("2-research");
await page.locator(".tech.track", { hasText: "Power" }).locator("button.primary").click();
await wait(800);
await page.locator(".tech", { hasText: "Valves" }).locator("button.primary").click();
await wait(1500);
company = await state();
check("a Lab", lab === "built", lab);
check("two lines at once", company.research.currentTrack === "power" && company.research.currentResearch === "valves", `${company.research.currentTrack} · ${company.research.currentResearch}`);
await shot("3-research-two-lines");
await page.keyboard.press("Escape");

// The market: everyone's own Hub Orders
await page.evaluate(() => tp.gameClient.openHub("market"));
await wait(1500);
const market = await page.evaluate(() => tp.appStore.getState().market);
check("the market lists Hub Orders", market.resources.some((t) => t.hubOrder > 0), `${market.resources.length} goods`);
await shot("4-market");

// The town and its Wonder: a give switch
await page.evaluate(() => tp.appStore.setState({ hubTab: "wonders" }));
await wait(1500);
const town = await page.evaluate(() => tp.appStore.getState().town);
check("the town's Wonder", town?.wonder !== null, town?.wonder?.name ?? "none");
await shot("5-town");
const good = town.wonder.needs[0].resource;
await page.locator(".wonder-need").first().locator("button", { hasText: "All" }).click();
await wait(1500);
company = await state();
check("a give switch", company.give?.[good] === "all", `${good}: ${company.give?.[good]}`);
await page.keyboard.press("Escape");

// A level from 11: the gold at once, the goods come like materials (the market brings them)
const moily = await page.evaluate(async () => {
  const s = () => tp.appStore.getState();
  const me = s().company.id;
  // A producer of the starter kit, or a Solar Panel built for it
  const producer = (t) => t.owner === me && s().content.building(t.type)?.kind === "producer" && Object.keys(s().content.building(t.type).out).length > 0;
  let building = [...tp.gameClient.world.buildings.values()].find(producer);
  if (building === undefined) {
    const co = s().company;
    const definition = s().content.building("solarPanel");
    let placed = false;
    for (let r = 2; r < 20 && !placed; r++)
      for (let dy = -r; dy <= r && !placed; dy++)
        for (let dx = -r; dx <= r && !placed; dx++) {
          const x = co.estateX + 16 + dx;
          const y = co.estateY + 16 + dy;
          if (tp.rules.canPlace(s().content, tp.gameClient.world, co, s().era, definition, x, y).ok) {
            placed = (await tp.gameClient.connection.placeBuilding("solarPanel", x, y)).success;
          }
        }
    await tp.gameClient.connection.cheat("finish");
    await new Promise((r) => setTimeout(r, 1500));
    building = [...tp.gameClient.world.buildings.values()].find(producer);
  }
  if (building === undefined) return null;
  for (let i = 0; i < 40 && s().company.gold > 0; i++) {
    const level = tp.gameClient.world.buildings.get(building.id)?.level ?? 0;
    if (level >= 10) break;
    await tp.gameClient.connection.upgrade(building.id, 10 - level);
    await new Promise((r) => setTimeout(r, 300));
  }
  const result = await tp.gameClient.connection.upgrade(building.id, 1);
  tp.gameClient.select(building.id);
  return { id: building.id, result: result.success ? "ok" : result.error };
});
await wait(2500);
const details = await page.evaluate(() => tp.appStore.getState().buildingDetails);
check("a level from 11 waits for its goods", details?.levelWait !== null && details?.levelWait?.boughtLevel === 11, JSON.stringify(moily));
await shot("6-level-wait");

// The Yard's end: a workshop, and the Depot
await page.evaluate(() => tp.gameClient.select(null));
const yard = await page.evaluate(async () => {
  const s = () => tp.appStore.getState();
  const co = s().company;
  const w = tp.gameClient.world;
  const tryBuild = async (type) => {
    const definition = s().content.building(type);
    for (let y = co.estateY - 120; y < co.estateY + 150; y++)
      for (let x = co.estateX - 150; x < co.estateX + 150; x++)
        if (tp.rules.inOwnWorkshops(co, x, y) && tp.rules.canPlace(s().content, w, s().company, s().era, definition, x, y).ok) {
          const result = await tp.gameClient.connection.placeBuilding(type, x, y);
          return result.success ? { x, y } : result.error;
        }
    return "no spot";
  };
  return { workshop: await tryBuild("windMoily"), depot: await tryBuild("yardDepot") };
});
await cheat("finish");
await wait(2500);
await page.evaluate(() => tp.gameClient.openYard());
await wait(2000);
company = await state();
check("a workshop at the Yard's end", typeof yard.workshop === "object" && company.workshops.tilesUsed === 1, JSON.stringify(yard.workshop));
check("the Depot", company.workshops.depot !== null, JSON.stringify(yard.depot));
if (typeof yard.depot === "object") {
  await page.evaluate(({ x, y }) => tp.gameClient.engine?.lookAt(x, y, 0.9), yard.depot);
}
await wait(2500);
await shot("7-yard-depot");

// Era 2: the outskirts (Survey from the Era tree)
await cheat("era");
await cheat("eratech");
for (let i = 0; i < 110; i++) await cheat("earn");
await wait(2000);
const parcel = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const corner = tp.layout.outskirtsParcels(co.slot)[0];
  return { x: tp.layout.districtOriginX(co.districtX) + corner.x + 3, y: tp.layout.districtOriginY(co.districtY) + corner.y + 3 };
});
await page.evaluate(({ x, y }) => {
  tp.gameClient.engine?.lookAt(x, y, 0.8);
  tp.appStore.setState({ selectedOutskirts: 0 });
}, parcel);
await wait(2000);
await shot("8-outskirts-panel");
await page.locator(".parcel-panel button.primary").click();
await wait(2000);
company = await state();
check("an outskirts parcel bought", (company.outskirts?.parcels ?? 0) === 1, company.outskirts?.blocker ?? "");
await shot("9-outskirts-bought");

const pass = checks.every(Boolean) && errors.length === 0;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

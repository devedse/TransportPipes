// 1.4 phase C in the browser: a plot through Eras 2 to 5 with each Era's buildings (a screenshot each), the fog over the
// wilderness your expeditions haven't mapped, and a trunk line built from the Links tab to the next town, drawn on the map.
//   node eras.mjs shots/eras
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/eras";
const { browser, page, errors, shot, cheat } = await start(out);
const checks = [];
const check = (name, ok, detail = "") => {
  checks.push(ok);
  console.log(ok ? "ok  " : "FAIL", name, detail);
};
const wait = (ms = 1500) => page.waitForTimeout(ms);

await newPlayer(page, "eras");
for (let i = 0; i < 6; i++) await cheat("level");
for (let i = 0; i < 20; i++) await cheat("gold");
await page.evaluate(async () => {
  for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0);
});

/** Builds these near the trading post (their materials bought at once) and looks at them */
async function buildEra(types) {
  return page.evaluate(async (types) => {
    const c = tp.gameClient.connection;
    const s = () => tp.appStore.getState();
    const w = tp.gameClient.world;
    const placed = [];
    for (const type of types) {
      const definition = s().content.building(type);
      const co = s().company;
      let done = false;
      for (let r = 2; r < 16 && !done; r++)
        for (let dy = -r; dy <= r && !done; dy++)
          for (let dx = -r; dx <= r && !done; dx++) {
            if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue;
            const x = co.estateX + 16 + dx;
            const y = co.estateY + 16 + dy;
            if (tp.rules.canPlace(s().content, w, co, s().era, definition, x, y).ok && (await c.placeBuilding(type, x, y)).success) {
              placed.push(type);
              done = true;
            }
          }
    }
    for (const b of [...w.buildings.values()].filter((t) => t.owner === s().company.id && t.siteShare !== null)) await c.orderRest(b.id);
    await c.cheat("finish");
    const co = s().company;
    tp.gameClient.engine.lookAt(co.estateX + 16, co.estateY + 16, 0.6);
    return placed;
  }, types);
}

const eras = [
  [1, ["windMoily", "ironMine", "smallFactory", "brickworks", "glassworks", "storage"]],
  [2, ["steelMill", "wireMill", "cementKiln", "aluminiumWorks"]],
  [3, ["refinery", "machineShop", "motorWorks"]],
  [4, ["circuitFab", "batteryWorks", "robotWorks"]],
  [5, ["titaniumWorks", "superconductorLab", "spaceframeWorks"]],
];
for (const [era, types] of eras) {
  if (era > 1) await cheat("era");
  await cheat("eratech");
  for (let i = 0; i < 30; i++) await cheat("earn");
  const placed = await buildEra(types);
  await wait(4000);
  const company = await page.evaluate(() => tp.appStore.getState().company);
  check(`Era ${era}: its buildings`, company.era === era && placed.length === types.length, `${placed.join(", ")} · town Era ${company.era}`);
  await shot(`era-${era}`);
}

// The fog: the wilderness around the town, nothing mapped yet
await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  tp.gameClient.engine.lookAt(tp.layout.districtOriginX(co.districtX) - 60, co.estateY + 30, 0.18);
});
await wait(4000);
const explored = await page.evaluate(() => tp.appStore.getState().company.explored?.length ?? 0);
check("the wilderness starts as fog", explored === 0, `${explored} pieces mapped`);
await shot("fog");

// A trunk line to the next town, from the Links tab
await cheat("district");
await cheat("stock");
await page.evaluate(() => tp.gameClient.openLinks());
await wait(2500);
await shot("links-tab");
const button = page.locator(".line-option button.primary").first();
const canBuild = (await button.count()) > 0;
if (canBuild) {
  await button.click();
  await wait(2500);
}
const lines = await page.evaluate(() => tp.appStore.getState().lines);
check("a trunk line built from the Links tab", (lines?.lines ?? []).some((t) => t.yours), JSON.stringify(lines?.options ?? []).slice(0, 200));
await shot("line-built");
await page.keyboard.press("Escape");
await page.evaluate(() => {
  const line = tp.appStore.getState().lines?.lines.find((t) => t.yours);
  if (line) tp.gameClient.engine.lookAt(line.route[0], line.route[1], 0.14);
});
await wait(4000);
await shot("line-on-map");

const pass = checks.every(Boolean) && errors.length === 0;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

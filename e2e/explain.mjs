// Every game word says what it is on hover (the owner, 2026-10-31: "a yard round ... needs user explanations ... add
// an on hover here ... and go through all other things"): the top bar's Yard round, gold, Company Level and Era, the
// map overlays, and in a network panel the underlined words (spare); and a Lumber Camp by too little forest says how
// many forest tiles there are and how many it needs ("can't place a lumbercamp near a forest even though there is a
// forest").   node explain.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/explain"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };
const popup = async (selector) => {
  await page.mouse.move(10, 600);
  await page.waitForTimeout(200);
  await page.locator(selector).first().hover();
  await page.waitForTimeout(400);
  return (await page.locator(".goods-tip").first().innerText().catch(() => "")).replace(/\s+/g, " ");
};

await newPlayer(page, "ex");
for (const c of ["level", "level", "gold"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
console.log(await kit(page, ["windMoily", "windMoily"]));
await page.waitForTimeout(2000);

const yard = await popup(".top-yard");
console.log("yard round:", yard.slice(0, 160));
check(/Yard moves those goods in rounds/.test(yard) && /once a minute/.test(yard), "the Yard round says what it is", yard.slice(0, 120));
await shot("1-yard-round");
const gold = await popup(".top-gold");
check(/what you earn a minute/.test(gold), "the gold says what its numbers are", gold.slice(0, 120));
const level = await popup(".top-level");
check(/all the gold your company ever earned/.test(level), "the Company Level says where it comes from", level.slice(0, 120));
const era = await popup(".top-era");
check(/An Era is a stage of the game/.test(era), "the Era says what it is", era.slice(0, 120));
const deposits = await popup(".overlay-bar .chip:nth-child(2), .chip:has-text('Deposits')");
check(/in the ground/.test(deposits), "the Deposits overlay says what it shows", deposits.slice(0, 120));

// A network's underlined words
const moily = await page.evaluate(() => [...tp.gameClient.world.buildings.values()].find((t) => t.owner === tp.appStore.getState().company.id && t.type === "windMoily")?.id);
await page.evaluate((id) => tp.gameClient.showBuilding(id), moily);
await page.waitForSelector(".net-res .term", { timeout: 15000 });
const spare = await popup(".net-res .term");
check(/^Spare|^Short/.test(spare), "a network's spare or short says what it is", spare.slice(0, 120));
await shot("2-term");
await page.keyboard.press("Escape");

// A Lumber Camp by too little forest: how many tiles there are, how many it needs
const forest = await page.evaluate(() => {
  const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s.company.id && t.type === "tradingPost");
  const definition = s.content.building("lumberCamp");
  for (let r = 1; r < 30; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const n = tp.rules.forestAround(w, post.x + dx, post.y + dy, definition.size);
        if (n < 1 || n > 2) continue;
        const result = tp.rules.canPlace(s.content, w, s.company, s.era, definition, post.x + dx, post.y + dy);
        if (!result.ok && /forest/.test(result.reason)) return { n, reason: result.reason };
      }
  return null;
});
console.log("forest:", JSON.stringify(forest));
check(forest === null || new RegExp(`Needs 3 forest tiles within 2 tiles of it: here there (is|are) ${forest.n}`).test(forest.reason), "too little forest: how many there are and how many it needs", JSON.stringify(forest));
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

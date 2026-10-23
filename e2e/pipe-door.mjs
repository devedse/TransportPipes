// The pipe door (after run 4: the pipe tier is the game's real level cap, and the panel said "+0 g/min: its pipes carry
// 205 iron ore a minute" with no button): a Wind Moily that makes more than its grey pipe carries shows, in its upgrade
// section, one button "Copper Pipe on its link (N tiles), then Level L · price + ingots" with its preview in g/min; the
// status above has no second button; the tracker has a Pipes row; one tap lays the copper and buys the levels. At the
// Copper Pipe's limit the door is Widen ×2 for an hour of income, then ×4.
//   node pipe-door.mjs shots/pipe-door
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/pipe-door";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "door");
for (const c of ["gold", "earn", "earn", "earn", "level", "level", "level", "level"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

// A Storage with copper ingots, and the starter kit's first Wind Moily upgraded far past what a grey pipe carries
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const co = tp.appStore.getState().company;
  await c.skipTutorial();
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  for (let d = 3; d < 14; d++) {
    if ((await c.placeBuilding("storage", post.x - d, post.y + 1)).success) break;
  }
  await c.cheat("finish");
  await c.cheat("stock");
  const m = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  // Its trading post sells far more than a pipe brings
  await c.upgrade(post.id, 20);
  return { moily: m.id, level: tp.appStore.getState().company.level, copper: tp.appStore.getState().company.researched.includes("copperPipes") };
});
console.log("setup:", JSON.stringify(setup));
expect("Copper Pipes come with Company Level 8, no Lab", setup.level >= 8 && setup.copper, `Level ${setup.level}`);
const moily = setup.moily;
// Levels in steps of ten, with gold for each
const raise = (levels) =>
  page.evaluate(
    async ({ id, levels }) => {
      const c = tp.gameClient.connection;
      const done = [];
      for (let left = levels; left > 0; left -= 10) {
        for (let i = 0; i < 5; i++) await c.cheat("earn");
        done.push((await c.upgrade(id, Math.min(10, left))).success);
      }
      return done;
    },
    { id: moily, levels },
  );
console.log("to level 21:", JSON.stringify(await raise(20)));
await page.waitForTimeout(4000);
const open = async () => {
  await page.evaluate((id) => tp.gameClient.showBuilding(id), moily);
  await page.waitForSelector(".building-panel", { timeout: 15000 });
  await page.waitForTimeout(2500);
};
const doorOf = () => page.evaluate(() => tp.appStore.getState().buildingDetails?.pipeDoor ?? null);
await open();
let door = await doorOf();
console.log("door:", JSON.stringify(door));
const button = page.locator(".building-panel .pipe-door button.primary");
const label = await button.innerText().catch(() => "");
expect("a full grey pipe's door: Copper Pipe, then the levels it carries", door?.kind === "tier" && /^Copper Pipe on its link \(\d+ tiles?\), then Level \d+ · [\d.,kM]+ g \+ \d+ copper ingots$/.test(label), label);
const preview = await page.locator(".building-panel .pipe-door .spend-preview").innerText().catch(() => "");
expect("its preview in gold a minute, and when it pays back", /^\+[\d.,k]+ g\/min · pays back in /.test(preview), preview);
expect("no second button in the status", (await page.locator(".building-panel button.fix").count()) === 0);
const words = await page.locator(".building-panel .spend-preview").first().innerText().catch(() => "");
expect("the next level says it adds nothing, in words", !/\+0 g\/min/.test(words), words);
const row = await page.locator(".goal-tracker li", { hasText: "Pipes" }).innerText().catch(() => "");
expect("the tracker has a Pipes row", /🔧 Your Wind Moily's pipe is full · Copper Pipe on its link/.test(row), row.replace(/\s+/g, " "));
await shot("1-grey-door");

const level = await page.evaluate((id) => tp.gameClient.world.buildings.get(id).level, moily);
await button.click();
await page.waitForTimeout(4000);
await open();
const laid = await page.evaluate((id) => ({ level: tp.gameClient.world.buildings.get(id).level, door: tp.appStore.getState().buildingDetails?.pipeDoor ?? null }), moily);
console.log("after:", JSON.stringify(laid));
expect("one tap laid the copper and bought the levels", laid.level === door.toLevel && laid.level > level, `level ${level} → ${laid.level}`);
await shot("2-copper");

console.log("to level 61:", JSON.stringify(await raise(40)));
await page.waitForTimeout(4000);
await open();
door = await doorOf();
const widen = await button.innerText().catch(() => "");
expect("at the Copper Pipe's limit the door widens it", door?.kind === "widen" && /^Widen its link ×[24]/.test(widen), widen);
const before = await page.evaluate((id) => tp.gameClient.world.buildings.get(id).level, moily);
await button.click();
await page.waitForTimeout(4000);
await open();
const widened = await page.evaluate((id) => ({ level: tp.gameClient.world.buildings.get(id).level, door: tp.appStore.getState().buildingDetails?.pipeDoor ?? null }), moily);
expect("one tap widened it, and bought the levels it carries", widened.door?.kind !== "widen" || /^Widen its link ×4/.test(widened.door.text) || widened.level > before, JSON.stringify(widened));
await shot("3-widened");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

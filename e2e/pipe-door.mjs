// The pipe door (after run 4: the pipe tier is the game's real level cap, and the panel said "+0 g/min: its pipes carry
// 205 iron ore a minute" with no button): a Wind Moily that makes more than its grey pipe carries shows, in its upgrade
// section, one button "Copper Pipe on its link (N tiles), then Level L · price + ingots" with its preview in g/min; the
// status above has no second button; the tracker has a Pipes row; one tap lays the copper and buys the levels. (1.4:
// pipes carry per building pair, levels end at 25 and need goods from 11, which a Storage gives; Widen is retired.)
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
  // Away from the Moily's pipes (a level's goods come from any Storage; one on its pipes would take its energy)
  const s = () => tp.appStore.getState();
  const definition = s().content.building("storage");
  const spots = [];
  for (let dy = -15; dy <= 15; dy++)
    for (let dx = -15; dx <= 15; dx++)
      if (Math.max(Math.abs(dx), Math.abs(dy)) >= 9 && tp.rules.canPlace(s().content, w, co, s().era, definition, post.x + dx, post.y + dy).ok)
        spots.push({ x: post.x + dx, y: post.y + dy });
  let storage = null;
  for (const { x, y } of spots) {
    if ((await c.placeBuilding("storage", x, y)).success) {
      storage = { x, y };
      break;
    }
  }
  await c.cheat("finish");
  // Copper ingots for the pipe, planks and bricks for the levels past 10
  const stock = await c.cheat("stock");
  await c.upgrade(post.id, 20);
  return { storage, stock: stock.success, level: s().company.level, copper: s().company.research.researched.includes("copperPipes") };
});
// A Wind Moily by the trading post, on one grey pipe to it (1.4: a plot starts empty; a pipe carries 100 a minute
// between two buildings, so the Moily's only pair is its limit)
setup.moily = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const definition = s().content.building("windMoily");
  // The windiest spot near the post: on a calm one a Level 21 Moily makes less than the grey pipe carries
  const spots = [];
  for (let dy = -9; dy <= 9; dy++)
    for (let dx = -9; dx <= 9; dx++) {
      if (Math.max(Math.abs(dx), Math.abs(dy)) < 3) continue;
      const placement = tp.rules.canPlace(s().content, w, s().company, s().era, definition, post.x + dx, post.y + dy);
      if (placement.ok) spots.push({ x: post.x + dx, y: post.y + dy, wind: placement.multiplier });
    }
  spots.sort((a, b) => b.wind - a.wind);
  for (const { x, y } of spots) {
    if (!(await c.placeBuilding("windMoily", x, y)).success) continue;
    await c.cheat("finish");
    for (const [fx, fy] of [[x, y - 1], [x, y + 1], [x - 1, y], [x + 1, y]]) {
      const route = tp.rules.routePipe(w, s().company, { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) break;
    }
    return [...w.buildings.values()].find((t) => t.x === x && t.y === y)?.id;
  }
  return undefined;
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
      // The goods of levels past 10 (the trading post's levels took the first stock)
      await c.cheat("stock");
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
const row = await page
  .locator(".goal-tracker li")
  .filter({ has: page.locator(".goal-label", { hasText: /^Pipes$/ }) })
  .innerText()
  .catch(() => "");
expect("the tracker has a Pipes row", /🔧 Your .+'s pipe is full · /.test(row), row.replace(/\s+/g, " "));
await shot("1-grey-door");

const level = await page.evaluate(() => tp.appStore.getState().buildingDetails?.levelWait?.boughtLevel ?? 0);
await button.click();
await page.waitForTimeout(4000);
await open();
// Levels past 10 come once their goods are in (1.4 §9): the bought level is what the tap bought
const laid = await page.evaluate((id) => {
  const s = tp.appStore.getState();
  const w = tp.gameClient.world;
  const tiers = [...w.conductors.values()].filter((t) => t.owner === s.company.id && t.kind === "pipe").map((t) => t.tier);
  return { level: s.buildingDetails?.levelWait?.boughtLevel ?? w.buildings.get(id).level, copper: tiers.filter((t) => t === 2).length, grey: tiers.filter((t) => t === 1).length, door: s.buildingDetails?.pipeDoor ?? null };
}, moily);
console.log("after:", JSON.stringify(laid));
expect("one tap laid the copper and bought the levels", laid.level === door.toLevel && laid.copper > 0, `bought level ${level} → ${laid.level} · ${laid.copper} copper tiles`);
await shot("2-copper");

// Past copper: no Widen any more; the next pipe is Era 2's Steel Pipe (a Moily at level 25 stays under copper's 5,000 a minute)
const after = await doorOf();
expect("past copper, nothing to widen", after === null || after.kind !== "widen", JSON.stringify(after));

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

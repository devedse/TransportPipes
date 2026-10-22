// A dead end explains itself (F2): Ana's day-4 layout (an Iron Mine piped to the trading post and a windmill, a Small
// Factory piped to the mine and another windmill: its ingots can't leave), then the panel's pill, reach line and one
// fix button, the planned pipe as a ghost, and the factory running after one more click.
//   node deadend.mjs shots/deadend
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/deadend";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "dead");
await cheat("level");
await cheat("gold");

const placed = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const co = () => s().company;
  const content = s().content;
  const ok = (type, x, y) => tp.rules.canPlace(content, w, co(), s().era, content.building(type), x, y).ok;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  // The mine on an iron deposit near the post
  let mine = null;
  for (let r = 3; r < 14 && !mine; r++)
    for (let dy = -r; dy <= r && !mine; dy++)
      for (let dx = -r; dx <= r && !mine; dx++) {
        const x = post.x + dx, y = post.y + dy;
        if (w.tile(x, y)?.deposit === "ironOre" && ok("ironMine", x, y)) mine = { x, y };
      }
  if (!mine) return "no iron deposit";
  const place = async (type, x, y) => {
    const r = await c.placeBuilding(type, x, y);
    if (!r.success) throw new Error(`${type}: ${r.error}`);
    await c.cheat("finish");
  };
  const pipe = async (from, to) => {
    const route = tp.rules.routePipe(w, co(), from, to, content);
    if (!route) throw new Error(`no route ${JSON.stringify(from)} → ${JSON.stringify(to)}`);
    const r = await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1);
    if (!r.success) throw new Error(r.error);
    await new Promise((res) => setTimeout(res, 300));
  };
  const isPipe = (x, y) => w.conductorAt(x, y)?.kind === "pipe";
  const freeTile = (x, y) => !w.buildingAt(x, y) && !isPipe(x, y);
  // No pipe next to a tile except where we want one (pipes next to each other join into one pipe group)
  const lonely = (x, y, except) => [[1, 0], [-1, 0], [0, 1], [0, -1]].every(([dx, dy]) => {
    const nx = x + dx, ny = y + dy;
    return (except && nx === except.x && ny === except.y) || !isPipe(nx, ny);
  });
  const dirs = [[1, 0], [-1, 0], [0, 1], [0, -1]];
  // A one-tile pipe from a building at (x, y) to a new building two tiles off, that touches nothing else
  async function besides(at, type) {
    for (const [dx, dy] of dirs) {
      const p = { x: at.x + dx, y: at.y + dy };
      const b = { x: at.x + 2 * dx, y: at.y + 2 * dy };
      if (!freeTile(p.x, p.y) || !lonely(p.x, p.y) || !ok(type, b.x, b.y) || !lonely(b.x, b.y)) continue;
      if (dirs.some(([ex, ey]) => { const n = { x: p.x + ex, y: p.y + ey }; return !(n.x === at.x && n.y === at.y) && !(n.x === b.x && n.y === b.y) && w.buildingAt(n.x, n.y); })) continue;
      await place(type, b.x, b.y);
      const r = await c.placePipes([p.x, p.y], 1);
      if (!r.success) throw new Error(r.error);
      await new Promise((res) => setTimeout(res, 300));
      return b;
    }
    throw new Error(`no room for a ${type} beside ${JSON.stringify(at)}`);
  }
  await place("ironMine", mine.x, mine.y);
  await pipe({ x: mine.x, y: mine.y }, { x: post.x, y: post.y });
  // Ana's day 4: a windmill for the mine, the factory on its own pipe to the mine, and the factory's windmill
  await besides(mine, "windMoily");
  const factory = await besides(mine, "smallFactory");
  await besides(factory, "windMoily");
  await new Promise((res) => setTimeout(res, 1500));
  const id = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "smallFactory").id;
  tp.gameClient.showBuilding(id);
  return JSON.stringify({ mine, factory, id });
});
console.log("placed", placed);
await page.waitForSelector(".building-panel .idle-status", { timeout: 15000 }).catch(async () => { await shot("0-no-status"); console.log("panel text:", await page.textContent(".building-panel").catch(() => "no panel")); });
await page.waitForTimeout(1500);
const head = await page.evaluate(() => ({
  pill: document.querySelector(".building-panel .idle-status .status")?.textContent,
  reach: document.querySelector(".building-panel .idle-status .reach")?.textContent,
  fix: document.querySelector(".building-panel .idle-status .fix")?.textContent,
}));
console.log("panel", JSON.stringify(head));
await shot("1-stuck-panel");

await page.click(".building-panel .idle-status .fix");
await page.waitForSelector(".route-banner", { timeout: 10000 });
await page.waitForTimeout(800);
console.log("banner", await page.textContent(".route-banner"));
await shot("2-planned-pipe");
await page.click(".route-banner button.primary");
await page.waitForTimeout(4000);
const after = await page.evaluate(() => ({
  pill: document.querySelector(".building-panel .idle-status .status")?.textContent ?? null,
  status: document.querySelector(".building-panel .status")?.textContent ?? null,
}));
console.log("after", JSON.stringify(after));
await shot("3-running");

const pass = head.pill?.includes("Stuck: iron ingots can't leave") && head.fix?.startsWith("Lay the pipe to your Trading Post") && after.pill === null;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

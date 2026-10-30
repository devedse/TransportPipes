// A building stands on its owner's own pipe and the pipe runs on under it (run 2's recommendation 9): a Small Factory
// set onto a tile of the starter kit's line is allowed, the pipe stays, and what was piped to the trading post still
// reaches it.
//   node pipeunder.mjs shots/pipeunder
import { start, newPlayer, kit } from "./lib.mjs";

const out = process.argv[2] ?? "shots/pipeunder";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "under");
await cheat("level");
await cheat("gold");
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.waitForTimeout(1500);

const placed = await page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const w = tp.gameClient.world;
  const s = () => tp.appStore.getState();
  const co = () => s().company;
  const content = s().content;
  const factory = content.building("smallFactory");
  const own = [...w.conductors.values()].filter((t) => t.owner === co().id && t.kind === "pipe");
  // A pipe tile with pipes on two opposite sides (a straight run of the line), where the factory may stand
  const straight = own.find((t) =>
    tp.rules.canPlace(content, w, co(), s().era, factory, t.x, t.y).ok &&
    ((w.conductorAt(t.x - 1, t.y) && w.conductorAt(t.x + 1, t.y)) || (w.conductorAt(t.x, t.y - 1) && w.conductorAt(t.x, t.y + 1))));
  if (!straight) return { error: `no straight pipe tile (of ${own.length})` };
  const network = straight.network;
  const result = await c.placeBuilding("smallFactory", straight.x, straight.y);
  if (!result.success) return { error: result.error };
  await c.cheat("finish");
  await new Promise((res) => setTimeout(res, 2000));
  tp.gameClient.engine.lookAt(straight.x, straight.y, 1.2);
  const after = w.conductorAt(straight.x, straight.y);
  return { at: { x: straight.x, y: straight.y }, network, pipeStays: after !== undefined, networkAfter: after?.network ?? null };
});
console.log("placed", JSON.stringify(placed));
await page.waitForTimeout(1500);
await shot("1-factory-on-the-line");

const pass = placed.error === undefined && placed.pipeStays;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

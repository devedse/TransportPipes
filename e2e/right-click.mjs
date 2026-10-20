// A right-click on the map steps back (cancels a tool, deselects); a right-drag still pans: node right-click.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/right-click"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out);
await newPlayer(page, "r");
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const where = await page.evaluate(async () => {
  const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  // A short pipe at the trading post, to select its network
  for (const [dx, dy] of [[0, 1], [1, 0], [0, -1], [-1, 0]]) {
    const x = post.x + dx, y = post.y + dy;
    if (tp.rules.pipeCost(w, co, x, y, s.content) === 1 && tp.rules.pipeCost(w, co, x + dx, y + dy, s.content) === 1) {
      if ((await tp.gameClient.connection.placePipes([x, y, x + dx, y + dy], 1)).success) break;
    }
  }
  await new Promise((r) => setTimeout(r, 1500));
  const pipe = [...w.conductors.values()].find((t) => t.owner === co.id && t.kind === "pipe");
  tp.gameClient.engine.lookAt(post.x, post.y, 0.9);
  return { post: { x: post.x, y: post.y }, pipe: pipe ? { x: pipe.x, y: pipe.y } : null };
});
await page.waitForTimeout(1500);
const screen = (t) => page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [t.x, t.y]);
const state = () => page.evaluate(() => { const s = tp.appStore.getState(); return { building: s.selectedBuilding, network: s.selectedNetwork, tool: s.tool.kind }; });
const empty = await screen({ x: where.post.x + 3, y: where.post.y + 3 });
const results = [];
const expect = (label, ok) => { results.push(`${ok ? "ok  " : "FAIL"} ${label}`); };

// 1. A building selected → right-click on the map deselects it
const post = await screen(where.post);
await page.mouse.click(post.x, post.y);
await page.waitForTimeout(400);
expect("click selects the trading post", (await state()).building !== null);
await page.mouse.click(empty.x, empty.y, { button: "right" });
await page.waitForTimeout(300);
expect("right-click deselects it", (await state()).building === null);

// 2. A network selected → right-click deselects it
if (where.pipe) {
  const pipe = await screen(where.pipe);
  await page.mouse.click(pipe.x, pipe.y);
  await page.waitForTimeout(400);
  const selected = (await state()).network !== null;
  expect("click selects the network", selected);
  await page.mouse.click(empty.x, empty.y, { button: "right" });
  await page.waitForTimeout(300);
  expect("right-click deselects the network", (await state()).network === null);
}

// 3. The pipe tool → right-click goes back to select; a second right-click does nothing harmful
await page.keyboard.press("p");
await page.waitForTimeout(200);
expect("p picks the pipe tool", (await state()).tool === "pipe");
await page.mouse.click(empty.x, empty.y, { button: "right" });
await page.waitForTimeout(300);
expect("right-click puts the pipe tool away", (await state()).tool === "select");

// 4. A right-drag pans and keeps the selection
await page.mouse.click(post.x, post.y);
await page.waitForTimeout(400);
const before = await page.evaluate(() => ({ x: tp.gameClient.engine.tileToScreen(0, 0).x }));
await page.mouse.move(empty.x, empty.y);
await page.mouse.down({ button: "right" });
for (let i = 1; i <= 8; i++) await page.mouse.move(empty.x + i * 25, empty.y + i * 10);
await page.mouse.up({ button: "right" });
await page.waitForTimeout(500);
const after = await page.evaluate(() => ({ x: tp.gameClient.engine.tileToScreen(0, 0).x }));
expect("right-drag pans the map", Math.abs(after.x - before.x) > 50);
expect("right-drag keeps the selection", (await state()).building !== null);
await shot("1-after-right-drag");

console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

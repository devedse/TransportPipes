// Connectors on the far sides of a pipe (the building behind it, up the screen) go behind the pipe: the belt, the
// cables and the goods on them hide behind the tube instead of being drawn over it.
// node connector-depth.mjs shots/connector-depth (BEFORE=1 captures the old layering, without the order check).
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/connector-depth";
const { browser, page, errors, shot, cheat } = await start(out);
let failed = 0;
const check = (ok, message) => { console.log(`${ok ? "ok" : "FAIL"} ${message}`); if (!ok) failed++; };
await newPlayer(page, "depth");
for (const command of ["gold", "gold", ...Array(8).fill("level")]) await cheat(command);
const fixture = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const state = () => tp.appStore.getState();
  const co = state().company;
  for (let p = 0; p < 16; p++) await c.buyParcel(p, 0);
  const post = [...w.buildings.values()].find((b) => b.owner === co.id && b.type === "tradingPost");
  const made = [];
  // The pipe leaves each building from its +x (far side "l" for the pipe) or +y (far side "u") edge.
  for (const [type, edge] of [["storage", null], ["windMoily", null], ["smallFactory", "x"], ["waterPump", "y"], ["ironMine", "x"]]) {
    const definition = state().content.building(type);
    const spots = [];
    for (let y = co.estateY + 2; y < co.estateY + 29; y++) for (let x = co.estateX + 2; x < co.estateX + 29; x++) {
      if (made.some((b) => Math.abs(x - b.x) < 4 && Math.abs(y - b.y) < 4)) continue;
      if (type === "ironMine" && w.tile(x, y)?.deposit !== "ironOre") continue;
      if (tp.rules.canPlace(state().content, w, co, state().era, definition, x, y).ok)
        spots.push({ x, y, distance: Math.abs(x - post.x) + Math.abs(y - post.y) });
    }
    const at = spots.sort((a, b) => a.distance - b.distance)[0];
    if (!at) { if (type === "ironMine") continue; throw new Error(`no place for ${type}`); }
    const result = await c.placeBuilding(type, at.x, at.y);
    if (!result.success) throw new Error(result.error);
    await c.cheat("finish");
    const building = [...w.buildings.values()].find((b) => b.owner === co.id && b.type === type && b.x === at.x && b.y === at.y);
    made.push(building);
    if (type === "storage") for (let i = 0; i < 4; i++) await c.cheat("stock");
    const size = definition.size;
    const starts = edge === "x" ? [[at.x + size, at.y]] : edge === "y" ? [[at.x, at.y + size]] : [[at.x, at.y + size], [at.x + size, at.y], [at.x, at.y - 1], [at.x - 1, at.y]];
    let laid = false;
    for (const [sx, sy] of starts) {
      const route = tp.rules.routePipe(w, co, { x: sx, y: sy }, post, state().content);
      if (route && (await c.placePipes(route.flatMap((p) => [p.x, p.y]), 1)).success) { laid = true; break; }
    }
    if (!laid) throw new Error(`cannot pipe ${type}`);
  }
  tp.appStore.setState({ levelUp: null, hintTile: null, photoMode: true, selectedBuilding: null, selectedNetwork: null, tool: { kind: "select" } });
  return made.map((b) => ({ id: b.id, type: b.type, x: b.x, y: b.y }));
});
console.log("fixture", JSON.stringify(fixture));
await page.waitForTimeout(4000);
// Every connector on a far side, with whether its moving goods are drawn before (under) the pipe's own tube.
const connectors = await page.evaluate(() => [...tp.gameClient.engine.connectorViews].filter((c) => !c.container.destroyed).map((c) => {
  let node = c.container;
  while (node.parent && node.parent.parent && !node.parent.zIndex) node = node.parent;
  const pipe = node.parent;
  const tube = pipe.children.findIndex((s) => s.label === "pipe-body");
  return { key: c.key, x: c.x, y: c.y, side: c.side, active: c.active, kinds: c.activeKinds, under: tube >= 0 && pipe.children.indexOf(node) < tube };
}));
console.log("connectors", JSON.stringify(connectors));
const far = connectors.filter((c) => c.side === "u" || c.side === "l");
check(far.some((c) => c.side === "l") && far.some((c) => c.side === "u"), "connectors on both far sides in the fixture");
check(far.some((c) => c.active), "goods move on a far-side connector");
if (!process.env.BEFORE) {
  check(far.length > 0 && far.every((c) => c.under), "far-side lanes and their goods are drawn under the pipe's tube");
  const near = connectors.filter((c) => c.side === "r" || c.side === "d");
  check(near.every((c) => !c.under), "near-side lanes and their goods stay over the pipe's tube");
}
for (const c of connectors) {
  await page.evaluate((c) => tp.gameClient.engine.lookAt(c.x, c.y, 2.6), c);
  await page.mouse.move(1590, 990);
  await page.waitForTimeout(900);
  await page.screenshot({ path: `${out}/${c.side}-${c.x}-${c.y}.png`, clip: { x: 500, y: 250, width: 600, height: 450 } });
  console.log("shot", `${c.side}-${c.x}-${c.y}`);
}
check(errors.length === 0, `browser errors: ${errors.join(" | ") || "none"}`);
console.log(failed ? `${failed} failures` : "PASS");
await browser.close();
process.exitCode = failed ? 1 : 0;

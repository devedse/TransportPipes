// Live goods on the building connectors, and a gate with a receiving inlet rather than a pipe cap through the pad.
// node connector-motion.mjs shots/connector-motion (BEFORE=1 captures the previous rendering).
import { start, newPlayer } from "./lib.mjs";
import { mkdirSync, writeFileSync } from "node:fs";

const out = process.argv[2] ?? "shots/connector-motion";
const { browser, page, errors, shot, cheat } = await start(out);
let failed = 0;
const check = (ok, message) => { console.log(`${ok ? "ok" : "FAIL"} ${message}`); if (!ok) failed++; };
await newPlayer(page, "motion");
for (const command of ["gold", "gold", ...Array(8).fill("level")]) await cheat(command);
const fixture = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const state = () => tp.appStore.getState();
  const co = state().company;
  for (let p = 0; p < 16; p++) await c.buyParcel(p, 0);
  const post = [...w.buildings.values()].find((b) => b.owner === co.id && b.type === "tradingPost");
  const made = [];
  for (const type of ["storage", "windMoily", "smallFactory", "waterPump"]) {
    const definition = state().content.building(type);
    const spots = [];
    for (let y = co.estateY + 1; y < co.estateY + 31; y++) for (let x = co.estateX + 1; x < co.estateX + 31; x++) {
      if (made.some((b) => Math.abs(x - b.x) < 3 && Math.abs(y - b.y) < 3)) continue;
      if (tp.rules.canPlace(state().content, w, co, state().era, definition, x, y).ok)
        spots.push({ x, y, distance: Math.abs(x - post.x) + Math.abs(y - post.y) });
    }
    const at = spots.sort((a, b) => a.distance - b.distance)[0];
    if (!at) throw new Error(`no place for ${type}`);
    const result = await c.placeBuilding(type, at.x, at.y);
    if (!result.success) throw new Error(result.error);
    await c.cheat("finish");
    const building = [...w.buildings.values()].find((b) => b.owner === co.id && b.type === type);
    made.push(building);
    if (type === "storage") for (let i = 0; i < 4; i++) await c.cheat("stock");
    let laid = false;
    for (const [dx, dy] of [[0, 1], [1, 0], [0, -1], [-1, 0]]) {
      const route = tp.rules.routePipe(w, co, { x: at.x + dx, y: at.y + dy }, post, state().content);
      if (route && (await c.placePipes(route.flatMap((p) => [p.x, p.y]), 1)).success) { laid = true; break; }
    }
    if (!laid) throw new Error(`cannot pipe ${type}`);
  }
  await tp.gameClient.planYardPipe();
  await tp.gameClient.buildPlannedRoute();
  const yard = await c.getYard();
  tp.appStore.setState({ levelUp: null, hintTile: null, photoMode: true, selectedBuilding: null, selectedNetwork: null, tool: { kind: "select" } });
  return { buildings: made.map((b) => ({ id: b.id, type: b.type, x: b.x, y: b.y })), gate: { x: yard.gateX, y: yard.gateY } };
});
console.log("fixture", JSON.stringify(fixture));
writeFileSync(`${out}/fixture.json`, JSON.stringify(fixture));
await page.context().storageState({ path: `${out}/login.json` });
await page.waitForTimeout(3000);
const flows = await page.evaluate(() => [...tp.gameClient.world.flows.values()].flatMap((n) => n.flows).filter((f) => f.delivered > 0));
for (const resource of ["energy", "ironOre", "ironIngot", "water"]) check(flows.some((f) => f.resource === resource), `real ${resource} flow in the fixture`);
await page.evaluate((g) => tp.gameClient.engine.lookAt(g.x, g.y, 1.5), fixture.gate);
await page.mouse.move(1590, 990);
await page.waitForTimeout(1200);
await shot("1-gate-inlet");
for (const type of ["smallFactory", "waterPump"]) {
  const b = fixture.buildings.find((b) => b.type === type);
  await page.evaluate((b) => tp.gameClient.engine.lookAt(b.x, b.y, 1.5), b);
  await page.waitForTimeout(1200);
  await shot(`2-${type}`);
  if (!process.env.BEFORE) {
    const moving = await page.evaluate(() => [...tp.gameClient.engine.connectorViews].filter((c) => !c.container.destroyed && c.active).flatMap((c) => c.activeKinds));
    for (const kind of type === "smallFactory" ? ["solid", "power"] : ["fluid", "power"])
      check(moving.includes(kind), `${type}: active ${kind} connector animation`);
    mkdirSync(`${out}/${type}-frames`, { recursive: true });
    for (let i = 0; i < (process.env.SKIP_FRAMES ? 0 : 24); i++) {
      await page.screenshot({ path: `${out}/${type}-frames/${String(i).padStart(3, "0")}.png`, clip: { x: 450, y: 240, width: 700, height: 500 } });
      await page.waitForTimeout(85);
    }
  }
}
if (!process.env.BEFORE) {
  const positions = () => page.evaluate(() => [...tp.gameClient.engine.connectorViews].filter((c) => !c.container.destroyed && c.container.visible).flatMap((c) => c.container.children.filter((s) => s.visible && s.label).map((s) => `${c.key}:${s.label}:${s.x.toFixed(2)},${s.y.toFixed(2)}`)).sort());
  await page.evaluate(() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve))));
  const a = await positions(); const clockA = await page.evaluate(() => tp.gameClient.engine.time); await page.waitForTimeout(400); const b = await positions();
  if (!a.length || JSON.stringify(a) === JSON.stringify(b)) console.log("motion difference", { a: a.slice(0, 5), b: b.slice(0, 5), clockA, clockB: await page.evaluate(() => tp.gameClient.engine.time) });
  check(a.length > 0 && JSON.stringify(a) !== JSON.stringify(b), "packets actually move between frames");
  check(await page.evaluate(() => [...tp.gameClient.engine.connectorViews].every((c) => c.container.destroyed || c.container.children.length <= 28)), "at most 28 pooled sprites per inlet, independent of throughput");
  await page.emulateMedia({ reducedMotion: "reduce" }); await page.waitForTimeout(1000);
  const still = await positions(); await page.waitForTimeout(180);
  const held = await positions();
  if (JSON.stringify(still) !== JSON.stringify(held)) console.log("reduced-motion difference", await page.evaluate(() => ({ media: matchMedia("(prefers-reduced-motion: reduce)").matches, engine: tp.gameClient.engine.reducedMotion.matches })), still.slice(0, 5), held.slice(0, 5));
  check(JSON.stringify(still) === JSON.stringify(held), "reduced motion holds connector cargo still");
  await page.emulateMedia({ reducedMotion: "no-preference" });
  const pump = fixture.buildings.find((b) => b.type === "waterPump");
  await page.evaluate((b) => tp.gameClient.engine.lookAt(b.x, b.y, 0.2), pump); await page.waitForTimeout(1000);
  check(await page.evaluate(() => [...tp.gameClient.engine.connectorViews].every((c) => c.container.destroyed || !c.container.visible)), "connector motion fades out at a distance");
  await shot("3-zoomed-out");
  const factory = fixture.buildings.find((b) => b.type === "smallFactory");
  await page.evaluate(async (b) => {
    const w = tp.gameClient.world; const me = tp.appStore.getState().company.id;
    // Leave the factory's own inlet in place but cut its supply, so its attached belts must stop.
    const adjacent = (p) => Math.abs(p.x - b.x) + Math.abs(p.y - b.y) === 1;
    const remove = [...w.conductors.values()].filter((p) => p.owner === me && p.kind === "pipe" && !adjacent(p));
    await tp.gameClient.connection.removePipes(remove.flatMap((p) => [p.x, p.y]));
    tp.gameClient.engine.lookAt(b.x, b.y, 1.5);
  }, factory);
  await page.waitForFunction((b) => [...tp.gameClient.world.flows.values()].every((n) => !n.flows.some((f) => (f.from === b.id || f.to === b.id) && f.delivered > 0)), factory, { timeout: 15000 });
  await page.waitForTimeout(1000);
  const idle = await page.evaluate((b) => [...tp.gameClient.engine.connectorViews].filter((c) => !c.container.destroyed && c.key.endsWith(`:${b.id}`)).map((c) => ({ active: c.active, visible: c.container.visible })), factory);
  check(idle.length > 0 && idle.every((c) => !c.active && !c.visible), "the attached inlet stops when its real flow stops");
  await shot("4-idle-factory");
}
check(errors.length === 0, `browser errors: ${errors.join(" | ") || "none"}`);
console.log(failed ? `${failed} failures` : "PASS");
await browser.close();
process.exitCode = failed ? 1 : 0;

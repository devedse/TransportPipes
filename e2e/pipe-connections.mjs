// Building inlets and the Yard barrier: run with a development server, node pipe-connections.mjs shots/connections.
// Optional BEFORE=1 captures the reported rendering and gesture before the fix.
import { start, newPlayer, emptySeats, moveTo } from "./lib.mjs";

const out = process.argv[2] ?? "shots/connections";
const player = await start(out, process.env.TOUCH ? { touch: true } : {});
const { browser, page, errors, shot, cheat } = player;
let failures = 0;
const check = (ok, text) => { console.log(`${ok ? "ok" : "FAIL"} ${text}`); if (!ok) failures++; };
await newPlayer(page, "join");
await cheat("gold");
if (process.env.SIDE !== undefined) {
  const seats = await emptySeats(page, 4);
  await moveTo(player, seats[Number(process.env.SIDE)]);
  check(await page.evaluate(() => tp.appStore.getState().company.slot % 4) === Number(process.env.SIDE), "the requested gate orientation is being tested");
}
if (process.env.TOUCH) await page.setViewportSize(process.env.PLAN_ONLY ? { width: 390, height: 844 } : { width: 900, height: 800 });
// Go straight to the gate step without changing the player's level.
for (let i = 0; i < 4; i++) await cheat("tutorial");
await page.waitForFunction(() => tp.appStore.getState().company.tutorial.id === "firstYard");
const gate = await page.evaluate(async () => {
  const yard = await tp.gameClient.connection.getYard();
  return { x: yard.gateX, y: yard.gateY };
});
await page.evaluate((g) => tp.gameClient.engine.lookAt(g.x, g.y, 1), gate);
await page.waitForFunction((g) => tp.gameClient.world.conductorAt(g.x, g.y)?.kind === "gate" &&
  [...tp.gameClient.world.buildings.values()].some((b) => b.type === "tradingPost" && b.owner === tp.appStore.getState().company.id), gate);
const post = await page.evaluate(() => [...tp.gameClient.world.buildings.values()].find((b) => b.type === "tradingPost" && b.owner === tp.appStore.getState().company.id));
if (!process.env.BEFORE) {
  await page.locator(".loady-card button.primary").click();
  await page.waitForFunction(() => tp.appStore.getState().plannedRoute !== null);
  const planned = await page.evaluate(() => tp.appStore.getState().plannedRoute);
  check(planned.some((p) => p.x === gate.x && p.y === gate.y), "Show me plans a pipe to the actual gate");
  await page.waitForFunction(() => ![...document.querySelectorAll(".toast")].some((t) => t.textContent.startsWith("Welcome,")), null, { timeout: 15000 });
  await shot("0-show-me");
  if (process.env.PLAN_ONLY) {
    const banner = await page.locator(".route-banner").boundingBox();
    check(banner.width <= 390 && banner.height < 160, "the planned pipe and buttons fit the phone without narrow columns");
    await page.locator(".route-banner button.primary").click();
    await page.waitForFunction(() => tp.appStore.getState().company.tutorial.id !== "firstYard", null, { timeout: 10000 });
    check(await page.evaluate(() => tp.appStore.getState().hintTile === null), "laying the planned pipe completes the quest and clears its marker");
    await shot("1-connected");
    check(errors.length === 0, `browser errors: ${errors.join(" | ") || "none"}`);
    await browser.close();
    process.exit(failures ? 1 : 0);
  }
  await page.evaluate(() => tp.appStore.setState({ plannedRoute: null, hintTile: null }));
}
// Keep the gesture's source clear of transient HUD cards on either side of the Yard.
await page.evaluate((g) => { tp.appStore.setState({ photoMode: true, tool: { kind: "pipe", tier: 1 } }); tp.gameClient.engine.lookAt(g.x, g.y, 1); }, gate);
await page.waitForTimeout(1500);
const points = await page.evaluate(({ gate, post }) => {
  const e = tp.gameClient.engine;
  const center = e.tileToScreen(gate.x, gate.y);
  // Midway up the yellow post, well above the gate's ground tile.
  const alongU = tp.appStore.getState().company.slot % 4 === 0 || tp.appStore.getState().company.slot % 4 === 3;
  const pole = { x: center.x + (alongU ? -40 : 40), y: center.y - 70 };
  return { from: e.tileToScreen(post.x, post.y), pole, ground: center, groundUnderPole: e.tileAtScreen(pole.x, pole.y) };
}, { gate, post });
console.log("gate", { x: gate.x, y: gate.y, ...points });
let touch;
if (process.env.TOUCH) {
  touch = await page.context().newCDPSession(page);
  await touch.send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: [{ ...points.from, id: 1 }] });
  for (let i = 1; i <= 20; i++) {
    const x = points.from.x + (points.pole.x - points.from.x) * i / 20;
    const y = points.from.y + (points.pole.y - points.from.y) * i / 20;
    await touch.send("Input.dispatchTouchEvent", { type: "touchMove", touchPoints: [{ x, y, id: 1 }] });
  }
} else {
  await page.mouse.move(points.from.x, points.from.y);
  await page.mouse.down();
  await page.mouse.move(points.pole.x, points.pole.y, { steps: 20 });
}
await page.waitForTimeout(300);
const route = await page.evaluate(() => tp.appStore.getState().pipeRoute);
check(route?.some((p) => p.x === gate.x && p.y === gate.y), "dragging to the raised post targets the gate tile");
await shot("1-gate-preview");
if (touch) await touch.send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: [] });
else await page.mouse.up();
await page.waitForTimeout(3000);
check(await page.evaluate(() => tp.appStore.getState().company.tutorial.id !== "firstYard"), "the gate quest completes immediately after laying the pipe");
await page.evaluate(() => tp.appStore.setState({ tool: { kind: "select" }, selectedBuilding: null }));
if (process.env.GATE_ONLY) {
  check(errors.length === 0, `browser errors: ${errors.join(" | ") || "none"}`);
  await browser.close();
  process.exit(failures ? 1 : 0);
}

// A spread of building inlets: bends and ends as well as continuous runs, all four orientations.
for (let i = 0; i < 8; i++) await cheat("level");
await cheat("alltech");
await page.evaluate(async () => { for (let p = 0; p < 16; p++) await tp.gameClient.connection.buyParcel(p, 0); });
const fixtures = await page.evaluate(async () => {
  const s = tp.appStore.getState(); const w = tp.gameClient.world; const c = tp.gameClient.connection;
  const co = s.company; const content = s.content;
  const sides = [["u", 0, -1], ["r", 1, 0], ["d", 0, 1], ["l", -1, 0]];
  const made = [];
  for (let row = 0; row < 4; row++) {
    const [side, dx, dy] = sides[row];
    for (let col = 0; col < 3; col++) {
      // Find an empty 5x5 square for the inlet and the building.
      let at = null; let bestScore = Infinity;
      for (let y = co.estateY + 3; y < co.estateY + 29 && bestScore > 0; y++) {
        for (let x = co.estateX + 3; x < co.estateX + 29 && bestScore > 0; x++) {
          let free = true;
          for (let oy = -2; oy <= 2 && free; oy++) for (let ox = -2; ox <= 2 && free; ox++)
            free = tp.rules.pipeCost(w, co, x + ox, y + oy, content) === 1 && !made.some((m) => Math.abs(x + ox - m.x) <= 2 && Math.abs(y + oy - m.y) <= 2);
          if (free && tp.rules.canPlace(content, w, co, s.era, content.building("smallFactory"), x + dx, y + dy).ok) {
            // Prefer a clearing: foreground trees obscure the inlet even when they fade.
            let score = 0;
            for (let fy = -2; fy <= 4; fy++) for (let fx = -2; fx <= 4; fx++) {
              if (["forest", "mountain"].includes(w.terrain(x + fx, y + fy))) score++;
            }
            if (score < bestScore) { at = { x, y }; bestScore = score; }
          }
        }
      }
      if (!at) throw new Error(`no fixture space for ${side}, ${col}`);
      const { x, y } = at;
      const b = await c.placeBuilding("smallFactory", x + dx, y + dy);
      if (!b.success) throw new Error(b.error);
      await c.cheat("finish");
      const rest = col === 0 ? [[-dx, -dy]] : col === 1 ? [[-dx, -dy], [dy, dx]] : [[dy, dx], [-dy, -dx]];
      const tiles = [[x, y], ...rest.map(([a, b]) => [x + a, y + b])];
      const r = await c.placePipes(tiles.flat(), 1);
      if (!r.success) throw new Error(r.error);
      made.push({ x, y, side, kind: ["end", "bend", "through"][col] });
    }
  }
  tp.appStore.setState({ levelUp: null, selectedBuilding: null, tool: { kind: "select" }, photoMode: true });
  return made;
});
for (const f of fixtures) {
  await page.evaluate((f) => tp.gameClient.engine.lookAt(f.x, f.y, 1.5), f);
  await page.mouse.move(1590, 990);
  await page.waitForTimeout(500);
  await shot(`2-${f.side}-${f.kind}`);
}
check(errors.length === 0, `browser errors: ${errors.join(" | ") || "none"}`);
console.log(failures ? `${failures} failures` : "PASS");
await browser.close();
if (!process.env.BEFORE) process.exitCode = failures ? 1 : 0;

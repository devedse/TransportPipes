// Selecting several buildings: a left-drag box, Shift and Ctrl clicks, upgrade all, the keys; a right-drag still
// pans. node selection.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/selection"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "s");
for (let i = 0; i < 6; i++) await cheat("gold");
// The level-up card from the cheats can show during the drag: the map must work under it
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
const placed = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const ids = [];
  for (let i = 0; i < 4; i++) {
    for (let dy = 0; dy < 10; dy++) {
      const x = co.estateX + 10 + i * 2, y = co.estateY + 19 + dy;
      if (tp.rules.canPlace(s.content, w, tp.appStore.getState().company, s.era, s.content.building("windMoily"), x, y).ok) {
        if ((await c.placeBuilding("windMoily", x, y)).success) { ids.push({ x, y }); break; }
      }
    }
  }
  await c.cheat("finish");
  await new Promise((r) => setTimeout(r, 1200));
  tp.gameClient.engine.lookAt(co.estateX + 13, co.estateY + 21, 0.9);
  return ids;
});
await page.waitForTimeout(1500);
const results = [];
const expect = (label, ok, extra = "") => results.push(`${ok ? "ok  " : "FAIL"} ${label}${extra ? ` (${extra})` : ""}`);
const screen = (t) => page.evaluate(([x, y]) => tp.gameClient.engine.tileToScreen(x, y), [t.x, t.y]);
const state = () => page.evaluate(() => { const s = tp.appStore.getState(); return { building: s.selectedBuilding, selection: s.selection }; });
const levels = () => page.evaluate((ps) => ps.map((p) => [...tp.gameClient.world.buildings.values()].find((t) => t.x === p.x && t.y === p.y)?.level), placed);
expect("placed 4 moilies", placed.length === 4, JSON.stringify(placed));
const pts = await Promise.all(placed.map(screen));

// 1. A left-drag box around them selects all four
const xs = pts.map((t) => t.x), ys = pts.map((t) => t.y);
const box = { left: Math.min(...xs) - 60, top: Math.min(...ys) - 60, right: Math.max(...xs) + 60, bottom: Math.max(...ys) + 60 };
await page.mouse.move(box.left, box.top);
await page.mouse.down();
for (let i = 1; i <= 10; i++) await page.mouse.move(box.left + ((box.right - box.left) * i) / 10, box.top + ((box.bottom - box.top) * i) / 10);
await shot("1-dragging");
await page.mouse.up();
await page.waitForTimeout(800);
let s = await state();
expect("box selects 4", s.selection.length >= 4, `${s.selection.length}`);
await shot("2-selected");
expect("the selection panel shows", await page.locator(".selection-panel").isVisible());

// 2. Upgrade all +1
const before = await levels();
await page.locator(".selection-panel button.primary").click({ timeout: 5000 }).catch(() => undefined);
await page.waitForTimeout(1200);
const after = await levels();
expect("all +1 raises each by one", after.every((l, i) => l === before[i] + 1), `${before} → ${after}`);

// 3. U and Shift+U
await page.keyboard.press("u");
await page.waitForTimeout(1000);
const afterU = await levels();
expect("U: each +1", afterU.every((l, i) => l === after[i] + 1), `${after} → ${afterU}`);
await page.keyboard.press("Shift+U");
await page.waitForTimeout(1000);
const afterShiftU = await levels();
expect("Shift+U: each +10", afterShiftU.every((l, i) => l === afterU[i] + 10), `${afterU} → ${afterShiftU}`);
await shot("3-upgraded");

// 4. A right-drag pans and keeps the selection (also under a level-up card)
const before0 = await screen({ x: 0, y: 0 });
await page.mouse.move(620, 680);
await page.mouse.down({ button: "right" });
for (let i = 1; i <= 8; i++) await page.mouse.move(620 + i * 20, 680 + i * 8);
await page.mouse.up({ button: "right" });
await page.waitForTimeout(500);
const after0 = await screen({ x: 0, y: 0 });
expect("right-drag pans", Math.abs(after0.x - before0.x) > 50);
expect("right-drag keeps the selection", (await state()).selection.length >= 4);

// 5. A plain click on one selects just that one; Shift-click adds another; Shift-click again takes it out
await page.evaluate((t) => { tp.appStore.setState({ levelUp: null }); tp.gameClient.engine.lookAt(t.x + 3, t.y + 2, 0.9); }, placed[0]);
await page.waitForTimeout(800);
const p = await Promise.all(placed.map(screen));
await page.mouse.click(p[0].x, p[0].y);
await page.waitForTimeout(500);
s = await state();
expect("click selects one", s.building !== null && s.selection.length === 0);
await page.keyboard.down("Shift"); await page.mouse.click(p[1].x, p[1].y); await page.keyboard.up("Shift");
await page.waitForTimeout(500);
s = await state();
expect("Shift-click adds a second", s.selection.length === 2, `${s.selection.length}`);
await page.keyboard.down("Shift"); await page.mouse.click(p[1].x, p[1].y); await page.keyboard.up("Shift");
await page.waitForTimeout(500);
s = await state();
expect("Shift-click again takes it out", s.selection.length === 0 && s.building !== null, JSON.stringify(s));
await shot("3b-shift-out");

// 6. Ctrl-click: all moilies
await page.keyboard.down("Control"); await page.mouse.click(p[2].x, p[2].y); await page.keyboard.up("Control");
await page.waitForTimeout(500);
s = await state();
// All of the player's moilies, the starter kit's too
const moilies = await page.evaluate(() => [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === tp.appStore.getState().company.id && t.type === "windMoily").length);
expect("Ctrl-click selects all of its kind", s.selection.length === moilies && moilies >= 4, `${s.selection.length} of ${moilies}`);

// 7. Escape clears; Ctrl+A selects all own buildings on screen; right-click clears
await page.keyboard.press("Escape");
await page.waitForTimeout(300);
expect("Escape clears", (await state()).selection.length === 0);
await page.keyboard.press("Control+a");
await page.waitForTimeout(500);
s = await state();
expect("Ctrl+A selects own buildings in view", s.selection.length >= 4, `${s.selection.length}`);
await shot("4-ctrl-a");
await page.mouse.click(5, 600, { button: "right" });
await page.waitForTimeout(300);
expect("right-click clears", (await state()).selection.length === 0, await page.evaluate(() => { const s = tp.appStore.getState(); return JSON.stringify(Object.fromEntries(Object.entries(s).filter(([k, v]) => /Open$|plannedRoute|tool|photoMode|visiting|wildTile|selectedGate|selectedNetwork|Draft|DeepParcel/.test(k) && v !== null && v !== false))); }));

// 8. A budget: spend 2,000 g cheapest first
await page.keyboard.press("Control+a");
await page.waitForTimeout(500);
const gold = await page.evaluate(() => tp.appStore.getState().company.gold);
const res = await page.evaluate((ids) => tp.gameClient.connection.upgradeMany(ids, 1, 2000), (await state()).selection);
const gold2 = await page.evaluate(async () => { await new Promise((r) => setTimeout(r, 800)); return tp.appStore.getState().company.gold; });
expect("budget spends at most 2,000 g", res.success && res.gold <= 2000 && gold - gold2 <= 2001, JSON.stringify(res));

console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

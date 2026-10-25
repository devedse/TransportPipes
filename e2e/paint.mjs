// Painting with the build tool: a drag builds a row, Shift-drag an area, a click one; the ? key lists the keys.
// node paint.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/paint"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "w");
for (const c of ["gold", "gold"]) await cheat(c);
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.evaluate(async () => {
  await tp.gameClient.connection.skipTutorial();
  const co = tp.appStore.getState().company;
  tp.gameClient.engine.lookAt(co.estateX + 14, co.estateY + 19, 0.9);
  tp.appStore.setState({ levelUp: null });
});
await page.waitForTimeout(1500);
const results = [];
const expect = (label, ok, extra = "") => results.push(`${ok ? "ok  " : "FAIL"} ${label}${extra ? ` (${extra})` : ""}`);
const panels = () => page.evaluate(() => [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === tp.appStore.getState().company.id && t.type === "solarPanel").length);
const screen = (x, y) => page.evaluate(([x, y]) => { const co = tp.appStore.getState().company; return tp.gameClient.engine.tileToScreen(co.estateX + x, co.estateY + y); }, [x, y]);
// A free row in the start parcels
const row = await page.evaluate(() => {
  const s = tp.appStore.getState(); const co = s.company; const def = s.content.building("solarPanel");
  for (let y = 18; y < 23; y++) {
    let run = 0;
    for (let x = 8; x < 24; x++) {
      run = tp.rules.canPlace(s.content, tp.gameClient.world, co, s.era, def, co.estateX + x, co.estateY + y).ok ? run + 1 : 0;
      if (run === 6) return { x: x - 5, y };
    }
  }
  return null;
});
console.log("row", JSON.stringify(row));
await page.evaluate(() => tp.gameClient.setTool({ kind: "build", type: "solarPanel" }));
const a = await screen(row.x, row.y), b = await screen(row.x + 5, row.y);
await page.mouse.move(a.x, a.y);
await page.mouse.down();
for (let i = 1; i <= 12; i++) await page.mouse.move(a.x + ((b.x - a.x) * i) / 12, a.y + ((b.y - a.y) * i) / 12);
await shot("1-painting");
await page.mouse.up();
await page.waitForTimeout(2500);
expect("a drag paints a row of six", (await panels()) === 6, `${await panels()}`);
await shot("2-row");

// Shift: an area (two rows of the free spots below, if any)
const before = await panels();
const c = await screen(row.x, row.y + 2), d = await screen(row.x + 3, row.y + 3);
await page.keyboard.down("Shift");
await page.mouse.move(c.x, c.y);
await page.mouse.down();
for (let i = 1; i <= 10; i++) await page.mouse.move(c.x + ((d.x - c.x) * i) / 10, c.y + ((d.y - c.y) * i) / 10);
await shot("3-area");
await page.mouse.up();
await page.keyboard.up("Shift");
await page.waitForTimeout(2500);
expect("Shift-drag paints an area", (await panels()) - before >= 4, `${(await panels()) - before}`);

// A click builds one
const one = await panels();
const e = await screen(row.x + 7, row.y + 5);
await page.mouse.click(e.x, e.y);
await page.waitForTimeout(2000);
expect("a click builds one", (await panels()) - one <= 1, `${(await panels()) - one}`);

// Right-click puts the tool away; ? lists the keys
await page.mouse.click(e.x, e.y, { button: "right" });
await page.keyboard.press("Shift+?");
await page.waitForTimeout(400);
expect("? lists the keys", await page.locator(".keys-help").isVisible());
await shot("4-keys");
await page.keyboard.press("Escape");
await page.waitForTimeout(300);
expect("Esc closes it", !(await page.locator(".keys-help").isVisible()));
console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

// Visual evolution: moilies at levels 1, 10, 25, 50, 75, 100. node evolution.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/evolution"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "e");
for (let i = 0; i < 40; i++) await cheat("gold"); for (let i = 0; i < 1500; i++) await cheat("earn");
console.log(await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const co = s.company; const w = tp.gameClient.world;
  const ids = [];
  for (let i = 0; i < 6; i++) {
    for (let dy = 0; dy < 10; dy++) {
      const x = co.estateX + 10 + i * 2, y = co.estateY + 19 + dy;
      if (tp.rules.canPlace(s.content, w, tp.appStore.getState().company, s.era, s.content.building("windMoily"), x, y).ok) {
        await c.placeBuilding("windMoily", x, y); ids.push({ x, y }); break;
      }
    }
  }
  await c.cheat("finish");
  const levels = [1, 10, 25, 50, 75, 100];
  const results = [];
  for (const [i, p] of ids.entries()) {
    const b = [...w.buildings.values()].find((t) => t.x === p.x && t.y === p.y);
    if (b && levels[i] > 1) results.push((await c.upgrade(b.id, levels[i] - 1)).error ?? `L${levels[i]}`);
  }
  tp.gameClient.engine.lookAt(co.estateX + 15, co.estateY + 21, 0.9);
  return results.join(",");
}));
await page.waitForTimeout(5000);
await shot("1-levels");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

// A network says what its buildings could make and why some make less (the owner, 2026-10-31: "when I upgrade a
// Smelter, that my copper ore production increases. I would only expect it's need to increase": his mines made only
// what the Smelters took, as their pipe reached no Trading Post): a Wind Moily whose only pipe reaches an Iron Mine
// (piped to the Trading Post itself); the network's energy says "makes X of the Y it could", the reason, and Show it
// opens the moily.   node slowed.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/slowed"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "sl");
for (const c of ["gold", "level", "level"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
console.log(await kit(page, ["ironMine"]));
// A moily beside the mine, joined to it by one pipe tile that touches no other pipe
const placed = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = () => tp.appStore.getState();
  const me = s().company.id;
  const mine = [...w.buildings.values()].find((t) => t.owner === me && t.type === "ironMine");
  const moily = s().content.building("windMoily");
  const free = (x, y) => !w.isOccupied(x, y) && w.conductorAt(x, y) === undefined;
  const lonely = (x, y) => [[1, 0], [-1, 0], [0, 1], [0, -1]].every(([dx, dy]) => w.conductorAt(x + dx, y + dy) === undefined);
  for (let i = 0; i < mine.size; i++) {
    for (const [px, py, mx, my] of [
      [mine.x - 1, mine.y + i, mine.x - 1 - moily.size, mine.y + i],
      [mine.x + mine.size, mine.y + i, mine.x + mine.size + 1, mine.y + i],
      [mine.x + i, mine.y - 1, mine.x + i, mine.y - 1 - moily.size],
      [mine.x + i, mine.y + mine.size, mine.x + i, mine.y + mine.size + 1],
    ]) {
      if (!free(px, py) || !lonely(px, py) || !tp.rules.canPlace(s().content, w, s().company, s().era, moily, mx, my).ok) continue;
      if (!(await c.placeBuilding("windMoily", mx, my)).success) continue;
      await c.cheat("finish");
      const pipe = await c.placePipes([px, py], 1);
      return { moily: [...w.buildings.values()].find((t) => t.owner === me && t.type === "windMoily")?.id, pipe: pipe.success || pipe.error };
    }
  }
  return null;
});
console.log("placed", JSON.stringify(placed));
check(placed?.pipe === true, "a moily whose pipe reaches only the mine", JSON.stringify(placed));
await page.waitForTimeout(4000);
await page.evaluate((id) => tp.gameClient.showBuilding(id), placed.moily);
await page.waitForSelector(".net-res", { timeout: 15000 });
await page.waitForTimeout(1500);
const energy = page.locator(".net-res", { hasText: "Energy" }).first();
const text = (await energy.innerText()).replace(/\s+/g, " ");
console.log("energy:", text);
check(/makes [\d.,]+ of the [\d.,]+ it could/.test(text), "it says how much it could make", text);
check(/makes less than it could: only the Iron Mine takes its energy\. It sells nothing/.test(text), "and why", text);
await energy.scrollIntoViewIfNeeded();
await shot("1-network");
await energy.locator("button", { hasText: "Show it" }).click();
await page.waitForTimeout(1500);
const status = (await page.locator(".building-panel, aside.panel").first().innerText()).replace(/\s+/g, " ");
check(/Wind Moily/.test(status) && /Slowed: only the Iron Mine takes its energy/.test(status), "Show it opens the moily, with the same reason", status.slice(0, 200));
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

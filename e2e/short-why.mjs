// Why a network is short of a good it could have (the owner, 2026-10-31: coal 5 short while the coal link brought 25;
// the 5 were a Brickworks' that had no stone, and the Yard and links bring a building only what it can use): a
// Brickworks on a Wind Moily, no stone, no coal: the network panel's coal line says the 5 are for the Brickworks, which
// gets no stone, and its stone line the same about coal.   node short-why.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/short-why"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "sw");
for (let i = 0; i < 6; i++) await cheat("level");
await cheat("gold");
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const built = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const w = tp.gameClient.world; const c = tp.gameClient.connection;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const ok = (type, x, y) => tp.rules.canPlace(s().content, w, s().company, s().era, s().content.building(type), x, y).ok;
  // The two a tile apart on the plot, the tile between them free for the pipe (not by the Yard)
  for (let r = 3; r < 14; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const [x, y] = [post.x + dx, post.y + dy];
        if (!ok("brickworks", x, y) || !ok("windMoily", x + 2, y) || !ok("windMoily", x + 1, y)) continue;
        if (!(await c.placeBuilding("brickworks", x, y)).success) continue;
        if (!(await c.placeBuilding("windMoily", x + 2, y)).success) continue;
        await c.cheat("finish");
        await new Promise((r) => setTimeout(r, 1000));
        const placed = await c.placePipes([x + 1, y], 1);
        return { works: { x, y }, moily: { x: x + 2, y }, piped: placed.success ? 1 : null, error: placed.error };
      }
  return { works: null };
});
console.log(JSON.stringify(built));
check(built.piped != null, "a Brickworks piped to a Wind Moily", JSON.stringify(built));
await page.waitForTimeout(2000);
const id = await page.evaluate(({ x, y }) => tp.gameClient.world.buildingAt(x, y)?.id, built.works);
await page.evaluate((id) => tp.gameClient.showBuilding(id), id);
await page.waitForSelector(".net-res", { timeout: 15000 });
await page.waitForTimeout(1500);
const rows = await page.locator(".net-res").allInnerTexts();
const coal = (rows.find((t) => /^\s*Coal/.test(t)) ?? "").replace(/\s+/g, " ");
const stone = (rows.find((t) => /^\s*Stone/.test(t)) ?? "").replace(/\s+/g, " ");
console.log("coal:", coal, "\nstone:", stone);
check(/5 of it for your Brickworks, which also gets no stone:/.test(coal), "coal: the 5 are for the Brickworks, which gets no stone", coal);
check(/10 of it for your Brickworks, which also gets no coal:/.test(stone), "stone: the 10 likewise, which gets no coal", stone);
check(/only what it can use/.test(coal), "and why nobody brings it", coal);
await shot("1-short");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

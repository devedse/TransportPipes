// Every pipe piece on a fresh estate, for close-ups: node pipe-gallery.mjs <outdir>
// Straight runs, a loop (four bends), a T, a cross, ends, pipes plugged into buildings on every side, and the five tiers.
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/pipe-gallery"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
page.on("console", (m) => { if (m.type() === "warning") console.log("warn:", m.text()); });
await newPlayer(page, "p");
for (const c of ["gold", "gold", "gold", "gold", "level", "level", "level", "level", "alltech"]) await cheat(c);
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.waitForTimeout(1000);
const origin = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const co = () => tp.appStore.getState().company;
  const W = 26, H = 13;
  const free = (x, y) => tp.rules.pipeCost(w, co(), x, y, s.content) === 1;
  // A free rectangle on the estate
  let origin = null;
  for (let oy = co().estateY + 1; oy < co().estateY + 32 - H && !origin; oy++) {
    for (let ox = co().estateX + 1; ox < co().estateX + 32 - W && !origin; ox++) {
      let ok = true;
      for (let y = 0; y < H && ok; y++) for (let x = 0; x < W && ok; x++) ok = free(ox + x, oy + y);
      if (ok) origin = { x: ox, y: oy };
    }
  }
  if (!origin) return null;
  const { x: ox, y: oy } = origin;
  const pipe = async (tiles, tier = 1) => {
    const r = await c.placePipes(tiles.flatMap(([x, y]) => [ox + x, oy + y]), tier);
    if (!r.success) console.warn("pipe", JSON.stringify(tiles), r.error);
  };
  const line = (x0, y0, x1, y1) => { const t = []; for (let x = x0, y = y0; ; ) { t.push([x, y]); if (x === x1 && y === y1) break; x += Math.sign(x1 - x); y += Math.sign(y1 - y); } return t; };
  // A storage first, stocked with what the better pipes and the factory cost
  await c.placeBuilding("storage", ox + 2, oy + 9);
  await c.cheat("finish");
  await c.cheat("stock");
  await pipe(line(0, 0, 5, 0));
  await pipe(line(8, 0, 8, 5));
  await pipe([...line(0, 3, 4, 3), ...line(4, 4, 4, 6), ...line(3, 6, 0, 6), ...line(0, 5, 0, 4)]);
  await pipe([...line(10, 3, 14, 3), ...line(12, 1, 12, 2), ...line(12, 4, 12, 5)]);
  await pipe([...line(16, 1, 18, 1), ...line(17, 2, 17, 3)]);
  await pipe(line(16, 5, 17, 5));
  // Buildings with pipes plugged in
  const build = async (type, x, y) => { const r = await c.placeBuilding(type, ox + x, oy + y); if (!r.success) console.warn(type, r.error); };
  await pipe(line(0, 9, 1, 9));
  await pipe(line(3, 9, 4, 9));
  await pipe([[2, 8]]);
  await pipe(line(2, 10, 2, 11));
  await build("windMoily", 7, 10);
  await pipe([...line(7, 9, 9, 9), ...line(9, 10, 9, 11)]);
  await build("bigFactory", 12, 8);
  await pipe(line(14, 8, 15, 8));
  await pipe(line(12, 10, 12, 11));
  for (let tier = 1; tier <= 5; tier++) await pipe(line(16 + (tier - 1) * 2, 7, 16 + (tier - 1) * 2, 11), tier);
  await c.cheat("finish");
  return origin;
});
console.log("gallery at", JSON.stringify(origin));
if (origin) {
  const look = async (x, y, zoom, name) => {
    await page.evaluate(([x, y, z]) => { tp.gameClient.select(null); tp.appStore.setState({ levelUp: null }); tp.gameClient.engine.lookAt(x, y, z); }, [origin.x + x, origin.y + y, zoom]);
    await page.waitForTimeout(2500);
    await shot(name);
  };
  await look(12, 6, 0.62, "1-all");
  await look(4, 3, 1.5, "2-runs-and-bends");
  await look(14, 3, 1.5, "3-cross-and-t");
  await look(6, 9.5, 1.5, "4-into-buildings");
  await look(20, 9, 1.4, "5-tiers");
}
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

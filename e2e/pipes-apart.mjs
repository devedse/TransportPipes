// Two players' pipes that touch in the wilderness (playtest: "we connected our pipes to each other and all got very slow";
// "I think pipes still join one network ... I also visually see them connecting"): two networks on the server, and
// drawn apart: neither pipe reaches into the other.   node pipes-apart.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/pipes-apart"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "pa");
await cheat("gold");
const other = await browser.newPage({ viewport: { width: 1200, height: 800 } });
await newPlayer(other, "pb");
await other.evaluate(() => tp.gameClient.connection.cheat("gold"));
for (const p of [page, other]) for (let i = 0; i < 3 && (await p.locator("[role=dialog]:visible").count()) > 0; i++) { await p.keyboard.press("Escape"); await p.waitForTimeout(400); }

// A wilderness tile beside A's plot, and the one east of it
const spot = await page.evaluate(() => {
  const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s.company.id && t.type === "tradingPost");
  for (let r = 5; r < 80; r++) for (let dx = -r; dx <= r; dx++) for (let dy = -r; dy <= r; dy++) {
    const x = post.x + dx, y = post.y + dy;
    const wild = (tx, ty) => tp.layout.zoneAt(tx, ty).kind === "wilderness" && w.conductorAt(tx, ty) === undefined && w.buildingAt(tx, ty) === undefined
      && !["mountain", "water"].includes(w.terrain(tx, ty));
    if (wild(x, y) && wild(x + 1, y)) return { x, y };
  }
  return null;
});
console.log("spot:", JSON.stringify(spot));
check(spot !== null, "two free wilderness tiles side by side");
if (spot !== null) {
  const a = await page.evaluate((p) => tp.gameClient.connection.placePipes([p.x, p.y], 1), spot);
  const b = await other.evaluate((p) => tp.gameClient.connection.placePipes([p.x + 1, p.y], 1), spot);
  console.log("A:", JSON.stringify(a), "B:", JSON.stringify(b));
  check(a.success && b.success, "each lays a pipe there");
  await page.evaluate((p) => tp.gameClient.engine.lookAt(p.x, p.y, 1), spot);
  await page.waitForTimeout(4000);
  const seen = await page.evaluate((p) => {
    const w = tp.gameClient.world;
    const mine = w.conductorAt(p.x, p.y); const theirs = w.conductorAt(p.x + 1, p.y);
    return { mine: mine?.network ?? null, theirs: theirs?.network ?? null, owners: [mine?.owner, theirs?.owner], shape: tp.gameClient.engine.pipeLook(p.x, p.y).shape };
  }, spot);
  console.log("seen:", JSON.stringify(seen));
  check(seen.owners[0] !== seen.owners[1], "the two pipes are the two players'");
  check(seen.mine !== null && seen.theirs !== null && seen.mine !== seen.theirs, "two networks on the server", JSON.stringify(seen));
  check(!seen.shape.includes("r"), "A's pipe isn't drawn reaching into B's", seen.shape);
  await shot("1-apart");
}
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// A network keeps one id on all its pipes and buildings when it is rebuilt: select it, add pipes, check the selection
// covers every pipe and no running building shows the idle bubble. node network-ids.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/network-ids"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "i");
for (const c of ["gold", "gold", "gold", "level", "level", "level", "level"]) await cheat(c);
await page.evaluate(async () => { for (let i = 0; i < 16; i++) await tp.gameClient.connection.buyParcel(i, 0); });
await page.waitForTimeout(800);
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world;
  const s = () => tp.appStore.getState(); const co = () => s().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  const ok = (x, y) => tp.rules.canPlace(s().content, w, co(), s().era, s().content.building("windMoily"), x, y).ok;
  // A moily far from the trading post, in another chunk (chunks are 32 tiles): its pipe crosses a chunk border
  const chunk = (x, y) => `${Math.floor(x / 32)},${Math.floor(y / 32)}`;
  let spot = null;
  for (let r = 22; r >= 8 && !spot; r--) for (let d = -r; d <= r && !spot; d++) for (const [x, y] of [[post.x + d, post.y + r], [post.x + r, post.y + d], [post.x + d, post.y - r], [post.x - r, post.y + d]]) if (!spot && ok(x, y) && chunk(x, y) !== chunk(post.x, post.y)) spot = { x, y };
  await c.placeBuilding("windMoily", spot.x, spot.y);
  await c.cheat("finish");
  let laid = 0;
  for (const [fx, fy] of [[spot.x - 1, spot.y], [spot.x + 1, spot.y], [spot.x, spot.y - 1], [spot.x, spot.y + 1]]) {
    const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
    if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) { laid = route.length; break; }
  }
  tp.gameClient.engine.lookAt((spot.x + post.x) / 2, (spot.y + post.y) / 2, 0.45);
  return { moily: spot, post: { x: post.x, y: post.y }, laid, chunks: [chunk(spot.x, spot.y), chunk(post.x, post.y)] };
});
console.log("setup", JSON.stringify(setup));
await page.waitForTimeout(4000);
const check = (label) => page.evaluate((label) => {
  const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  const pipes = [...w.conductors.values()].filter((t) => t.owner === co.id && t.kind === "pipe");
  const ids = new Set(pipes.map((t) => t.network));
  const moily = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily");
  const selected = s.selectedNetwork;
  return `${label}: ${pipes.length} pipes, ids ${[...ids].join(",")}, selected ${selected}, ` +
    `highlighted ${pipes.filter((t) => t.network === selected).length}, moily network ${moily.network} rate ${w.rate(moily)}`;
}, label);
// Select the network by clicking a pipe next to the trading post (as a player does)
await page.evaluate(({ post }) => {
  const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  const pipe = [...w.conductors.values()].find((t) => t.owner === co.id && t.kind === "pipe" && Math.abs(t.x - post.x) + Math.abs(t.y - post.y) <= 2);
  tp.gameClient.engineCallbacks.onClick({ x: pipe.x, y: pipe.y }, undefined);
}, setup);
await page.waitForTimeout(1500);
console.log(await check("selected"));
// Rebuild the network three times: a pipe stub next to it, near the trading post
for (let round = 1; round <= 3; round++) {
  console.log(await page.evaluate(async ({ post, round }) => {
    const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
    const pipes = [...w.conductors.values()].filter((t) => t.owner === co.id && t.kind === "pipe").sort((a, b) => Math.abs(a.x - post.x) + Math.abs(a.y - post.y) - (Math.abs(b.x - post.x) + Math.abs(b.y - post.y)));
    for (const p of pipes) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const x = p.x + dx, y = p.y + dy;
      if (tp.rules.pipeCost(w, co, x, y, s.content) === 1 && w.buildingAt(x, y) === undefined) {
        const r = await c.placePipes([x, y], 1);
        if (r.success) return `round ${round}: stub at ${x},${y}`;
      }
    }
    return `round ${round}: no stub`;
  }, { post: setup.post, round }));
  await page.waitForTimeout(3500);
  console.log(await check(`after round ${round}`));
}
await shot("1-selected-after-rebuilds");
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

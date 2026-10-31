// What pipes lose on their length, shown wherever the goods are (the owner, 2026-10-31: "if things are lost due to
// pipe length always display this"): a Wind Moily far from the Trading Post, piped to it: its panel's Where it goes
// says what is sent, what arrives and the share lost, and its gold is for what arrives; the pipe's hover says sent and
// arrived; the Pipe tool says what a planned pipe loses over its length before it is laid.   node losses.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/losses"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "lo");
for (const c of ["level", "gold"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const laid = await page.evaluate(async () => {
  const s = () => tp.appStore.getState(); const w = tp.gameClient.world; const c = tp.gameClient.connection;
  const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
  const definition = s().content.building("windMoily");
  const sides = (b, size) => [[-1, 0], [size, 0], [0, -1], [0, size]].map(([dx, dy]) => ({ x: b.x + dx, y: b.y + dy }));
  for (let r = 12; r > 5; r--)
    for (let dy = -r; dy <= r; dy++)
      for (const dx of [-r, r]) {
        const x = post.x + dx, y = post.y + dy;
        if (!tp.rules.canPlace(s().content, w, s().company, s().era, definition, x, y).ok || !(await c.placeBuilding("windMoily", x, y)).success) continue;
        await c.cheat("finish");
        await new Promise((r) => setTimeout(r, 1000));
        let best = null;
        for (const from of sides({ x, y }, 1)) for (const to of sides(post, 2)) {
          const route = tp.rules.routePipe(w, s().company, from, to, s().content);
          if (route && (best === null || route.length < best.length)) best = route;
        }
        if (best === null) return null;
        const placed = await c.placePipes(best.flatMap((t) => [t.x, t.y]), 1);
        return placed.success ? { x, y, route: best } : null;
      }
  return null;
});
check(laid !== null, "a Wind Moily piped to the Trading Post from afar", JSON.stringify(laid?.route?.length));
await page.waitForTimeout(3000);
const id = await page.evaluate(({ x, y }) => tp.gameClient.world.buildingAt(x, y)?.id, laid);
// Its sale solved first (the panel lists flows once there are some)
for (let i = 0; i < 30; i++) {
  const selling = await page.evaluate((id) => [...tp.gameClient.world.flows.values()].some((n) => n.flows.some((f) => f.from === id && f.kind === "sale")), id);
  if (selling) break;
  await page.waitForTimeout(1000);
}
await page.evaluate((id) => tp.gameClient.showBuilding(id), id);
await page.waitForSelector(".flow-groups", { timeout: 15000 });
await page.waitForTimeout(1500);
const where = (await page.locator(".flow-groups").innerText()).replace(/\s+/g, " ");
console.log("where it goes:", where.slice(0, 240));
check(/sent, [\d.,]+ arrive \(\d+% lost on the way\)/.test(where), "Where it goes: sent, what arrives, the share lost", where.slice(0, 200));
const paid = await page.evaluate(() => {
  const f = tp.appStore.getState().buildingDetails.flows.find((t) => t.kind === "sale");
  return f ? { gold: f.gold, delivered: f.delivered, sent: f.sent, price: f.price } : null;
});
console.log("sale", JSON.stringify(paid));
check(paid !== null && Math.abs(paid.gold - paid.delivered * paid.price) < 0.05 && paid.sent > paid.delivered, "its gold is for what arrives", JSON.stringify(paid));
await shot("1-where");

// The pipe's hover: sent and arrived (the pointer on it)
await page.keyboard.press("Escape");
await page.waitForTimeout(400);
const mid = laid.route[Math.floor(laid.route.length / 2)];
await page.evaluate((t) => tp.gameClient.engine.lookAt(t.x, t.y, 1.4), mid);
await page.waitForTimeout(2000);
const at = await page.evaluate((t) => tp.gameClient.engine.tileToScreen(t.x, t.y), mid);
await page.mouse.move(at.x, at.y);
await page.waitForTimeout(1200);
const hover = (await page.locator(".hover-tip").innerText().catch(() => "")).replace(/\s+/g, " ");
console.log("hover:", hover);
check(/sent, [\d.,]+ arrive \(\d+% lost\)/.test(hover), "the pipe's hover: sent and what arrives", hover);
await shot("2-hover");

// The Pipe tool: a planned pipe's loss, before it is laid (dragged from the Wind Moily, not let go)
await page.mouse.move(10, 500);
await page.evaluate(({ x, y, route }) => {
  tp.appStore.setState({ tool: { kind: "pipe", tier: 1 } });
  const end = route[route.length - 1];
  tp.gameClient.engine.lookAt((x + end.x) / 2, (y + end.y) / 2, 0.9);
}, laid);
await page.waitForTimeout(1500);
const ends = await page.evaluate(({ x, y, route }) => [{ x, y }, route[route.length - 1]].map((t) => tp.gameClient.engine.tileToScreen(t.x, t.y)), laid);
await page.mouse.move(ends[0].x, ends[0].y);
await page.mouse.down();
await page.mouse.move(ends[1].x, ends[1].y, { steps: 20 });
await page.waitForTimeout(600);
// A drag the map didn't take (a panel's edge in the way): the planned route as the drag would have made it, the
// pointer resting on the map
if ((await page.evaluate(() => tp.appStore.getState().pipeRoute)) === null) {
  await page.mouse.up();
  await page.mouse.move(ends[1].x, ends[1].y);
  await page.evaluate((route) => tp.appStore.setState({ tool: { kind: "pipe", tier: 1 }, pipeRoute: route }), laid.route);
  await page.waitForTimeout(400);
}
const plan = (await page.locator(".hover-tip").innerText().catch(() => "")).replace(/\s+/g, " ");
console.log("plan:", plan, JSON.stringify(await page.evaluate(() => tp.appStore.getState().pipeRoute?.length ?? null)));
check(/loses \d+% over its \d+ tiles/.test(plan), "the Pipe tool: what it loses over its length", plan);
await page.mouse.click(ends[1].x, ends[1].y, { button: "right" });
await shot("2-plan");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

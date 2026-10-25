// Shared helpers for the e2e screenshot scripts (see README.md). The game runs at http://localhost:5173 (BASE_URL).
import { chromium } from "playwright-core";
import { mkdirSync } from "node:fs";

export async function start(out, { width = 1600, height = 1000, touch = false } = {}) {
  mkdirSync(out, { recursive: true });
  // A Chromium from Playwright's cache (npx playwright install chromium), or CHROMIUM=/path/to/chrome
  const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
  const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
  const page = await browser.newPage({ viewport: { width, height }, hasTouch: touch, isMobile: touch });
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  page.on("console", (m) => { if (m.type() === "error" && !m.text().includes("401")) errors.push(m.text()); });
  const shot = async (name) => { await page.screenshot({ path: `${out}/${name}.png`, animations: "disabled" }); console.log("shot", name); };
  const cheat = (what) => page.evaluate((w) => tp.gameClient.connection.cheat(w).then((r) => r.success || r.error), what);
  return { browser, page, errors, shot, cheat };
}

/** Registers a new player, who picks a plot on the map (1.4 §6: Pick for me, or the plot given) and plays */
export async function newPlayer(page, prefix = "p", seat = null) {
  const user = `${prefix}${Date.now() % 100000}`;
  for (let i = 0; i < 40; i++) { try { await page.goto(`${process.env.BASE_URL ?? "http://localhost:5173"}/`); await page.waitForSelector(".auth-panel", { timeout: 15000 }); break; } catch { await page.waitForTimeout(1000); } }
  await page.click("text=New player");
  await page.fill("input[autocomplete=username]", user);
  await page.fill("input[type=password]", "secret123");
  await page.click("button[type=submit]");
  await pickPlot(page, `${user} Works`, seat);
  return user;
}

/** On the start map: chooses a plot (Pick for me's, or the one given), names the company and plays */
export async function pickPlot(page, name, seat = null) {
  await page.waitForSelector(".picker-top", { timeout: 30000 });
  if (seat !== null) {
    await page.evaluate((seat) => tp.appStore.setState({ picker: { ...tp.appStore.getState().picker, chosen: seat } }), seat);
  } else {
    await page.click("text=Pick for me");
  }
  await page.waitForSelector(".plot-card .start-form", { timeout: 30000 });
  await page.fill(".start-form input[minlength='3']", name);
  await page.click(".start-form button[type=submit]");
  await page.waitForSelector(".top-bar", { timeout: 30000 });
  await page.waitForTimeout(3000);
}

/**
 * A small estate like the old starter kit (1.4: a plot starts empty): two Wind Moilys, a Solar Panel, an Iron Mine on iron
 * ore, a Small Factory and a Storage near the trading post, finished, each piped to the post (what isn't unlocked yet is
 * left out). What was built, as type@x,y
 */
export async function kit(page, types = ["windMoily", "windMoily", "solarPanel", "ironMine", "smallFactory", "storage"]) {
  const built = await page.evaluate(async (types) => {
    const c = tp.gameClient.connection;
    const w = tp.gameClient.world;
    const s = () => tp.appStore.getState();
    const post = [...w.buildings.values()].find((t) => t.owner === s().company.id && t.type === "tradingPost");
    const out = [];
    for (const type of types) {
      const definition = s().content.building(type);
      let spot = null;
      for (let r = 2; r < 16 && !spot; r++)
        for (let dy = -r; dy <= r && !spot; dy++)
          for (let dx = -r; dx <= r && !spot; dx++)
            if (Math.max(Math.abs(dx), Math.abs(dy)) === r && tp.rules.canPlace(s().content, w, s().company, s().era, definition, post.x + dx, post.y + dy).ok)
              spot = { x: post.x + dx, y: post.y + dy };
      if (!spot || !(await c.placeBuilding(type, spot.x, spot.y)).success) continue;
      await c.cheat("finish");
      const size = definition.size;
      const border = [];
      for (let i = 0; i < size; i++) border.push([spot.x + i, spot.y - 1], [spot.x + i, spot.y + size], [spot.x - 1, spot.y + i], [spot.x + size, spot.y + i]);
      for (const [fx, fy] of border) {
        const route = tp.rules.routePipe(w, s().company, { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
        if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) break;
      }
      out.push(`${type}@${spot.x},${spot.y}`);
    }
    return out;
  }, types);
  await page.waitForTimeout(2500);
  return built;
}

/** Builds a building near the trading post (or on a deposit), pipes it to the post and plans and lays the pipe to the Yard gate */
export async function settle(player, type, deposit) {
  return player.page.evaluate(async ({ type, deposit }) => {
    const c = tp.gameClient.connection;
    const w = tp.gameClient.world;
    const s = () => tp.appStore.getState();
    const co = () => s().company;
    const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
    const definition = s().content.building(type);
    const ok = (x, y) => tp.rules.canPlace(s().content, w, co(), s().era, definition, x, y).ok;
    let spot = null;
    for (let r = 2; r < 12 && !spot; r++)
      for (let dy = -r; dy <= r && !spot; dy++)
        for (let dx = -r; dx <= r && !spot; dx++)
          if (ok(post.x + dx, post.y + dy) && (!deposit || w.tile(post.x + dx, post.y + dy)?.deposit === deposit)) spot = { x: post.x + dx, y: post.y + dy };
    if (!spot) return `no spot for ${type}`;
    const placed = await c.placeBuilding(type, spot.x, spot.y);
    if (!placed.success) return placed.error;
    await c.cheat("finish");
    const border = (b) => [[b.x, b.y - 1], [b.x, b.y + 1], [b.x - 1, b.y], [b.x + 1, b.y]];
    let toPost = null;
    for (const [fx, fy] of border(spot)) {
      const route = tp.rules.routePipe(w, co(), { x: fx, y: fy }, { x: post.x - 1, y: post.y }, s().content);
      if (route && (await c.placePipes(route.flatMap((t) => [t.x, t.y]), 1)).success) { toPost = route.length; break; }
    }
    await new Promise((r) => setTimeout(r, 1500));
    // The Yard panel's button: plan the pipe to the gate, then build it
    await tp.gameClient.openYard();
    await tp.gameClient.planYardPipe();
    for (let i = 0; i < 30 && s().plannedRoute === null; i++) await new Promise((r) => setTimeout(r, 300));
    const planned = s().plannedRoute?.length ?? null;
    if (planned !== null) await tp.gameClient.buildPlannedRoute();
    await new Promise((r) => setTimeout(r, 1500));
    return JSON.stringify({ type, spot, toPost, planned, slot: co().slot });
  }, { type, deposit });
}

/** Free plots of one Yard nobody lives at, in any open town (not in the Yards given, as "x,y,index") */
export async function emptySeats(page, count, notIn = []) {
  const found = await findEmptySeats(page, count, notIn);
  if (found.length > 0) return found;
  // A busy test world: the next town (dev cheat, needs a company on the page)
  await page.evaluate(() => tp.gameClient.connection.cheat("district"));
  return findEmptySeats(page, count, notIn);
}

async function findEmptySeats(page, count, notIn) {
  return page.evaluate(async ({ count, notIn }) => {
    const c = tp.gameClient.connection;
    const first = await c.getStations(null, null);
    const free = (t) => t.seats.every((s) => s.state === "free");
    for (const d of [{ districtX: first.pickForMe.districtX, districtY: first.pickForMe.districtY }, ...first.districts]) {
      const dto = await c.getStations(d.districtX, d.districtY);
      const station = dto.stations.find((t) => free(t) && !notIn.includes(`${t.districtX},${t.districtY},${t.index}`) && t.seats.length >= count);
      if (station !== undefined) {
        return station.seats.slice(0, count).map((s) => ({ districtX: station.districtX, districtY: station.districtY, slot: s.slot, station: `${station.districtX},${station.districtY},${station.index}` }));
      }
    }
    return [];
  }, { count, notIn });
}

/** Moves a player's plot to another one and lands it right away (the dev cheat), waiting for its buildings to load */
export async function moveTo(player, seat) {
  return player.page.evaluate(async (seat) => {
    const c = tp.gameClient.connection;
    const result = await c.relocateTo({ districtX: seat.districtX, districtY: seat.districtY, slot: seat.slot });
    if (!result.success) return result.error;
    await c.cheat("arrive");
    // The camera follows the estate; its chunks load there
    for (let i = 0; i < 40 && ![...tp.gameClient.world.buildings.values()].some((t) => t.owner === tp.appStore.getState().company.id && t.type === "tradingPost"); i++) await new Promise((r) => setTimeout(r, 250));
    return `seat ${seat.slot}`;
  }, seat);
}

/** Demolishes a player's starter kit (a House's buildings on its seat), so the test starts from an empty estate */
export async function clearKit(player) {
  return player.page.evaluate(async () => {
    const c = tp.gameClient.connection;
    const me = tp.appStore.getState().company.id;
    const kit = [...tp.gameClient.world.buildings.values()].filter((t) => t.owner === me && t.type !== "hq" && t.type !== "tradingPost");
    for (const b of kit) await c.demolish(b.id);
    return kit.length;
  });
}

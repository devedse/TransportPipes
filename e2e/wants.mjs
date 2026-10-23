// Wants (after run 3, the design team's step 2): Ana places a Market Hall without its 500 bricks; it stands as a scaffold
// that wants them, with its panel saying so and Give up; Ben, her crew mate next door, sees it in his Yard panel, sends
// his Storage's bricks with one tap, and her Market Hall is built. Two browsers.
//   node wants.mjs shots/wants
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/wants";
const base = process.env.BASE_URL ?? "http://localhost:5173";

async function register(page, user) {
  for (let i = 0; i < 40; i++) {
    try {
      await page.goto(`${base}/`);
      await page.waitForSelector(".auth-panel", { timeout: 15000 });
      break;
    } catch {
      await page.waitForTimeout(1000);
    }
  }
  await page.click("text=New player");
  await page.fill("input[autocomplete=username]", user);
  await page.fill("input[type=password]", "secret123");
  await page.click("button[type=submit]");
  await page.waitForSelector(".found-panel", { timeout: 30000 });
}
async function closeDialogs(page) {
  for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
    await page.keyboard.press("Escape");
    await page.waitForTimeout(400);
  }
}
const cheat = (page, what) => page.evaluate((w) => tp.gameClient.connection.cheat(w), what);
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};
const id = Date.now() % 100000;

// Ana starts a crew with a seat for Ben
const ana = await start(`${out}/ana`);
// Each browser an address of its own: two accounts on one device can't trade (the dev server takes this where cheats are on)
await ana.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.7.${id % 250}.1` });
await register(ana.page, `ana${id}`);
const seat = await ana.page.evaluate(() => tp.gameClient.connection.getStations(null, null).then((t) => t.friends ?? t.neighbours));
const crew = await ana.page.evaluate(
  (s) => tp.gameClient.connection.startWithFriends(`Ana ${Date.now() % 100000} Pipes`, "#e07a3f", `Kiln ${Date.now() % 1000}`, "kettle", "#24575a", "#c9963a", s, 1),
  seat,
);
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".toolbar", { timeout: 60000 });
await ana.page.waitForTimeout(1500);
await closeDialogs(ana.page);

// Ben moves into it, and builds a Storage that holds 600 bricks
const ben = await start(`${out}/ben`);
await ben.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.7.${id % 250}.2` });
await register(ben.page, `ben${id}`);
await ben.page.evaluate((code) => tp.gameClient.foundCompany(`Ben ${Date.now() % 100000} Co`, "#3f8ae0", null, code), crew.holds[0].code);
await ben.page.waitForSelector(".toolbar", { timeout: 60000 });
await ben.page.waitForTimeout(1500);
await closeDialogs(ben.page);
await cheat(ben.page, "gold");
const storage = await ben.page.evaluate(async () => {
  const s = tp.appStore.getState();
  const w = tp.gameClient.world;
  const co = s.company;
  const def = s.content.building("storage");
  for (let y = co.estateY + 4; y < co.estateY + 28; y++)
    for (let x = co.estateX + 4; x < co.estateX + 28; x++)
      if (tp.rules.canPlace(s.content, w, co, s.era, def, x, y).ok) {
        const r = await tp.gameClient.connection.placeBuilding("storage", x, y);
        return r.success ? { x, y } : r.error;
      }
  return null;
});
await cheat(ben.page, "finish");
await ben.page.waitForTimeout(1000);
await cheat(ben.page, "bricks");
console.log("Ben's Storage:", JSON.stringify(storage));

// Ana: Level 16, gold, and a Market Hall without bricks
for (let i = 0; i < 4; i++) await cheat(ana.page, "level");
for (let i = 0; i < 2; i++) await cheat(ana.page, "earn");
await ana.page.waitForTimeout(1500);
const placed = await ana.page.evaluate(async () => {
  const s = tp.appStore.getState();
  const w = tp.gameClient.world;
  const co = s.company;
  const def = s.content.building("marketHall");
  for (let y = co.estateY + 4; y < co.estateY + 28; y++)
    for (let x = co.estateX + 4; x < co.estateX + 28; x++)
      if (tp.rules.canPlace(s.content, w, co, s.era, def, x, y).ok) {
        const r = await tp.gameClient.connection.placeBuilding("marketHall", x, y);
        if (!r.success) return { error: r.error };
        await new Promise((res) => setTimeout(res, 1500));
        const b = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "marketHall");
        tp.gameClient.showBuilding(b.id);
        return { id: b.id };
      }
  return { error: "no spot" };
});
expect("Ana's Market Hall stands without its bricks", placed.id !== undefined, JSON.stringify(placed));
await ana.page.waitForSelector(".site-wants", { timeout: 15000 }).catch(() => null);
await ana.page.waitForTimeout(1500);
const panel = await ana.page.locator(".site-wants").innerText().catch(() => "");
await ana.shot("1-scaffold");
expect("its panel says what it wants, and Give up", /Wants 500 bricks/.test(panel) && /Give it up/.test(panel), panel.replace(/\s+/g, " ").slice(0, 160));

// Ben sees it and sends his bricks
await ben.page.evaluate(() => tp.gameClient.refreshCompany());
await ben.page.waitForTimeout(1000);
const near = await ben.page.evaluate(() => tp.appStore.getState().company.wantsNear);
expect("Ben sees Ana's want, with what his Storage holds", near?.some((w) => w.resource === "bricks" && w.holds >= 500), JSON.stringify(near));
await ben.page.evaluate(() => tp.gameClient.openYard?.() ?? tp.gameClient.openNeighbours());
await ben.page.waitForTimeout(1500);
expect("his Yard panel offers 'Send them ▸'", (await ben.page.locator("button", { hasText: "Send them ▸" }).count()) > 0);
await ben.shot("2-ben-yard");
const tapped = await ben.page.evaluate((s) => tp.gameClient.connection.sendToSite(s), placed.id);
console.log("Ben's tap:", JSON.stringify(tapped));
await ben.page.waitForTimeout(2500);

// Ana's Market Hall is built
await ana.page.waitForTimeout(2000);
const after = await ana.page.evaluate((b) => {
  const building = tp.gameClient.world.buildings.get(b);
  return { site: building?.siteShare ?? null, ends: building?.constructionEndsAt ?? null };
}, placed.id);
await ana.shot("3-building");
expect("Ana's Market Hall has its bricks and is being built", after.site === null && after.ends !== null && !after.ends.startsWith("9999"), JSON.stringify(after));

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", [...ana.errors, ...ben.errors].join(" | ") || "none");
await ana.browser.close();
await ben.browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

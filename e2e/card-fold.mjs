// The return card leads with what matters (after run 3, the analysts' rows 9 and 11 and the designers' rule): Ana and
// Ben are a crew in one station; Ana's Market Hall stands as a scaffold that wants 500 bricks. Ben comes back after ten
// days: his card has at most six lines, one about people, "Order the rest for Ana" though he makes no bricks, and no
// crew line that says it twice; the rest folds under "more". Two browsers.
//   node card-fold.mjs shots/card-fold
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/card-fold";
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
  await page.waitForSelector(".picker-top", { timeout: 30000 });
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

// Ana starts a crew with a seat for Ben; Ben moves into it
const ana = await start(`${out}/ana`);
await ana.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.8.${id % 250}.1` });
await register(ana.page, `ana${id}`);
// Ana settles in an empty Yard and gets her "Play next to me" link (1.4 §6)
const empty = await ana.page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const first = await c.getStations(null, null);
  for (const d of [{ districtX: first.pickForMe.districtX, districtY: first.pickForMe.districtY }, ...first.districts]) {
    const dto = await c.getStations(d.districtX, d.districtY);
    const station = dto.stations.find((t) => t.seats.every((s) => s.state === "free"));
    if (station !== undefined) return { districtX: station.districtX, districtY: station.districtY, slot: station.seats[0].slot };
  }
  return null;
});
await ana.page.evaluate((s) => tp.gameClient.connection.createCompany(`Ana ${Date.now() % 100000} Pipes`, "#e07a3f", s, null), empty);
const link = await ana.page.evaluate(() => tp.gameClient.connection.getNextToMeLink());
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".toolbar", { timeout: 60000 });
await ana.page.waitForTimeout(1500);
await closeDialogs(ana.page);

const ben = await start(`${out}/ben`, { width: 1280, height: 860 });
await ben.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.8.${id % 250}.2` });
await register(ben.page, `ben${id}`);
await ben.page.evaluate((code) => tp.gameClient.foundCompany(`Ben ${Date.now() % 100000} Co`, "#3f8ae0", null, code), link);
await ben.page.waitForSelector(".toolbar", { timeout: 60000 });
await ben.page.waitForTimeout(1500);
await closeDialogs(ben.page);
await cheat(ben.page, "gold");

// Ana: a Market Hall without its bricks
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
        return r.success ? "placed" : r.error;
      }
  return "no spot";
});
expect("Ana's Market Hall stands as a scaffold", placed === "placed", placed);

// Ben comes back after ten days
await cheat(ben.page, "gone");
await ben.page.evaluate(() => tp.gameClient.hello());
await ben.page.waitForSelector(".return-card", { timeout: 30000 });
await ben.page.waitForTimeout(800);
const card = await ben.page.evaluate(() => tp.appStore.getState().returnCard);
console.log("lines:", JSON.stringify(card.lines.map((t) => `${t.kind}: ${t.text}`), null, 1));
console.log("folded:", JSON.stringify((card.folded ?? []).map((t) => `${t.kind}: ${t.text}`)));
await ben.shot("1-card");
expect("at most six lines lead", card.lines.length <= 6, `${card.lines.length}`);
expect("one line about people", card.lines.filter((t) => t.kind === "people" || t.kind === "social").length <= 1);
const order = card.lines.find((t) => t.action === "orderWant") ?? (card.folded ?? []).find((t) => t.action === "orderWant");
expect("Ana's want, to order for her though he makes no bricks", order !== undefined && /Market Hall wants 500 bricks/.test(order.text), order?.text ?? "none");
const crewLine = card.lines.find((t) => t.kind === "crew");
expect("the crew line doesn't repeat the want the card offers", crewLine === undefined || !/Market Hall wants/.test(crewLine.text), crewLine?.text ?? "none");
const button = ben.page.locator(".return-card button", { hasText: /^Order the rest for/ });
if ((await button.count()) === 0 && (await ben.page.locator(".return-more").count()) > 0) {
  await ben.page.click(".return-more");
  await ben.page.waitForTimeout(400);
  await ben.shot("2-more");
}
expect("its button says what it costs", /^Order the rest for .* · [\d.,k]+ g$/.test((await button.first().innerText().catch(() => "")) ?? ""), await button.first().innerText().catch(() => "none"));

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", [...ana.errors, ...ben.errors].join(" | ") || "none");
await ana.browser.close();
await ben.browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

// Scripted replays (docs/playtest-fixes.html §6): a moment of run 1 restored on today's build, the clock moved on, and
// the numbers checked against the fix plan's marks. Every replay must first fail on the build run 1 played, then pass
// once its fix ships. They run on the playtest server (playtest/bin, :5090) in a run folder of their own.
//   node playtest/replay.mjs [--publish] <scenario> [--from <run>]
//     --publish   builds the server into playtest/bin first (do it after every change to the game)
//     --from      the run whose snapshots to restore (default story1)
// Scenarios:
//   row10-yard    day 14 19:00 + 6 h, nobody acting: incomes, what the Yard sends that nobody takes, Cleo's Lab
//   return-d27    day 14 19:00 to day 27 02:30, nobody acting: Ana's return card names Ben's stand-in (F5)
//   return-phone  day 36 18:00: Ben opens the game on his phone; one card, then the map in at most two taps (F5)
//   keystone      run 3 (--from story3) from day 10 19:55 to day 22, everyone playing when the story has them play:
//                 Capital SE's Workshop and NW's Water Tower move on instead of waiting weeks on water (after run 3, row 1)
//   send-desktop  day 14 19:00: Dev's stuck Sand Dredger, one tap lays its pipe and sends to the Water Tower; his sand
//   send-phone    arrives within 20 game minutes and the first gift is announced (step 4; on a desktop, on a phone)
//                 (needs Vite on :5174 with TP_SERVER=http://localhost:5090)
import { execSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const SERVER = process.env.PLAYTEST_SERVER ?? "http://localhost:5090";
const TOKEN = process.env.PLAYTEST_TOKEN ?? "playtest-local";

const args = process.argv.slice(2);
const publish = args.includes("--publish");
const fromIndex = args.indexOf("--from");
const from = fromIndex >= 0 ? args[fromIndex + 1] : "story1";
const scenario = args.find((t, i) => !t.startsWith("--") && args[i - 1] !== "--from");

// Run 1's companies
const ANA = 25, CLEO = 26, BEN = 27, MIA = 28, DEV = 29;
const NAMES = { [ANA]: "Ana", [CLEO]: "Cleo", [BEN]: "Ben", [MIA]: "Mia", [DEV]: "Dev" };

const env = { ...process.env, PLAYTEST_RUN: `replay-${scenario}`, PLAYTEST_SNAPSHOTS: path.join(ROOT, "runs", from, "snapshots") };
const director = (...cmd) => execSync(`node ${path.join(ROOT, "director.mjs")} ${cmd.join(" ")}`, { env, stdio: "pipe" }).toString().trim();

async function api(method, route, body) {
  const res = await fetch(`${SERVER}/playtest/${route}`, {
    method,
    headers: { "X-Playtest-Token": TOKEN, "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${route}: ${res.status}`);
  return res.json();
}

const companies = async () => Object.fromEntries((await api("GET", "companies")).players.map((p) => [p.id, p]));
async function advance(hours, step = 1) {
  for (let left = hours; left > 1e-6; left -= step) await api("POST", "advance", { hours: Math.min(step, left) });
}

const results = [];
function check(name, ok, detail) {
  results.push({ name, ok });
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}${detail ? `  (${detail})` : ""}`);
}
const round = (x, d = 1) => Number(x).toFixed(d);

/** A player's browser on the playtest client with run 1's login: a desktop, or a phone with touch */
async function browserFor(player, phone) {
  const { chromium } = createRequire(new URL("../e2e/package.json", import.meta.url))("playwright-core");
  const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
  const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
  const context = await browser.newContext({
    viewport: phone ? { width: 420, height: 860 } : { width: 1600, height: 1000 },
    hasTouch: phone,
    isMobile: phone,
    storageState: path.join(ROOT, "runs", from, player, "storage.json"),
  });
  const page = await context.newPage();
  await page.goto(process.env.PLAYTEST_URL ?? "http://localhost:5174");
  await page.waitForSelector(".toolbar", { timeout: 90_000 });
  await page.waitForTimeout(3000);
  // The return card first: Continue
  for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
    await page.locator("[role=dialog]:visible button.primary").last().click();
    await page.waitForTimeout(800);
  }
  return { browser, page };
}

/** Step 4's gate: Dev's stuck dredger, one tap, his sand at the Water Tower within 20 game minutes, the first gift announced */
async function sendScenario(phone) {
  console.log(director("restore", "row10"));
  const before = (await companies())[DEV];
  const dredger = before.idle.find((t) => t.type === "sandDredger");
  console.log("Dev's dredger:", JSON.stringify(dredger ?? null), "basket:", JSON.stringify(before.projectBasket));
  const { browser, page } = await browserFor("dev", phone);
  const dir = path.join(ROOT, "runs", `replay-send-${phone ? "phone" : "desktop"}`);
  mkdirSync(dir, { recursive: true });
  try {
    const id = dredger?.id ?? (await page.evaluate(() => [...tp.gameClient.world.buildings.values()].find((t) => t.type === "sandDredger" && t.owner === tp.appStore.getState().company.id)?.id));
    await page.evaluate((building) => tp.gameClient.showBuilding(building), id);
    await page.waitForSelector(".neighbour-trade", { timeout: 20_000 });
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(dir, "1-panel.png") });
    // The panel's one fix button is the Send that lays the pipe (or the Neighbours line's own)
    const button = page.locator("button", { hasText: /send to the Water Tower|^Send ▸$/ }).first();
    const label = await button.innerText();
    console.log("the button:", label);
    if (phone) await button.tap();
    else await button.click();
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(dir, "2-sent.png") });
    const sent = (await companies())[DEV];
    check("one tap sends Dev's sand to the project", sent.sends.includes("sand>project"), sent.sends.join(", "));
    await advance(20 / 60, 5 / 60);
    await page.waitForTimeout(1500);
    const after = (await companies())[DEV];
    const delivered = (basket) => Number(String(basket?.sand ?? basket?.["sand*"] ?? "0").split(" / ")[0]);
    console.log("basket after:", JSON.stringify(after.projectBasket));
    check("his sand is at the Water Tower within 20 game minutes", delivered(after.projectBasket) > delivered(before.projectBasket), `${delivered(before.projectBasket)} → ${delivered(after.projectBasket)}`);
    check("the first gift is announced", after.giftsAnnounced.some((t) => t.startsWith("project:") && t.endsWith(":sand")), after.giftsAnnounced.join(", "));
    await page.screenshot({ path: path.join(dir, "3-after.png") });
  } finally {
    await browser.close();
  }
}

const scenarios = {
  /** F5: Ana comes back on day 27, after Ben's stand-in was announced (row 13 of run 1): her one card says so */
  async "return-d27"() {
    console.log(director("restore", "row10"));
    const story = JSON.parse(execSync(`cat ${path.join(ROOT, "runs", from, "story.json")}`).toString());
    const target = new Date(story.day1).getTime() + (26 * 24 + 4.5) * 3600e3;
    const { now } = await api("GET", "now");
    await advance((target - new Date(now).getTime()) / 3600e3, 6);
    const card = await api("GET", `card/${ANA}`);
    console.log(JSON.stringify(card, null, 2));
    check("Ana gets a card", card !== null);
    const lines = card?.lines ?? [];
    check("it names Ben's stand-in", lines.some((t) => /Riverside Co\./.test(t.text) && /stand/.test(t.text)), lines.map((t) => t.text).join(" | "));
    check("six lines at most", lines.length <= 6, `${lines.length}`);
    check("on the server's clock", /day|h|min/.test(card?.away ?? ""), card?.away);
  },

  /** Step 4: Dev sends his sand to the Water Tower with one tap, on a desktop */
  async "send-desktop"() {
    await sendScenario(false);
  },

  /** Step 4: the same on a phone, with touch */
  async "send-phone"() {
    await sendScenario(true);
  },

  /** F5: Ben comes back on his phone on day 36 (row 14 of run 1, after three dialogs): one card, then the map */
  async "return-phone"() {
    console.log(director("restore", "row14"));
    const { chromium } = createRequire(new URL("../e2e/package.json", import.meta.url))("playwright-core");
    const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
    const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
    try {
      const context = await browser.newContext({
        viewport: { width: 420, height: 860 },
        hasTouch: true,
        isMobile: true,
        storageState: path.join(ROOT, "runs", from, "ben", "storage.json"),
      });
      const page = await context.newPage();
      await page.goto(process.env.PLAYTEST_URL ?? "http://localhost:5174");
      await page.waitForSelector(".toolbar, [role=dialog]", { timeout: 90_000 });
      await page.waitForTimeout(4000);
      const dir = path.join(ROOT, "runs", `replay-return-phone`);
      mkdirSync(dir, { recursive: true });
      await page.screenshot({ path: path.join(dir, "ben-card.png") });
      const dialogs = page.locator("[role=dialog]:visible");
      const first = await dialogs.count();
      const card = first > 0 ? await dialogs.first().innerText() : "";
      console.log(card);
      let taps = 0;
      while ((await dialogs.count()) > 0 && taps < 6) {
        await dialogs.first().locator("button.primary").last().tap();
        taps++;
        await page.waitForTimeout(1500);
      }
      await page.screenshot({ path: path.join(dir, "ben-after.png") });
      check("one card on the way back", first === 1 && /While you were away/.test(card), `${first} dialog(s)`);
      check("it names what happened to Ben's estate", /stood in|stand/i.test(card), card.split("\n").slice(0, 4).join(" / "));
      check("the map in at most two taps", taps <= 2 && (await dialogs.count()) === 0, `${taps} tap(s)`);
    } finally {
      await browser.close();
    }
  },

  /** Run 3's frozen projects (docs/playtest-run3-review.html, row 1): the residents play when run 3's story had them play */
  async keystone() {
    const S3 = { jo: 25, mia: 26, ana: 27, ben: 28, mia2: 29, cleo: 30, dev: 31 };
    mkdirSync(path.join(ROOT, "runs", env.PLAYTEST_RUN), { recursive: true });
    writeFileSync(path.join(ROOT, "runs", env.PLAYTEST_RUN, "story.json"), readFileSync(path.join(ROOT, "runs", from, "story.json")));
    console.log(director("restore", "row09"));
    const sessions = [[10, "20:00", ["cleo", "dev", "jo"]], [14, "19:00", ["ana", "cleo", "dev", "jo"]], [15, "21:00", ["mia", "mia2", "jo"]], [21, "19:00", ["ana", "cleo", "dev", "jo"]]];
    const stage = (c) => `${c.projectStage} [${(c.projectFinished ?? []).join(", ")}] ${JSON.stringify(c.projectBasket)}`;
    const seen = [];
    for (const [day, time, who] of [...sessions, [22, "20:00", []]]) {
      console.log(director("goto", day, time));
      for (const id of who) await api("POST", `seen/${S3[id]}`);
      const c = await companies();
      seen.push({ day, se: c[S3.ana], nw: c[S3.mia] });
      console.log(`day ${day} ${time}  SE: ${stage(c[S3.ana])}\n            NW: ${stage(c[S3.mia])}\n            SE says: ${c[S3.ana].week}`);
    }
    const end = seen.at(-1);
    check("Capital SE's Workshop doesn't wait on water to day 22", !(end.se.projectStage === "Workshop" && "water*" in (end.se.projectBasket ?? {})), stage(end.se));
    check("Capital SE stands past the Workshop by day 22", (end.se.projectFinished ?? []).includes("Workshop"), stage(end.se));
    check("Capital NW's Water Tower doesn't wait on water to day 22", !(end.nw.projectStage === "WaterTower" && "water*" in (end.nw.projectBasket ?? {})), stage(end.nw));
  },

  /** Every middle-game Yard bug at once: six hours of nobody acting from day 14 19:00 (A2 row10, step 1) */
  async "row10-yard"() {
    console.log(director("restore", "row10"));
    await advance(1);
    const a = await companies();
    await advance(5);
    const b = await companies();
    const minutes = 5 * 60;
    const growth = (id) => (b[id].gold - a[id].gold) / minutes;
    const delta = (id, key) => (b[id][key] ?? 0) - (a[id][key] ?? 0);
    for (const [id, mark] of [[BEN, 70], [ANA, 200], [DEV, 120], [MIA, 90]]) {
      check(`${NAMES[id]} earns at least ${mark} g/min`, growth(id) >= mark, `${round(growth(id))} g/min`);
    }
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const sent = delta(id, "yardSent");
      const untaken = delta(id, "yardUntaken");
      if (sent > 1) check(`${NAMES[id]}: what nobody takes is at most 5% of what the Yard sends`, untaken <= 0.05 * sent, `${round(untaken, 0)} of ${round(sent, 0)} units`);
    }
    const labEnergy = b[CLEO].yardImports?.energy ?? 0;
    check("Cleo's estate is matched under 1 energy a minute", labEnergy < 1, `${round(labEnergy, 2)}/min`);
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const shown = b[id].goldPerMinute;
      check(`${NAMES[id]}'s top bar shows what was paid`, Math.abs(shown - growth(id)) <= 0.02 * Math.max(1, Math.abs(growth(id))), `${round(shown)} shown, ${round(growth(id))} real`);
    }
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const shown = b[id].goldPerMinute;
      const real = growth(id);
      console.log(`info  ${NAMES[id]}: real ${round(real)} g/min, top bar ${round(shown)}, paid by the Yard ${round(delta(id, "yardPaid") / minutes)}, hub ${round(delta(id, "hubGold") / minutes)}, Houses ${round(delta(id, "npcGold") / minutes)}, project ${round(delta(id, "projectGold") / minutes)}, Yard matched ${round(b[id].yardMatched)}`);
    }
  },
};

if (!scenarios[scenario]) {
  console.log(`Scenarios: ${Object.keys(scenarios).join(", ")}`);
  process.exit(1);
}
if (publish) {
  console.log("Publishing the server into playtest/bin…");
  execSync(`dotnet publish ${path.join(ROOT, "..", "TransportPipes.Server")} -c Release -o ${path.join(ROOT, "bin")} --nologo -v q`, { stdio: "inherit", env: { ...process.env, DOTNET_ROOT: `${process.env.HOME}/.dotnet`, PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}` } });
}
try {
  await scenarios[scenario]();
} finally {
  console.log(director("server", "stop"));
}
const failed = results.filter((t) => !t.ok).length;
console.log(`\n${scenario}: ${results.length - failed} passed, ${failed} failed`);
process.exit(failed ? 1 : 0);

// Statistics (docs/design-notes/statistics.md): two and a half days of play on the playtest clock, with stretches away,
// then the panel: the cards and graphs, key moments and the journal, the away stretches, the return card's graph,
// another player's public part, compare, the admin's Pace and the phone.
// Needs a server with the playtest clock and cheats, on a fresh database (see README): node statistics.mjs <outdir>
import { start, newPlayer, kit, pickPlot } from "./lib.mjs";
const [out = "shots/statistics"] = process.argv.slice(2);
const API = process.env.API_URL ?? "http://localhost:5080";
const BASE = process.env.BASE_URL ?? "http://localhost:5173";
const TOKEN = process.env.PLAYTEST_TOKEN ?? "stats-local";
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text) => { console.log(`${ok ? "ok  " : "FAIL"} ${text}`); if (!ok) failures++; };
const advance = async (hours) => {
  for (let left = hours; left > 1e-9; left -= 2) {
    const r = await fetch(`${API}/playtest/advance`, { method: "POST", headers: { "Content-Type": "application/json", "X-Playtest-Token": TOKEN }, body: JSON.stringify({ hours: Math.min(2, left) }) });
    if (!r.ok) throw new Error(`advance: ${r.status} (is the server on the playtest clock?)`);
  }
  await fetch(`${API}/playtest/save`, { method: "POST", headers: { "X-Playtest-Token": TOKEN } });
};
const game = async () => {
  await page.goto(`${BASE}/`);
  await page.waitForSelector(".top-bar", { timeout: 30000 });
  await page.waitForTimeout(2500);
};
// Playing: a quarter hour at a time, each with something done
const play = async (quarters, act) => { for (let i = 0; i < quarters; i++) { await advance(0.25); await act?.(i); } };
const away = async (hours) => { await page.goto("about:blank"); await page.waitForTimeout(1500); await advance(hours); await game(); };
const upgradeSome = () => page.evaluate(async () => {
  const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  const own = [...w.buildings.values()].filter((t) => t.owner === co.id && !["hq", "tradingPost"].includes(t.type));
  const r = await tp.gameClient.connection.upgradeMany(own.map((t) => t.id), 1, null);
  return r.success;
});

// A neighbour in the same town to compare with, first and in a browser of its own (a page behind one that draws the map
// gets no time to run)
const neighbour = await start(out);
await newPlayer(neighbour.page, "st");
await neighbour.cheat("gold");
await kit(neighbour.page, ["windMoily", "windMoily", "solarPanel"]);
const otherName = await neighbour.page.evaluate(() => tp.appStore.getState().company.name);
await neighbour.browser.close();
console.log("neighbour", otherName);

// The admin (the seeded user) plays: their Pace is checked at the end
await page.goto(`${BASE}/`);
await page.waitForSelector(".auth-panel", { timeout: 30000 });
await page.click(".auth-panel button:has-text('Log in')");
await page.fill("input[autocomplete=username]", "admin");
await page.fill("input[type=password]", "a");
await page.click("button[type=submit]");
const playing = await Promise.race([
  page.waitForSelector(".picker-top", { timeout: 30000 }).then(() => "picker"),
  page.waitForSelector(".top-bar", { timeout: 30000 }).then(() => "playing"),
]);
if (playing === "picker") await pickPlot(page, "Graphs & Co");
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
// Gold to build with (gold the cheat gives isn't earned: the graphs show what the plot makes)
for (const c of ["gold", "gold", "gold", "gold"]) await cheat(c);
console.log("kit", (await kit(page, ["windMoily", "windMoily", "windMoily", "solarPanel", "ironMine", "smallFactory"])).join(" "));


// Day 1: an evening's play, a night away, a morning's play with land and research, a day away, an evening's play
await play(8, async (i) => { if (i % 2 === 1) await upgradeSome(); });
console.log("played the first evening");
await away(9);
// The return card: what came while away, with its graph
const card = await page.waitForSelector(".return-card", { timeout: 15000 }).then(() => true).catch(() => false);
check(card, "a return card after 9 hours away");
// (attached: a steady income is a flat line, which has no height)
const cardGraph = await page.waitForSelector(".return-card .return-graph .sparkline-line", { state: "attached", timeout: 8000 }).then(() => true).catch(() => false);
check(cardGraph, "it shows your income while you were away");
await shot("1-return-card");
await page.locator(".return-card button.primary").click().catch(() => undefined);
await play(4, async (i) => {
  if (i === 0) {
    const bought = await page.evaluate(async () => {
      for (let p = 0; p < 16; p++) if ((await tp.gameClient.connection.buyParcel(p, 0)).success) return p;
      return null;
    });
    console.log("parcel", bought);
  }
  if (i === 1) await upgradeSome();
});
await away(8);
await page.locator(".return-card button.primary").click().catch(() => undefined);
await play(8, async (i) => { if (i % 3 === 0) await upgradeSome(); });
await away(11);
await page.locator(".return-card button.primary").click().catch(() => undefined);
await play(6, async (i) => { if (i === 2) await upgradeSome(); });
// A last day away: two days in all, so All counts the company's days
await away(12);
await page.locator(".return-card button.primary").click().catch(() => undefined);
await play(2);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));

// The panel: J
await page.keyboard.press("j");
await page.waitForSelector(".stats-panel .stats-chart svg", { timeout: 15000 });
await page.waitForTimeout(1500);
await shot("2-week-income");
const cards = await page.locator(".stats-card .stats-card-name").allInnerTexts();
console.log("cards", cards.join(" | "));
for (const name of ["Income", "Company value", "Gold earned", "Earned by source", "Gold in the bank", "Company Level", "Buildings", "Land"])
  check(cards.some((t) => t.trim() === name), `a card: ${name}`);
check(cards.some((t) => t.trim() === "Energy"), "a production card per good (Energy)");
const lines = await page.locator(".stats-chart .stats-line").count();
check(lines >= 1, `the graph draws its line (${lines})`);
const awayBands = await page.locator(".stats-chart .stats-away").count();
check(awayBands >= 3, `the three stretches away are shaded (${awayBands})`);
const flags = await page.locator(".stats-chart .stats-flag").count();
check(flags >= 3, `key moments are flags on the graph (${flags})`);
const journal = await page.locator(".stats-journal").innerText();
for (const moment of ["Graphs & Co founded", "First Energy", "Company Level", "Bought parcel"])
  check(journal.includes(moment), `the journal: ${moment}`);
const insights = await page.locator(".stats-insights").innerText();
console.log("insights", insights.replace(/\n/g, " / "));
check(/while you were away/.test(insights), "an insight about the gold made while away");

// Hover: the value and time
const box = await page.locator(".stats-chart svg").boundingBox();
await page.mouse.move(box.x + box.width * 0.6, box.y + box.height * 0.4);
await page.waitForTimeout(400);
const tip = await page.locator(".stats-tip").innerText().catch(() => "");
check(/day \d/.test(tip) && /\/min/.test(tip), `hovering shows the time and the value: ${tip.replace(/\n/g, " / ")}`);
await shot("3-hover");

// A journal moment points at its place on the graph
await page.locator(".stats-journal li button").first().click();
await page.waitForTimeout(300);
check((await page.locator(".stats-chart .stats-pointed").count()) === 1, "a journal moment points at the graph");

// Earned by source: stacked
await page.locator(".stats-card", { hasText: "Earned by source" }).click();
await page.waitForTimeout(600);
check((await page.locator(".stats-chart .stats-band").count()) >= 1, "earned by source is stacked");
const legend = await page.locator(".stats-legend").innerText();
check(/The market/.test(legend), `with a legend: ${legend.replace(/\n/g, " ")}`);
await shot("4-earned-by-source");

// → steps to the next graph
const before = await page.locator(".stats-title h3").innerText();
await page.keyboard.press("ArrowRight");
await page.waitForTimeout(300);
check((await page.locator(".stats-title h3").innerText()) !== before, "→ steps to the next graph");

// Ranges: 24 hours shows the clock, All the days
await page.locator(".stats-ranges .tab", { hasText: "24 hours" }).click();
await page.waitForTimeout(1500);
// (SVG text has no innerText)
const clock = await page.locator(".stats-chart .stats-time text").allTextContents();
check(clock.length >= 3 && clock.every((t) => /^\d\d:00$/.test(t)), `24 hours: the clock (${clock.join(" ")})`);
await page.locator(".stats-ranges .tab", { hasText: "All" }).click();
await page.waitForTimeout(1500);
await page.locator(".stats-card", { hasText: "Company value" }).click();
await page.waitForTimeout(500);
const days = await page.locator(".stats-chart .stats-time text").allTextContents();
check(days.some((t) => /^Day \d+$/.test(t)), `All: the company's days (${days.join(" ")})`);
await shot("5-all-value");

// Compare with the neighbour
await page.locator(".stats-ranges .tab", { hasText: "7 days" }).click();
await page.waitForTimeout(1200);
await page.locator(".stats-card", { hasText: "Income" }).first().click();
const peer = page.locator(".stats-peer", { hasText: otherName });
check((await peer.count()) === 1, `${otherName} is offered to compare with`);
if ((await peer.count()) === 1) {
  await peer.click();
  await page.waitForTimeout(1500);
  check((await page.locator(".stats-chart .stats-line.compare").count()) === 1, "their line is drawn");
  await shot("6-compare");
  await peer.click();
}

// The admin's Pace
await page.locator(".stats-views .tab", { hasText: "Pace" }).click();
await page.waitForSelector(".stats-pace .stats-chart svg", { timeout: 15000 });
await page.waitForTimeout(800);
check((await page.locator(".stats-pace .stats-line.dashed").count()) === 1, "the Pace has the model's player");
check((await page.locator(".stats-pace .stats-line").count()) >= 3, "and every company");
await shot("7-pace");
await page.locator(".stats-views .tab", { hasText: "Graphs" }).click();
await page.keyboard.press("Escape");
await page.waitForTimeout(300);
check((await page.locator(".stats-panel").count()) === 0, "Esc closes it");

// Someone else's statistics, from the leaderboard: the public part
await page.keyboard.press("l");
await page.waitForSelector(".leaderboard-panel", { timeout: 10000 });
await page.waitForTimeout(800);
await page.locator(".leaderboard-panel tr", { hasText: otherName }).locator(".stats-link").click();
await page.waitForSelector(".stats-panel .stats-chart svg", { timeout: 15000 });
await page.waitForTimeout(1200);
const title = await page.locator(".stats-panel h2").innerText();
check(title.includes(otherName), `their statistics: ${title}`);
const theirCards = await page.locator(".stats-card .stats-card-name").allInnerTexts();
check(!theirCards.some((t) => t.trim() === "Gold in the bank" || t.trim() === "Earned by source"), `only what everyone may see (${theirCards.join(" | ")})`);
check((await page.locator(".stats-insights").count()) === 0, "no insights for someone else");
await shot("8-theirs");
await page.keyboard.press("Escape");

// The Company panel's numbers open their graph
await page.keyboard.press("i");
await page.waitForSelector(".company-panel button.stat", { timeout: 10000 });
await page.locator(".company-panel button.stat", { hasText: "Net worth" }).click();
await page.waitForSelector(".stats-panel", { timeout: 10000 });
await page.waitForTimeout(800);
check((await page.locator(".stats-title h3").innerText()).includes("Company value"), "Net worth in the Company panel opens Company value");
await page.keyboard.press("Escape");

// The phone
// (the same login in a phone-sized page; the big page goes, so the phone's gets the time to draw)
const login = await page.context().storageState();
await page.close();
const phone = await (await browser.newContext({ storageState: login, viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true })).newPage();
await phone.goto(`${BASE}/`);
await phone.waitForSelector(".top-bar", { timeout: 30000 });
await phone.waitForTimeout(2500);
await phone.locator(".return-card button.primary").click().catch(() => undefined);
await phone.evaluate(() => { tp.appStore.setState({ levelUp: null }); return tp.gameClient.openStatistics(null, "income"); });
await phone.waitForSelector(".stats-panel .stats-chart svg", { timeout: 15000 });
await phone.waitForTimeout(1200);
await phone.screenshot({ path: `${out}/9-phone.png` });
const overflow = await phone.evaluate(() => {
  const panel = document.querySelector(".stats-panel").getBoundingClientRect();
  const chart = document.querySelector(".stats-chart svg").getBoundingClientRect();
  return { panel: [Math.round(panel.left), Math.round(panel.right)], chart: [Math.round(chart.left), Math.round(chart.right)], width: innerWidth };
});
check(overflow.panel[0] >= 0 && overflow.panel[1] <= overflow.width && overflow.chart[1] <= overflow.panel[1], `the phone: the panel and graph fit (${JSON.stringify(overflow)})`);
await phone.locator(".stats-journal").scrollIntoViewIfNeeded();
await phone.screenshot({ path: `${out}/10-phone-journal.png` });

console.log(errors.length ? errors.join("\n") : "no errors");
console.log(failures ? `done with ${failures} failures` : "PASS");
await browser.close();

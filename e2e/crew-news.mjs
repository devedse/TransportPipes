// The crew's news in one place, and friends who live elsewhere (after run 3, the analysts' row 12): Ana and Ben are a
// crew; their chat has a Crew and a Station channel first, and the project is named by its station. Ben moves to another
// station: Ana's Crew panel lists him with Visit and Link ▸, Link ▸ opens a new link with him (marked as crew), and her
// visit is on Ben's next card. Folded, the first Next step is a button. Two browsers.
//   node crew-news.mjs shots/crew-news
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/crew-news";
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

const ana = await start(`${out}/ana`);
await ana.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.9.${id % 250}.1` });
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

const ben = await start(`${out}/ben`, { width: 1280, height: 860 });
await ben.page.setExtraHTTPHeaders({ "X-Playtest-Ip": `10.9.${id % 250}.2` });
await register(ben.page, `ben${id}`);
await ben.page.evaluate((code) => tp.gameClient.foundCompany(`Ben ${Date.now() % 100000} Co`, "#3f8ae0", null, code), crew.holds[0].code);
await ben.page.waitForSelector(".toolbar", { timeout: 60000 });
await ben.page.waitForTimeout(1500);
await closeDialogs(ben.page);

// The chat: the crew, then the station
await ben.page.evaluate(() => tp.gameClient.toggleChat());
await ben.page.waitForSelector(".chat-panel", { timeout: 15000 });
await ben.page.waitForTimeout(1000);
const channels = await ben.page.evaluate(() => tp.appStore.getState().chat.channels.map((t) => `${t.kind}:${t.name}`));
expect("the chat has the crew, then the station, first", channels[0]?.startsWith("crew:") && channels[1]?.startsWith("station:"), JSON.stringify(channels));
let week = "";
for (let i = 0; i < 10 && week === ""; i++) {
  // The station's project starts with the first Yard rounds
  await ben.page.evaluate(() => tp.gameClient.refreshCompany());
  await ben.page.waitForTimeout(1500);
  week = await ben.page.evaluate(() => tp.appStore.getState().company.week?.text ?? "");
}
const stationName = channels[1]?.slice("station:".length) ?? "?";
// A new crew's station may have no project yet: then there's nothing to name
expect("the project is named by its station", week === "" || week.startsWith(`🏗 ${stationName}`), week || "no project yet");
await ana.page.evaluate(() => tp.gameClient.toggleChat());
await ana.page.waitForTimeout(1500);
await ana.page.evaluate(async () => {
  const crewChannel = tp.appStore.getState().chat.channels.find((t) => t.kind === "crew");
  await tp.gameClient.openChannel(crewChannel.id);
  await tp.gameClient.sendChat("Bricks for the Market Hall tonight?");
});
await ben.page.waitForTimeout(2500);
const heard = await ben.page.evaluate(() => Object.values(tp.appStore.getState().chatMessages).flat().some((m) => m.text === "Bricks for the Market Hall tonight?"));
expect("Ben reads Ana's crew message", heard);
await ben.shot("1-chat");

// Ben moves to another station
const moved = await ben.page.evaluate(async () => {
  const c = tp.gameClient.connection;
  const co = tp.appStore.getState().company;
  const first = await c.getStations(co.districtX, co.districtY);
  for (const district of first.districts) {
    const stations = await c.getStations(district.districtX, district.districtY);
    for (const s of stations.stations.filter((t) => t.open && !t.seats.some((x) => x.state === "you" || x.ownerName === co.name))) {
      const free = s.seats.find((t) => t.state === "free" || t.state === "house");
      if (free) {
        const r = await c.relocateTo({ districtX: district.districtX, districtY: district.districtY, slot: free.slot });
        if (r.success) return s.name;
      }
    }
  }
  return "no free seat";
});
console.log("Ben moved to:", moved);
// A move is a minute in the air: wait until he lands
await ben.page.waitForTimeout(70000);

// Ana: her crew panel lists Ben elsewhere, with Visit and Link
await ana.page.evaluate(() => tp.gameClient.openCrew());
await ana.page.waitForSelector(".crew-panel", { timeout: 15000 });
await ana.page.waitForTimeout(1500);
const row = ana.page.locator(".crew-members li.elsewhere");
expect("Ana's crew panel lists Ben in another station", (await row.count()) === 1, (await row.first().innerText().catch(() => "")).replace(/\s+/g, " "));
await ana.shot("2-crew");
await row.first().locator("button", { hasText: "Visit" }).click();
await ana.page.waitForTimeout(2500);
await ana.shot("3-visit");
await ana.page.evaluate(() => tp.gameClient.openCrew());
await ana.page.waitForTimeout(1500);
await ana.page.locator(".crew-members li.elsewhere button", { hasText: "Link" }).click();
await ana.page.waitForTimeout(2500);
const partner = await ana.page.evaluate(() => {
  const select = document.querySelector('select[aria-label="Partner"]');
  return select ? select.options[select.selectedIndex]?.textContent ?? "" : "";
});
expect("Link ▸ starts a new link with Ben, marked as crew", /Ben .*\(your crew\)/.test(partner), partner);
await ana.shot("4-link");

// Ben's next card: Ana came to look
await cheat(ben.page, "gone");
await ben.page.evaluate(() => tp.gameClient.hello());
await ben.page.waitForSelector(".return-card", { timeout: 30000 });
const card = await ben.page.evaluate(() => tp.appStore.getState().returnCard);
const lines = [...card.lines, ...(card.folded ?? [])].map((t) => t.text);
expect("Ana's visit is on Ben's card", lines.some((t) => /visited your estate/.test(t)), JSON.stringify(lines));
await ben.shot("5-card");
await closeDialogs(ben.page);

// Folded, the first Next step is a button (Next steps show once Loady's tutorial is done)
await ben.page.evaluate(() => tp.gameClient.connection.skipTutorial());
await ben.page.evaluate(() => tp.gameClient.refreshCompany());
await ben.page.waitForSelector(".next-steps", { timeout: 60000 }).catch(() => null);
const folded = await ben.page.evaluate(async () => {
  const head = document.querySelector(".next-steps-head");
  if (head?.getAttribute("aria-expanded") === "true") head.click();
  await new Promise((r) => setTimeout(r, 300));
  return document.querySelector(".next-steps-peek")?.getAttribute("aria-label") ?? null;
});
expect("folded, the first Next step is a Show me button", folded !== null && folded.startsWith("Show me: "), folded ?? "none");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", [...ana.errors, ...ben.errors].join(" | ") || "none");
await ana.browser.close();
await ben.browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

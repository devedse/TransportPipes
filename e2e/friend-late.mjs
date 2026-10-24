// Find your friends (after run 3, row 4): Ben started before Ana's link came and was never found again. Three browsers:
// Ana starts a crew for her friends; Ben settles elsewhere first, then opens Ana's link, says "Not now", and the seat
// stays in front of him while Ana's Crew panel says where he lives; Cleo, new, pastes the other seat's link on the start
// screen and lands on the invite.
//   node friend-late.mjs shots/friend-late
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/friend-late";
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

const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};
const id = Date.now() % 100000;

// Ana starts a crew for two friends
const ana = await start(`${out}/ana`);
await register(ana.page, `ana${id}`);
const seat = await ana.page.evaluate(() => tp.gameClient.connection.getStations(null, null).then((t) => t.friends ?? t.neighbours));
const crew = await ana.page.evaluate(
  (s) => tp.gameClient.connection.startWithFriends(`Ana ${Date.now() % 100000} Pipes`, "#e07a3f", `Kettle ${Date.now() % 1000}`, "kettle", "#24575a", "#c9963a", s, 2),
  seat,
);
expect("Ana's crew is founded with two seats", crew.success && crew.holds.length === 2, crew.error ?? "");
const [benCode, cleoCode] = crew.holds.map((t) => t.code);

// Ben settles on his own first (33 seconds before the link in run 3)
const ben = await start(`${out}/ben`);
await register(ben.page, `ben${id}`);
await ben.page.click("text=Find me neighbours");
await ben.page.waitForSelector(".seat-row.chosen", { timeout: 30000 });
await ben.page.fill(".found-panel input[minlength='3']", `Ben ${id} Co`);
await ben.page.click(".found-panel button[type=submit]");
await ben.page.waitForSelector(".toolbar", { timeout: 60000 });
await ben.page.waitForTimeout(2000);
for (let i = 0; i < 3 && (await ben.page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await ben.page.keyboard.press("Escape");
  await ben.page.waitForTimeout(500);
}

// Then he opens Ana's link
await ben.page.goto(`${base}/?seat=${encodeURIComponent(benCode)}`);
await ben.page.waitForSelector("text=Join and move", { timeout: 60000 });
await ben.shot("1-invite");
expect("the in-game invite offers 'Not with them'", (await ben.page.locator("button", { hasText: "Not with them" }).count()) === 1);
await ben.page.click("button:has-text('Not now')");
await ben.page.waitForTimeout(2500);
const kept = await ben.page.evaluate(async () => {
  await tp.gameClient.refreshCompany();
  return tp.appStore.getState().company.keptSeat;
});
expect("the seat stays in front of Ben after 'Not now'", kept !== null && kept.code.replace(/-/g, "") === benCode.replace(/-/g, ""), JSON.stringify(kept));

// Ana's Crew panel says where he lives
await ana.page.goto(`${base}/`);
await ana.page.waitForSelector(".toolbar", { timeout: 60000 });
await ana.page.waitForTimeout(2000);
for (let i = 0; i < 3 && (await ana.page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await ana.page.keyboard.press("Escape");
  await ana.page.waitForTimeout(500);
}
await ana.page.evaluate(() => tp.gameClient.openCrew());
await ana.page.waitForTimeout(2000);
const crewText = await ana.page.locator(".crew-panel, [aria-label=Crew], .panel").filter({ hasText: "opened this link" }).first().innerText().catch(() => "");
await ana.shot("2-crew-panel");
expect("Ana's Crew panel: Ben opened the link but lives elsewhere", /opened this link but lives in/.test(crewText), crewText.split("\n").find((t) => t.includes("opened")) ?? "");

// Cleo, new: the friend's link pasted on the start screen
const cleo = await start(`${out}/cleo`);
await register(cleo.page, `cleo${id}`);
await cleo.shot("3-start");
await cleo.page.fill("input[aria-label=\"Your friend's link or code\"]", `${base}/?seat=${encodeURIComponent(cleoCode)}`);
await cleo.page.click("form.friend-code button[type=submit]");
const invited = await cleo.page.waitForSelector("text=invites you", { timeout: 20000 }).then(() => true).catch(() => false);
await cleo.shot("4-invite");
expect("a pasted link opens the friend's invite", invited);

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", [...ana.errors, ...ben.errors, ...cleo.errors].join(" | ") || "none");
await ana.browser.close();
await ben.browser.close();
await cleo.browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

// The seat picker tells the truth (F7), run 1's race: Mia's picker shows a seat ("Find me neighbours") while Ana starts
// a crew on that very station; Mia's Settle is refused, the picker loads again, says who settled there and where the
// next free seat is, and her next Settle works. Two browsers.
//   node seat-race.mjs shots/seat-race
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/seat-race";
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

const mia = await start(`${out}/mia`);
const ana = await start(`${out}/ana`);
const id = Date.now() % 100000;
await register(mia.page, `mia${id}`);
await mia.page.click("text=Find me neighbours");
await mia.page.waitForSelector(".seat-row.chosen", { timeout: 30000 });
await mia.page.fill(".found-panel input[minlength='3']", `Mia ${id} Studio`);
const station = await mia.page.textContent(".station-card .plaque");
const seat = await mia.page.evaluate(() => tp.gameClient.connection.getStations(null, null).then((t) => t.neighbours));
console.log("Mia's picker shows", station, JSON.stringify(seat));
await mia.shot("1-picker");

// Ana takes that station for her crew, 20 seconds later in run 1
await register(ana.page, `ana${id}`);
const crew = await ana.page.evaluate(
  (seat) => tp.gameClient.connection.startWithFriends(`Ana ${Date.now() % 100000} Pipes`, "#e07a3f", `Tea ${Date.now() % 1000}`, "kettle", "#24575a", "#c9963a", seat, 3),
  seat,
);
console.log("Ana's crew:", crew.success ? "founded" : crew.error);

await mia.page.click(".found-panel button[type=submit]");
await mia.page.waitForSelector(".picker-moved", { timeout: 15000 }).catch(() => console.log("no 'moved' note"));
const note = await mia.page.textContent(".picker-moved").catch(() => null);
console.log("Mia reads:", note);
await mia.page.waitForTimeout(800);
await mia.shot("2-refused");
await mia.page.click(".found-panel button[type=submit]");
const settled = await mia.page.waitForSelector(".top-bar", { timeout: 30000 }).then(() => true).catch(() => false);
await mia.page.waitForTimeout(2000);
await mia.shot("3-settled");

const pass = crew.success && note !== null && note.includes("just settled in") && settled;
console.log(pass ? "PASS" : "FAIL", "errors:", [...mia.errors, ...ana.errors].join(" | ") || "none");
await mia.browser.close();
await ana.browser.close();
process.exit(pass ? 0 : 1);

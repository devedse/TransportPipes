// The research queue in a way that makes sense (the owner, 2026-10-31: "stop existing research without removing spend
// research on it"): each line shows what it does now with Stop, its queue with up, down and out, and what waits on
// hold, paid, with Continue; on a busy line a card says Do it first (what the line was at waits, first in the queue),
// and can still be queued.   node research-queue.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/research-queue";
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => {
  console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`);
  if (!ok) failures++;
};
const lines = () => page.evaluate(() => tp.appStore.getState().company.research);
const gold = () => page.evaluate(() => tp.appStore.getState().company.gold);
const wait = (ms = 1200) => page.waitForTimeout(ms);

await newPlayer(page, "rq");
for (let i = 0; i < 2; i++) await cheat("level");
await cheat("gold");
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await wait(400);
}
const lab = await page.evaluate(async () => {
  const s = () => tp.appStore.getState();
  const w = tp.gameClient.world;
  const co = s().company;
  const definition = s().content.building("lab");
  for (let r = 1; r < 20; r++)
    for (let dy = -r; dy <= r; dy++)
      for (let dx = -r; dx <= r; dx++) {
        const x = co.estateX + 16 + dx;
        const y = co.estateY + 16 + dy;
        if (tp.rules.canPlace(s().content, w, co, s().era, definition, x, y).ok) {
          const result = await tp.gameClient.connection.placeBuilding("lab", x, y);
          return result.success ? "built" : result.error;
        }
      }
  return "no spot";
});
check(lab === "built", "a Lab", lab);
await cheat("finish");
await wait(2000);
await page.evaluate(() => tp.gameClient.openResearch());
await page.waitForSelector(".research-panel");
await wait(800);

// Power on the tracks line, then Iron and Copper queued from their cards while the line is busy
const track = (name) => page.locator(".tech.track", { hasText: name }).first();
await track("Power").locator("button", { hasText: "Research tier 1" }).click();
await wait();
check((await lines()).currentTrack === "power", "Power researches");
// The top bar shows it with its time left and a bar (the owner: "power should show a progress bar in the top")
await page.keyboard.press("Escape");
await wait(1500);
const top = (await page.locator(".top-research").innerText()).replace(/\s+/g, " ");
check(/Power 1 \d+ min/.test(top) && (await page.locator(".top-research .top-research-bar").count()) === 1, "the top bar: Power 1, its time left and a bar", top);
await page.locator(".top-research").screenshot({ path: `${out}/0-top-bar.png` });
await page.evaluate(() => tp.gameClient.openResearch());
await page.waitForSelector(".research-panel");
await wait(800);
check(await track("Iron").locator("button", { hasText: "Do tier 1 first" }).isVisible(), "a busy line: the card says Do it first");
await track("Iron").locator("button", { hasText: "Queue it" }).click();
await wait(600);
await track("Copper").locator("button", { hasText: "Queue it" }).click();
await wait(1000);
check(JSON.stringify((await lines()).trackQueue) === '["iron","copper"]', "a busy line's cards can still be queued", JSON.stringify((await lines()).trackQueue));
const box = page.locator(".research-line[aria-label='Tracks line']");
const text = (await box.innerText()).replace(/\s+/g, " ");
console.log("line:", text);
check(/Now Power 1 · .* left Stop/.test(text) && /1\. Iron 1/.test(text) && /2\. Copper 1/.test(text), "the line says now and next", text);
await shot("1-now-and-next");

// Copper first: down on Iron
await box.getByRole("button", { name: "Move Iron 1 down" }).click();
await wait();
check(JSON.stringify((await lines()).trackQueue) === '["copper","iron"]', "down moves it later", JSON.stringify((await lines()).trackQueue));

// Stop Power: on hold, paid, its work kept; Copper starts
await box.getByRole("button", { name: "Stop" }).click();
await wait(1500);
let now = await lines();
check(now.held?.power > 0, "Power is on hold with its work left", JSON.stringify(now.held));
check(now.currentTrack === "copper", "the next in the queue starts", now.currentTrack);
const onHold = (await box.innerText()).replace(/\s+/g, " ");
console.log("line:", onHold);
check(/On hold Power 1 · paid, .* of work left Continue first/.test(onHold), "the line lists Power on hold, paid", onHold);
check(/On hold: paid/.test(await track("Power").innerText()), "Power's card says on hold, paid");
await shot("2-on-hold");

// Continue Power first: for nothing; Copper waits, first in the queue
const before = await gold();
await box.getByRole("button", { name: "Continue first" }).click();
await wait(1500);
now = await lines();
check(now.currentTrack === "power", "Power goes on", now.currentTrack);
check((await gold()) >= before - 1, "for nothing", `${before} → ${await gold()}`);
check(now.trackQueue[0] === "copper" && now.held?.copper > 0, "Copper waits on hold, first in the queue", JSON.stringify(now));
const queued = (await box.innerText()).replace(/\s+/g, " ");
check(/1\. Copper 1 · on hold, paid/.test(queued), "the queue says Copper is on hold, paid", queued);

// Out of the queue: the place clicked on
await box.getByRole("button", { name: "Take Iron 1 out of the queue" }).click();
await wait();
check(JSON.stringify((await lines()).trackQueue) === '["copper"]', "× takes it out", JSON.stringify((await lines()).trackQueue));

// The Era tree: Valves, then Pump Stations first
const tree = (name) => page.locator(".tech:not(.track)", { hasText: name }).first();
await tree("Valves").locator("button", { hasText: "Research" }).click();
await wait();
await tree("Pump Stations").locator("button", { hasText: "Do it first" }).click();
await wait(1500);
now = await lines();
check(now.currentResearch === "pumpStations" && now.researchQueue[0] === "valves" && now.held?.valves > 0, "Do it first: Valves waits first in the queue", JSON.stringify(now));
await page.locator(".research-line[aria-label='Era tree line']").scrollIntoViewIfNeeded();
await shot("3-tree-first");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

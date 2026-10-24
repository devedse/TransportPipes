// Refinements (after run 3, the design team's step 6): after an Era's tree the Labs refine the station by themselves,
// walls first (pipes, trading posts, Hub Orders); the research panel shows every line with its share of +50% and lets
// the player pick the next; the top bar names what is refined now.
//   node refinements.mjs shots/refinements
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/refinements";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "refine");
await cheat("level");
await cheat("eratech");
await page.waitForTimeout(4000);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) {
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);
}
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(1500);
const refine = await page.evaluate(() => tp.appStore.getState().company.refine);
console.log("refine:", JSON.stringify({ ...refine, lines: refine.lines.map((t) => `${t.id} +${t.now}`) }));
expect("Refinements are open after the Era's tree", refine.open);
expect("the Labs refine the walls first, by themselves", refine.current === "pipes", String(refine.current));
const top = await page.locator(".top-research").innerText().catch(() => "");
expect("the top bar names it", /Pipes carry \+10%/.test(top), top.replace(/\s+/g, " "));
await shot("1-topbar");

await page.evaluate(() => tp.gameClient.openResearch());
await page.waitForSelector(".refinements", { timeout: 15000 });
await page.waitForTimeout(1000);
const lines = await page.locator(".refine-line").count();
expect("the panel lists the lines", lines >= 4, `${lines} lines`);
await shot("2-panel");

const good = refine.lines.find((t) => !t.wall);
await page.locator(".refine-line", { hasText: good.name }).locator("button").click();
await page.waitForTimeout(2000);
const after = await page.evaluate(() => tp.appStore.getState().company.refine);
expect("a picked line comes next", after.choice === good.id && after.next === good.id, JSON.stringify({ choice: after.choice, next: after.next }));
await shot("3-picked");

console.log(results.every(Boolean) ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(results.every(Boolean) ? 0 : 1);

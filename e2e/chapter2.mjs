// Loady's second chapter above the advisor, the level roadmap in the XP panel, and a level-up card with what the
// level brings and whether the estate can use it. node chapter2.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/chapter2"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "c");
await cheat("gold");
// Through chapter 1 (12 steps since v1.2's Yard and gift steps)
for (let i = 0; i < 12; i++) await cheat("tutorial");
await page.evaluate(async () => {
  const c = tp.gameClient.connection; const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const co = () => tp.appStore.getState().company;
  const post = [...w.buildings.values()].find((t) => t.owner === co().id && t.type === "tradingPost");
  for (let d = 3; d < 12; d++) if ((await c.placeBuilding("windMoily", post.x - d, post.y)).success) break;
  await c.cheat("finish");
  tp.gameClient.engine.lookAt(post.x, post.y, 0.8);
});
await page.waitForTimeout(2500);
const state = await page.evaluate(() => { const t = tp.appStore.getState().company.tutorial; return t && { id: t.id, chapter: t.chapter, step: `${t.chapterStep}/${t.chapterSteps}`, wait: t.waitLevel }; });
console.log("loady", JSON.stringify(state));
await shot("1-chapter2-and-advisor");
const results = [];
const expect = (label, ok, extra = "") => results.push(`${ok ? "ok  " : "FAIL"} ${label}${extra ? ` (${extra})` : ""}`);
// Nine steps since the first Copper Pipe joined it (after run 4)
expect("chapter 2, step 1 of 9", state?.chapter === 2 && state.step === "1/9");
expect("the Loady goal card shows", await page.locator(".loady-goal").isVisible());
expect("the advisor shows below it", await page.locator(".next-steps").isVisible());

// The XP panel: what the next levels bring
await page.locator(".level-pill, .top-level, button:has-text('Company Level')").first().click();
await page.waitForTimeout(500);
const roadmap = await page.locator(".roadmap li").allTextContents();
console.log("roadmap", JSON.stringify(roadmap));
expect("the roadmap lists the next levels", roadmap.length >= 3);
await shot("2-roadmap");
await page.keyboard.press("Escape");

// A level up: the card says what it brings
await cheat("level");
await page.waitForTimeout(1200);
const card = await page.locator(".level-up-card").textContent().catch(() => null);
console.log("level-up card", card);
expect("the level-up card shows", card !== null);
await shot("3-level-up");
console.log(results.join("\n"));
console.log(errors.length ? errors.join("\n") : "no errors");
await browser.close();

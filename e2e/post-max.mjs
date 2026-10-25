// A full Trading Post grows (after run 4's bots: Max bought every level a bank could on a post, its panel saw no gain,
// and Grow everything left it out, so Mia's 5-minute level waited to day 28): node post-max.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/post-max"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "q");
for (const c of ["gold", "gold", "gold", "gold", "level", "level"]) await cheat(c);
// 1.4: a plot starts empty: eight Wind Moilys on copper pipes and a Storage with the goods for their levels past 10
console.log("kit", (await kit(page, [...Array(8).fill("windMoily"), "storage"], 2)).length);
for (let i = 0; i < 12; i++) await cheat("stock");
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
let failures = 0;
const check = (ok, text) => { console.log(`${ok ? "ok  " : "FAIL"} ${text}`); if (!ok) failures++; };

// The producers far past what the post's Hub Order takes
const setup = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const co = tp.appStore.getState().company;
  await c.cheat("finish");
  const post = [...w.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  const makers = [...w.buildings.values()].filter((t) => t.owner === co.id && t.network !== null && t.network === post.network && t.id !== post.id && t.type !== "hq");
  const ups = [];
  for (const b of makers) ups.push(`${b.type} ${(await c.upgrade(b.id, 25)).success}`);
  tp.gameClient.engine.lookAt(post.x, post.y, 0.5);
  return { post: post.id, level: post.level, ups };
});
console.log("setup", JSON.stringify(setup));
// Their goods past level 10 come from the Storage at the next settle
await page.waitForTimeout(12000);

// Its panel
await page.evaluate((id) => tp.gameClient.select?.(id) ?? tp.appStore.setState({ selectedBuilding: id }), setup.post);
await page.waitForSelector(".building-panel", { timeout: 15000 });
await page.waitForTimeout(3000);
await shot("1-full-post");
const panel = await page.locator(".building-panel").innerText();
const preview = panel.split("\n").find((t) => t.startsWith("Level ") && t.includes(":")) ?? "";
check(/\+[\d.,k]+ g\/min/.test(preview), `the panel says what a level brings: ${preview.trim()}`);
const max = await page.locator(".building-panel button", { hasText: /^Max \+/ }).first();
const maxText = (await max.count()) > 0 ? (await max.innerText()).replace(/\s+/g, " ") : "";
const before = await page.evaluate(() => tp.appStore.getState().company.gold);
check(maxText !== "", `Max is offered: ${maxText}`);
const past = panel.split("\n").find((t) => t.startsWith("Past level")) ?? "";
check(/all it sells would be within your Hub Orders/.test(past), `and why it stops there: ${past.trim()}`);
const maxCost = Number((maxText.match(/· ([\d.,]+[kM]?) g/)?.[1] ?? "0").replace(/,/g, "").replace(/k$/, "e3").replace(/M$/, "e6"));
check(maxCost > 0 && maxCost < before * 0.5, `Max costs a part of the bank (${maxCost.toLocaleString("en")} of ${Math.round(before).toLocaleString("en")} g)`);

// Your gold can: Grow everything takes the post
const uses = await page.evaluate(() => tp.gameClient.connection.getGoldUses());
const part = uses?.grow?.parts.find((t) => t.building === setup.post);
check(part !== undefined, `Grow everything takes the post: ${part ? `${part.text} for ${part.cost.toLocaleString("en")} g, +${part.gain}/min` : JSON.stringify(uses?.grow?.parts.map((t) => t.text) ?? null)}`);

// Max, pressed: the levels it said, and the post busy after
if ((await max.count()) > 0) {
  await max.click();
  await page.waitForTimeout(4000);
  await shot("2-after-max");
  const after = await page.evaluate((id) => ({ level: tp.gameClient.world.buildings.get(id).level, gold: tp.appStore.getState().company.gold }), setup.post);
  const levels = Number(maxText.match(/Max \+(\d+)/)?.[1] ?? 0);
  check(after.level === setup.level + levels, `Max bought its ${levels} levels (level ${setup.level} → ${after.level})`);
  const again = await page.locator(".building-panel").innerText();
  check(!/Max \+/.test(again) || /sells all it can/.test(again), "after it, no more useful levels unless the post is full again");
}
console.log(errors.length ? errors.join("\n") : "no errors");
console.log(failures ? "done with failures" : "PASS");
await browser.close();

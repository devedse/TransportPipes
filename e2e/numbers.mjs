// Words and numbers that agree (after run 4: "+2,547/min · hub +2,598" against a top bar of +2,574, a Yard "spare 13"
// against a panel "18 spare", and a Next step that stayed after it was done): node numbers.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/numbers"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "n");
for (const c of ["gold", "gold", "level", "level", "finish"]) await cheat(c);
// 1.4: a plot starts empty: a small estate first
await kit(page);
await page.evaluate(() => tp.appStore.setState({ levelUp: null }));
await page.waitForTimeout(3000);
await cheat("yard");
await page.waitForTimeout(3000);
let failed = 0;
const check = (ok, what, detail = "") => {
  if (!ok) failed++;
  console.log(`${ok ? "ok  " : "FAIL"} ${what}${detail ? `  (${detail})` : ""}`);
};

// 1. A building's network: its money lines add up to its total
const moily = await page.evaluate(() => {
  const co = tp.appStore.getState().company;
  const b = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "windMoily" && t.network !== null);
  tp.gameClient.showBuilding(b.id);
  return b.id;
});
await page.waitForSelector(".net-owner.mine", { timeout: 15000 });
await page.waitForTimeout(1500);
const money = await page.evaluate(() => {
  const mine = document.querySelector(".net-owner.mine");
  const num = (t) => Number(t.replace(/[^\d.\-−]/g, "").replace("−", "-"));
  const total = num(mine.querySelector(".net-gold").textContent);
  const parts = [...mine.querySelectorAll(".net-breakdown span")].map((t) => (t.classList.contains("minus") ? -1 : 1) * Math.abs(num(t.textContent.split(/[+−]/).pop())));
  return { total, parts, hub: /hub \+/.test(mine.querySelector(".net-breakdown")?.textContent ?? ""), text: mine.textContent };
});
const sum = money.parts.reduce((a, b) => a + b, 0);
check(money.parts.length > 0 && Math.abs(sum - money.total) <= Math.max(2, Math.abs(money.total) * 0.02), "the network's money lines add up to its total", `${money.total} vs ${sum.toFixed(1)}: ${money.text}`);
await shot("1-network-money");

// 2. The Yard: what "Spare" means, and what goes to neighbours beside it
await page.evaluate(() => tp.gameClient.openYard());
await page.waitForSelector(".yard-residents", { timeout: 15000 });
await page.waitForTimeout(1500);
const yard = await page.locator("aside").filter({ has: page.locator(".yard-residents") }).innerText();
check(/Spare: what each still sells to the hub/.test(yard), "the Yard says what Spare means");
check(/Spare \(to the hub\)/.test(yard), "your own row says your spare goes to the hub");
const income = (yard.match(/Here [^\n]*/) ?? [""])[0];
check(/of your [+−][\d,.]+\/min/.test(income), "the Yard says what it pays of your income, and whom for", income);
// What the Yard pays here is what the network's "the Yard and neighbours" said, give or take the project's round
const paid = [...income.matchAll(/([\d,.]+) g\/min/g)].reduce((a, m) => a + Number(m[1].replace(/,/g, "")), 0);
// The network's parts past the hub's: neighbours, the project (and what is bought, below zero)
const yardPart = money.parts.slice(money.hub ? 1 : 0).reduce((a, b) => a + b, 0);
check(yardPart === 0 || Math.abs(paid - yardPart) <= Math.max(3, yardPart * 0.15), "the Yard's line and the network's Yard part agree", `${paid} vs ${yardPart}`);
await shot("2-yard");
await page.keyboard.press("Escape");

// 3. Next steps: a step its own button did says so at once
await page.evaluate(() => tp.gameClient.refreshNextSteps());
await page.waitForTimeout(1500);
const button = page.locator(".next-step .next-step-actions button.primary").filter({ hasText: /^Upgrade/ }).first();
if ((await button.count()) === 0) {
  console.log("note no Upgrade step to press");
} else {
  const item = button.locator("xpath=ancestor::li[1]");
  const before = await item.innerText();
  await button.click();
  await page.waitForTimeout(400);
  const done = await page.locator(".next-step.done .next-step-done").count();
  const same = await page.locator(".next-step:not(.done)").filter({ hasText: before.split("\n")[0] }).filter({ hasText: before.split("\n")[1] ?? "" }).count();
  check(done === 1 || same === 0, "the step says ✓ Done the moment it is done, or is gone", done === 1 ? "done" : "replaced");
  await shot("3-step-done");
  await page.waitForTimeout(2500);
  const still = await page.locator(".next-step.done").count();
  check(still === 0, "and the advisor's next list replaces it");
}

console.log(errors.length ? `FAIL errors: ${errors.join("\n")}` : failed > 0 ? `FAIL ${failed} checks` : "PASS errors: none");
await browser.close();

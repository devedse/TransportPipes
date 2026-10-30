// The middle game's chores explain themselves (run 2's recommendation 5): the Build menu's Processing tab lists a
// factory with what it uses ("Uses 12 iron ore a minute: you make none yet, an Iron Mine does"; 1.4's Lab uses
// nothing), and the Hub's daily orders say how fast the trading posts fill them (since run 3 they need no Storage).
//   node chores.mjs shots/chores
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/chores";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "chores");
await cheat("level");
await cheat("gold");
await page.waitForTimeout(1500);

await page.evaluate(() => tp.appStore.setState({ buildMenuOpen: true, buildCategory: "processing" }));
await page.waitForTimeout(800);
const lab = await page.evaluate(() => {
  const item = [...document.querySelectorAll(".build-item")].find((t) => t.querySelector(".build-name")?.textContent?.startsWith("Small Factory"));
  return item ? (item.querySelector(".supply-line")?.textContent ?? "(no supply line)") : null;
});
console.log("Small Factory under Processing:", lab);
await shot("1-processing");
await page.evaluate(() => tp.appStore.setState({ buildMenuOpen: false }));

await page.evaluate(() => tp.gameClient.openHub("orders"));
await page.waitForTimeout(1500);
const orders = await page.evaluate(() => [...document.querySelectorAll(".order")].map((t) => ({
  stock: t.querySelector(".order-stock")?.textContent ?? null,
  button: t.querySelector("button")?.textContent ?? null,
})));
console.log("orders", JSON.stringify(orders));
await shot("2-orders");

// Since run 3 the orders fill from what the trading posts sell: each says how fast, or what to pipe
const pass = lab !== null && lab.startsWith("Uses 12 iron ore a minute: you make none yet, an Iron Mine does") && orders.length > 0
  && orders.every((t) => t.stock?.startsWith("Filled") || t.stock?.startsWith("Your trading posts sell"));
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

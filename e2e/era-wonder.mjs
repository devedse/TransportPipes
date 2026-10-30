// The town's Era Wonder (1.4 §13): the goal tracker's Era row names the Wonder and its stage and opens Hub › Town &
// Wonder, where each good the stage wants has its give switch, "Buy and give the rest" and, when nobody near makes it,
// the chain that would; buying one good fills it, and the giver is listed among the stage's builders.
//   node era-wonder.mjs shots/era-wonder
import { start, newPlayer } from "./lib.mjs";

const out = process.argv[2] ?? "shots/era-wonder";
const { browser, page, errors, shot, cheat } = await start(out);
await newPlayer(page, "era");
for (let i = 0; i < 20; i++) await cheat("earn");
await page.evaluate(() => tp.gameClient.refreshCompany());
await page.waitForTimeout(1500);
const results = [];
const expect = (label, ok, detail = "") => {
  results.push(ok);
  console.log(`${ok ? "ok  " : "FAIL"} ${label}${detail ? `  (${detail})` : ""}`);
};

const row = page.locator(".goal-tracker li").filter({ has: page.locator(".goal-label", { hasText: /^Era$/ }) });
const text = (await row.innerText().catch(() => "")).replace(/\s+/g, " ");
expect("the tracker has an Era row with the town's Wonder and its stage", /🏛 The .+ stage \d of 3/.test(text), text);
await shot("1-tracker");

await row.locator("button.goal-text").click();
await page.waitForSelector(".wonder-card", { timeout: 15000 });
await page.waitForTimeout(1500);
const card = page.locator(".wonder-card").first();
expect("it opens Town & Wonder", /your town builds it together/i.test(await card.innerText()));
const needs = card.locator(".wonder-need");
const count = await needs.count();
expect("the stage lists its goods", count >= 2, `${count}`);
// The first good it still lacks (a good already in has no buy button)
const first = needs.filter({ has: page.locator("button", { hasText: /^Buy and give the rest/ }) }).first();
expect("each good has its give switch", (await first.locator(".segmented button").count()) === 3);
const buy = first.locator("button", { hasText: /^Buy and give the rest · [\d.,kMB]+ g$/ });
expect("and Buy and give the rest", (await buy.count()) === 1, await buy.innerText().catch(() => "none"));
const chain = await card.locator(".wonder-chain").first().innerText().catch(() => "");
expect("a good nobody near makes says what would make it", chain === "" || /makes|make/.test(chain), chain);
await shot("2-town-wonder");

// Buy one good and give it (as much as the gold buys: a stage's good is worth hundreds of millions): more of it is in,
// and the giver shows among the builders
const name = await first.locator(".need-name").innerText();
const before = await page.evaluate(() =>
  Object.fromEntries((tp.appStore.getState().town?.wonder?.needs ?? []).map((t) => [t.resource, t.delivered])),
);
await buy.click();
await page.waitForTimeout(3000);
const after = await page.evaluate(() => {
  const s = tp.appStore.getState();
  return {
    needs: s.town?.wonder?.needs?.map((t) => [t.resource, t.delivered, t.required]) ?? [],
    builders: s.town?.wonder?.topBuilders?.map((t) => t.name) ?? [],
    me: s.company.name,
  };
});
const grown = after.needs.filter(([r, delivered]) => delivered > (before[r] ?? 0) + 0.5);
expect(`${name} bought and given`, grown.length === 1, JSON.stringify(after.needs));
expect("the giver is among the builders", after.builders.includes(after.me), after.builders.join(", "));
await shot("3-given");

const pass = results.every(Boolean) && errors.length === 0;
console.log(pass ? "PASS" : "FAIL", "errors:", errors.length ? errors.join(" | ") : "none");
await browser.close();
process.exit(pass ? 0 : 1);

// Your prices in the trade settings (playtest: a crew price cut to 1.0× for energy couldn't be put back): a cut counts at
// once, a rise waits an hour, and the stepper steps from where the price is going, says so and can go back.
// Needs the game at BASE_URL: node prices.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/prices"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "pr");
await cheat("gold");
await kit(page, ["windMoily"]);
await page.evaluate(() => tp.gameClient.openTradeSettings());
await page.waitForSelector(".trade-table");
const stepper = (label) => page.locator(`.price-stepper[aria-label="${label}"]`);
const energy = stepper("Crew price for Energy");
const shown = async (s) => (await s.locator("strong").innerText()).trim();
const note = async (s) => (await s.locator("xpath=..").innerText()).replace(/\s+/g, " ");
const setting = (key) => page.evaluate((key) => tp.appStore.getState().company.prices?.find((t) => t.key === key) ?? null, key);
const click = async (s, which) => { await s.locator(`button[aria-label$="${which}"]`).click(); await page.waitForTimeout(700); };

check((await shown(energy)) === "1.15×", "energy's crew price starts at the crew price", await shown(energy));
for (let i = 0; i < 3; i++) await click(energy, "lower");
check((await shown(energy)) === "1.0×", "a cut to 1× counts at once", await shown(energy));
await shot("cut");

await click(energy, "higher");
const first = await setting("crew:energy");
check(first?.pending === 1.05, "a rise waits", JSON.stringify(first));
check((await shown(energy)) === "1.05×", "the stepper shows where the price is going", await shown(energy));
check(/1\.0× until \d\d:\d\d/.test(await note(energy)), "and what it is until then", await note(energy));
await click(energy, "higher");
await click(energy, "higher");
const back = await setting("crew:energy");
check(back === null || back.back === true, "stepping back to the crew price goes back to it (no setting of its own)", JSON.stringify(back));
check((await shown(energy)) === "1.15×", "the stepper reaches 1.15× again", await shown(energy));
await shot("rising");

await click(energy, "lower");
await click(energy, "lower");
await click(energy, "lower");
const cut = await setting("crew:energy");
check(cut?.value === 1 && cut.pending == null, "a cut while a rise waits counts at once and the rise is off", JSON.stringify(cut));
check(!/until/.test(await note(energy)), "nothing waits any more", await note(energy));

// The general crew price the same way
const crew = stepper("Your crew price");
await click(crew, "higher");
check((await shown(crew)) === "1.2×", "the crew price steps up to where it is going", await shown(crew));
await click(crew, "higher");
check((await shown(crew)) === "1.25×", "and on from there", await shown(crew));
const general = await setting("crew");
check(general?.value === 1.15 && general.pending === 1.25, "one rise, to 1.25×, waits", JSON.stringify(general));
await shot("general");

check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

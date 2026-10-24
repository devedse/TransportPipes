// The station's project, the Plan board and Asks (v1.2 §12): A pipes a Wind Moily to its Yard gate; the project
// takes its spare; the "project" cheat finishes the stage and the next asks for a keystone; A pins what it makes
// and asks the station for coal; the project stands in the Yard's corner on the map: node project.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/project"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out);
await newPlayer(a.page, "pa");
await a.cheat("gold");
console.log("A", await settle(a, "windMoily"));
await a.cheat("yard");
await a.cheat("yard");
await a.page.waitForTimeout(1500);

await a.page.evaluate(() => tp.gameClient.openProject());
await a.page.waitForSelector(".project-basket", { timeout: 15000 });
await a.page.waitForTimeout(800);
await a.shot("1-foundation");
const first = await a.page.evaluate(() => tp.appStore.getState().project);
console.log("stage", first.stage, first.basket.map((t) => `${t.resource} ${t.delivered.toFixed(0)}/${t.target}`));
if (first.basket.length === 0 && first.stageIndex < first.stages.length) fail("a basket to fill");

await a.cheat("project");
await a.page.evaluate(() => tp.gameClient.refreshProject());
await a.page.waitForTimeout(800);
await a.shot("2-water-tower");
const tower = await a.page.evaluate(() => tp.appStore.getState().project);
console.log("stage", tower.stage, tower.basket.map((t) => `${t.resource}${t.keystone ? " (keystone)" : ""} ${t.target}`), tower.line, tower.bonus);
if (tower.stageIndex !== first.stageIndex + 1) fail("the cheat finishes the stage: the next one starts");
if (tower.stageIndex < 4 && !tower.basket.some((t) => t.keystone)) fail("with a keystone");
if (first.stageIndex < 3 && !(tower.bonus > first.bonus)) fail("every finished stage lifts every seat");

// The Plan board and an Ask
await a.page.evaluate(() => tp.gameClient.openYard());
// The Plan board is folded (after run 4)
await a.page.waitForSelector(".yard-fold > summary", { timeout: 15000 });
await a.page.click(".yard-fold > summary");
await a.page.waitForSelector(".plan-board", { timeout: 15000 });
await a.page.selectOption("select[aria-label='Good to pin']", "energy");
await a.page.click(".plan-board button:has-text('I make it')");
await a.page.waitForTimeout(800);
await a.page.selectOption("select[aria-label='Good to ask for']", "coal");
await a.page.click(".ask-form button.primary");
await a.page.waitForTimeout(1200);
const me = await a.page.evaluate(() => { const s = tp.appStore.getState(); return s.yard.residents.find((t) => t.id === s.company.id); });
console.log("pins", me.makes, me.needs, "asks", me.asks.map((t) => `${t.resource} ${t.amount}`));
if (!me.makes.includes("energy")) fail("A's pin");
await a.page.evaluate(() => document.querySelector(".plan-board")?.scrollIntoView());
await a.shot("3-plan-board");
if (me.asks.length === 0) console.log("(no Ask: A has no storage yet, as the rules say)");

// The project on the map
const yard = await a.page.evaluate(() => { const y = tp.appStore.getState().yard; return { x: y.x0, y: y.y0 }; });
await a.page.evaluate(() => tp.appStore.setState({ yardOpen: false }));
await a.page.evaluate(({ x, y }) => tp.gameClient.engine.lookAt(x + 3, y + 3, 0.5), yard);
await a.page.waitForTimeout(2500);
await a.shot("4-map");

if (a.errors.length > 0) fail(a.errors.join(" | "));
await a.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

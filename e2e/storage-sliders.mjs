// A Storage's sliders (the owner, 2026-10-31: "drag sliders per resource. At the bottom a total slider should show the
// total. If you specify more than the total that would fit, it goes red and you can't save it"): a Storage on the
// Trading Post's network, a good added, two sliders set to more than its room together: the total goes red and Save
// waits; Fit to the room brings them in; Save keeps them, and the panel shows them saved.   node storage-sliders.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/storage-sliders"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "ss");
for (const c of ["gold", "level", "level"]) await cheat(c);
await page.evaluate(() => tp.gameClient.connection.skipTutorial());
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
console.log(await kit(page, ["windMoily", "windMoily", "storage"]));
const storage = await page.evaluate(() => [...tp.gameClient.world.buildings.values()].find((t) => t.owner === tp.appStore.getState().company.id && t.type === "storage")?.id);
check(storage !== undefined, "a Storage");
await page.evaluate((id) => tp.gameClient.showBuilding(id), storage);
await page.waitForSelector(".storage-sliders", { timeout: 15000 });
await page.waitForTimeout(1500);
const room = await page.evaluate(() => tp.appStore.getState().buildingDetails?.capacity ?? 0);
console.log("room", room);

// A good added, and two sliders set to more than the room together
await page.selectOption(".storage-add", "ironOre");
await page.waitForTimeout(300);
const rows = page.locator(".storage-rows li");
console.log("rows", await rows.count(), await rows.locator("strong").allInnerTexts());
check((await rows.count()) >= 2, "a row a good, with the one added");
const number = (good) => page.locator(`.storage-rows li:has(strong:text-is("${good}")) input.storage-number`);
await number("Iron ore").fill(String(Math.round(room * 0.7)));
await number("Energy").fill(String(Math.round(room * 0.6)));
await page.waitForTimeout(300);
const total = page.locator(".storage-total");
const totalText = (await total.innerText()).replace(/\s+/g, " ");
console.log("over:", totalText);
check(await total.evaluate((t) => t.classList.contains("over")), "more than fits: the total goes red", totalText);
const save = page.locator(".storage-sliders button.primary", { hasText: "Save" });
check(await save.isDisabled(), "and Save waits");
await shot("1-over");

// Fit to the room, then Save
await page.locator(".storage-total button", { hasText: "Fit to the room" }).click();
await page.waitForTimeout(300);
check(!(await total.evaluate((t) => t.classList.contains("over"))) && !(await save.isDisabled()), "Fit to the room: it fits, Save can");
const wanted = { energy: Number(await number("Energy").inputValue()), ironOre: Number(await number("Iron ore").inputValue()) };
await save.click();
await page.waitForTimeout(2000);
const saved = await page.evaluate(() => tp.appStore.getState().buildingDetails?.keep);
console.log("wanted", JSON.stringify(wanted), "saved", JSON.stringify(saved?.set), "manual", saved?.manual);
check(saved?.manual === true && Math.abs((saved?.set?.energy ?? -1) - wanted.energy) <= 1 && Math.abs((saved?.set?.ironOre ?? -1) - wanted.ironOre) <= 1, "Save keeps what the sliders say", JSON.stringify(saved));
check((await page.locator(".storage-sliders button.primary", { hasText: "Save" }).count()) === 0, "saved: nothing left to save");
check(wanted.energy + wanted.ironOre <= room + 1, "together no more than its room", `${wanted.energy + wanted.ironOre} of ${room}`);
await shot("2-saved");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

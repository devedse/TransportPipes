// One Lab (the owner, 2026-10-31: "Can we just disable people building more than 1 lab please?"): with a Lab, the
// Build menu's Lab says "You have one" and can't be picked, and placing a second is refused with why.
//   node one-lab.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/one-lab"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "ol");
for (const c of ["level", "gold"]) await cheat(c);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const labItem = () => page.locator(".build-item", { hasText: "Lab" }).first();
const openMenu = async () => {
  await page.evaluate(() => tp.appStore.setState({ buildMenuOpen: true }));
  await page.waitForSelector(".build-list", { timeout: 10000 });
  const tab = page.locator("button", { hasText: /^Company$/ });
  if ((await tab.count()) > 0) await tab.first().click();
  await page.waitForTimeout(500);
};
await openMenu();
check(!(await labItem().isDisabled()), "without a Lab it can be picked");
await page.keyboard.press("Escape");
console.log(await kit(page, ["lab"]));
await page.waitForTimeout(1500);
await openMenu();
check(await labItem().isDisabled(), "with one, the Lab can't be picked");
check(/You have one/.test(await labItem().innerText()), "and says so", await labItem().innerText());
await shot("1-menu");
const second = await page.evaluate(async () => {
  const s = tp.appStore.getState(); const w = tp.gameClient.world;
  const post = [...w.buildings.values()].find((t) => t.owner === s.company.id && t.type === "tradingPost");
  for (let r = 2; r < 12; r++) for (let dx = -r; dx <= r; dx++) for (let dy = -r; dy <= r; dy++) {
    const x = post.x + dx, y = post.y + dy;
    if (w.buildingAt(x, y) === undefined && w.conductorAt(x, y) === undefined) {
      const local = tp.rules.canPlace(s.content, w, s.company, s.era, s.content.building("lab"), x, y);
      const server = await tp.gameClient.connection.placeBuilding("lab", x, y);
      return { local: local.reason, server: server.error };
    }
  }
  return null;
});
console.log(JSON.stringify(second));
check(/You have a Lab: one is enough/.test(second?.local ?? ""), "the ghost says why", second?.local);
check(/You have a Lab: one is enough/.test(second?.server ?? ""), "and the server refuses it", second?.server);
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

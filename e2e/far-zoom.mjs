// Zoomed out (playtest: "when I'm zoomed out like this the game is really slow"): the far view draws each chunk as one
// picture, not its ground and objects (hidden, they were drawn in full and again on every change); zoomed in again
// the map is drawn in full.   node far-zoom.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/far-zoom"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out, { width: 1600, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "fz");
await cheat("gold");
console.log(await kit(page, ["windMoily", "windMoily"]));
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const look = (scale) => page.evaluate((s) => {
  const co = tp.appStore.getState().company;
  const post = [...tp.gameClient.world.buildings.values()].find((t) => t.owner === co.id && t.type === "tradingPost");
  tp.gameClient.engine.lookAt(post.x, post.y, s);
}, scale);
const count = () => page.evaluate(() => {
  const chunks = [...tp.gameClient.engine.chunks.values()].filter((t) => t.loaded);
  return { chunks: chunks.length, farOnly: chunks.filter((t) => t.farOnly).length, objects: chunks.reduce((s, t) => s + t.objects.length, 0), ground: chunks.filter((t) => t.ground !== null).length, minis: chunks.filter((t) => t.mini !== null).length };
});
await look(0.08);
await page.waitForTimeout(8000);
const far = await count();
console.log("far:", JSON.stringify(far));
check(far.chunks > 10 && far.farOnly === far.chunks, "zoomed out every chunk is drawn as its far picture only", JSON.stringify(far));
check(far.objects === 0 && far.ground === 0 && far.minis === far.chunks, "no ground or objects drawn", JSON.stringify(far));
await shot("1-far");
await look(0.6);
await page.waitForTimeout(5000);
const near = await count();
console.log("near:", JSON.stringify(near));
check(near.farOnly === 0 && near.objects > 0 && near.ground === near.chunks, "zoomed in again it is drawn in full", JSON.stringify(near));
await shot("2-near");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

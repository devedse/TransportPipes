// Lumber Camps on a plot's forest (playtest: "why can't we place lumber camps anywhere": the client still asked for the
// old forest's marked trees, which the 1.4 map doesn't make): the client allows them next to the plot's forest, and the
// server builds one there. node lumber.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/lumber"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "lu");
for (let i = 0; i < 5; i++) await cheat("level");
await cheat("gold");
const result = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const s = tp.appStore.getState(); const co = s.company;
  const def = s.content.building("lumberCamp");
  let forest = 0, marked = 0, client = 0, reason = null, spot = null;
  for (let y = co.estateY; y < co.estateY + 32; y++) for (let x = co.estateX; x < co.estateX + 32; x++) {
    const tile = w.tile(x, y);
    if (tile?.terrain === "forest") { forest++; if (tile.deposit === "wood") marked++; }
    const p = tp.rules.canPlace(s.content, w, co, s.era, def, x, y);
    if (p.ok) { client++; spot ??= { x, y }; }
    else if (/forest/.test(p.reason ?? "")) reason ??= p.reason;
  }
  // The server's own check on a spot the client allows
  const placed = spot ? await c.placeBuilding("lumberCamp", spot.x, spot.y) : null;
  return { dealtLand: w.dealtLand, forest, marked, client, reason, spot, server: placed && (placed.success || placed.error) };
});
console.log(JSON.stringify(result));
check(result.forest > 0, "the plot has forest", JSON.stringify(result));
check(result.client > 0, "the client lets a Lumber Camp stand next to it", `${result.reason} (${result.marked} marked trees of ${result.forest} forest tiles)`);
check(result.server === true, "and the server builds it there", String(result.server));
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

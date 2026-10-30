// A better pipe asked for twice (playtest: every click on the network card's Upgrade made another Copper Pipe want on the
// same 13 tiles, each paid; the Yard listed five): the second ask is refused and says why, one want waits, paid once,
// and the network card counts its tiles as waiting. node pipe-wants.mjs <outdir>
import { start, newPlayer, kit } from "./lib.mjs";
const [out = "shots/pipe-wants"] = process.argv.slice(2);
const { browser, page, errors, shot, cheat } = await start(out);
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "pw");
await cheat("gold");
await cheat("eratech");
await kit(page, ["windMoily"]);
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
const result = await page.evaluate(async () => {
  const c = tp.gameClient.connection; const w = tp.gameClient.world; const co = () => tp.appStore.getState().company;
  const tiles = [...w.conductors.values()].filter((t) => t.owner === co().id && t.kind === "pipe" && t.tier === 1);
  const flat = tiles.flatMap((t) => [t.x, t.y]);
  const gold0 = co().gold;
  const first = await c.upgradePipes(flat, 2);
  await new Promise((r) => setTimeout(r, 1500));
  const gold1 = co().gold;
  const second = await c.upgradePipes(flat, 2);
  await new Promise((r) => setTimeout(r, 1500));
  const gold2 = co().gold;
  const network = w.networkAt?.(tiles[0].x, tiles[0].y) ?? null;
  return { tiles: tiles.length, first: first.success || first.error, second: second.success || second.error, paidFirst: gold0 - gold1, paidSecond: gold1 - gold2,
    wants: co().wants.filter((t) => t.tiles !== null).map((t) => `${t.amount} ${t.resource} on ${t.tiles}`), network };
});
console.log(JSON.stringify(result));
check(result.tiles > 0, "a grey pipe to upgrade");
check(result.first === true, "the first ask is taken", String(result.first));
check(/wait for their Copper Pipe already/.test(String(result.second)), "the second is refused and says why", String(result.second));
check(result.wants.length === 1, "one want waits", JSON.stringify(result.wants));
check(result.paidSecond < result.paidFirst / 2, "paid once", `${result.paidFirst} then ${result.paidSecond}`);
await shot("1-asked-twice");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

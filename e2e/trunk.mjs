// A crew trunk (v1.2 §13): A founds a crew, the world moves to Era 2 (dev cheat), A starts a steel trunk to the next
// hub from the Links tab and pays its segments (the stock cheat brings the steel and concrete); the trunk shows in
// the tab and on the map, its segments in A's colour: node trunk.mjs <outdir>
import { start, newPlayer, settle } from "./lib.mjs";
const [out = "shots/trunk"] = process.argv.slice(2);
const fail = (why) => { console.log("FAIL", why); process.exitCode = 1; };

const a = await start(out);
await newPlayer(a.page, "ta");
await a.cheat("gold");
await a.cheat("level");
console.log("storage", await settle(a, "storage"));
const era = await a.page.evaluate(() => tp.appStore.getState().era);
if (era < 2) await a.cheat("era");
await a.page.evaluate(() => tp.gameClient.foundCrew(`Trunkers ${Date.now() % 10000}`, "train", "#2d6fb4", "#c9963a", 0));
await a.page.waitForTimeout(1000);
// Another district must be open for a hub to go to
const targets = await a.page.evaluate(async () => (await tp.gameClient.connection.getLinks()).trunks.targets.map((t) => t.name));
console.log("targets", targets);

await a.page.evaluate(() => tp.gameClient.openLinks());
await a.page.waitForSelector(".trunks", { timeout: 15000 });
await a.cheat("stock");
// A segment costs two hours of A's income: the gold cheat a few times
for (let i = 0; i < 3; i++) await a.cheat("gold");
const startError = await a.page.evaluate(async () => {
  const t = (await tp.gameClient.connection.getLinks()).trunks;
  if (!t.canBuild) return t.blocker;
  const target = t.targets[0];
  const r = await tp.gameClient.connection.startTrunk(target.districtX, target.districtY, target.port);
  return r.success ? null : r.error;
});
console.log("start", startError ?? "ok", "era", await a.page.evaluate(() => tp.appStore.getState().era));
await a.page.evaluate(() => tp.gameClient.refreshLinks());
await a.page.waitForTimeout(1500);
let trunk = await a.page.evaluate(async () => (await tp.gameClient.connection.getLinks()).trunks.crew[0] ?? null);
console.log("trunk", trunk && `${trunk.target} ${trunk.segments}/${trunk.needed} · ${trunk.length} tiles`);
if (trunk === null) fail("the trunk is started");
for (let i = 0; i < 60 && trunk !== null && !trunk.built; i++) {
  await a.cheat("stock");
  await a.cheat("gold");
  await a.cheat("gold");
  const paid = await a.page.evaluate((id) => tp.gameClient.connection.buildTrunkSegment(id), trunk.id);
  if (!paid.success) { console.log("segment", paid.error); break; }
  if (i === 2) {
    await a.page.evaluate(() => tp.gameClient.refreshLinks());
    await a.page.waitForTimeout(500);
    await a.shot("1-building");
  }
  trunk = await a.page.evaluate(async () => (await tp.gameClient.connection.getLinks()).trunks.crew[0]);
}
console.log("trunk", trunk && `${trunk.segments}/${trunk.needed} built ${trunk.built} · ${Math.round(trunk.efficiency * 100)}% arrives`);
if (!trunk?.built) fail("the trunk is built");
await a.page.evaluate(() => tp.gameClient.refreshLinks());
await a.page.waitForTimeout(800);
await a.shot("2-built");

// The trunk on the map
const route = await a.page.evaluate(() => tp.appStore.getState().mapLinks.find((t) => t.kind === "trunk" && t.colors?.length)?.route ?? null);
if (route === null) fail("the map knows the trunk");
else {
  await a.page.evaluate(() => tp.appStore.setState({ yardOpen: false }));
  await a.page.evaluate((r) => tp.gameClient.engine.lookAt(r[0], r[1], 0.3), route);
  await a.page.waitForTimeout(2500);
  await a.shot("3-map");
}
if (a.errors.length > 0) fail(a.errors.join(" | "));
await a.browser.close();
console.log(process.exitCode ? "done with failures" : "ok");

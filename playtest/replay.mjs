// Scripted replays (docs/playtest-fixes.html §6): a moment of run 1 restored on today's build, the clock moved on, and
// the numbers checked against the fix plan's marks. Every replay must first fail on the build run 1 played, then pass
// once its fix ships. They run on the playtest server (playtest/bin, :5090) in a run folder of their own.
//   node playtest/replay.mjs [--publish] <scenario> [--from <run>]
//     --publish   builds the server into playtest/bin first (do it after every change to the game)
//     --from      the run whose snapshots to restore (default story1)
// Scenarios:
//   row10-yard    day 14 19:00 + 6 h, nobody acting: incomes, what the Yard sends that nobody takes, Cleo's Lab
import { execSync } from "node:child_process";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const SERVER = process.env.PLAYTEST_SERVER ?? "http://localhost:5090";
const TOKEN = process.env.PLAYTEST_TOKEN ?? "playtest-local";

const args = process.argv.slice(2);
const publish = args.includes("--publish");
const fromIndex = args.indexOf("--from");
const from = fromIndex >= 0 ? args[fromIndex + 1] : "story1";
const scenario = args.find((t, i) => !t.startsWith("--") && args[i - 1] !== "--from");

// Run 1's companies
const ANA = 25, CLEO = 26, BEN = 27, MIA = 28, DEV = 29;
const NAMES = { [ANA]: "Ana", [CLEO]: "Cleo", [BEN]: "Ben", [MIA]: "Mia", [DEV]: "Dev" };

const env = { ...process.env, PLAYTEST_RUN: `replay-${scenario}`, PLAYTEST_SNAPSHOTS: path.join(ROOT, "runs", from, "snapshots") };
const director = (...cmd) => execSync(`node ${path.join(ROOT, "director.mjs")} ${cmd.join(" ")}`, { env, stdio: "pipe" }).toString().trim();

async function api(method, route, body) {
  const res = await fetch(`${SERVER}/playtest/${route}`, {
    method,
    headers: { "X-Playtest-Token": TOKEN, "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${route}: ${res.status}`);
  return res.json();
}

const companies = async () => Object.fromEntries((await api("GET", "companies")).players.map((p) => [p.id, p]));
async function advance(hours, step = 1) {
  for (let left = hours; left > 1e-6; left -= step) await api("POST", "advance", { hours: Math.min(step, left) });
}

const results = [];
function check(name, ok, detail) {
  results.push({ name, ok });
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}${detail ? `  (${detail})` : ""}`);
}
const round = (x, d = 1) => Number(x).toFixed(d);

const scenarios = {
  /** Every middle-game Yard bug at once: six hours of nobody acting from day 14 19:00 (A2 row10, step 1) */
  async "row10-yard"() {
    console.log(director("restore", "row10"));
    await advance(1);
    const a = await companies();
    await advance(5);
    const b = await companies();
    const minutes = 5 * 60;
    const growth = (id) => (b[id].gold - a[id].gold) / minutes;
    const delta = (id, key) => (b[id][key] ?? 0) - (a[id][key] ?? 0);
    for (const [id, mark] of [[BEN, 70], [ANA, 200], [DEV, 120], [MIA, 90]]) {
      check(`${NAMES[id]} earns at least ${mark} g/min`, growth(id) >= mark, `${round(growth(id))} g/min`);
    }
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const sent = delta(id, "yardSent");
      const untaken = delta(id, "yardUntaken");
      if (sent > 1) check(`${NAMES[id]}: what nobody takes is at most 5% of what the Yard sends`, untaken <= 0.05 * sent, `${round(untaken, 0)} of ${round(sent, 0)} units`);
    }
    const labEnergy = b[CLEO].yardImports?.energy ?? 0;
    check("Cleo's estate is matched under 1 energy a minute", labEnergy < 1, `${round(labEnergy, 2)}/min`);
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const shown = b[id].goldPerMinute;
      check(`${NAMES[id]}'s top bar shows what was paid`, Math.abs(shown - growth(id)) <= 0.02 * Math.max(1, Math.abs(growth(id))), `${round(shown)} shown, ${round(growth(id))} real`);
    }
    for (const id of [ANA, BEN, CLEO, DEV, MIA]) {
      const shown = b[id].goldPerMinute;
      const real = growth(id);
      console.log(`info  ${NAMES[id]}: real ${round(real)} g/min, top bar ${round(shown)}, paid by the Yard ${round(delta(id, "yardPaid") / minutes)}, hub ${round(delta(id, "hubGold") / minutes)}, Houses ${round(delta(id, "npcGold") / minutes)}, project ${round(delta(id, "projectGold") / minutes)}, Yard matched ${round(b[id].yardMatched)}`);
    }
  },
};

if (!scenarios[scenario]) {
  console.log(`Scenarios: ${Object.keys(scenarios).join(", ")}`);
  process.exit(1);
}
if (publish) {
  console.log("Publishing the server into playtest/bin…");
  execSync(`dotnet publish ${path.join(ROOT, "..", "TransportPipes.Server")} -c Release -o ${path.join(ROOT, "bin")} --nologo -v q`, { stdio: "inherit", env: { ...process.env, DOTNET_ROOT: `${process.env.HOME}/.dotnet`, PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}` } });
}
try {
  await scenarios[scenario]();
} finally {
  console.log(director("server", "stop"));
}
const failed = results.filter((t) => !t.ok).length;
console.log(`\n${scenario}: ${results.length - failed} passed, ${failed} failed`);
process.exit(failed ? 1 : 0);

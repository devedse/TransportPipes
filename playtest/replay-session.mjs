// Agent replays (docs/playtest-fixes.html §6): run 1's own sessions played again on today's build, from the snapshot
// before them, by agents with that day's prompt (it carries the player's memory). A scripted replay checks that a line
// is true; only a player checks that it is understood.
//   node playtest/replay-session.mjs <replay run> <snapshot> <player>:<session> [<player>:<session> …] [--from story1] [--fresh] [--snapshots <run>]
// (--fresh: the players start without run 1's logins, for a snapshot from before they had accounts; --snapshots: the
// snapshot is another run's, e.g. a scripted replay's world moved on to a later day)
// prepares runs/<replay run>/ (players.json, story.json, each player's login and memory, the prompt pointed at the new
// folder, the group chat up to the snapshot's moment) and restores the snapshot on the playtest server (paused). Then:
//   PLAYTEST_RUN=<replay run> node playtest/driver.mjs &     (the browsers; Vite on :5174 with TP_SERVER=:5090)
//   PLAYTEST_RUN=<replay run> node playtest/director.mjs resume 5
//   …one agent per player: "read runs/<replay run>/<player>/prompt-<session>.md and follow it"…
//   PLAYTEST_RUN=<replay run> node playtest/director.mjs pause
import { execSync } from "node:child_process";
import { copyFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const args = process.argv.slice(2);
const fromIndex = args.indexOf("--from");
const from = fromIndex >= 0 ? args[fromIndex + 1] : "story1";
const fresh = args.includes("--fresh");
const snapshotsIndex = args.indexOf("--snapshots");
const [run, snapshot, ...sessions] = args.filter((t, i) => !t.startsWith("--") && args[i - 1] !== "--from" && args[i - 1] !== "--snapshots");
if (!run || !snapshot || sessions.length === 0) {
  console.log(readFileSync(new URL(import.meta.url), "utf8").split("\n").filter((t) => t.startsWith("//")).join("\n"));
  process.exit(1);
}
const source = path.join(ROOT, "runs", from);
const snapshots = path.join(ROOT, "runs", snapshotsIndex >= 0 ? args[snapshotsIndex + 1] : from, "snapshots");
const target = path.join(ROOT, "runs", run);
mkdirSync(target, { recursive: true });
for (const file of ["players.json", "story.json"]) copyFileSync(path.join(source, file), path.join(target, file));

// The world's moment of the snapshot: the group chat up to then
const { now } = JSON.parse(readFileSync(path.join(snapshots, `${snapshot}.json`), "utf8"));
const group = readFileSync(path.join(source, "group.jsonl"), "utf8").split("\n").filter(Boolean)
  .filter((line) => new Date(JSON.parse(line).game).getTime() <= new Date(now).getTime());
writeFileSync(path.join(target, "group.jsonl"), group.map((t) => t + "\n").join(""));

for (const entry of sessions) {
  const [player, session] = entry.split(":");
  const dir = path.join(target, player);
  mkdirSync(path.join(dir, "diary"), { recursive: true });
  const login = path.join(source, player, "storage.json");
  if (existsSync(login) && !fresh) copyFileSync(login, path.join(dir, "storage.json"));
  const prompt = readFileSync(path.join(source, player, `prompt-${session}.md`), "utf8").replaceAll(`/runs/${from}/`, `/runs/${run}/`);
  writeFileSync(path.join(dir, `prompt-${session}.md`), prompt);
  // The memory the prompt quotes is the one this session started with
  const memory = prompt.match(/memory\.md\):\n\n([\s\S]*?)\n\nStart with:/)?.[1];
  if (memory) writeFileSync(path.join(dir, "memory.md"), memory + "\n");
  console.log(`${player} ${session}: ${path.join(dir, `prompt-${session}.md`)}`);
}

const env = { ...process.env, PLAYTEST_RUN: run, PLAYTEST_SNAPSHOTS: snapshots };
console.log(execSync(`node ${path.join(ROOT, "director.mjs")} restore ${snapshot}`, { env }).toString().trim());
console.log(`Ready: ${group.length} group messages up to ${now}.`);

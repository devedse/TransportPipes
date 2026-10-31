// What's new in the game, for its "What's new" card: the commits that change the game itself (not its tests, docs or art
// scripts), newest first, each said by its line in scripts/changes-text.json when it has one, else by its "Player:"
// lines ("Player: -": not for players), else by its subject; each with the version it came in (the release tag 1.0.N
// that first holds it, this build's VERSION for what no tag holds yet). Nothing from before the playtest (players came
// on 2026-10-30; the commits before are the developers' words). CI runs it before the Docker build (it needs the git
// history and tags): VERSION=1.0.N node scripts/changes.mjs [out] [count]
import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, writeFileSync } from "node:fs";

const [out = "TransportPipes.Frontend/public/changes.json", count = "300"] = process.argv.slice(2);
const since = new Date("2026-10-30T15:00:00Z");
const paths = [
  "TransportPipes.Frontend/src",
  "TransportPipes.Frontend/public",
  "TransportPipes.Frontend/index.html",
  "TransportPipes.Game",
  "TransportPipes.Server",
  "TransportPipes.Database",
  ":(exclude)TransportPipes.Frontend/public/changes.json",
];
const git = (...args) => execFileSync("git", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
const textFile = new URL("./changes-text.json", import.meta.url);
const texts = existsSync(textFile) ? JSON.parse(readFileSync(textFile, "utf8")) : {};

// The version each commit came in: the first release tag that holds it. A tag that brought a whole branch at once (the
// move to master) holds work that came in builds of its own before: that keeps no version (it shows by day)
const versionOf = new Map();
let previous = null;
for (const tag of git("tag", "-l", "1.0.*", "--sort=v:refname").split("\n").filter(Boolean)) {
  const commits = git("rev-list", previous ? `${previous}..${tag}` : tag).split("\n").filter(Boolean);
  if (commits.length <= 30) {
    for (const commit of commits) versionOf.set(commit, tag);
  }
  previous = tag;
}
const current = process.env.VERSION || null;
const tagged = new Set(previous ? git("rev-list", previous).split("\n").filter(Boolean) : []);

const [headCommit, headDate] = git("log", "-1", "--format=%H%x1f%cI").trim().split("\x1f");
const log = git("log", "--no-merges", `-n${count}`, "--format=%H%x1f%cI%x1f%s%x1f%b%x1e", "--", ...paths);
const changes = [];
for (const record of log.split("\x1e")) {
  const [commit, date, subject, body = ""] = record.replace(/^\n/, "").split("\x1f");
  if (!commit || new Date(date) < since) continue;
  const short = commit.slice(0, 7);
  const own = texts[short];
  if (own === "-") continue;
  const player = body.split("\n").filter((t) => /^Player:/i.test(t)).map((t) => t.replace(/^Player:\s*/i, "").trim());
  if (own === undefined && player.some((t) => t === "-")) continue;
  const lines = player.filter((t) => t.length > 0 && t !== "-");
  const version = versionOf.get(commit) ?? (tagged.has(commit) ? null : current);
  changes.push({ commit: short, date, version, text: Array.isArray(own) ? own : lines.length > 0 ? lines : [subject.trim()] });
}
writeFileSync(out, `${JSON.stringify({ commit: headCommit.slice(0, 7), date: headDate, version: current, changes }, null, 1)}\n`);
console.log(`${out}: ${changes.length} changes up to ${headCommit.slice(0, 7)}`);

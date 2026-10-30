// What's new in the game, for its "What's new" card: the commits that change the game itself (not its tests, docs or art
// scripts), newest first, each said by its "Player:" line when it has one ("Player: -": not for players), else by its
// subject. CI runs it before the Docker build (it needs the git history): node scripts/changes.mjs [out] [count]
import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";

const [out = "TransportPipes.Frontend/public/changes.json", count = "300"] = process.argv.slice(2);
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
const [headCommit, headDate] = git("log", "-1", "--format=%H%x1f%cI").trim().split("\x1f");
const log = git("log", "--no-merges", `-n${count}`, "--format=%H%x1f%cI%x1f%s%x1f%b%x1e", "--", ...paths);
const changes = [];
for (const record of log.split("\x1e")) {
  const [commit, date, subject, body = ""] = record.replace(/^\n/, "").split("\x1f");
  if (!commit) continue;
  const player = body.split("\n").filter((t) => /^Player:/i.test(t)).map((t) => t.replace(/^Player:\s*/i, "").trim());
  if (player.some((t) => t === "-")) continue;
  const lines = player.filter((t) => t.length > 0);
  changes.push({ commit: commit.slice(0, 7), date, text: lines.length > 0 ? lines : [subject.trim()] });
}
writeFileSync(out, `${JSON.stringify({ commit: headCommit.slice(0, 7), date: headDate, changes }, null, 1)}\n`);
console.log(`${out}: ${changes.length} changes up to ${headCommit.slice(0, 7)}`);

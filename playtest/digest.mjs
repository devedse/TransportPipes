// A run's digest for the analyst (playtest/analyst.md): every session's diary, notes and page errors, the friends'
// group chat, the events the players saw and their numbers over time, in one Markdown file.
//   PLAYTEST_RUN=<run> node playtest/digest.mjs → playtest/runs/<run>/digest.md
import { existsSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const RUN = process.env.PLAYTEST_RUN ?? "pilot";
const RUN_DIR = path.join(ROOT, "runs", RUN);
const ORDER = ["ana", "ben", "cleo", "dev", "mia", "mia2"];
const lines = (file) => (existsSync(file) ? readFileSync(file, "utf8").split("\n").filter((t) => t.trim() !== "").map((t) => JSON.parse(t)) : []);
const read = (file) => (existsSync(file) ? readFileSync(file, "utf8") : "");

const story = JSON.parse(read(path.join(RUN_DIR, "story.json")) || "{}");
const metrics = lines(path.join(RUN_DIR, "metrics.jsonl"));
const day1 = new Date(story.day1 ?? new Date(new Date(metrics[0]?.now ?? Date.now()).getTime() - 20 * 3600e3)).getTime();
const when = (iso) => {
  const d = (new Date(iso).getTime() - day1) / 86400e3 + 1;
  const day = Math.floor(d);
  const m = Math.round((d - day) * 1440);
  return `day ${day} ${String(Math.floor(m / 60) % 24).padStart(2, "0")}:${String(m % 60).padStart(2, "0")}`;
};

const out = [`# Digest of run "${RUN}"`, ""];
const companies = new Map();
const sessions = [];
for (const id of ORDER) {
  const dir = path.join(RUN_DIR, id);
  if (!existsSync(path.join(dir, "actions.jsonl"))) continue;
  const company = JSON.parse(read(path.join(dir, "company.json")) || "null");
  if (company) companies.set(company.id, id);
  const actions = lines(path.join(dir, "actions.jsonl"));
  const notes = lines(path.join(dir, "notes.jsonl"));
  for (const session of [...new Set(actions.map((t) => t.session))]) {
    const acts = actions.filter((t) => t.session === session);
    sessions.push({ id, session, acts, notes: notes.filter((t) => t.session === session), diary: read(path.join(dir, "diary", `${session}.md`)), dir });
  }
}
sessions.sort((a, b) => new Date(a.acts[0].game) - new Date(b.acts[0].game));

out.push("## Sessions", "");
for (const s of sessions) {
  const errors = [...new Set(s.acts.flatMap((t) => t.errors ?? []))];
  out.push(`### ${s.id} · ${s.session} · ${when(s.acts[0].game)} to ${when(s.acts[s.acts.length - 1].game)} · actions #${s.acts[0].n}–#${s.acts[s.acts.length - 1].n}`);
  out.push(`Player folder: ${s.dir}`, "");
  out.push("Commands: " + s.acts.map((t) => `#${t.n} ${t.cmd}${t.args.length ? " " + t.args.join(" ").slice(0, 60) : ""}${t.ok ? "" : " (FAILED)"}`).join(" · "), "");
  if (s.notes.length) out.push("Notes (thinking aloud):", ...s.notes.map((t) => `- #${t.n} ${when(t.game)}: ${t.text}${t.shot ? ` (last screenshot: ${t.shot})` : ""}`), "");
  if (errors.length) out.push("Page errors:", ...errors.map((t) => `- ${t}`), "");
  out.push("Diary:", "", s.diary.trim() || "(none)", "");
}

out.push("## The friends' group chat", "");
for (const m of lines(path.join(RUN_DIR, "group.jsonl"))) out.push(`- ${when(m.game ?? m.at)} ${m.from}: ${m.text}`);
out.push("");

out.push("## What the game told the players (events, deduplicated)", "");
const seen = new Set();
for (const e of lines(path.join(RUN_DIR, "server", "events.jsonl"))) {
  if (!companies.has(e.company) || /^Loady: Well done/.test(e.text)) continue;
  const key = `${e.company}|${e.text}`;
  if (seen.has(key)) continue;
  seen.add(key);
  out.push(`- ${when(e.at)} → ${companies.get(e.company)} (${e.kind}): ${e.text}`);
}
out.push("");

out.push("## Numbers over time (one row per sample, players by their company)", "");
const header = ["time", ...[...companies.values()].map((id) => `${id}: level · gold · g/min · Yard g/min · buildings · stage`)];
out.push(`| ${header.join(" | ")} |`, `| ${header.map(() => "---").join(" | ")} |`);
let last = -1e9;
for (const m of metrics) {
  const t = new Date(m.now).getTime();
  // At most one row per game hour, besides the session marks
  if (t - last < 3600e3 && m.label === "advance") continue;
  last = t;
  const cells = [...companies.entries()].map(([cid]) => {
    const c = m.players.find((p) => p.id === cid);
    return c ? `L${c.level} · ${Math.round(c.gold)} · ${c.goldPerMinute.toFixed(0)} · ${c.neighbourIncome.toFixed(1)} · ${c.buildings} · ${c.projectStage ?? "-"}${c.asleep ? " · asleep" : ""}` : "-";
  });
  out.push(`| ${when(m.now)} ${m.label === "advance" ? "" : `(${m.label})`} | ${cells.join(" | ")} |`);
}
const last2 = metrics[metrics.length - 1];
if (last2) out.push("", `World at the end: era ${last2.era}, small mode ${last2.smallMode}, ${last2.activePlayers} active players, ${last2.houses} Houses.`);

const file = path.join(RUN_DIR, "digest.md");
writeFileSync(file, out.join("\n") + "\n");
console.log(file, `${(out.join("\n").length / 1024).toFixed(0)} KB`);

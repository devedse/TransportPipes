// Writes a player's session prompt (playtest/session.md filled in) for the director to give a player agent:
//   node playtest/prompt.mjs <player> <session> <budget> "<situation>" [url]
import { execSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const RUN = process.env.PLAYTEST_RUN ?? "pilot";
const [id, session, budget, situation, url] = process.argv.slice(2);
const dir = path.join(ROOT, "runs", RUN, id);
mkdirSync(path.join(dir, "diary"), { recursive: true });

const card = readFileSync(path.join(ROOT, "players", `${id}.md`), "utf8").trim();
const name = card.match(/^# (.+)$/m)[1];
const friends = id !== "mia";
const help = execSync(`node ${path.join(ROOT, "pt.mjs")} ${id} help`).toString().trim().replace(/^\[#\d+[^\]]*\] /, "");
const memoryFile = path.join(dir, "memory.md");
const memory = existsSync(memoryFile)
  ? `Your memory from your earlier sessions (${memoryFile}):\n\n${readFileSync(memoryFile, "utf8").trim()}`
  : "This is your first session: you have never played this game.";

const social = friends
  ? "**Talk with your friends** as real friends would: read the group chat (`group`) when you start and before you finish, and write in it when you'd text a friend — to coordinate, ask for help, share something nice, or decide something together. Use the game's own chat (`chat`) too whenever you'd rather talk inside the game. At least once a session; more when there's something to sort out together."
  : "**Other people:** nobody you know plays. The game's chat (`chat`) is there if you'd like to greet someone or ask something, as you would in a new place.";

const text = readFileSync(path.join(ROOT, "session.md"), "utf8")
  .replaceAll("{{CARD}}", card.replace(/^# .+\n+/, ""))
  .replaceAll("{{HELP}}", help.split("\n").map((t) => "    " + t).join("\n"))
  .replaceAll("{{ROOT}}", ROOT)
  .replaceAll("{{ID}}", id)
  .replaceAll("{{NAME}}", name)
  .replaceAll("{{DIR}}", dir)
  .replaceAll("{{SESSION}}", session)
  .replaceAll("{{BUDGET}}", budget)
  .replaceAll("{{TRIES}}", id === "ben" ? "two" : "three")
  .replaceAll("{{SOCIAL}}", social)
  .replaceAll("{{SCORE3}}", friends ? "together" : "alive")
  .replaceAll("{{TOGETHER}}", friends ? "Together                         (what I did with or for my friends, what we talked about)" : "Other people                     (did I meet anyone? did the place feel alive?)")
  .replaceAll("{{SITUATION}}", situation)
  .replaceAll("{{MEMORY}}", memory)
  .replaceAll("{{URL}}", url ? ` ${url}` : "");
const out = path.join(dir, `prompt-${session}.md`);
writeFileSync(out, text);
console.log(out);

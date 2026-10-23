// The AI playtest's report (docs/playtest-design.html §7): one page with everything a run produced — the analyst's
// findings, the promises, the story session by session with its screenshots, graphs of every player's level and
// income over game time, the diaries' scores, the friends' group chat and the world's events. Screenshots are embedded,
// so the page stands alone. PLAYTEST_RUN=<run> node playtest/report.mjs → playtest/runs/<run>/report/report.html
import { createRequire } from "node:module";
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const sharp = createRequire(new URL("../art/package.json", import.meta.url))("sharp");
const ROOT = path.dirname(new URL(import.meta.url).pathname);
const RUN = process.env.PLAYTEST_RUN ?? "pilot";
const RUN_DIR = path.join(ROOT, "runs", RUN);
const OUT = path.join(RUN_DIR, "report");
mkdirSync(OUT, { recursive: true });

const ORDER = ["ana", "ben", "cleo", "dev", "mia", "mia2"];
// The categorical palette (validated on the page's cream surface; legend, direct labels and tables carry identity too)
const COLORS = { ana: "#2a78d6", ben: "#eb6834", cleo: "#1baf7a", dev: "#eda100", mia: "#e87ba4", mia2: "#8b5fbf" };
// Run 2 plays Mia twice (docs/playtest-redesign.html step 7): the second one is told apart by name
const NAMES = { mia2: "Mia-B" };

const lines = (file) => (existsSync(file) ? readFileSync(file, "utf8").split("\n").filter((t) => t.trim() !== "").map((t) => JSON.parse(t)) : []);
const read = (file, fallback = null) => (existsSync(file) ? readFileSync(file, "utf8") : fallback);
const esc = (t) => String(t ?? "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");

const story = JSON.parse(read(path.join(RUN_DIR, "story.json"), "{}"));
const metrics = lines(path.join(RUN_DIR, "metrics.jsonl"));
const events = lines(path.join(RUN_DIR, "server", "events.jsonl"));
const groupChat = lines(path.join(RUN_DIR, "group.jsonl"));
const findings = JSON.parse(read(path.join(RUN_DIR, "findings.json"), "{}"));
const costs = new Map(JSON.parse(read(path.join(RUN_DIR, "costs.json"), "[]")).map((t) => [`${t.player}|${t.session}`, t]));
const directorSummary = read(path.join(RUN_DIR, "director-summary.md"), "");
const day1 = new Date(story.day1 ?? new Date(new Date(metrics[0]?.now ?? Date.now()).getTime() - 20 * 3600e3).toISOString()).getTime();
const dayOf = (iso) => (new Date(iso).getTime() - day1) / 86400e3 + 1;
const when = (iso) => {
  const d = dayOf(iso);
  const day = Math.floor(d);
  const minutes = Math.round((d - day) * 1440);
  return `day ${day} · ${String(Math.floor(minutes / 60) % 24).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;
};

// ---------------------------------------------------------------------------------------------- the players

/** A diary: its front matter and its sections */
function parseDiary(text) {
  const front = {};
  let body = text;
  const m = text.match(/^---\n([\s\S]*?)\n---\n?/);
  if (m) {
    for (const line of m[1].split("\n")) {
      const kv = line.match(/^\s*([a-z_]+):\s*(.*)$/i);
      if (kv) front[kv[1].toLowerCase()] = kv[2].trim();
    }
    body = text.slice(m[0].length);
  }
  const sections = {};
  let current = "Intro";
  for (const line of body.split("\n")) {
    const h = line.match(/^##\s+(.+?)\s*(\(.*\))?\s*$/);
    if (h) {
      current = h[1].trim();
      sections[current] = "";
    } else {
      sections[current] = (sections[current] ?? "") + line + "\n";
    }
  }
  return { front, sections };
}

const players = ORDER.filter((id) => existsSync(path.join(RUN_DIR, id, "actions.jsonl"))).map((id) => {
  const dir = path.join(RUN_DIR, id);
  const card = read(path.join(ROOT, "players", `${id}.md`), "");
  const actions = lines(path.join(dir, "actions.jsonl"));
  const notes = lines(path.join(dir, "notes.jsonl"));
  const company = JSON.parse(read(path.join(dir, "company.json"), "null"));
  const config = JSON.parse(read(path.join(RUN_DIR, "players.json"), "{}"))[id] ?? {};
  const bySession = new Map();
  for (const a of actions) {
    if (!bySession.has(a.session)) bySession.set(a.session, []);
    bySession.get(a.session).push(a);
  }
  const sessions = [...bySession.entries()].map(([session, acts]) => {
    const diaryText = read(path.join(dir, "diary", `${session}.md`), "");
    const diary = parseDiary(diaryText);
    const sessionNotes = notes.filter((t) => t.session === session);
    return {
      player: id,
      session,
      start: acts[0].game,
      end: acts[acts.length - 1].game,
      realMinutes: (new Date(acts[acts.length - 1].at) - new Date(acts[0].at)) / 60000,
      actions: acts.length,
      shots: acts.filter((t) => t.cmd === "shot" || (t.cmd === "look" && t.args.includes("--shot"))).length,
      errors: acts.flatMap((t) => t.errors ?? []),
      stuck: sessionNotes.filter((t) => /^\s*STUCK/i.test(t.text)).length,
      notes: sessionNotes,
      diary,
      hasDiary: diaryText !== "",
    };
  });
  return { id, name: NAMES[id] ?? card.match(/^# (.+)$/m)?.[1] ?? id, device: config.device ?? "desktop", color: COLORS[id], company, sessions, actions };
});
const sessions = players.flatMap((p) => p.sessions).sort((a, b) => new Date(a.start) - new Date(b.start));
const playerOf = (id) => players.find((p) => p.id === id);
const companyPlayer = new Map(players.filter((p) => p.company).map((p) => [p.company.id, p]));

// ---------------------------------------------------------------------------------------------- screenshots

const images = new Map();
/** Embeds a screenshot (from its full-size PNG when there is one) and returns its id */
async function image(file) {
  if (!file) return null;
  const small = path.isAbsolute(file) ? file : path.join(RUN_DIR, file);
  const full = small.replace(`${path.sep}small${path.sep}`, path.sep).replace(/\.jpg$/, ".png");
  const source = existsSync(full) ? full : existsSync(small) ? small : null;
  if (!source) return null;
  if (!images.has(source)) {
    const meta = await sharp(source).metadata();
    const width = Math.min(meta.width ?? 800, meta.width && meta.width < 600 ? 390 : 800);
    const buffer = await sharp(source).resize({ width }).webp({ quality: 62 }).toBuffer();
    images.set(source, { id: `img${images.size}`, data: `data:image/webp;base64,${buffer.toString("base64")}`, name: path.basename(source) });
  }
  return images.get(source).id;
}
/** The screenshots a text names (absolute, or relative to the player's folder like shots/small/…jpg) */
const shotPaths = (text, dir) => [...String(text ?? "").matchAll(/((?:\/|shots\/)[^\s`)\],;]+\.(?:jpg|png))/g)]
  .map((m) => (m[1].startsWith("/") ? m[1] : path.join(dir, m[1])));
async function figure(file, caption, cls = "") {
  const id = await image(file);
  if (!id) return "";
  // Thumbnails beside a text need no caption of their own
  return `<figure class="${cls}"><img data-img="${id}" alt="${esc(caption)}">${cls.includes("thumb") ? "" : `<figcaption>${esc(caption)}</figcaption>`}</figure>`;
}

// ---------------------------------------------------------------------------------------------- text

/** A little Markdown for the diaries: paragraphs, lists, bold, code and [#n] marks */
function md(text) {
  const inline = (t) => esc(t)
    .replace(/\*\*(.+?)\*\*/g, "<b>$1</b>")
    .replace(/(^|[^\w*])\*(?!\s)([^*]+?)\*(?!\w)/g, "$1<i>$2</i>")
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    .replace(/\[#(\d+)\]/g, '<span class="ref">#$1</span>')
    .replace(/(\/[^\s<]+\/shots\/small\/)([^\s<]+\.jpg)/g, '<span class="shotref">$2</span>');
  // Blocks are separated by blank lines; a block's lines join into one paragraph, a list item's into one item
  const out = [];
  let list = null;
  let para = [];
  const flushPara = () => {
    if (para.length) out.push(`<p>${inline(para.join(" "))}</p>`);
    para = [];
  };
  const flushList = () => {
    if (list) out.push(`<ul>${list.map((t) => `<li>${inline(t)}</li>`).join("")}</ul>`);
    list = null;
  };
  // A table: "| a | b |" rows, the first the header, a "| --- |" row under it (run 3's chores counts)
  let table = null;
  const cells = (line) => line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((t) => t.trim());
  const flushTable = () => {
    if (table) {
      const [head, ...rows] = table;
      out.push(`<div class="scroll"><table><tr>${head.map((t) => `<th>${inline(t)}</th>`).join("")}</tr>${rows.map((r) => `<tr>${r.map((t) => `<td>${inline(t)}</td>`).join("")}</tr>`).join("")}</table></div>`);
    }
    table = null;
  };
  for (const raw of String(text ?? "").split("\n")) {
    const line = raw.trimEnd();
    const item = line.match(/^\s*(?:[-*]|\d+\.)\s+(.*)$/);
    if (/^\s*\|.*\|\s*$/.test(line)) {
      flushPara();
      flushList();
      table ??= [];
      if (!/^[\s|:-]+$/.test(line)) table.push(cells(line));
      continue;
    }
    flushTable();
    if (item) {
      flushPara();
      list ??= [];
      list.push(item[1]);
    } else if (line.trim() === "") {
      flushPara();
      flushList();
    } else if (list && /^\s+/.test(raw)) {
      list[list.length - 1] += " " + line.trim();
    } else {
      flushList();
      para.push(line.trim());
    }
  }
  flushPara();
  flushList();
  flushTable();
  return out.join("");
}

const pill = (status) => {
  const s = String(status ?? "").toLowerCase();
  const cls = s === "kept" || s === "yes" || s === "pass" ? "good" : s === "late" || s === "partly" || s === "maybe" || s === "medium" ? "warn" : s === "missed" || s === "no" || s === "high" || s === "fail" ? "bad" : "info";
  return `<span class="pill ${cls}">${esc(status ?? "—")}</span>`;
};
const dot = (p) => `<span class="key" style="background:${p.color}"></span>`;

// ---------------------------------------------------------------------------------------------- the series

/** Jo (run 2 on): a scripted solo human with no agent and no logs, only numbers; a grey line beside the players */
const joId = metrics.at(-1)?.players.find((t) => t.name === "Jo's Works")?.id;
const charted = [...players.filter((p) => p.company), ...(joId ? [{ id: "jo", name: "Jo (scripted)", color: "#9a8f7a", company: { id: joId } }] : [])];

/** One line per player of a number from the samples (a field, or a function of a sample's player) */
function series(field) {
  const value = typeof field === "function" ? field : (c) => Number(c[field] ?? 0);
  return charted.map((p) => ({
    id: p.id,
    name: p.name,
    color: p.color,
    // A value of null leaves the sample out (a sleeping company has no income to count days of)
    points: metrics.map((m) => {
      const c = m.players.find((t) => t.id === p.company.id);
      const v = c ? value(c) : null;
      return v === null ? null : [Number(dayOf(m.now).toFixed(4)), v];
    }).filter(Boolean),
  })).filter((s) => s.points.length > 0);
}
/** Gold earned a minute between samples (lifetime earnings: hub, Yard, orders and rewards), over at least an hour */
function earnedSeries() {
  return charted.map((p) => {
    const points = [];
    let last = null;
    for (const m of metrics) {
      const c = m.players.find((t) => t.id === p.company.id);
      if (!c) continue;
      const t = new Date(m.now).getTime();
      if (last && t - last.t >= 3600e3) {
        points.push([Number(dayOf(m.now).toFixed(4)), Math.max(0, (c.earned - last.earned) / ((t - last.t) / 60000))]);
        last = { t, earned: c.earned };
      } else if (!last) {
        last = { t, earned: c.earned };
      }
    }
    return { id: p.id, name: p.name, color: p.color, points };
  }).filter((s) => s.points.length > 0);
}

const charts = {
  level: { title: "Company Level", unit: "", series: series("level") },
  earned: { title: "What they really earned (gold a minute, from their lifetime earnings)", unit: " g/min", series: earnedSeries() },
  gpm: { title: "What the top bar showed (hub sales, gold a minute)", unit: " g/min", series: series("goldPerMinute") },
  neighbour: { title: "What the Yard matched to sell to other players (gold a minute)", unit: " g/min", series: series("neighbourIncome") },
  buildings: { title: "Buildings", unit: "", series: series("buildings") },
};
// Run 4 on: what the gold was for (the samples count gold spent since run 4)
const spending = metrics.some((m) => m.players.some((c) => c.spent !== undefined));
if (spending) {
  // From 100k earned: a newcomer's first buildings, bought with the starting gold, would read as half their earnings
  charts.spent = { title: "Gold spent, as a share of all they earned (from 100k earned)", unit: " %", series: series((c) => (c.earned >= 100000 ? Number((100 * (c.spent ?? 0) / c.earned).toFixed(2)) : null)) };
  charts.banked = { title: "Gold held, in days of the top bar's income", unit: " days", series: series((c) => (c.goldPerMinute >= 1 ? Number((c.gold / (c.goldPerMinute * 1440)).toFixed(1)) : null)) };
}
/**
 * Run by run (run 3 on): each player's Level and what they earned a minute, averaged over each game day, in every
 * earlier story run (story1, story2, …) beside this one; one small chart per player
 */
const runNumber = Number(RUN.match(/^story(\d+)$/)?.[1] ?? 0);
// The most recent earlier run darker, the one before it lighter
const RUN_SHADES = ["#8f8472", "#c9bfae"];
function dailyRun(dir, id) {
  const company = JSON.parse(read(path.join(dir, id, "company.json"), "null"));
  const m = lines(path.join(dir, "metrics.jsonl"));
  const d1 = new Date(JSON.parse(read(path.join(dir, "story.json"), "{}")).day1 ?? m[0]?.now).getTime();
  if (!company || m.length === 0) return null;
  const byDay = new Map();
  for (const sample of m) {
    const c = sample.players.find((t) => t.id === company.id);
    if (c) byDay.set(Math.floor((new Date(sample.now).getTime() - d1) / 86400e3) + 1, c);
  }
  const days = [...byDay.keys()].sort((a, b) => a - b);
  return {
    level: days.map((d) => [d, byDay.get(d).level]),
    earned: days.slice(1).map((d, i) => [d, Math.max(0, (byDay.get(d).earned - byDay.get(days[i]).earned) / ((d - days[i]) * 1440))]),
  };
}
const runCharts = {};
const BUILDS = ["run 1 on the first v1.2", "run 2 on the redesign (dealt land)", "run 3 on the build after run 2's fixes", "run 4 on run 3's fixes and the designers' nine steps"];
if (runNumber >= 2) {
  for (const p of players.filter((t) => t.company)) {
    const runs = [];
    for (let n = 1; n <= runNumber; n++) {
      const data = dailyRun(path.join(ROOT, "runs", `story${n}`), p.id);
      if (data) runs.push({ n, data, color: n === runNumber ? p.color : RUN_SHADES[(runNumber - 1 - n) % RUN_SHADES.length] });
    }
    if (runs.length < 2) continue;
    for (const key of ["earned", "level"]) {
      runCharts[`${key}-${p.id}`] = {
        title: `${p.name}: ${key === "earned" ? "earned a minute (each day's average)" : "Level at each day's end"}`,
        unit: key === "earned" ? " g/min" : "",
        player: p.id,
        h: 200,
        series: runs.map((r) => ({ id: `run${r.n}`, name: `Run ${r.n}`, color: r.color, points: r.data[key] })),
      };
    }
  }
}
Object.assign(charts, runCharts);
const sessionMarks = sessions.map((s) => ({ player: s.player, color: playerOf(s.player).color, start: dayOf(s.start), end: dayOf(s.end), label: s.session }));

// ---------------------------------------------------------------------------------------------- sections

async function storySection() {
  const parts = [];
  for (const s of sessions) {
    const p = playerOf(s.player);
    const f = s.diary.front;
    const keyShots = shotPaths(s.diary.sections["Key screenshots"], path.join(RUN_DIR, s.player));
    const noteShots = s.notes.map((t) => t.shot).filter(Boolean);
    const all = [...new Set([...keyShots, ...noteShots])].slice(0, 6);
    const lineOf = (file) => (s.diary.sections["Key screenshots"] ?? "").split("\n").find((t) => t.includes(path.basename(file)))?.replace(/.*?\.jpg`?\s*[—–-]*\s*/, "") ?? "";
    const figs = [];
    for (const file of all) figs.push(await figure(file, lineOf(file) || path.basename(file), p.device === "phone" ? "phone" : ""));
    const scores = ["clarity", "fun", "together", "alive"].filter((k) => f[k]).map((k) => `<span class="score"><b>${f[k]}</b> ${k}</span>`).join("");
    parts.push(`<article class="session" id="${s.player}-${s.session}">
      <header>${dot(p)}<h3>${esc(p.name)} · ${esc(when(s.start))}</h3>
        <span class="muted small">${s.actions} actions · ${s.realMinutes.toFixed(0)} real min · ${s.shots} screenshots${s.stuck ? ` · <b>${s.stuck} stuck</b>` : ""}${s.errors.length ? ` · ${s.errors.length} page errors` : ""}</span>
        <span class="scores">${scores}${f.come_back_tomorrow ? ` <span class="score">tomorrow? ${pill(f.come_back_tomorrow)}</span>` : ""}</span></header>
      <div class="grid2">
        <div>${["What I wanted", "What felt good", "The three worst moments"].map((k) => s.diary.sections[k] ? `<h4>${esc(k)}</h4>${md(s.diary.sections[k])}` : "").join("")}</div>
        <div>${["Where I got stuck", "What looked wrong or broken", "Together", "Other people", "Would I come back tomorrow, and why"].map((k) => s.diary.sections[k] && s.diary.sections[k].trim() ? `<h4>${esc(k)}</h4>${md(s.diary.sections[k])}` : "").join("")}</div>
      </div>
      ${figs.length ? `<div class="shots">${figs.join("")}</div>` : ""}
      ${s.hasDiary ? "" : '<p class="muted">No diary for this session.</p>'}
    </article>`);
  }
  return parts.join("\n");
}

/** "Cleo · d06-1400 #177 · max-lab": whose screen, which session and action, and the player's label */
function shotCaption(file) {
  const id = ORDER.find((t) => file.includes(`${path.sep}${t}${path.sep}`));
  const m = path.basename(file).match(/^(d\d+-\d+)-0*(\d+)-(.+)\.(?:jpg|png)$/);
  const who = id ? playerOf(id)?.name ?? id : "";
  return m ? `${who} · ${m[1]} #${m[2]} · ${m[3].replace(/-/g, " ")}` : `${who} · ${path.basename(file)}`;
}

async function findingList(items, kind) {
  const out = [];
  for (const item of items ?? []) {
    const figs = [];
    for (const file of (item.shots ?? []).slice(0, 3)) figs.push(await figure(file, shotCaption(file)));
    const who = (item.players ?? []).map((id) => playerOf(id)).filter(Boolean).map((p) => `${dot(p)}${esc(p.name)}`).join(" ");
    out.push(`<div class="finding ${kind}">
      <div class="finding-head">${item.severity ? pill(item.severity) : ""}<h3>${esc(item.title)}</h3>${who ? `<span class="who">${who}</span>` : ""}</div>
      ${item.what ? md(item.what) : ""}${item.repro ? `<h4>Repro</h4>${md(item.repro)}` : ""}${item.evidence ? `<p class="evidence">${esc(item.evidence)}</p>` : ""}
      ${item.fix ? `<p class="fix"><b>Fix:</b> ${esc(item.fix)}</p>` : ""}
      ${figs.length ? `<div class="shots">${figs.join("")}</div>` : ""}
    </div>`);
  }
  return out.join("\n") || '<p class="muted">None.</p>';
}

async function promisesTable() {
  if (!findings.promises?.length) return '<p class="muted">The analyst hasn\'t checked the promises yet.</p>';
  const rows = [];
  for (const p of findings.promises) {
    const figs = [];
    for (const file of (p.shots ?? []).slice(0, 2)) figs.push(await figure(file, p.promise, "thumb"));
    rows.push(`<tr><td class="nowrap"><b>${esc(p.when)}</b></td><td>${esc(p.promise)}</td><td>${pill(p.status)}</td><td>${md(p.evidence)}${figs.join("")}</td></tr>`);
  }
  return `<div class="scroll"><table><tr><th>When</th><th>The design promises</th><th></th><th>What happened</th></tr>${rows.join("")}</table></div>`;
}

/** Who played when: a row per player, a mark per session, and the moments that mattered */
function calendar() {
  const last = Math.max(2, ...sessions.map((t) => dayOf(t.end)), ...metrics.map((m) => dayOf(m.now)));
  const days = Math.ceil(last);
  const W = 1000, L = 70, R = 14, rowH = 30, T = 26;
  const H = T + players.length * rowH + 40;
  const X = (d) => L + ((d - 1) / days) * (W - L - R);
  let svg = `<svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Who played when">`;
  for (let d = 1; d <= days + 1; d += days > 20 ? 7 : 1) {
    svg += `<line x1="${X(d)}" x2="${X(d)}" y1="${T - 6}" y2="${H - 34}" stroke="rgba(80,60,30,.12)"/><text x="${X(d)}" y="${H - 18}" font-size="12" text-anchor="middle" fill="#6b5c43">day ${d}</text>`;
  }
  // Moments: the first finished stage, the Era changing, a player falling asleep
  const moments = [];
  const firstFinished = metrics.find((m) => m.players.some((p) => (p.projectFinished ?? []).length > 0));
  if (firstFinished) moments.push([dayOf(firstFinished.now), "Foundation built"]);
  for (let i = 1; i < metrics.length; i++) if (metrics[i].era !== metrics[i - 1].era) moments.push([dayOf(metrics[i].now), `Era ${metrics[i].era}`]);
  for (const p of players.filter((t) => t.company)) {
    const asleep = metrics.find((m) => m.players.find((c) => c.id === p.company.id)?.asleep);
    if (asleep) moments.push([dayOf(asleep.now), `${p.name} asleep`]);
  }
  moments.forEach(([d, label], i) => {
    svg += `<line x1="${X(d)}" x2="${X(d)}" y1="${T - 10}" y2="${H - 34}" stroke="#94692a" stroke-width="1.5"/><text x="${X(d) + 4}" y="${T - 12 + (i % 2) * 0}" font-size="11" fill="#94692a">${esc(label)}</text>`;
  });
  players.forEach((p, i) => {
    const y = T + i * rowH + rowH / 2;
    svg += `<text x="${L - 10}" y="${y + 4}" font-size="13" text-anchor="end" fill="#2b2418">${esc(p.name)}</text>`;
    for (const t of p.sessions) {
      const x0 = X(dayOf(t.start));
      const w = Math.max(6, X(dayOf(t.end)) - x0);
      svg += `<rect x="${x0}" y="${y - 8}" width="${w}" height="16" rx="4" fill="${p.color}"><title>${esc(p.name)} · ${esc(when(t.start))} · ${t.actions} actions · ${t.stuck} stuck</title></rect>`;
    }
  });
  return `${svg}</svg>`;
}

function scoresTable() {
  const rows = sessions.map((s) => {
    const p = playerOf(s.player);
    const f = s.diary.front;
    const cell = (v) => `<td class="num c${esc(v)}">${esc(v ?? "—")}</td>`;
    const cost = costs.get(`${s.player}|${s.session}`);
    return `<tr><td>${dot(p)}${esc(p.name)}</td><td class="nowrap">${esc(when(s.start))}</td>${cell(f.clarity)}${cell(f.fun)}${cell(f.together ?? f.alive)}<td>${pill(f.come_back_tomorrow)}</td><td class="num">${s.actions}</td><td class="num">${s.stuck}</td><td class="num">${s.shots}</td><td class="num">${cost ? `${Math.round(cost.tokens / 1000)}k` : ""}</td><td class="num">${cost ? (cost.seconds / 60).toFixed(1) : ""}</td></tr>`;
  });
  return `<div class="scroll"><table class="scores-table"><tr><th>Player</th><th>Session</th><th>Clarity</th><th>Fun</th><th>Together / alive</th><th>Tomorrow?</th><th>Actions</th><th>Stuck</th><th>Shots</th><th>Tokens</th><th>Agent min</th></tr>${rows.join("")}</table></div>`;
}

function groupSection() {
  if (groupChat.length === 0) return '<p class="muted">Nobody wrote in the group chat.</p>';
  return `<div class="messenger">${groupChat.map((m) => {
    const p = playerOf(m.from);
    return `<div class="msg"><span class="from">${p ? dot(p) + esc(p.name) : esc(m.from)}</span><span class="at">${esc(when(m.game ?? m.at))}</span><p>${esc(m.text)}</p></div>`;
  }).join("")}</div>`;
}

function eventsSection() {
  const interesting = /Handshake|Foundation|Water Tower|Workshop|Foundry|Landmark|moved in|moved out|arrived|crew|stand-in|asleep|Tailwind|keep|Discovery|league|trunk|Ask|link|Era|tended/i;
  const own = events.filter((e) => companyPlayer.has(e.company) && interesting.test(e.text));
  const seen = new Set();
  const rows = own.filter((e) => {
    const key = `${e.company}|${e.text}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  }).slice(0, 400).map((e) => {
    const p = companyPlayer.get(e.company);
    return `<tr><td class="nowrap">${esc(when(e.at))}</td><td>${dot(p)}${esc(p.name)}</td><td>${esc(e.text)}</td></tr>`;
  });
  return rows.length ? `<div class="scroll tall"><table><tr><th>When</th><th>Who saw it</th><th>What the game said</th></tr>${rows.join("")}</table></div>` : '<p class="muted">No events.</p>';
}

function tiles() {
  const stuck = sessions.reduce((s, t) => s + t.stuck, 0);
  const shots = sessions.reduce((s, t) => s + t.shots, 0);
  const actions = sessions.reduce((s, t) => s + t.actions, 0);
  const days = metrics.length ? Math.max(...metrics.map((m) => dayOf(m.now))) : 0;
  const kept = (findings.promises ?? []).filter((t) => /kept/i.test(t.status)).length;
  const items = [
    ["Players", players.length],
    ["Sessions", sessions.length],
    ["Game days", Math.ceil(days)],
    ["Actions", actions.toLocaleString("en")],
    ["Screenshots", shots],
    ["Stuck moments", stuck],
    ["Bugs found", (findings.bugs ?? []).length],
    ["Promises kept", findings.promises ? `${kept} of ${findings.promises.length}` : "—"],
  ];
  return `<div class="tiles">${items.map(([k, v]) => `<div class="tile"><span>${esc(k)}</span><b>${esc(v)}</b></div>`).join("")}</div>`;
}

/** The run's marks (docs/playtest-redesign.html §7): each against its pass, with the earlier runs beside it, and what changed since */
function marksSection() {
  const since = Object.keys(findings).filter((t) => /^sinceRun\d+$/.test(t)).sort();
  if (!findings.marks?.length && !since.length) return "";
  const status = (t) => pill(/pass/i.test(t) ? "pass" : /fail/i.test(t) ? "fail" : t);
  // One column per run the marks name (run 2: run1 and run2; run 3: run1, run2 and run3)
  const runs = [...new Set((findings.marks ?? []).flatMap((m) => Object.keys(m).filter((t) => /^run\d+$/.test(t))))].sort();
  const run = runs.at(-1)?.slice(3) ?? "";
  // The evidence on its own line under each mark, across the table (in the first cell it made a tall narrow column)
  const rows = (findings.marks ?? []).map((m) => `<tr class="mark"><td><b>${esc(m.mark)}</b></td><td>${esc(m.pass ?? "")}</td>${runs.map((r) => `<td>${esc(m[r] ?? "")}</td>`).join("")}<td>${status(m.status ?? "")}</td></tr>${m.evidence ? `<tr class="mark-evidence"><td colspan="${runs.length + 3}" class="small muted">${md(m.evidence)}</td></tr>` : ""}`).join("");
  return `<section class="page">
  <h2>The marks</h2>
  <p>The plan's marks (docs/playtest-redesign.html §7; from run 4 also the designers' table in docs/playtest-run3-design.html),
  measured from the server's numbers and the logs for run ${esc(run)},
  with the earlier runs beside them.</p>
  ${rows ? `<div class="scroll"><table><tr><th>Mark</th><th>Pass</th>${runs.map((r) => `<th>Run ${esc(r.slice(3))}</th>`).join("")}<th></th></tr>${rows}</table></div>` : ""}
  ${since.map((k) => `<h3>Since run ${esc(k.slice(8))}</h3>${md(findings[k])}`).join("")}
</section>`;
}

// ---------------------------------------------------------------------------------------------- the page

const body = `
<section class="page cover">
  <div class="kicker">TransportPipes · v1.2 · AI playtest · run "${esc(RUN)}"</div>
  <h1>${esc(findings.title ?? "How the playtest went")}</h1>
  <p class="lead">${esc(findings.summary ?? "")}</p>
  <div class="cast">${players.map((p) => `<div class="member">${dot(p)}<b>${esc(p.name)}</b><span>${esc(p.device)}${p.company ? ` · ${esc(p.company.name)}` : ""} · ${p.sessions.length} session${p.sessions.length === 1 ? "" : "s"}</span></div>`).join("")}</div>
</section>

<section class="page">
  <h2>At a glance</h2>
  ${tiles()}
  ${(findings.verdict ?? []).map((v) => `<div class="callout ${esc(v.kind ?? "")}"><strong>${esc(v.title)}</strong>${md(v.text)}</div>`).join("")}
</section>

${marksSection()}

${findings.gold ? `<section class="page">
  <h2>The gold</h2>
  <p>What the players spent, on what, and why the rest piled up, from the samples and the diaries.</p>
  ${md(findings.gold)}
</section>` : ""}

${findings.chores ? `<section class="page">
  <h2>Chores</h2>
  <p>How much of the players' time went to upkeep (storm repair, hub-order deliveries, a full Storage, pipe fixes)
  against growing their estates and playing together, counted from the logs.</p>
  ${md(findings.chores)}
</section>` : ""}

<section class="page">
  <h2>How the playtest ran</h2>
  ${md(directorSummary)}
  <h3>Who played when</h3>
  <p class="muted small">One row per player, one mark per session (hover for details); brass lines are the moments
  that mattered.</p>
  <div class="calendar">${calendar()}</div>
</section>

<section class="page">
  <h2>The design's promises</h2>
  <p>Each moment the design promises something (docs/multiplayer-design.html §17), checked against the logs, the events
  and the screenshots. The players never saw these.</p>
  ${await promisesTable()}
</section>

<section class="page">
  <h2>Five weeks in numbers</h2>
  <p>Sampled from the server every six game hours and at every session's start and end. The shaded bands are the
  players' sessions. Hover a chart for the values; the table under each chart has them all.</p>
  <div class="chart" data-chart="level"></div>
  <div class="chart" data-chart="earned"></div>
  <div class="chart" data-chart="gpm"></div>
  <div class="chart" data-chart="neighbour"></div>
  <div class="chart" data-chart="buildings"></div>
  ${spending ? `<div class="chart" data-chart="spent"></div>
  <div class="chart" data-chart="banked"></div>` : ""}
</section>

${Object.keys(runCharts).length ? `<section class="page">
  <h2>Run by run</h2>
  <p>Each player in every run so far: what they earned a minute (each game day's average, from their lifetime
  earnings) and their Level at the end of each day. This run in the player's colour, the earlier runs in grey (the
  darker one is the most recent); the shaded bands are this run's sessions. The runs played the same story on
  different builds: ${BUILDS.slice(0, runNumber).join(", ")}.</p>
  <div class="runs">${players.filter((p) => runCharts[`earned-${p.id}`]).map((p) => `<div class="chart" data-chart="earned-${p.id}"></div><div class="chart" data-chart="level-${p.id}"></div>`).join("")}</div>
</section>` : ""}

<section class="page">
  <h2>How each session felt</h2>
  <p>The players' own scores at the end of each session (1–5), and how many times they wrote STUCK.</p>
  ${scoresTable()}
</section>

<section class="page">
  <h2>Where people got stuck</h2>
  ${await findingList(findings.friction, "friction")}
</section>

<section class="page">
  <h2>Bugs</h2>
  ${await findingList(findings.bugs, "bug")}
</section>

<section class="page">
  <h2>How it looks</h2>
  ${await findingList(findings.looks, "look")}
</section>

<section class="page">
  <h2>What came from the playtest itself</h2>
  <p>Trouble that the setup caused (the driver, the AI's aiming by tile numbers, the clock jumping), not the game.</p>
  ${(findings.artifacts ?? []).map((a) => `<div class="callout"><strong>${esc(a.title)}</strong>${md(a.what)}</div>`).join("") || '<p class="muted">None noted.</p>'}
</section>

<section class="page">
  <h2>Together</h2>
  ${findings.together ? md(findings.together) : ""}
  <h3>The friends' group chat</h3>
  ${groupSection()}
</section>

<section class="page">
  <h2>In their words</h2>
  <div class="quotes">${(findings.quotes ?? []).map((q) => { const p = playerOf(q.player); return `<blockquote>${md(q.text)}<cite>${p ? dot(p) + esc(p.name) : ""}${q.session ? ` · ${esc(q.session)}` : ""}</cite></blockquote>`; }).join("") || '<p class="muted">No quotes picked.</p>'}</div>
</section>

<section class="page">
  <h2>What to change</h2>
  ${(findings.recommendations ?? []).map((r, i) => `<div class="rec"><span class="n">${i + 1}</span><div><h3>${esc(r.title)} ${r.priority ? pill(r.priority) : ""}</h3>${md(r.text)}</div></div>`).join("") || '<p class="muted">None yet.</p>'}
</section>

<section class="page">
  <h2>The story, session by session</h2>
  <p>Every session in game time: what the player wanted, what felt good and bad, where they got stuck, and the
  screenshots they took at the key moments.</p>
  ${await storySection()}
</section>

<section class="page">
  <h2>What the game told them</h2>
  <p>The world's notes and toasts to the players about each other, the station and its project (deduplicated).</p>
  ${eventsSection()}
</section>
`;

const imageData = Object.fromEntries([...images.values()].map((t) => [t.id, t.data]));
const chartData = { charts, sessionMarks, players: players.map((p) => ({ id: p.id, name: p.name, color: p.color })) };

const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Playtest Report</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,600;9..144,800&family=Nunito:wght@400;600;700;800&display=swap" rel="stylesheet">
<style>
:root { --teal:#24575a; --teal-dark:#173c3f; --accent:#236b69; --brass:#c9963a; --brass-dark:#94692a; --cream:#f4ead3;
  --paper:#fffaf0; --ink:#2b2418; --muted:#6b5c43; --green:#33761f; --red:#a12f1f; --gold:#8a5d00; --line:rgba(80,60,30,.18);
  --grid:rgba(80,60,30,.12); --good-bg:#d6eccd; --bad-bg:#f6d3cc; --warn-bg:#f3e6c4; }
* { box-sizing:border-box; }
html, body { background:#e9dfc6; }
body { margin:0; font-family:Nunito, system-ui, sans-serif; color:var(--ink); line-height:1.55; }
.page { max-width:1120px; margin:24px auto; padding:44px 52px; background:var(--paper); border-radius:18px; box-shadow:0 10px 30px rgba(40,30,10,.18); border:1px solid var(--line); }
@media (max-width:720px) { .page { margin:12px 8px; padding:22px 16px; border-radius:12px; } }
h1, h2, h3 { font-family:Fraunces, Georgia, serif; color:var(--teal-dark); line-height:1.2; text-wrap:balance; }
h1 { font-size:2.5rem; margin:0 0 10px; } h2 { font-size:1.8rem; margin:0 0 14px; padding-bottom:8px; border-bottom:3px solid var(--brass); }
h3 { font-size:1.15rem; margin:0; } h4 { margin:14px 0 4px; font-size:.82rem; text-transform:uppercase; letter-spacing:.06em; color:var(--muted); }
p { margin:0 0 10px; max-width:78ch; } ul { margin:0 0 10px; padding-left:20px; } li { margin-bottom:3px; }
.kicker { text-transform:uppercase; letter-spacing:.12em; font-weight:800; color:var(--brass-dark); font-size:.82rem; }
.cover { background:linear-gradient(160deg,#1f4d50 0%,#24575a 55%,#2f6b66 100%); color:#fff; }
.cover h1 { color:#fff; } .cover .kicker { color:#f2d27a; } .cover .lead { color:#f4ead3; font-size:1.12rem; }
.cast { display:flex; flex-wrap:wrap; gap:10px; margin-top:20px; }
.member { background:rgba(255,255,255,.1); border:1px solid rgba(242,210,122,.5); border-radius:12px; padding:8px 12px; display:flex; align-items:center; gap:8px; flex-wrap:wrap; }
.member b { font-family:Fraunces, Georgia, serif; } .member span { color:#f4ead3; font-size:.86rem; }
.muted { color:var(--muted); } .small { font-size:.86rem; } .nowrap { white-space:nowrap; }
.key { display:inline-block; width:11px; height:11px; border-radius:50%; margin-right:6px; vertical-align:-1px; border:2px solid var(--paper); box-shadow:0 0 0 1px var(--line); }
.cover .key { border-color:#24575a; }
.tiles { display:grid; grid-template-columns:repeat(4,minmax(0,1fr)); gap:12px; margin-bottom:18px; }
@media (max-width:720px) { .tiles { grid-template-columns:repeat(2,minmax(0,1fr)); } }
.tile { background:var(--cream); border-radius:12px; padding:12px 14px; display:flex; flex-direction:column; }
.tile span { font-size:.8rem; color:var(--muted); font-weight:700; } .tile b { font-size:1.6rem; color:var(--teal-dark); }
.callout { padding:14px 18px; border-radius:12px; background:var(--cream); border-left:6px solid var(--brass); margin:12px 0; }
.callout.good { border-left-color:var(--accent); background:#e4efe9; } .callout.bad { border-left-color:var(--red); background:#f8e7e2; }
.callout strong:first-child { display:block; font-family:Fraunces, Georgia, serif; font-size:1.05rem; margin-bottom:4px; }
.pill { display:inline-block; padding:1px 9px; border-radius:999px; font-size:.76rem; font-weight:800; white-space:nowrap; }
.pill.good { background:var(--good-bg); color:var(--green); } .pill.bad { background:var(--bad-bg); color:var(--red); }
.pill.warn { background:var(--warn-bg); color:var(--gold); } .pill.info { background:#dde9f3; color:#24507a; }
.scroll { overflow-x:auto; } .scroll.tall { max-height:640px; overflow-y:auto; }
/* Long paths and names in the findings break rather than push the page wider (run 2's evidence cites absolute paths) */
.page { overflow-wrap:anywhere; }
/* Table cells keep their words whole: a narrow screen scrolls the table instead (the evidence rows still break) */
.scroll table { min-width:34em; } .scroll th, .scroll td:not([colspan]) { overflow-wrap:normal; }
tr.mark td { border-bottom:none; padding-bottom:2px; } tr.mark-evidence td { padding-top:0; }
table { width:100%; border-collapse:collapse; margin:8px 0 16px; font-size:.9rem; }
th { text-align:left; background:var(--cream); color:var(--teal-dark); padding:7px 10px; font-weight:800; position:sticky; top:0; }
td { padding:7px 10px; border-top:1px solid var(--line); vertical-align:top; } td.num { text-align:right; font-variant-numeric:tabular-nums; }
.scores-table td.c1 { background:#f6d3cc; } .scores-table td.c2 { background:#f8e2d6; } .scores-table td.c3 { background:#f3ecd6; }
.scores-table td.c4 { background:#e3efdc; } .scores-table td.c5 { background:#d6eccd; }
.grid2 { display:grid; grid-template-columns:1fr 1fr; gap:22px; } .grid2 > * { min-width:0; }
@media (max-width:820px) { .grid2 { grid-template-columns:1fr; } }
.session { border-top:2px solid var(--line); padding:18px 0 8px; }
.session header { display:flex; flex-wrap:wrap; align-items:center; gap:8px 14px; margin-bottom:6px; }
.scores { display:flex; gap:8px; flex-wrap:wrap; } .score { background:var(--cream); border-radius:8px; padding:1px 8px; font-size:.82rem; }
.shots { display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:12px; margin-top:10px; }
figure { margin:0; } figure img { width:100%; border-radius:10px; border:1px solid var(--line); display:block; background:#ddd; cursor:zoom-in; }
figure.phone img { max-width:240px; } figure.thumb { max-width:260px; margin-top:6px; }
figcaption { font-size:.8rem; color:var(--muted); margin-top:4px; }
.ref { background:#e4efe9; color:var(--teal-dark); border-radius:5px; padding:0 4px; font-size:.82em; font-weight:700; }
.shotref { font-size:.78em; color:var(--muted); } code { background:var(--cream); padding:1px 5px; border-radius:5px; font-size:.86em; }
.finding { border:1px solid var(--line); border-radius:14px; padding:14px 18px; margin-bottom:14px; background:#fffdf7; }
.finding-head { display:flex; flex-wrap:wrap; align-items:center; gap:8px 12px; margin-bottom:6px; } .who { font-size:.86rem; color:var(--muted); }
.evidence { font-size:.86rem; color:var(--muted); } .fix { background:#e4efe9; border-radius:8px; padding:6px 10px; }
.messenger { background:#e8e2d2; border-radius:14px; padding:14px; display:flex; flex-direction:column; gap:8px; max-height:720px; overflow-y:auto; }
.msg { background:#fff; border-radius:12px; padding:8px 12px; max-width:640px; box-shadow:0 1px 2px rgba(0,0,0,.08); }
.msg .from { font-weight:800; font-size:.86rem; } .msg .at { font-size:.76rem; color:var(--muted); margin-left:8px; } .msg p { margin:2px 0 0; }
.quotes { display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:14px; }
blockquote { margin:0; background:var(--cream); border-radius:12px; padding:14px 16px; font-family:Fraunces, Georgia, serif; font-size:1.02rem; }
blockquote p { margin:0 0 6px; } cite { font-family:Nunito, sans-serif; font-style:normal; font-size:.84rem; color:var(--muted); }
.rec { display:flex; gap:14px; margin-bottom:14px; } .rec .n { flex:none; width:32px; height:32px; border-radius:50%; background:var(--teal); color:#fff; display:grid; place-items:center; font-weight:800; }
.calendar svg { width:100%; height:auto; display:block; }
.chart { margin:8px 0 26px; } .runs .chart { margin:4px 0 14px; } .runs .chart:nth-child(2n) { margin-bottom:34px; } .chart h3 { margin-bottom:6px; } .chart svg { width:100%; height:auto; display:block; overflow:visible; }
.chart .legend { display:flex; flex-wrap:wrap; gap:6px 16px; font-size:.86rem; margin:4px 0 6px; }
.chart .legend span { display:inline-flex; align-items:center; gap:6px; } .chart .legend i { width:16px; height:3px; border-radius:2px; display:inline-block; }
.chart details { font-size:.84rem; } .chart details table { font-size:.82rem; }
.tip { position:fixed; pointer-events:none; background:var(--teal-dark); color:#fff; border-radius:8px; padding:6px 10px; font-size:.8rem; z-index:20; box-shadow:0 4px 12px rgba(0,0,0,.25); }
.tip i { display:inline-block; width:9px; height:9px; border-radius:50%; margin-right:5px; }
.zoom { position:fixed; inset:0; background:rgba(20,16,8,.85); display:grid; place-items:center; z-index:30; cursor:zoom-out; padding:16px; }
.zoom img { max-width:100%; max-height:100%; border-radius:10px; }
</style>
</head>
<body>
${body}
<script>
const IMAGES = ${JSON.stringify(imageData)};
const DATA = ${JSON.stringify(chartData)};
document.querySelectorAll("img[data-img]").forEach((img) => { img.src = IMAGES[img.dataset.img]; });
document.addEventListener("click", (e) => {
  const img = e.target.closest("figure img");
  if (img) { const z = document.createElement("div"); z.className = "zoom"; z.innerHTML = '<img src="' + img.src + '">'; z.onclick = () => z.remove(); document.body.append(z); }
});
const tip = document.createElement("div"); tip.className = "tip"; tip.hidden = true; document.body.append(tip);
const fmt = (v, unit) => (Math.abs(v) >= 1000 ? (v / 1000).toFixed(v >= 10000 ? 0 : 1) + "K" : v.toFixed(v < 10 && v % 1 !== 0 ? 1 : 0)) + unit;
const dayLabel = (d) => { const day = Math.floor(d); const m = Math.round((d - day) * 1440); return "day " + day + " " + String(Math.floor(m / 60) % 24).padStart(2, "0") + ":" + String(m % 60).padStart(2, "0"); };
/** Clean axis steps (1, 2, 2.5, 5 × 10ⁿ; whole numbers for whole-number data) and the top they reach */
function axis(max, whole) {
  if (max <= 0) return { top: 1, step: whole ? 1 : 0.25 };
  const raw = max / 4, p = Math.pow(10, Math.floor(Math.log10(raw)));
  let step = [1, 2, 2.5, 5, 10].map((s) => s * p).find((s) => s >= raw);
  if (whole) step = Math.max(1, Math.ceil(step));
  return { top: Math.ceil(max / step) * step, step };
}
function draw(el) {
  const chart = DATA.charts[el.dataset.chart];
  if (!chart || chart.series.length === 0) { el.innerHTML = "<h3>" + (chart ? chart.title : "") + "</h3><p class='muted'>No data.</p>"; return; }
  const W = 1000, H = chart.h ?? 300, L = 56, R = 96, T = 10, B = 30;
  const all = chart.series.flatMap((s) => s.points);
  const x0 = Math.min(...all.map((p) => p[0])), x1 = Math.max(...all.map((p) => p[0]), x0 + 0.1);
  const { top: yMax, step: yStep } = axis(Math.max(...all.map((p) => p[1])), all.every((p) => Number.isInteger(p[1])));
  const X = (x) => L + ((x - x0) / (x1 - x0)) * (W - L - R), Y = (y) => T + (1 - y / yMax) * (H - T - B);
  let svg = '<svg viewBox="0 0 ' + W + " " + H + '" role="img" aria-label="' + chart.title + '">';
  for (const m of DATA.sessionMarks) {
    if (m.end < x0 || m.start > x1 || (chart.player && m.player !== chart.player)) continue;
    svg += '<rect x="' + X(m.start) + '" y="' + T + '" width="' + Math.max(2, X(m.end) - X(m.start)) + '" height="' + (H - T - B) + '" fill="' + m.color + '" opacity="0.10"></rect>';
  }
  for (let v = 0; v <= yMax + 1e-9; v += yStep) {
    const y = Y(v);
    svg += '<line x1="' + L + '" x2="' + (W - R) + '" y1="' + y + '" y2="' + y + '" stroke="rgba(80,60,30,.12)" stroke-width="1"></line>';
    svg += '<text x="' + (L - 8) + '" y="' + (y + 4) + '" text-anchor="end" font-size="12" fill="#6b5c43">' + fmt(v, "") + "</text>";
  }
  const span = x1 - x0, step = span > 20 ? 7 : span > 8 ? 2 : span > 3 ? 1 : span > 1 ? 0.5 : 0.25;
  for (let d = Math.ceil(x0 / step) * step; d <= x1 + 1e-9; d += step) {
    svg += '<text x="' + X(d) + '" y="' + (H - 8) + '" text-anchor="middle" font-size="12" fill="#6b5c43">' + (step >= 1 ? "day " + Math.floor(d) : dayLabel(d)) + "</text>";
  }
  svg += '<line x1="' + L + '" x2="' + (W - R) + '" y1="' + Y(0) + '" y2="' + Y(0) + '" stroke="rgba(80,60,30,.35)" stroke-width="1"></line>';
  const ends = [];
  for (const s of chart.series) {
    const d = s.points.map((p, i) => (i ? "L" : "M") + X(p[0]).toFixed(1) + " " + Y(p[1]).toFixed(1)).join(" ");
    svg += '<path d="' + d + '" fill="none" stroke="' + s.color + '" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"></path>';
    const last = s.points[s.points.length - 1];
    svg += '<circle cx="' + X(last[0]) + '" cy="' + Y(last[1]) + '" r="4.5" fill="' + s.color + '" stroke="#fffaf0" stroke-width="2"></circle>';
    ends.push({ s, y: Y(last[1]), x: X(last[0]), v: last[1] });
  }
  ends.sort((a, b) => a.y - b.y);
  for (let i = 1; i < ends.length; i++) if (ends[i].y - ends[i - 1].y < 14) ends[i].y = ends[i - 1].y + 14;
  for (const e of ends) svg += '<text x="' + (W - R + 10) + '" y="' + (e.y + 4) + '" font-size="12" fill="#2b2418">' + e.s.name + " " + fmt(e.v, "") + "</text>";
  svg += '<line class="cross" x1="0" x2="0" y1="' + T + '" y2="' + (H - B) + '" stroke="rgba(23,60,63,.5)" stroke-width="1" visibility="hidden"></line>';
  svg += '<rect class="hit" x="' + L + '" y="' + T + '" width="' + (W - L - R) + '" height="' + (H - T - B) + '" fill="transparent"></rect></svg>';
  const legend = '<div class="legend">' + chart.series.map((s) => '<span><i style="background:' + s.color + '"></i>' + s.name + "</span>").join("") + "</div>";
  const times = [...new Set(all.map((p) => p[0]))].sort((a, b) => a - b);
  const table = "<details><summary>The numbers</summary><div class='scroll tall'><table><tr><th>Time</th>" + chart.series.map((s) => "<th>" + s.name + "</th>").join("") + "</tr>" +
    times.map((t) => "<tr><td class='nowrap'>" + dayLabel(t) + "</td>" + chart.series.map((s) => { const p = s.points.find((q) => q[0] === t); return "<td class='num'>" + (p ? fmt(p[1], "") : "") + "</td>"; }).join("") + "</tr>").join("") + "</table></div></details>";
  el.innerHTML = "<h3>" + chart.title + "</h3>" + legend + svg + table;
  const svgEl = el.querySelector("svg"), cross = el.querySelector(".cross");
  el.querySelector(".hit").addEventListener("mousemove", (ev) => {
    const box = svgEl.getBoundingClientRect(), sx = ((ev.clientX - box.left) / box.width) * W;
    const t = x0 + ((sx - L) / (W - L - R)) * (x1 - x0);
    const near = times.reduce((a, b) => (Math.abs(b - t) < Math.abs(a - t) ? b : a), times[0]);
    cross.setAttribute("x1", X(near)); cross.setAttribute("x2", X(near)); cross.setAttribute("visibility", "visible");
    tip.innerHTML = "<b>" + dayLabel(near) + "</b><br>" + chart.series.map((s) => { const p = s.points.find((q) => q[0] === near); return p ? "<i style='background:" + s.color + "'></i>" + s.name + ": " + fmt(p[1], chart.unit) : ""; }).filter(Boolean).join("<br>");
    tip.hidden = false; tip.style.left = Math.min(ev.clientX + 14, innerWidth - 200) + "px"; tip.style.top = ev.clientY + 14 + "px";
  });
  el.querySelector(".hit").addEventListener("mouseleave", () => { tip.hidden = true; cross.setAttribute("visibility", "hidden"); });
}
document.querySelectorAll(".chart").forEach(draw);
</script>
</body>
</html>`;
const file = path.join(OUT, "report.html");
writeFileSync(file, html);
console.log(`${file} (${(html.length / 1024 / 1024).toFixed(1)} MB, ${images.size} screenshots, ${sessions.length} sessions)`);
void readdirSync;

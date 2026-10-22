// The AI playtest's driver (docs/playtest-design.html §5). It keeps one browser per player open across commands and
// lets the players act the way people do: they read the screen and click, tap, type and drag for real. Start it once
// for a run (PLAYTEST_RUN=pilot node playtest/driver.mjs); the players send it commands with playtest/pt.mjs. Every
// action is logged in playtest/runs/<run>/<player>/actions.jsonl, with its screenshots in shots/.
import http from "node:http";
import { createRequire } from "node:module";
import { appendFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";

const { chromium } = createRequire(new URL("../e2e/package.json", import.meta.url))("playwright-core");
const sharp = createRequire(new URL("../art/package.json", import.meta.url))("sharp");

const BASE = process.env.PLAYTEST_URL ?? "http://localhost:5174";
const SERVER = process.env.PLAYTEST_SERVER ?? "http://localhost:5090";
const TOKEN = process.env.PLAYTEST_TOKEN ?? "playtest-local";
const PORT = Number(process.env.PLAYTEST_DRIVER_PORT ?? 5199);
const RUN = process.env.PLAYTEST_RUN ?? "pilot";
const RUN_DIR = path.join(path.dirname(new URL(import.meta.url).pathname), "runs", RUN);
mkdirSync(RUN_DIR, { recursive: true });

/** desktop or phone, and whether the player skims (Ben: only the first words of each text) */
function playerConfig(name) {
  const file = path.join(RUN_DIR, "players.json");
  const all = existsSync(file) ? JSON.parse(readFileSync(file, "utf8")) : {};
  return { device: "desktop", skim: false, ...(all[name] ?? {}) };
}

const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"], handleSIGTERM: false, handleSIGINT: false });
// Stopping the driver frees its port at once (it is restarted between sessions)
for (const signal of ["SIGTERM", "SIGINT"]) {
  process.on(signal, () => {
    setTimeout(() => process.exit(0), 3000).unref();
    browser.close().finally(() => process.exit(0));
  });
}

/** @type {Map<string, any>} the players with an open browser */
const players = new Map();
/** One command at a time per player */
const queues = new Map();

async function gameNow() {
  try {
    const res = await fetch(`${SERVER}/playtest/now`, { headers: { "X-Playtest-Token": TOKEN } });
    return (await res.json()).now;
  } catch {
    return null;
  }
}

function dirOf(name) {
  const dir = path.join(RUN_DIR, name);
  mkdirSync(path.join(dir, "shots", "small"), { recursive: true });
  return dir;
}

function lines(file) {
  return existsSync(file) ? readFileSync(file, "utf8").split("\n").filter((t) => t.trim() !== "") : [];
}

// ---------------------------------------------------------------------------------------------- opening and closing

async function open(name, session, url) {
  if (players.has(name)) {
    return `${name} is open already (session ${players.get(name).session}).`;
  }
  const config = playerConfig(name);
  const dir = dirOf(name);
  const storage = path.join(dir, "storage.json");
  const phone = config.device === "phone";
  const context = await browser.newContext({
    viewport: phone ? { width: 420, height: 860 } : { width: 1600, height: 1000 },
    hasTouch: phone,
    isMobile: phone,
    // The game's clock in the answers is UTC: the game's own times (chat, calendar) must read the same
    timezoneId: "UTC",
    // An address of its own (the server takes it in Playtest mode): all players share this machine, and one address on
    // one day makes the same-device guard price every trade between them at the hub's (run 2, unseen until after)
    extraHTTPHeaders: { "X-Playtest-Ip": `10.0.${[...name].reduce((h, c) => (h * 31 + c.charCodeAt(0)) % 250, 7) + 1}.1` },
    storageState: existsSync(storage) ? storage : undefined,
    // Copied links can be pasted into the group chat (the clipboard command)
    permissions: ["clipboard-read", "clipboard-write"],
  });
  // Many browsers share four cores and draw WebGL in software: the game draws at about 12 frames a second (the
  // screenshots look the same; animations are slower)
  await context.addInitScript(() => {
    // Each callback waits its own 80 ms (one shared "last frame" starved every callback but the first)
    const raf = window.requestAnimationFrame.bind(window);
    const caf = window.cancelAnimationFrame.bind(window);
    const live = new Map();
    let next = 1;
    window.requestAnimationFrame = (callback) => {
      const id = next++;
      const asked = performance.now();
      const step = (t) => {
        if (t - asked < 80) {
          live.set(id, raf(step));
          return;
        }
        live.delete(id);
        callback(t);
      };
      live.set(id, raf(step));
      return id;
    };
    window.cancelAnimationFrame = (id) => {
      if (live.has(id)) {
        caf(live.get(id));
        live.delete(id);
      }
    };
  });
  const page = await context.newPage();
  const player = { name, session, context, page, dir, config, errors: [], reported: 0, n: lines(path.join(dir, "actions.jsonl")).length, shots: 0 };
  page.on("pageerror", (e) => player.errors.push(`page error: ${e.message}`));
  page.on("console", (m) => {
    if (m.type() === "error" && !m.text().includes("401") && !m.text().includes("Failed to load resource")) {
      player.errors.push(`console error: ${m.text()}`);
    }
  });
  players.set(name, player);
  const target = url && url.startsWith(BASE) ? url : BASE + "/";
  await page.goto(target);
  await page.waitForTimeout(4000);
  return `Opened the game for ${name} (${phone ? "phone" : "desktop"}) at ${target}.\n\n${await look(player, {})}`;
}

async function end(player) {
  // For the report (never shown to the player): which company is this player's
  const company = await player.page.evaluate(() => {
    const c = window.tp?.appStore.getState().company;
    return c ? { id: c.id, name: c.name, color: c.color } : null;
  }).catch(() => null);
  if (company) writeFileSync(path.join(player.dir, "company.json"), JSON.stringify(company));
  await player.context.storageState({ path: path.join(player.dir, "storage.json") });
  await player.context.close();
  players.delete(player.name);
  return `Session ${player.session} ended for ${player.name} (closed the game).`;
}

// ---------------------------------------------------------------------------------------------- seeing

/** What a person reads: the page's text and controls as a tree, without the map (that is the canvas) */
async function screenText(player) {
  let text;
  try {
    text = await player.page.locator("body").ariaSnapshot({ timeout: 25000 });
  } catch {
    // A busy page (the machine is loaded): once more
    await player.page.waitForTimeout(2000);
    text = await player.page.locator("body").ariaSnapshot({ timeout: 25000 });
  }
  if (player.config.skim) {
    // Skimming: only the first words of each text
    text = text.replace(/("?)([^"\n]{70})[^"\n]{10,}("?)/g, (_, a, keep, b) => `${a}${keep}…${b}`);
  }
  return text;
}

async function shot(player, label) {
  player.shots++;
  const n = String(player.n + 1).padStart(4, "0");
  const file = `${player.session}-${n}-${(label ?? "look").replace(/[^a-z0-9-]+/gi, "-").slice(0, 40)}`;
  const full = path.join(player.dir, "shots", `${file}.png`);
  const small = path.join(player.dir, "shots", "small", `${file}.jpg`);
  await player.page.screenshot({ path: full, animations: "disabled" });
  const width = player.config.device === "phone" ? 420 : 1000;
  await sharp(full).resize({ width }).jpeg({ quality: 72 }).toFile(small);
  return { full, small };
}

async function look(player, { withShot, label }) {
  await player.page.waitForTimeout(500);
  const text = await screenText(player);
  let out = text;
  if (withShot) {
    const { small } = await shot(player, label);
    out += `\n\nScreenshot (open it with your Read tool to see the screen): ${small}`;
    player.lastShot = small;
  }
  return out;
}

/** The map around the middle of the screen as text, so the player can aim (x grows to the lower right, y to the lower left) */
async function map(player, { radius = 14, overlay = null, at = null }) {
  return player.page.evaluate(({ radius, overlay, at }) => {
    const { gameClient, appStore, layout } = window.tp;
    const engine = gameClient.engine;
    const world = gameClient.world;
    const state = appStore.getState();
    const me = state.company?.id;
    const center = at ?? engine.tileAtScreen(innerWidth / 2, innerHeight / 2);
    const cx = Math.round(center.x);
    const cy = Math.round(center.y);
    const terrainChar = { grass: ".", hills: "n", forest: "t", beach: ":", desert: ":", water: "~", mountain: "^", rock: "%", road: "+", city: "H", yard: "Y", cavern: "o" };
    const depositChars = new Map();
    const used = new Set(Object.values(terrainChar).concat([",", "=", "-", "G", "#", "+", "H", "Y", " "]));
    const depositChar = (d) => {
      if (!depositChars.has(d)) {
        const c = [...d.toLowerCase()].find((t) => /[a-z]/.test(t) && !used.has(t)) ?? "*";
        used.add(c);
        depositChars.set(d, c);
      }
      return depositChars.get(d);
    };
    const ownEstate = (x, y) => (state.company ? window.tp.rules.ownEstateAt(state.company, x, y) : undefined);
    const buildingLetters = new Map();
    const seen = new Map();
    // Your buildings get letters in turn, never one the map already uses (G the gate, H the hub, Y the Yard: Dev's replay
    // couldn't tell his moily "G" from his Yard gate "G")
    const letters = [..."ABCDEFIJKLMNOPQRSTUVWXZabcdefghijklmnopqrstuvwxyz"].filter((t) => !used.has(t));
    const letterFor = (b) => {
      if (!buildingLetters.has(b.id)) {
        buildingLetters.set(b.id, letters[buildingLetters.size % letters.length]);
      }
      return buildingLetters.get(b.id);
    };
    const rows = [];
    for (let y = cy - radius; y <= cy + radius; y++) {
      let row = "";
      for (let x = cx - radius; x <= cx + radius; x++) {
        const b = world.buildingAt(x, y);
        if (b) {
          seen.set(b.id, b);
          row += b.owner === me ? letterFor(b) : "#";
          continue;
        }
        const c = world.conductorAt(x, y);
        if (c) {
          row += c.kind === "gate" ? "G" : c.owner === me ? "=" : "-";
          continue;
        }
        const zone = layout.zoneAt(x, y);
        if (zone.kind === "corridor") { row += "+"; continue; }
        if (zone.kind === "hub") { row += "H"; continue; }
        if (zone.kind === "yard") { row += "Y"; continue; }
        const tile = world.tile(x, y);
        if (!tile) { row += " "; continue; }
        let ch = terrainChar[tile.terrain] ?? "?";
        if (tile.deposit && tile.terrain !== "water") {
          ch = depositChar(tile.deposit);
        } else if (overlay && ch === ".") {
          const v = overlay === "sun" ? tile.sun : tile.wind;
          ch = String(Math.max(0, Math.min(9, Math.round((v - 0.5) * 10))));
        }
        const own = ownEstate(x, y);
        if (own && ch === ".") {
          const parcel = layout.parcelOf(x - own.x, y - own.y);
          if (!layout.isParcelUnlocked(own.unlockedParcels, parcel)) ch = ",";
        }
        row += ch;
      }
      rows.push(`${String(y).padStart(5)} ${row}`);
    }
    // Column numbers every five tiles
    let head = "      ";
    for (let x = cx - radius; x <= cx + radius; x++) {
      head += x % 5 === 0 ? "|" : " ";
    }
    const marks = [];
    for (let x = cx - radius; x <= cx + radius; x++) {
      if (x % 5 === 0) marks.push(`${x}`);
    }
    const names = (owner) => {
      const known = [...(state.yard?.residents ?? [])].find((t) => t.id === owner || t.companyId === owner);
      return known?.name ?? `company ${owner}`;
    };
    const yours = [...seen.values()].filter((b) => b.owner === me).map((b) =>
      `  ${letterFor(b)}  ${b.type} level ${b.level}${b.constructionEndsAt ? " (being built)" : ""} at (${b.x},${b.y}), ${b.size}×${b.size}${b.network === null && b.type !== "hq" ? ", not piped" : ""}`);
    const others = [...seen.values()].filter((b) => b.owner !== me).map((b) => `  #  ${b.type} of ${names(b.owner)} at (${b.x},${b.y})`);
    const deposits = [...depositChars.entries()].map(([d, c]) => `${c} ${d}`).join(", ");
    return [
      `The map around (${cx},${cy}), ${2 * radius + 1}×${2 * radius + 1} tiles. Row = y, column = x (marks at x = ${marks.join(", ")}).`,
      "On the screen x grows to the lower right and y to the lower left (the map is drawn turned 45°).",
      head,
      ...rows,
      "",
      `Legend: . free land  , your land not bought yet  t forest  n hills  : beach/desert  ~ water  ^ mountain  % rock  + corridor (no building)  Y Yard  H hub  = your pipe  - someone's pipe  G Yard gate  # someone's building${deposits ? `  deposits: ${deposits}` : ""}${overlay ? `  0–9 ${overlay} (5 = normal, 9 = best)` : ""}`,
      yours.length ? `Your buildings here:\n${yours.join("\n")}` : "None of your buildings are in this part of the map.",
      others.length ? `Others' buildings here:\n${others.slice(0, 20).join("\n")}` : "",
    ].join("\n");
  }, { radius, overlay, at });
}

// ---------------------------------------------------------------------------------------------- acting

/** Where tiles are on the screen; the camera moves to them first when one is off screen or under a panel */
async function tilePoints(player, tiles) {
  const where = () => player.page.evaluate((tiles) => {
    const canvas = document.querySelector("canvas");
    const box = canvas.getBoundingClientRect();
    return tiles.map(({ x, y }) => {
      const s = window.tp.gameClient.engine.tileToScreen(x, y);
      const px = box.left + s.x;
      const py = box.top + s.y;
      const hit = document.elementFromPoint(px, py);
      return { x: px, y: py, free: hit === canvas && px > 10 && py > 10 && px < innerWidth - 10 && py < innerHeight - 10 };
    });
  }, tiles);
  let points = await where();
  let moved = false;
  if (points.some((t) => !t.free)) {
    const mx = tiles.reduce((s, t) => s + t.x, 0) / tiles.length;
    const my = tiles.reduce((s, t) => s + t.y, 0) / tiles.length;
    await player.page.evaluate(({ x, y }) => window.tp.gameClient.engine.lookAt(x, y), { x: mx, y: my });
    await player.page.waitForTimeout(600);
    points = await where();
    moved = true;
  }
  return { points, moved };
}

async function tapAt(player, p) {
  if (player.config.device === "phone") {
    await player.page.touchscreen.tap(p.x, p.y);
  } else {
    await player.page.mouse.click(p.x, p.y);
  }
}

// A person's tap never times out: the limit is for a stuck page, not for a busy machine (five browsers on four cores
// made Ben's phone taps miss 5 s in run 2's first row)
const TAP_TIMEOUT = 15000;

/**
 * A click or tap as a person makes it: on the element, or, when something lies over it (a panel's see-through
 * backdrop), on whatever is on top at that point (Cleo's run 2 afternoon: a dozen clicks waited on a backdrop that one
 * click would have closed). False when it hit something else.
 */
async function press(player, item) {
  const phone = player.config.device === "phone";
  try {
    if (phone) await item.tap({ timeout: 2500, trial: true });
    else await item.click({ timeout: 2500, trial: true });
  } catch {
    // Slow, or covered? What is on top at its middle decides (a busy machine makes the trial slow too)
    const box = await item.boundingBox();
    const covered = box !== null && (await item.evaluate((el, p) => {
      const top = document.elementFromPoint(p.x, p.y);
      return top !== null && !el.contains(top);
    }, { x: box.x + box.width / 2, y: box.y + box.height / 2 }));
    if (covered) {
      await tapAt(player, { x: box.x + box.width / 2, y: box.y + box.height / 2 });
      await player.page.waitForTimeout(400);
      return false;
    }
  }
  if (phone) await item.tap({ timeout: TAP_TIMEOUT });
  else await item.click({ timeout: TAP_TIMEOUT });
  return true;
}

const ROLES = ["button", "link", "tab", "radio", "checkbox", "menuitem", "option", "switch", "combobox", "textbox"];

async function clickLabel(player, label, nth = 1) {
  const page = player.page;
  for (const exact of [true, false]) {
    for (const role of ROLES) {
      const found = page.getByRole(role, { name: label, exact });
      const count = await found.count();
      let seen = 0;
      for (let i = 0; i < count; i++) {
        const item = found.nth(i);
        if ((await item.isVisible()) && ++seen === nth) {
          if (!(await press(player, item))) return `Your click on the ${role} "${label}" landed on something lying over it.`;
          return `Clicked the ${role} "${label}".`;
        }
      }
    }
  }
  const text = page.getByText(label, { exact: false });
  if ((await text.count()) > 0 && (await text.first().isVisible())) {
    if (!(await press(player, text.first()))) return `Your click on "${label}" landed on something lying over it.`;
    return `Clicked the text "${label}".`;
  }
  return `Nothing called "${label}" is on the screen.`;
}

async function fill(player, label, value, nth = 1) {
  const page = player.page;
  for (const locator of [page.getByLabel(label, { exact: false }), page.getByPlaceholder(label, { exact: false }), page.getByRole("textbox", { name: label })]) {
    // The nth visible field of that name (Ana's second seat name went into the first field in run 2)
    const visible = [];
    for (let i = 0; i < (await locator.count()); i++) {
      if (await locator.nth(i).isVisible()) visible.push(locator.nth(i));
    }
    const field = visible[nth - 1];
    if (field === undefined) {
      if (visible.length > 0) return `There are only ${visible.length} fields called "${label}" on the screen.`;
      continue;
    }
    // A drop-down: choose the option with that text (Dev's replay couldn't pick a good on the Plan board)
    if ((await field.evaluate((t) => t.tagName)) === "SELECT") {
      const options = await field.locator("option").allTextContents();
      const option = options.find((t) => t.trim().toLowerCase() === value.trim().toLowerCase()) ?? options.find((t) => t.toLowerCase().includes(value.trim().toLowerCase()));
      if (option === undefined) return `"${label}" has no option "${value}". It offers: ${options.join(", ")}.`;
      await field.selectOption({ label: option });
      return `Chose "${option.trim()}" in "${label}".`;
    }
    await field.fill(value);
    return `Typed "${value}" into "${label}"${nth > 1 ? ` (the ${ordinal(nth)})` : ""}.`;
  }
  return `No field called "${label}" is on the screen.`;
}

function ordinal(n) {
  return n === 2 ? "2nd" : n === 3 ? "3rd" : `${n}th`;
}

async function chat(player, { channel, text }) {
  const page = player.page;
  if ((await page.locator(".chat-panel").count()) === 0) {
    const toggle = page.locator(".chat-toggle").first();
    if ((await toggle.count()) === 0 || !(await toggle.isVisible())) return "The chat's button isn't on the screen now (close the panel first).";
    if (!(await press(player, toggle))) return "You can't open the chat now: something lies over its button.";
    await page.waitForTimeout(800);
  }
  if (channel) {
    const button = page.locator(".chat-channel", { hasText: channel });
    if ((await button.count()) === 0) {
      return `No chat channel called "${channel}". ${await page.locator(".chat-panel").ariaSnapshot()}`;
    }
    await button.first().click();
    await page.waitForTimeout(800);
  }
  if (text) {
    await page.locator(".chat-form input").fill(text);
    await page.locator(".chat-form input").press("Enter");
    await page.waitForTimeout(800);
  }
  return page.locator(".chat-panel").ariaSnapshot();
}

/** The friends' group chat outside the game (like a messenger group): everyone reads all of it */
async function group(player, text) {
  // Mia knows nobody who plays: she has no friends' group
  if (player.name.startsWith("mia")) return "You have no group chat for this game: nobody you know plays it.";
  const file = path.join(RUN_DIR, "group.jsonl");
  if (text) {
    appendFileSync(file, JSON.stringify({ at: new Date().toISOString(), game: await gameNow(), from: player.name, text }) + "\n");
  }
  const all = lines(file).map((t) => JSON.parse(t));
  const readFile = path.join(player.dir, "group-read.txt");
  const read = existsSync(readFile) ? Number(readFileSync(readFile, "utf8")) : 0;
  writeFileSync(readFile, String(all.length));
  if (all.length === 0) return "The group chat is empty.";
  // What's new, with the few messages before it (like scrolling up a little in a messenger)
  const from = Math.max(0, Math.min(read, all.length - 1) - 4);
  const older = from > 0 ? `(${from} older messages above)\n` : "";
  return older + all.slice(from).map((m, i) => {
    const index = from + i;
    return `${index >= read ? "NEW " : "    "}[${(m.game ?? m.at).slice(0, 16).replace("T", " ")}] ${m.from}: ${m.text}`;
  }).join("\n");
}

// ---------------------------------------------------------------------------------------------- commands

const HELP = `Commands (node playtest/pt.mjs <you> <command> …):
  open <session> [url]        open the game (your browser keeps your login between sessions)
  look [--shot] [label]       what is on the screen, as text; --shot also takes a screenshot (open it with Read)
  shot <label>                a screenshot of a key moment (kept for the report)
  map [--sun|--wind] [r] [x y]  the map around the middle of the screen (or around x y), with tile numbers
  click "<label>" [--nth 2]   click a button, tab, link or text by what it says (--nth: the 2nd, 3rd… of that name)
  tile <x> <y>                click (or tap) the map on that tile
  rclick <x> <y>              right-click the map on that tile
  hover <x> <y>               move the mouse over that tile (to see what the game shows there)
  drag <x1> <y1> <x2> <y2>    drag on the map from one tile to another (pipes, areas)
  fill "<field>" "<text>" [--nth 2]  type into the field with that label or placeholder (or choose that option of a drop-down)
  type "<text>" / key <Key>   type with the keyboard / press a key (Escape, Enter, b, p, …)
  goto <url>                  open a link someone sent you (only links to the game)
  wait <seconds>              wait (up to 120 s) and look again
  chat ["<text>"] [--channel <name>]  the game's chat: read it, or write in it
  group ["<text>"]            the friends' group chat outside the game: read it, or write in it
  clipboard                   what you copied (to paste a link into the group chat)
  note "<thought>"            a thought for your diary (stuck, surprised, pleased)
  end                         close the game (always do this at the end of a session)`;

function parse(args) {
  const flags = {};
  const rest = [];
  for (let i = 0; i < args.length; i++) {
    if (args[i].startsWith("--")) {
      const key = args[i].slice(2);
      if (key === "channel" || key === "nth") { flags[key] = args[++i]; } else { flags[key] = true; }
    } else {
      rest.push(args[i]);
    }
  }
  return { flags, rest };
}

async function run(name, cmd, args) {
  const { flags, rest } = parse(args);
  if (cmd === "help") return HELP;
  if (cmd === "open") return open(name, rest[0] ?? "session", rest[1]);
  const player = players.get(name);
  if (!player) return `${name} has no game open: start with "open <session>".`;
  const num = (i) => Number(rest[i]);
  switch (cmd) {
    case "look": return look(player, { withShot: flags.shot, label: rest[0] });
    case "shot": {
      const { small } = await shot(player, rest[0] ?? "moment");
      return `Screenshot kept for the report: ${small}`;
    }
    case "map": {
      const radius = rest.length === 1 || rest.length === 3 ? Number(rest[0]) : 14;
      const at = rest.length >= 2 ? { x: Number(rest[rest.length - 2]), y: Number(rest[rest.length - 1]) } : null;
      return map(player, { radius: Math.min(Math.max(radius || 14, 4), 24), overlay: flags.sun ? "sun" : flags.wind ? "wind" : null, at });
    }
    case "click": return (await clickLabel(player, rest.join(" "), Number(flags.nth ?? 1))) + "\n\n" + (await look(player, {}));
    case "tile":
    case "rclick": {
      const { points: [p], moved } = await tilePoints(player, [{ x: num(0), y: num(1) }]);
      if (!p.free) return `Tile (${num(0)},${num(1)}) is under a panel or off your screen even after moving the view: close a panel or move the map first.\n\n${await look(player, {})}`;
      if (cmd === "rclick") await player.page.mouse.click(p.x, p.y, { button: "right" });
      else await tapAt(player, p);
      return `${moved ? "(The view moved to the tile first.) " : ""}${cmd === "rclick" ? "Right-clicked" : "Clicked"} tile (${num(0)},${num(1)}).\n\n${await look(player, {})}`;
    }
    case "hover": {
      const { points: [p] } = await tilePoints(player, [{ x: num(0), y: num(1) }]);
      await player.page.mouse.move(p.x, p.y);
      return `Mouse over tile (${num(0)},${num(1)}).\n\n${await look(player, {})}`;
    }
    case "drag": {
      const { points: [a, b], moved } = await tilePoints(player, [{ x: num(0), y: num(1) }, { x: num(2), y: num(3) }]);
      if (!a.free || !b.free) {
        return `Tile (${!a.free ? `${num(0)},${num(1)}` : `${num(2)},${num(3)}`}) is under a panel or off your screen even after moving the view: close a panel, zoom out or move the map first.\n\n${await look(player, {})}`;
      }
      if (player.config.device === "phone") {
        // A finger, as a phone sends it
        player.cdp ??= await player.context.newCDPSession(player.page);
        const touch = (type, x, y) => player.cdp.send("Input.dispatchTouchEvent", { type, touchPoints: type === "touchEnd" ? [] : [{ x, y }] });
        await touch("touchStart", a.x, a.y);
        await player.page.waitForTimeout(120);
        for (let i = 1; i <= 12; i++) {
          await touch("touchMove", a.x + ((b.x - a.x) * i) / 12, a.y + ((b.y - a.y) * i) / 12);
          await player.page.waitForTimeout(25);
        }
        await touch("touchEnd");
      } else {
        await player.page.mouse.move(a.x, a.y);
        await player.page.mouse.down();
        for (let i = 1; i <= 12; i++) {
          await player.page.mouse.move(a.x + ((b.x - a.x) * i) / 12, a.y + ((b.y - a.y) * i) / 12);
          await player.page.waitForTimeout(25);
        }
        await player.page.mouse.up();
      }
      return `${moved ? "(The view moved.) " : ""}Dragged from (${num(0)},${num(1)}) to (${num(2)},${num(3)}).\n\n${await look(player, {})}`;
    }
    case "fill": return (await fill(player, rest[0], rest.slice(1).join(" "), Number(flags.nth ?? 1))) + "\n\n" + (await look(player, {}));
    case "type":
      await player.page.keyboard.type(rest.join(" "));
      return `Typed "${rest.join(" ")}".`;
    case "key":
      await player.page.keyboard.press(rest[0]);
      return `Pressed ${rest[0]}.\n\n${await look(player, {})}`;
    case "goto":
      if (!rest[0]?.startsWith(BASE)) return `Only links to the game (${BASE}) open here.`;
      await player.page.goto(rest[0]);
      await player.page.waitForTimeout(3000);
      return look(player, {});
    case "wait":
      await player.page.waitForTimeout(Math.min(Math.max(num(0) || 5, 1), 120) * 1000);
      return look(player, {});
    case "chat": return chat(player, { channel: flags.channel, text: rest.join(" ") });
    case "group": return group(player, rest.join(" "));
    case "clipboard": {
      const text = await player.page.evaluate(() => navigator.clipboard.readText().catch(() => ""));
      return text ? `Your clipboard holds: ${text}` : "Your clipboard is empty.";
    }
    case "note":
      appendFileSync(path.join(player.dir, "notes.jsonl"), JSON.stringify({ session: player.session, n: player.n, at: new Date().toISOString(), game: await gameNow(), text: rest.join(" "), shot: player.lastShot ?? null }) + "\n");
      return "Noted.";
    case "end": return end(player);
    default: return `Unknown command "${cmd}".\n\n${HELP}`;
  }
}

async function handle(name, cmd, args) {
  const started = Date.now();
  const player = players.get(name);
  let text;
  let ok = true;
  try {
    text = await run(name, cmd, args);
  } catch (e) {
    ok = false;
    text = `That didn't work: ${String(e.message ?? e).split("\n")[0]}`;
  }
  const after = players.get(name) ?? player;
  if (after) {
    after.n++;
    const fresh = after.errors.slice(after.reported);
    after.reported = after.errors.length;
    if (fresh.length) text += `\n\n⚠ The page reported ${fresh.length === 1 ? "an error" : "errors"}: ${fresh.slice(0, 3).join(" | ")}`;
    const game = await gameNow();
    text = `[#${after.n} · ${game ? new Date(game).toUTCString().slice(0, 22) : "?"}] ${text}`;
    appendFileSync(path.join(after.dir, "actions.jsonl"), JSON.stringify({
      n: after.n, session: after.session, at: new Date().toISOString(), game, cmd, args, ok,
      ms: Date.now() - started, shot: after.lastShot ?? null, errors: fresh, out: text.slice(0, 4000),
    }) + "\n");
    after.lastShot = null;
    // The login is kept after every command, so a restarted driver loses nobody's (guest) account
    if (players.has(after.name)) await after.context.storageState({ path: path.join(after.dir, "storage.json") }).catch(() => {});
  }
  return { ok, text };
}

const server = http.createServer((req, res) => {
  let body = "";
  req.on("data", (c) => (body += c));
  req.on("end", () => {
    const { player, cmd, args = [] } = JSON.parse(body || "{}");
    if (req.url === "/players") {
      res.end(JSON.stringify([...players.keys()]));
      return;
    }
    if (req.url === "/closeall") {
      Promise.all([...players.values()].map(end)).then((r) => res.end(JSON.stringify(r)));
      return;
    }
    const previous = queues.get(player) ?? Promise.resolve();
    const next = previous.then(() => handle(player, cmd, args));
    queues.set(player, next.catch(() => {}));
    next.then((r) => res.end(JSON.stringify(r)), (e) => res.end(JSON.stringify({ ok: false, text: String(e) })));
  });
});
server.listen(PORT, "127.0.0.1", () => console.log(`playtest driver for run "${RUN}" on :${PORT}, game ${BASE}`));

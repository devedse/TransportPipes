// The AI playtest's director (docs/playtest-design.html §2): the playtest server, its clock and database, and the
// numbers the report draws. PLAYTEST_RUN names the run (the same as the driver's).
//   node playtest/director.mjs reset                 a fresh world: stops the server, empties its database and clock
//   node playtest/director.mjs server start|stop     the playtest server (:5090, from playtest/bin)
//   node playtest/director.mjs now                   the game's time and the clock's rate
//   node playtest/director.mjs advance <hours> [step]  moves the clock on in steps (default 6 h), sampling after each
//   node playtest/director.mjs pause | resume [rate]  stops the clock between sessions (0.01), runs it during them
//   node playtest/director.mjs sample [label]        one sample of every player's numbers (metrics.jsonl)
//   node playtest/director.mjs snapshot <name>       the world saved and its database copied (to replay from)
//   node playtest/director.mjs restore <name>        back to a snapshot (stops and starts the server)
import { execSync, spawn } from "node:child_process";
import { appendFileSync, mkdirSync, openSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import path from "node:path";

const ROOT = path.dirname(new URL(import.meta.url).pathname);
const RUN = process.env.PLAYTEST_RUN ?? "pilot";
const RUN_DIR = path.join(ROOT, "runs", RUN);
const SERVER_DIR = path.join(RUN_DIR, "server");
const SNAPSHOTS = path.join(RUN_DIR, "snapshots");
const SERVER = process.env.PLAYTEST_SERVER ?? "http://localhost:5090";
const TOKEN = process.env.PLAYTEST_TOKEN ?? "playtest-local";
const DB = "transportpipes_playtest";
mkdirSync(SERVER_DIR, { recursive: true });

const sql = (statement) => execSync(`sg docker -c "docker exec transportpipes-dev-db psql -U transportpipes -d transportpipes -c '${statement}'"`, { stdio: "pipe" }).toString();

async function call(method, route, body) {
  const res = await fetch(`${SERVER}/playtest/${route}`, {
    method,
    headers: { "X-Playtest-Token": TOKEN, "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${route}: ${res.status}`);
  return res.json();
}

async function running() {
  try {
    return (await fetch(`${SERVER}/health`)).ok;
  } catch {
    return false;
  }
}

async function start() {
  if (await running()) return "The playtest server is running already.";
  const log = openSync(path.join(SERVER_DIR, "server.log"), "a");
  const child = spawn("dotnet", ["TransportPipes.Server.dll", "--urls", SERVER, `--TransportPipes:PlaytestDataDir=${SERVER_DIR}`], {
    cwd: path.join(ROOT, "bin"),
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Playtest", DOTNET_ROOT: `${process.env.HOME}/.dotnet`, PATH: `${process.env.HOME}/.dotnet:${process.env.PATH}` },
    detached: true,
    stdio: ["ignore", log, log],
  });
  child.unref();
  for (let i = 0; i < 90; i++) {
    if (await running()) return `The playtest server is up (run "${RUN}").`;
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error("The playtest server didn't start: see " + path.join(SERVER_DIR, "server.log"));
}

async function stop() {
  try {
    execSync(`pkill -f "[T]ransportPipes.Server.dll --urls ${SERVER}"`);
  } catch {
    return "The playtest server wasn't running.";
  }
  for (let i = 0; i < 60 && (await running()); i++) await new Promise((r) => setTimeout(r, 1000));
  await new Promise((r) => setTimeout(r, 1500));
  return "The playtest server stopped (it saved the world).";
}

async function sample(label) {
  const data = await call("GET", "companies");
  appendFileSync(path.join(RUN_DIR, "metrics.jsonl"), JSON.stringify({ label, ...data }) + "\n");
  return data;
}

function summary(data) {
  return `${data.now.slice(0, 16)} · ${data.players.map((p) => `${p.name} L${p.level} ${Math.round(p.gold).toLocaleString("en")} g ${p.goldPerMinute.toFixed(0)}/min${p.asleep ? " (asleep)" : ""}`).join(" · ")}`;
}

const [cmd, ...args] = process.argv.slice(2);
switch (cmd) {
  case "reset": {
    console.log(await stop());
    sql(`DROP DATABASE IF EXISTS ${DB} WITH (FORCE)`);
    sql(`CREATE DATABASE ${DB} OWNER transportpipes`);
    for (const file of ["clock.json", "events.jsonl"]) rmSync(path.join(SERVER_DIR, file), { force: true });
    console.log(`A fresh world for run "${RUN}" (start the server).`);
    break;
  }
  case "server":
    console.log(args[0] === "stop" ? await stop() : await start());
    break;
  case "now":
    console.log(await call("GET", "now"));
    break;
  case "pause":
    console.log(await call("POST", "rate", { rate: 0.01 }));
    break;
  case "resume":
    console.log(await call("POST", "rate", { rate: Number(args[0] ?? 1) }));
    break;
  case "advance": {
    let left = Number(args[0]);
    const step = Number(args[1] ?? 6);
    while (left > 1e-6) {
      const hours = Math.min(step, left);
      await call("POST", "advance", { hours });
      left -= hours;
      const data = await sample(`advance`);
      console.log(summary(data));
    }
    break;
  }
  case "sample":
    console.log(summary(await sample(args[0] ?? "sample")));
    break;
  case "snapshot": {
    const name = args[0];
    mkdirSync(SNAPSHOTS, { recursive: true });
    await call("POST", "save");
    const { now } = await call("GET", "now");
    execSync(`sg docker -c "docker exec transportpipes-dev-db pg_dump -U transportpipes -Fc ${DB}" > "${path.join(SNAPSHOTS, name + ".dump")}"`);
    writeFileSync(path.join(SNAPSHOTS, name + ".json"), JSON.stringify({ now }));
    console.log(`Snapshot "${name}" at ${now}.`);
    break;
  }
  case "restore": {
    const name = args[0];
    const { now } = JSON.parse(readFileSync(path.join(SNAPSHOTS, name + ".json"), "utf8"));
    console.log(await stop());
    sql(`DROP DATABASE IF EXISTS ${DB} WITH (FORCE)`);
    sql(`CREATE DATABASE ${DB} OWNER transportpipes`);
    execSync(`sg docker -c "docker exec -i transportpipes-dev-db pg_restore -U transportpipes -d ${DB}" < "${path.join(SNAPSHOTS, name + ".dump")}"`);
    // The clock goes on from the snapshot's moment, paused
    writeFileSync(path.join(SERVER_DIR, "clock.json"), JSON.stringify({ Game: now, Real: new Date().toISOString(), Rate: 0.01 }));
    console.log(await start());
    console.log(`Back at "${name}" (${now}).`);
    break;
  }
  default:
    console.log(readFileSync(new URL(import.meta.url), "utf8").split("\n").filter((t) => t.startsWith("//")).join("\n"));
}

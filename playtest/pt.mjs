// A player's command to the playtest's driver: node playtest/pt.mjs <you> <command> [arguments…] (help lists them)
const [player, cmd = "help", ...args] = process.argv.slice(2);
if (!player) {
  console.log("Usage: node playtest/pt.mjs <you> <command> [arguments…]");
  process.exit(1);
}
const port = process.env.PLAYTEST_DRIVER_PORT ?? 5199;
try {
  const res = await fetch(`http://127.0.0.1:${port}/cmd`, { method: "POST", body: JSON.stringify({ player, cmd, args }) });
  const out = await res.json();
  console.log(out.text);
  process.exitCode = out.ok ? 0 : 1;
} catch (e) {
  console.log(`The driver is not running (${e.message}).`);
  process.exitCode = 2;
}

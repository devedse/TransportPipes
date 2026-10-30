// The start on the map (1.4 §6): a new player sees the real map with the free plots glowing, Pick for me chooses one,
// its card says what the land has and lacks and who lives at the Yard, and Play settles there. A phone too.
// node start-map.mjs shots/start-map [phone]
import { start } from "./lib.mjs";

const out = process.argv[2] ?? "shots/start-map";
const phone = process.argv[3] === "phone";
const p = await start(out, phone ? { width: 390, height: 844, touch: true } : {});
const user = `sm${Date.now() % 100000}`;
for (let i = 0; i < 40; i++) {
  try {
    await p.page.goto(`${process.env.BASE_URL ?? "http://localhost:5173"}/`);
    await p.page.waitForSelector(".auth-panel", { timeout: 15000 });
    break;
  } catch {
    await p.page.waitForTimeout(1000);
  }
}
await p.page.click("text=New player");
await p.page.fill("input[autocomplete=username]", user);
await p.page.fill("input[type=password]", "secret123");
await p.page.click("button[type=submit]");
await p.page.waitForSelector(".picker-top", { timeout: 30000 });
await p.page.waitForTimeout(6000);
await p.shot("1-map");
// The whole town, zoomed out: the Yards with their banners, the free plots glowing, the gates and the outskirts
await p.page.evaluate(() => tp.gameClient.lookAtTown(0, 0));
await p.page.waitForTimeout(5000);
await p.shot("1-town");
await p.page.click("text=Pick for me");
await p.page.waitForSelector(".plot-card .start-form", { timeout: 30000 });
await p.page.waitForTimeout(6000);
await p.shot("2-card");
await p.page.fill(".start-form input[minlength='3']", `${user} Works`);
await p.page.click(".start-form button[type=submit]");
await p.page.waitForSelector(".top-bar", { timeout: 30000 });
await p.page.waitForTimeout(6000);
await p.shot("3-playing");
console.log("errors:", p.errors);
await p.browser.close();

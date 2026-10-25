// How long a new player waits on a phone network: node load.mjs [fast4g|slow4g] [seconds spent typing on the title]
// (BASE_URL, default :5080: the server with a production build, see README.md)
import { chromium } from "playwright-core";
const profile = process.argv[2] ?? "fast4g";
const typing = Number(process.argv[3] ?? 0);
const NETWORKS = {
  fast4g: { downloadThroughput: (9e6 / 8) | 0, uploadThroughput: (1.5e6 / 8) | 0, latency: 60 },
  slow4g: { downloadThroughput: (1.6e6 / 8) | 0, uploadThroughput: (0.75e6 / 8) | 0, latency: 150 },
};
const base = process.env.BASE_URL ?? "http://localhost:5080";
const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
const context = await browser.newContext({ viewport: { width: 412, height: 860 }, isMobile: true, hasTouch: true, deviceScaleFactor: 2 });
const page = await context.newPage();
const cdp = await context.newCDPSession(page);
await cdp.send("Network.enable");
await cdp.send("Network.emulateNetworkConditions", { offline: false, ...NETWORKS[profile] });
let bytes = 0, requests = 0;
cdp.on("Network.loadingFinished", (e) => { bytes += e.encodedDataLength; requests++; });
const t = () => ((Date.now() - t0) / 1000).toFixed(1) + " s";
const mb = () => (bytes / 1e6).toFixed(1) + " MB";
let t0 = Date.now();
await page.goto(base + "/");
await page.waitForSelector(".auth-panel", { timeout: 120000 });
console.log(`${profile}: title screen ${t()} (${mb()}, ${requests} requests)`);
const user = `l${Date.now() % 100000}`;
await page.waitForTimeout(typing * 1000);
await page.click("text=New player");
await page.fill("input[autocomplete=username]", user);
await page.fill("input[type=password]", "secret123");
t0 = Date.now(); bytes = 0; requests = 0;
await page.click("button[type=submit]");
await page.waitForSelector(".picker-top", { timeout: 120000 });
console.log(`  signed up → the start map ${t()}`);
await page.click("text=Pick for me");
await page.waitForSelector(".plot-card .start-form", { timeout: 60000 });
await page.fill(".start-form input[minlength='3']", `${user} Works`);
t0 = Date.now();
await page.click(".start-form button[type=submit]");
// Loady covers the map until it has been drawn
if (await page.waitForSelector(".map-loading", { timeout: 5000 }).catch(() => null)) {
  await page.screenshot({ path: `shots/load-${profile}-loading.png` });
}
await page.waitForSelector("body[data-ready='1']", { timeout: 300000 });
console.log(`  founded → map ready ${t()} (${mb()}, ${requests} requests since sign-up)`);
await page.screenshot({ path: `shots/load-${profile}-ready.png` });
await page.waitForTimeout(6000);
await page.screenshot({ path: `shots/load-${profile}-6s.png` });
// A returning player: the same browser comes back
bytes = 0; requests = 0; t0 = Date.now();
await page.reload();
await page.waitForSelector("body[data-ready='1']", { timeout: 300000 });
console.log(`  coming back → map ready ${t()} (${mb()}, ${requests} requests)`);
await browser.close();

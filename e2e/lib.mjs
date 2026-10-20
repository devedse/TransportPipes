// Shared helpers for the e2e screenshot scripts (see README.md). The game runs at http://localhost:5173 (BASE_URL).
import { chromium } from "playwright-core";
import { mkdirSync } from "node:fs";

export async function start(out, { width = 1600, height = 1000 } = {}) {
  mkdirSync(out, { recursive: true });
  // A Chromium from Playwright's cache (npx playwright install chromium), or CHROMIUM=/path/to/chrome
  const executablePath = process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`;
  const browser = await chromium.launch({ executablePath, args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
  const page = await browser.newPage({ viewport: { width, height } });
  const errors = [];
  page.on("pageerror", (e) => errors.push(e.message));
  page.on("console", (m) => { if (m.type() === "error" && !m.text().includes("401")) errors.push(m.text()); });
  const shot = async (name) => { await page.screenshot({ path: `${out}/${name}.png`, animations: "disabled" }); console.log("shot", name); };
  const cheat = (what) => page.evaluate((w) => tp.gameClient.connection.cheat(w).then((r) => r.success || r.error), what);
  return { browser, page, errors, shot, cheat };
}

export async function newPlayer(page, prefix = "p") {
  const user = `${prefix}${Date.now() % 100000}`;
  for (let i = 0; i < 40; i++) { try { await page.goto(`${process.env.BASE_URL ?? "http://localhost:5173"}/`); await page.waitForSelector(".auth-panel", { timeout: 15000 }); break; } catch { await page.waitForTimeout(1000); } }
  await page.click("text=New player");
  await page.fill("input[autocomplete=username]", user);
  await page.fill("input[type=password]", "secret123");
  await page.click("button[type=submit]");
  await page.waitForSelector(".found-panel", { timeout: 30000 });
  await page.fill(".found-panel input", `${user} Works`);
  await page.click(".found-panel button[type=submit]");
  await page.waitForSelector(".top-bar", { timeout: 30000 });
  await page.waitForTimeout(3000);
  return user;
}

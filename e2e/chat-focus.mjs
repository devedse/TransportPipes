// Typing on in the chat after Enter (playtest: "if you type in the chat and press enter and then continue typing the
// focus on the chat box is gone"): the box keeps the focus, the next message goes in it (and no map key fires), and
// what is typed while the first one is on its way stays.   node chat-focus.mjs <outdir>
import { start, newPlayer } from "./lib.mjs";
const [out = "shots/chat-focus"] = process.argv.slice(2);
const { browser, page, errors, shot } = await start(out, { width: 1400, height: 1000 });
let failures = 0;
const check = (ok, text, detail = "") => { console.log(`${ok ? "ok  " : "FAIL"} ${text}${ok ? "" : ` ${detail}`}`); if (!ok) failures++; };

await newPlayer(page, "cf");
for (let i = 0; i < 3 && (await page.locator("[role=dialog]:visible").count()) > 0; i++) { await page.keyboard.press("Escape"); await page.waitForTimeout(400); }
await page.click(".chat-toggle");
await page.waitForSelector(".chat-form input", { timeout: 15000 });
await page.waitForTimeout(1500);
const box = page.locator(".chat-form input");
await box.click();
await page.keyboard.type("hello there");
await page.keyboard.press("Enter");
// Straight on, as one types: "pipe" would pick the Pipe tool if the keys went to the map
await page.keyboard.type("pipe", { delay: 30 });
await page.waitForTimeout(2000);
const state = await page.evaluate(() => ({
  focused: document.activeElement?.matches(".chat-form input") ?? false,
  value: document.querySelector(".chat-form input")?.value ?? null,
  tool: tp.appStore.getState().tool.kind,
}));
console.log(JSON.stringify(state));
check(state.focused, "the box keeps the focus after Enter");
check(state.value === "pipe", "what was typed on is in the box", JSON.stringify(state.value));
check(state.tool === "select", "no map key fired", state.tool);
const sent = await page.locator(".chat-list").innerText();
check(/hello there/.test(sent), "the first message was sent");
await page.keyboard.press("Enter");
await page.waitForTimeout(1500);
check(/pipe/.test(await page.locator(".chat-list").innerText()), "and the second one too");
await shot("1-two-messages");
check(errors.length === 0, "browser errors: none", errors.join("\n"));
await browser.close();
console.log(failures === 0 ? "PASS" : `${failures} FAILED`);
process.exit(failures === 0 ? 0 : 1);

// Foto dei mockup della Reportistica (task #408): Chromium senza finestra pilotato via DevTools (come portale-ux/foto.mjs).
// Uso: node foto.mjs   (scrive in ../foto/: computer 1440 px e telefono 390 px)
import path from "node:path";
import fs from "node:fs";
import { spawn } from "node:child_process";

const qui = path.dirname(new URL(import.meta.url).pathname);
const uscita = path.join(qui, "..", "foto");
fs.mkdirSync(uscita, { recursive: true });
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--disable-gpu", "--remote-debugging-port=9336",
   `--user-data-dir=${tmp}/foto-reportistica`, "--no-first-run", "--lang=it-IT", "about:blank"], { stdio: "ignore" });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9336/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

const pagine = ["1-menu-report", "2-backend-report", "3-report-cliente", "4-flussi-meteo"];
for (const nome of pagine) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  await s("Emulation.setDeviceMetricsOverride", { width: 1440, height: 900, deviceScaleFactor: 1, mobile: false });
  await s("Page.navigate", { url: "file://" + path.join(qui, nome + ".html") });
  await attesa(1500);
  const { contentSize } = await s("Page.getLayoutMetrics");
  const altezza = Math.ceil(contentSize.height);
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: 1440, height: altezza, scale: 1 } });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(data, "base64"));
  console.log(nome, altezza);
  await cdp("Target.closeTarget", { targetId });
}
ws.close();
chrome.kill();
process.exit(0);

// Foto della tappa R0 (task #413): menù «Report» e pagina /report. Chromium senza finestra via DevTools (come ../mockup/foto.mjs).
// Prima: Web su http://127.0.0.1:5810 con Accesso:Abilitato=false e l'Api senza 4Grapes (vedi giro.sh).
// Uso: node foto.mjs   (scrive qui: computer 1440 px e telefono 390 px)
import path from "node:path";
import fs from "node:fs";
import { spawn } from "node:child_process";

const qui = path.dirname(new URL(import.meta.url).pathname);
const base = process.env.WEB ?? "http://127.0.0.1:5810";
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--disable-gpu", "--remote-debugging-port=9337",
   `--user-data-dir=${tmp}/foto-r0`, "--no-first-run", "--lang=it-IT", "about:blank"], { stdio: "ignore" });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9337/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

// nome, indirizzo, larghezza, script da eseguire prima della foto
const foto = [
  ["tendina-computer", "/report-settimanale", 1440, "document.querySelector('details.tendina-report').open = true"],
  ["pagina-report-computer", "/report", 1440, ""],
  ["pagina-report-telefono", "/report", 390, ""],
  ["menu-telefono", "/report", 390, "document.querySelector('details.menu-intestazione').open = true"],
];
for (const [nome, indirizzo, larghezza, prima] of foto) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  const telefono = larghezza < 800;
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: telefono ? 844 : 900, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  await s("Page.navigate", { url: base + indirizzo });
  await attesa(4000);
  if (prima) { await s("Runtime.evaluate", { expression: prima }); await attesa(500); }
  const { contentSize } = await s("Page.getLayoutMetrics");
  const altezza = Math.max(telefono ? 844 : 900, Math.ceil(contentSize.height));
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: altezza, scale: 1 } });
  fs.writeFileSync(path.join(qui, nome + ".png"), Buffer.from(data, "base64"));
  console.log(nome, altezza);
  await cdp("Target.closeTarget", { targetId });
}
ws.close();
chrome.kill();

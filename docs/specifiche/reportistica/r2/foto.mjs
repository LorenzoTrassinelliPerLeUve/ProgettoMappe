// Foto della tappa R2 (task #416): pagina «Prepara», segna pronto e pubblica con i pulsanti veri, poi report pubblicato e dal vivo.
// Chromium senza finestra via DevTools (come ../r0/foto.mjs). Prima: giro.sh (Web su 5820 senza login, autore di prova).
// Uso: node foto.mjs   (scrive qui: computer 1440 px e telefono 390 px)
import path from "node:path";
import fs from "node:fs";
import { spawn } from "node:child_process";

const qui = path.dirname(new URL(import.meta.url).pathname);
const base = process.env.WEB ?? "http://127.0.0.1:5820";
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--disable-gpu", "--remote-debugging-port=9338",
   `--user-data-dir=${tmp}/foto-r2`, "--no-first-run", "--lang=it-IT", "about:blank"], { stdio: "ignore" });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9338/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

const clic = testo => `[...document.querySelectorAll('button')].find(b => b.textContent.trim().startsWith(${JSON.stringify(testo)}))?.click()`;
const q = "azienda=1&settimana=2026-30";

// nome, indirizzo, larghezza, script da eseguire prima della foto (con l'attesa dopo)
const foto = [
  ["prima-report-dal-vivo-computer", `/report-settimanale?azienda=1&settimana=2026-29`, 1440, null],
  ["prepara-bozza-computer", `/report-settimanale/prepara?${q}`, 1440, null],
  ["prepara-pronto-computer", `/report-settimanale/prepara?${q}`, 1440, [clic("Segna pronto"), 2000]],
  ["prepara-pubblicato-computer", `/report-settimanale/prepara?${q}`, 1440, [clic("Pubblica"), 8000]],
  ["prepara-pubblicato-telefono", `/report-settimanale/prepara?${q}`, 390, null],
  ["dopo-report-pubblicato-computer", `/report-settimanale?${q}&pubblicato=1`, 1440, null],
  ["dopo-report-pubblicato-telefono", `/report-settimanale?${q}&pubblicato=1`, 390, null],
  ["dopo-report-dal-vivo-computer", `/report-settimanale?${q}`, 1440, null],
];
for (const [nome, indirizzo, larghezza, prima] of foto) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  const telefono = larghezza < 800;
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: telefono ? 844 : 900, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  await s("Page.navigate", { url: base + indirizzo });
  await attesa(6000);
  if (prima) { await s("Runtime.evaluate", { expression: prima[0] }); await attesa(prima[1]); }
  const { contentSize } = await s("Page.getLayoutMetrics");
  const altezza = Math.min(6000, Math.max(telefono ? 844 : 900, Math.ceil(contentSize.height)));
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: altezza, scale: 1 } });
  fs.writeFileSync(path.join(qui, nome + ".png"), Buffer.from(data, "base64"));
  console.log(nome, altezza);
  await cdp("Target.closeTarget", { targetId });
}
ws.close();
chrome.kill();

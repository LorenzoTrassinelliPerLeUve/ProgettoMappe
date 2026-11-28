// Foto della tappa R6 (task #419): obiettivi e cruscotto dell'Indice Bigot senza obiettivi scritti (MODO=prima) e con (MODO=dopo).
// Chromium senza finestra via DevTools (come ../r4/foto.mjs). Lo lancia giro.sh (Web su 5850 senza login, autore di prova).
import path from "node:path";
import fs from "node:fs";
import { spawn } from "node:child_process";

const qui = path.dirname(new URL(import.meta.url).pathname);
const base = process.env.WEB ?? "http://127.0.0.1:5850";
const anno = process.env.ANNO ?? "2026";
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--disable-gpu", "--remote-debugging-port=9358",
   `--user-data-dir=${tmp}/foto-r6`, "--no-first-run", "--lang=it-IT", "about:blank"], { stdio: "ignore" });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9358/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

const modo = process.env.MODO ?? "dopo";
const obiettivi = `/indice-bigot/obiettivi?azienda=1&anno=${anno}`;
const predefiniti = `/indice-bigot/obiettivi?anno=${anno}`;
const cruscotto = `/indice-bigot/cruscotto?azienda=1&anno=${anno}`;

const foto = modo === "prima" ? [
  // Prima = nessun obiettivo scritto: valgono le proposte dalle curve di 4Grapes. Il menù di prima (voci «in arrivo») è in ../r0.
  ["prima-obiettivi-azienda-computer", obiettivi, 1440],
  ["prima-cruscotto-computer", cruscotto, 1440],
] : [
  ["dopo-menu-report-computer", "/report", 1440],
  ["dopo-predefiniti-computer", predefiniti, 1440],
  ["dopo-obiettivi-azienda-computer", obiettivi, 1440],
  ["dopo-obiettivi-azienda-telefono", obiettivi, 390],
  ["dopo-cruscotto-computer", cruscotto, 1440],
  ["dopo-cruscotto-telefono", cruscotto, 390],
];
for (const [nome, indirizzo, larghezza] of foto) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  const telefono = larghezza < 800;
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: telefono ? 844 : 900, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  await s("Page.navigate", { url: base + indirizzo });
  await attesa(10000);
  const { contentSize } = await s("Page.getLayoutMetrics");
  const altezza = Math.min(6000, Math.max(telefono ? 844 : 900, Math.ceil(contentSize.height)));
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: altezza, scale: 1 } });
  fs.writeFileSync(path.join(qui, nome + ".png"), Buffer.from(data, "base64"));
  console.log(nome, altezza);
  await cdp("Target.closeTarget", { targetId });
}
ws.close();
chrome.kill();

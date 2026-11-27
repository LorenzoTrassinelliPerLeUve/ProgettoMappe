// Foto della tappa R3 (task #417): report settimanale con i numeri da toccare, pannello «da dove viene» su un KPI della
// sanità e sul mal dell'esca, fino al rilievo con valori per pianta e foto. Chromium senza finestra via DevTools (come ../r2).
// Prima: giro.sh (Web su 5830). MODO=prima fotografa solo il report com'era.
import path from "node:path";
import fs from "node:fs";
import { spawn } from "node:child_process";

const qui = path.dirname(new URL(import.meta.url).pathname);
const base = process.env.WEB ?? "http://127.0.0.1:5830";
const modo = process.env.MODO ?? "dopo";
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--disable-gpu", "--remote-debugging-port=9339",
   `--user-data-dir=${tmp}/foto-r3`, "--no-first-run", "--lang=it-IT", "about:blank"], { stdio: "ignore" });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9339/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

const indirizzo = "/report-settimanale?azienda=1&settimana=2026-28";
const numero = (sezione, i) => `document.getElementById(${JSON.stringify(sezione)}).querySelectorAll('button.rs-origine')[${i}].click()`;
const apri = `(() => { const b = document.querySelector('.rs-origine-apri'); b.click(); })()`;
const scorri = `document.querySelector('.rs-origine-rilievi li.aperto')?.scrollIntoView({ block: 'start' })`;

// nome, larghezza, passi [script, attesa], foto intera (true) o solo lo schermo
const foto = modo === "prima"
  ? [["prima-report-computer", 1440, [], true], ["prima-report-telefono", 390, [], false]]
  : [
      ["dopo-report-computer", 1440, [], true],
      ["dopo-report-telefono", 390, [], false],
      ["dopo-origine-peronospora-computer", 1440, [[numero("rs.sanita-grappoli", 3), 1500]], false],
      ["dopo-origine-peronospora-rilievo-computer", 1440, [[numero("rs.sanita-grappoli", 3), 1500], [apri, 2500], [scorri, 500]], false],
      ["dopo-origine-esca-rilievo-computer", 1440, [[numero("rs.esca", 0), 1500], [apri, 3500], [scorri, 2500]], false],
      ["dopo-origine-esca-rilievo-telefono", 390, [[numero("rs.esca", 0), 1500], [apri, 3500], [scorri, 2500]], false],
    ];
for (const [nome, larghezza, passi, intera] of foto) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  const telefono = larghezza < 800;
  const alto = telefono ? 844 : 900;
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: alto, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  await s("Page.navigate", { url: base + indirizzo });
  await attesa(9000);
  for (const [script, ms] of passi) { await s("Runtime.evaluate", { expression: script }); await attesa(ms); }
  const { contentSize } = await s("Page.getLayoutMetrics");
  const altezza = intera ? Math.min(3200, Math.max(alto, Math.ceil(contentSize.height))) : alto;
  const { data } = await s("Page.captureScreenshot", intera
    ? { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: altezza, scale: 1 } }
    : { format: "png" });
  fs.writeFileSync(path.join(qui, nome + ".png"), Buffer.from(data, "base64"));
  console.log(nome, altezza);
  await cdp("Target.closeTarget", { targetId });
}
ws.close();
chrome.kill();

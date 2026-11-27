// Foto della pagina di accesso (task #400), stesso metodo di ../portale-ux/foto.mjs: Chromium pilotato via DevTools.
// Web su 5800 (accesso acceso) che inoltra /perleuve/* all'Api su 5801 (accesso unico acceso, nessun 4Grapes: ogni
// tentativo è «non corretto»). Lanciato da giro.sh.  Uso: node foto.mjs CARTELLA-USCITA
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const uscita = process.argv[2] ?? "foto";
fs.mkdirSync(uscita, { recursive: true });
const WEB = "http://127.0.0.1:5800";

const tmp = process.env.TMPDIR ?? "/tmp";
fs.rmSync(`${tmp}/ux-accesso-chrome`, { recursive: true, force: true });
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--remote-debugging-port=9335", `--user-data-dir=${tmp}/ux-accesso-chrome`,
   "--no-first-run", "--disable-gpu", "--lang=it-IT", "about:blank"], { stdio: ["ignore", "ignore", fs.openSync(`${tmp}/ux-accesso-chrome.log`, "w")] });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9335/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

const suite = "/perleuve/accesso?ritorno=" + encodeURIComponent(
  "/connect/authorize?client_id=suite&response_type=code&redirect_uri=" + encodeURIComponent("https://suite.77-42-19-27.sslip.io/accesso/ritorno"));
// Prova un'email e una password (sbagliate: non c'è 4Grapes) e mostra la password.
const sbaglia = [
  `(() => { const w = document.querySelector('input[type=password]'); document.querySelector('input[type=email]').value = 'mario.rossi@cantina.it';
    w.value = 'vendemmia'; setTimeout(() => w.form.submit(), 50); return 'inviato'; })()`,
  `(() => { document.querySelector('input[type=password]').value = 'vendemmia';
    document.querySelector('.mostra, .mostra-password').click(); return document.querySelector('[role=alert]')?.innerText; })()`,
];

async function foto(nome, indirizzo, { telefono = false, azioni } = {}) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  // Chromium senza interfaccia chiede l'inglese: le foto «normali» come un browser italiano.
  await s("Network.enable");
  await s("Network.setExtraHTTPHeaders", { headers: { "Accept-Language": "it-IT,it;q=0.9" } });
  await s("Emulation.setDeviceMetricsOverride", telefono
    ? { width: 390, height: 844, deviceScaleFactor: 2, mobile: true }
    : { width: 1366, height: 768, deviceScaleFactor: 1, mobile: false });
  await s("Page.navigate", { url: WEB + indirizzo });
  await attesa(2500);
  for (const azione of azioni ?? []) {
    console.log(`  ${nome}: ${(await s("Runtime.evaluate", { expression: azione, returnByValue: true, awaitPromise: true })).result.value}`);
    await attesa(2500);
  }
  await attesa(600);
  const vista = await s("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(vista.data, "base64"));
  console.log("foto", nome);
  await cdp("Target.closeTarget", { targetId });
}

await foto("accesso-computer", "/accesso?ReturnUrl=%2F");
await foto("accesso-telefono", "/accesso?ReturnUrl=%2F", { telefono: true });
await foto("accesso-errore-mostra-telefono", "/accesso?ReturnUrl=%2F", { telefono: true, azioni: sbaglia });
await foto("accesso-computer-en", "/accesso?ReturnUrl=%2F&lingua=en");
await foto("accesso-unico-suite-computer", suite);
await foto("accesso-unico-suite-telefono-en", suite + "&lingua=en", { telefono: true });
await foto("accesso-unico-errore-computer-en", suite + "&lingua=en", { azioni: sbaglia });
ws.close(); chrome.kill();

// Foto UX del Portale (task #387). Stesso metodo di ../bilancio-idrico/ux/foto.mjs: un proxy davanti all'Api locale e
// Chromium pilotato via DevTools. Il proxy finge solo il login (POST api/accesso/verifica → utente 9001): l'Api, senza
// 4Grapes, dà a quell'Id tutti i permessi sui dati di esempio (Portale:UtenteDiProva=9001).
// Uso: node foto.mjs CARTELLA-USCITA [prima|dopo] [chiave …]   (Api su 5801, Web su 5800 che parla col proxy 5802)
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const uscita = process.argv[2] ?? "foto";
const giro = process.argv[3] ?? "prima";
const solo = process.argv.slice(4);
fs.mkdirSync(uscita, { recursive: true });
const API = "http://127.0.0.1:5801", WEB = "http://127.0.0.1:5800";

// --- proxy ---
const proxy = http.createServer(async (req, res) => {
  const url = new URL(req.url, API);
  if (req.method === "POST" && url.pathname === "/api/accesso/verifica") {
    res.writeHead(200, { "content-type": "application/json" });
    return res.end(JSON.stringify({ id: 9001, nome: "Prova UX" }));
  }
  const corpo = [];
  for await (const c of req) corpo.push(c);
  const r = await fetch(API + req.url, { method: req.method, headers: { ...req.headers, host: "127.0.0.1:5801" }, body: corpo.length ? Buffer.concat(corpo) : undefined }).catch(() => null);
  if (!r) { res.writeHead(502); return res.end(); }
  res.writeHead(r.status, { "content-type": r.headers.get("content-type") ?? "application/octet-stream" });
  res.end(Buffer.from(await r.arrayBuffer()));
});
await new Promise(ok => proxy.listen(5802, "127.0.0.1", ok));

// --- Chromium via DevTools ---
const tmp = process.env.TMPDIR ?? "/tmp";
fs.rmSync(`${tmp}/ux-portale-chrome`, { recursive: true, force: true });
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--remote-debugging-port=9334", `--user-data-dir=${tmp}/ux-portale-chrome`, "--no-first-run",
   "--disable-gpu", "--lang=it-IT", "about:blank"], { stdio: ["ignore", "ignore", fs.openSync(`${tmp}/ux-portale-chrome.log`, "w")] });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9334/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

async function scheda(larghezza, altezza, telefono) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: altezza, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  if (telefono) await s("Emulation.setTouchEmulationEnabled", { enabled: true });
  const valuta = async espr => (await s("Runtime.evaluate", { expression: espr, returnByValue: true, awaitPromise: true })).result.value;
  return { targetId, s, valuta };
}

// Login finto: una volta sola, il cookie vale per tutte le schede.
{
  const { targetId, s, valuta } = await scheda(1366, 768, false);
  await s("Page.navigate", { url: WEB + "/accesso?ReturnUrl=%2Fportale" });
  for (let i = 0; i < 40 && !(await valuta("!!document.querySelector('input[name=\"Dati.NomeUtente\"]')").catch(() => false)); i++) await attesa(250);
  await valuta(`(() => { document.querySelector('input[name="Dati.NomeUtente"]').value = 'prova';
    document.querySelector('input[name="Dati.Password"]').value = 'prova'; document.querySelector('input[name="Dati.Password"]').form.submit(); return 1; })()`);
  await attesa(3000);
  console.log("accesso:", await valuta("location.pathname"));
  await cdp("Target.closeTarget", { targetId });
}

// Dopo il caricamento: se la sezione chiede un'azienda, si sceglie la prima (le foto «vuote» non dicono niente).
const sceltaAzienda = `(async () => { const p = ms => new Promise(r => setTimeout(r, ms));
  const sel = [...document.querySelectorAll('.portale-sezione select, .pn-corpo select, main select')].find(x =>
    /azienda/i.test((x.labels?.[0]?.innerText ?? '') + (x.closest('label')?.innerText ?? '') + (x.getAttribute('aria-label') ?? '') + (x.name ?? '') + (x.id ?? '')));
  if (!sel) return 'nessuna scelta';
  const o = [...sel.options].find(o => o.value && o.value !== '0' && o.value !== '');
  if (!o) return 'nessuna azienda';
  sel.value = o.value; sel.dispatchEvent(new Event('change', { bubbles: true })); await p(2500);
  return 'azienda ' + o.text.trim(); })()`;

async function foto(nome, indirizzo, { larghezza, altezza, telefono = false, azioni }) {
  const { targetId, s, valuta } = await scheda(larghezza, altezza, telefono);
  await s("Page.navigate", { url: WEB + indirizzo });
  let ok = false;
  for (let i = 0; i < 80 && !ok; i++) { await attesa(250); ok = await valuta("!!document.querySelector('.portale-sezione, .pn-corpo') && !/Carico|caricamento|Guardo cosa/i.test(document.querySelector('.portale-sezione, .pn-corpo')?.innerText ?? '')").catch(() => false); }
  await attesa(3500); // circuito di Blazor acceso
  if (azioni) console.log(`  ${nome}: ${await valuta(azioni).catch(e => e.message)}`);
  await attesa(800);
  const vista = await s("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(uscita, nome + "-alto.png"), Buffer.from(vista.data, "base64"));
  const h = await valuta("Math.max(document.documentElement.scrollHeight, document.body.scrollHeight)");
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: Math.min(h, 9000), scale: 1 } });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(data, "base64"));
  console.log(`${nome}: ${ok ? "pronta" : "NON pronta"}, alta ${h} px`);
  await cdp("Target.closeTarget", { targetId });
}

// Nel giro «dopo»: si tocca il primo trattamento «da fare» visibile, o si apre il menù (sul telefono è chiuso).
const apriChip = `(async () => { const c = [...document.querySelectorAll('button.pn-chip.pn-suggerito')].find(b => b.offsetParent);
  if (!c) return 'nessun trattamento'; c.click(); await new Promise(r => setTimeout(r, 800));
  document.querySelector('.pn-prodotti')?.scrollIntoView({ block: 'center' }); return 'aperto ' + c.innerText; })()`;
const apriMenu = `(async () => { const b = document.querySelector('.pn-sezioni'); if (!b || !b.offsetParent) return 'menù già aperto';
  b.click(); await new Promise(r => setTimeout(r, 600)); return 'menù aperto'; })()`;

const TEL = { larghezza: 390, altezza: 844, telefono: true }, PC = { larghezza: 1366, altezza: 768 };
const SEZIONI = ["home", "indicatori", "vigneti", "rilievi", "produzione", "fallanze", "analisi-uve", "analisi-terreni", "galleria",
  "trattamenti", "bollettini", "linee-difesa", "soglie-trattamenti", "avversita", "irroratrici", "irroratrici-azienda",
  "concimazioni", "asportazioni", "dashboard-nutrizione", "notifiche", "account", "utenti", "abilitazioni", "gruppi-utenti", "impostazioni"];
const scatti = giro === "prima"
  ? [["00-ingresso", "/portale"], ...SEZIONI.map((k, i) => [`${String(i + 1).padStart(2, "0")}-${k}`, `/portale/${k}`, sceltaAzienda]),
     ["30-nutrizione", "/nutrizione"]]
  : giro === "esperienze" // task #401: le tre esperienze, con «Vedi come…» dell'utente di prova (interno)
  ? [["01-perleuve", "/esperienza"], ["02-consulenza", "/esperienza?come=consulenza"],
     ["03-abbonato", "/esperienza?come=abbonato"], ["04-consulenza-menu", "/esperienza?come=consulenza", apriMenu],
     ["05-abbonato-menu", "/esperienza?come=abbonato", apriMenu], ["06-perleuve-menu", "/esperienza", apriMenu],
     ["07-consulenza-trattamenti", "/esperienza/trattamenti?come=consulenza"]]
  : giro === "prima-dati"
  ? [["10-trattamenti-con-dati", "/portale/trattamenti"], ["00-ingresso", "/portale"]]
  : [["00-oggi", "/portale-nuovo"], ["10-trattamenti", "/portale-nuovo/difesa/trattamenti"],
     ["11-trattamenti-aperto", "/portale-nuovo/difesa/trattamenti", apriChip],
     ["12-menu", "/portale-nuovo", apriMenu],
     ["13-azienda", "/portale-nuovo?azienda=9102"],
     ["14-sezione-com-era", "/portale-nuovo/difesa/linee-difesa"]];
for (const [nome, indirizzo, azioni] of scatti) {
  if (solo.length && !solo.some(x => nome.includes(x))) continue;
  await foto(nome + "-computer", indirizzo, { ...PC, azioni });
  await foto(nome + "-telefono", indirizzo, { ...TEL, azioni });
}
ws.close(); chrome.kill(); proxy.close();
process.exit(0);

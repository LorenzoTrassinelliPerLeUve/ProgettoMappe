// Foto del bilancio idrico giorno per giorno (task #386). Stesso metodo di ../ux-config/foto.mjs: un proxy davanti all'Api
// locale che aggiunge i nomi veri dei vigneti, e Chromium pilotato via DevTools.
// Uso: node foto.mjs CARTELLA-USCITA   (Api su 127.0.0.1:5801, Web su 127.0.0.1:5800 che parla col proxy 5802)
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const uscita = process.argv[2] ?? "foto";
fs.mkdirSync(uscita, { recursive: true });
const API = "http://127.0.0.1:5801", WEB = "http://127.0.0.1:5800";
const AZIENDE = { 1173: "Michele Satta", 1116: "Rainoldi", 425: "Casa E. Mirafiore" };
const AZIENDA = +(process.env.AZIENDA ?? 1173);
const GIORNO = process.env.GIORNO ?? "2026-08-14";

// --- vigneti dal CSV del dba (IdVigneto;Ente_IdEnte;NomeVigneto;Vigna;Vitigno;...) ---
const vigneti = {};
if (process.env.VIGNETI_CSV && fs.existsSync(process.env.VIGNETI_CSV)) {
  const righe = fs.readFileSync(process.env.VIGNETI_CSV, "utf8").split(/\r?\n/).filter(Boolean);
  const intest = righe.shift().split(";").map(s => s.trim().toLowerCase());
  const [cId, cEnte, cNome, cVigna, cVit] = ["idvigneto", "ente_idente", "nomevigneto", "vigna", "vitigno"].map(n => intest.indexOf(n));
  for (const r of righe) {
    const c = r.split(";");
    const id = +c[cId], ente = +c[cEnte];
    if (id) (vigneti[ente] ??= []).push({ id, nome: (c[cNome] || `Vigneto ${id}`).trim(), vigna: c[cVigna]?.trim() || null, vitigno: c[cVit]?.trim() || null });
  }
}
const vignetiAzienda = id => ({
  vigneti: (vigneti[id] ?? []).map(v => ({ id: v.id, aziendaId: id, nome: v.nome, varieta: v.vitigno, superficieEttari: null, haGeometria: false, vignaId: null, vigna: v.vigna })),
  geoJson: { type: "FeatureCollection", features: [] },
});

const proxy = http.createServer(async (req, res) => {
  const url = new URL(req.url, API);
  const json = o => { res.writeHead(200, { "content-type": "application/json" }); res.end(JSON.stringify(o)); };
  if (req.method === "GET" && url.pathname === "/api/aziende") return json(Object.entries(AZIENDE).map(([id, nome]) => ({ id: +id, nome, comune: null, provincia: null })));
  const m = url.pathname.match(/^\/api\/aziende\/(\d+)\/vigneti$/);
  if (req.method === "GET" && m && vigneti[m[1]]) return json(vignetiAzienda(+m[1]));
  const corpo = [];
  for await (const c of req) corpo.push(c);
  const r = await fetch(API + req.url, { method: req.method, headers: { ...req.headers, host: "127.0.0.1:5801" }, body: corpo.length ? Buffer.concat(corpo) : undefined }).catch(() => null);
  if (!r) { res.writeHead(502); return res.end(); }
  res.writeHead(r.status, { "content-type": r.headers.get("content-type") ?? "application/octet-stream" });
  res.end(Buffer.from(await r.arrayBuffer()));
});
await new Promise(ok => proxy.listen(5802, "127.0.0.1", ok));

// Il vigneto: il primo dell'azienda con un consiglio (dal più urgente), se non è indicato.
const stato = await fetch(`${API}/api/bilancio-idrico/aziende/${AZIENDA}/stato`).then(r => r.json());
const VIGNETO = +(process.env.VIGNETO ?? stato.vigneti.find(v => v.suggerimento !== "N_A")?.idVigneto ?? stato.vigneti[0].idVigneto);
console.log(`azienda ${AZIENDA}, vigneto ${VIGNETO}, giorno ${GIORNO}`);

// --- Chromium via DevTools ---
const tmp = process.env.TMPDIR ?? "/tmp";
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--remote-debugging-port=9333", `--user-data-dir=${tmp}/ux-chrome`, "--no-first-run",
   "--disable-gpu", "--lang=it-IT", "about:blank"], { stdio: ["ignore", "ignore", fs.openSync(`${tmp}/ux-chrome.log`, "w")] });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9333/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

async function foto(nome, indirizzo, { larghezza, altezza = 900, telefono = false, pronto, azioni }) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: altezza, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  if (telefono) await s("Emulation.setTouchEmulationEnabled", { enabled: true });
  await s("Page.navigate", { url: WEB + indirizzo });
  const valuta = async espr => (await s("Runtime.evaluate", { expression: espr, returnByValue: true, awaitPromise: true })).result.value;
  let ok = false;
  for (let i = 0; i < 120 && !ok; i++) { await attesa(250); ok = await valuta(pronto).catch(() => false); }
  await attesa(azioni ? 4000 : 1500);
  if (azioni) { console.log(`  ${nome}: ${await valuta(azioni)}`); await attesa(1500); }
  const vista = await s("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(uscita, nome + "-alto.png"), Buffer.from(vista.data, "base64"));
  await valuta("window.scrollTo(0, 0)");
  const h = await valuta("Math.max(document.documentElement.scrollHeight, document.body.scrollHeight)");
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: Math.min(h, 12000), scale: 1 } });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(data, "base64"));
  console.log(`${nome}: ${ok ? "pronta" : "NON pronta"}, alta ${h} px`);
  await cdp("Target.closeTarget", { targetId });
}

const TEL = { larghezza: 390, altezza: 844, telefono: true }, PC = { larghezza: 1366, altezza: 900 };
const prontoVigneto = "!!document.querySelector('.bi-anno tbody tr') || /non è nel calcolo|Non riesco/.test(document.body.innerText)";
// Un tocco sul grafico dell'acqua a metà: deve cambiare il giorno in cima.
const tocca = `(async () => { const f = [...document.querySelectorAll('svg.bi-grafico')[0].querySelectorAll('rect.bi-fascia')];
  const prima = document.querySelector('.bi-stato-giorno')?.innerText; f[Math.floor(f.length / 2)].dispatchEvent(new MouseEvent('click', { bubbles: true }));
  await new Promise(r => setTimeout(r, 1200)); return prima + ' → ' + document.querySelector('.bi-stato-giorno')?.innerText + ' · ' + location.search; })()`;
const pagina = `/bilancio-idrico/vigneto?azienda=${AZIENDA}&vigneto=${VIGNETO}`;
const scatti = [
  ["vigneto-giorno", `${pagina}&giorno=${GIORNO}`, prontoVigneto],
  ["vigneto-oggi-tocco", pagina, prontoVigneto, tocca],
  ["azienda", `/bilancio-idrico?azienda=${AZIENDA}`, "!!document.querySelector('.bi-tabella, .bi-vigneti')"],
];
for (const [nome, indirizzo, pronto, azioni] of scatti) {
  await foto(nome + "-telefono", indirizzo, { ...TEL, pronto, azioni });
  await foto(nome + "-computer", indirizzo, { ...PC, pronto, azioni });
}
// L'Excel del vigneto, come lo scarica il browser.
const excel = await fetch(`${WEB}/bilancio-idrico/excel?azienda=${AZIENDA}&vigneto=${VIGNETO}`);
const buf = Buffer.from(await excel.arrayBuffer());
fs.writeFileSync(path.join(uscita, "esempio.xlsx"), buf);
console.log(`excel: ${excel.status} ${excel.headers.get("content-disposition")} ${buf.length} byte`);
ws.close(); chrome.kill(); proxy.close();
process.exit(0);

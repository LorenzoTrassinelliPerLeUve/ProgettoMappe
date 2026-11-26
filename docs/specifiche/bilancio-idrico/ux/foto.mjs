// Foto UX del bilancio idrico (task #378): un proxy davanti all'Api locale (in memoria, perimetro vuoto, database dei
// risultati copiato dall'anteprima) che aggiunge nomi e poligoni veri dei vigneti, e Chromium pilotato via DevTools.
// Uso: node foto.mjs CARTELLA-USCITA   (Api su 127.0.0.1:5801, Web su 127.0.0.1:5800 che parla col proxy 5802)
// Variabili: VIGNETI_CSV (export del dba, sotto-task #379: IdVigneto;Ente_IdEnte;NomeVigneto;Vigna;Vitigno;StatoEntita;PoligonoWKT),
// CHROMIUM (eseguibile). Nel recinto ogni comando ha la sua rete: Api, Web e questo script vanno lanciati insieme (giro.sh).
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const uscita = process.argv[2] ?? "foto";
fs.mkdirSync(uscita, { recursive: true });
const API = "http://127.0.0.1:5801", WEB = "http://127.0.0.1:5800";
const AZIENDE = { 1173: "Michele Satta", 1116: "Rainoldi", 425: "Casa E. Mirafiore" };

// --- vigneti dal CSV del dba ---
function anelli(wkt) {
  // POLYGON ((x y, ...), (...)) o MULTIPOLYGON (((...)), ((...))) → array di poligoni, ognuno array di anelli
  const t = wkt.trim().toUpperCase();
  const nums = s => s.split(",").map(p => p.trim().split(/\s+/).map(Number));
  if (t.startsWith("POLYGON")) {
    return [[...wkt.matchAll(/\(([^()]+)\)/g)].map(m => nums(m[1]))];
  }
  if (t.startsWith("MULTIPOLYGON")) {
    const corpo = wkt.slice(wkt.indexOf("(") + 1, wkt.lastIndexOf(")"));
    return corpo.split(/\)\s*\)\s*,\s*\(\s*\(/).map(p => [...("(" + p.replace(/^\s*\(+/, "(").replace(/\)+\s*$/, ")") + ")").matchAll(/\(([^()]+)\)/g)].map(m => nums(m[1])));
  }
  return null;
}
const vigneti = {};
if (process.env.VIGNETI_CSV && fs.existsSync(process.env.VIGNETI_CSV)) {
  const righe = fs.readFileSync(process.env.VIGNETI_CSV, "utf8").split(/\r?\n/).filter(Boolean);
  const intest = righe.shift().split(";").map(s => s.trim().toLowerCase());
  const col = n => intest.indexOf(n);
  const [cId, cEnte, cNome, cVigna, cVit, cWkt] = ["idvigneto", "ente_idente", "nomevigneto", "vigna", "vitigno", "poligonowkt"].map(col);
  for (const r of righe) {
    const c = r.split(";");
    const id = +c[cId], ente = +c[cEnte];
    if (!id) continue;
    const poli = c[cWkt] && c[cWkt] !== "NULL" ? anelli(c[cWkt]) : null;
    (vigneti[ente] ??= []).push({ id, nome: (c[cNome] || `Vigneto ${id}`).trim(), vigna: c[cVigna]?.trim() || null, vitigno: c[cVit]?.trim() || null, poli });
  }
}
function bbox(poli) {
  let w = 180, s = 90, e = -180, n = -90;
  for (const p of poli) for (const a of p) for (const [x, y] of a) { w = Math.min(w, x); e = Math.max(e, x); s = Math.min(s, y); n = Math.max(n, y); }
  return [w, s, e, n];
}
function vignetiAzienda(id) {
  const lista = vigneti[id] ?? [];
  const feats = lista.filter(v => v.poli).map(v => ({
    type: "Feature", id: v.id, bbox: bbox(v.poli),
    geometry: { type: "MultiPolygon", coordinates: v.poli },
    properties: { id: v.id, nome: v.nome, aziendaId: id, varieta: v.vitigno, vigna: v.vigna },
  }));
  const tutto = feats.length ? bbox(lista.filter(v => v.poli).flatMap(v => v.poli)) : null;
  return {
    vigneti: lista.map(v => ({ id: v.id, aziendaId: id, nome: v.nome, varieta: v.vitigno, superficieEttari: null, haGeometria: !!v.poli, vignaId: null, vigna: v.vigna })),
    geoJson: { type: "FeatureCollection", ...(tutto ? { bbox: tutto } : {}), features: feats },
  };
}

// --- proxy ---
const proxy = http.createServer(async (req, res) => {
  const url = new URL(req.url, API);
  const json = o => { res.writeHead(200, { "content-type": "application/json" }); res.end(JSON.stringify(o)); };
  if (req.method === "GET" && url.pathname === "/api/aziende") return json(Object.entries(AZIENDE).map(([id, nome]) => ({ id: +id, nome, comune: null, provincia: null })));
  let m = url.pathname.match(/^\/api\/aziende\/(\d+)\/vigneti$/);
  if (req.method === "GET" && m && vigneti[m[1]]) return json(vignetiAzienda(+m[1]));
  const corpo = [];
  for await (const c of req) corpo.push(c);
  const r = await fetch(API + req.url, { method: req.method, headers: { ...req.headers, host: "127.0.0.1:5801" }, body: corpo.length ? Buffer.concat(corpo) : undefined }).catch(() => null);
  if (!r) { res.writeHead(502); return res.end(); }
  res.writeHead(r.status, { "content-type": r.headers.get("content-type") ?? "application/octet-stream" });
  res.end(Buffer.from(await r.arrayBuffer()));
});
await new Promise(ok => proxy.listen(5802, "127.0.0.1", ok));
if (process.argv.includes("--solo-proxy")) { console.log("proxy su 5802"); await new Promise(() => {}); }

// --- Chromium via DevTools ---
const chrome = spawn(process.env.CHROMIUM ?? `${process.env.HOME}/.cache/ms-playwright/chromium-1243/chrome-linux64/chrome`,
  ["--headless=new", "--no-sandbox", "--no-proxy-server", "--disable-dev-shm-usage", "--remote-debugging-port=9333", `--user-data-dir=${process.env.TMPDIR ?? "/tmp"}/ux-chrome`, "--no-first-run",
   "--disable-gpu", "--use-gl=swiftshader", "--enable-unsafe-swiftshader", "--lang=it-IT", "about:blank"], { stdio: ["ignore", "ignore", fs.openSync(`${process.env.TMPDIR ?? "/tmp"}/ux-chrome.log`, "w")] });
const attesa = ms => new Promise(ok => setTimeout(ok, ms));
let wsUrl;
for (let i = 0; i < 50 && !wsUrl; i++) { await attesa(200); wsUrl = await fetch("http://127.0.0.1:9333/json/version").then(r => r.json()).then(j => j.webSocketDebuggerUrl).catch(() => null); }
const ws = new WebSocket(wsUrl);
await new Promise(ok => ws.addEventListener("open", ok));
let seq = 0; const attese = new Map();
ws.addEventListener("message", e => { const m = JSON.parse(e.data); if (m.id && attese.has(m.id)) { attese.get(m.id)(m); attese.delete(m.id); } });
const cdp = (method, params = {}, sessionId) => new Promise((ok, ko) => { const id = ++seq; attese.set(id, m => m.error ? ko(new Error(method + ": " + m.error.message)) : ok(m.result)); ws.send(JSON.stringify({ id, method, params, sessionId })); });

async function foto(nome, indirizzo, { larghezza, altezza = 900, telefono = false, pronto, azioni, intera = true }) {
  const { targetId } = await cdp("Target.createTarget", { url: "about:blank" });
  const { sessionId } = await cdp("Target.attachToTarget", { targetId, flatten: true });
  const s = (m, p) => cdp(m, p, sessionId);
  await s("Page.enable");
  await s("Emulation.setDeviceMetricsOverride", { width: larghezza, height: altezza, deviceScaleFactor: telefono ? 2 : 1, mobile: telefono });
  if (telefono) await s("Emulation.setTouchEmulationEnabled", { enabled: true });
  const t0 = Date.now();
  await s("Page.navigate", { url: WEB + indirizzo });
  const valuta = async espr => (await s("Runtime.evaluate", { expression: espr, returnByValue: true, awaitPromise: true })).result.value;
  let ok = false;
  for (let i = 0; i < 120 && !ok; i++) { await attesa(250); ok = await valuta(pronto ?? "document.readyState === 'complete'").catch(() => false); }
  const tempo = Date.now() - t0;
  await attesa(azioni ? 4000 : 800); // la pagina arriva prerenderizzata: i clic contano solo col circuito di Blazor acceso
  if (azioni) { console.log(`  ${nome}: ${await valuta(azioni)}`); await attesa(1500); }
  let clip;
  if (intera) {
    const h = await valuta("Math.max(document.documentElement.scrollHeight, document.body.scrollHeight)");
    clip = { x: 0, y: 0, width: larghezza, height: Math.min(h, 12000), scale: 1 };
  }
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: !!intera, ...(clip ? { clip } : {}) });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(data, "base64"));
  console.log(`${nome}: ${ok ? "pronta" : "NON pronta"} in ${tempo} ms`);
  await cdp("Target.closeTarget", { targetId });
}

const TEL = { larghezza: 390, altezza: 844, telefono: true }, PC = { larghezza: 1366, altezza: 768 };
const paginaPronta = "!!document.querySelector('.bi-tabella, .bi-vigneti, .bi-vuota') || /Nessun|Non ancora|Non riesco/.test(document.body.innerText)";
const schedaPronta = "!!document.querySelector('details.bi-scheda')";
const apriScheda = `(async () => { const p = ms => new Promise(r => setTimeout(r, ms));
  const det = [...document.querySelectorAll('button')].find(b => /Dettagli/.test(b.textContent) && b.offsetParent); if (det) { det.click(); await p(800); }
  const d = document.querySelector('details.bi-scheda'); if (!d) return false;
  if (!d.open) d.querySelector('summary').click();
  for (let i = 0; i < 80 && !d.querySelector('.bi-grafico, .bi-anomalie, .bi-vuota') && !/non è nel calcolo|Non riesco/.test(d.innerText); i++) await p(250);
  d.scrollIntoView({ block: 'start' }); return (d.open ? 'aperta' : 'chiusa') + ': ' + d.innerText.slice(0, 80).replace(/\\s+/g, ' '); })()`;

// argv[3] = pagine [[nome, indirizzo]], argv[4] = schede [[nome, azienda, vigneto]] (JSON; "-" = predefinite)
const arg = i => process.argv[i] && process.argv[i] !== "-" ? JSON.parse(process.argv[i]) : null;
const scatti = arg(3) ?? [
  ["satta-pagina", "/bilancio-idrico?azienda=1173"],
  ["rainoldi-pagina", "/bilancio-idrico?azienda=1116"],
  ["mirafiore-pagina", "/bilancio-idrico?azienda=425"],
];
for (const [nome, indirizzo] of scatti) {
  await foto(nome + "-telefono", indirizzo, { ...TEL, pronto: paginaPronta });
  await foto(nome + "-computer", indirizzo, { ...PC, pronto: paginaPronta });
}
const schede = arg(4) ?? [["satta-scheda", 1173, 17149], ["rainoldi-scheda", 1116, 15703]];
for (const [nome, azienda, vigneto] of schede) {
  await foto(nome + "-telefono", `/mappa/${azienda}?vigneto=${vigneto}`, { ...TEL, pronto: schedaPronta, azioni: apriScheda, intera: false });
  await foto(nome + "-computer", `/mappa/${azienda}?vigneto=${vigneto}`, { ...PC, pronto: schedaPronta, azioni: apriScheda, intera: false });
}
ws.close(); chrome.kill(); proxy.close();
process.exit(0);

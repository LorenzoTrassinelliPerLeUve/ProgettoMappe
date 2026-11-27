// Foto UX della configurazione del bilancio idrico (task #405, configurazione intorno al vigneto). Stesso metodo di ../ux/foto.mjs (task #378): un proxy davanti
// all'Api locale che aggiunge nomi e poligoni veri dei vigneti, e Chromium pilotato via DevTools.
// Uso: node foto.mjs CARTELLA-USCITA [prima|dopo]   (Api su 127.0.0.1:5801, Web su 127.0.0.1:5800 che parla col proxy 5802)
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const uscita = process.argv[2] ?? "foto";
const giro = process.argv[3] ?? "prima";
fs.mkdirSync(uscita, { recursive: true });
const API = "http://127.0.0.1:5801", WEB = "http://127.0.0.1:5800";
const AZIENDE = { 1173: "Michele Satta", 1116: "Rainoldi", 425: "Casa E. Mirafiore" };

// --- vigneti dal CSV del dba (IdVigneto;Ente_IdEnte;NomeVigneto;Vigna;Vitigno;StatoEntita;PoligonoWKT) ---
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

// --- proxy ---
const proxy = http.createServer(async (req, res) => {
  const url = new URL(req.url, API);
  const json = o => { res.writeHead(200, { "content-type": "application/json" }); res.end(JSON.stringify(o)); };
  if (req.method === "GET" && url.pathname === "/api/aziende") return json(Object.entries(AZIENDE).map(([id, nome]) => ({ id: +id, nome, comune: null, provincia: null })));
  const m = url.pathname.match(/^\/api\/aziende\/(\d+)\/vigneti$/);
  if (req.method === "GET" && m && vigneti[m[1]]) return json(vignetiAzienda(+m[1]));
  const corpo = [];
  for await (const c of req) corpo.push(c);
  // Senza le intestazioni di collegamento: fetch le rifiuta (prima ogni scrittura finiva in 502).
  const { host, connection, "content-length": _l, "transfer-encoding": _t, ...intestazioni } = req.headers;
  const r = await fetch(API + req.url, { method: req.method, headers: intestazioni, body: corpo.length ? Buffer.concat(corpo) : undefined })
    .catch(e => { console.log("proxy:", req.method, req.url, e.cause?.message ?? e.message); return null; });
  if (!r) { res.writeHead(502); return res.end(); }
  res.writeHead(r.status, { "content-type": r.headers.get("content-type") ?? "application/octet-stream" });
  res.end(Buffer.from(await r.arrayBuffer()));
});
await new Promise(ok => proxy.listen(5802, "127.0.0.1", ok));

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
  await attesa(azioni ? 4000 : 800); // la pagina arriva prerenderizzata: i clic contano solo col circuito di Blazor acceso
  if (azioni) { console.log(`  ${nome}: ${await valuta(azioni)}`); await attesa(1500); }
  // «-alto» = la schermata com'è (con la barra «in blocco» incollata in fondo, se c'è e se la sezione è più lunga).
  if (azioni) await valuta("(() => { const el = document.querySelector(window.__mira ?? '.bi-conf-sezione'); if (el) window.scrollTo(0, el.getBoundingClientRect().top + window.scrollY - 10); return true; })()");
  const vista = await s("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(uscita, nome + "-alto.png"), Buffer.from(vista.data, "base64"));
  // Nella foto intera la barra resta in fondo alla sezione, senza coprire l'elenco.
  await valuta("(() => { const st = document.createElement('style'); st.textContent = '.bi-blocco{position:static!important}'; document.head.append(st); window.scrollTo(0, 0); return true; })()");
  const h = await valuta("Math.max(document.documentElement.scrollHeight, document.body.scrollHeight)");
  const { data } = await s("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip: { x: 0, y: 0, width: larghezza, height: Math.min(h, 12000), scale: 1 } });
  fs.writeFileSync(path.join(uscita, nome + ".png"), Buffer.from(data, "base64"));
  console.log(`${nome}: ${ok ? "pronta" : "NON pronta"}, alta ${h} px`);
  await cdp("Target.closeTarget", { targetId });
}

const TEL = { larghezza: 390, altezza: 844, telefono: true }, PC = { larghezza: 1366, altezza: 768 };
const pronto = "!!document.querySelector('.bi-tabella, .bi-conf') && !/Carico la configurazione/.test(document.body.innerText)";
// Azioni: si clicca come farebbe il consulente; window.__mira = dove scorrere per la foto «-alto».
const clic = (testo, mira, quanti = 1) => `(async () => { const p = ms => new Promise(r => setTimeout(r, ms)); window.__mira = ${JSON.stringify(mira)};
  const b = [...document.querySelectorAll('button')].filter(x => { const t = x.textContent.trim().toLowerCase(), c = ${JSON.stringify(testo.toLowerCase())}; return t === c || (c.length > 2 && t.startsWith(c)); }).slice(0, ${quanti});
  for (const x of b) { x.click(); await p(500); } return b.length + ' clic su ' + ${JSON.stringify(testo)}; })()`;
const spunta = `(async () => { const p = ms => new Promise(r => setTimeout(r, ms)); window.__mira = '.bi-foglio';
  const c = [...document.querySelectorAll('.bi-foglio tbody input[type=checkbox]')].slice(0, 3);
  for (const x of c) { x.click(); await p(300); } return c.length + ' spuntati'; })()`;
// Giro 2 (task #405): la pagina nuova è /bilancio-idrico/configurazione, quella di prima /bilancio-idrico/configurazione/esperti.
const nuova = id => `/bilancio-idrico/configurazione?azienda=${id}`, vecchia = id => `/bilancio-idrico/configurazione/esperti?azienda=${id}`;
const scatti = giro === "prima"
  ? [["mirafiore-config", vecchia(425)]]
  : [["satta-config", nuova(1173)], ["rainoldi-config", nuova(1116)],
     ["rainoldi-suolo-guidato", nuova(1116), clic("Descrivi il suolo", ".bi-guida")],
     ["rainoldi-riempi-giu", nuova(1116), spunta],
     ["rainoldi-irriga", nuova(1116), clic("Sì, irriga", "#irrigazione")],
     ["satta-impianto", nuova(1173), clic("Cambia l'impianto", "#irrigazione")],
     ["satta-ho-irrigato", nuova(1173), `(() => { window.__mira = '#irrigato'; return 'ok'; })()`],
     ["satta-pagina", "/bilancio-idrico?azienda=1173"],
     // «No» si salva nella copia del database: dal secondo giro di foto Rainoldi risulta già «non irriga» (e «Sì, irriga» diventa «Ora irriga»).
     ["rainoldi-non-irriga", nuova(1116), clic("No", "#irrigazione")],
     ["mirafiore-config", nuova(425)]];
for (const [nome, indirizzo, azioni] of scatti) {
  const p = nome.endsWith("pagina") ? "!!document.querySelector('.bi-tabella, .bi-vigneti, .bi-vuota')" : pronto;
  await foto(nome + "-telefono", indirizzo, { ...TEL, pronto: p, azioni });
  await foto(nome + "-computer", indirizzo, { ...PC, pronto: p, azioni });
}
ws.close(); chrome.kill(); proxy.close();
process.exit(0);

// Layout di stampa a riquadri (task #90). Blazor decide tutto (gruppi, griglia, numeri, colori del
// tematismo); qui si disegnano i riquadri e si consegna il foglio.
//
// Ogni riquadro è una mappa MapLibre senza interazione, creata una alla volta: si aspetta che le
// tessere arrivino (idle, con un limite), si fotografa in un'immagine e la mappa si distrugge. Così
// sul foglio ci sono solo immagini (niente limite ai contesti WebGL del browser, poca memoria) e la
// stampa e il PNG vedono esattamente quello che si vede a video. Numeri dei vigneti e barra di scala
// sono elementi HTML sopra l'immagine: restano nitidi in stampa.

import {
    STYLE_INTERNO, COLORE_VIGNETO, COLORE_BORDO_VIGNETO, specSorgenteRaster, consegnaImmagine, dataPerFile, rettangoloTondo
} from "./mappa.js";

const fogli = new Map();

const ATTESA_STYLE_MS = 10000;
const ATTESA_TESSERE_MS = 20000;
const ZOOM_MASSIMO = 18;
// Margine intorno ai vigneti dentro il riquadro, in frazione del lato corto.
const MARGINE_RIQUADRO = 0.07;
const SCALA_PNG = 2;
const ID_STILE_PAGINA = "stampa-pagina";
const SVG = "http://www.w3.org/2000/svg";
// Numeri dei vigneti: anelli di posti provati intorno al vigneto, e direzioni per anello.
const ANELLI_NUMERO = 8;
const DIREZIONI_NUMERO = 12;
// PNG: stacco grigio fra una pagina e l'altra, in px del foglio a video.
const STACCO_PAGINE_PNG = 12;

/// opzioni: le stesse sorgenti della mappa (ConfigurazioneMappa), con basemap, riserva e imagery predefinite.
/// Torna true sul telefono (stesso breakpoint di app.css): lì il modo manuale non c'è.
export function preparaFoglio(contenitoreId, foglioId, opzioni, dotNetRef) {
    const stato = {
        foglioId,
        dotNetRef,
        sorgenti: new Map((opzioni.sorgenti ?? []).map((s) => [s.id, s])),
        basemapId: opzioni.basemapId ?? null,
        basemapFallbackId: opzioni.basemapFallbackId ?? null,
        imageryId: opzioni.imageryId ?? null,
        geojson: null,
        generazione: 0,
        // Indirizzi blob delle immagini dei riquadri, da liberare al disegno successivo.
        immagini: [],
        // Style di base già scaricati ("mappa" / "satellite"): { style, baseId }.
        basi: new Map(),
        // Mappa d'insieme del modo manuale (task #92), null se chiusa.
        insieme: null
    };
    fogli.set(foglioId, stato);

    // Modo manuale: un riquadro del foglio trascinato su un altro prende il suo posto nell'ordine.
    const contenitore = document.getElementById(contenitoreId);
    contenitore?.addEventListener("dragstart", (e) => {
        const riquadro = e.target.closest?.("[data-chiave-manuale]");
        if (riquadro) {
            e.dataTransfer.setData("text/plain", riquadro.dataset.chiaveManuale);
            e.dataTransfer.effectAllowed = "move";
        }
    });
    contenitore?.addEventListener("dragover", (e) => {
        if (e.target.closest?.("[data-chiave-manuale]")) {
            e.preventDefault();
            e.dataTransfer.dropEffect = "move";
        }
    });
    contenitore?.addEventListener("drop", (e) => {
        const su = e.target.closest?.("[data-chiave-manuale]");
        const testo = e.dataTransfer.getData("text/plain");
        const da = Number(testo);
        if (su && testo !== "" && Number.isInteger(da) && da !== Number(su.dataset.chiaveManuale)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync("OnRiquadroTrascinato", da, Number(su.dataset.chiaveManuale));
        }
    });

    // «Stampa / salva PDF» e «Salva PNG» nel click stesso: iOS apre una scheda (PNG senza download) solo lì.
    document.getElementById(contenitoreId)?.addEventListener("click", async (e) => {
        if (e.target.closest?.("[data-stampa]")) {
            window.print();
            return;
        }

        const pulsante = e.target.closest?.("[data-salva-png]");
        if (!pulsante || pulsante.disabled) {
            return;
        }

        const scheda = "download" in HTMLAnchorElement.prototype ? null : window.open("", "_blank");
        try {
            const blob = await componiFogli(document.getElementById(foglioId));
            consegnaImmagine(blob, `${pulsante.dataset.salvaPng}-${dataPerFile(new Date())}.png`, scheda);
        } catch (errore) {
            console.warn("[stampa] PNG non creato", errore);
            scheda?.close();
            dotNetRef.invokeMethodAsync("OnPngNonSalvato");
        }
    });

    return window.matchMedia("(max-width: 800px)").matches;
}

/// La FeatureCollection dell'Api (Feature.id = IdVigneto): una volta, i riquadri ne prendono un pezzo.
export function impostaVigneti(foglioId, geojson) {
    const stato = fogli.get(foglioId);
    if (stato) {
        stato.geojson = geojson;
    }
}

/// Formato del foglio per la stampa: «A4 portrait», «A3 landscape»… Margini dentro il foglio, non della pagina.
export function impostaPagina(misura) {
    let stile = document.getElementById(ID_STILE_PAGINA);
    if (!stile) {
        stile = document.createElement("style");
        stile.id = ID_STILE_PAGINA;
        document.head.appendChild(stile);
    }
    stile.textContent = `@page { size: ${misura}; margin: 0; }`;
}

export function distruggiFoglio(foglioId) {
    const stato = fogli.get(foglioId);
    if (stato) {
        stato.generazione = -1; // un disegno in corso si ferma al prossimo riquadro
        stato.immagini.forEach((url) => URL.revokeObjectURL(url));
        chiudi(stato);
        fogli.delete(foglioId);
    }
    document.getElementById(ID_STILE_PAGINA)?.remove();
}

/// dati: { satellite, colori (null = nessun tematismo), classi { IdVigneto: classe }, numeri { IdVigneto: n },
/// riquadri: [{ id (elemento), ids, bbox }], esatta }. Con esatta (modo manuale) il riquadro mostra proprio
/// la zona bbox, senza margine, con tutti i vigneti che ci cadono (numerati solo quelli di numeri). Un disegno nuovo (generazione più alta) ferma il precedente.
/// A ogni riquadro finito Blazor riceve OnRiquadroPronto(generazione, fatti, falliti, base usata).
/// Torna subito: il disegno dura più del tempo massimo di una chiamata da Blazor.
export function disegnaRiquadri(foglioId, generazione, dati) {
    disegna(foglioId, generazione, dati).catch((errore) => console.warn("[stampa] riquadri non disegnati", errore));
}

async function disegna(foglioId, generazione, dati) {
    const stato = fogli.get(foglioId);
    if (!stato || !stato.geojson) {
        return;
    }

    stato.generazione = generazione;
    // Le immagini del disegno precedente: Blazor ha già tolto i loro riquadri.
    stato.immagini.forEach((url) => URL.revokeObjectURL(url));
    stato.immagini = [];
    const base = await baseDi(stato, dati.satellite);
    let fatti = 0;
    let falliti = 0;
    for (const riquadro of dati.riquadri) {
        if (stato.generazione !== generazione) {
            return;
        }

        const contenitore = document.getElementById(riquadro.id);
        if (contenitore) {
            try {
                await disegnaRiquadro(stato, contenitore, riquadro, dati, base.style, generazione);
            } catch (errore) {
                console.warn("[stampa] riquadro non disegnato", errore);
                falliti++;
                const avviso = document.createElement("p");
                avviso.className = "riquadro-errore";
                avviso.textContent = "Mappa non disponibile";
                contenitore.replaceChildren(avviso);
            }
        }

        fatti++;
        if (stato.generazione !== generazione) {
            return;
        }
        await stato.dotNetRef.invokeMethodAsync("OnRiquadroPronto", generazione, fatti, falliti, base.baseId);
    }
}

// ---------- Base cartografica ----------

/// Mappa: lo style della basemap (o della riserva, o quello interno senza rete). Satellite: style
/// interno con sopra l'imagery, come il pulsante Satellite della mappa (che copre la basemap).
async function baseDi(stato, satellite) {
    const chiave = satellite ? "satellite" : "mappa";
    if (!stato.basi.has(chiave)) {
        stato.basi.set(chiave, satellite ? baseSatellite(stato) : await baseMappa(stato));
    }
    return stato.basi.get(chiave);
}

function baseSatellite(stato) {
    const style = structuredClone(STYLE_INTERNO);
    const sorgente = stato.imageryId ? stato.sorgenti.get(stato.imageryId) : null;
    if (sorgente) {
        style.sources.imagery = specSorgenteRaster(sorgente);
        style.layers.push({ id: "imagery", type: "raster", source: "imagery" });
    }
    return { style, baseId: sorgente ? sorgente.id : "interno" };
}

async function baseMappa(stato) {
    for (const id of [stato.basemapId, stato.basemapFallbackId]) {
        const sorgente = id ? stato.sorgenti.get(id) : null;
        if (!sorgente?.styleUrl) {
            continue;
        }

        const abort = new AbortController();
        const timer = setTimeout(() => abort.abort(), ATTESA_STYLE_MS);
        try {
            const risposta = await fetch(sorgente.styleUrl, { signal: abort.signal });
            if (!risposta.ok) {
                throw new Error(`HTTP ${risposta.status}`);
            }
            const style = await risposta.json();
            if (style?.version === 8 && Array.isArray(style.layers) && typeof style.sources === "object") {
                return { style, baseId: id };
            }
        } catch (errore) {
            console.warn(`[stampa] basemap '${id}' non disponibile`, errore?.message ?? errore);
        } finally {
            clearTimeout(timer);
        }
    }
    return { style: structuredClone(STYLE_INTERNO), baseId: "interno" };
}

// ---------- Un riquadro ----------

async function disegnaRiquadro(stato, contenitore, riquadro, dati, style, generazione) {
    contenitore.replaceChildren();
    const tela = document.createElement("div");
    tela.className = "riquadro-tela";
    contenitore.appendChild(tela);

    const larghezza = contenitore.clientWidth;
    const altezza = contenitore.clientHeight;
    const margine = dati.esatta ? 0 : Math.max(8, Math.round(Math.min(larghezza, altezza) * MARGINE_RIQUADRO));
    const ids = new Set(riquadro.ids);
    const dentro = dati.esatta ? (f) => intersecaBbox(f, riquadro.bbox) : (f) => ids.has(f.id);
    const features = stato.geojson.features.filter(dentro).map((f) => {
        const { colore, opacita } = stileVigneto(dati, f.id);
        return { type: "Feature", id: f.id, geometry: f.geometry, properties: { colore, opacita } };
    });

    const mappa = new maplibregl.Map({
        container: tela,
        style: structuredClone(style),
        bounds: riquadro.bbox,
        fitBoundsOptions: { padding: margine, maxZoom: ZOOM_MASSIMO },
        interactive: false,
        attributionControl: false,
        preserveDrawingBuffer: true,
        fadeDuration: 0,
        pixelRatio: Math.max(SCALA_PNG, window.devicePixelRatio || 1)
    });

    try {
        await new Promise((fatto, errore) => {
            const timer = setTimeout(() => errore(new Error("style non caricato")), ATTESA_STYLE_MS);
            mappa.once("load", () => { clearTimeout(timer); fatto(); });
        });

        mappa.addSource("vigneti", { type: "geojson", data: { type: "FeatureCollection", features } });
        mappa.addLayer({
            id: "vigneti-fill", type: "fill", source: "vigneti",
            paint: { "fill-color": ["get", "colore"], "fill-opacity": ["get", "opacita"] }
        });
        mappa.addLayer({
            id: "vigneti-outline", type: "line", source: "vigneti",
            paint: { "line-color": COLORE_BORDO_VIGNETO, "line-width": 1.5 }
        });
        mappa.addLayer({
            id: "vigneti-punti", type: "circle", source: "vigneti", filter: ["==", ["geometry-type"], "Point"],
            paint: { "circle-color": ["get", "colore"], "circle-radius": 5, "circle-stroke-color": COLORE_BORDO_VIGNETO, "circle-stroke-width": 1.5 }
        });

        // Le tessere: si aspettano, ma una che non arriva non blocca il foglio.
        await new Promise((fatto) => {
            const timer = setTimeout(fatto, ATTESA_TESSERE_MS);
            mappa.once("idle", () => { clearTimeout(timer); fatto(); });
            mappa.triggerRepaint();
        });

        if (stato.generazione !== generazione) {
            return;
        }

        const url = await fotografa(mappa.getCanvas());
        stato.immagini.push(url);
        const etichette = features.map((f) => {
            const punto = mappa.project(puntoEtichetta(f.geometry));
            return { numero: dati.numeri?.[f.id], x: punto.x / larghezza * 100, y: punto.y / altezza * 100 };
        });
        const scala = barraDiScala(mappa, larghezza, altezza);

        const immagine = document.createElement("img");
        immagine.className = "riquadro-immagine";
        immagine.alt = "";
        immagine.draggable = false;
        await new Promise((fatto, errore) => {
            immagine.onload = fatto;
            immagine.onerror = () => errore(new Error("immagine del riquadro non caricata"));
            immagine.src = url;
        });
        contenitore.replaceChildren(immagine);

        const richiami = document.createElementNS(SVG, "svg");
        richiami.classList.add("riquadro-richiami");
        richiami.setAttribute("viewBox", `0 0 ${larghezza} ${altezza}`);
        richiami.setAttribute("preserveAspectRatio", "none");
        richiami.setAttribute("aria-hidden", "true");
        contenitore.appendChild(richiami);

        const numeri = [];
        for (const e of etichette.sort((a, b) => (a.numero ?? 0) - (b.numero ?? 0))) {
            if (e.numero === undefined || e.x < 0 || e.x > 100 || e.y < 0 || e.y > 100) {
                continue;
            }
            const numero = document.createElement("span");
            numero.className = "riquadro-numero";
            numero.style.left = `${e.x}%`;
            numero.style.top = `${e.y}%`;
            numero.textContent = String(e.numero);
            contenitore.appendChild(numero);
            numeri.push({ el: numero, x: e.x / 100 * larghezza, y: e.y / 100 * altezza });
        }

        if (scala) {
            const barra = document.createElement("div");
            barra.className = "riquadro-scala";
            const linea = document.createElement("span");
            linea.className = "linea";
            linea.style.width = `${scala.percento}%`;
            const testo = document.createElement("span");
            testo.className = "testo";
            testo.textContent = scala.testo;
            barra.append(linea, testo);
            contenitore.appendChild(barra);
        }

        sistemaNumeri(contenitore, numeri, richiami, larghezza, altezza);
    } finally {
        mappa.remove();
    }
}

// ---------- Numeri che non si coprono (task #91) ----------

/// Ogni numero (in ordine) va sul suo vigneto se lì c'è posto; altrimenti nel posto libero più vicino,
/// su anelli sempre più larghi, senza coprire altri numeri, lettera, nord e scala né uscire dal
/// riquadro. Se finisce lontano (poligoni minuscoli e fitti) una linea lo collega al vigneto.
/// Le posizioni restano in % del riquadro: in stampa il foglio ha le stesse misure.
function sistemaNumeri(contenitore, numeri, richiami, larghezza, altezza) {
    const box = contenitore.getBoundingClientRect();
    const scala = box.width > 0 ? larghezza / box.width : 1;
    const relativo = (r) => ({
        x: (r.left - box.left) * scala, y: (r.top - box.top) * scala, w: r.width * scala, h: r.height * scala
    });
    const ostacoli = [
        ...(contenitore.parentElement?.querySelectorAll(".riquadro-lettera, .riquadro-nord") ?? []),
        ...contenitore.querySelectorAll(".riquadro-scala .linea, .riquadro-scala .testo")
    ].map((el) => relativo(el.getBoundingClientRect()));
    const posati = [];

    for (const n of numeri) {
        const w = n.el.offsetWidth * scala;
        const h = n.el.offsetHeight * scala;
        const rettangolo = (cx, cy) => ({ x: cx - w / 2 - 0.5, y: cy - h / 2 - 0.5, w: w + 1, h: h + 1 });
        const libero = (r) => r.x >= 0 && r.y >= 0 && r.x + r.w <= larghezza && r.y + r.h <= altezza
            && !posati.some((p) => sovrapposti(p, r)) && !ostacoli.some((o) => sovrapposti(o, r));

        let posto = null;
        const passo = h * 0.8;
        for (let anello = 0; anello <= ANELLI_NUMERO && !posto; anello++) {
            const direzioni = anello === 0 ? 1 : DIREZIONI_NUMERO;
            for (let d = 0; d < direzioni && !posto; d++) {
                // Prima sopra, poi in giro: il numero sopra il suo vigneto si legge meglio.
                const angolo = -Math.PI / 2 + d * 2 * Math.PI / DIREZIONI_NUMERO;
                const cx = n.x + Math.cos(angolo) * anello * passo * (w / h);
                const cy = n.y + Math.sin(angolo) * anello * passo;
                if (libero(rettangolo(cx, cy))) {
                    posto = { x: cx, y: cy };
                }
            }
        }
        // Nessun posto libero: resta sul vigneto (meglio coperto che lontano dal suo vigneto).
        posto ??= { x: n.x, y: n.y };
        posati.push(rettangolo(posto.x, posto.y));
        n.el.style.left = `${posto.x / larghezza * 100}%`;
        n.el.style.top = `${posto.y / altezza * 100}%`;

        if (Math.hypot(posto.x - n.x, posto.y - n.y) > Math.max(w, h) * 0.6) {
            // Dal vigneto al bordo del numero più vicino.
            const bx = Math.min(Math.max(n.x, posto.x - w / 2), posto.x + w / 2);
            const by = Math.min(Math.max(n.y, posto.y - h / 2), posto.y + h / 2);
            const linea = document.createElementNS(SVG, "line");
            linea.setAttribute("x1", String(n.x));
            linea.setAttribute("y1", String(n.y));
            linea.setAttribute("x2", String(bx));
            linea.setAttribute("y2", String(by));
            linea.setAttribute("stroke", "currentColor");
            linea.setAttribute("stroke-width", "0.8");
            const punto = document.createElementNS(SVG, "circle");
            punto.setAttribute("cx", String(n.x));
            punto.setAttribute("cy", String(n.y));
            punto.setAttribute("r", "1.3");
            punto.setAttribute("fill", "currentColor");
            richiami.append(linea, punto);
        }
    }
}

/// Il bbox della Feature (dall'Api) tocca il rettangolo [W, S, E, N]?
function intersecaBbox(feature, bbox) {
    const b = feature.bbox;
    return !Array.isArray(b) || (b[0] <= bbox[2] && b[2] >= bbox[0] && b[1] <= bbox[3] && b[3] >= bbox[1]);
}

function sovrapposti(a, b) {
    return a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
}

/// Colore e trasparenza come sulla mappa: classe del tematismo (−1 nessun dato grigio tenue, −2 non
/// valutato quasi trasparente), senza tematismo il verde di base.
function stileVigneto(dati, id) {
    if (!dati.colori) {
        return { colore: COLORE_VIGNETO, opacita: 0.3 };
    }
    const classe = dati.classi?.[id] ?? -1;
    if (classe >= 0 && classe < dati.colori.length) {
        return { colore: dati.colori[classe], opacita: 0.75 };
    }
    return { colore: "#9e9e9e", opacita: classe === -2 ? 0.05 : 0.25 };
}

/// Il disegno WebGL in un'immagine (con preserveDrawingBuffer il buffer resta leggibile dopo idle).
function fotografa(canvas) {
    return new Promise((fatto, errore) =>
        canvas.toBlob((blob) => blob ? fatto(URL.createObjectURL(blob)) : errore(new Error("toBlob vuoto")), "image/png"));
}

/// Dove scrivere il numero: baricentro dell'anello esterno della parte più grande (per i poligoni a
/// L o a C può cadere appena fuori, va bene: il numero resta vicino al suo vigneto).
function puntoEtichetta(geometria) {
    if (geometria.type === "Point") {
        return geometria.coordinates;
    }
    const poligoni = geometria.type === "Polygon" ? [geometria.coordinates]
        : geometria.type === "MultiPolygon" ? geometria.coordinates : [];
    let migliore = null;
    for (const poligono of poligoni) {
        const c = baricentro(poligono[0] ?? []);
        if (c && (!migliore || c.area > migliore.area)) {
            migliore = c;
        }
    }
    if (migliore) {
        return [migliore.x, migliore.y];
    }
    // Linee o altro: il primo vertice.
    const primo = (c) => (Array.isArray(c?.[0]) ? primo(c[0]) : c);
    return primo(geometria.coordinates) ?? [0, 0];
}

function baricentro(anello) {
    let area = 0, x = 0, y = 0;
    for (let i = 0, j = anello.length - 1; i < anello.length; j = i++) {
        const [x0, y0] = anello[j];
        const [x1, y1] = anello[i];
        const f = x0 * y1 - x1 * y0;
        area += f;
        x += (x0 + x1) * f;
        y += (y0 + y1) * f;
    }
    if (Math.abs(area) < 1e-14) {
        return anello.length ? { x: anello[0][0], y: anello[0][1], area: 0 } : null;
    }
    return { x: x / (3 * area), y: y / (3 * area), area: Math.abs(area / 2) };
}

/// Barra di scala lunga una cifra tonda (1, 2, 5 × 10ⁿ m), al più un quarto della larghezza del riquadro.
function barraDiScala(mappa, larghezza, altezza) {
    const y = altezza / 2;
    const a = mappa.unproject([0, y]);
    const b = mappa.unproject([larghezza, y]);
    const metri = a.distanceTo(b);
    if (!(metri > 0)) {
        return null;
    }
    const massimo = metri / 4;
    const potenza = Math.pow(10, Math.floor(Math.log10(massimo)));
    const tonda = [5, 2, 1].map((m) => m * potenza).find((v) => v <= massimo) ?? potenza;
    const testo = tonda >= 1000 ? `${(tonda / 1000).toLocaleString("it-IT")} km` : `${tonda} m`;
    return { percento: tonda / metri * 100, testo };
}

// ---------- PNG del foglio ----------

/// Le pagine del foglio (la prima coi riquadri, poi quelle dell'indice) in un'immagine sola, una
/// sotto l'altra con uno stacco grigio, come a video.
async function componiFogli(contenitore) {
    await document.fonts?.ready;
    const pagine = [...contenitore.querySelectorAll(".stampa-foglio")];
    const misure = pagine.map((p) => p.getBoundingClientRect());
    const larghezza = Math.max(...misure.map((m) => m.width));
    const altezza = misure.reduce((somma, m) => somma + m.height, 0) + STACCO_PAGINE_PNG * (pagine.length - 1);
    const canvas = document.createElement("canvas");
    canvas.width = Math.round(larghezza * SCALA_PNG);
    canvas.height = Math.round(altezza * SCALA_PNG);
    const ctx = canvas.getContext("2d");
    ctx.scale(SCALA_PNG, SCALA_PNG);
    ctx.fillStyle = "#d9dee1";
    ctx.fillRect(0, 0, larghezza, altezza);

    let y = 0;
    for (let i = 0; i < pagine.length; i++) {
        ctx.save();
        ctx.translate(0, y);
        await disegnaFoglio(ctx, pagine[i]);
        ctx.restore();
        y += misure[i].height + STACCO_PAGINE_PNG;
    }

    return new Promise((fatto, errore) =>
        canvas.toBlob((blob) => blob ? fatto(blob) : errore(new Error("toBlob vuoto")), "image/png"));
}

/// Una pagina com'è a video, ridisegnata sul canvas: sfondi, bordi, immagini dei riquadri, SVG (frecce
/// del nord, linee di richiamo) e testi, carattere per carattere nella posizione calcolata dal browser
/// (a capo, spaziature e maiuscole compresi). Gli elementi con overflow nascosto tagliano quello che contengono.
async function disegnaFoglio(ctx, foglio) {
    const box = foglio.getBoundingClientRect();
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, box.width, box.height);

    const relativo = (r) => ({ x: r.left - box.left, y: r.top - box.top, w: r.width, h: r.height });
    const ritagli = new Map();
    const ritaglioDi = (el) => {
        if (el === foglio || !el.parentElement) {
            return null;
        }
        if (ritagli.has(el)) {
            return ritagli.get(el);
        }
        let ritaglio = ritaglioDi(el.parentElement);
        if (getComputedStyle(el).overflow !== "visible") {
            const r = relativo(el.getBoundingClientRect());
            ritaglio = ritaglio ? intersezione(ritaglio, r) : r;
        }
        ritagli.set(el, ritaglio);
        return ritaglio;
    };

    const range = document.createRange();
    for (const el of [foglio, ...foglio.querySelectorAll("*")]) {
        if (el instanceof SVGElement && el.tagName.toLowerCase() !== "svg") {
            continue; // disegnati con il loro <svg>
        }
        const cs = getComputedStyle(el);
        if (cs.visibility === "hidden" || el.getClientRects().length === 0) {
            continue;
        }

        const r = relativo(el.getBoundingClientRect());
        ctx.save();
        const ritaglio = el.parentElement ? ritaglioDi(el.parentElement) : null;
        if (ritaglio) {
            ctx.beginPath();
            ctx.rect(ritaglio.x, ritaglio.y, ritaglio.w, ritaglio.h);
            ctx.clip();
        }
        ctx.globalAlpha = Number(cs.opacity) || 1;

        const raggio = Math.min(parseFloat(cs.borderTopLeftRadius) || 0, r.w / 2, r.h / 2);
        if (!trasparente(cs.backgroundColor)) {
            ctx.fillStyle = cs.backgroundColor;
            rettangoloTondo(ctx, r.x, r.y, r.w, r.h, raggio);
            ctx.fill();
        }
        disegnaBordi(ctx, cs, r, raggio);

        if (el instanceof HTMLImageElement && el.complete && el.naturalWidth > 0) {
            ctx.drawImage(el, r.x, r.y, r.w, r.h);
        } else if (el instanceof SVGSVGElement) {
            await disegnaSvg(ctx, el, cs, r);
        }

        for (const nodo of el.childNodes) {
            if (nodo.nodeType === Node.TEXT_NODE && nodo.textContent.trim()) {
                disegnaTesto(ctx, nodo, cs, range, relativo);
            }
        }
        ctx.restore();
    }
}

function intersezione(a, b) {
    const x = Math.max(a.x, b.x);
    const y = Math.max(a.y, b.y);
    return { x, y, w: Math.max(0, Math.min(a.x + a.w, b.x + b.w) - x), h: Math.max(0, Math.min(a.y + a.h, b.y + b.h) - y) };
}

function trasparente(colore) {
    return !colore || colore === "transparent" || /rgba\(.*,\s*0\)$/.test(colore);
}

function disegnaBordi(ctx, cs, r, raggio) {
    const lati = ["Top", "Right", "Bottom", "Left"].map((lato) => ({
        lato,
        spessore: cs[`border${lato}Style`] === "none" ? 0 : parseFloat(cs[`border${lato}Width`]) || 0,
        colore: cs[`border${lato}Color`]
    }));
    const uguali = lati.every((l) => l.spessore === lati[0].spessore && l.colore === lati[0].colore);
    if (uguali && lati[0].spessore > 0 && raggio > 0) {
        const s = lati[0].spessore;
        ctx.strokeStyle = lati[0].colore;
        ctx.lineWidth = s;
        rettangoloTondo(ctx, r.x + s / 2, r.y + s / 2, r.w - s, r.h - s, Math.max(0, raggio - s / 2));
        ctx.stroke();
        return;
    }
    for (const { lato, spessore, colore } of lati) {
        if (spessore <= 0 || trasparente(colore)) {
            continue;
        }
        ctx.fillStyle = colore;
        if (lato === "Top") ctx.fillRect(r.x, r.y, r.w, spessore);
        else if (lato === "Bottom") ctx.fillRect(r.x, r.y + r.h - spessore, r.w, spessore);
        else if (lato === "Left") ctx.fillRect(r.x, r.y, spessore, r.h);
        else ctx.fillRect(r.x + r.w - spessore, r.y, spessore, r.h);
    }
}

/// Un SVG della pagina (freccia del nord) come immagine: copiato con le misure e il colore calcolati.
async function disegnaSvg(ctx, svg, cs, r) {
    const copia = svg.cloneNode(true);
    copia.setAttribute("xmlns", "http://www.w3.org/2000/svg");
    copia.setAttribute("width", String(r.w));
    copia.setAttribute("height", String(r.h));
    copia.setAttribute("color", cs.color);
    const url = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(copia)], { type: "image/svg+xml" }));
    try {
        const immagine = new Image();
        await new Promise((fatto, errore) => {
            immagine.onload = fatto;
            immagine.onerror = errore;
            immagine.src = url;
        });
        ctx.drawImage(immagine, r.x, r.y, r.w, r.h);
    } catch {
        // Una freccia mancante non deve far perdere il foglio.
    } finally {
        URL.revokeObjectURL(url);
    }
}

function disegnaTesto(ctx, nodo, cs, range, relativo) {
    ctx.font = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
    ctx.fillStyle = cs.color;
    ctx.textAlign = "left";
    ctx.textBaseline = "alphabetic";
    const misura = ctx.measureText("Hg");
    const sopra = misura.fontBoundingBoxAscent ?? parseFloat(cs.fontSize) * 0.8;
    const sotto = misura.fontBoundingBoxDescent ?? parseFloat(cs.fontSize) * 0.2;
    const maiuscolo = cs.textTransform === "uppercase";
    const testo = nodo.textContent;
    for (let i = 0; i < testo.length; i++) {
        // Coppie surrogate (emoji, simboli): un carattere solo.
        const lunghezza = testo.codePointAt(i) > 0xffff ? 2 : 1;
        const carattere = testo.slice(i, i + lunghezza);
        if (carattere.trim()) {
            range.setStart(nodo, i);
            range.setEnd(nodo, i + lunghezza);
            const rettangolo = range.getBoundingClientRect();
            if (rettangolo.width > 0) {
                const r = relativo(rettangolo);
                const y = r.y + (r.h - sopra - sotto) / 2 + sopra;
                ctx.fillText(maiuscolo ? carattere.toUpperCase() : carattere, r.x, y);
            }
        }
        i += lunghezza - 1;
    }
}

// ---------- Modo manuale: mappa d'insieme (task #92) ----------
//
// Una mappa interattiva con tutti i vigneti e, sopra, i riquadri dell'utente come rettangoli HTML
// (sempre nord in alto: la rotazione è spenta). Blazor tiene i riquadri: qui si disegnano, si spostano
// dall'etichetta e si ridimensionano da bordi e angoli; a fine gesto la zona nuova va a Blazor
// (OnZonaCambiata), che ricalcola conteggi e foglio e rimanda i riquadri (aggiornaInsieme).

const LATI = ["n", "s", "e", "w", "ne", "nw", "se", "sw"];
// Sotto questi pixel un trascinamento non è un riquadro (un clic per sbaglio).
const LATO_MINIMO_PX = 12;

/// dati: { satellite, colori, classi, riquadri: [{ chiave, lettera, bbox, testo }] }.
export async function apriInsieme(foglioId, elementoId, dati) {
    const stato = fogli.get(foglioId);
    const elemento = document.getElementById(elementoId);
    if (!stato || !stato.geojson || !elemento) {
        return;
    }
    if (stato.insieme?.elemento === elemento) {
        await aggiornaInsieme(foglioId, dati);
        return;
    }
    chiudi(stato);

    const insieme = { elemento, dati, mappa: null, livello: null, satellite: dati.satellite, gesto: null, cattura: null };
    stato.insieme = insieme;
    const base = await baseDi(stato, dati.satellite);
    if (stato.insieme !== insieme) {
        return;
    }

    const mappa = new maplibregl.Map({
        container: elemento,
        style: structuredClone(base.style),
        bounds: bboxDi(stato.geojson),
        fitBoundsOptions: { padding: 40, maxZoom: 16 },
        dragRotate: false,
        pitchWithRotate: false,
        touchPitch: false,
        attributionControl: { compact: true }
    });
    mappa.touchZoomRotate.disableRotation();
    mappa.keyboard.disableRotation();
    mappa.addControl(new maplibregl.NavigationControl({ showCompass: false }), "top-right");
    insieme.mappa = mappa;

    const livello = document.createElement("div");
    livello.className = "insieme-livello";
    elemento.appendChild(livello);
    insieme.livello = livello;

    mappa.on("style.load", () => aggiungiVignetiInsieme(stato, insieme));
    if (mappa.isStyleLoaded()) {
        aggiungiVignetiInsieme(stato, insieme);
    }
    mappa.on("move", () => posizionaRettangoli(insieme));
    mappa.on("resize", () => posizionaRettangoli(insieme));
    disegnaRettangoli(stato, insieme);
}

/// Colori o riquadri cambiati (o lo sfondo: allora cambia lo style e i vigneti si rimettono).
export async function aggiornaInsieme(foglioId, dati) {
    const stato = fogli.get(foglioId);
    const insieme = stato?.insieme;
    if (!insieme) {
        return;
    }
    insieme.dati = dati;
    if (!insieme.gesto) {
        disegnaRettangoli(stato, insieme);
    }
    if (insieme.mappa && dati.satellite !== insieme.satellite) {
        insieme.satellite = dati.satellite;
        const base = await baseDi(stato, dati.satellite);
        // Senza diff: style.load arriva sempre e rimette i vigneti.
        insieme.mappa?.setStyle(structuredClone(base.style), { diff: false });
    } else if (insieme.mappa?.getSource("vigneti")) {
        insieme.mappa.getSource("vigneti").setData(vignetiInsieme(stato, dati));
    }
}

export function chiudiInsieme(foglioId) {
    const stato = fogli.get(foglioId);
    if (stato) {
        chiudi(stato);
    }
}

/// «+ Riquadro»: il prossimo trascinamento sulla mappa disegna un rettangolo (Esc annulla).
/// Alla fine OnZonaDisegnata([W, S, E, N]), o OnZonaDisegnata(null) se annullato.
export function disegnaZona(foglioId) {
    const stato = fogli.get(foglioId);
    const insieme = stato?.insieme;
    if (!insieme?.mappa || insieme.cattura) {
        return false;
    }

    const cattura = document.createElement("div");
    cattura.className = "insieme-cattura";
    const rettangolo = document.createElement("div");
    rettangolo.className = "insieme-nuovo";
    rettangolo.hidden = true;
    cattura.appendChild(rettangolo);
    insieme.livello.appendChild(cattura);
    insieme.cattura = cattura;

    let inizio = null;
    let fine = null;
    const punto = (e) => {
        const box = cattura.getBoundingClientRect();
        return { x: Math.min(Math.max(e.clientX - box.left, 0), box.width), y: Math.min(Math.max(e.clientY - box.top, 0), box.height) };
    };
    const esc = (e) => {
        if (e.key === "Escape") {
            termina(null);
        }
    };
    const termina = (bbox) => {
        document.removeEventListener("keydown", esc);
        cattura.remove();
        insieme.cattura = null;
        stato.dotNetRef.invokeMethodAsync("OnZonaDisegnata", bbox);
    };
    document.addEventListener("keydown", esc);

    cattura.addEventListener("pointerdown", (e) => {
        if (e.button !== 0) {
            return;
        }
        e.preventDefault();
        cattura.setPointerCapture(e.pointerId);
        inizio = fine = punto(e);
    });
    cattura.addEventListener("pointermove", (e) => {
        if (!inizio) {
            return;
        }
        fine = punto(e);
        mettiRettangolo(rettangolo, rettangoloDa(inizio, fine));
        rettangolo.hidden = false;
    });
    cattura.addEventListener("pointerup", () => {
        if (!inizio) {
            return;
        }
        const r = rettangoloDa(inizio, fine);
        inizio = null;
        if (r.w < LATO_MINIMO_PX || r.h < LATO_MINIMO_PX) {
            rettangolo.hidden = true; // troppo piccolo: si riprova
            return;
        }
        termina(bboxDaPixel(insieme.mappa, r));
    });
    return true;
}

/// La zona dell'inquadratura corrente, un po' più stretta (così il riquadro si vede e si afferra).
export function zonaInquadratura(foglioId) {
    const mappa = fogli.get(foglioId)?.insieme?.mappa;
    if (!mappa) {
        return null;
    }
    const { clientWidth: w, clientHeight: h } = mappa.getContainer();
    return bboxDaPixel(mappa, { x: w * 0.1, y: h * 0.1, w: w * 0.8, h: h * 0.8 });
}

/// Inquadra la mappa d'insieme su una zona (clic sulla lettera nell'elenco).
export function mostraZona(foglioId, bbox) {
    fogli.get(foglioId)?.insieme?.mappa?.fitBounds(bbox, { padding: 60, maxZoom: 17 });
}

function chiudi(stato) {
    const insieme = stato.insieme;
    stato.insieme = null;
    if (insieme) {
        insieme.cattura?.remove();
        insieme.livello?.remove();
        insieme.mappa?.remove();
    }
}

function bboxDi(geojson) {
    if (Array.isArray(geojson.bbox) && geojson.bbox.length === 4) {
        return geojson.bbox;
    }
    const b = [180, 90, -180, -90];
    for (const f of geojson.features ?? []) {
        if (Array.isArray(f.bbox)) {
            b[0] = Math.min(b[0], f.bbox[0]);
            b[1] = Math.min(b[1], f.bbox[1]);
            b[2] = Math.max(b[2], f.bbox[2]);
            b[3] = Math.max(b[3], f.bbox[3]);
        }
    }
    return b[0] <= b[2] ? b : [6, 36, 19, 47];
}

function vignetiInsieme(stato, dati) {
    return {
        type: "FeatureCollection",
        features: stato.geojson.features.map((f) => {
            const { colore, opacita } = stileVigneto(dati, f.id);
            return { type: "Feature", id: f.id, geometry: f.geometry, properties: { colore, opacita: Math.max(opacita, 0.15) } };
        })
    };
}

function aggiungiVignetiInsieme(stato, insieme) {
    const mappa = insieme.mappa;
    if (!mappa || mappa.getSource("vigneti")) {
        return;
    }
    mappa.addSource("vigneti", { type: "geojson", data: vignetiInsieme(stato, insieme.dati) });
    mappa.addLayer({
        id: "vigneti-fill", type: "fill", source: "vigneti", filter: ["!=", ["geometry-type"], "Point"],
        paint: { "fill-color": ["get", "colore"], "fill-opacity": ["get", "opacita"] }
    });
    mappa.addLayer({
        id: "vigneti-outline", type: "line", source: "vigneti", filter: ["!=", ["geometry-type"], "Point"],
        paint: { "line-color": COLORE_BORDO_VIGNETO, "line-width": 1 }
    });
    mappa.addLayer({
        id: "vigneti-punti", type: "circle", source: "vigneti", filter: ["==", ["geometry-type"], "Point"],
        paint: { "circle-color": ["get", "colore"], "circle-radius": 4, "circle-stroke-color": COLORE_BORDO_VIGNETO, "circle-stroke-width": 1 }
    });
}

/// I rettangoli da capo, dai riquadri di Blazor: etichetta (lettera, conteggio, elimina) e 8 maniglie.
function disegnaRettangoli(stato, insieme) {
    if (!insieme.livello) {
        return;
    }
    insieme.livello.querySelectorAll(".insieme-riquadro").forEach((el) => el.remove());
    for (const riquadro of insieme.dati.riquadri ?? []) {
        const el = document.createElement("div");
        el.className = "insieme-riquadro";
        el.dataset.chiave = String(riquadro.chiave);

        const etichetta = document.createElement("div");
        etichetta.className = "insieme-etichetta";
        etichetta.title = "Trascina per spostare il riquadro";
        const lettera = document.createElement("strong");
        lettera.textContent = riquadro.lettera;
        const testo = document.createElement("span");
        testo.textContent = riquadro.testo;
        const elimina = document.createElement("button");
        elimina.type = "button";
        elimina.className = "insieme-elimina";
        elimina.title = `Elimina il riquadro ${riquadro.lettera}`;
        elimina.setAttribute("aria-label", elimina.title);
        elimina.textContent = "×";
        elimina.addEventListener("pointerdown", (e) => e.stopPropagation());
        elimina.addEventListener("click", () => stato.dotNetRef.invokeMethodAsync("OnEliminaRiquadro", riquadro.chiave));
        etichetta.append(lettera, testo, elimina);
        el.appendChild(etichetta);
        etichetta.addEventListener("pointerdown", (e) => iniziaGesto(stato, insieme, riquadro, el, "sposta", e));

        for (const lato of LATI) {
            const maniglia = document.createElement("span");
            maniglia.className = `insieme-maniglia ${lato}`;
            maniglia.addEventListener("pointerdown", (e) => iniziaGesto(stato, insieme, riquadro, el, lato, e));
            el.appendChild(maniglia);
        }
        // Prima delle catture del disegno: un riquadro nuovo si disegna anche sopra quelli che ci sono.
        insieme.livello.insertBefore(el, insieme.cattura);
    }
    posizionaRettangoli(insieme);
}

function posizionaRettangoli(insieme) {
    if (!insieme.mappa || !insieme.livello) {
        return;
    }
    for (const el of insieme.livello.querySelectorAll(".insieme-riquadro")) {
        const riquadro = (insieme.dati.riquadri ?? []).find((r) => String(r.chiave) === el.dataset.chiave);
        if (riquadro && insieme.gesto?.el !== el) {
            mettiRettangolo(el, pixelDaBbox(insieme.mappa, riquadro.bbox));
        }
    }
}

/// Spostamento (dall'etichetta) o ridimensionamento (da una maniglia): il rettangolo segue il puntatore
/// in pixel; al rilascio la zona nuova va a Blazor.
function iniziaGesto(stato, insieme, riquadro, el, tipo, e) {
    if (e.button !== 0 || insieme.cattura) {
        return;
    }
    e.preventDefault();
    e.stopPropagation();
    const presa = e.currentTarget;
    presa.setPointerCapture(e.pointerId);
    const partenza = pixelDaBbox(insieme.mappa, riquadro.bbox);
    const x0 = e.clientX;
    const y0 = e.clientY;
    let attuale = partenza;
    insieme.gesto = { el };
    el.classList.add("in-movimento");

    const muovi = (ev) => {
        const dx = ev.clientX - x0;
        const dy = ev.clientY - y0;
        let { x, y, w, h } = partenza;
        if (tipo === "sposta") {
            x += dx;
            y += dy;
        } else {
            if (tipo.includes("w")) {
                const d = Math.min(dx, w - LATO_MINIMO_PX);
                x += d;
                w -= d;
            }
            if (tipo.includes("e")) {
                w = Math.max(LATO_MINIMO_PX, w + dx);
            }
            if (tipo.includes("n")) {
                const d = Math.min(dy, h - LATO_MINIMO_PX);
                y += d;
                h -= d;
            }
            if (tipo.includes("s")) {
                h = Math.max(LATO_MINIMO_PX, h + dy);
            }
        }
        attuale = { x, y, w, h };
        mettiRettangolo(el, attuale);
    };
    const fine = () => {
        presa.removeEventListener("pointermove", muovi);
        presa.removeEventListener("pointerup", fine);
        presa.removeEventListener("pointercancel", fine);
        insieme.gesto = null;
        el.classList.remove("in-movimento");
        if (attuale === partenza) {
            return;
        }
        // La zona nuova subito, così il rettangolo non torna indietro mentre Blazor risponde.
        riquadro.bbox = bboxDaPixel(insieme.mappa, attuale);
        stato.dotNetRef.invokeMethodAsync("OnZonaCambiata", riquadro.chiave, riquadro.bbox);
    };
    presa.addEventListener("pointermove", muovi);
    presa.addEventListener("pointerup", fine);
    presa.addEventListener("pointercancel", fine);
}

function rettangoloDa(a, b) {
    return { x: Math.min(a.x, b.x), y: Math.min(a.y, b.y), w: Math.abs(a.x - b.x), h: Math.abs(a.y - b.y) };
}

function mettiRettangolo(el, r) {
    el.style.left = `${r.x}px`;
    el.style.top = `${r.y}px`;
    el.style.width = `${r.w}px`;
    el.style.height = `${r.h}px`;
}

function pixelDaBbox(mappa, bbox) {
    const a = mappa.project([bbox[0], bbox[3]]);
    const b = mappa.project([bbox[2], bbox[1]]);
    return { x: a.x, y: a.y, w: b.x - a.x, h: b.y - a.y };
}

function bboxDaPixel(mappa, r) {
    const a = mappa.unproject([r.x, r.y]);
    const b = mappa.unproject([r.x + r.w, r.y + r.h]);
    const tondo = (v) => Math.round(v * 1e7) / 1e7;
    return [tondo(Math.min(a.lng, b.lng)), tondo(Math.min(a.lat, b.lat)), tondo(Math.max(a.lng, b.lng)), tondo(Math.max(a.lat, b.lat))];
}

// ---------- Modo manuale: layout nel browser e su file ----------

/// Il layout salvato con questa chiave o, se non c'è, l'ultimo dell'azienda (con un altro tematismo).
export function leggiLayout(chiave, chiaveRiserva) {
    try {
        const testo = localStorage.getItem(chiave);
        if (testo) {
            return { testo, riserva: false };
        }
        const riserva = chiaveRiserva ? localStorage.getItem(chiaveRiserva) : null;
        return riserva ? { testo: riserva, riserva: true } : null;
    } catch {
        return null; // localStorage spento (navigazione privata): si lavora senza
    }
}

export function scriviLayout(chiave, chiaveRiserva, testo) {
    try {
        localStorage.setItem(chiave, testo);
        if (chiaveRiserva) {
            localStorage.setItem(chiaveRiserva, testo);
        }
        return true;
    } catch {
        return false;
    }
}

/// «Salva layout»: il JSON come file scaricato.
export function scaricaLayout(nomeFile, testo) {
    const url = URL.createObjectURL(new Blob([testo], { type: "application/json" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = nomeFile;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}

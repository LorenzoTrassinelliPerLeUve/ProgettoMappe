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

/// opzioni: le stesse sorgenti della mappa (ConfigurazioneMappa), con basemap, riserva e imagery predefinite.
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
        basi: new Map()
    };
    fogli.set(foglioId, stato);

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
            const blob = await componiFoglio(document.getElementById(foglioId));
            consegnaImmagine(blob, `${pulsante.dataset.salvaPng}-${dataPerFile(new Date())}.png`, scheda);
        } catch (errore) {
            console.warn("[stampa] PNG non creato", errore);
            scheda?.close();
            dotNetRef.invokeMethodAsync("OnPngNonSalvato");
        }
    });
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
        fogli.delete(foglioId);
    }
    document.getElementById(ID_STILE_PAGINA)?.remove();
}

/// dati: { satellite, colori (null = nessun tematismo), classi { IdVigneto: classe }, numeri { IdVigneto: n },
/// riquadri: [{ id (elemento), ids, bbox }] }. Un disegno nuovo (generazione più alta) ferma il precedente.
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
    const margine = Math.max(8, Math.round(Math.min(larghezza, altezza) * MARGINE_RIQUADRO));
    const ids = new Set(riquadro.ids);
    const features = stato.geojson.features.filter((f) => ids.has(f.id)).map((f) => {
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
        await new Promise((fatto, errore) => {
            immagine.onload = fatto;
            immagine.onerror = () => errore(new Error("immagine del riquadro non caricata"));
            immagine.src = url;
        });
        contenitore.replaceChildren(immagine);

        for (const e of etichette) {
            if (e.numero === undefined || e.x < 0 || e.x > 100 || e.y < 0 || e.y > 100) {
                continue;
            }
            const numero = document.createElement("span");
            numero.className = "riquadro-numero";
            numero.style.left = `${e.x}%`;
            numero.style.top = `${e.y}%`;
            numero.textContent = String(e.numero);
            contenitore.appendChild(numero);
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
    } finally {
        mappa.remove();
    }
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

/// Il foglio com'è a video, ridisegnato su un canvas: sfondi, bordi, immagini dei riquadri, frecce del
/// nord (SVG) e testi, carattere per carattere nella posizione calcolata dal browser (a capo, spaziature
/// e maiuscole compresi). Gli elementi con overflow nascosto tagliano quello che contengono.
async function componiFoglio(foglio) {
    await document.fonts?.ready;
    const box = foglio.getBoundingClientRect();
    const canvas = document.createElement("canvas");
    canvas.width = Math.round(box.width * SCALA_PNG);
    canvas.height = Math.round(box.height * SCALA_PNG);
    const ctx = canvas.getContext("2d");
    ctx.scale(SCALA_PNG, SCALA_PNG);
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

    return new Promise((fatto, errore) =>
        canvas.toBlob((blob) => blob ? fatto(blob) : errore(new Error("toBlob vuoto")), "image/png"));
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

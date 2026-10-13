// Motore cartografico MapLibre, provider-neutral: basemap, imagery e terreno arrivano da Blazor
// come sorgenti già validate (ConfigurazioneMappa), qui applicate in modo generico. Nessun
// provider, URL o attribuzione è scritto in questo file. Navigazione sempre libera: il
// fitBounds/flyTo è solo un aiuto, dopo l'animazione pan/zoom/rotate/pitch restano all'utente.
//
// Bootstrap: la mappa nasce con uno style interno senza rete (STYLE_INTERNO), quindi vigneti,
// elenco, pannello e selezione funzionano anche se tutti i provider esterni sono irraggiungibili.
// Poi si tenta la basemap configurata: provider → riserva (fallback) → style interno.
//
// Lo stato applicativo (FeatureCollection, indice IdVigneto → Feature con bbox dal server,
// selezione, imagery e terreno scelti) vive qui, fuori dallo style MapLibre: dopo ogni cambio di
// style applicaOverlay() lo ripristina. La selezione non dipende mai dalle feature renderizzate.

const mappe = new Map();

const ID_STYLE_INTERNO = "interno";
const STYLE_INTERNO = {
    version: 8,
    name: "interno",
    sources: {},
    layers: [{ id: "sfondo-interno", type: "background", paint: { "background-color": "#eef1f2" } }]
};

const SOURCE_VIGNETI = "vigneti";
const SOURCE_IMAGERY = "overlay-imagery";
const LAYER_IMAGERY = "overlay-imagery";
const SOURCE_TERRENO = "overlay-terreno";
const LAYER_VIGNETI_FILL = "vigneti-fill";
const LAYER_VIGNETI_OUTLINE = "vigneti-outline";
const MAX_AVVISI_CONSOLE_PER_SORGENTE = 3;
// Inclinazione portata dal pulsante 3D (60 = massimo predefinito di MapLibre).
const PITCH_3D = 60;
// Tessere di sfondo fallite di fila (senza nessuna riuscita in mezzo) prima di avvisare l'utente:
// una tessera mancante ogni tanto (es. zoom oltre la copertura) non è un guasto.
const SOGLIA_ERRORI_SFONDO = 3;
const ATTESA_INQUADRATURA_MS = 400;
// Area di tocco intorno al dito / al mouse per scegliere un vigneto (vedi vignetoNelPunto).
const RAGGIO_TOCCO_PX = 16;
const RAGGIO_MOUSE_PX = 3;
// Pannelli dal basso (vedi collegaFogli): trascinati giù oltre questa quota, o con un colpo veloce, si chiudono.
const SOGLIA_CHIUSURA_PX = 90;
const VELOCITA_CHIUSURA = 0.5; // px/ms
// Colori del marchio Per Le Uve / 4Grapes (verde lime) per i vigneti senza tematismo.
const COLORE_VIGNETO = "#8dc63e";
const COLORE_BORDO_VIGNETO = "#4f7a1f";
const COLORE_SELEZIONE = "#eae711";
const COLORE_HOVER = "#3c3950";

// «Dove sono»: punto e cerchio di precisione sopra i vigneti, attesa massima del primo punto.
const SOURCE_POSIZIONE = "posizione";
const LAYER_POSIZIONE_PRECISIONE = "posizione-precisione";
const LAYER_POSIZIONE_BORDO = "posizione-precisione-bordo";
const LAYER_POSIZIONE_PUNTO = "posizione-punto";
const COLORE_POSIZIONE = "#1a73e8";
const ATTESA_POSIZIONE_MS = 10000;
const ZOOM_POSIZIONE = 17;
const CHIAVE_SATELLITE = "mappe.satellite";

export function creaMappa(elementId, opzioni, dotNetRef) {
    const stato = {
        mappa: null,
        elementId,
        dotNetRef,
        // Vigneto sotto il mouse (mappa o elenco), vedi evidenziaHover.
        idHover: null,
        sorgenti: new Map((opzioni.sorgenti ?? []).map((s) => [s.id, s])),
        basemapFallbackId: opzioni.basemapFallbackId ?? null,
        timeoutStyleMs: opzioni.timeoutStyleMs ?? 10000,
        terrainId: opzioni.terrainId ?? null,
        // Valore unico deciso in C# (MapOptions.TerrainExaggerationPredefinita): nessun default qui.
        terrainExaggeration: opzioni.terrainExaggeration,

        // Dati applicativi, indipendenti dallo style.
        geojson: null,
        indice: null,
        vignetoAttivoId: null,
        selezionePendente: null,
        primoFitFatto: false,
        timerVista: null,

        // Scelte cartografiche correnti.
        basemapId: null,
        imageryId: null,
        terrenoAttivo: false,
        // Tematismo sui vigneti: { colori: [...], classi: { IdVigneto: indiceColore } } oppure null.
        tematismo: null,
        // Vigneti visibili (filtri Vigna/Vitigno): array di IdVigneto oppure null = tutti.
        filtroIds: null,

        // Caricamento style: ogni tentativo ha una generazione; conta solo l'ultimo.
        generazione: 0,
        inApplicazione: null,
        timerStyle: null,
        abortStyle: null,
        styleCaricato: false,
        avvisoPendente: opzioni.avvisoIniziale ?? null,
        attribuzione: null,
        interazioniRegistrate: false,

        // Diagnostica (benchmark): richieste per host/tipo e risorse fallite per sorgente.
        richieste: {},
        erroriRisorse: {},

        // Avviso "mappe di sfondo non rispondono": errori di fila per source e source in errore.
        erroriSfondo: new Map(),
        sfondoKo: new Set(),
        sfondoAvvisato: false,

        // «Dove sono»: watchPosition attivo, ultima posizione { lon, lat, precisione } e ultimo esito
        // mandato a Blazor (si rimanda solo se cambia).
        posizioneWatch: null,
        posizione: null,
        esitoPosizione: null
    };

    const mappa = new maplibregl.Map({
        container: elementId,
        style: STYLE_INTERNO,
        center: opzioni.centro,
        zoom: opzioni.zoom,
        pitch: 0,
        bearing: 0,
        antialias: true,
        attributionControl: false,
        transformRequest: (url, tipoRisorsa) => {
            contaRichiesta(stato, url, tipoRisorsa);
            return { url };
        }
    });
    stato.mappa = mappa;

    mappa.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), "top-right");
    aggiornaAttribuzione(stato, null);

    mappa.on("style.load", () => onStyleCaricato(stato));

    // Errori di singole risorse (tile, glyph, sprite...): registrati e, se le tessere di sfondo
    // falliscono di fila, un avviso all'utente. MAI motivo di fallback: il fallimento dello style
    // è gestito a parte in caricaBasemap (fetch + timeout).
    mappa.on("error", (e) => {
        registraErroreRisorsa(stato, e);
        erroreTesseraSfondo(stato, e);
    });
    mappa.on("data", (e) => tesseraSfondoArrivata(stato, e));

    // Inquadratura per il link della pagina: a fine movimento, con un attimo di attesa così
    // un'animazione o più gesti di fila danno un solo aggiornamento dell'indirizzo.
    mappa.on("moveend", () => {
        clearTimeout(stato.timerVista);
        stato.timerVista = setTimeout(() => {
            const centro = mappa.getCenter();
            stato.dotNetRef?.invokeMethodAsync("OnInquadratura", centro.lng, centro.lat, mappa.getZoom());
        }, ATTESA_INQUADRATURA_MS);
    });

    mappe.set(elementId, stato);

    // Dopo il primo style.load (interno) si tenta la basemap configurata, a meno che nel
    // frattempo non sia già partito un tentativo (es. scelta manuale): quello ha la precedenza.
    mappa.once("style.load", () => {
        if (stato.generazione > 0) {
            return;
        }

        if (opzioni.basemapId) {
            caricaBasemap(stato, opzioni.basemapId);
        } else {
            notificaBasemap(stato, ID_STYLE_INTERNO, "interno");
        }
    });
}

// ---------- Caricamento basemap: provider → riserva → style interno ----------

/// Scarica lo style (così il suo fallimento è distinguibile dagli errori delle singole tile), lo
/// applica e attende style.load entro il timeout. Un nuovo tentativo invalida i precedenti.
async function caricaBasemap(stato, id) {
    const generazione = ++stato.generazione;
    clearTimeout(stato.timerStyle);
    stato.abortStyle?.abort();

    const sorgente = id === ID_STYLE_INTERNO ? null : stato.sorgenti.get(id);
    if (!sorgente || sorgente.ruolo !== "Basemap") {
        applicaStyle(stato, generazione, ID_STYLE_INTERNO, STYLE_INTERNO);
        return;
    }

    const abort = new AbortController();
    stato.abortStyle = abort;
    stato.timerStyle = setTimeout(() => {
        abort.abort();
        basemapFallita(stato, generazione, id, "timeout");
    }, stato.timeoutStyleMs);

    try {
        contaRichiesta(stato, sorgente.styleUrl, "Style");
        const risposta = await fetch(sorgente.styleUrl, { signal: abort.signal });
        if (!risposta.ok) {
            throw new Error(`HTTP ${risposta.status}`);
        }

        const style = await risposta.json();
        if (style?.version !== 8 || !Array.isArray(style.layers) || typeof style.sources !== "object") {
            throw new Error("style non valido");
        }

        if (generazione !== stato.generazione) {
            return; // superato da un tentativo più recente
        }

        applicaStyle(stato, generazione, id, style);
    } catch (errore) {
        if (errore?.name !== "AbortError") {
            basemapFallita(stato, generazione, id, errore?.message ?? String(errore));
        }
    }
}

function applicaStyle(stato, generazione, id, style) {
    stato.inApplicazione = { generazione, id };
    stato.styleCaricato = false;
    dimenticaSfondo(stato, null); // nuovo style, nuove source: si riparte da zero
    stato.mappa.setStyle(style, { diff: false });
}

function onStyleCaricato(stato) {
    const applicato = stato.inApplicazione;
    stato.styleCaricato = true;

    if (applicato === null) {
        stato.basemapId = ID_STYLE_INTERNO; // bootstrap
    } else if (applicato.generazione === stato.generazione) {
        clearTimeout(stato.timerStyle);
        stato.basemapId = applicato.id;
        aggiornaAttribuzione(stato, stato.sorgenti.get(applicato.id)?.attribution ?? null);

        const avviso = stato.avvisoPendente;
        stato.avvisoPendente = null;
        notificaBasemap(stato, applicato.id, avviso);
    }

    applicaOverlay(stato);
}

function basemapFallita(stato, generazione, id, motivo) {
    if (generazione !== stato.generazione) {
        return;
    }

    clearTimeout(stato.timerStyle);
    console.warn(`[mappa] basemap '${id}' non disponibile (${motivo})`);

    if (stato.basemapFallbackId && id !== stato.basemapFallbackId) {
        stato.avvisoPendente = "riserva";
        caricaBasemap(stato, stato.basemapFallbackId);
    } else {
        stato.avvisoPendente = "interno";
        caricaBasemap(stato, ID_STYLE_INTERNO);
    }
}

function notificaBasemap(stato, id, avviso) {
    stato.dotNetRef?.invokeMethodAsync("OnStatoBasemap", id, avviso ?? null);
}

/// Attribuzione della basemap dalla configurazione; quelle di imagery e terreno arrivano dal
/// campo attribution delle rispettive source (MapLibre le mostra solo se la source è attiva).
function aggiornaAttribuzione(stato, testo) {
    if (stato.attribuzione) {
        stato.mappa.removeControl(stato.attribuzione);
    }

    stato.attribuzione = new maplibregl.AttributionControl({
        compact: true,
        customAttribution: testo ?? undefined
    });
    stato.mappa.addControl(stato.attribuzione, "bottom-right");
}

// ---------- Overlay applicativi (idempotente, dopo ogni style.load) ----------

/// Ordine: imagery (sopra la basemap) → terreno → source vigneti → layer vigneti → selezione.
function applicaOverlay(stato) {
    const { mappa } = stato;
    if (!stato.styleCaricato) {
        return;
    }

    aggiungiImagery(stato);
    aggiornaTerreno(stato);

    if (stato.geojson) {
        if (mappa.getSource(SOURCE_VIGNETI)) {
            mappa.getSource(SOURCE_VIGNETI).setData(stato.geojson);
        } else {
            // Nessun promoteId: MapLibre usa direttamente Feature.id (= IdVigneto) per il feature-state.
            mappa.addSource(SOURCE_VIGNETI, { type: "geojson", data: stato.geojson });
        }

        if (!mappa.getLayer(LAYER_VIGNETI_FILL)) {
            mappa.addLayer({
                id: LAYER_VIGNETI_FILL,
                type: "fill",
                source: SOURCE_VIGNETI,
                paint: {
                    "fill-color": COLORE_VIGNETO,
                    "fill-opacity": ["case", ["boolean", ["feature-state", "selezionato"], false], 0.55, 0.3]
                }
            });
        }

        if (!mappa.getLayer(LAYER_VIGNETI_OUTLINE)) {
            mappa.addLayer({
                id: LAYER_VIGNETI_OUTLINE,
                type: "line",
                source: SOURCE_VIGNETI,
                paint: {
                    // Selezionato giallo, sotto il mouse (mappa o elenco) viola, gli altri verde scuro.
                    "line-color": ["case", ["boolean", ["feature-state", "selezionato"], false], COLORE_SELEZIONE,
                        ["boolean", ["feature-state", "hover"], false], COLORE_HOVER, COLORE_BORDO_VIGNETO],
                    "line-width": ["case", ["boolean", ["feature-state", "selezionato"], false], 3,
                        ["boolean", ["feature-state", "hover"], false], 3, 1.5]
                }
            });
        }

        registraInterazioni(stato);
        applicaTematismo(stato);
        applicaFiltro(stato);

        if (stato.vignetoAttivoId !== null) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id: stato.vignetoAttivoId }, { selezionato: true });
        }
    }

    disegnaPosizione(stato, true);
}

/// Colori dei vigneti: senza tematismo il verde di base; con tematismo il colore della classe
/// (feature-state "tema" = indice in colori; -1 = nessun dato → grigio tenue; -2 = fuori perimetro
/// BIGOT → quasi trasparente, resta il bordo). Le classi arrivano
/// già calcolate da Blazor: qui nessuna logica sui parametri. Il feature-state va riapplicato dopo
/// ogni setStyle (la source viene ricreata), per questo è chiamato da applicaOverlay.
function applicaTematismo(stato) {
    const { mappa } = stato;
    if (!mappa.getLayer(LAYER_VIGNETI_FILL) || !mappa.getSource(SOURCE_VIGNETI)) {
        return;
    }

    const tema = stato.tematismo;
    const selezionato = ["boolean", ["feature-state", "selezionato"], false];
    const hover = ["boolean", ["feature-state", "hover"], false];

    if (!tema) {
        mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-color", COLORE_VIGNETO);
        mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-opacity", ["case", selezionato, 0.55, hover, 0.5, 0.3]);
        return;
    }

    const classe = ["coalesce", ["feature-state", "tema"], -1];
    const colore = ["match", classe];
    tema.colori.forEach((c, i) => colore.push(i, c));
    colore.push("#9e9e9e");

    mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-color", colore);
    mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-opacity",
        ["case", selezionato, 0.9, [">=", classe, 0], ["case", hover, 0.9, 0.75], ["==", classe, -2], 0.05, 0.25]);

    for (const id of stato.indice?.keys() ?? []) {
        mappa.setFeatureState({ source: SOURCE_VIGNETI, id }, { tema: tema.classi[id] ?? -1 });
    }
}

function specSorgenteRaster(sorgente) {
    const spec = { type: sorgente.tipo };
    if (sorgente.tileUrl) {
        spec.tiles = [sorgente.tileUrl];
    } else {
        spec.url = sorgente.tileJsonUrl;
    }

    if (sorgente.tileSize) spec.tileSize = sorgente.tileSize;
    if (sorgente.minZoom !== null && sorgente.minZoom !== undefined) spec.minzoom = sorgente.minZoom;
    if (sorgente.maxZoom !== null && sorgente.maxZoom !== undefined) spec.maxzoom = sorgente.maxZoom;
    if (sorgente.attribution) spec.attribution = sorgente.attribution;
    if (sorgente.encoding) spec.encoding = sorgente.encoding;
    if (sorgente.bounds?.length === 4) spec.bounds = sorgente.bounds;
    return spec;
}

function aggiungiImagery(stato) {
    const { mappa } = stato;
    const sorgente = stato.imageryId ? stato.sorgenti.get(stato.imageryId) : null;
    if (!sorgente || mappa.getSource(SOURCE_IMAGERY)) {
        return;
    }

    mappa.addSource(SOURCE_IMAGERY, specSorgenteRaster(sorgente));
    // Sopra la basemap e sotto i vigneti (se già presenti).
    mappa.addLayer({ id: LAYER_IMAGERY, type: "raster", source: SOURCE_IMAGERY },
        mappa.getLayer(LAYER_VIGNETI_FILL) ? LAYER_VIGNETI_FILL : undefined);
}

function rimuoviImagery(stato) {
    const { mappa } = stato;
    if (mappa.getLayer(LAYER_IMAGERY)) mappa.removeLayer(LAYER_IMAGERY);
    if (mappa.getSource(SOURCE_IMAGERY)) mappa.removeSource(SOURCE_IMAGERY);
    dimenticaSfondo(stato, SOURCE_IMAGERY);
}

/// Terreno ON: source raster-dem + setTerrain. OFF: setTerrain(null) e rimozione della source
/// (nessuna richiesta DEM finché resta spento). Pitch e bearing non vengono mai toccati.
function aggiornaTerreno(stato) {
    const { mappa } = stato;
    const sorgente = stato.terrainId ? stato.sorgenti.get(stato.terrainId) : null;

    if (stato.terrenoAttivo && sorgente) {
        if (!mappa.getSource(SOURCE_TERRENO)) {
            mappa.addSource(SOURCE_TERRENO, specSorgenteRaster(sorgente));
        }
        mappa.setTerrain({ source: SOURCE_TERRENO, exaggeration: stato.terrainExaggeration });
    } else {
        if (mappa.getTerrain()) mappa.setTerrain(null);
        if (mappa.getSource(SOURCE_TERRENO)) mappa.removeSource(SOURCE_TERRENO);
    }
}

// ---------- API verso Blazor ----------

/// adatta = true: porta la camera sulla FeatureCollection anche se non è il primo caricamento
/// (es. cambio azienda). Il filtro e la selezione della FeatureCollection precedente decadono.
/// vista = { lon, lat, zoom } dal link della pagina: la camera va lì invece che sui vigneti.
export function mostraVigneti(elementId, geojson, adatta = false, vista = null) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    stato.geojson = geojson;
    stato.indice = new Map((geojson.features ?? []).map((feature) => [feature.id, feature]));
    if (stato.vignetoAttivoId !== null && !stato.indice.has(stato.vignetoAttivoId)) {
        stato.vignetoAttivoId = null;
    }
    // Nuovi vigneti: il filtro dei precedenti non vale più (Blazor riapplica quello nuovo).
    stato.filtroIds = null;
    stato.idHover = null;
    if (vista) {
        stato.primoFitFatto = true;
        stato.mappa.jumpTo({ center: [vista.lon, vista.lat], zoom: vista.zoom });
    } else if (adatta) {
        stato.primoFitFatto = false;
    }
    applicaOverlay(stato);

    // Zoom iniziale: tutti i poligoni dell'azienda con un margine (bbox complessivo dal server, che
    // comprende anche gruppi di vigneti lontani); maxZoom per un vigneto solo, piccolo.
    if (!stato.primoFitFatto && geojson.bbox) {
        stato.primoFitFatto = true;
        stato.mappa.fitBounds(bboxInBounds(geojson.bbox), { padding: 60, maxZoom: 17, duration: 0 });
    }

    if (stato.selezionePendente !== null) {
        const id = stato.selezionePendente;
        stato.selezionePendente = null;
        selezionaVigneto(elementId, id);
    }

    // «Dove sono» acceso: l'ultima posizione si rivaluta sui vigneti nuovi.
    stato.esitoPosizione = null;
    valutaPosizione(stato);
}

/// Selezione da elenco (Blazor → mappa): evidenzia il vigneto e porta la camera sul suo bbox,
/// letto dall'indice locale. Vigneto senza geometria: nessuna evidenziazione, camera ferma.
/// muoviCamera = false: solo evidenziazione (scheda aperta da un link con la sua inquadratura).
export function selezionaVigneto(elementId, vignetoId, muoviCamera = true) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    if (stato.indice === null) {
        stato.selezionePendente = vignetoId;
        return;
    }

    // null = deseleziona (es. il vigneto selezionato è uscito dal filtro): nessun movimento camera.
    evidenzia(stato, vignetoId);
    if (vignetoId === null || !muoviCamera) {
        return;
    }

    const bbox = stato.indice.get(vignetoId)?.bbox;
    if (bbox) {
        portaCameraSu(stato.mappa, bbox);
    }
}

/// id della basemap (o "interno"): cambio manuale, ad es. dal selettore sviluppatore.
export function impostaBasemap(elementId, id) {
    const stato = mappe.get(elementId);
    if (stato) {
        stato.avvisoPendente = null;
        caricaBasemap(stato, id);
    }
}

/// id di una sorgente Imagery, oppure null per tornare alla sola basemap. Nessun setStyle.
export function impostaImagery(elementId, id) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    stato.imageryId = id && stato.sorgenti.get(id)?.ruolo === "Imagery" ? id : null;
    if (stato.styleCaricato) {
        rimuoviImagery(stato);
        aggiungiImagery(stato);
    }
}

/// ids = elenco di IdVigneto da mostrare, oppure null per mostrarli tutti. adatta = true porta la
/// camera sull'insieme filtrato (unione dei bbox già calcolati dal server). I dati restano tutti
/// nell'indice: il filtro è solo visivo, quindi selezione e tematismo non vanno ricalcolati.
export function impostaFiltroVigneti(elementId, ids, adatta = false) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    stato.filtroIds = Array.isArray(ids) ? ids : null;
    if (stato.styleCaricato) {
        applicaFiltro(stato);
    }

    if (adatta) {
        inquadraVigneti(elementId);
    }
}

/// Pulsante «Inquadra»: la camera sull'insieme dei vigneti visibili (filtrati, o tutti), unione dei
/// bbox già calcolati dal server. Vigneti in gruppi lontani: il bbox li comprende tutti.
export function inquadraVigneti(elementId) {
    const stato = mappe.get(elementId);
    const bbox = (stato?.filtroIds ?? [...(stato?.indice?.keys() ?? [])])
        .map((id) => stato.indice?.get(id)?.bbox)
        .filter(Boolean)
        .reduce((a, b) => (a ? [Math.min(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[2], b[2]), Math.max(a[3], b[3])] : b), null);
    if (bbox) {
        portaCameraSu(stato.mappa, bbox);
    }
}

function applicaFiltro(stato) {
    const { mappa } = stato;
    const filtro = stato.filtroIds ? ["in", ["id"], ["literal", stato.filtroIds]] : null;
    for (const layer of [LAYER_VIGNETI_FILL, LAYER_VIGNETI_OUTLINE]) {
        if (mappa.getLayer(layer)) {
            mappa.setFilter(layer, filtro);
        }
    }
}

/// tematismo = { colori: ["#rrggbb", ...], classi: { IdVigneto: indice } } oppure null per tornare
/// al colore di base. Nessun setStyle: cambiano solo paint e feature-state.
export function impostaTematismo(elementId, tematismo) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    stato.tematismo = tematismo && Array.isArray(tematismo.colori) ? tematismo : null;
    if (stato.styleCaricato) {
        applicaTematismo(stato);
    }
}

export function impostaTerreno(elementId, attivo) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    const cambiato = stato.terrenoAttivo !== !!attivo;
    stato.terrenoAttivo = !!attivo;
    if (stato.styleCaricato) {
        aggiornaTerreno(stato);
    }

    // Il rilievo si vede solo inclinando la mappa: 3D acceso → pitch PITCH_3D, spento → piatta.
    // È un'animazione breve (easeTo), interrompibile: poi pitch e rotazione restano all'utente.
    if (cambiato && stato.mappa) {
        stato.mappa.easeTo({ pitch: stato.terrenoAttivo ? PITCH_3D : 0, duration: 800 });
    }
}

/// Pulsante «Copia link»: copia l'indirizzo della pagina (Blazor lo tiene aggiornato con lo stato
/// della mappa) dentro il click stesso, perché Safari/iOS permettono di scrivere negli appunti solo
/// lì e non dopo un giro sul server. L'esito torna a Blazor per la conferma.
/// Pulsanti «Copia link» dentro il contenitore (barra della mappa e scheda del vigneto, che
/// Blazor crea e toglie): un ascoltatore solo, sul contenitore. data-copia-link = quale pulsante.
export function collegaCopiaLink(contenitoreId, dotNetRef) {
    document.getElementById(contenitoreId)?.addEventListener("click", async (e) => {
        const pulsante = e.target.closest?.("[data-copia-link]");
        if (!pulsante) {
            return;
        }
        const riuscito = await copiaTesto(location.href);
        dotNetRef.invokeMethodAsync("OnLinkCopiato", riuscito, pulsante.dataset.copiaLink);
    });
}

/// Pannelli dal basso del telefono ([data-foglio]: Filtri, mappe tematiche, scheda del vigneto). Si
/// trascinano dalla maniglia ([data-maniglia]) seguendo il dito; lasciati giù oltre una soglia (o con
/// un colpo veloce) si chiudono col loro pulsante [data-chiudi], così lo stato resta a Blazor. La
/// scheda del vigneto ([data-espandibile]) ha due altezze: su = espansa, giù = ridotta, poi chiusa;
/// un tocco sulla maniglia o su [data-espandi] commuta (data-espansa, gestito solo qui).
/// [data-apre-foglio="id"] apre il pannello già nel tocco e mette il cursore nel suo [data-fuoco]:
/// iOS apre la tastiera solo dentro il gesto, non dopo un giro sul server.
/// Ascoltatori sul contenitore: i pannelli li crea e li toglie Blazor.
export function collegaFogli(contenitoreId) {
    const contenitore = document.getElementById(contenitoreId);
    if (!contenitore) {
        return;
    }

    let t = null;
    let fineTrascinamento = -Infinity;
    contenitore.addEventListener("pointerdown", (e) => {
        const maniglia = e.target.closest?.("[data-maniglia]");
        const foglio = maniglia?.closest("[data-foglio]");
        // I pulsanti dentro la maniglia (la X) restano pulsanti; su desktop niente pannelli dal basso.
        if (!foglio || e.button > 0 || e.target.closest("button:not([data-maniglia]), a, select, input") || !eTelefono()) {
            return;
        }
        maniglia.setPointerCapture(e.pointerId);
        t = {
            foglio, maniglia, id: e.pointerId, y0: e.clientY, t0: e.timeStamp, mosso: false,
            altezza0: foglio.getBoundingClientRect().height,
            espandibile: foglio.hasAttribute("data-espandibile")
        };
    });

    contenitore.addEventListener("pointermove", (e) => {
        if (!t || e.pointerId !== t.id) {
            return;
        }
        const dy = e.clientY - t.y0;
        if (!t.mosso && Math.abs(dy) < 6) {
            return;
        }
        t.mosso = true;
        t.foglio.classList.add("trascinato");
        if (dy >= 0) {
            t.foglio.style.height = "";
            t.foglio.style.transform = `translateY(${dy}px)`;
        } else if (t.espandibile) {
            // La scheda sta in .mappa-destra (display: contents, senza altezza): conta la pagina della mappa.
            const massima = t.foglio.closest(".mappa-layout").getBoundingClientRect().height - 8;
            t.foglio.style.transform = "";
            t.foglio.style.maxHeight = "none";
            t.foglio.style.height = `${Math.min(massima, t.altezza0 - dy)}px`;
        }
    });

    const fine = (e) => {
        if (!t || e.pointerId !== t.id) {
            return;
        }
        const { foglio, maniglia, y0, t0, mosso, altezza0, espandibile } = t;
        t = null;
        if (maniglia.hasPointerCapture?.(e.pointerId)) {
            maniglia.releasePointerCapture(e.pointerId);
        }
        // Gesto interrotto dal browser (es. diventa uno scroll): il pannello torna com'era.
        const dy = e.type === "pointercancel" ? 0 : e.clientY - y0;
        const veloce = dy / Math.max(1, e.timeStamp - t0) > VELOCITA_CHIUSURA;
        foglio.classList.remove("trascinato");

        if (!mosso) {
            // Un tocco: sulla maniglia-pulsante ([data-espandi]) ci pensa il suo click.
            if (espandibile && e.type !== "pointercancel" && !maniglia.hasAttribute("data-espandi")) {
                impostaEspansa(foglio, foglio.dataset.espansa !== "true");
            }
            return;
        }
        fineTrascinamento = e.timeStamp;

        const espansa = espandibile && foglio.dataset.espansa === "true";
        // Espansa: giù riduce, giù di molto chiude. Ridotta o pannello semplice: giù chiude.
        if (dy > 0 && (espansa ? dy > altezza0 * 0.6 : dy > SOGLIA_CHIUSURA_PX || veloce)) {
            chiudiFoglio(foglio);
            return;
        }
        // Torna alle altezze del CSS (ridotta / espansa / aperto): niente altezze a metà.
        foglio.style.transform = "";
        foglio.style.height = "";
        foglio.style.maxHeight = "";
        if (espandibile && dy < 0) {
            impostaEspansa(foglio, true);
        } else if (espansa && (dy > SOGLIA_CHIUSURA_PX / 2 || veloce)) {
            impostaEspansa(foglio, false);
        }
    };
    contenitore.addEventListener("pointerup", fine);
    contenitore.addEventListener("pointercancel", fine);

    contenitore.addEventListener("click", (e) => {
        const espandi = e.target.closest?.("[data-espandi]");
        const foglioEspandi = espandi?.closest("[data-foglio]");
        // Il click che segue un trascinamento non commuta di nuovo.
        if (foglioEspandi && e.timeStamp - fineTrascinamento > 300) {
            impostaEspansa(foglioEspandi, foglioEspandi.dataset.espansa !== "true");
            return;
        }

        const apre = e.target.closest?.("[data-apre-foglio]");
        const foglio = apre && document.getElementById(apre.dataset.apreFoglio);
        if (foglio && !foglio.classList.contains("aperto") && eTelefono()) {
            foglio.classList.add("aperto"); // Blazor scrive la stessa classe subito dopo
            foglio.querySelector("[data-fuoco]")?.focus({ preventScroll: true });
        }

        // Scelto un risultato o un vigneto: il pannello si chiude, la tastiera anche.
        if (e.target.closest?.(".risultati-ricerca, .elenco-vigneti li") && eTelefono()) {
            document.activeElement?.blur?.();
        }
    }, true);

    contenitore.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && e.target.matches?.("[data-fuoco]") && eTelefono()) {
            e.target.blur();
        }
    });
}

function eTelefono() {
    return window.matchMedia("(max-width: 800px)").matches;
}

function impostaEspansa(foglio, espansa) {
    foglio.dataset.espansa = String(espansa);
    foglio.querySelectorAll("[data-espandi], .info-maniglia").forEach((p) => p.setAttribute("aria-expanded", String(espansa)));
}

/// Il pannello scende fuori dallo schermo, poi lo chiude il suo pulsante (Blazor). Un pannello con
/// la classe «aperto» la perde subito, così non risale nell'attesa della risposta del server.
function chiudiFoglio(foglio) {
    foglio.style.transition = "transform 0.2s ease";
    foglio.style.transform = "translateY(110%)";
    setTimeout(() => {
        const conClasse = foglio.classList.contains("aperto");
        foglio.classList.remove("aperto");
        foglio.querySelector("[data-chiudi]")?.click();
        const ripristina = () => {
            foglio.style.transition = "";
            foglio.style.transform = "";
            foglio.style.height = "";
            foglio.style.maxHeight = "";
        };
        // La scheda del vigneto sparisce col render di Blazor; se restasse, torna visibile.
        setTimeout(ripristina, conClasse ? 0 : 1500);
    }, 200);
}

async function copiaTesto(testo) {
    try {
        await navigator.clipboard.writeText(testo);
        return true;
    } catch {
        // navigator.clipboard manca fuori da https o senza permesso: metodo classico.
        const area = document.createElement("textarea");
        area.value = testo;
        area.setAttribute("readonly", "");
        area.style.position = "fixed";
        area.style.opacity = "0";
        document.body.appendChild(area);
        area.select();
        try {
            return document.execCommand("copy");
        } catch {
            return false;
        } finally {
            area.remove();
        }
    }
}

/// Porta in vista una voce dell'elenco scorrendo SOLO il riquadro che la contiene. Mai
/// scrollIntoView: scorrerebbe anche la pagina e la mappa (sul telefono, col pannello Filtri chiuso,
/// la mappa finiva fuori schermo). Pannello nascosto = niente da fare.
export function portaInVista(elementoId) {
    const voce = document.getElementById(elementoId);
    const pannello = voce?.closest(".vigneti-list");
    if (!voce || !pannello || !voce.checkVisibility?.({ visibilityProperty: true })) {
        return;
    }
    let riquadro = voce.parentElement;
    while (riquadro && riquadro !== pannello && riquadro.scrollHeight <= riquadro.clientHeight) {
        riquadro = riquadro.parentElement;
    }
    if (!riquadro) {
        return;
    }
    const r = riquadro.getBoundingClientRect();
    const v = voce.getBoundingClientRect();
    if (v.top < r.top) {
        riquadro.scrollTop -= r.top - v.top;
    } else if (v.bottom > r.bottom) {
        riquadro.scrollTop += v.bottom - r.bottom;
    }
}

// ---------- «Dove sono» (posizione del telefono) ----------

/// Acceso: chiede la posizione al browser e la segue (watchPosition) finché non si spegne. Il primo
/// punto porta la camera lì a zoom 17, poi la camera resta libera. Esito a Blazor con OnPosizione
/// (vigneto in cui si è, o il più vicino e la distanza), errori con OnPosizioneErrore: codici della
/// Geolocation API (1 permesso negato, 2 posizione non disponibile, 3 nessuna risposta entro 10 s),
/// 0 = il browser non può darla (niente https o niente geolocalizzazione). Dopo un errore è spento.
export function attivaPosizione(elementId) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    fermaPosizione(elementId);
    if (!window.isSecureContext || !navigator.geolocation) {
        stato.dotNetRef?.invokeMethodAsync("OnPosizioneErrore", 0);
        return;
    }

    let primo = true;
    stato.posizioneWatch = navigator.geolocation.watchPosition((p) => {
        const { longitude: lon, latitude: lat, accuracy } = p.coords;
        stato.posizione = { lon, lat, precisione: accuracy ?? 0 };
        disegnaPosizione(stato);
        if (primo) {
            primo = false;
            const { mappa } = stato;
            mappa.flyTo({ center: [lon, lat], zoom: ZOOM_POSIZIONE, bearing: mappa.getBearing(), pitch: mappa.getPitch(), duration: 1200 });
        }
        valutaPosizione(stato);
    }, (e) => {
        // Dopo il primo punto un segnale perso per un po' non spegne nulla: resta l'ultimo punto.
        if (!primo && e.code !== 1) {
            return;
        }
        fermaPosizione(elementId);
        stato.dotNetRef?.invokeMethodAsync("OnPosizioneErrore", e.code);
    }, { enableHighAccuracy: true, timeout: ATTESA_POSIZIONE_MS, maximumAge: 5000 });
}

/// Spento (secondo tocco, errore, altra pagina): niente più GPS, il punto sparisce.
export function fermaPosizione(elementId) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    fermaWatch(stato);
    stato.posizione = null;
    stato.esitoPosizione = null;
    disegnaPosizione(stato);
}

function fermaWatch(stato) {
    if (stato.posizioneWatch !== null) {
        navigator.geolocation?.clearWatch(stato.posizioneWatch);
        stato.posizioneWatch = null;
    }
}

/// Punto e cerchio di precisione, sopra i vigneti (anche dopo un cambio di style: lo chiama
/// applicaOverlay, con inCima = true). Senza posizione toglie layer e source.
function disegnaPosizione(stato, inCima = false) {
    const { mappa } = stato;
    if (!stato.styleCaricato) {
        return;
    }

    const layer = [LAYER_POSIZIONE_PRECISIONE, LAYER_POSIZIONE_BORDO, LAYER_POSIZIONE_PUNTO];
    if (!stato.posizione) {
        layer.filter((id) => mappa.getLayer(id)).forEach((id) => mappa.removeLayer(id));
        if (mappa.getSource(SOURCE_POSIZIONE)) mappa.removeSource(SOURCE_POSIZIONE);
        return;
    }

    const { lon, lat, precisione } = stato.posizione;
    const dati = {
        type: "FeatureCollection",
        features: [
            { type: "Feature", properties: {}, geometry: cerchio(lon, lat, precisione) },
            { type: "Feature", properties: {}, geometry: { type: "Point", coordinates: [lon, lat] } }
        ]
    };
    if (mappa.getSource(SOURCE_POSIZIONE)) {
        mappa.getSource(SOURCE_POSIZIONE).setData(dati);
    } else {
        mappa.addSource(SOURCE_POSIZIONE, { type: "geojson", data: dati });
    }

    const poligono = ["==", ["geometry-type"], "Polygon"];
    if (!mappa.getLayer(LAYER_POSIZIONE_PRECISIONE)) {
        mappa.addLayer({ id: LAYER_POSIZIONE_PRECISIONE, type: "fill", source: SOURCE_POSIZIONE, filter: poligono,
            paint: { "fill-color": COLORE_POSIZIONE, "fill-opacity": 0.15 } });
        mappa.addLayer({ id: LAYER_POSIZIONE_BORDO, type: "line", source: SOURCE_POSIZIONE, filter: poligono,
            paint: { "line-color": COLORE_POSIZIONE, "line-width": 1, "line-opacity": 0.6 } });
        mappa.addLayer({ id: LAYER_POSIZIONE_PUNTO, type: "circle", source: SOURCE_POSIZIONE, filter: ["==", ["geometry-type"], "Point"],
            paint: { "circle-radius": 7, "circle-color": COLORE_POSIZIONE, "circle-stroke-width": 2.5, "circle-stroke-color": "#ffffff" } });
    } else if (inCima) {
        // I vigneti aggiunti dopo (arrivati col GPS già acceso) finirebbero sopra il punto.
        layer.forEach((id) => mappa.moveLayer(id));
    }
}

/// Cerchio di raggio r metri attorno a lon/lat, come poligono (distanze approssimate, vanno bene a scala di campo).
function cerchio(lon, lat, r) {
    const kx = 111320 * Math.cos(lat * Math.PI / 180);
    const anello = [];
    for (let i = 0; i <= 64; i++) {
        const a = (i / 64) * 2 * Math.PI;
        anello.push([lon + (r * Math.cos(a)) / kx, lat + (r * Math.sin(a)) / 110540]);
    }
    return { type: "Polygon", coordinates: [anello] };
}

/// Il vigneto dell'azienda in cui cade il punto o, se nessuno, il più vicino con la distanza (a 10 m):
/// sui poligoni già caricati, nessuna chiamata all'Api. A Blazor solo se l'esito cambia; entrando in
/// un vigneto nuovo lo si evidenzia (Blazor apre la scheda).
function valutaPosizione(stato) {
    if (!stato.posizione || !stato.indice) {
        return;
    }

    const { lon, lat } = stato.posizione;
    // Metri per grado attorno al punto: piano locale, l'errore è trascurabile entro qualche km.
    const kx = 111320 * Math.cos(lat * Math.PI / 180);
    const ky = 110540;
    let dentro = null;
    let vicino = null;
    let minima = Infinity;
    for (const [id, feature] of stato.indice) {
        const d = distanzaDaPoligoni(feature.geometry, (c) => [(c[0] - lon) * kx, (c[1] - lat) * ky]);
        if (d === 0) {
            dentro = id;
            break;
        }
        if (d < minima) {
            minima = d;
            vicino = id;
        }
    }

    const esito = dentro !== null
        ? { dentro, vicino: null, metri: null }
        : { dentro: null, vicino, metri: vicino === null ? null : Math.round(minima / 10) * 10 };
    const prima = stato.esitoPosizione;
    if (prima && prima.dentro === esito.dentro && prima.vicino === esito.vicino && prima.metri === esito.metri) {
        return;
    }

    stato.esitoPosizione = esito;
    if (esito.dentro !== null && esito.dentro !== prima?.dentro) {
        evidenzia(stato, esito.dentro);
    }
    stato.dotNetRef?.invokeMethodAsync("OnPosizione", esito.dentro, esito.vicino, esito.metri);
}

/// Distanza in metri dall'origine (il punto) a un Polygon/MultiPolygon: 0 se il punto è dentro
/// (pari/dispari su tutti gli anelli, così i buchi restano fuori). Altre geometrie: Infinity.
function distanzaDaPoligoni(geometria, inMetri) {
    const poligoni = geometria?.type === "Polygon" ? [geometria.coordinates]
        : geometria?.type === "MultiPolygon" ? geometria.coordinates : [];
    let minima = Infinity;
    for (const anelli of poligoni) {
        let dentro = false;
        for (const anello of anelli) {
            const punti = anello.map(inMetri);
            for (let i = 0, j = punti.length - 1; i < punti.length; j = i++) {
                const [xa, ya] = punti[j];
                const [xb, yb] = punti[i];
                if ((ya > 0) !== (yb > 0) && 0 < xa + ((0 - ya) * (xb - xa)) / (yb - ya)) {
                    dentro = !dentro;
                }
                minima = Math.min(minima, distanzaDalSegmento(xa, ya, xb, yb));
            }
        }
        if (dentro) {
            return 0;
        }
    }
    return minima;
}

/// Distanza dall'origine al segmento (xa, ya)–(xb, yb).
function distanzaDalSegmento(xa, ya, xb, yb) {
    const dx = xb - xa;
    const dy = yb - ya;
    const lunghezza2 = dx * dx + dy * dy;
    const t = lunghezza2 === 0 ? 0 : Math.max(0, Math.min(1, -(xa * dx + ya * dy) / lunghezza2));
    return Math.hypot(xa + t * dx, ya + t * dy);
}

/// Mappa o satellite all'apertura: la scelta ricordata nel browser, altrimenti il satellite sul
/// telefono (in campo si riconoscono i filari) e la mappa sul computer.
export function satellitePredefinito() {
    try {
        const ricordato = localStorage.getItem(CHIAVE_SATELLITE);
        if (ricordato !== null) {
            return ricordato === "1";
        }
    } catch {
        // localStorage chiuso (navigazione privata): vale il predefinito.
    }
    return eTelefono();
}

export function ricordaSatellite(satellite) {
    try {
        localStorage.setItem(CHIAVE_SATELLITE, satellite ? "1" : "0");
    } catch {
        // Non si ricorda: alla prossima apertura vale il predefinito.
    }
}

/// Diagnostica per sviluppo/benchmark (dalla console): nessun URL completo, solo host e contatori.
export function diagnostica(elementId) {
    const stato = elementId ? mappe.get(elementId) : mappe.values().next().value;
    if (!stato) {
        return null;
    }

    const { mappa } = stato;
    return {
        basemap: stato.basemapId,
        imagery: stato.imageryId,
        terreno: stato.terrenoAttivo,
        terrenoApplicato: !!mappa.getTerrain(),
        selezionato: stato.vignetoAttivoId,
        vignetiIndicizzati: stato.indice?.size ?? 0,
        zoom: mappa.getZoom(),
        centro: mappa.getCenter().toArray(),
        pitch: mappa.getPitch(),
        bearing: mappa.getBearing(),
        inMovimento: mappa.isMoving(),
        // Ordine reale nello style (dal basso verso l'alto), solo per i layer applicativi.
        layerApplicativi: (mappa.getStyle()?.layers ?? []).map((l) => l.id)
            .filter((id) => [LAYER_IMAGERY, LAYER_VIGNETI_FILL, LAYER_VIGNETI_OUTLINE].includes(id)),
        layerTotali: mappa.getStyle()?.layers?.length ?? 0,
        tematismoClassi: stato.tematismo ? Object.keys(stato.tematismo.classi).length : null,
        filtroVigneti: stato.filtroIds ? stato.filtroIds.length : null,
        statoSelezionatoInMappa: stato.vignetoAttivoId !== null && mappa.getSource(SOURCE_VIGNETI)
            ? mappa.getFeatureState({ source: SOURCE_VIGNETI, id: stato.vignetoAttivoId })
            : null,
        richieste: { ...stato.richieste },
        erroriRisorse: { ...stato.erroriRisorse }
    };
}

export function azzeraDiagnostica(elementId) {
    const stato = elementId ? mappe.get(elementId) : mappe.values().next().value;
    if (stato) {
        stato.richieste = {};
        stato.erroriRisorse = {};
    }
}

export function distruggiMappa(elementId) {
    const stato = mappe.get(elementId);
    if (stato) {
        clearTimeout(stato.timerStyle);
        clearTimeout(stato.timerVista);
        fermaWatch(stato);
        stato.abortStyle?.abort();
        stato.mappa.remove();
        mappe.delete(elementId);
    }
}

// ---------- Interazioni e selezione (Milestone 1) ----------

function registraInterazioni(stato) {
    // I listener per layer id sopravvivono ai cambi di style: si registrano una volta sola.
    if (stato.interazioniRegistrate) {
        return;
    }
    stato.interazioniRegistrate = true;

    const { mappa } = stato;
    const popup = new maplibregl.Popup({ closeButton: false, closeOnClick: false });

    mappa.on("mousemove", LAYER_VIGNETI_FILL, (e) => {
        mappa.getCanvas().style.cursor = "pointer";

        if (e.features.length === 0) {
            return;
        }

        const feature = e.features[0];
        evidenziaHover(stato, feature.id);

        // Il nome arriva da 4Grapes: solo come testo (textContent), mai come markup.
        const nome = document.createElement("strong");
        nome.textContent = feature.properties.nome ?? "";
        popup.setLngLat(e.lngLat)
            .setDOMContent(nome)
            .addTo(mappa);
    });

    mappa.on("mouseleave", LAYER_VIGNETI_FILL, () => {
        mappa.getCanvas().style.cursor = "";
        evidenziaHover(stato, null);
        popup.remove();
    });

    // Click sulla mappa: il punto cliccato è per definizione già in vista, quindi qui l'uso della
    // feature renderizzata serve solo a sapere QUALE vigneto è stato cliccato. Si evidenzia senza
    // muovere la camera e si notifica Blazor, che aggiorna solo il proprio stato (pannello ed
    // elenco) senza richiamare selezionaVigneto: nessun loop.
    mappa.on("click", (e) => {
        const vignetoId = vignetoNelPunto(stato, e.point);
        if (vignetoId === null) {
            return;
        }

        evidenzia(stato, vignetoId);

        if (stato.dotNetRef) {
            stato.dotNetRef.invokeMethodAsync("OnVignetoCliccato", vignetoId);
        }
    });
}

/// Il vigneto toccato: quello sotto il punto o, se non c'è, il più vicino entro RAGGIO_TOCCO_PX
/// (col dito i vigneti piccoli sono difficili da centrare; col mouse il raggio è minimo). Fra più
/// vicini vince quello col riquadro (bbox dal server, proiettato sullo schermo) più vicino al punto.
function vignetoNelPunto(stato, punto) {
    const { mappa } = stato;
    if (!mappa.getLayer(LAYER_VIGNETI_FILL)) {
        return null;
    }

    const sotto = mappa.queryRenderedFeatures(punto, { layers: [LAYER_VIGNETI_FILL] });
    if (sotto.length > 0) {
        return sotto[0].id;
    }

    const r = window.matchMedia("(pointer: coarse)").matches ? RAGGIO_TOCCO_PX : RAGGIO_MOUSE_PX;
    const vicini = mappa.queryRenderedFeatures([[punto.x - r, punto.y - r], [punto.x + r, punto.y + r]],
        { layers: [LAYER_VIGNETI_FILL] });
    let migliore = null;
    let distanzaMinima = Infinity;
    for (const feature of vicini) {
        const bbox = stato.indice?.get(feature.id)?.bbox;
        const distanza = bbox ? distanzaDalRiquadro(mappa, bbox, punto) : r;
        if (distanza < distanzaMinima) {
            distanzaMinima = distanza;
            migliore = feature.id;
        }
    }
    return migliore;
}

/// Distanza in pixel fra il punto e il riquadro [W, S, E, N] proiettato sullo schermo (0 = dentro).
function distanzaDalRiquadro(mappa, [west, south, east, north], punto) {
    const angoli = [[west, south], [west, north], [east, south], [east, north]].map((c) => mappa.project(c));
    const xs = angoli.map((p) => p.x);
    const ys = angoli.map((p) => p.y);
    const dx = Math.max(Math.min(...xs) - punto.x, 0, punto.x - Math.max(...xs));
    const dy = Math.max(Math.min(...ys) - punto.y, 0, punto.y - Math.max(...ys));
    return Math.hypot(dx, dy);
}

/// Vigneto sotto il mouse, sulla mappa o nell'elenco (null = nessuno): feature-state "hover" sul
/// poligono e data-evidenza sulla voce dell'elenco (attributo che Blazor non scrive, quindi non lo
/// cancella ridisegnando). Nessun giro sul server.
function evidenziaHover(stato, vignetoId) {
    const id = vignetoId !== null && stato.indice?.has(vignetoId) ? vignetoId : null;
    if (id === stato.idHover) {
        return;
    }

    const { mappa } = stato;
    const sourcePresente = !!mappa.getSource(SOURCE_VIGNETI);
    if (stato.idHover !== null) {
        if (sourcePresente) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id: stato.idHover }, { hover: false });
        }
        document.getElementById(`${stato.elementId}-voce-${stato.idHover}`)?.removeAttribute("data-evidenza");
    }

    stato.idHover = id;
    if (id !== null) {
        if (sourcePresente) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id }, { hover: true });
        }
        document.getElementById(`${stato.elementId}-voce-${id}`)?.setAttribute("data-evidenza", "");
    }
}

/// Passando col mouse su una voce dell'elenco (li[data-vigneto]) si evidenzia il suo poligono.
/// Un ascoltatore solo, sul pannello: le voci le crea e le toglie Blazor.
export function collegaEvidenzaElenco(elementId, elencoId) {
    const elenco = document.getElementById(elencoId);
    if (!elenco) {
        return;
    }

    const voceDi = (e) => e.target.closest?.("li[data-vigneto]");
    elenco.addEventListener("mouseover", (e) => {
        const stato = mappe.get(elementId);
        const voce = voceDi(e);
        if (stato && voce) {
            evidenziaHover(stato, Number(voce.dataset.vigneto));
        }
    });
    elenco.addEventListener("mouseout", (e) => {
        const stato = mappe.get(elementId);
        const voce = voceDi(e);
        // Si esce dalla voce (non solo da un suo figlio verso un altro).
        if (stato && voce && !voce.contains(e.relatedTarget)) {
            evidenziaHover(stato, null);
        }
    });
}

/// Menu a tendina (details[data-menu], es. «⋯ Azioni» e il menu dell'intestazione sul telefono): si
/// chiude cliccando fuori, con Esc o dopo aver scelto una voce. Il details non è scritto da Blazor
/// (attributo open), quindi resta com'è.
export function collegaMenu(contenitoreId) {
    if (!document.getElementById(contenitoreId)) {
        return;
    }

    const chiudi = (tranne) => document.querySelectorAll("details[data-menu][open]").forEach((menu) => {
        if (menu !== tranne) {
            menu.removeAttribute("open");
        }
    });
    document.addEventListener("click", (e) => {
        const menu = e.target.closest?.("details[data-menu]");
        // Una voce scelta (link o pulsante dentro il menu) chiude il menu; il summary lo apre e chiude da sé.
        const voce = menu && e.target.closest("a, button");
        chiudi(voce ? null : menu);
    });
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
            chiudi(null);
        }
    });
}

/// Solo stato visivo: sposta il feature-state "selezionato" sul vigneto indicato (o lo toglie
/// se il vigneto non è nell'indice, es. perché senza geometria). Non muove la camera. Se la
/// source non esiste ancora (style in caricamento) basta lo stato: lo riapplica applicaOverlay.
function evidenzia(stato, vignetoId) {
    const { mappa } = stato;
    const sourcePresente = !!mappa.getSource(SOURCE_VIGNETI);

    if (stato.vignetoAttivoId !== null && sourcePresente) {
        mappa.setFeatureState({ source: SOURCE_VIGNETI, id: stato.vignetoAttivoId }, { selezionato: false });
    }
    stato.vignetoAttivoId = null;

    if (stato.indice?.has(vignetoId)) {
        stato.vignetoAttivoId = vignetoId;
        if (sourcePresente) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id: vignetoId }, { selezionato: true });
        }
    }
}

/// Mantiene bearing e pitch correnti; per bbox praticamente puntiformi usa flyTo (fitBounds
/// su un'area nulla produrrebbe lo zoom massimo).
function portaCameraSu(mappa, bbox) {
    const [west, south, east, north] = bbox;
    const camera = { bearing: mappa.getBearing(), pitch: mappa.getPitch(), duration: 1200 };

    if (Math.abs(east - west) < 1e-7 && Math.abs(north - south) < 1e-7) {
        mappa.flyTo({ ...camera, center: [west, south], zoom: Math.max(mappa.getZoom(), 17) });
    } else {
        mappa.fitBounds(bboxInBounds(bbox), { ...camera, padding: paddingSchedaVigneto(mappa), maxZoom: 18 });
    }
}

/// Spazio lasciato libero dalla scheda del vigneto selezionato (vedi .info-panel in app.css):
/// su mobile la scheda ridotta in basso (sotto i pulsanti in alto), su desktop 320px a destra. Così
/// il vigneto inquadrato non finisce sotto la scheda.
function paddingSchedaVigneto(mappa) {
    const { width, height } = mappa.getContainer().getBoundingClientRect();
    if (window.matchMedia("(max-width: 800px)").matches) {
        return { top: 70, left: 30, right: 30, bottom: Math.min(220, Math.round(height * 0.4)) };
    }
    const destra = width > 900 ? 400 : 80;
    return { top: 80, left: 80, right: destra, bottom: 80 };
}

function bboxInBounds([west, south, east, north]) {
    return [[west, south], [east, north]];
}

// ---------- Diagnostica ----------

/// Chiave "host tipo /primi/due-segmenti": il percorso distingue ad es. tile vettoriali e raster
/// dello stesso provider. Query string (dove stanno le API key) e resto del percorso esclusi.
function contaRichiesta(stato, url, tipoRisorsa) {
    let host = "?";
    let percorso = "";
    try {
        const u = new URL(url, location.href);
        host = u.host;
        percorso = "/" + u.pathname.split("/").filter(Boolean).slice(0, 2).join("/");
    } catch {
        // URL non analizzabile: resta "?"
    }

    const chiave = `${host} ${tipoRisorsa ?? "Unknown"} ${percorso}`;
    stato.richieste[chiave] = (stato.richieste[chiave] ?? 0) + 1;
}

// ---------- Avviso sfondo: tessere di basemap o satellite che non arrivano ----------

/// Source di sfondo = tutte tranne i vigneti (dati locali) e il terreno 3D (solo rilievo).
function eSorgenteSfondo(sourceId) {
    return !!sourceId && sourceId !== SOURCE_VIGNETI && sourceId !== SOURCE_TERRENO;
}

function erroreTesseraSfondo(stato, e) {
    if (!eSorgenteSfondo(e.sourceId)) {
        return;
    }

    const errori = (stato.erroriSfondo.get(e.sourceId) ?? 0) + 1;
    stato.erroriSfondo.set(e.sourceId, errori);
    if (errori >= SOGLIA_ERRORI_SFONDO) {
        stato.sfondoKo.add(e.sourceId);
        notificaSfondo(stato);
    }
}

/// Una tessera arrivata azzera gli errori di quella source (e toglie l'avviso se era l'ultima).
function tesseraSfondoArrivata(stato, e) {
    if (e.dataType === "source" && e.tile && eSorgenteSfondo(e.sourceId) && stato.erroriSfondo.has(e.sourceId)) {
        dimenticaSfondo(stato, e.sourceId);
    }
}

/// sourceId null = tutte (cambio di style).
function dimenticaSfondo(stato, sourceId) {
    if (sourceId === null) {
        stato.erroriSfondo.clear();
        stato.sfondoKo.clear();
    } else {
        stato.erroriSfondo.delete(sourceId);
        stato.sfondoKo.delete(sourceId);
    }
    notificaSfondo(stato);
}

/// Blazor viene avvisato solo quando lo stato cambia (sfondo ok ↔ sfondo che non risponde).
function notificaSfondo(stato) {
    const ko = stato.sfondoKo.size > 0;
    if (ko !== stato.sfondoAvvisato) {
        stato.sfondoAvvisato = ko;
        stato.dotNetRef?.invokeMethodAsync("OnSfondoNonRisponde", ko);
    }
}

function registraErroreRisorsa(stato, e) {
    const chiave = e.sourceId ?? "altro";
    const conteggio = (stato.erroriRisorse[chiave] ?? 0) + 1;
    stato.erroriRisorse[chiave] = conteggio;

    if (conteggio <= MAX_AVVISI_CONSOLE_PER_SORGENTE) {
        console.warn(`[mappa] risorsa non caricata (${chiave})`, e.error?.message ?? e.error);
    }
}

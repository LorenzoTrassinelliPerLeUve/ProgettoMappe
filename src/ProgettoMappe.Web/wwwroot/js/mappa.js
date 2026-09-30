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
// Colori del marchio Per Le Uve / 4Grapes (verde lime) per i vigneti senza tematismo.
const COLORE_VIGNETO = "#8dc63e";
const COLORE_BORDO_VIGNETO = "#4f7a1f";
const COLORE_SELEZIONE = "#eae711";

export function creaMappa(elementId, opzioni, dotNetRef) {
    const stato = {
        mappa: null,
        dotNetRef,
        sorgenti: new Map((opzioni.sorgenti ?? []).map((s) => [s.id, s])),
        basemapFallbackId: opzioni.basemapFallbackId ?? null,
        timeoutStyleMs: opzioni.timeoutStyleMs ?? 10000,
        terrainId: opzioni.terrainId ?? null,
        terrainExaggeration: opzioni.terrainExaggeration ?? 1.2,

        // Dati applicativi, indipendenti dallo style.
        geojson: null,
        indice: null,
        vignetoAttivoId: null,
        selezionePendente: null,
        primoFitFatto: false,

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
        erroriRisorse: {}
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

    // Errori di singole risorse (tile, glyph, sprite...): solo registrati, MAI motivo di fallback.
    // Il fallimento dello style è gestito a parte in caricaBasemap (fetch + timeout).
    mappa.on("error", (e) => registraErroreRisorsa(stato, e));

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
                    "line-color": ["case", ["boolean", ["feature-state", "selezionato"], false], COLORE_SELEZIONE, COLORE_BORDO_VIGNETO],
                    "line-width": ["case", ["boolean", ["feature-state", "selezionato"], false], 3, 1.5]
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
}

/// Colori dei vigneti: senza tematismo il verde di base; con tematismo il colore della classe
/// (feature-state "tema" = indice in colori, -1 = nessun dato → grigio tenue). Le classi arrivano
/// già calcolate da Blazor: qui nessuna logica sui parametri. Il feature-state va riapplicato dopo
/// ogni setStyle (la source viene ricreata), per questo è chiamato da applicaOverlay.
function applicaTematismo(stato) {
    const { mappa } = stato;
    if (!mappa.getLayer(LAYER_VIGNETI_FILL) || !mappa.getSource(SOURCE_VIGNETI)) {
        return;
    }

    const tema = stato.tematismo;
    const selezionato = ["boolean", ["feature-state", "selezionato"], false];

    if (!tema) {
        mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-color", COLORE_VIGNETO);
        mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-opacity", ["case", selezionato, 0.55, 0.3]);
        return;
    }

    const classe = ["coalesce", ["feature-state", "tema"], -1];
    const colore = ["match", classe];
    tema.colori.forEach((c, i) => colore.push(i, c));
    colore.push("#9e9e9e");

    mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-color", colore);
    mappa.setPaintProperty(LAYER_VIGNETI_FILL, "fill-opacity",
        ["case", selezionato, 0.9, [">=", classe, 0], 0.75, 0.25]);

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
export function mostraVigneti(elementId, geojson, adatta = false) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    stato.geojson = geojson;
    stato.indice = new Map((geojson.features ?? []).map((feature) => [feature.id, feature]));
    if (stato.vignetoAttivoId !== null && !stato.indice.has(stato.vignetoAttivoId)) {
        stato.vignetoAttivoId = null;
    }
    if (adatta) {
        stato.filtroIds = null;
        stato.primoFitFatto = false;
    }
    applicaOverlay(stato);

    if (!stato.primoFitFatto && geojson.bbox) {
        stato.primoFitFatto = true;
        stato.mappa.fitBounds(bboxInBounds(geojson.bbox), { padding: 60, duration: 0 });
    }

    if (stato.selezionePendente !== null) {
        const id = stato.selezionePendente;
        stato.selezionePendente = null;
        selezionaVigneto(elementId, id);
    }
}

/// Selezione da elenco (Blazor → mappa): evidenzia il vigneto e porta la camera sul suo bbox,
/// letto dall'indice locale. Vigneto senza geometria: nessuna evidenziazione, camera ferma.
export function selezionaVigneto(elementId, vignetoId) {
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
    if (vignetoId === null) {
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
        const bbox = (stato.filtroIds ?? [...(stato.indice?.keys() ?? [])])
            .map((id) => stato.indice?.get(id)?.bbox)
            .filter(Boolean)
            .reduce((a, b) => (a ? [Math.min(a[0], b[0]), Math.min(a[1], b[1]), Math.max(a[2], b[2]), Math.max(a[3], b[3])] : b), null);
        if (bbox) {
            portaCameraSu(stato.mappa, bbox);
        }
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

    stato.terrenoAttivo = !!attivo;
    if (stato.styleCaricato) {
        aggiornaTerreno(stato);
    }
}

/// Porta in vista un elemento dell'elenco.
export function portaInVista(elementoId) {
    document.getElementById(elementoId)?.scrollIntoView({ block: "nearest" });
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
    let idHover = null;
    const popup = new maplibregl.Popup({ closeButton: false, closeOnClick: false });

    mappa.on("mousemove", LAYER_VIGNETI_FILL, (e) => {
        mappa.getCanvas().style.cursor = "pointer";

        if (e.features.length === 0) {
            return;
        }

        const feature = e.features[0];

        if (idHover !== null && idHover !== feature.id) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id: idHover }, { hover: false });
        }

        idHover = feature.id;
        mappa.setFeatureState({ source: SOURCE_VIGNETI, id: idHover }, { hover: true });

        popup.setLngLat(e.lngLat)
            .setHTML(`<strong>${feature.properties.nome}</strong>`)
            .addTo(mappa);
    });

    mappa.on("mouseleave", LAYER_VIGNETI_FILL, () => {
        mappa.getCanvas().style.cursor = "";

        if (idHover !== null && mappa.getSource(SOURCE_VIGNETI)) {
            mappa.setFeatureState({ source: SOURCE_VIGNETI, id: idHover }, { hover: false });
        }

        idHover = null;
        popup.remove();
    });

    // Click sulla mappa: il punto cliccato è per definizione già in vista, quindi qui l'uso della
    // feature renderizzata serve solo a sapere QUALE vigneto è stato cliccato. Si evidenzia senza
    // muovere la camera e si notifica Blazor, che aggiorna solo il proprio stato (pannello ed
    // elenco) senza richiamare selezionaVigneto: nessun loop.
    mappa.on("click", LAYER_VIGNETI_FILL, (e) => {
        if (e.features.length === 0) {
            return;
        }

        const vignetoId = e.features[0].id;
        evidenzia(stato, vignetoId);

        if (stato.dotNetRef) {
            stato.dotNetRef.invokeMethodAsync("OnVignetoCliccato", vignetoId);
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
/// su mobile occupa fino a metà altezza in basso, su desktop 320px a destra. Così il vigneto
/// inquadrato non finisce sotto la scheda.
function paddingSchedaVigneto(mappa) {
    const { width, height } = mappa.getContainer().getBoundingClientRect();
    if (window.matchMedia("(max-width: 800px)").matches) {
        return { top: 60, left: 40, right: 40, bottom: Math.round(height * 0.5) + 30 };
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

function registraErroreRisorsa(stato, e) {
    const chiave = e.sourceId ?? "altro";
    const conteggio = (stato.erroriRisorse[chiave] ?? 0) + 1;
    stato.erroriRisorse[chiave] = conteggio;

    if (conteggio <= MAX_AVVISI_CONSOLE_PER_SORGENTE) {
        console.warn(`[mappa] risorsa non caricata (${chiave})`, e.error?.message ?? e.error);
    }
}

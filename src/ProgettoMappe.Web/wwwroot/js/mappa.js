// Motore cartografico MapLibre: navigazione libera (pan/zoom/rotazione/inclinazione nativi
// di MapLibre), satellite/aereo e terreno 3D configurabili da Blazor (nessun provider
// hardcodato qui: arrivano da MapOptions), hover/click/selezione con callback verso il
// componente Blazor. Il fitBounds/flyTo è solo un aiuto alla navigazione: dopo l'animazione
// l'utente ha subito il pieno controllo della mappa (pan/zoom/rotate restano sempre attivi).
//
// La selezione usa un indice locale IdVigneto → Feature costruito dalla FeatureCollection
// ricevuta dall'API (che porta già Feature.id e bbox calcolati lato server): non dipende mai
// dalle feature che MapLibre ha renderizzato o tile-izzato, quindi funziona a qualsiasi zoom e
// posizione della camera.

const mappe = new Map();
const sourceId = "vigneti";

export function creaMappa(elementId, opzioni, dotNetRef) {
    const mappa = new maplibregl.Map({
        container: elementId,
        style: opzioni.styleUrl,
        center: opzioni.centro,
        zoom: opzioni.zoom,
        pitch: 0,
        bearing: 0,
        antialias: true
    });

    mappa.addControl(new maplibregl.NavigationControl({ visualizePitch: true }), "top-right");

    mappa.on("load", () => {
        if (opzioni.terrainSourceUrl) {
            mappa.addSource("terreno-dem", {
                type: "raster-dem",
                tiles: [opzioni.terrainSourceUrl],
                tileSize: opzioni.terrainTileSize ?? 256
            });
            mappa.setTerrain({ source: "terreno-dem", exaggeration: opzioni.terrainExaggeration ?? 1.2 });
        }
    });

    // indice: null finché source e layer non sono pronti; selezionePendente: IdVigneto scelto
    // dall'elenco prima di quel momento, applicato appena l'indice esiste.
    mappe.set(elementId, { mappa, dotNetRef, vignetoAttivoId: null, indice: null, selezionePendente: null });
}

export function mostraVigneti(elementId, geojson) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    const { mappa } = stato;

    const applica = () => {
        if (mappa.getSource(sourceId)) {
            mappa.getSource(sourceId).setData(geojson);
        } else {
            // Nessun promoteId: MapLibre usa direttamente Feature.id (= IdVigneto) per il feature-state.
            mappa.addSource(sourceId, { type: "geojson", data: geojson });

            mappa.addLayer({
                id: "vigneti-fill",
                type: "fill",
                source: sourceId,
                paint: {
                    "fill-color": "#4c8c4a",
                    "fill-opacity": ["case", ["boolean", ["feature-state", "selezionato"], false], 0.55, 0.3]
                }
            });

            mappa.addLayer({
                id: "vigneti-outline",
                type: "line",
                source: sourceId,
                paint: {
                    "line-color": "#2f5c2d",
                    "line-width": ["case", ["boolean", ["feature-state", "selezionato"], false], 3, 1.5]
                }
            });

            registraInterazioni(mappa, stato);
        }

        stato.indice = new Map((geojson.features ?? []).map((feature) => [feature.id, feature]));

        if (geojson.bbox) {
            mappa.fitBounds(bboxInBounds(geojson.bbox), { padding: 60, duration: 0 });
        }

        if (stato.selezionePendente !== null) {
            const id = stato.selezionePendente;
            stato.selezionePendente = null;
            selezionaVigneto(elementId, id);
        }
    };

    if (mappa.isStyleLoaded()) {
        applica();
    } else {
        mappa.once("load", applica);
    }
}

function registraInterazioni(mappa, stato) {
    let idHover = null;
    const popup = new maplibregl.Popup({ closeButton: false, closeOnClick: false });

    mappa.on("mousemove", "vigneti-fill", (e) => {
        mappa.getCanvas().style.cursor = "pointer";

        if (e.features.length === 0) {
            return;
        }

        const feature = e.features[0];

        if (idHover !== null && idHover !== feature.id) {
            mappa.setFeatureState({ source: sourceId, id: idHover }, { hover: false });
        }

        idHover = feature.id;
        mappa.setFeatureState({ source: sourceId, id: idHover }, { hover: true });

        popup.setLngLat(e.lngLat)
            .setHTML(`<strong>${feature.properties.nome}</strong>`)
            .addTo(mappa);
    });

    mappa.on("mouseleave", "vigneti-fill", () => {
        mappa.getCanvas().style.cursor = "";

        if (idHover !== null) {
            mappa.setFeatureState({ source: sourceId, id: idHover }, { hover: false });
        }

        idHover = null;
        popup.remove();
    });

    // Click sulla mappa: il punto cliccato è per definizione già in vista, quindi qui l'uso della
    // feature renderizzata serve solo a sapere QUALE vigneto è stato cliccato. Si evidenzia senza
    // muovere la camera e si notifica Blazor, che aggiorna solo il proprio stato (pannello ed
    // elenco) senza richiamare selezionaVigneto: nessun loop.
    mappa.on("click", "vigneti-fill", (e) => {
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
/// se il vigneto non è nell'indice, es. perché senza geometria). Non muove la camera.
function evidenzia(stato, vignetoId) {
    const { mappa } = stato;

    if (stato.vignetoAttivoId !== null) {
        mappa.setFeatureState({ source: sourceId, id: stato.vignetoAttivoId }, { selezionato: false });
        stato.vignetoAttivoId = null;
    }

    if (stato.indice?.has(vignetoId)) {
        mappa.setFeatureState({ source: sourceId, id: vignetoId }, { selezionato: true });
        stato.vignetoAttivoId = vignetoId;
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

    evidenzia(stato, vignetoId);

    const bbox = stato.indice.get(vignetoId)?.bbox;
    if (bbox) {
        portaCameraSu(stato.mappa, bbox);
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
        mappa.fitBounds(bboxInBounds(bbox), { ...camera, padding: 80, maxZoom: 18 });
    }
}

function bboxInBounds([west, south, east, north]) {
    return [[west, south], [east, north]];
}

/// Porta in vista (senza animare la pagina oltre il necessario) un elemento dell'elenco.
export function portaInVista(elementoId) {
    document.getElementById(elementoId)?.scrollIntoView({ block: "nearest" });
}

export function distruggiMappa(elementId) {
    const stato = mappe.get(elementId);
    if (stato) {
        stato.mappa.remove();
        mappe.delete(elementId);
    }
}

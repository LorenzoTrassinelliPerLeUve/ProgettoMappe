// Motore cartografico MapLibre: navigazione libera (pan/zoom/rotazione/inclinazione nativi
// di MapLibre), satellite/aereo e terreno 3D configurabili da Blazor (nessun provider
// hardcodato qui: arrivano da MapOptions), hover/click/selezione con callback verso il
// componente Blazor. Il flyTo è solo un aiuto alla navigazione: dopo l'animazione l'utente
// ha subito il pieno controllo della mappa (pan/zoom/rotate restano sempre attivi).

const mappe = new Map();

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

    mappe.set(elementId, { mappa, dotNetRef, vignetoAttivoId: null });
}

export function mostraVigneti(elementId, geojson) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    const { mappa } = stato;
    const sourceId = "vigneti";

    const applica = () => {
        if (mappa.getSource(sourceId)) {
            mappa.getSource(sourceId).setData(geojson);
        } else {
            mappa.addSource(sourceId, { type: "geojson", data: geojson, promoteId: "id" });

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

            registraInterazioni(elementId, mappa, sourceId, stato);
        }

        adattaAiVigneti(mappa, geojson);
    };

    if (mappa.isStyleLoaded()) {
        applica();
    } else {
        mappa.once("load", applica);
    }
}

function registraInterazioni(elementId, mappa, sourceId, stato) {
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

    mappa.on("click", "vigneti-fill", (e) => {
        if (e.features.length === 0) {
            return;
        }

        const vignetoId = e.features[0].properties.id;
        selezionaFeature(elementId, vignetoId);

        if (stato.dotNetRef) {
            stato.dotNetRef.invokeMethodAsync("OnVignetoCliccato", vignetoId);
        }
    });
}

function selezionaFeature(elementId, vignetoId) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    const { mappa } = stato;
    const sourceId = "vigneti";

    if (stato.vignetoAttivoId !== null) {
        mappa.setFeatureState({ source: sourceId, id: stato.vignetoAttivoId }, { selezionato: false });
    }

    mappa.setFeatureState({ source: sourceId, id: vignetoId }, { selezionato: true });
    stato.vignetoAttivoId = vignetoId;
}

/// Selezione da lista (Blazor -> mappa): evidenzia il vigneto e ci vola sopra (flyTo), senza
/// bloccare la navigazione libera una volta completata l'animazione.
export function selezionaVigneto(elementId, vignetoId) {
    const stato = mappe.get(elementId);
    if (!stato) {
        return;
    }

    selezionaFeature(elementId, vignetoId);

    const feature = stato.mappa
        .querySourceFeatures("vigneti", { filter: ["==", ["get", "id"], vignetoId] })[0];

    const bounds = feature ? calcolaBounds(feature.geometry) : null;
    if (bounds) {
        stato.mappa.fitBounds(bounds, { padding: 80, duration: 1200, maxZoom: 17 });
    }
}

function adattaAiVigneti(mappa, geojson) {
    if (!geojson.features || geojson.features.length === 0) {
        return;
    }

    let bounds = null;
    for (const feature of geojson.features) {
        const featureBounds = calcolaBounds(feature.geometry);
        if (!featureBounds) {
            continue;
        }

        bounds = bounds
            ? [
                [Math.min(bounds[0][0], featureBounds[0][0]), Math.min(bounds[0][1], featureBounds[0][1])],
                [Math.max(bounds[1][0], featureBounds[1][0]), Math.max(bounds[1][1], featureBounds[1][1])]
            ]
            : featureBounds;
    }

    if (bounds) {
        mappa.fitBounds(bounds, { padding: 60, duration: 0 });
    }
}

function calcolaBounds(geometry) {
    if (!geometry || !geometry.coordinates) {
        return null;
    }

    const coordinatePiatte = [];
    const raccogli = (coord) => {
        if (typeof coord[0] === "number") {
            coordinatePiatte.push(coord);
        } else {
            coord.forEach(raccogli);
        }
    };
    raccogli(geometry.coordinates);

    if (coordinatePiatte.length === 0) {
        return null;
    }

    let minLon = Infinity, minLat = Infinity, maxLon = -Infinity, maxLat = -Infinity;
    for (const [lon, lat] of coordinatePiatte) {
        minLon = Math.min(minLon, lon);
        minLat = Math.min(minLat, lat);
        maxLon = Math.max(maxLon, lon);
        maxLat = Math.max(maxLat, lat);
    }

    return [[minLon, minLat], [maxLon, maxLat]];
}

export function distruggiMappa(elementId) {
    const stato = mappe.get(elementId);
    if (stato) {
        stato.mappa.remove();
        mappe.delete(elementId);
    }
}

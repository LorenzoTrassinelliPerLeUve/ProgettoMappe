// Interop minimo tra Blazor e MapLibre GL JS per la visualizzazione cartografica (mese 2 del piano).
// Le mappe di base sono lasciate configurabili tramite styleUrl per poter cambiare provider in futuro.

const mappe = new Map();

export function creaMappa(elementId, styleUrl, centro, zoom) {
    const mappa = new maplibregl.Map({
        container: elementId,
        style: styleUrl,
        center: centro,
        zoom: zoom
    });

    mappa.addControl(new maplibregl.NavigationControl(), "top-right");
    mappe.set(elementId, mappa);
}

export function mostraVigneti(elementId, geojson) {
    const mappa = mappe.get(elementId);
    if (!mappa) {
        return;
    }

    const sourceId = "vigneti";

    const applica = () => {
        if (mappa.getSource(sourceId)) {
            mappa.getSource(sourceId).setData(geojson);
            return;
        }

        mappa.addSource(sourceId, { type: "geojson", data: geojson });

        mappa.addLayer({
            id: "vigneti-fill",
            type: "fill",
            source: sourceId,
            paint: { "fill-color": "#4c8c4a", "fill-opacity": 0.35 }
        });

        mappa.addLayer({
            id: "vigneti-outline",
            type: "line",
            source: sourceId,
            paint: { "line-color": "#2f5c2d", "line-width": 2 }
        });
    };

    if (mappa.isStyleLoaded()) {
        applica();
    } else {
        mappa.once("load", applica);
    }
}

export function distruggiMappa(elementId) {
    const mappa = mappe.get(elementId);
    if (mappa) {
        mappa.remove();
        mappe.delete(elementId);
    }
}

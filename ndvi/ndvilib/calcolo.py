"""Vigneti dal GeoJSON, riquadri da chiedere al Process API e statistiche NDVI per poligono."""

import math
from dataclasses import dataclass

import numpy as np
import rasterio
from rasterio.mask import raster_geometry_mask
from shapely.geometry import mapping, shape

METRI_PER_GRADO_LAT = 110_540.0
METRI_PER_GRADO_LON_EQUATORE = 111_320.0


@dataclass
class Vigneto:
    id: int
    geometria: object  # shapely Polygon/MultiPolygon in WGS84


@dataclass
class Riquadro:
    vigneti: list
    bbox: tuple  # (ovest, sud, est, nord)
    larghezza: int
    altezza: int


@dataclass
class Statistica:
    id_vigneto: int
    media: float | None
    mediana: float | None
    pixel_validi: int
    pixel_totali: int
    nuvole_pct: float | None


def vigneti_da_geojson(collezione: dict) -> list[Vigneto]:
    """Solo le Feature dei vigneti con poligono: i punti e i sottovigneti (senza id) restano fuori."""
    vigneti = []
    for feature in collezione.get("features", []):
        id_vigneto = feature.get("id")
        geometria = feature.get("geometry") or {}
        if id_vigneto is None or geometria.get("type") not in ("Polygon", "MultiPolygon"):
            continue
        forma = shape(geometria)
        if forma.is_empty:
            continue
        vigneti.append(Vigneto(int(id_vigneto), forma))
    return vigneti


def bbox_vigneti(vigneti, margine_m=0.0) -> tuple:
    ovest = min(v.geometria.bounds[0] for v in vigneti)
    sud = min(v.geometria.bounds[1] for v in vigneti)
    est = max(v.geometria.bounds[2] for v in vigneti)
    nord = max(v.geometria.bounds[3] for v in vigneti)
    dx, dy = _gradi(margine_m, (sud + nord) / 2)
    return (ovest - dx, sud - dy, est + dx, nord + dy)


def _gradi(metri, lat):
    return (metri / (METRI_PER_GRADO_LON_EQUATORE * math.cos(math.radians(lat))),
            metri / METRI_PER_GRADO_LAT)


def dimensioni_pixel(bbox, risoluzione_m) -> tuple[int, int]:
    """Larghezza e altezza in pixel perché il lato del pixel sia circa `risoluzione_m`."""
    ovest, sud, est, nord = bbox
    lat = math.radians((sud + nord) / 2)
    larghezza_m = (est - ovest) * METRI_PER_GRADO_LON_EQUATORE * math.cos(lat)
    altezza_m = (nord - sud) * METRI_PER_GRADO_LAT
    return (max(1, math.ceil(larghezza_m / risoluzione_m)),
            max(1, math.ceil(altezza_m / risoluzione_m)))


def riquadri(vigneti, risoluzione_m, max_pixel, margine_m) -> tuple[list[Riquadro], list[Vigneto]]:
    """Gruppi di vigneti il cui bbox sta in max_pixel × max_pixel.

    Se il bbox di tutti è troppo grande, si divide il gruppo a metà (per baricentro, lungo il lato
    più lungo) finché ogni gruppo ci sta. Restituisce anche i vigneti che da soli non ci stanno.
    """
    pronti, troppo_grandi = [], []
    da_fare = [list(vigneti)] if vigneti else []
    while da_fare:
        gruppo = da_fare.pop()
        bbox = bbox_vigneti(gruppo, margine_m)
        larghezza, altezza = dimensioni_pixel(bbox, risoluzione_m)
        if larghezza <= max_pixel and altezza <= max_pixel:
            pronti.append(Riquadro(gruppo, bbox, larghezza, altezza))
        elif len(gruppo) == 1:
            troppo_grandi.extend(gruppo)
        else:
            asse = 0 if larghezza >= altezza else 1
            ordinati = sorted(gruppo, key=lambda v: v.geometria.centroid.coords[0][asse])
            meta = len(ordinati) // 2
            da_fare += [ordinati[:meta], ordinati[meta:]]
    pronti.sort(key=lambda r: (r.bbox[1], r.bbox[0]))
    return pronti, troppo_grandi


def statistiche(dataset, vigneti) -> list[Statistica]:
    """Media, mediana, pixel validi/totali e % nuvole dei pixel col centro nel poligono.

    Totali = pixel del poligono (all_touched=False); validi = quelli con un NDVI (non NaN).
    Un poligono più piccolo di un pixel può avere 0 pixel: tutto None.
    """
    risultati = []
    for vigneto in vigneti:
        try:
            fuori, _, finestra = raster_geometry_mask(
                dataset, [mapping(vigneto.geometria)], crop=True, all_touched=False)
        except ValueError:  # il poligono non tocca il raster
            risultati.append(Statistica(vigneto.id, None, None, 0, 0, None))
            continue
        valori = dataset.read(1, window=finestra, masked=False)[~fuori]
        validi = valori[np.isfinite(valori)]
        totali = int(valori.size)
        risultati.append(Statistica(
            vigneto.id,
            float(validi.mean()) if validi.size else None,
            float(np.median(validi)) if validi.size else None,
            int(validi.size),
            totali,
            100.0 * (totali - validi.size) / totali if totali else None,
        ))
    return risultati


def nuvole_passaggio(statistiche_vigneti) -> float | None:
    """% di pixel non validi su tutti i vigneti del passaggio (None se nessun pixel)."""
    totali = sum(s.pixel_totali for s in statistiche_vigneti)
    validi = sum(s.pixel_validi for s in statistiche_vigneti)
    return 100.0 * (totali - validi) / totali if totali else None


def apri_geotiff(percorso):
    return rasterio.open(percorso)

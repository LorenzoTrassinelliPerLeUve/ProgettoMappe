"""Vigneti dal GeoJSON, riquadri da chiedere al Process API e statistiche NDVI per poligono."""

import math
from dataclasses import dataclass

import numpy as np
import rasterio
from rasterio.io import MemoryFile
from rasterio.mask import raster_geometry_mask
from shapely.geometry import mapping, shape

METRI_PER_GRADO_LAT = 110_540.0
METRI_PER_GRADO_LON_EQUATORE = 111_320.0

# GeoTIFF salvati: NDVI × 10000 in int16 (NaN → -32768), deflate + predictor 2, a tessere.
# Circa 4-6 volte più piccoli del float32; la lettura (`leggi_ndvi`) riporta a float con NaN.
SCALA = 10_000
NODATA = -32768

# Stima delle processing units: B04, B08 e SCL in ingresso, uscita float32 (fattore 2).
BANDE_INPUT = 3
FATTORE_FLOAT32 = 2


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


def pu_stimate(larghezza, altezza) -> float:
    """Processing units di una richiesta al Process API (formula di Sentinel Hub): pixel / 512²
    (minimo 0,01) × bande in ingresso / 3 × 2 per l'uscita float32."""
    return max(0.01, larghezza * altezza / 512 ** 2) * BANDE_INPUT / 3 * FATTORE_FLOAT32


def distanza_m(a, b) -> float:
    """Distanza in metri fra i bbox di due geometrie (0 se si toccano o si sovrappongono)."""
    a0, a1, a2, a3 = a.bounds
    b0, b1, b2, b3 = b.bounds
    dx, dy = max(0.0, b0 - a2, a0 - b2), max(0.0, b1 - a3, a1 - b3)
    lat = math.radians((a1 + a3 + b1 + b3) / 4)
    return math.hypot(dx * METRI_PER_GRADO_LON_EQUATORE * math.cos(lat), dy * METRI_PER_GRADO_LAT)


def gruppi_vicini(vigneti, distanza_max_m) -> list[list[Vigneto]]:
    """Vigneti raggruppati per vicinanza: due vigneti a meno di `distanza_max_m` (fra i bbox)
    stanno nello stesso gruppo, anche attraverso una catena di vicini. 0 = un gruppo solo."""
    if distanza_max_m <= 0 or len(vigneti) < 2:
        return [list(vigneti)] if vigneti else []
    capo = list(range(len(vigneti)))

    def radice(i):
        while capo[i] != i:
            capo[i] = capo[capo[i]]
            i = capo[i]
        return i

    for i in range(len(vigneti)):
        for j in range(i + 1, len(vigneti)):
            if radice(i) != radice(j) and \
                    distanza_m(vigneti[i].geometria, vigneti[j].geometria) <= distanza_max_m:
                capo[radice(i)] = radice(j)
    gruppi = {}
    for i, vigneto in enumerate(vigneti):
        gruppi.setdefault(radice(i), []).append(vigneto)
    return list(gruppi.values())


def riquadri(vigneti, risoluzione_m, max_pixel, margine_m,
             distanza_gruppi_m=0) -> tuple[list[Riquadro], list[Vigneto]]:
    """Gruppi di vigneti il cui bbox sta in max_pixel × max_pixel.

    Prima i vigneti si raggruppano per vicinanza (`gruppi_vicini`): un riquadro stretto per
    gruppo invece del bbox dell'intera azienda, perché le processing units dipendono dai pixel.
    Se il bbox di un gruppo è troppo grande, si divide a metà (per baricentro, lungo il lato
    più lungo) finché ogni gruppo ci sta. Restituisce anche i vigneti che da soli non ci stanno.
    """
    pronti, troppo_grandi = [], []
    da_fare = gruppi_vicini(vigneti, distanza_gruppi_m)
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
        valori = leggi_ndvi(dataset, finestra)[~fuori]
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


def leggi_ndvi(dataset, finestra=None):
    """NDVI float con NaN dove manca: i file int16 si riconvertono, i vecchi float32 no."""
    valori = dataset.read(1, window=finestra, masked=False)
    if valori.dtype.kind == "f":
        return valori
    ndvi = valori.astype("float32") / SCALA
    if dataset.nodata is not None:
        ndvi[valori == dataset.nodata] = np.nan
    return ndvi


def in_int16(valori):
    """NDVI float → int16 scalato; NaN e infiniti → NODATA."""
    scalati = np.clip(np.rint(np.nan_to_num(valori, nan=0.0) * SCALA), -32767, 32767)
    scalati[~np.isfinite(valori)] = NODATA
    return scalati.astype("int16")


def scrivi_compresso(dataset, percorso):
    """Copia `dataset` (NDVI float) in un GeoTIFF int16 compresso."""
    profilo = dataset.profile.copy()
    profilo.update(driver="GTiff", dtype="int16", nodata=NODATA, count=1, compress="deflate",
                   predictor=2, tiled=True, blockxsize=256, blockysize=256)
    with rasterio.open(percorso, "w", **profilo) as uscita:
        uscita.write(in_int16(dataset.read(1, masked=False)), 1)
        uscita.scales = (1 / SCALA,)


def apri_geotiff(percorso):
    return rasterio.open(percorso)


def apri_in_memoria(contenuto: bytes):
    """Il GeoTIFF ricevuto, aperto senza passare dal disco (chiudere memoria e dataset)."""
    memoria = MemoryFile(contenuto)
    return memoria, memoria.open()

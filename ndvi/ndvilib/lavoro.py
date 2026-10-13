"""Il giro: per ogni azienda, passaggi nuovi → GeoTIFF → statistiche per vigneto → SQLite."""

import gc
import logging
from dataclasses import dataclass, field
from datetime import date, timedelta
from pathlib import Path

from . import calcolo
from .rete import ErroreRete, giorno_dopo

log = logging.getLogger("ndvi")


@dataclass
class Riassunto:
    aziende: int = 0
    passaggi: int = 0
    vigneti: set = field(default_factory=set)
    errori: int = 0

    def testo(self):
        return (f"{self.aziende} aziende, {self.passaggi} passaggi, "
                f"{len(self.vigneti)} vigneti aggiornati, {self.errori} errori")


def periodo(archivio, id_azienda, oggi: date, giorni_prima_volta: int, da: date | None):
    """Da quando cercare. Con --da: da quella data (le date già fatte si saltano dopo, così
    lo storico si riprende da dove si era fermato); senza: dal giorno dopo l'ultimo elaborato,
    o gli ultimi N giorni la prima volta."""
    if da is not None:
        return da
    ultimo = archivio.ultimo_elaborato(id_azienda)
    if ultimo is None:
        return oggi - timedelta(days=giorni_prima_volta)
    return giorno_dopo(ultimo)


def elabora_azienda(conf, copernicus, api, archivio, azienda, oggi, da, riassunto):
    id_azienda = int(azienda["id"])
    nome = azienda.get("nome") or f"Azienda {id_azienda}"
    archivio.ricorda_azienda(id_azienda, nome)

    vigneti = calcolo.vigneti_da_geojson(api.vigneti_geojson(id_azienda))
    if not vigneti:
        log.info("%s (%d): nessun vigneto con poligono", nome, id_azienda)
        return
    gruppi, troppo_grandi = calcolo.riquadri(vigneti, conf.risoluzione_m, conf.max_pixel,
                                             conf.margine_m)
    if troppo_grandi:
        log.warning("%s (%d): vigneti troppo grandi per una richiesta, saltati: %s", nome,
                    id_azienda, ", ".join(str(v.id) for v in troppo_grandi[:10]))
    if not gruppi:
        return

    dal = periodo(archivio, id_azienda, oggi, conf.giorni_prima_volta, da)
    if dal > oggi:
        return
    giorni = copernicus.date_passaggi(calcolo.bbox_vigneti(vigneti), dal, oggi)
    fatti = archivio.date_elaborate(id_azienda)
    nuovi = [g for g in giorni if g not in fatti]
    log.info("%s (%d): %d vigneti in %d riquadri, %d passaggi nuovi dal %s", nome, id_azienda,
             len(vigneti), len(gruppi), len(nuovi), dal.isoformat())

    cartella = conf.cartella_dati / str(id_azienda)
    cartella.mkdir(parents=True, exist_ok=True)
    for giorno in nuovi:
        # In ordine di data: al primo errore l'azienda si ferma (lo rilancia il giro dopo),
        # così un passaggio fallito non resta scavalcato da quelli dopo.
        statistiche, file = elabora_passaggio(conf, copernicus, gruppi, cartella, giorno)
        nuvole = calcolo.nuvole_passaggio(statistiche)
        valido = nuvole is not None and nuvole <= conf.nuvole_max_pct
        archivio.salva_passaggio(id_azienda, giorno, statistiche, nuvole, valido, ";".join(file))
        riassunto.passaggi += 1
        riassunto.vigneti.update(s.id_vigneto for s in statistiche if s.pixel_validi)
        log.info("%s (%d) %s: nuvole %s%%, %s", nome, id_azienda, giorno,
                 "?" if nuvole is None else f"{nuvole:.0f}", "pulito" if valido else "scartato")


def elabora_passaggio(conf, copernicus, gruppi, cartella: Path, giorno):
    """Un GeoTIFF per riquadro (dati/{azienda}/{data}.tif, o {data}-N.tif se più d'uno)."""
    statistiche, file = [], []
    for n, riquadro in enumerate(gruppi, start=1):
        nome = f"{giorno}.tif" if len(gruppi) == 1 else f"{giorno}-{n}.tif"
        percorso = cartella / nome
        contenuto = copernicus.geotiff_ndvi(riquadro.bbox, giorno, riquadro.larghezza,
                                            riquadro.altezza)
        provvisorio = percorso.with_suffix(".tif.tmp")
        provvisorio.write_bytes(contenuto)
        provvisorio.replace(percorso)
        del contenuto
        with calcolo.apri_geotiff(percorso) as dataset:
            statistiche += calcolo.statistiche(dataset, riquadro.vigneti)
        file.append(nome)
    return statistiche, file


def esegui(conf, copernicus, api, archivio, oggi: date, da: date | None = None,
           solo_azienda: int | None = None) -> Riassunto:
    riassunto = Riassunto()
    aziende = api.aziende()
    if solo_azienda is not None:
        aziende = [a for a in aziende if int(a["id"]) == solo_azienda]
    for azienda in aziende:
        try:
            elabora_azienda(conf, copernicus, api, archivio, azienda, oggi, da, riassunto)
        except (ErroreRete, OSError, ValueError) as e:
            riassunto.errori += 1
            log.error("azienda %s: %s", azienda.get("id"), e)
        riassunto.aziende += 1
        gc.collect()  # una sola azienda in memoria per volta
    return riassunto

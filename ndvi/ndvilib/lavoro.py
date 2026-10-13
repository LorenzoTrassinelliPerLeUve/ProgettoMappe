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
    raster_cancellati: int = 0
    spazio_dati: int = 0  # byte occupati dalla cartella dati/ a fine giro

    def testo(self):
        testo = (f"{self.aziende} aziende, {self.passaggi} passaggi, "
                 f"{len(self.vigneti)} vigneti aggiornati, {self.errori} errori")
        if self.raster_cancellati:
            testo += f", {self.raster_cancellati} GeoTIFF vecchi cancellati"
        return testo + f"; dati/ occupa {megabyte(self.spazio_dati)}"


def megabyte(byte: int) -> str:
    return f"{byte / 1_000_000:.1f} MB"


def spazio_occupato(cartella: Path) -> int:
    """Byte dei file sotto `cartella` (0 se non esiste)."""
    if not cartella.exists():
        return 0
    return sum(f.stat().st_size for f in cartella.rglob("*") if f.is_file())


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
    if not conf.senza_raster:
        cartella.mkdir(parents=True, exist_ok=True)
    for giorno in nuovi:
        # In ordine di data: al primo errore l'azienda si ferma (lo rilancia il giro dopo),
        # così un passaggio fallito non resta scavalcato da quelli dopo.
        statistiche, file = elabora_passaggio(conf, copernicus, gruppi, cartella, giorno)
        nuvole = calcolo.nuvole_passaggio(statistiche)
        valido = nuvole is not None and nuvole <= conf.nuvole_max_pct
        archivio.salva_passaggio(id_azienda, giorno, statistiche, nuvole, valido,
                                 ";".join(file) or None)
        riassunto.passaggi += 1
        riassunto.vigneti.update(s.id_vigneto for s in statistiche if s.pixel_validi)
        log.info("%s (%d) %s: nuvole %s%%, %s", nome, id_azienda, giorno,
                 "?" if nuvole is None else f"{nuvole:.0f}", "pulito" if valido else "scartato")


def elabora_passaggio(conf, copernicus, gruppi, cartella: Path, giorno):
    """Statistiche dal GeoTIFF ricevuto (in memoria). Salvo con --senza-raster, il GeoTIFF va su
    disco in int16 compresso: dati/{azienda}/{data}.tif, o {data}-N.tif se i riquadri sono più
    d'uno. Restituisce le statistiche e i nomi dei file salvati (vuoto se nessuno)."""
    statistiche, file = [], []
    for n, riquadro in enumerate(gruppi, start=1):
        contenuto = copernicus.geotiff_ndvi(riquadro.bbox, giorno, riquadro.larghezza,
                                            riquadro.altezza)
        memoria, dataset = calcolo.apri_in_memoria(contenuto)
        try:
            statistiche += calcolo.statistiche(dataset, riquadro.vigneti)
            if not conf.senza_raster:
                nome = f"{giorno}.tif" if len(gruppi) == 1 else f"{giorno}-{n}.tif"
                percorso = cartella / nome
                provvisorio = percorso.with_suffix(".tif.tmp")
                calcolo.scrivi_compresso(dataset, provvisorio)
                provvisorio.replace(percorso)
                file.append(nome)
        finally:
            dataset.close()
            memoria.close()
        del contenuto
    return statistiche, file


def pulisci_raster(conf, archivio, oggi: date, solo_azienda: int | None = None) -> int:
    """Cancella i GeoTIFF dei passaggi più vecchi di `conserva_raster_giorni` (le statistiche
    restano in SQLite, `passaggi.file` diventa NULL). Restituisce i file cancellati."""
    if conf.conserva_raster_giorni <= 0:
        return 0
    limite = (oggi - timedelta(days=conf.conserva_raster_giorni)).isoformat()
    cancellati = 0
    for id_azienda, giorno, file in archivio.passaggi_con_file_prima_di(limite):
        if solo_azienda is not None and id_azienda != solo_azienda:
            continue
        for nome in file.split(";"):
            percorso = conf.cartella_dati / str(id_azienda) / nome
            if percorso.is_file():
                percorso.unlink()
                cancellati += 1
        archivio.togli_file(id_azienda, giorno)
    if cancellati:
        log.info("cancellati %d GeoTIFF più vecchi del %s", cancellati, limite)
    return cancellati


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
    try:
        riassunto.raster_cancellati = pulisci_raster(conf, archivio, oggi, solo_azienda)
    except OSError as e:
        riassunto.errori += 1
        log.error("pulizia dei GeoTIFF: %s", e)
    riassunto.spazio_dati = spazio_occupato(conf.cartella_dati)
    return riassunto

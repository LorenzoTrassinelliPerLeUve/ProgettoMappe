"""Il giro: per ogni azienda, passaggi nuovi → GeoTIFF → statistiche per vigneto → SQLite."""

import gc
import logging
from dataclasses import dataclass, field
from datetime import date, timedelta
from pathlib import Path

from . import calcolo
from .rete import ErroreQuota, ErroreRete, giorno_dopo

log = logging.getLogger("ndvi")

QUOTA_ESAURITA = "quota Copernicus esaurita, riprovo domani"


class TettoRaggiunto(Exception):
    """Il prossimo passo supererebbe il tetto del giro: ci si ferma pulito."""


@dataclass
class Tetto:
    """PU stimate e richieste (Catalog + Process API) del giro, con il loro massimo."""
    pu_max: float
    richieste_max: int
    attivo: bool = True
    pu: float = 0.0
    richieste: int = 0

    def verifica(self, pu, richieste):
        if not self.attivo:
            return
        if self.pu + pu > self.pu_max or self.richieste + richieste > self.richieste_max:
            raise TettoRaggiunto(
                f"tetto del giro raggiunto ({self.pu:.1f} PU su {self.pu_max:g}, "
                f"{self.richieste} richieste su {self.richieste_max}), riprendo al prossimo giro")

    def aggiungi(self, pu, richieste):
        self.pu += pu
        self.richieste += richieste


@dataclass
class Riassunto:
    aziende: int = 0
    passaggi: int = 0
    scartati_catalogo: int = 0
    vigneti: set = field(default_factory=set)
    errori: int = 0
    raster_cancellati: int = 0
    spazio_dati: int = 0  # byte occupati dalla cartella dati/ a fine giro
    pu: float = 0.0
    richieste: int = 0
    fermato: str | None = None  # perché il giro si è fermato prima della fine

    def testo(self):
        testo = (f"{self.aziende} aziende, {self.passaggi} passaggi, "
                 f"{len(self.vigneti)} vigneti aggiornati, {self.errori} errori")
        if self.scartati_catalogo:
            testo += f", {self.scartati_catalogo} passaggi saltati per nuvole dal catalogo"
        if self.raster_cancellati:
            testo += f", {self.raster_cancellati} GeoTIFF vecchi cancellati"
        testo += f"; {self.pu:.1f} PU stimate in {self.richieste} richieste"
        if self.fermato:
            testo += f"; {self.fermato}"
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


def chiamata_registrata(copernicus, archivio, tetto, riassunto, id_azienda, tipo, pixel, pu,
                        funzione):
    """Esegue una chiamata a Copernicus e la scrive nel registro `richieste` (anche se fallisce),
    contando PU stimate e richieste HTTP (tentativi e pagine comprese) nel tetto e nel riassunto."""
    prima = copernicus.richieste_quota
    esito = "errore"
    try:
        risultato = funzione()
        esito = "ok"
        return risultato
    except ErroreQuota:
        esito = "quota"
        raise
    finally:
        fatte = copernicus.richieste_quota - prima
        pu_vere = pu if esito == "ok" else 0.0
        archivio.registra_richiesta(id_azienda, tipo, pixel, pu, fatte, esito)
        tetto.aggiungi(pu_vere, fatte)
        riassunto.pu += pu_vere
        riassunto.richieste += fatte


def elabora_azienda(conf, copernicus, api, archivio, azienda, oggi, da, riassunto, tetto):
    id_azienda = int(azienda["id"])
    nome = azienda.get("nome") or f"Azienda {id_azienda}"
    archivio.ricorda_azienda(id_azienda, nome)

    vigneti = calcolo.vigneti_da_geojson(api.vigneti_geojson(id_azienda))
    if not vigneti:
        log.info("%s (%d): nessun vigneto con poligono", nome, id_azienda)
        return
    gruppi, troppo_grandi = calcolo.riquadri(vigneti, conf.risoluzione_m, conf.max_pixel,
                                             conf.margine_m, conf.distanza_gruppi_m)
    if troppo_grandi:
        log.warning("%s (%d): vigneti troppo grandi per una richiesta, saltati: %s", nome,
                    id_azienda, ", ".join(str(v.id) for v in troppo_grandi[:10]))
    if not gruppi:
        return

    dal = periodo(archivio, id_azienda, oggi, conf.giorni_prima_volta, da)
    if dal > oggi:
        return
    tetto.verifica(0, 1)
    giorni = chiamata_registrata(
        copernicus, archivio, tetto, riassunto, id_azienda, "catalogo", 0, 0.0,
        lambda: copernicus.date_passaggi(calcolo.bbox_vigneti(vigneti), dal, oggi))
    fatti = archivio.date_elaborate(id_azienda)
    nuovi = [g for g in giorni if g not in fatti]
    pu_passaggio = sum(calcolo.pu_stimate(r.larghezza, r.altezza) for r in gruppi)
    log.info("%s (%d): %d vigneti in %d riquadri (%.2f PU stimate a passaggio), "
             "%d passaggi nuovi dal %s", nome, id_azienda, len(vigneti), len(gruppi),
             pu_passaggio, len(nuovi), dal.isoformat())

    cartella = conf.cartella_dati / str(id_azienda)
    if not conf.senza_raster:
        cartella.mkdir(parents=True, exist_ok=True)
    for giorno in nuovi:
        nuvole_catalogo = giorni[giorno]
        if nuvole_catalogo is not None and nuvole_catalogo > conf.nuvole_catalogo_max_pct:
            # Si sa già che è nuvoloso: niente Process API, il passaggio resta come scartato.
            archivio.salva_passaggio(id_azienda, giorno, [], nuvole_catalogo, False, None,
                                     scartato_catalogo=True)
            riassunto.scartati_catalogo += 1
            log.info("%s (%d) %s: nuvole %.0f%% dal catalogo, saltato", nome, id_azienda, giorno,
                     nuvole_catalogo)
            continue
        tetto.verifica(pu_passaggio, len(gruppi))
        # In ordine di data: al primo errore l'azienda si ferma (lo rilancia il giro dopo),
        # così un passaggio fallito non resta scavalcato da quelli dopo.
        statistiche, file = elabora_passaggio(conf, copernicus, gruppi, cartella, giorno,
                                              archivio, tetto, riassunto, id_azienda)
        nuvole = calcolo.nuvole_passaggio(statistiche)
        valido = nuvole is not None and nuvole <= conf.nuvole_max_pct
        archivio.salva_passaggio(id_azienda, giorno, statistiche, nuvole, valido,
                                 ";".join(file) or None)
        riassunto.passaggi += 1
        riassunto.vigneti.update(s.id_vigneto for s in statistiche if s.pixel_validi)
        log.info("%s (%d) %s: nuvole %s%%, %s, %d richieste, %.2f PU stimate", nome, id_azienda,
                 giorno, "?" if nuvole is None else f"{nuvole:.0f}",
                 "pulito" if valido else "scartato", len(gruppi), pu_passaggio)


def elabora_passaggio(conf, copernicus, gruppi, cartella: Path, giorno, archivio, tetto,
                      riassunto, id_azienda):
    """Statistiche dal GeoTIFF ricevuto (in memoria). Salvo con --senza-raster, il GeoTIFF va su
    disco in int16 compresso: dati/{azienda}/{data}.tif, o {data}-N.tif se i riquadri sono più
    d'uno. Restituisce le statistiche e i nomi dei file salvati (vuoto se nessuno)."""
    statistiche, file = [], []
    for n, riquadro in enumerate(gruppi, start=1):
        pixel = riquadro.larghezza * riquadro.altezza
        pu = calcolo.pu_stimate(riquadro.larghezza, riquadro.altezza)
        log.debug("riquadro %d: %d × %d pixel, %.2f PU stimate", n, riquadro.larghezza,
                  riquadro.altezza, pu)
        contenuto = chiamata_registrata(
            copernicus, archivio, tetto, riassunto, id_azienda, "process", pixel, pu,
            lambda: copernicus.geotiff_ndvi(riquadro.bbox, giorno, riquadro.larghezza,
                                            riquadro.altezza))
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
           solo_azienda: int | None = None, senza_tetto: bool = False) -> Riassunto:
    riassunto = Riassunto()
    tetto = Tetto(conf.pu_max_per_giro, conf.richieste_max_per_giro, attivo=not senza_tetto)
    aziende = api.aziende()
    if solo_azienda is not None:
        aziende = [a for a in aziende if int(a["id"]) == solo_azienda]
    for azienda in aziende:
        riassunto.aziende += 1
        try:
            elabora_azienda(conf, copernicus, api, archivio, azienda, oggi, da, riassunto, tetto)
        except TettoRaggiunto as e:
            riassunto.fermato = str(e)
            log.info("azienda %s: %s", azienda.get("id"), e)
            break
        except ErroreQuota as e:
            # Non si ritenta e non si passa alle altre aziende: fallirebbero tutte.
            riassunto.errori += 1
            riassunto.fermato = QUOTA_ESAURITA
            log.error("azienda %s: %s; %s", azienda.get("id"), e, QUOTA_ESAURITA)
            break
        except (ErroreRete, OSError, ValueError) as e:
            riassunto.errori += 1
            log.error("azienda %s: %s", azienda.get("id"), e)
        finally:
            gc.collect()  # una sola azienda in memoria per volta
    try:
        riassunto.raster_cancellati = pulisci_raster(conf, archivio, oggi, solo_azienda)
    except OSError as e:
        riassunto.errori += 1
        log.error("pulizia dei GeoTIFF: %s", e)
    riassunto.spazio_dati = spazio_occupato(conf.cartella_dati)
    return riassunto

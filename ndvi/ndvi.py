#!/usr/bin/env python3
"""NDVI medio per vigneto da Sentinel-2 (Copernicus Data Space) in SQLite. Vedi LEGGIMI.md.

  ndvi.py                     passaggi nuovi di tutte le aziende (il giro notturno)
  ndvi.py --da 2019-01-01     storico da quella data (riprendibile: le date fatte si saltano)
  ndvi.py --azienda 123       solo un'azienda
  ndvi.py --stato             ultimo passaggio pulito e spazio dei GeoTIFF per azienda
  ndvi.py --da 2019-01-01 --senza-raster   storico senza salvare i GeoTIFF
  ndvi.py --da 2019-01-01 --senza-tetto    storico senza il tetto di PU e richieste per giro
  ndvi.py --quota             PU stimate e richieste di oggi e degli ultimi 30 giorni
"""

import argparse
import logging
import sys
from datetime import date, timedelta
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from ndvilib import configurazione  # noqa: E402
from ndvilib.archivio import Archivio  # noqa: E402
from ndvilib.lavoro import QUOTA_ESAURITA, esegui, megabyte, spazio_occupato  # noqa: E402
from ndvilib.rete import ApiMappe, Copernicus, oggi_utc  # noqa: E402


def avviso_quota(archivio):
    """Riga per --stato e --quota se l'ultima risposta di Copernicus era «quota esaurita»."""
    quando = archivio.quota_esaurita_dal()
    if quando:
        print(f"Attenzione: {QUOTA_ESAURITA} (risposta del {quando[:16].replace('T', ' ')} UTC).")


def stampa_quota(archivio, oggi: date):
    for testo, dal in (("Oggi (UTC)", oggi), ("Ultimi 30 giorni", oggi - timedelta(days=29))):
        pu, richieste = archivio.uso_quota(dal.isoformat())
        print(f"{testo}: {pu:.1f} PU stimate, {richieste} richieste")
    print("Piano gratuito di Copernicus: circa 10.000 PU al mese.")
    avviso_quota(archivio)


def stampa_stato(archivio, cartella_dati):
    righe = archivio.stato()
    if not righe:
        print("Nessun passaggio elaborato.")
        avviso_quota(archivio)
        return
    for id_azienda, nome, pulito, nuvole, ultimo, numero in righe:
        if pulito:
            testo_pulito = f"ultimo pulito {pulito} (nuvole {nuvole:.0f}%)"
        else:
            testo_pulito = "nessun passaggio pulito"
        print(f"{nome or 'Azienda'} ({id_azienda}): {testo_pulito}; "
              f"ultimo elaborato {ultimo}; {numero} passaggi; "
              f"GeoTIFF {megabyte(spazio_occupato(cartella_dati / str(id_azienda)))}")
    print(f"Cartella dati/: {megabyte(spazio_occupato(cartella_dati))}")
    avviso_quota(archivio)


def main(argomenti=None):
    parser = argparse.ArgumentParser(description="NDVI medio per vigneto da Sentinel-2")
    parser.add_argument("--config", type=Path, default=Path(__file__).resolve().parent / "config.json")
    parser.add_argument("--da", type=date.fromisoformat, help="storico da AAAA-MM-GG")
    parser.add_argument("--azienda", type=int, help="solo questa azienda (Id)")
    parser.add_argument("--stato", action="store_true", help="ultimo passaggio pulito per azienda")
    parser.add_argument("--senza-raster", action="store_true",
                        help="statistiche in memoria, senza salvare i GeoTIFF")
    parser.add_argument("--senza-tetto", action="store_true",
                        help="ignora pu_max_per_giro e richieste_max_per_giro")
    parser.add_argument("--quota", action="store_true",
                        help="PU stimate e richieste di oggi e degli ultimi 30 giorni")
    parser.add_argument("-v", "--verboso", action="store_true")
    a = parser.parse_args(argomenti)

    logging.basicConfig(level=logging.DEBUG if a.verboso else logging.INFO,
                        format="%(asctime)s %(levelname)s %(message)s")
    conf = configurazione.carica(a.config)
    if a.senza_raster:
        conf.senza_raster = True
    conf.cartella_dati.mkdir(parents=True, exist_ok=True)
    archivio = Archivio(conf.database)
    try:
        if a.stato:
            stampa_stato(archivio, conf.cartella_dati)
            return 0
        if a.quota:
            stampa_quota(archivio, oggi_utc())
            return 0
        credenziali = configurazione.leggi_credenziali(conf.percorso(conf.credenziali_file))
        riassunto = esegui(conf, Copernicus(conf, credenziali), ApiMappe(conf), archivio,
                           oggi_utc(), a.da, a.azienda, a.senza_tetto)
        print(riassunto.testo())
        return 1 if riassunto.errori else 0
    finally:
        archivio.chiudi()


if __name__ == "__main__":
    sys.exit(main())

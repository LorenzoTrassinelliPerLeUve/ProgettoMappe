#!/usr/bin/env python3
"""NDVI medio per vigneto da Sentinel-2 (Copernicus Data Space) in SQLite. Vedi LEGGIMI.md.

  ndvi.py                     passaggi nuovi di tutte le aziende (il giro notturno)
  ndvi.py --da 2019-01-01     storico da quella data (riprendibile: le date fatte si saltano)
  ndvi.py --azienda 123       solo un'azienda
  ndvi.py --stato             ultimo passaggio pulito per azienda
"""

import argparse
import logging
import sys
from datetime import date
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from ndvilib import configurazione  # noqa: E402
from ndvilib.archivio import Archivio  # noqa: E402
from ndvilib.lavoro import esegui  # noqa: E402
from ndvilib.rete import ApiMappe, Copernicus, oggi_utc  # noqa: E402


def stampa_stato(archivio):
    righe = archivio.stato()
    if not righe:
        print("Nessun passaggio elaborato.")
        return
    for id_azienda, nome, pulito, nuvole, ultimo, numero in righe:
        if pulito:
            testo_pulito = f"ultimo pulito {pulito} (nuvole {nuvole:.0f}%)"
        else:
            testo_pulito = "nessun passaggio pulito"
        print(f"{nome or 'Azienda'} ({id_azienda}): {testo_pulito}; "
              f"ultimo elaborato {ultimo}; {numero} passaggi")


def main(argomenti=None):
    parser = argparse.ArgumentParser(description="NDVI medio per vigneto da Sentinel-2")
    parser.add_argument("--config", type=Path, default=Path(__file__).resolve().parent / "config.json")
    parser.add_argument("--da", type=date.fromisoformat, help="storico da AAAA-MM-GG")
    parser.add_argument("--azienda", type=int, help="solo questa azienda (Id)")
    parser.add_argument("--stato", action="store_true", help="ultimo passaggio pulito per azienda")
    parser.add_argument("-v", "--verboso", action="store_true")
    a = parser.parse_args(argomenti)

    logging.basicConfig(level=logging.DEBUG if a.verboso else logging.INFO,
                        format="%(asctime)s %(levelname)s %(message)s")
    conf = configurazione.carica(a.config)
    conf.cartella_dati.mkdir(parents=True, exist_ok=True)
    archivio = Archivio(conf.database)
    try:
        if a.stato:
            stampa_stato(archivio)
            return 0
        credenziali = configurazione.leggi_credenziali(conf.percorso(conf.credenziali_file))
        riassunto = esegui(conf, Copernicus(conf, credenziali), ApiMappe(conf), archivio,
                           oggi_utc(), a.da, a.azienda)
        print(riassunto.testo())
        return 1 if riassunto.errori else 0
    finally:
        archivio.chiudi()


if __name__ == "__main__":
    sys.exit(main())

"""Test senza rete: file degli avvisi finti, invii sostituiti da liste.

    cd notifiche-avvisi && python3 -m unittest discover -s tests
"""

import contextlib
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import avvisi_notifica as an  # noqa: E402

BASE = "https://mappe.esempio.it/"


def regola(id_, breve, disponibile=True, ferma=None):
    return {"id": id_, "nome": breve, "breve": breve, "disponibile": disponibile, "conteggio": 0, "ferma": ferma}


def avviso(regola_, azienda, nome_azienda, oggetto):
    return {"chiave": f"{regola_}:{oggetto}", "regola": regola_, "aziendaId": azienda, "azienda": nome_azienda,
            "oggetto": "vigneto", "idVigneto": 1, "nome": oggetto, "valore": "", "giorni": None,
            "link": f"{BASE}mappa/{azienda}?vigneto=1"}


def file_avvisi(giorno, avvisi, regole=None):
    regole = regole or [regola("senza-rilievi", "Senza rilievi"), regola("stazione-ferma", "Stazione ferma")]
    aziende = {}
    for a in avvisi:
        aziende.setdefault(a["aziendaId"], {"id": a["aziendaId"], "nome": a["azienda"], "conteggio": 0,
                                            "link": f"{BASE}mappa/{a['aziendaId']}",
                                            "linkAvvisi": f"{BASE}avvisi?azienda={a['aziendaId']}"})
    return {"calcolati": None, "giorno": giorno, "totale": len(avvisi), "regole": regole,
            "aziende": list(aziende.values()), "avvisi": avvisi}


class Giro(unittest.TestCase):
    def setUp(self):
        self.cartella = Path(tempfile.mkdtemp())
        self.file = self.cartella / "avvisi.json"
        self.stato = self.cartella / "stato.json"
        self.notifiche, self.telegram = [], []

    def gira(self, giorno, dati, *altri):
        self.file.write_text(json.dumps(dati))
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            return an.main(["--file", str(self.file), "--stato", str(self.stato), *altri],
                           manda_notifica=lambda *a: self.notifiche.append(a),
                           manda_telegram=lambda t: self.telegram.append(t), giorno=giorno)

    def test_il_primo_giro_salva_la_foto_e_non_manda_niente(self):
        self.gira("2026-10-30", file_avvisi("2026-10-30", [avviso("senza-rilievi", 1, "Collio", "v10")]))
        self.assertEqual([], self.telegram)
        self.assertEqual(["senza-rilievi:v10"], json.loads(self.stato.read_text())["chiavi"])

    def test_solo_i_nuovi_raggruppati_per_azienda_un_messaggio_al_giorno(self):
        vecchio = avviso("senza-rilievi", 1, "Collio", "v10")
        self.gira("2026-10-29", file_avvisi("2026-10-29", [vecchio]))
        oggi = file_avvisi("2026-10-30", [vecchio, avviso("senza-rilievi", 1, "Collio", "v11"),
                                          avviso("stazione-ferma", 1, "Collio", "s3"), avviso("senza-rilievi", 2, "Brda", "v20")])
        self.assertEqual(0, self.gira("2026-10-30", oggi))
        self.assertEqual(1, len(self.telegram))
        testo = self.telegram[0]
        self.assertTrue(testo.startswith("Mappe: 3 avvisi nuovi in 2 aziende"))
        self.assertIn("• Collio: Senza rilievi 1, Stazione ferma 1\n  https://mappe.esempio.it/avvisi?azienda=1", testo)
        self.assertIn("• Brda: Senza rilievi 1\n  https://mappe.esempio.it/avvisi?azienda=2", testo)
        self.assertLess(testo.index("Collio"), testo.index("Brda"))   # prima chi ne ha di più
        self.assertNotIn("v10", testo)
        self.assertEqual([("Mappe: 3 avvisi nuovi in 2 aziende", "Collio (2), Brda (1)", f"{BASE}avvisi")], self.notifiche)

        # Secondo giro nello stesso giorno, anche con un avviso in più: niente.
        oggi["avvisi"].append(avviso("senza-rilievi", 3, "Carso", "v30"))
        self.gira("2026-10-30", oggi)
        self.assertEqual(1, len(self.telegram))
        # Il giorno dopo quello comparso dopo l'invio è nuovo, gli altri no.
        domani = dict(oggi, giorno="2026-10-31")
        self.gira("2026-10-31", domani)
        self.assertEqual(2, len(self.telegram))
        self.assertTrue(self.telegram[1].startswith("Mappe: 1 avviso nuovo (Carso)"))
        self.assertEqual(f"{BASE}avvisi?azienda=3", self.notifiche[1][2])

    def test_regole_ferme_e_non_disponibili_non_contano(self):
        regole = [regola("senza-rilievi", "Senza rilievi"), regola("stazione-ferma", "Stazione ferma")]
        ieri = [avviso("senza-rilievi", 1, "Collio", "v10"), avviso("stazione-ferma", 1, "Collio", "s3")]
        self.gira("2026-10-29", file_avvisi("2026-10-29", ieri, regole))
        # Oggi 4Grapes non risponde per i rilievi e la stazione è fuori stagione: nessun nuovo.
        oggi = [avviso("senza-rilievi", 1, "Collio", "v11"), avviso("stazione-ferma", 1, "Collio", "s4")]
        self.gira("2026-10-30", file_avvisi("2026-10-30", oggi, [regola("senza-rilievi", "Senza rilievi", disponibile=False),
                                                                regola("stazione-ferma", "Stazione ferma", ferma="Regola ferma fino ad aprile")]))
        self.assertEqual([], self.telegram)
        # La foto tiene i rilievi di ieri: domani v10 non è nuovo, v12 sì.
        self.gira("2026-10-31", file_avvisi("2026-10-31", [avviso("senza-rilievi", 1, "Collio", "v10"),
                                                          avviso("senza-rilievi", 1, "Collio", "v12")], regole))
        self.assertEqual(1, len(self.telegram))
        self.assertTrue(self.telegram[0].startswith("Mappe: 1 avviso nuovo (Collio)"))

    def test_file_vecchio_non_manda_e_non_tocca_la_foto(self):
        self.gira("2026-10-29", file_avvisi("2026-10-29", []))
        prima = self.stato.read_text()
        self.gira("2026-10-30", file_avvisi("2026-10-29", [avviso("senza-rilievi", 1, "Collio", "v10")]))
        self.assertEqual([], self.telegram)
        self.assertEqual(prima, self.stato.read_text())

    def test_un_canale_rotto_non_ferma_l_altro(self):
        self.gira("2026-10-29", file_avvisi("2026-10-29", []))
        self.file.write_text(json.dumps(file_avvisi("2026-10-30", [avviso("senza-rilievi", 1, "Collio", "v10")])))

        def rotto(*_):
            raise OSError("rete")

        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            esito = an.main(["--file", str(self.file), "--stato", str(self.stato)], manda_notifica=rotto,
                            manda_telegram=lambda t: self.telegram.append(t), giorno="2026-10-30")
        self.assertEqual(1, esito)
        self.assertEqual(1, len(self.telegram))
        self.assertEqual("2026-10-30", json.loads(self.stato.read_text())["inviato"])

    def test_prova_non_manda_e_non_salva(self):
        self.gira("2026-10-29", file_avvisi("2026-10-29", []))
        prima = self.stato.read_text()
        self.gira("2026-10-30", file_avvisi("2026-10-30", [avviso("senza-rilievi", 1, "Collio", "v10")]), "--prova")
        self.assertEqual([], self.telegram)
        self.assertEqual(prima, self.stato.read_text())

    def test_link_relativi_usano_l_indirizzo_predefinito(self):
        dati = file_avvisi("2026-10-30", [avviso("senza-rilievi", 1, "Collio", "v10")])
        for a in dati["aziende"]:
            a.pop("linkAvvisi")
            a["link"] = "mappa/1"
        base = an.indirizzo_base(dati, "https://altro.it")
        self.assertEqual("https://altro.it/", base)
        self.assertEqual("https://altro.it/avvisi?azienda=1", an.messaggio(dati["avvisi"], dati, base)[3])


if __name__ == "__main__":
    unittest.main()

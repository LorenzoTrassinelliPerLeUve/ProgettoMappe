"""Test senza rete: risposte HTTP finte e GeoTIFF piccoli generati in memoria.

    cd ndvi && ~/progetti/mappe/ndvi/.venv/bin/python -m unittest discover -s tests
"""

import contextlib
import io
import json
import math
import sqlite3
import sys
import tempfile
import unittest
from datetime import date
from pathlib import Path

import numpy as np
import requests
from rasterio.io import MemoryFile
from rasterio.transform import from_bounds
from shapely.geometry import box, mapping

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import ndvi  # noqa: E402
from ndvilib import calcolo, configurazione  # noqa: E402
from ndvilib.archivio import Archivio  # noqa: E402
from ndvilib.lavoro import esegui  # noqa: E402
from ndvilib.rete import ApiMappe, Copernicus, ErroreRete, Limitatore, richiesta  # noqa: E402


class Risposta:
    def __init__(self, status_code=200, corpo=None, content=b""):
        self.status_code = status_code
        self._corpo = corpo
        self.content = content
        self.text = json.dumps(corpo) if corpo is not None else ""

    def json(self):
        return self._corpo


class SessioneFinta:
    """Chiama `gestore(metodo, url, kwargs)` e registra ogni richiesta."""

    def __init__(self, gestore):
        self.gestore = gestore
        self.richieste = []

    def request(self, metodo, url, **kwargs):
        self.richieste.append((metodo, url, kwargs))
        esito = self.gestore(metodo, url, kwargs)
        if isinstance(esito, Exception):
            raise esito
        return esito


def geotiff(bbox, larghezza, altezza, valori=None):
    """GeoTIFF float32 EPSG:4326 in memoria; valori predefiniti: colonna / larghezza."""
    if valori is None:
        valori = np.tile(np.arange(larghezza, dtype="float32") / larghezza, (altezza, 1))
    with MemoryFile() as memoria:
        with memoria.open(driver="GTiff", width=larghezza, height=altezza, count=1,
                          dtype="float32", crs="EPSG:4326",
                          transform=from_bounds(*bbox, larghezza, altezza)) as d:
            d.write(valori.astype("float32"), 1)
        return memoria.read()


def conf_prova(cartella):
    return configurazione.Configurazione(
        dati=str(cartella), chiave_api_file=str(Path(cartella) / "nessuna-chiave.txt"),
        api="http://api.finta")


class TestConfigurazione(unittest.TestCase):
    def test_credenziali_dal_file(self):
        with tempfile.TemporaryDirectory() as t:
            f = Path(t) / "copernicus.txt"
            f.write_text("# finto\nCLIENT_ID = id-finto\n\nCLIENT_SECRET=segreto=con=uguali\n")
            self.assertEqual(configurazione.leggi_credenziali(f), ("id-finto", "segreto=con=uguali"))
            f.write_text("CLIENT_ID=x\n")
            with self.assertRaises(ValueError):
                configurazione.leggi_credenziali(f)

    def test_config_json(self):
        with tempfile.TemporaryDirectory() as t:
            f = Path(t) / "config.json"
            self.assertEqual(configurazione.carica(f).giorni_prima_volta, 60)
            f.write_text('{"giorni_prima_volta": 10}')
            self.assertEqual(configurazione.carica(f).giorni_prima_volta, 10)
            f.write_text('{"giorni_prma_volta": 10}')
            with self.assertRaises(ValueError):
                configurazione.carica(f)

    def test_config_del_repository_valido(self):
        conf = configurazione.carica(Path(ndvi.__file__).parent / "config.json")
        self.assertEqual(conf.richieste_al_secondo, 4)


class TestRete(unittest.TestCase):
    def test_limitatore_quattro_al_secondo(self):
        adesso = [0.0]
        dormite = []

        def dormi(s):
            dormite.append(s)
            adesso[0] += s

        lim = Limitatore(4, orologio=lambda: adesso[0], dormi=dormi)
        for _ in range(5):
            lim.aspetta()
        self.assertEqual(len(dormite), 4)
        self.assertAlmostEqual(adesso[0], 1.0)

    def test_tentativi_poi_riuscita(self):
        esiti = [requests.ConnectionError("giù"), Risposta(503), Risposta(200, {"ok": 1})]
        s = SessioneFinta(lambda *_: esiti.pop(0))
        r = richiesta(s, "GET", "http://x", 3, dormi=lambda _: None)
        self.assertEqual(r.json(), {"ok": 1})
        self.assertEqual(len(s.richieste), 3)

    def test_tre_tentativi_poi_errore(self):
        s = SessioneFinta(lambda *_: requests.Timeout("lento"))
        with self.assertRaises(ErroreRete):
            richiesta(s, "GET", "http://x", 3, dormi=lambda _: None)
        self.assertEqual(len(s.richieste), 4)  # la prima + 3 tentativi

    def test_errore_4xx_non_si_ripete(self):
        s = SessioneFinta(lambda *_: Risposta(400, {"error": "bbox"}))
        with self.assertRaises(ErroreRete):
            richiesta(s, "GET", "http://x", 3, dormi=lambda _: None)
        self.assertEqual(len(s.richieste), 1)

    def test_token_rinnovato_prima_della_scadenza(self):
        adesso = [0.0]
        token = [0]

        def gestore(metodo, url, kw):
            if "openid-connect" in url:
                token[0] += 1
                return Risposta(200, {"access_token": f"t{token[0]}", "expires_in": 300})
            return Risposta(200, {"features": []})

        s = SessioneFinta(gestore)
        cop = Copernicus(configurazione.Configurazione(), ("id", "sec"), sessione=s,
                         orologio=lambda: adesso[0], dormi=lambda _: None,
                         limitatore=Limitatore(1000, dormi=lambda _: None))
        for istante in (0, 200, 250):
            adesso[0] = istante
            cop.date_passaggi((0, 0, 1, 1), date(2026, 1, 1), date(2026, 1, 2))
        autorizzazioni = [kw["headers"]["Authorization"] for _, u, kw in s.richieste
                          if "catalog" in u]
        self.assertEqual(autorizzazioni, ["Bearer t1", "Bearer t1", "Bearer t2"])

    def test_catalogo_pagine_e_giorni_unici(self):
        pagine = [
            {"features": [{"properties": {"datetime": "2026-09-03T10:20:00Z"}},
                          {"properties": {"datetime": "2026-09-01T10:10:00Z"}}],
             "context": {"next": 2}},
            {"features": [{"properties": {"datetime": "2026-09-03T10:21:00Z"}}], "context": {}},
        ]

        def gestore(metodo, url, kw):
            if "openid-connect" in url:
                return Risposta(200, {"access_token": "t", "expires_in": 3600})
            return Risposta(200, pagine.pop(0))

        s = SessioneFinta(gestore)
        cop = Copernicus(configurazione.Configurazione(), ("id", "sec"), sessione=s,
                         dormi=lambda _: None, limitatore=Limitatore(1000, dormi=lambda _: None))
        giorni = cop.date_passaggi((13, 45, 14, 46), date(2026, 9, 1), date(2026, 9, 5))
        self.assertEqual(giorni, ["2026-09-01", "2026-09-03"])
        corpi = [kw["json"] for _, u, kw in s.richieste if "catalog" in u]
        self.assertEqual(corpi[0]["datetime"], "2026-09-01T00:00:00Z/2026-09-05T23:59:59Z")
        self.assertEqual(corpi[1]["next"], 2)

    def test_chiave_api_solo_se_il_file_esiste(self):
        with tempfile.TemporaryDirectory() as t:
            conf = conf_prova(t)
            self.assertEqual(ApiMappe(conf).intestazioni, {})
            Path(conf.chiave_api_file).write_text("abc\n")
            self.assertEqual(ApiMappe(conf).intestazioni, {"X-Mappe-Chiave": "abc"})


class TestCalcolo(unittest.TestCase):
    BBOX = (13.0, 46.0, 13.01, 46.01)  # 10×10 pixel da 0,001°

    def pixel(self, colonna, riga, colonne=1, righe=1):
        """Poligono che copre esattamente quei pixel (riga 0 = nord)."""
        o, _, _, n = self.BBOX
        return box(o + colonna * 0.001, n - (riga + righe) * 0.001,
                   o + (colonna + colonne) * 0.001, n - riga * 0.001)

    def test_media_mediana_con_maschera_e_nuvole(self):
        valori = np.tile(np.arange(10, dtype="float32") / 10, (10, 1))
        valori[3, 3] = np.nan  # un pixel nuvoloso dentro il vigneto
        vigneti = [calcolo.Vigneto(1, self.pixel(2, 2, 3, 3)),
                   calcolo.Vigneto(2, box(20, 20, 21, 21))]  # fuori dal raster
        with MemoryFile(geotiff(self.BBOX, 10, 10, valori)) as m, m.open() as d:
            s1, s2 = calcolo.statistiche(d, vigneti)
        attesi = [0.2, 0.3, 0.4, 0.2, 0.4, 0.2, 0.3, 0.4]
        self.assertEqual((s1.pixel_totali, s1.pixel_validi), (9, 8))
        self.assertAlmostEqual(s1.media, float(np.mean(attesi)), places=5)
        self.assertAlmostEqual(s1.mediana, float(np.median(attesi)), places=5)
        self.assertAlmostEqual(s1.nuvole_pct, 100 / 9)
        self.assertEqual((s2.pixel_totali, s2.media, s2.nuvole_pct), (0, None, None))
        self.assertAlmostEqual(calcolo.nuvole_passaggio([s1, s2]), 100 / 9)

    def test_all_touched_falso(self):
        # Un poligono che tocca 4 pixel ma non ne contiene nessun centro: 0 pixel.
        o, _, _, n = self.BBOX
        piccolo = box(o + 0.0008, n - 0.0012, o + 0.0012, n - 0.0008)
        with MemoryFile(geotiff(self.BBOX, 10, 10)) as m, m.open() as d:
            (s,) = calcolo.statistiche(d, [calcolo.Vigneto(1, piccolo)])
        self.assertEqual((s.pixel_totali, s.media), (0, None))

    def test_tutto_nuvoloso(self):
        valori = np.full((10, 10), np.nan, dtype="float32")
        with MemoryFile(geotiff(self.BBOX, 10, 10, valori)) as m, m.open() as d:
            (s,) = calcolo.statistiche(d, [calcolo.Vigneto(1, self.pixel(0, 0, 2, 2))])
        self.assertEqual((s.pixel_totali, s.pixel_validi, s.media, s.nuvole_pct), (4, 0, None, 100))

    def test_vigneti_da_geojson_salta_punti_e_sottovigneti(self):
        collezione = {"features": [
            {"type": "Feature", "id": 7, "geometry": mapping(box(13, 46, 13.001, 46.001))},
            {"type": "Feature", "id": 8, "geometry": {"type": "Point", "coordinates": [13, 46]}},
            {"type": "Feature", "geometry": mapping(box(13, 46, 13.001, 46.001))},
        ]}
        self.assertEqual([v.id for v in calcolo.vigneti_da_geojson(collezione)], [7])

    def test_dimensioni_circa_dieci_metri(self):
        larghezza, altezza = calcolo.dimensioni_pixel((13.0, 46.0, 13.01, 46.01), 10)
        self.assertEqual(altezza, 111)
        self.assertEqual(larghezza, math.ceil(0.01 * 111320 * math.cos(math.radians(46.005)) / 10))

    def test_riquadri_spezzati_entro_il_limite(self):
        # 20 vigneti piccoli sparsi su ~15 km: con 300 pixel al massimo servono più riquadri.
        vigneti = [calcolo.Vigneto(i, box(13 + (i % 5) * 0.04, 46 + (i // 5) * 0.03,
                                          13 + (i % 5) * 0.04 + 0.002, 46 + (i // 5) * 0.03 + 0.002))
                   for i in range(20)]
        gruppi, grandi = calcolo.riquadri(vigneti, 10, 300, 30)
        self.assertEqual(grandi, [])
        self.assertGreater(len(gruppi), 1)
        self.assertEqual(sorted(v.id for g in gruppi for v in g.vigneti), list(range(20)))
        for g in gruppi:
            self.assertLessEqual(max(g.larghezza, g.altezza), 300)
        uno, _ = calcolo.riquadri(vigneti, 10, 2500, 30)
        self.assertEqual(len(uno), 1)

    def test_vigneto_troppo_grande_da_solo(self):
        gruppi, grandi = calcolo.riquadri([calcolo.Vigneto(1, box(13, 46, 13.5, 46.5))], 10, 2500, 0)
        self.assertEqual((gruppi, [v.id for v in grandi]), ([], [1]))


class FintiServizi:
    """Api locale + Copernicus finti. `nuvolosi`: giorni tutti NaN; `rotti`: giorni con errore 500."""

    def __init__(self, giorni, nuvolosi=(), rotti=()):
        self.giorni = giorni
        self.nuvolosi = set(nuvolosi)
        self.rotti = set(rotti)
        self.process = []
        self.catalogo = []

    def gestore(self, metodo, url, kw):
        if url == "http://api.finta/api/aziende":
            return Risposta(200, [{"id": 5, "nome": "Cantina Prova"}, {"id": 6, "nome": "Vuota"}])
        if url == "http://api.finta/api/aziende/5/vigneti/esporta":
            assert kw["params"] == {"formato": "geojson"}
            return Risposta(200, {"type": "FeatureCollection", "features": [
                {"type": "Feature", "id": 101, "geometry": mapping(box(13.002, 46.002, 13.004, 46.004))},
                {"type": "Feature", "id": 102, "geometry": mapping(box(13.006, 46.006, 13.008, 46.008))},
            ]})
        if url == "http://api.finta/api/aziende/6/vigneti/esporta":
            return Risposta(200, {"type": "FeatureCollection", "features": []})
        if "openid-connect" in url:
            assert kw["data"]["client_secret"] == "sec"
            return Risposta(200, {"access_token": "t", "expires_in": 3600})
        if "catalog" in url:
            self.catalogo.append(kw["json"])
            dal, al = kw["json"]["datetime"][:10], kw["json"]["datetime"][21:31]
            return Risposta(200, {"features": [
                {"properties": {"datetime": f"{g}T10:00:00Z"}} for g in self.giorni if dal <= g <= al]})
        if "process" in url:
            corpo = kw["json"]
            giorno = corpo["input"]["data"][0]["dataFilter"]["timeRange"]["from"][:10]
            self.process.append(giorno)
            assert "SCL" in corpo["evalscript"]
            if giorno in self.rotti:
                return Risposta(500)
            w, h = corpo["output"]["width"], corpo["output"]["height"]
            valori = np.full((h, w), np.nan if giorno in self.nuvolosi else 0.5, dtype="float32")
            return Risposta(200, content=geotiff(corpo["input"]["bounds"]["bbox"], w, h, valori))
        raise AssertionError(f"richiesta inattesa {metodo} {url}")


class TestGiro(unittest.TestCase):
    def setUp(self):
        self.cartella = tempfile.TemporaryDirectory()
        self.conf = conf_prova(self.cartella.name)
        self.archivio = Archivio(self.conf.database)

    def tearDown(self):
        self.archivio.chiudi()
        self.cartella.cleanup()

    def giro(self, servizi, oggi=date(2026, 10, 13), da=None):
        s = SessioneFinta(servizi.gestore)
        cop = Copernicus(self.conf, ("id", "sec"), sessione=s, dormi=lambda _: None,
                         limitatore=Limitatore(1000, dormi=lambda _: None))
        return esegui(self.conf, cop, ApiMappe(self.conf, sessione=s, dormi=lambda _: None),
                      self.archivio, oggi, da)

    def righe(self, sql):
        return sqlite3.connect(self.conf.database).execute(sql).fetchall()

    def test_primo_giro_scrive_file_e_sqlite(self):
        servizi = FintiServizi(["2026-08-01", "2026-10-01", "2026-10-06"], nuvolosi=["2026-10-06"])
        r = self.giro(servizi)
        # Prima volta: ultimi 60 giorni, il 1° agosto resta fuori.
        self.assertEqual(servizi.catalogo[0]["datetime"][:10], "2026-08-14")
        self.assertEqual(r.testo(), "2 aziende, 2 passaggi, 2 vigneti aggiornati, 0 errori")
        self.assertTrue((Path(self.cartella.name) / "5" / "2026-10-01.tif").is_file())
        self.assertEqual(self.righe("SELECT id_vigneto, data, round(media, 3), pixel_validi > 0, "
                                    "nuvole_pct FROM ndvi WHERE data = '2026-10-01' ORDER BY 1"),
                         [(101, "2026-10-01", 0.5, 1, 0.0), (102, "2026-10-01", 0.5, 1, 0.0)])
        self.assertEqual(self.righe("SELECT data, valido, file, nuvole_pct FROM passaggi ORDER BY data"),
                         [("2026-10-01", 1, "2026-10-01.tif", 0.0),
                          ("2026-10-06", 0, "2026-10-06.tif", 100.0)])

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            ndvi.stampa_stato(self.archivio)
        self.assertIn("Cantina Prova (5): ultimo pulito 2026-10-01 (nuvole 0%); "
                      "ultimo elaborato 2026-10-06", out.getvalue())

    def test_giro_notturno_dal_giorno_dopo_l_ultimo(self):
        self.giro(FintiServizi(["2026-10-01"]))
        servizi = FintiServizi(["2026-10-01", "2026-10-11"])
        r = self.giro(servizi, oggi=date(2026, 10, 14))
        self.assertEqual(servizi.catalogo[0]["datetime"][:10], "2026-10-02")
        self.assertEqual(servizi.process, ["2026-10-11"])
        self.assertEqual(r.passaggi, 1)

    def test_storico_riprende_dopo_un_errore(self):
        giorni = ["2019-01-05", "2019-01-10", "2019-01-15"]
        rotto = FintiServizi(giorni, rotti=["2019-01-10"])
        r = self.giro(rotto, da=date(2019, 1, 1))
        # Al primo errore l'azienda si ferma: il 15 non si tenta, così non scavalca il 10.
        self.assertEqual(rotto.process, ["2019-01-05"] + ["2019-01-10"] * 4)  # 1 + 3 tentativi
        self.assertEqual((r.passaggi, r.errori), (1, 1))

        ripreso = FintiServizi(giorni)
        r = self.giro(ripreso, da=date(2019, 1, 1))
        self.assertEqual(ripreso.process, ["2019-01-10", "2019-01-15"])
        self.assertEqual((r.passaggi, r.errori), (2, 0))
        self.assertEqual(self.righe("SELECT COUNT(*) FROM passaggi WHERE id_azienda = 5"), [(3,)])

    def test_riquadri_multipli_nomi_dei_file(self):
        self.conf.max_pixel = 40  # i due vigneti (~600 m l'uno dall'altro) non stanno insieme
        servizi = FintiServizi(["2026-10-01"])
        r = self.giro(servizi)
        self.assertEqual(servizi.process, ["2026-10-01", "2026-10-01"])
        self.assertEqual(self.righe("SELECT file FROM passaggi"), [("2026-10-01-1.tif;2026-10-01-2.tif",)])
        self.assertEqual(len(r.vigneti), 2)


if __name__ == "__main__":
    unittest.main()

using _4Grapes.Traduzioni;
using _4GrapesAPI.Entity;
using _4GrapesAPI.EntityStorico;
using _4GrapesAPI.Models;
using _4GrapesAPI.Properties;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace _4GrapesAPI.Controllers
{    
    public partial class TrattamentiController : _BaseController
    {
        /// <summary>
        /// ------------------------------------------------------
        /// ---------------- PROBABILITA' STORICO ----------------
        /// -----------------------------------------------------
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Probabilita_Storico(ProbabilitaModel model)
        {
            var DB_Storico = new DB_Storico();

            var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;

            var regioni = DB.VignetoMaps.Where(x => x.AdministrativeAreaLevel1 != null).OrderBy(x => x.AdministrativeAreaLevel1).Select(x => x.AdministrativeAreaLevel1).Distinct().ToList().Select(x => new { Id = x, Nome = x }).ToList();
            var filtro_regioni = regioni.ToList();

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                regioni = regioni.Where(x => x.Id == model.IdRegione).ToList();
            }

            var ids_regioni = regioni.Select(x => x.Id).ToList();

            var province = DB.VignetoMaps.Where(x => x.AdministrativeAreaLevel2 != null && ids_regioni.Contains(x.AdministrativeAreaLevel1)).OrderBy(x => x.AdministrativeAreaLevel1).ThenBy(x => x.AdministrativeAreaLevel2).Select(x => new { x.AdministrativeAreaLevel2, x.AdministrativeAreaLevel1 }).Distinct().ToList().Select(x => new { Id = x.AdministrativeAreaLevel2, Nome = x.AdministrativeAreaLevel2, IdRegione = x.AdministrativeAreaLevel1 }).ToList();
            var filtro_province = province.ToList();

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                province = province.Where(x => x.Id == model.IdProvincia).ToList();
            }

            var ids_province = province.Select(x => x.Id).ToList();

            var comuni = DB.VignetoMaps.Where(x => x.AdministrativeAreaLevel3 != null && ids_province.Contains(x.AdministrativeAreaLevel2)).OrderBy(x => x.AdministrativeAreaLevel1).ThenBy(x => x.AdministrativeAreaLevel2).ThenBy(x => x.AdministrativeAreaLevel3).Select(x => new { x.AdministrativeAreaLevel3, x.AdministrativeAreaLevel2, x.AdministrativeAreaLevel1 }).Distinct().ToList().Select(x => new { Id = x.AdministrativeAreaLevel3, Nome = x.AdministrativeAreaLevel3, IdProvincia = x.AdministrativeAreaLevel2, IdRegione = x.AdministrativeAreaLevel1 }).ToList();
            var filtro_comuni = comuni.ToList();

            if (model.IdComune != null && model.IdComune != "-")
            {
                comuni = comuni.Where(x => x.Id == model.IdComune).ToList();
            }

            var ids_comuni = comuni.Select(x => x.Id).ToList();

            var ids_enti_in_comuni = DB.VignetoMaps.Where(x => ids_comuni.Contains(x.AdministrativeAreaLevel3)).Select(x => x.Vigneto.Vigna.Ente_IdEnte).Distinct().ToList();

            var aziende = DB.Ente.Where(x => x.IdEnte > 0 && x.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && ids_enti_in_comuni.Contains(x.IdEnte)).Select(x => new { Id = x.IdEnte, Nome = x.RagioneSociale }).ToList();
            var filtro_aziende = aziende.ToList();

            if (model.IdAzienda != 0)
            {
                aziende = aziende.Where(x => x.Id == model.IdAzienda).ToList();
            }

            //var oggi = DateTime.Today;
            var dati_meteo_da_giorno = data.StartOfDay();
            var dati_meteo_a_giorno = data.AddDays(7).EndOfDay();

            model.DaGiorno = dati_meteo_da_giorno;
            model.AGiorno = dati_meteo_a_giorno;


            //--- RECUPERO TUTTI I DATI METEO
            var ids_aziende = aziende.Select(x => x.Id).ToList();
            var ids_vigneti = DB.Vigneto.Where(x => ids_aziende.Contains(x.Vigna.Ente_IdEnte)).Select(x => x.IdVigneto).ToList();

            var dati_meteo = DB_Storico.MeteoGiornaliero_Storico.Where(x => x.Data >= dati_meteo_da_giorno && x.Data <= dati_meteo_a_giorno && ids_vigneti.Contains(x.IdVigneto)).Select(x => new DatoMeteoGiornaliero_Storico
            {
                Data = x.Data,
                //AdministrativeAreaLevel3 = x.Vigneto.VignetoMaps.AdministrativeAreaLevel3,
                IdVigneto = x.IdVigneto,
                Precipitazione1 = x.Precipitazione1,
                BagnaturaFogliareProbabilitaAlta = x.BagnaturaFogliareProbabilitaAlta,
                Precipitazione = x.Precipitazione,
            }).ToList();

            //---- INTEGRO DATI AREA AMMINISTRATIVA
            var vigneti_maps = DB.VignetoMaps.Where(x => ids_vigneti.Contains(x.IdVigneto)).ToList();
            foreach(var dato_meteo in dati_meteo)
            {
                dato_meteo.AdministrativeAreaLevel3 = vigneti_maps.Find(x => x.IdVigneto == dato_meteo.IdVigneto)?.AdministrativeAreaLevel3;
            }

            //--- PRENDO SOLO I COMUNI PER CUI CI SONO DATI METEO
            var comuni_dati_meteo = dati_meteo.Select(x => x.AdministrativeAreaLevel3).Distinct().ToList();
            comuni = comuni.Where(x => comuni_dati_meteo.ContainsIgnoreCase(x.Id)).ToList();

            foreach (var comune in comuni)
            {
                var riga_PERONOSPORA = new RigaProbabilita()
                {
                    IdComune = comune.Id,
                    Nome = comune.Nome,
                };

                var riga_OIDIO = new RigaProbabilita()
                {
                    IdComune = comune.Id,
                    Nome = comune.Nome,
                };

                for (int i = 0; i < 5; i++)
                {
                    var da_giorno = data.AddDays(i).StartOfDay();
                    var a_giorno = data.AddDays(i + 2).EndOfDay();

                    //var dati_meteo = DB.MeteoGiornaliero.Where(x => x.Data >= da_giorno && x.Data <= a_giorno && id_aziende_per_comune.Contains(x.Vigneto.Vigna.Ente_IdEnte)).ToList();
                    var dati_meteo_cella = dati_meteo.Where(x => x.Data >= da_giorno && x.Data <= a_giorno && x.AdministrativeAreaLevel3.EqualsIgnoreCase(comune.Id)).ToList();

                    var dati_raggruppati = dati_meteo_cella.GroupBy(x => x.IdVigneto).Select(x => new
                    {
                        IdVigneto = x.Key,
                        NumGiorni = x.Count(),
                        OrePioggiaVigneto = x.Sum(y => y.Precipitazione1),
                        OreBagnaturaVigneto = x.Sum(y => y.BagnaturaFogliareProbabilitaAlta),
                        Precipitazione = x.Sum(y => y.Precipitazione)
                    }).ToList();

                    //--- PRENDO SOLO I DATI PER CUI CI SONO 3 GIORNI PER VIGNETO
                    dati_raggruppati = dati_raggruppati.Where(x => x.NumGiorni == 3).ToList();

                    if (dati_raggruppati.Count == 0)
                    {
                        continue;
                    }

                    var media_ore_pioggia = (decimal)dati_raggruppati.Average(x => x.OrePioggiaVigneto);
                    var media_ore_bagnatura = (decimal)dati_raggruppati.Average(x => x.OreBagnaturaVigneto);
                    var media_mm_pioggia = (decimal)dati_raggruppati.Average(x => x.Precipitazione);

                    //------ PERONOSPORA
                    {
                        var percentuale_ore_pioggia = Utils.MapValue(media_ore_pioggia, 0, 72, 0, 100);
                        var percentuale_ore_bagnatura = Utils.MapValue(media_ore_bagnatura, 0, 72, 0, 100);
                        var percentuale_mm_pioggia = PercentualeMillimetriMPioggia_PERONOSPORA(media_mm_pioggia);

                        riga_PERONOSPORA.Valori[i] = (percentuale_ore_pioggia + percentuale_ore_bagnatura + percentuale_mm_pioggia) / 3;
                        riga_PERONOSPORA.Tooltips[i] = $"{Tr.ore_pioggia}: {media_ore_pioggia.ToString("0.##")} <br> {Tr.ore_bagnatura}: {media_ore_bagnatura.ToString("0.##")}<br> {Tr.mm_pioggia}: {media_mm_pioggia.ToString("0.##")}";
                    }

                    //------- OIDIO
                    {
                        decimal percentuale_ore_pioggia = Utils.MapValue(media_ore_pioggia, 0, 10, 72, 0, 100, 0);
                        decimal percentuale_ore_bagnatura = Utils.MapValue(media_ore_bagnatura, 0, 10, 72, 0, 100, 0);
                        decimal percentuale_mm_pioggia = Utils.MapValue(media_mm_pioggia, 0, 10, 120, 0, 100, 0);

                        riga_OIDIO.Valori[i] = (percentuale_ore_pioggia + percentuale_ore_bagnatura + percentuale_mm_pioggia) / 3;
                        riga_OIDIO.Tooltips[i] = $"{Tr.ore_pioggia}: {media_ore_pioggia.ToString("0.##")} <br> {Tr.ore_bagnatura}: {media_ore_bagnatura.ToString("0.##")}<br> {Tr.mm_pioggia}: {media_mm_pioggia.ToString("0.##")}";
                    }
                }

                model.Righe_PERONOSPORA.Add(riga_PERONOSPORA);
                model.Righe_OIDIO.Add(riga_OIDIO);
            }

            filtro_regioni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_province.Insert(0, new { Id = "-", Nome = "-", IdRegione = "" });
            filtro_comuni.Insert(0, new { Id = "-", Nome = "-", IdProvincia = "", IdRegione = "" });
            filtro_aziende.Insert(0, new { Id = 0, Nome = "-" });

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);

            return View(model);
        }

        class DatoMeteoGiornaliero_Storico
        {
            public DateTime Data { get; set; }
            public string AdministrativeAreaLevel3 { get; set; }
            public int IdVigneto { get; set; }
            public int Precipitazione1 { get; set; }
            public int BagnaturaFogliareProbabilitaAlta { get; set; }
            public decimal Precipitazione { get; set; }
        }

    }
}

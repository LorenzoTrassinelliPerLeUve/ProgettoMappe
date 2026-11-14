using _4GrapesAPI.Models;
using _4GrapesAPI.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using Microsoft.AspNet.Identity.Owin;

using _4GrapesAPI.Properties;
using Microsoft.AspNet.Identity;
using Microsoft.Ajax.Utilities;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Threading;

namespace _4GrapesAPI.Controllers
{
    //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
    [AuthorizePortal(Abilitazione = Abilitazione.INDICATORI_AZIENDALI)]
    public class IndicatoriAziendaliController : _BaseController
    {        

        [HttpGet]
        public ActionResult Dashboard(DashboardIndicatoriAziendaliModel model)
        {
            var utente = User.GetUtente();            

            var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;

            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));

            var sql_filtri_vigneti = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.VignetoMaps.Area3 != null).AsQueryable();
            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = DB.VistaUtentiVigneti.Where(x => x.idUtente == utente.idUtente).Select(x => x.IdVigneto).ToList();
                sql_filtri_vigneti = sql_filtri_vigneti.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
            }
                          
            /*var filtro_tipologieLinea = tipologieLinea.Where(x => ids_tipologie_linee.Contains(x.Id)).ToList();
            var filtro_regioni = regioni.Where(x => ids_regioni.Contains(x.Id)).ToList();
            var filtro_comuni = comuni.Where(x => ids_comuni.Contains(x.Id)).ToList();
            var filtro_aziende = aziende.Where(x => ids_aziende.Contains(x.Id)).ToList();
            var filtro_rischi = rischi.Where(x => ids_rischi.Contains(x.Id)).ToList();
            var filtro_vitigni = vitigni.Where(x => ids_vitigni.Contains(x.Id)).ToList();*/            

            //var aziende = sql_vigneti.Where(x => x.Vigna.Ente_IdEnte > 0).Select(x => new { Id = x.Vigna.Ente.IdEnte, Nome = x.Vigna.Ente.RagioneSociale }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            //var comuni = sql_vigneti.Where(x => x.VignetoMaps.AdministrativeAreaLevel3 != null).Select(x => new { Id = x.VignetoMaps.Area3, Nome = x.VignetoMaps.AdministrativeAreaLevel3, IdProvincia = x.VignetoMaps.Area2, IdRegione = x.VignetoMaps.Area1 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            //var province = sql_vigneti.Where(x => x.VignetoMaps.AdministrativeAreaLevel2 != null).Select(x => new { Id = x.VignetoMaps.Area2, Nome = x.VignetoMaps.AdministrativeAreaLevel2, IdRegione = x.VignetoMaps.Area1 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            //var regioni = sql_vigneti.Where(x => x.VignetoMaps.AdministrativeAreaLevel1 != null).Select(x => new { Id = x.VignetoMaps.Area1, Nome = x.VignetoMaps.AdministrativeAreaLevel1, }).Distinct().ToList().OrderBy(x => x.Nome).ToList();

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                //regioni = regioni.Where(x => x.Id == model.IdRegione).ToList();
                //province = province.Where(x => x.IdRegione == model.IdRegione).ToList();
                //comuni = comuni.Where(x => x.IdRegione == model.IdRegione).ToList();
                sql_filtri_vigneti = sql_filtri_vigneti.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
            }

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                //province = province.Where(x => x.Id == model.IdProvincia).ToList();
                //comuni = comuni.Where(x => x.IdProvincia == model.IdProvincia).ToList();
                sql_filtri_vigneti = sql_filtri_vigneti.Where(x => x.VignetoMaps.Area2 == model.IdProvincia);
            }

            if (model.IdComune != null && model.IdComune != "-")
            {
                //comuni = comuni.Where(x => x.Id == model.IdComune).ToList();
                sql_filtri_vigneti = sql_filtri_vigneti.Where(x => x.VignetoMaps.Area3 == model.IdComune);
            }

            if (model.IdAzienda != 0)
            {
                //aziende = aziende.Where(x => x.Id == model.IdAzienda).ToList();
                sql_filtri_vigneti = sql_filtri_vigneti.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
            }

            var filtri_vigneti = sql_filtri_vigneti.Select(x => new
            {
                IdVigneto = x.IdVigneto,
                IdRegione = x.VignetoMaps.Area1,
                IdProvincia = x.VignetoMaps.Area2,
                IdComune = x.VignetoMaps.Area3,
                IdAzienda = x.Vigna.Ente_IdEnte,
            }).ToList();

            var ids_vigneti_da_filtri = filtri_vigneti.Select(x => x.IdVigneto).ToList();
            var ids_regioni_da_filtri = filtri_vigneti.Select(x => x.IdRegione).Distinct().ToList();
            var ids_province_da_filtri = filtri_vigneti.Select(x => x.IdProvincia).Distinct().ToList();
            var ids_comuni_da_filtri = filtri_vigneti.Select(x => x.IdComune).Distinct().ToList();
            var ids_aziende_da_filtri = filtri_vigneti.Select(x => x.IdAzienda).Distinct().ToList();            

            //var nomi_vigneti = DB.Vigneto.Where(x => ids_vigneti.Contains(x.IdVigneto)).Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto }).ToList();
            //var tipologieLinea = DB.TipologiaLinea.Where(x => ids_tipologie_linee.Contains(x.Id)).Select(x => new { Id = x.Id, Nome = x.Descrizione }).ToList();
            var regioni = DB.VignetoMaps.Where(x => ids_regioni_da_filtri.Contains(x.Area1) && x.AdministrativeAreaLevel1 != null).Select(x => new { Id = x.Area1, Nome = x.AdministrativeAreaLevel1 }).Distinct().OrderBy(x => x.Nome).ToList();
            var province = DB.VignetoMaps.Where(x => ids_province_da_filtri.Contains(x.Area2) && x.AdministrativeAreaLevel2 != null).Select(x => new { Id = x.Area2, Nome = x.AdministrativeAreaLevel2, IdRegione = x.Area1 }).Distinct().OrderBy(x => x.Nome).ToList();
            var comuni = DB.VignetoMaps.Where(x => ids_comuni_da_filtri.Contains(x.Area3) && x.AdministrativeAreaLevel3 != null).Select(x => new { Id = x.Area3, Nome = x.AdministrativeAreaLevel3, IdProvincia = x.Area2, IdRegione = x.Area1 }).Distinct().OrderBy(x => x.Nome).ToList();
            //var gruppi = DB.Gruppo.Where(x => ids_gruppi.Contains(x.IdGruppo) && x.IdGruppo > 0).Select(x => new { Id = x.IdGruppo, Nome = x.Descrizione }).ToList();
            var aziende = DB.Ente.Where(x => ids_aziende_da_filtri.Contains(x.IdEnte) && x.IdEnte > 0 && x.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA).Select(x => new { Id = x.IdEnte, Nome = x.RagioneSociale }).OrderBy(x => x.Nome).ToList();
            //var rischi = DB.Rischio.Where(x => ids_rischi.Contains(x.Id)).Select(x => new { Id = x.Id, Nome = x.DescrizioneRischio_PERONOSPORA + " / " + x.DescrizioneRischio_OIDIO }).ToList();
            // var vitigni = DB.Vitigno.Where(x => ids_vitigni.Contains(x.idVitigno) && x.idVitigno > 0).Select(x => new { Id = x.idVitigno, Nome = x.Vitigno1 }).ToList();

            /*var ids_regioni = regioni.Select(x => x.Id).ToList();
            var ids_province = province.Select(x => x.Id).ToList();
            var ids_comuni = comuni.Select(x => x.Id).ToList();
            var ids_aziende = aziende.Select(x => x.Id).ToList();*/

            var filtro_aziende = aziende.ToList();
            var filtro_comuni = comuni.ToList();
            var filtro_province = province.ToList();
            var filtro_regioni = regioni.ToList();

            filtro_regioni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_province.Insert(0, new { Id = "-", Nome = "-", IdRegione = "" });
            filtro_comuni.Insert(0, new { Id = "-", Nome = "-", IdProvincia = "", IdRegione = "" });
            filtro_aziende.Insert(0, new { Id = 0, Nome = "-" });
            
            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);            

            //--------------------------------------------------------
            //-------- TABELLE PROBABILITA
            //----------------------------------------------------------            

            var da_giorno = data.StartOfDay();
            var a_giorno = data.EndOfDay();            
           
            //--- RECUPERO DATI PROBABILITA AZIENDE          
            var dati_probabilita_aziende = DB.ModelloInfezione
                .Where(x => x.Data >= da_giorno && x.Data <= a_giorno && ids_vigneti_da_filtri.Contains(x.IdVigneto))
                .Select(x => new
                {                    
                    x.Vigneto.Vigna.Ente_IdEnte,
                    x.IdAvversitaDaTrattare,
                    x.PercInfezione,                    
                })                
                .GroupBy(x => new { x.Ente_IdEnte, x.IdAvversitaDaTrattare } )
                .Select(x => new
                {
                    IdAzienda = x.Key.Ente_IdEnte,
                    IdAvversita = x.Key.IdAvversitaDaTrattare,
                    MaxInfezione = x.Max(y => y.PercInfezione),                                     
                })
                .ToList();            

            //--- RECUPERO DATI PROBABILITA COMUNI         
           
            var dati_probabilita_comuni = DB.ModelloInfezione
                .Where(x => x.Data >= da_giorno && x.Data <= a_giorno && ids_vigneti_da_filtri.Contains(x.IdVigneto))
                .Select(x => new
                {
                    x.Vigneto.VignetoMaps.Area3,
                    x.IdAvversitaDaTrattare,
                    x.PercInfezione,
                })
                .GroupBy(x => new { x.Area3, x.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    IdComune = x.Key.Area3,
                    IdAvversita = x.Key.IdAvversitaDaTrattare,
                    MaxInfezione = x.Max(y => y.PercInfezione),
                })
                .ToList();

            //--- PRENDO SOLO I COMUNI PER CUI CI SONO DATI METEO
            //var comuni_dati_meteo = dati_meteo.Select(x => x.Area3).Distinct().ToList();
            //comuni = comuni.Where(x => comuni_dati_meteo.ContainsIgnoreCase(x.Id)).ToList();            

            foreach (var azienda in aziende)
            {
                var item_Peronospora = dati_probabilita_aziende.Where(x => x.IdAzienda == azienda.Id && x.IdAvversita == AvversitaDaTrattare.PERONOSPORA).FirstOrDefault();
                var item_Oidio = dati_probabilita_aziende.Where(x => x.IdAzienda == azienda.Id && x.IdAvversita == AvversitaDaTrattare.OIDIO).FirstOrDefault();

                if (item_Peronospora == null && item_Oidio == null)
                {
                    continue;
                }

                var riga = new RigaIndicatoreAziendale()
                {
                    Id = azienda.Id.ToString(),
                    Nome = azienda.Nome,

                    Peronospora = ModelloInfezione.GetTestoDaValore(item_Peronospora?.MaxInfezione),
                    Oidio = ModelloInfezione.GetTestoDaValore(item_Oidio?.MaxInfezione)
                };

                model.Righe_Probabilita_Aziende.Add(riga);
            }

            foreach (var comune in comuni)
            {
                var item_Peronospora = dati_probabilita_comuni.Where(x => x.IdComune == comune.Id && x.IdAvversita == AvversitaDaTrattare.PERONOSPORA).FirstOrDefault();
                var item_Oidio = dati_probabilita_comuni.Where(x => x.IdComune == comune.Id && x.IdAvversita == AvversitaDaTrattare.OIDIO).FirstOrDefault();

                if (item_Peronospora == null && item_Oidio == null)
                {
                    continue;
                }

                var riga = new RigaIndicatoreAziendale()
                {
                    Id = comune.Id,
                    Nome = comune.Nome,

                    Peronospora = ModelloInfezione.GetTestoDaValore(item_Peronospora?.MaxInfezione),
                    Oidio = ModelloInfezione.GetTestoDaValore(item_Oidio?.MaxInfezione)
                };

                model.Righe_Probabilita_Comuni.Add(riga);
            }

            //-----------------------------------------------------
            //--------- TABELLE PRESSIONE ---------------------
            //------------------------------------------------------

            var elenco_bbch = DB.BBCH.ToList();

            var anno = data.Year;
            var a_data = data.EndOfDay();
           
            var sql_vigneti = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.VignetoMaps.Area3 != null).AsQueryable();

            if (model.IdRegione != null && model.IdRegione != "-")
            {                
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
            }

            if (model.IdComune != null && model.IdComune != "-")
            {                
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area3 == model.IdComune);
            }                     

            var vigneti_T = sql_vigneti.Select(x => new
            {
                IdVigneto = x.IdVigneto,
                IdRegione = x.VignetoMaps.Area1,
                IdComune = x.VignetoMaps.Area3,
                IdVitigno = x.Vitigno_idVitigno,
                IdAzienda = x.Vigna.Ente_IdEnte,
                IdRischio = x.IdRischio,
                IdTipologiaLinea = x.IdTipologiaLinea,
            }).ToList();           

            var vigneti_R_PERONOSPORA = DB.Rilievo
                .Where(x => x.Vigneto_IdVigneto != null && x.TipoRilievo_idTipoRilievo == AvversitaDaTrattare.PERONOSPORA && x.PercDiffusione != null && x.PercIntensità != null && x.InizioRilievo.Year == anno && x.InizioRilievo <= a_data && (x.Organo_idOrgano == Organo.FOGLIA || x.Organo_idOrgano == Organo.GRAPPOLO))
                .GroupBy(x => (int)x.Vigneto_IdVigneto)
                .Select(x => new
                {
                    IdVigneto = x.Key,
                    ConInfezione = x.Count(y => y.PercDiffusione > 0m),
                    MaxDiffusione = x.Max(y => (decimal)y.PercDiffusione),
                    MaxIntensita = x.Max(y => (decimal)y.PercIntensità)
                }).ToList();

            var vigneti_R_OIDIO = DB.Rilievo
                .Where(x => x.Vigneto_IdVigneto != null && x.TipoRilievo_idTipoRilievo == AvversitaDaTrattare.OIDIO && x.PercDiffusione != null && x.PercIntensità != null && x.InizioRilievo.Year == anno && x.InizioRilievo <= a_data && (x.Organo_idOrgano == Organo.FOGLIA || x.Organo_idOrgano == Organo.GRAPPOLO))
                .GroupBy(x => (int)x.Vigneto_IdVigneto)
                .Select(x => new
                {
                    IdVigneto = x.Key,
                    ConInfezione = x.Count(y => y.PercDiffusione > 0m),
                    MaxDiffusione = x.Max(y => (decimal)y.PercDiffusione),
                    MaxIntensita = x.Max(y => (decimal)y.PercIntensità)
                }).ToList();

            var vigneti_F = DB.ModelloBBCH.Where(x => x.Data.Year == anno && x.Data < a_data)
               .GroupBy(x => x.IdVigneto)
               .Select(x => new
               {
                    IdVigneto = x.Key,                       
                   BBCH = x.Max(y => (int?)y.IdBBCH_UltimaSettimana),
               })
               .ToList()
               .Select(x => new
               {
                   x.IdVigneto,
                   x.BBCH,
                   Fenologia_PERONOSPORA = GetFenologia(elenco_bbch, x.BBCH, AvversitaDaTrattare.PERONOSPORA),
                   Fenologia_OIDIO = GetFenologia(elenco_bbch, x.BBCH, AvversitaDaTrattare.OIDIO)
               })
               .ToList();

            var vigneti_IntornoAMe = vigneti_T.Select(x => new
            {
                x.IdVigneto,
                x.IdComune,
                x.IdRegione,
                x.IdRischio,
                x.IdAzienda,
                x.IdTipologiaLinea,
                x.IdVitigno,
                Controllo_PERONOSPORA = vigneti_R_PERONOSPORA.Find(y => y.IdVigneto == x.IdVigneto),
                Controllo_OIDIO = vigneti_R_OIDIO.Find(y => y.IdVigneto == x.IdVigneto),               
                Fenologia_PERONOSPORA = vigneti_F.Find(y => y.IdVigneto == x.IdVigneto)?.Fenologia_PERONOSPORA,
                Fenologia_OIDIO = vigneti_F.Find(y => y.IdVigneto == x.IdVigneto)?.Fenologia_OIDIO
            }).ToList();

            var vigneti_MioMonitoraggio = vigneti_IntornoAMe.ToList();

            if (model.IdAzienda != 0)
            {
                //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.Vigna.Ente_IdEnte == model.IdAzienda);
                vigneti_MioMonitoraggio = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == model.IdAzienda).ToList();
            }



            //--------------------- COMUNI PERONOSPORA
            var righe_comuni_PERONOSPORA = new List<RigaComune>();
            foreach (var comune in comuni)
            {
                var riga_comune = new RigaComune() { Id = comune.Id, Nome = comune.Nome };
                
                var controlli = vigneti_IntornoAMe.Where(x => x.IdComune == comune.Id && x.Controllo_PERONOSPORA != null).Select(x => x.Controllo_PERONOSPORA).ToList();
                    
                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    riga_comune.IntornoAMe = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_INTORNO_A_ME;
                }
               
                righe_comuni_PERONOSPORA.Add(riga_comune);               
            }

            //----------------------- COMUNI OIDIO
            var righe_comuni_OIDIO = new List<RigaComune>();
            foreach (var comune in comuni)
            {
                var riga_comune = new RigaComune() { Id = comune.Id, Nome = comune.Nome };

                var controlli = vigneti_IntornoAMe.Where(x => x.IdComune == comune.Id && x.Controllo_OIDIO != null).Select(x => x.Controllo_OIDIO).ToList();

                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    riga_comune.IntornoAMe = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_INTORNO_A_ME;
                }

                righe_comuni_OIDIO.Add(riga_comune);
            }

            //------ AZIENDE PERONOSPORA
            var righe_aziende_PERONOSPORA = new List<RigaAzienda>();
            foreach (var azienda in aziende)
            {
                var riga_azienda = new RigaAzienda() { Id = azienda.Id, Nome = azienda.Nome };
                
                var controlli = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Controllo_PERONOSPORA != null).Select(x => x.Controllo_PERONOSPORA).ToList();

                var num_vigneti_controllati_totali = controlli.Count;
                var num_vigneti_controllati_con_infezione = controlli.Where(x => x.ConInfezione > 0).Count();

                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    riga_azienda.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_MIO_MONITORAGGIO;
                }                
                //riga_azienda.IdsVignetiControllati = vigneti_controllati.Select(x => x.IdVigneto).ToList();

                var ids_comuni_controllati = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Controllo_PERONOSPORA != null).Select(x => x.IdComune).Distinct().ToList();

                riga_azienda.IntornoAMe = righe_comuni_PERONOSPORA.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);

                riga_azienda.Fenologia = (decimal?)vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Fenologia_PERONOSPORA != null).Average(x => x.Fenologia_PERONOSPORA) * TrattamentiController.PESO_BBCH;

                righe_aziende_PERONOSPORA.Add(riga_azienda);
            }

            //------ AZIENDE OIDIO
            var righe_aziende_OIDIO = new List<RigaAzienda>();
            foreach (var azienda in aziende)
            {
                var riga_azienda = new RigaAzienda() { Id = azienda.Id, Nome = azienda.Nome };

                var controlli = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Controllo_OIDIO != null).Select(x => x.Controllo_OIDIO).ToList();

                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    riga_azienda.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_MIO_MONITORAGGIO;
                }
                //riga_azienda.IdsVignetiControllati = vigneti_controllati.Select(x => x.IdVigneto).ToList();

                var ids_comuni_controllati = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Controllo_OIDIO != null).Select(x => x.IdComune).Distinct().ToList();

                riga_azienda.IntornoAMe = righe_comuni_OIDIO.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);

                riga_azienda.Fenologia = (decimal?)vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Fenologia_OIDIO != null).Average(x => x.Fenologia_OIDIO) * TrattamentiController.PESO_BBCH;

                righe_aziende_OIDIO.Add(riga_azienda);
            }

            //--- PRESSIONI AZIENDE
            foreach (var azienda in aziende)
            {
                var item_PERONOSPORA = righe_aziende_PERONOSPORA.Where(x => x.Id == azienda.Id).FirstOrDefault();
                var item_OIDIO = righe_aziende_OIDIO.Where(x => x.Id == azienda.Id).FirstOrDefault();

                if (item_PERONOSPORA == null && item_OIDIO == null)
                {
                    continue;
                }

                var riga = new RigaIndicatoreAziendale()
                {
                    Id = azienda.Id.ToString(),
                    Nome = azienda.Nome,

                    //Peronospora = GetTestoDaValore(item_PERONOSPORA?.Pressione),
                    //Oidio = GetTestoDaValore(item_OIDIO?.Pressione)
                    Peronospora = item_PERONOSPORA?.Pressione?.ToString("0.##"),
                    Oidio = item_OIDIO?.Pressione?.ToString("0.##")
                };

                model.Righe_Pressione.Add(riga);
            }

            //-----------------------------------------------------
            //--------- GRAFICI PROTEZIONE --- ---------------------
            //------------------------------------------------------

            var da_giorno_protezione = data.StartOfDay();
            var a_giorno_protezione = data.AddDays(7).EndOfDay();
           
            //--- RECUPERO DATI PROBABILITA AZIENDE            
            var modelli_protezione = DB.ModelloProtezione
                .Where(x => x.Data >= da_giorno_protezione && x.Data <= a_giorno_protezione && ids_vigneti_da_filtri.Contains(x.TrattamentoAvversita.Trattamento.IdVigneto))
                .Select(x => new
                {                               
                    x.TrattamentoAvversita.IdAvversitaDaTrattare,
                    Giorno = DbFunctions.TruncateTime(x.Data),
                    x.TrattamentoAvversita.Trattamento.IdVigneto,
                    x.PercProtezione,
                })
                .GroupBy(x => new { x.IdAvversitaDaTrattare, x.Giorno, x.IdVigneto })
                .Select(x => new
                {                    
                    x.Key.IdAvversitaDaTrattare,
                    x.Key.Giorno,
                    x.Key.IdVigneto,
                    PercProtezione = x.Average(y => y.PercProtezione),
                })
                .ToList();            


            var serie_PERONOSPORA = new ApexCharts.Serie() { name = "PERONOSPORA" };
            var serie_OIDIO = new ApexCharts.Serie() { name = "OIDIO" };

            for (var giorno = da_giorno_protezione.Date; giorno <= a_giorno_protezione; giorno = giorno.AddDays(1) )
            {               
                var modelli_protezione_giorno = modelli_protezione.Where(x => x.Giorno == giorno).ToList();

                if (ids_vigneti_da_filtri.Count > 0)
                {
                    var modelli_protezione_PERONOSPORA = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA).ToList();
                    var protezione_PERONOSPORA = modelli_protezione_PERONOSPORA.Sum(x => (decimal?)x.PercProtezione) / ids_vigneti_da_filtri.Count;
                    serie_PERONOSPORA.data.Add((int?)protezione_PERONOSPORA ?? 0);
                    //serie_PERONOSPORA.data.Add(new ApexCharts.Point() { x = giorno.ToString("dd/MM"), y = (int?)protezione_PERONOSPORA ?? 0 });

                    var modelli_protezione_OIDIO = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO).ToList();
                    var protezione_OIDIO = modelli_protezione_OIDIO.Sum(x => (decimal?)x.PercProtezione) / ids_vigneti_da_filtri.Count;
                    serie_OIDIO.data.Add((int?)protezione_OIDIO ?? 0);
                    //serie_OIDIO.data.Add(new ApexCharts.Point() { x = giorno.ToString("dd/MM"), y = (int?)protezione_OIDIO ?? 0 });
                }

                model.GraficoProtezione.Labels_X.Add(giorno.ToString("dd/MM"));
            }

            model.GraficoProtezione.Series.Add(serie_PERONOSPORA);
            model.GraficoProtezione.Series.Add(serie_OIDIO);


            return View(model);
        }                     

        /*private decimal? GetPressione(Ente ente, AvversitaDaTrattare avversita, DateTime giorno, int moda_BBCH, List<BBCH> elenco_bbch)
        {
            //--- OGNI TANTO GENERA UN ERRORE SQL: Transaction (Process ID 53) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction. 
            //--- RIPROVO FINO A 5 VOLTE
            Exception last_exception = null;
            for (int tentativo = 1; tentativo <= 5; tentativo++)
            {
                try
                {
                    //var a_data = giorno; //var a_data = giorno.EndOfDay();

                    decimal mio_monitorggio = 0;
                    decimal intorno_a_me = 0;

                    var sql_rilievi = DB.Rilievo.Where(x => x.InizioRilievo.Year == giorno.Year && x.InizioRilievo <= giorno
                                    && x.TipoRilievo_idTipoRilievo == avversita.Id
                                    && x.Vigneto.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA
                                    && (x.Organo_idOrgano == Organo.FOGLIA || x.Organo_idOrgano == Organo.GRAPPOLO));

                    //---- MIO MONITORAGGIO
                    {
                        var rilievi_vigneto = sql_rilievi.Where(x => x.Vigneto_IdVigneto == vigneto.IdVigneto).Select(x => new { x.Vigneto_IdVigneto, x.PercDiffusione, x.PercIntensità }).ToList();

                        if (rilievi_vigneto.Count > 0)
                        {
                            var num_rilievi_con_infezione = rilievi_vigneto.Where(x => x.PercDiffusione > 0).Count();
                            var perc_presenza_avversita = num_rilievi_con_infezione > 0 ? 100 : 0;

                            var massimi = rilievi_vigneto.GroupBy(x => x.Vigneto_IdVigneto).Select(x => new { IdVigneto = (int)x.Key, MaxDiffusione = x.Max(y => y.PercDiffusione ?? 0), MaxIntensita = x.Max(y => y.PercIntensità ?? 0) }).ToList();

                            var media_max_diffusione = massimi.Average(x => x.MaxDiffusione);
                            var media_max_intensita = massimi.Average(x => x.MaxIntensita);

                            mio_monitorggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_MIO_MONITORAGGIO;
                        }
                    }

                    {
                        //---- INTORNO A ME                
                        var rilievi_comune = sql_rilievi.Where(x => x.Vigneto.VignetoMaps.Area3 == vigneto.VignetoMaps.Area3).Select(x => new { x.Vigneto_IdVigneto, x.PercDiffusione, x.PercIntensità }).ToList();

                        if (rilievi_comune.Count > 0)
                        {
                            var num_vigneti_totale = rilievi_comune.Select(x => x.Vigneto_IdVigneto).Distinct().Count();
                            var num_vigneti_con_infezione = rilievi_comune.Where(x => x.PercDiffusione > 0).Select(x => x.Vigneto_IdVigneto).Distinct().Count();
                            var perc_presenza_avversita = (num_vigneti_con_infezione / (decimal)num_vigneti_totale * 100);

                            var massimi = rilievi_comune.GroupBy(x => x.Vigneto_IdVigneto).Select(x => new { IdVigneto = (int)x.Key, MaxDiffusione = x.Max(y => y.PercDiffusione ?? 0), MaxIntensita = x.Max(y => y.PercIntensità ?? 0) }).ToList();

                            var media_max_diffusione = massimi.Average(x => x.MaxDiffusione);
                            var media_max_intensita = massimi.Average(x => x.MaxIntensita);

                            intorno_a_me = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_INTORNO_A_ME;
                        }
                    }


                    var fenologia = GetFenologia(elenco_bbch, moda_BBCH, avversita.Id) * TrattamentiController.PESO_BBCH;

                    if (fenologia == null)
                    {
                        return null;
                    }

                    var valore = Math.Round(mio_monitorggio + intorno_a_me + (decimal)fenologia, 2);

                    return valore;
                }
                catch (Exception ex)
                {
                    last_exception = ex;
                    Thread.Sleep(1000);
                    Log.WriteWarning("Errore in GetPressione: " + ex.ToString());
                }
            }

            throw last_exception;
        }*/

        private int? GetFenologia(List<BBCH> elenco_bbch, int? moda_BBCH, int idAvversita)
        {
            if (moda_BBCH == null) return null;

            var BBCH = elenco_bbch.Find(x => x.IdBBCH == moda_BBCH);

            if (BBCH != null)
            {
                if (idAvversita == (int)TipoRilievo.Enum.PERONOSPORA)
                {
                    return BBCH.Pressione_PERONOSPORA;
                }
                else if (idAvversita == (int)TipoRilievo.Enum.OIDIO)
                {
                    return BBCH.Pressione_OIDIO;
                }
                else if (idAvversita == (int)TipoRilievo.Enum.MARCIUME_NERO)
                {
                    return BBCH.Pressione_BLACK_ROOT;
                }
                else if (idAvversita == (int)TipoRilievo.Enum.BOTRITE)
                {
                    return BBCH.Pressione_BOTRITE;
                }
            }

            return null;
        }
    }
}
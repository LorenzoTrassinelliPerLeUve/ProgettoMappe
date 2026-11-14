using _4GrapesAPI.Entity;
using _4GrapesAPI.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using OfficeOpenXml;
using _4GrapesAPI.Properties;
using _4Grapes.Traduzioni;

namespace _4GrapesAPI.Controllers
{
    public partial class TrattamentiController : _BaseController
    {
        List<int> IdsAvversitaInLista = new List<int> { (int)TipoRilievo.Enum.PERONOSPORA, (int)TipoRilievo.Enum.OIDIO /*, (int)TipoRilievo.Enum.MARCIUME_NERO, (int)TipoRilievo.Enum.BOTRITE*/ };
        public const decimal PESO_MIO_MONITORAGGIO = 0.5m;
        public const decimal PESO_INTORNO_A_ME = 0.3m;
        public const decimal PESO_BBCH = 0.2m;

        //------------------------------------- PRESSIONE ---------------------------------------------
        [HttpGet]
        public ActionResult Pressione(PressioneModel model)
        {
            var utente = User.GetUtente();
            var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;

            //---- FILTRI DA / IN SESSIONE
            model.IdAvversita = FiltroDaSessione(model.IdAvversita, nameof(model.IdAvversita));
            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));
            model.IdVitigno = FiltroDaSessione(model.IdVitigno, nameof(model.IdVitigno));
            model.IdRischio = FiltroDaSessione(model.IdRischio, nameof(model.IdRischio));

            //--- ASSEGNO 0 E NON NULL PERCHE' SELECT2 ALTRIMENTI NON MI VISUALIZZA IL VALORE VUOTO
            //--------- FILTRI ----------
            var avversita = DB.TipoRilievo.Where(x => IdsAvversitaInLista.Contains(x.idTipoRilievo)).Select(x => new { Id = x.idTipoRilievo, Nome = x.TipoRilevo }).ToList();

            var elenco_bbch = DB.BBCH.ToList();

            //--------- TABELLE ----------
            if (model.IdAvversita != 0)
            {                

                var anno = data.Year;
                var a_data = data.EndOfDay();

                //var sql_rilievi = DB.Rilievo.Where(x => x.Vigneto.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.InizioRilievo.Year == data.Year && x.InizioRilievo <= a_data);                

                //var sql_rilievi_aziende = sql_rilievi.AsQueryable();
                //var sql_rilievi_comuni = sql_rilievi.AsQueryable();

                //--------- VIGNETI T ----------------------
                var sql_vigneti_T = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.VignetoMaps.Area3 != null).AsQueryable();
                var sql_vigneti_R = DB.Rilievo.AsQueryable();
                var sql_vigneti_F = DB.ModelloBBCH.AsQueryable();

                //if (!utente.IsMasterUser)
                //{
                //    var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                //    sql_vigneti_T = sql_vigneti_T.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
                //    sql_vigneti_R = sql_vigneti_R.Where(x => ids_vigneti_utente.Contains((int)x.Vigneto_IdVigneto));
                //    sql_vigneti_F = sql_vigneti_F.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
                //}

                if (model.IdRegione != null && model.IdRegione != "-")
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.VignetoMaps.Area1 == model.IdRegione);
                    //sql_rilievi_comuni = sql_rilievi_comuni.Where(x => x.Vigneto.VignetoMaps.Area1 == model.IdRegione);
                    sql_vigneti_T = sql_vigneti_T.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
                }

                if (model.IdComune != null && model.IdComune != "-")
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.VignetoMaps.Area3 == model.IdComune);
                    //sql_rilievi_comuni = sql_rilievi_comuni.Where(x => x.Vigneto.VignetoMaps.Area3 == model.IdComune);
                    sql_vigneti_T = sql_vigneti_T.Where(x => x.VignetoMaps.Area3 == model.IdComune);
                }              

                if (model.IdTipologiaLinea != 0)
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.IdTipologiaLinea == model.IdTipologiaLinea);
                    sql_vigneti_T = sql_vigneti_T.Where(x => x.IdTipologiaLinea == model.IdTipologiaLinea);
                }

                if (model.IdVitigno != 0)
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.Vitigno_idVitigno == model.IdVitigno);
                    sql_vigneti_T = sql_vigneti_T.Where(x => x.Vitigno_idVitigno == model.IdVitigno);
                }

                if (model.IdRischio != 0)
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.IdRischio == model.IdRischio);
                    sql_vigneti_T = sql_vigneti_T.Where(x => x.IdRischio == model.IdRischio);
                }

                /*if (model.IdAzienda != 0)
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.Vigna.Ente_IdEnte == model.IdAzienda);
                    sql_vigneti = sql_vigneti.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
                };*/
               
                var vigneti_T = sql_vigneti_T.Select(x => new
                {
                    IdVigneto = x.IdVigneto,
                    IdRegione = x.VignetoMaps.Area1,
                    IdComune = x.VignetoMaps.Area3,
                    IdVitigno = x.Vitigno_idVitigno,
                    IdAzienda = x.Vigna.Ente_IdEnte,
                    IdRischio = x.IdRischio,
                    IdTipologiaLinea = x.IdTipologiaLinea,
                }).ToList();

                //--------- VIGNETI R ----------------------                               
                //.Where(x => x.Vigneto_IdVigneto != null && x.TipoRilievo_idTipoRilievo == model.IdAvversita && x.PercDiffusione != null  && x.PercIntensità != null && x.InizioRilievo.Year == anno && x.InizioRilievo <= a_data)
                var vigneti_R = sql_vigneti_R.Where(x => x.Vigneto_IdVigneto != null && x.TipoRilievo_idTipoRilievo == model.IdAvversita && x.PercDiffusione != null && x.PercIntensità != null && x.InizioRilievo.Year == anno && x.InizioRilievo <= a_data && (x.Organo_idOrgano == Organo.FOGLIA || x.Organo_idOrgano == Organo.GRAPPOLO))
                    .GroupBy(x => (int)x.Vigneto_IdVigneto)
                    .Select(x => new
                    {
                        IdVigneto = x.Key,
                        ConInfezione = x.Count(y => y.PercDiffusione > 0m),
                        MaxDiffusione = x.Max(y => (decimal)y.PercDiffusione),
                        MaxIntensita = x.Max(y => (decimal)y.PercIntensità)
                    }).ToList();

                //--------- VIGNETI F ----------------------                
                var vigneti_F = sql_vigneti_F.Where(x => x.Data.Year == anno && x.Data < a_data)
                   .GroupBy(x => x.IdVigneto)
                   .Select(x => new
                   {
                       IdVigneto = x.Key,
                       //IdAzienda = x.Key.Ente_IdEnte,
                       //IdComune = x.Key.Area3,
                       //IdVitigno = x.Key.Vitigno_idVitigno,
                       BBCH = x.Max(y => (int?)y.IdBBCH_UltimaSettimana),
                       //RecuperatoDa = x.Where(y => y.IdVigneto == x.Key.IdVigneto && y.IdBBCH_UltimaSettimana == x.Max(z => z.IdBBCH_UltimaSettimana)).OrderByDescending(y => y.Data).Select(y => y.IdBBCH_RecuperatoDa).FirstOrDefault()
                   })
                   .ToList()
                   .Select(x => new
                   {
                       x.IdVigneto,
                       x.BBCH,
                       Fenologia = GetFenologia(elenco_bbch, x.BBCH, model.IdAvversita)
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
                    Controllato = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto) != null,
                    ConInfezione = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto)?.ConInfezione > 0,
                    MaxDiffusione = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto)?.MaxDiffusione ?? 0,
                    MaxIntensita = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto)?.MaxIntensita ?? 0,
                    Fenologia = vigneti_F.Find(y => y.IdVigneto == x.IdVigneto)?.Fenologia
                }).ToList();

                var vigneti_MioMonitoraggio = vigneti_IntornoAMe.ToList();

                if (model.IdAzienda != 0)
                {
                    //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.Vigna.Ente_IdEnte == model.IdAzienda);
                    vigneti_MioMonitoraggio = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == model.IdAzienda).ToList();
                }

                if (model.IdGruppo != 0)
                {
                    var id_enti_da_filtro = DB.VistaGruppo_Azienda.Where(x => x.IdGruppo == model.IdGruppo).Select(x => x.IdEnte).ToList();
                    vigneti_MioMonitoraggio = vigneti_MioMonitoraggio.Where(x => id_enti_da_filtro.Contains(x.IdAzienda)).ToList();
                }

                if (!utente.IsMasterUser)
                {
                    var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                    vigneti_MioMonitoraggio = vigneti_MioMonitoraggio.Where(x => ids_vigneti_utente.Contains(x.IdVigneto)).ToList();
                }

                var ids_vigneti = vigneti_MioMonitoraggio.Select(x => x.IdVigneto).ToList();
                var ids_tipologie_linee = vigneti_MioMonitoraggio.Select(x => x.IdTipologiaLinea).Distinct().ToList();
                var ids_regioni = vigneti_MioMonitoraggio.Select(x => x.IdRegione).Distinct().ToList();
                var ids_comuni = vigneti_MioMonitoraggio.Select(x => x.IdComune).Distinct().ToList();
                //var ids_gruppi = vigneti.Select(x => x.IdTipologiaLinea).Distinct().ToList();
                var ids_aziende = vigneti_MioMonitoraggio.Select(x => x.IdAzienda).Distinct().ToList();
                var ids_rischi = vigneti_MioMonitoraggio.Select(x => x.IdRischio).Distinct().ToList();
                var ids_vitigni = vigneti_MioMonitoraggio.Select(x => x.IdVitigno).Distinct().ToList();                

                //var ids_gruppi = DB.Componente.Where(x => ids_aziende.Contains(x.Ente_IdEnte)).Select(x => x.Gruppo_IdGruppo).Distinct().ToList();

                if (ids_aziende.Count == 1)
                {
                    model.IdAzienda = ids_aziende[0];
                }

                /*var filtro_tipologieLinea = tipologieLinea.Where(x => ids_tipologie_linee.Contains(x.Id)).ToList();
                var filtro_regioni = regioni.Where(x => ids_regioni.Contains(x.Id)).ToList();
                var filtro_comuni = comuni.Where(x => ids_comuni.Contains(x.Id)).ToList();
                var filtro_aziende = aziende.Where(x => ids_aziende.Contains(x.Id)).ToList();
                var filtro_rischi = rischi.Where(x => ids_rischi.Contains(x.Id)).ToList();
                var filtro_vitigni = vitigni.Where(x => ids_vitigni.Contains(x.Id)).ToList();*/

                var nomi_vigneti = DB.Vigneto.Where(x => ids_vigneti.Contains(x.IdVigneto)).Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto }).ToList();
                var tipologieLinea = DB.TipologiaLinea.Where(x => ids_tipologie_linee.Contains(x.Id)).Select(x => new { Id = x.Id, Nome = x.Descrizione }).ToList();
                var regioni = DB.VignetoMaps.Where(x => ids_regioni.Contains(x.Area1) && x.AdministrativeAreaLevel1 != null).Select(x => new { Id = x.Area1, Nome = x.AdministrativeAreaLevel1 }).Distinct().ToList();
                var comuni = DB.VignetoMaps.Where(x => ids_comuni.Contains(x.Area3) && x.AdministrativeAreaLevel3 != null).Select(x => new { Id = x.Area3, Nome = x.AdministrativeAreaLevel3, IdProvincia = x.Area2, IdRegione = x.Area1 }).Distinct().ToList();
                var gruppi = DB.VistaGruppo_Azienda.Where(x => ids_aziende.Contains(x.IdEnte) && x.IdGruppo > 0).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().OrderBy(x => x.Nome).ToList();
                var aziende = DB.Ente.Where(x => ids_aziende.Contains(x.IdEnte) && x.IdEnte > 0 && x.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA).Select(x => new { Id = x.IdEnte, Nome = x.RagioneSociale }).ToList();
                var rischi = DB.Rischio.Where(x => ids_rischi.Contains(x.Id)).Select(x => new { Id = x.Id, Nome = x.DescrizioneRischio_PERONOSPORA + " / " + x.DescrizioneRischio_OIDIO }).ToList();
                var vitigni = DB.Vitigno.Where(x => ids_vitigni.Contains(x.idVitigno) && x.idVitigno > 0).Select(x => new { Id = x.idVitigno, Nome = x.NomeVitigno }).ToList();
                //var vigneti_F = DB.Vigneto.Where(x => x.IdVigneto > 0).Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto, IdVitigno = x.Vitigno_idVitigno }).ToList();            

                //------------------ TABELLA COMUNI ----------------------------------------
                foreach (var comune in comuni)
                {
                    var riga_comune = new RigaComune() { Id = comune.Id, Nome = comune.Nome };

                    //var ids_vigneti_comune = vigneti.Where(x => x.IdComune == comune.Id).Select(x => x.IdVigneto).ToList();
                    var vigneti_controllati = vigneti_IntornoAMe.Where(x => x.IdComune == comune.Id && x.Controllato).ToList();

                    var num_vigneti_controllati_totali = vigneti_controllati.Count;
                    var num_vigneti_controllati_con_infezione = vigneti_controllati.Where(x => x.ConInfezione).Count();

                    if (num_vigneti_controllati_totali > 0)
                    {
                        var perc_presenza_avversita = (num_vigneti_controllati_con_infezione / (decimal)num_vigneti_controllati_totali * 100);

                        var media_max_diffusione = vigneti_controllati.Average(x => x.MaxDiffusione);
                        var media_max_intensita = vigneti_controllati.Average(x => x.MaxIntensita);

                        riga_comune.IntornoAMe = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * PESO_INTORNO_A_ME;
                    }
                   
                    riga_comune.Fenologia = (decimal?)vigneti_IntornoAMe.Where(x => x.IdComune == comune.Id && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;

                    model.RigheComune.Add(riga_comune);                  
                }

                //------------------ TABELLA REGIONI ----------------------------------------
                foreach (var regione in regioni)
                {
                    var riga_regione = new RigaRegione() { Id = regione.Id, Nome = regione.Nome };

                    var vigneti_controllati = vigneti_IntornoAMe.Where(x => x.IdRegione == regione.Id && x.Controllato).ToList();
                    
                    var num_vigneti_controllati_totali = vigneti_controllati.Count;
                    var num_vigneti_controllati_con_infezione = vigneti_controllati.Where(x => x.ConInfezione).Count();

                    if (num_vigneti_controllati_totali > 0)
                    {
                        var perc_presenza_avversita = (num_vigneti_controllati_con_infezione / (decimal)num_vigneti_controllati_totali * 100);

                        var media_max_diffusione = vigneti_controllati.Average(x => x.MaxDiffusione);
                        var media_max_intensita = vigneti_controllati.Average(x => x.MaxIntensita);

                        riga_regione.IntornoAMe = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * PESO_INTORNO_A_ME;
                    }

                    riga_regione.Fenologia = (decimal?)vigneti_IntornoAMe.Where(x => x.IdRegione == regione.Id && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;

                    model.RigheRegione.Add(riga_regione);
                }

                //------------- TABELLA AZIENDE ------------------------------
                foreach (var azienda in aziende)
                {
                    var riga_azienda = new RigaAzienda() { Id = azienda.Id, Nome = azienda.Nome };
                   
                    var vigneti_controllati = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Controllato).ToList();
                    
                    var num_vigneti_controllati_totali = vigneti_controllati.Count;
                    var num_vigneti_controllati_con_infezione = vigneti_controllati.Where(x => x.ConInfezione).Count();

                    if (num_vigneti_controllati_totali > 0)
                    {
                        var perc_presenza_avversita = (num_vigneti_controllati_con_infezione / (decimal)num_vigneti_controllati_totali * 100);
                        
                        var media_max_diffusione = vigneti_controllati.Average(x => x.MaxDiffusione);
                        var media_max_intensita = vigneti_controllati.Average(x => x.MaxIntensita);

                        riga_azienda.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * PESO_MIO_MONITORAGGIO;
                    }

                    var ids_comuni_controllati = vigneti_controllati.Select(x => x.IdComune).Distinct().ToList();
                    riga_azienda.IntornoAMe = model.RigheComune.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);
                    //var ids_comuni_azienda = vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id).Select(x => x.IdComune).Distinct().ToList();
                    //riga_azienda.IntornoAMe = model.RigheComune.Where(x => ids_comuni_azienda.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);

                    riga_azienda.Fenologia = (decimal?)vigneti_MioMonitoraggio.Where(x => x.IdAzienda == azienda.Id && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;                    

                    model.RigheAzienda.Add(riga_azienda);
                }
             
                //--------- RILIEVI PER RISCHIO
                if (model.IdAzienda != 0)
                {
                    //var azienda = aziende.Find(x => x.Id == model.IdAzienda);
                    //var rilievi_azienda = rilievi_aziende.Where(x => x.Ente_IdEnte == azienda.Id).ToList();
                    //var rilievi_azienda_BBCH = rilievi_aziende_BBCH.Where(x => x.Ente_IdEnte == azienda.Id).ToList();

                    foreach (var rischio in rischi)
                    {
                        var riga_rischio = new RigaRischio() { Id = rischio.Id, Nome = rischio.Nome };

                        //var ids_vigneti_rischio = vigneti.Where(x => x.IdRischio == rischio.Id).Select(x => x.IdVigneto).ToList();
                        var vigneti_controllati = vigneti_MioMonitoraggio.Where(x => x.IdRischio == rischio.Id && x.Controllato).ToList();
                        //var rilievi_rischio_BBCH = rilievi_azienda_BBCH.Where(x => x.IdRischio == rischio.Id).ToList();

                        var num_vigneti_controllati_totali = vigneti_controllati.Count;
                        var num_vigneti_controllati_con_infezione = vigneti_controllati.Where(x => x.ConInfezione).Count();

                        if (num_vigneti_controllati_totali > 0)
                        {
                            var perc_presenza_avversita = (num_vigneti_controllati_con_infezione / (decimal)num_vigneti_controllati_totali * 100);
                            var media_max_diffusione = vigneti_controllati.Average(x => x.MaxDiffusione);
                            var media_max_intensita = vigneti_controllati.Average(x => x.MaxIntensita);

                            riga_rischio.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * PESO_MIO_MONITORAGGIO;
                        }

                        var ids_comuni_controllati = vigneti_controllati.Select(x => x.IdComune).Distinct().ToList();
                        riga_rischio.IntornoAMe = model.RigheComune.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);
                        //var ids_comuni_controllati = vigneti_controllati.Select(x => x.IdComune).Distinct().ToList();
                        //riga_rischio.IntornoAMe = model.RigheComune.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);



                        riga_rischio.Fenologia = (decimal?)vigneti_MioMonitoraggio.Where(x => x.IdRischio == rischio.Id && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;


                        model.RigheRischio.Add(riga_rischio);
                    }
                    /*model.RigheRischio.Add(new RigaRischio()
                    {
                        Id = rischio.Id,
                        Nome = rischio.Nome,
                        MioMonitoraggio = mio_monitorggio,
                        IdsVignetiControllati = id_vigneti_controllati,
                        IntornoAMe = intorno_a_me,
                        //Fenologia = fenologia,
                    });*/

                    //--------- RILIEVI PER VITIGNO                
                    //var azienda = aziende.Find(x => x.Id == model.IdAzienda);
                    //var rilievi_azienda = rilievi_aziende.Where(x => x.Ente_IdEnte == azienda.Id).ToList();
                    //var rilievi_azienda_BBCH = rilievi_aziende_BBCH.Where(x => x.Ente_IdEnte == azienda.Id).ToList();

                    foreach (var vitigno in vitigni)
                    {
                        var riga_vitigno = new RigaVitigno() { Id = vitigno.Id, Nome = vitigno.Nome };

                        //var ids_vigneti_vitigno = vigneti.Where(x => x.IdVitigno == vitigno.Id).Select(x => x.IdVigneto).ToList();
                        var vigneti_controllati = vigneti_MioMonitoraggio.Where(x => x.IdVitigno == vitigno.Id && x.Controllato).ToList();
                        //var rilievi_vitigno_BBCH = rilievi_azienda_BBCH.Where(x => x.Vitigno_idVitigno == vitigno.Id).ToList();

                        var num_vigneti_controllati_totali = vigneti_controllati.Count;
                        var num_vigneti_controllati_con_infezione = vigneti_controllati.Where(x => x.ConInfezione).Count();

                        if (num_vigneti_controllati_totali > 0)
                        {
                            var perc_presenza_avversita = (num_vigneti_controllati_con_infezione / (decimal)num_vigneti_controllati_totali * 100);
                            var media_max_diffusione = vigneti_controllati.Average(x => x.MaxDiffusione);
                            var media_max_intensita = vigneti_controllati.Average(x => x.MaxIntensita);

                            riga_vitigno.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * PESO_MIO_MONITORAGGIO;
                        }
                        
                        var ids_comuni_controllati = vigneti_controllati.Select(x => x.IdComune).Distinct().ToList();
                        riga_vitigno.IntornoAMe = model.RigheComune.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null).Average(x => (decimal?)x.IntornoAMe);

                        riga_vitigno.Fenologia = (decimal?)vigneti_MioMonitoraggio.Where(x => x.IdVitigno == x.IdVitigno && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;

                        model.RigheVitigno.Add(riga_vitigno);                        
                    }
                    //}

                    //--------- RILIEVI PER VIGNETO                
                    //var azienda = aziende.Find(x => x.Id == model.IdAzienda);
                    // var rilievi_azienda = rilievi_aziende.Where(x => x.Ente_IdEnte == azienda.Id).ToList();
                    //var rilievi_azienda_BBCH = rilievi_aziende_BBCH.Where(x => x.Ente_IdEnte == azienda.Id).ToList();

                    foreach (var vigneto in vigneti_MioMonitoraggio)
                    {
                        var riga_vigneto = new RigaVigneto() { Id = vigneto.IdVigneto, Nome = nomi_vigneti.FirstOrDefault(x => x.Id == vigneto.IdVigneto)?.Nome };

                        //var rilievi_vigneto = rilievi_azienda.Where(x => x.Vigneto_IdVigneto == vigneto.Id).ToList();
                        //var rilievi_vigneto_BBCH = rilievi_azienda_BBCH.Where(x => x.Vigneto_IdVigneto == vigneto.Id).ToList();

                        if (vigneto.Controllato)
                        {
                            var perc_presenza_avversita = vigneto.ConInfezione ? 100 : 0;

                            riga_vigneto.MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * vigneto.MaxDiffusione * vigneto.MaxIntensita) * PESO_MIO_MONITORAGGIO;
                        }

                        var ids_comuni_controllati = vigneto.Controllato ? new List<string> { vigneto.IdComune } : new List<string>();
                        var righe_comuni_controllati = model.RigheComune.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Id) && x.IntornoAMe != null);

                        riga_vigneto.IntornoAMe = righe_comuni_controllati.Average(x => (decimal?)x.IntornoAMe) ?? 0;                        

                        riga_vigneto.Fenologia = (decimal?)vigneti_IntornoAMe.Where(x => x.IdVigneto == vigneto.IdVigneto && x.Fenologia != null).Average(x => x.Fenologia) * PESO_BBCH;

                        model.RigheVigneto.Add(riga_vigneto);

                        /*model.RigheVigneto.Add(new RigaVigneto()
                        {
                            Id = vigneto.Id,
                            Nome = vigneto.Nome,
                            MioMonitoraggio = mio_monitorggio,
                            IntornoAMe = intorno_a_me,
                            Fenologia = fenologia,
                            //FenologiaDaComuni = BBCH_vigneto?.IdBBCH_RecuperatoDa == "COMUNE" || BBCH_vigneto?.IdBBCH_RecuperatoDa == "PROVINCIA" || BBCH_vigneto?.IdBBCH_RecuperatoDa == "REGIONE"
                        });*/
                    }
                }


                var filtro_tipologieLinea = tipologieLinea.ToList();
                var filtro_regioni = regioni.ToList();
                var filtro_comuni = comuni.ToList();
                var filtro_aziende = aziende.ToList();
                var filtro_rischi = rischi.ToList();
                var filtro_vitigni = vitigni.ToList();

                var filtro_gruppi = gruppi.ToList();

                filtro_tipologieLinea.Insert(0, new { Id = 0, Nome = "-" });
                filtro_regioni.Insert(0, new { Id = "-", Nome = "-" });
                filtro_comuni.Insert(0, new { Id = "-", Nome = "-", IdProvincia = "", IdRegione = "" });
                filtro_gruppi.Insert(0, new { Id = 0, Nome = "-" });
                filtro_aziende.Insert(0, new { Id = 0, Nome = "-" });
                filtro_rischi.Insert(0, new { Id = 0, Nome = "-" });
                filtro_vitigni.Insert(0, new { Id = 0, Nome = "-" });

                model.TipologieLinea = new SelectList(filtro_tipologieLinea, "Id", "Nome", model.IdTipologiaLinea);
                model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
                model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
                model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);
                model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
                model.Rischi = new SelectList(filtro_rischi, "Id", "Nome", model.IdRischio);
                model.Vitigni = new SelectList(filtro_vitigni, "Id", "Nome", model.IdVitigno);
            }


            var filtro_avversita = avversita.ToList();

            filtro_avversita.Insert(0, new { Id = 0, Nome = "-" });


            model.Avversita = new SelectList(filtro_avversita, "Id", "Nome", model.IdAvversita);
            //Utils.RadiceCubica();
           
            return View(model);
        }    
        
        [HttpGet]
        public ActionResult ExportPressione(PressioneModel model)
        {
            Pressione(model);

            var memoryStream = new System.IO.MemoryStream();

            using (var excel = new ExcelPackage())
            {
                //----- TABELLA COMUNI
                {
                    ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.comuni);
                    writer.Cells[1, 1].Value = Tr.comune;
                    writer.Cells[1, 2].Value = Tr.intorno_a_me;
                    writer.Cells[1, 3].Value = Tr.fenologia;

                    for (int i = 0; i < model.RigheComune.Count; i++)
                    {
                        var riga = model.RigheComune[i];
                        writer.Cells[i + 2, 1].Value = riga.Nome;
                        writer.Cells[i + 2, 2].Value = riga.IntornoAMe;
                        writer.Cells[i + 2, 3].Value = riga.Fenologia;
                    }
                }

                //----- TABELLA REGIONI
                {
                    ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.regioni);
                    writer.Cells[1, 1].Value = Tr.regione;
                    writer.Cells[1, 2].Value = Tr.intorno_a_me;
                    writer.Cells[1, 3].Value = Tr.fenologia;

                    for (int i = 0; i < model.RigheRegione.Count; i++)
                    {
                        var riga = model.RigheRegione[i];
                        writer.Cells[i + 2, 1].Value = riga.Nome;
                        writer.Cells[i + 2, 2].Value = riga.IntornoAMe;
                        writer.Cells[i + 2, 3].Value = riga.Fenologia;
                    }
                }

                //----- TABELLA AZIENDE
                {
                    ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.aziende);
                    writer.Cells[1, 1].Value = Tr.azienda;
                    writer.Cells[1, 2].Value = Tr.mio_monitoraggio;
                    writer.Cells[1, 3].Value = Tr.intorno_a_me;
                    writer.Cells[1, 4].Value = Tr.fenologia;
                    writer.Cells[1, 5].Value = Tr.pressione;

                    for (int i = 0; i < model.RigheAzienda.Count; i++)
                    {
                        var riga = model.RigheAzienda[i];
                        writer.Cells[i + 2, 1].Value = riga.Nome;
                        writer.Cells[i + 2, 2].Value = riga.MioMonitoraggio;
                        writer.Cells[i + 2, 3].Value = riga.IntornoAMe;
                        writer.Cells[i + 2, 4].Value = riga.Fenologia;
                        writer.Cells[i + 2, 5].Value = riga.Pressione;
                    }
                }

                if (model.IdAzienda != 0)
                {
                    //----- TABELLA RISCHI
                    {
                        ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.rischi);
                        writer.Cells[1, 1].Value = Tr.rischio;
                        writer.Cells[1, 2].Value = Tr.mio_monitoraggio;
                        writer.Cells[1, 3].Value = Tr.intorno_a_me;
                        writer.Cells[1, 4].Value = Tr.fenologia;
                        writer.Cells[1, 5].Value = Tr.pressione;

                        for (int i = 0; i < model.RigheRischio.Count; i++)
                        {
                            var riga = model.RigheRischio[i];
                            writer.Cells[i + 2, 1].Value = riga.Nome;
                            writer.Cells[i + 2, 2].Value = riga.MioMonitoraggio;
                            writer.Cells[i + 2, 3].Value = riga.IntornoAMe;
                            writer.Cells[i + 2, 4].Value = riga.Fenologia;
                            writer.Cells[i + 2, 5].Value = riga.Pressione;
                        }
                    }

                    //----- TABELLA VITIGNI
                    {
                        ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.vitigni);
                        writer.Cells[1, 1].Value = Tr.vitigno;
                        writer.Cells[1, 2].Value = Tr.mio_monitoraggio;
                        writer.Cells[1, 3].Value = Tr.intorno_a_me;
                        writer.Cells[1, 4].Value = Tr.fenologia;
                        writer.Cells[1, 5].Value = Tr.pressione;

                        for (int i = 0; i < model.RigheVitigno.Count; i++)
                        {
                            var riga = model.RigheVitigno[i];
                            writer.Cells[i + 2, 1].Value = riga.Nome;
                            writer.Cells[i + 2, 2].Value = riga.MioMonitoraggio;
                            writer.Cells[i + 2, 3].Value = riga.IntornoAMe;
                            writer.Cells[i + 2, 4].Value = riga.Fenologia;
                            writer.Cells[i + 2, 5].Value = riga.Pressione;
                        }
                    }

                    //----- TABELLA VIGNETI
                    {
                        ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.vigneti);
                        writer.Cells[1, 1].Value = Tr.vigneto;
                        writer.Cells[1, 2].Value = Tr.mio_monitoraggio;
                        writer.Cells[1, 3].Value = Tr.intorno_a_me;
                        writer.Cells[1, 4].Value = Tr.fenologia;
                        writer.Cells[1, 5].Value = Tr.pressione;

                        for (int i = 0; i < model.RigheVigneto.Count; i++)
                        {
                            var riga = model.RigheVigneto[i];
                            writer.Cells[i + 2, 1].Value = riga.Nome;
                            writer.Cells[i + 2, 2].Value = riga.MioMonitoraggio;
                            writer.Cells[i + 2, 3].Value = riga.IntornoAMe;
                            writer.Cells[i + 2, 4].Value = riga.Fenologia;
                            writer.Cells[i + 2, 5].Value = riga.Pressione;
                        }
                    }
                }

                excel.SaveAs(memoryStream);
            }

            memoryStream.Position = 0;

            return File(memoryStream, Const.ExcelMime, $"Export {Tr.pressione}.xlsx");
        }


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

        ///#############################################################################################################
        /// <summary>
        /// PROBABILITA'
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Probabilita(ProbabilitaModel model)
        {
            var utente = User.GetUtente();
            var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;

            //---- FILTRI DA / IN SESSIONE
            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));

            if (data.Year < DateTime.Today.Year)
            {
                return Probabilita_Storico(model);
            }

            var sql_vigneti = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.VignetoMaps.Area3 != null).AsQueryable();

            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                sql_vigneti = sql_vigneti.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
            }

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
            }

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area2 == model.IdProvincia);
            }

            if (model.IdComune != null && model.IdComune != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area3 == model.IdComune);
            }

            if (model.IdAzienda != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
            }

            if (model.IdGruppo != 0)
            {
                var id_enti_da_filtro = DB.VistaGruppo_Azienda.Where(x => x.IdGruppo == model.IdGruppo).Select(x => x.IdEnte).ToList();
                sql_vigneti = sql_vigneti.Where(x => id_enti_da_filtro.Contains(x.Vigna.Ente_IdEnte));
            }

            var vigneti = sql_vigneti.Select(x => new
            {
                IdRegione = x.VignetoMaps.Area1,
                IdProvincia = x.VignetoMaps.Area2,
                IdComune = x.VignetoMaps.Area3,
                IdAzienda = x.Vigna.Ente_IdEnte,
            }).Distinct().ToList();

            var ids_regioni = vigneti.Select(x => x.IdRegione).Distinct().ToList();
            var ids_comuni = vigneti.Select(x => x.IdComune).Distinct().ToList();
            var ids_province = vigneti.Select(x => x.IdProvincia).Distinct().ToList();
            var ids_aziende = vigneti.Select(x => x.IdAzienda).Distinct().ToList();

            //if (model.IdRegione != null && model.IdRegione != "-")
            //{                
            //    sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
            //}

            //if (model.IdComune != null && model.IdComune != "-")
            //{                
            //    sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area3 == model.IdComune);
            //}            

            /*if (model.IdAzienda != 0)
            {
                //sql_rilievi_aziende = sql_rilievi_aziende.Where(x => x.Vigneto.Vigna.Ente_IdEnte == model.IdAzienda);
                sql_vigneti = sql_vigneti.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
            };*/

            //var vigneti_T = sql_vigneti.Select(x => new
            //{
            //    IdVigneto = x.IdVigneto,
            //    IdRegione = x.VignetoMaps.Area1,
            //    IdComune = x.VignetoMaps.Area3,
            //    IdVitigno = x.Vitigno_idVitigno,
            //    IdAzienda = x.Vigna.Ente_IdEnte,
            //    IdRischio = x.IdRischio,
            //    IdTipologiaLinea = x.IdTipologiaLinea,
            //}).ToList();

            //-----------------


            var regioni = DB.VignetoMaps.Where(x => ids_regioni.Contains(x.Area1) && x.AdministrativeAreaLevel1 != null).OrderBy(x => x.AdministrativeAreaLevel1).Select(x => new { Id = x.Area1, Nome = x.AdministrativeAreaLevel1 }).Distinct().ToList();
            var filtro_regioni = regioni.ToList();

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                regioni = regioni.Where(x => x.Id == model.IdRegione).ToList();
            }

            //ids_regioni = regioni.Select(x => x.Id).ToList();

            var province = DB.VignetoMaps.Where(x => ids_province.Contains(x.Area2) && x.AdministrativeAreaLevel2 != null && ids_regioni.Contains(x.Area1)).OrderBy(x => x.AdministrativeAreaLevel1).ThenBy(x => x.AdministrativeAreaLevel2).Select(x => new { Id = x.Area2, Nome = x.AdministrativeAreaLevel2, IdRegione = x.Area1 }).Distinct().ToList();
            var filtro_province = province.ToList();

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                province = province.Where(x => x.Id == model.IdProvincia).ToList();
            }

            //var ids_province = province.Select(x => x.Id).ToList();

            var comuni = DB.VignetoMaps.Where(x => ids_comuni.Contains(x.Area3) && x.AdministrativeAreaLevel3 != null && ids_province.Contains(x.Area2)).OrderBy(x => x.AdministrativeAreaLevel1).ThenBy(x => x.AdministrativeAreaLevel2).ThenBy(x => x.AdministrativeAreaLevel3).Select(x => new { Id = x.Area3, Nome = x.AdministrativeAreaLevel3, IdProvincia = x.Area2, IdRegione = x.Area1 }).Distinct().ToList();
            var filtro_comuni = comuni.ToList();

            if (model.IdComune != null && model.IdComune != "-")
            {
                comuni = comuni.Where(x => x.Id == model.IdComune).ToList();
            }

            //var ids_comuni = comuni.Select(x => x.Id).ToList();

            var ids_enti_in_comuni = DB.VignetoMaps.Where(x => ids_comuni.Contains(x.Area3)).Select(x => x.Vigneto.Vigna.Ente_IdEnte).Distinct().ToList();

            var aziende = DB.Ente.Where(x => ids_aziende.Contains(x.IdEnte) && x.IdEnte > 0 && x.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && ids_enti_in_comuni.Contains(x.IdEnte)).Select(x => new { Id = x.IdEnte, Nome = x.RagioneSociale }).ToList();
            var filtro_aziende = aziende.ToList();

            if (model.IdAzienda != 0)
            {
                aziende = aziende.Where(x => x.Id == model.IdAzienda).ToList();
            }

            var id_enti_per_filtro = filtro_aziende.Select(x => x.Id).ToList();
            var filtro_gruppi = DB.VistaGruppo_Azienda.Where(x => id_enti_per_filtro.Contains(x.IdEnte)).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().ToList().OrderBy(x => x.Nome).ToList();

            //var oggi = DateTime.Today;
            var dati_meteo_da_giorno = data.StartOfDay();
            var dati_meteo_a_giorno = data.AddDays(7).EndOfDay();

            model.DaGiorno = dati_meteo_da_giorno;
            model.AGiorno = dati_meteo_a_giorno;


            //--- RECUPERO TUTTI I DATI METEO
            //var ids_aziende = aziende.Select(x => x.Id).ToList();
            var dati_meteo = DB.MeteoGiornaliero.Where(x => x.Data >= dati_meteo_da_giorno && x.Data <= dati_meteo_a_giorno && ids_aziende.Contains(x.Vigneto.Vigna.Ente_IdEnte)).Select(x => new
            {
                x.Data,
                x.Vigneto.VignetoMaps.Area3,
                x.IdVigneto,
                x.Precipitazione1,
                x.BagnaturaFogliareProbabilitaAlta,
                x.Precipitazione
            }).ToList();

            //--- PRENDO SOLO I COMUNI PER CUI CI SONO DATI METEO
            var comuni_dati_meteo = dati_meteo.Select(x => x.Area3).Distinct().ToList();
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
                    var dati_meteo_cella = dati_meteo.Where(x => x.Data >= da_giorno && x.Data <= a_giorno && x.Area3.EqualsIgnoreCase(comune.Id)).ToList();

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
            filtro_gruppi.Insert(0, new { Id = 0, Nome = "-" });

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
            model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);

            return View(model);
        }

        [HttpGet]
        public ActionResult ExportProbabilita(ProbabilitaModel model)
        {
            Probabilita(model);

            var memoryStream = new System.IO.MemoryStream();

            using (var excel = new ExcelPackage())
            {
                //----- TABELLA PERONOSPORA
                {
                    ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.peronospora.ToUpper());

                    var cella_intestazione = writer.Cells[1, 1, 1, 6];
                    cella_intestazione.Merge = true;
                    cella_intestazione.Value = Tr.messaggio_probabilita_infezioni_x_dal_al.FormatWith(Tr.peronospora, model.DaGiorno.ToString("dd/MM/yyyy"), model.AGiorno.ToString("dd/MM/yyyy"));

                    writer.Cells[2, 1].Value = Tr.comune;
                    writer.Cells[2, 2].Value = "0 - 2";
                    writer.Cells[2, 3].Value = "1 - 3";
                    writer.Cells[2, 4].Value = "2 - 4";
                    writer.Cells[2, 5].Value = "3 - 5";
                    writer.Cells[2, 6].Value = "4 - 6";

                    for (int i = 0; i < model.Righe_PERONOSPORA.Count; i++)
                    {
                        var riga = model.Righe_PERONOSPORA[i];
                        writer.Cells[i + 3, 1].Value = riga.Nome;
                        writer.Cells[i + 3, 2].Value = riga.Valori[0];
                        writer.Cells[i + 3, 3].Value = riga.Valori[1];
                        writer.Cells[i + 3, 4].Value = riga.Valori[2];
                        writer.Cells[i + 3, 5].Value = riga.Valori[3];
                        writer.Cells[i + 3, 6].Value = riga.Valori[4];
                    }
                }

                //----- TABELLA OIDIO
                {
                    ExcelWorksheet writer = excel.Workbook.Worksheets.Add(Tr.oidio.ToUpper());

                    var cella_intestazione = writer.Cells[1, 1, 1, 6];
                    cella_intestazione.Merge = true;
                    cella_intestazione.Value = Tr.messaggio_probabilita_infezioni_x_dal_al.FormatWith(Tr.oidio, model.DaGiorno.ToString("dd/MM/yyyy"), model.AGiorno.ToString("dd/MM/yyyy"));

                    writer.Cells[2, 1].Value = Tr.comune;
                    writer.Cells[2, 2].Value = "0 - 2";
                    writer.Cells[2, 3].Value = "1 - 3";
                    writer.Cells[2, 4].Value = "2 - 4";
                    writer.Cells[2, 5].Value = "3 - 5";
                    writer.Cells[2, 6].Value = "4 - 6";

                    for (int i = 0; i < model.Righe_OIDIO.Count; i++)
                    {
                        var riga = model.Righe_OIDIO[i];
                        writer.Cells[i + 3, 1].Value = riga.Nome;
                        writer.Cells[i + 3, 2].Value = riga.Valori[0];
                        writer.Cells[i + 3, 3].Value = riga.Valori[1];
                        writer.Cells[i + 3, 4].Value = riga.Valori[2];
                        writer.Cells[i + 3, 5].Value = riga.Valori[3];
                        writer.Cells[i + 3, 6].Value = riga.Valori[4];
                    }
                }

                excel.SaveAs(memoryStream);
            }

            memoryStream.Position = 0;

            return File(memoryStream, Const.ExcelMime, "Export Probabilita.xlsx");
        }

        private decimal PercentualeMillimetriMPioggia_PERONOSPORA(decimal mm)
        {
            if (mm <= 0) return 0;
            if (mm <= 1) return Utils.MapValue(mm, 0m, 1m, 0m, 0.5m);
            if (mm <= 2) return Utils.MapValue(mm, 1m, 2m, 0.5m, 1m);
            if (mm <= 3) return Utils.MapValue(mm, 2m, 3m, 1m, 1.5m);
            if (mm <= 4) return Utils.MapValue(mm, 3m, 4m, 1.5m, 2m);
            if (mm <= 5) return Utils.MapValue(mm, 4m, 5m, 2m, 2.5m);
            if (mm <= 6) return Utils.MapValue(mm, 5m, 6m, 2.5m, 3m);
            if (mm <= 7) return Utils.MapValue(mm, 6m, 7m, 3m, 3.5m);
            if (mm <= 8) return Utils.MapValue(mm, 7m, 8m, 3.5m, 4m);
            if (mm <= 9) return Utils.MapValue(mm, 8m, 9m, 4m, 4.5m);
            if (mm <= 10) return Utils.MapValue(mm, 9m, 10m, 4.5m, 5m);
            if (mm <= 20) return Utils.MapValue(mm, 10m, 20m, 5m, 25m);
            if (mm <= 30) return Utils.MapValue(mm, 20m, 30m, 25m, 45m);
            if (mm <= 40) return Utils.MapValue(mm, 30m, 40m, 45m, 65m);
            if (mm <= 50) return Utils.MapValue(mm, 40m, 50m, 65m, 80m);
            if (mm <= 60) return Utils.MapValue(mm, 50m, 60m, 80m, 90m);
            if (mm <= 70) return Utils.MapValue(mm, 60m, 70m, 90m, 95m);
            if (mm <= 80) return Utils.MapValue(mm, 70m, 80m, 95m, 96m);
            if (mm <= 90) return Utils.MapValue(mm, 80m, 90m, 96m, 97m);
            if (mm <= 100) return Utils.MapValue(mm, 90m, 100m, 97m, 98m);
            if (mm <= 110) return Utils.MapValue(mm, 100m, 110m, 98m, 99m);
            if (mm <= 120) return Utils.MapValue(mm, 110m, 120m, 99m, 100m);
            return 100;
        }

        ///#############################################################################################################
        /// <summary>
        /// PROTEZIONE
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpGet]
        [AuthorizePortal(IsMasterUser = false)]
        public ActionResult Protezione(ProtezioneModel model)
        {
            var utente = User.GetUtente();
            var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;

            //---- FILTRI DA / IN SESSIONE
            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));
            model.IdGruppo = FiltroDaSessione(model.IdGruppo, nameof(model.IdGruppo));
            model.IdVigna = FiltroDaSessione(model.IdVigna, nameof(model.IdVigna));
            model.IdVigneto = FiltroDaSessione(model.IdVigneto, nameof(model.IdVigneto));
            model.IdVitigno = FiltroDaSessione(model.IdVitigno, nameof(model.IdVitigno));
            model.IdRischio = FiltroDaSessione(model.IdRischio, nameof(model.IdRischio));

            //var oggi = DateTime.Today;
            var da_giorno = data.StartOfDay();
            var a_giorno = data.AddDays(6).EndOfDay();

            model.DaGiorno = da_giorno;
            model.AGiorno = a_giorno;

            var sql_vigneti_da_filtri = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && x.VignetoMaps.Area3 != null).AsQueryable();

            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
            }

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.VignetoMaps.Area1 == model.IdRegione);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
            }

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.VignetoMaps.Area2 == model.IdProvincia);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.VignetoMaps.Area2 == model.IdProvincia);
            }

            if (model.IdComune != null && model.IdComune != "-")
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.VignetoMaps.Area3 == model.IdComune);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.VignetoMaps.Area3 == model.IdComune);
            }

            if (model.IdAzienda != 0)
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente_IdEnte == model.IdAzienda);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
            }

            if (model.IdGruppo != 0)
            {
                var id_enti_da_filtro = DB.VistaGruppo_Azienda.Where(x => x.IdGruppo == model.IdGruppo).Select(x => x.IdEnte).ToList();
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => id_enti_da_filtro.Contains(x.Vigna.Ente_IdEnte));
            }

            if (model.IdVigna != 0)
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.Vigna_IdVigna == model.IdVigna);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.Vigna_IdVigna == model.IdVigna);
            }

            if (model.IdVigneto != 0)
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.IdVigneto == model.IdVigneto);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.IdVigneto == model.IdVigneto);
            }

            if (model.IdVitigno != 0)
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.Vitigno_idVitigno == model.IdVitigno);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.Vitigno_idVitigno == model.IdVitigno);
            }

            if (model.IdRischio != 0)
            {
                //sql_modelli_protezione_settimanale = sql_modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.IdRischio == model.IdRischio);
                sql_vigneti_da_filtri = sql_vigneti_da_filtri.Where(x => x.IdRischio == model.IdRischio);
            }

            var vigneti_da_filtri = sql_vigneti_da_filtri.Select(x => new
            {
                IdVigneto = x.IdVigneto,
                IdRegione = x.VignetoMaps.Area1,
                IdProvincia = x.VignetoMaps.Area2,
                IdComune = x.VignetoMaps.Area3,
                IdAzienda = x.Vigna.Ente_IdEnte,
                IdVigna = x.Vigna_IdVigna,
                IdVitigno = x.Vitigno_idVitigno,
                IdRischio = x.IdRischio
            }).ToList();

            var ids_vigneti_da_filtri = vigneti_da_filtri.Select(x => x.IdVigneto).ToList();            

            var sql_modelli_protezione_settimanale = DB.ModelloProtezione
                .Where(x => x.Data >= da_giorno && x.Data <= a_giorno && ids_vigneti_da_filtri.Contains(x.TrattamentoAvversita.Trattamento.IdVigneto) && x.TrattamentoAvversita.Trattamento.Vigneto.IdTipologiaLinea != null && x.TrattamentoAvversita.Trattamento.Vigneto.IdRischio != null && x.TrattamentoAvversita.Trattamento.Suggerito)
                .GroupBy(x => new { Giorno = DbFunctions.TruncateTime(x.Data), x.TrattamentoAvversita.Trattamento.IdVigneto, x.TrattamentoAvversita.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    x.Key.Giorno,
                    x.Key.IdVigneto,
                    x.Key.IdAvversitaDaTrattare,
                    PercEfficaciaProdotto = x.Average(y => y.PercEfficaciaProdotto),
                    PercDilavamento = x.Average(y => y.PercDilavamento),
                    PercProtezione = x.Average(y => y.PercProtezione),
                    SommaPrecipitazioni = x.Average(y => y.SommaPrecipitazioni)
                });

            /*var sql_modelli_protezione_settimanale = DB.ModelloProtezione
                .Where(x => x.Data >= da_giorno && x.Data <= a_giorno && ids_vigneti_da_filtri.Contains(x.TrattamentoAvversita.Trattamento.IdVigneto) && x.TrattamentoAvversita.Trattamento.Vigneto.IdTipologiaLinea != null && x.TrattamentoAvversita.Trattamento.Vigneto.IdRischio != null && x.TrattamentoAvversita.Trattamento.Suggerito)
                .Select(x => new
                {
                    Giorno = DbFunctions.TruncateTime(x.Data),
                    x.TrattamentoAvversita.Trattamento.IdVigneto,
                    x.TrattamentoAvversita.IdAvversitaDaTrattare,
                    x.PercEfficaciaProdotto,
                    x.PercDilavamento,
                    x.PercProtezione,
                    x.SommaPrecipitazioni
                });*/

            var modelli_protezione_settimanale = sql_modelli_protezione_settimanale.ToList();

            //--- I FILTRI HANNO SOLO I DATI DEI VIGNETI DI CUI HO ALMENO UNA PROTEZIONE
            //--- SI E' DECISO DI FAR VEDERE TUTTI I VIGNETI IN BASE AI FILTRI E NON PIU SOLO QUELLI PRESI DAL MODELLO PROTEZIONE
            //var ids_vigneti_da_modelli = modelli_protezione_settimanale.Select(x => x.IdVigneto).Distinct().ToList();

            //var vigneti_da_modelli = vigneti_da_filtri.Where(x => ids_vigneti_da_modelli.Contains(x.IdVigneto)).ToList();
            var vigneti_da_modelli = vigneti_da_filtri.ToList();

            var ids_vigneti_da_modelli = vigneti_da_modelli.Select(x => x.IdVigneto).Distinct().ToList();
            var ids_regioni_da_modelli = vigneti_da_modelli.Select(x => x.IdRegione).Distinct().ToList();
            var ids_province_da_modelli = vigneti_da_modelli.Select(x => x.IdProvincia).Distinct().ToList();
            var ids_comuni_da_modelli = vigneti_da_modelli.Select(x => x.IdComune).Distinct().ToList();
            var ids_aziende_da_modelli = vigneti_da_modelli.Select(x => x.IdAzienda).Distinct().ToList();
            var ids_vigne_da_modelli = vigneti_da_modelli.Select(x => x.IdVigna).Distinct().ToList();
            var ids_vitigni_da_modelli = vigneti_da_modelli.Select(x => x.IdVitigno).Distinct().ToList();
            var ids_rischi_da_modelli = vigneti_da_modelli.Select(x => x.IdRischio).Distinct().ToList();            

            var filtro_regioni = DB.VignetoMaps.Where(x => ids_regioni_da_modelli.Contains(x.Area1) && x.AdministrativeAreaLevel1 != null).Select(x => new { Id = x.Area1, Nome = x.AdministrativeAreaLevel1 }).Distinct().ToList();
            var filtro_province = DB.VignetoMaps.Where(x => ids_province_da_modelli.Contains(x.Area2) && x.AdministrativeAreaLevel2 != null).Select(x => new { Id = x.Area2, Nome = x.AdministrativeAreaLevel2, }).Distinct().ToList();
            var filtro_comuni = DB.VignetoMaps.Where(x => ids_comuni_da_modelli.Contains(x.Area3) && x.AdministrativeAreaLevel3 != null).Select(x => new { Id = x.Area3, Nome = x.AdministrativeAreaLevel3, }).Distinct().ToList();
            var filtro_aziende = DB.Ente.Where(x => ids_aziende_da_modelli.Contains(x.IdEnte)).Select(x => new { Id = x.IdEnte, Nome = x.RagioneSociale }).ToList();
            var filtro_vigne = DB.Vigna.Where(x => ids_vigne_da_modelli.Contains(x.IdVigna)).Select(x => new { Id = x.IdVigna, Nome = x.NomeVigna }).ToList();
            var filtro_vigneti = DB.Vigneto.Where(x => ids_vigneti_da_modelli.Contains(x.IdVigneto)).Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto }).ToList();
            var filtro_vitigni = DB.Vitigno.Where(x => ids_vitigni_da_modelli.Contains(x.idVitigno) && x.idVitigno > 0).Select(x => new { Id = x.idVitigno, Nome = x.NomeVitigno }).ToList();
            var filtro_rischi = DB.Rischio.Where(x => ids_rischi_da_modelli.Contains(x.Id)).Select(x => new { Id = x.Id, Nome = x.DescrizioneRischio_PERONOSPORA + " / " + x.DescrizioneRischio_OIDIO }).ToList();
           
            var filtro_gruppi = DB.VistaGruppo_Azienda.Where(x => ids_aziende_da_modelli.Contains(x.IdEnte)).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().ToList().OrderBy(x => x.Nome).ToList();



            //--------------- CARICO TABELLE AZIENDE
            /*var aziende = modelli_protezione_settimanale
                .Select(x => new { 
                    Id = x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente.IdEnte, 
                    Nome = x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente.RagioneSociale,                    
                })
                .Distinct().ToList();*/

            //var ids_aziende_da_modelli = modelli_protezione_settimanale.Select(x => x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente_IdEnte).Distinct();

            /*var aziende_sql = DB.Ente.Where(x => ids_aziende_da_modelli.Contains(x.IdEnte)).Select(x => new
            {
                Id = x.IdEnte,
                Nome = x.RagioneSociale,
                NumVigneti = x.Vigna.SelectMany(y => y.Vigneto).Where(y => y.Carico && !y.Estirpo).Count(),
            }).ToString();*/
            //var AAAA = DB.Ente.Where(x => ids_aziende_da_modelli.Contains(x.IdEnte)).Select(x => new
            //{
            //    Id = x.IdEnte,
            //    Nome = x.RagioneSociale,
            //    NumVigneti = x.Vigna.SelectMany(y => y.Vigneto).Where(y => y.Carico && !y.Estirpo && ids_vigneti_da_filtri.Contains(y.IdVigneto)).Count(),
            //}).ToString();


            var aziende = DB.Ente.Where(x => ids_aziende_da_modelli.Contains(x.IdEnte)).Select(x => new
            {
                Id = x.IdEnte,
                Nome = x.RagioneSociale,
                NumVigneti = x.Vigna.SelectMany(y => y.Vigneto).Where(y => y.Carico && !y.Estirpo && ids_vigneti_da_filtri.Contains(y.IdVigneto)).Count(),
            }).ToList();

            foreach (var azienda in aziende)
            {
                var ids_vigneti_azienda = vigneti_da_filtri.Where(x => x.IdAzienda == azienda.Id).Select(x => x.IdVigneto).ToList();
                var modelli_protezione_azienda = modelli_protezione_settimanale.Where(x => ids_vigneti_azienda.Contains(x.IdVigneto)).ToList();

                //if (modelli_protezione_azienda.Count == 0 || azienda.NumVigneti == 0)
                if (azienda.NumVigneti == 0)
                    continue;

                if (modelli_protezione_azienda.Count == 0 && model.IdAzienda == 0 && utente.IsMasterUser)
                    continue;


                var riga_OIDIO = new RigaProtezione()
                {
                    Id = azienda.Id,
                    Nome = azienda.Nome,
                };

                var riga_PERONOSPORA = new RigaProtezione()
                {
                    Id = azienda.Id,
                    Nome = azienda.Nome,
                };

                for (int i = 0; i <= 6; i++)
                {
                    var giorno = data.AddDays(i);
                    
                    var modelli_protezione_giorno = modelli_protezione_azienda.Where(x => x.Giorno == giorno).ToList();                   

                    //------ PERONOSPORA
                    {
                        var modelli_PERONOSPORA = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA).ToList();

                        if (modelli_PERONOSPORA.Count > 0 && azienda.NumVigneti > 0)
                        {
                            var media_efficacia_prodotto = modelli_PERONOSPORA.Sum(x => x.PercEfficaciaProdotto) / azienda.NumVigneti;
                            var media_dilavamento = modelli_PERONOSPORA.Sum(x => x.PercDilavamento) / azienda.NumVigneti;
                            var media_protezione = modelli_PERONOSPORA.Sum(x => x.PercProtezione) / azienda.NumVigneti;
                            var media_precipitazioni = modelli_PERONOSPORA.Sum(x => x.SommaPrecipitazioni) / azienda.NumVigneti;

                            riga_PERONOSPORA.Valori[i] = media_protezione;
                            //riga_PERONOSPORA.Valori[i] = random.Next(0, 100);
                            riga_PERONOSPORA.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }                        
                    }

                    //------ OIDIO
                    {
                        var modelli_OIDIO = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO).ToList();

                        if (modelli_OIDIO.Count > 0 && azienda.NumVigneti > 0)
                        {
                            var media_efficacia_prodotto = modelli_OIDIO.Sum(x => x.PercEfficaciaProdotto) / azienda.NumVigneti;
                            var media_dilavamento = modelli_OIDIO.Sum(x => x.PercDilavamento) / azienda.NumVigneti;
                            var media_protezione = modelli_OIDIO.Sum(x => x.PercProtezione) / azienda.NumVigneti;
                            var media_precipitazioni = modelli_OIDIO.Sum(x => x.SommaPrecipitazioni) / azienda.NumVigneti;

                            riga_OIDIO.Valori[i] = media_protezione;
                            //riga_OIDIO.Valori[i] = random.Next(0, 100);
                            riga_OIDIO.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }
                    }
                }

                model.Righe_Aziende_OIDIO.Add(riga_OIDIO);
                model.Righe_Aziende_PERONOSPORA.Add(riga_PERONOSPORA);
            }

            //--------------- CARICO TABELLE VIGNETI
            var vigneti = DB.Vigneto.Where(x => ids_vigneti_da_modelli.Contains(x.IdVigneto)).Select(x => new
            {
                Id = x.IdVigneto,
                Nome = x.NomeVigneto,                
            }).ToList();

            //modelli_protezione_settimanale.Select(x => new { Id = x.Trattamento.Vigneto.IdVigneto, Nome = x.TrattamentoAvversita.Trattamento.Vigneto.NomeVigneto }).Distinct().ToList();

            foreach (var vigneto in vigneti)
            {
                var modelli_protezione_vigneto = modelli_protezione_settimanale.Where(x => x.IdVigneto == vigneto.Id).ToList();

                if (modelli_protezione_vigneto.Count == 0 && model.IdAzienda == 0 && utente.IsMasterUser)
                    continue;

                var riga_OIDIO = new RigaProtezione()
                {
                    Id = vigneto.Id,
                    Nome = vigneto.Nome,
                };

                var riga_PERONOSPORA = new RigaProtezione()
                {
                    Id = vigneto.Id,
                    Nome = vigneto.Nome,
                };

                for (int i = 0; i <= 6; i++)
                {
                    var giorno = data.AddDays(i);

                    var modelli_protezione_giorno = modelli_protezione_vigneto.Where(x => x.Giorno == giorno).ToList();

                    //------ PERONOSPORA
                    {
                        var modello_PERONOSPORA = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA).FirstOrDefault();

                        if (modello_PERONOSPORA != null)
                        {
                            var media_efficacia_prodotto = modello_PERONOSPORA.PercEfficaciaProdotto;
                            var media_dilavamento = modello_PERONOSPORA.PercDilavamento;
                            var media_protezione = modello_PERONOSPORA.PercProtezione;
                            var media_precipitazioni = modello_PERONOSPORA.SommaPrecipitazioni;

                            riga_PERONOSPORA.Valori[i] = media_protezione;
                            //riga_PERONOSPORA.Valori[i] = random.Next(0, 100);
                            riga_PERONOSPORA.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }
                        else
                        {

                        }
                    }

                    //------ OIDIO
                    {
                        var modello_OIDIO = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO).FirstOrDefault();

                        if (modello_OIDIO != null)
                        {
                            var media_efficacia_prodotto = modello_OIDIO.PercEfficaciaProdotto;
                            var media_dilavamento = modello_OIDIO.PercDilavamento;
                            var media_protezione = modello_OIDIO.PercProtezione;
                            var media_precipitazioni = modello_OIDIO.SommaPrecipitazioni;

                            riga_OIDIO.Valori[i] = media_protezione;
                            //riga_OIDIO.Valori[i] = random.Next(0, 100);
                            riga_OIDIO.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }
                    }
                }

                model.Righe_Vigneti_OIDIO.Add(riga_OIDIO);
                model.Righe_Vigneti_PERONOSPORA.Add(riga_PERONOSPORA);
            }

            //--------------- CARICO TABELLA RISCHI
            //var rischi = modelli_protezione_settimanale.Select(x => new { Id = x.Trattamento.Vigneto.Rischio.Id, Nome = x.Trattamento.Vigneto.Rischio.Descrizione }).Distinct().ToList();

            var rischi = DB.Rischio.Where(x => ids_rischi_da_modelli.Contains(x.Id)).Select(x => new
            {
                Id = x.Id,
                Nome = x.Descrizione,
            }).ToList();

            foreach (var rischio in rischi)
            {
                //var modelli_protezione_rischio = modelli_protezione_settimanale.Where(x => x.TrattamentoAvversita.Trattamento.Vigneto.Rischio.Id == rischio.Id).ToList();
                var ids_vigneti_rischio = vigneti_da_filtri.Where(x => x.IdRischio == rischio.Id).Select(x => x.IdVigneto).ToList();
                var modelli_protezione_rischio = modelli_protezione_settimanale.Where(x => ids_vigneti_rischio.Contains(x.IdVigneto)).ToList();

                if (modelli_protezione_rischio.Count == 0 && model.IdAzienda == 0 && utente.IsMasterUser)
                    continue;

                var riga_OIDIO = new RigaProtezione()
                {
                    Id = rischio.Id,
                    Nome = rischio.Nome,
                };

                var riga_PERONOSPORA = new RigaProtezione()
                {
                    Id = rischio.Id,
                    Nome = rischio.Nome,
                };

                for (int i = 0; i <= 6; i++)
                {
                    var giorno = data.AddDays(i);

                    var modelli_protezione_giorno = modelli_protezione_rischio.Where(x => x.Giorno == giorno).ToList();

                    //------ PERONOSPORA
                    {
                        var modelli_PERONOSPORA = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA).ToList();

                        if (modelli_PERONOSPORA.Count > 0 && ids_vigneti_rischio.Count > 0)
                        {
                            var media_efficacia_prodotto = modelli_PERONOSPORA.Sum(x => x.PercEfficaciaProdotto) / ids_vigneti_rischio.Count;
                            var media_dilavamento = modelli_PERONOSPORA.Sum(x => x.PercDilavamento) / ids_vigneti_rischio.Count;
                            var media_protezione = modelli_PERONOSPORA.Sum(x => x.PercProtezione) / ids_vigneti_rischio.Count;
                            var media_precipitazioni = modelli_PERONOSPORA.Sum(x => x.SommaPrecipitazioni) / ids_vigneti_rischio.Count;

                            riga_PERONOSPORA.Valori[i] = media_protezione;
                            //riga_PERONOSPORA.Valori[i] = random.Next(0, 100);
                            riga_PERONOSPORA.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }
                    }

                    //------ OIDIO
                    {
                        var modelli_OIDIO = modelli_protezione_giorno.Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO).ToList();

                        if (modelli_OIDIO.Count > 0 && ids_vigneti_rischio.Count > 0)
                        {
                            var media_efficacia_prodotto = modelli_OIDIO.Sum(x => x.PercEfficaciaProdotto) / ids_vigneti_da_filtri.Count;
                            var media_dilavamento = modelli_OIDIO.Sum(x => x.PercDilavamento) / ids_vigneti_da_filtri.Count;
                            var media_protezione = modelli_OIDIO.Sum(x => x.PercProtezione) / ids_vigneti_da_filtri.Count;
                            var media_precipitazioni = modelli_OIDIO.Sum(x => x.SommaPrecipitazioni) / ids_vigneti_da_filtri.Count;

                            riga_OIDIO.Valori[i] = media_protezione;
                            //riga_OIDIO.Valori[i] = random.Next(0, 100);
                            riga_OIDIO.Tooltips[i] = $"{Tr.efficacia}: {media_efficacia_prodotto.ToString("0.##")} %<br>{Tr.dilavamento}: {media_dilavamento.ToString("0.##")} %<br>{Tr.mm_pioggia}: {media_precipitazioni.ToString("0.##")}";
                        }
                    }
                }

                model.Righe_Rischio_OIDIO.Add(riga_OIDIO);
                model.Righe_Rischio_PERONOSPORA.Add(riga_PERONOSPORA);
            }

            filtro_regioni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_province.Insert(0, new { Id = "-", Nome = "-" });
            filtro_comuni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_aziende.Insert(0, new { Id = 0, Nome = "-" });
            filtro_gruppi.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vigne.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vigneti.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vitigni.Insert(0, new { Id = 0, Nome = "-" });
            filtro_rischi.Insert(0, new { Id = 0, Nome = "-" });

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
            model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);
            model.Vigne = new SelectList(filtro_vigne, "Id", "Nome", model.IdVigna);
            model.Vigneti = new SelectList(filtro_vigneti, "Id", "Nome", model.IdVigneto);
            model.Vitigni = new SelectList(filtro_vitigni, "Id", "Nome", model.IdVitigno);
            model.Rischi = new SelectList(filtro_rischi, "Id", "Nome", model.IdRischio);

            return View(model);
        }

    }
}

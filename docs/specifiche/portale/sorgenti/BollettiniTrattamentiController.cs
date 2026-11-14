using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using _4Grapes.Traduzioni;
using _4GrapesAPI.Entity;
using _4GrapesAPI.Models;
using _4GrapesAPI.Properties;

namespace _4GrapesAPI.Controllers
{
    [AuthorizePortal(Abilitazione = Abilitazione.BOLLETTINI_AZIENDALI)]
    public class BollettiniTrattamentiController : _BaseController
    {
        //------------------------- BOLLETTINI AZIENDALI GIORNALIERI ----------------------        
        [HttpGet]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)] //- COMMENTATO, USATA ABILITAZIONE BOLLETTINI_AZIENDALI, VERIFICARE PER UTENTE IsUtentePerleuve SE E' CORRETTO
        public ActionResult BollettiniAziendaliGiornalieri(BollettiniAziendaliGiornalieriModel model)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            //--- NO.. SOLO PER TRATTAMENTI AUTOMATICI
            //Utils.RicaricaImpostazioni();
            //var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;
            //var data = DateTime.Today.AddDays(-3);
            var utente = User.GetUtente();

            //---- FILTRI DA / IN SESSIONE
            model.Data = FiltroDaSessione(model.Data, nameof(model.Data));
            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));
            model.IdTipologiaLinea = FiltroDaSessione(model.IdTipologiaLinea, nameof(model.IdTipologiaLinea));
            model.IdTipologiaLinea = FiltroDaSessione(model.IdTipologiaLinea, nameof(model.IdTipologiaLinea));
            model.IdVigneto = FiltroDaSessione(model.IdVigneto, nameof(model.IdVigneto));
            model.IdVitigno = FiltroDaSessione(model.IdVitigno, nameof(model.IdVitigno));
            model.IdRischio = FiltroDaSessione(model.IdRischio, nameof(model.IdRischio));

            //var oggi = DateTime.Today;
            var da_giorno = model.Data.AddDays(-3).StartOfDay();
            var a_giorno = model.Data.AddDays(3).EndOfDay();

            model.DaGiorno = da_giorno;
            model.AGiorno = a_giorno;

            var sql_vigneti = DB.Vigneto.Include(x => x.VignetoMaps).Include(x => x.Vigna.Ente).Include(x => x.Vitigno).Include(x => x.Rischio).Include(x => x.TipologiaLinea)
            .Where(x => x.Carico && !x.Estirpo && x.IdTipologiaLinea != null);
            //.Where(x => x.VignetoMaps != null && x.IdTipologiaLinea != null && x.VignetoMaps.AdministrativeAreaLevel3 != null && !x.Estirpo && x.IdRischio != null);

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

            if (model.IdTipologiaLinea != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdTipologiaLinea == model.IdTipologiaLinea);
            }            

            if (model.IdVigneto != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdVigneto == model.IdVigneto);
            }

            if (model.IdVitigno != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.Vitigno_idVitigno == model.IdVitigno);
            }

            if (model.IdRischio != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdRischio == model.IdRischio);
            }

            List<Vigneto> vigneti = sql_vigneti.ToList();

            var ids_vigneti = vigneti.Select(x => x.IdVigneto).ToList();

            var sql_trattamenti = DB.Trattamento.Include("TrattamentoAvversita.AvversitaDaTrattare").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.LineaDifesa").Include(x => x.TipoTrattamento)
                .Where(x => x.Data >= da_giorno && ids_vigneti.Contains(x.IdVigneto) && (x.Suggerito || x.Effettivo));
            
            if (model.IdTipoBollettino == (int)TipoBollettino.Enum.PERONOSPORA_OIDIO)
            {
                sql_trattamenti = sql_trattamenti.Where(x => x.IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO);
            }
            else if (model.IdTipoBollettino == (int)TipoBollettino.Enum.ALTRE_AVVERSITA)
            {
                sql_trattamenti = sql_trattamenti.Where(x => x.IdTipoTrattamento == (int)TipoTrattamento.Enum.ALTRE_AVVERSITA);
            }

            var trattamenti = sql_trattamenti.ToList();

            var filtro_regioni = vigneti.Where(x => x.VignetoMaps != null).Select(x => new { Id = x.VignetoMaps.Area1, Nome = x.VignetoMaps.AdministrativeAreaLevel1 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_province = vigneti.Where(x => x.VignetoMaps != null).Select(x => new { Id = x.VignetoMaps.Area2, Nome = x.VignetoMaps.AdministrativeAreaLevel2 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_comuni = vigneti.Where(x => x.VignetoMaps != null).Select(x => new { Id = x.VignetoMaps.Area3, Nome = x.VignetoMaps.AdministrativeAreaLevel3 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_aziende = vigneti.Select(x => new { Id = x.Vigna.Ente.IdEnte, Nome = x.Vigna.Ente.RagioneSociale }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_tipologielinee = vigneti.Where(x => x.TipologiaLinea != null).Select(x => new { Id = x.TipologiaLinea.Id, Nome = x.TipologiaLinea.Descrizione }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_vigneti = vigneti.Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_vitigni = vigneti.Where(x => x.Vitigno != null).Select(x => new { Id = x.Vitigno.idVitigno, Nome = x.Vitigno.NomeVitigno }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_rischi = vigneti.Where(x => x.Rischio != null).Select(x => new { Id = x.Rischio.Id, Nome = x.Rischio.DescrizioneInLingua(utente.Lingua) }).Distinct().ToList().OrderBy(x => x.Nome).ToList();                        

            var id_enti_per_filtro = filtro_aziende.Select(x => x.Id).ToList();
            var filtro_gruppi = DB.VistaGruppo_Azienda.Where(x => id_enti_per_filtro.Contains(x.IdEnte)).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().ToList().OrderBy(x => x.Nome).ToList();

            var ids_trattamenti_suggeriti_da_nascondere = trattamenti.Select(x => x.GeneratoDa_IdTrattamento).Where(x => x != null).Distinct().ToList();

            trattamenti = trattamenti.Where(x => !ids_trattamenti_suggeriti_da_nascondere.Contains(x.Id)).ToList();

            //trattamenti.Where(x => x.TipoTrattamento != null).Select(x => new { Id = x.TipoTrattamento.IdTipoTrattamento, Nome = x.TipoTrattamento.Descrizione }).Distinct().ToList().OrderBy(x => x.Id).ToList();            
            var filtro_tipibollettini = DB.TipoBollettino.Select(x => new { Id = x.IdTipoBollettino, Nome = x.Descrizione }).Distinct().ToList().OrderBy(x => x.Id).ToList();  

            var enti = vigneti.Select(x => x.Vigna.Ente).Distinct().ToList();

            //--------------- CARICO TABELLE ENTI            
            foreach (var ente in enti)
            {
                var ids_vigneti_ente = vigneti.Where(x => x.Vigna.Ente_IdEnte == ente.IdEnte).Select(x => x.IdVigneto).ToList();

                var trattamenti_ente = trattamenti.Where(x => ids_vigneti_ente.Contains(x.IdVigneto)).ToList();                

                var riga = new RigaBollettiniAziendaliGiornalieri()
                {
                    Id = ente.IdEnte,
                    Nome = ente.RagioneSociale,
                };

                for (int i = 0; i <= 3 + 3; i++)
                {
                    var giorno = da_giorno.AddDays(i);
                    var da_data_giorno = giorno.StartOfDay();
                    var a_data_giorno = giorno.EndOfDay();

                    var trattamenti_giorno = trattamenti_ente.Where(x => x.Data >= da_data_giorno && x.Data <= a_data_giorno).ToList();
                    //var trattamenti_raggruppati = Trattamento.RaggruppaTrattamenti(trattamenti_giorno);

                    var ids_vigneti_trattamenti_OP = trattamenti_giorno.Where(x => x.IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO).Select(x => x.IdVigneto).ToList();
                    var tipologie_linee_ente_OP = vigneti.Where(x => x.TipologiaLinea != null).Where(x => ids_vigneti_trattamenti_OP.Contains(x.IdVigneto)).Select(x => x.TipologiaLinea).Distinct().ToList();

                    var pulsanti = new List<string>();

                    foreach (var tipologia_linea in tipologie_linee_ente_OP)
                    {
                        //var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                        var text = tipologia_linea.Descrizione;
                        var tooltip = tipologia_linea.Descrizione;

                        var colore = "btn-warning";

                        var attributes = new Dictionary<string, string>();
                        attributes.Add("IdAzienda", ente.IdEnte.ToString());
                        attributes.Add("IdTipologiaLinea", tipologia_linea.Id.ToString());
                        attributes.Add("Giorno", giorno.ToString("dd/MM/yyyy"));

                        pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                    }

                    if (trattamenti_giorno.Any(x => x.IdTipoTrattamento == (int)TipoTrattamento.Enum.ALTRE_AVVERSITA))
                    {
                        var text = Tr.altre_avv.ToUpper();
                        var tooltip = Tr.altre_avv.ToUpper();

                        var colore = "btn-info";

                        var attributes = new Dictionary<string, string>();
                        attributes.Add("IdAzienda", ente.IdEnte.ToString());
                        attributes.Add("Giorno", giorno.ToString("dd/MM/yyyy"));

                        pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino_AltreAvversita btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                    }                    

                    //var tootip_prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => $"{x.AgrofarmacoUnivoco.NOME_COMMERCIALE} [{x.ProdottoVite.NumReg}]").ToList();

                    //var pulsanti_trattamenti = avversita_da_trattare.Select(x => x.Descrizione.EscapeHtmlAttr()).ToList();                    


                    riga.Trattamenti[i] = trattamenti_giorno;
                    riga.Colore[i] = trattamenti_giorno.Count > 0 ? "red" : null;
                    riga.Testo[i] = trattamenti_giorno.Count > 0 ? string.Join(" ", pulsanti) : null;
                    //riga.Tooltips[i] = string.Join("<br>", tootip_prodotti);
                }

                model.RigheEnti.Add(riga);
            }

            filtro_regioni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_province.Insert(0, new { Id = "-", Nome = "-" });
            filtro_comuni.Insert(0, new { Id = "-", Nome = "-" });
            filtro_aziende.Insert(0, new { Id = 0, Nome = "-" });
            filtro_gruppi.Insert(0, new { Id = 0, Nome = "-" });
            filtro_tipologielinee.Insert(0, new { Id = 0, Nome = "-" });
            filtro_tipibollettini.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vigneti.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vitigni.Insert(0, new { Id = 0, Nome = "-" });
            filtro_rischi.Insert(0, new { Id = 0, Nome = "-" });

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
            model.TipologieLinee = new SelectList(filtro_tipologielinee, "Id", "Nome", model.IdTipologiaLinea);
            model.TipiBollettino = new SelectList(filtro_tipibollettini, "Id", "Nome", model.IdTipoBollettino);
            model.Vigneti = new SelectList(filtro_vigneti, "Id", "Nome", model.IdVigneto);
            model.Vitigni = new SelectList(filtro_vitigni, "Id", "Nome", model.IdVitigno);
            model.Rischi = new SelectList(filtro_rischi, "Id", "Nome", model.IdRischio);
            model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);

            var elenco_rischi_completo = DB.Rischio.Select(x => new { Id = x.Id.ToString(), Nome = x.Descrizione }).ToList();
            elenco_rischi_completo.Insert(0, new { Id = "-", Nome = "-" });

            model.Elenco_Rischi_Completo = new SelectList(elenco_rischi_completo, "Id", "Nome", null);

            return View(model);
        }

        public string GeneraBollettino(Bollettino bollettino, string lingua)
        {
            //await EmailUtils.SendAsync("dario.picco@redecode.it;caspel2.0@gmail.com", "Oggetto di prova", "Buongiorno Dario,<br>Ciao!<br><br>4G");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lingua);
            var utente = User.GetUtente();

            var meteo_comuni = GetDatiMeteoComuni(bollettino.IdEnte, bollettino.GiornoRif);

            var ente = DB.Ente.FirstOrDefault(x => x.IdEnte == bollettino.IdEnte);
            var tipologiaLinea = DB.TipologiaLinea.FirstOrDefault(x => x.Id == bollettino.IdTipologiaLinea);

            var trattamenti_azienda = DB.Trattamento.Include(x => x.Rischio).Include(x => x.TransitoNelVigneto).Include("TrattamentoAvversita.LineaDifesa.Rischio").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include(x => x.Vigneto.Vigna).Include(x => x.Vigneto)
                    .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif
                        && x.Gruppo_IdEnte == bollettino.IdEnte
                        && (x.Gruppo_IdTipologiaLinea == null || x.Gruppo_IdTipologiaLinea == bollettino.IdTipologiaLinea)
                        && x.Vigneto.IdTipologiaLinea == bollettino.IdTipologiaLinea
                        && (x.Suggerito || x.Effettivo)
                        && x.IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO).ToList();

            var trattamenti_azienda_inclusi = trattamenti_azienda.Where(x => x.Gruppo_Esclusi != true).ToList();
            var trattamenti_azienda_esclusi = trattamenti_azienda.Where(x => x.Gruppo_Esclusi == true).ToList();

            //var trattamento_Rischi = new List<Trattamento>() { trattamento_ALTO_ALTO, trattamento_ALTO_BASSO, trattamento_BASSO_ALTO, trattamento_BASSO_BASSO };
            var trattamenti_Rischi = trattamenti_azienda_inclusi.ToList();

            var pressione_PERONOSPORA = Ente.GetTestoPressioneDaValore(ente.GetPressione(DB, bollettino.GiornoRif, AvversitaDaTrattare.PERONOSPORA));
            var pressione_OIDIO = Ente.GetTestoPressioneDaValore(ente.GetPressione(DB, bollettino.GiornoRif, AvversitaDaTrattare.OIDIO));

            //---- GET DATI INFEZIONE ------

            var dati_infezione_azienda = DB.ModelloInfezione
                .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif && x.Vigneto.Vigna.Ente_IdEnte == bollettino.IdEnte)
                .Select(x => new
                {
                    x.IdVigneto,
                    x.IdAvversitaDaTrattare,
                    x.PercInfezione,
                })
                .GroupBy(x => new { x.IdVigneto, x.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    x.Key.IdVigneto,
                    x.Key.IdAvversitaDaTrattare,
                    MaxInfezione = x.Max(y => y.PercInfezione),
                })
                .GroupBy(x => new { x.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    x.Key.IdAvversitaDaTrattare,
                    MaxInfezione = x.Max(y => y.MaxInfezione),
                })
                .ToList();

            var probabilita_OIDIO = ModelloInfezione.GetTestoDaValore(dati_infezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)?.MaxInfezione);
            var probabilita_PERONOSPORA = ModelloInfezione.GetTestoDaValore(dati_infezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)?.MaxInfezione);

            //---- GET DATI PROTEZIONE ------

            var data_prima_trattamento = bollettino.GiornoRif.AddHours(-1);

            var dati_protezione_azienda = DB.ModelloProtezione
                .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif && x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente_IdEnte == bollettino.IdEnte && x.TrattamentoAvversita.Trattamento.DataCreazione < data_prima_trattamento && x.TrattamentoAvversita.Trattamento.Suggerito)
                .Select(x => new
                {
                    x.TrattamentoAvversita.Trattamento.IdVigneto,
                    x.TrattamentoAvversita.IdAvversitaDaTrattare,
                    x.PercProtezione,
                })
                .GroupBy(x => new { x.IdVigneto, x.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    x.Key.IdVigneto,
                    x.Key.IdAvversitaDaTrattare,
                    PercProtezione = x.Average(y => y.PercProtezione)
                })
                .GroupBy(x => new { x.IdAvversitaDaTrattare })
                .Select(x => new
                {
                    x.Key.IdAvversitaDaTrattare,
                    PercProtezione = x.Average(y => y.PercProtezione)
                })
                .ToList();

            var protezione_OIDIO = ModelloProtezione.GetTestoDaValore(dati_protezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)?.PercProtezione);
            var protezione_PERONOSPORA = ModelloProtezione.GetTestoDaValore(dati_protezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)?.PercProtezione);

            //var prodotti_ESCLUSI = (trattamenti_azienda_esclusi.SelectMany(x => x.TrattamentoAvversita.ToList() ?? new List<TrattamentoAvversita>())
            //                    .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
            //                    .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
            //                    .OrderBy(x => x.NomeCommerciale)
            //                    .ToList();

            var html_AA = GeneraBollettino_BloccoRischio(bollettino, lingua, Rischio.ALTO_ALTO, "AA", trattamenti_azienda_inclusi, utente, ente);
            var html_AB = GeneraBollettino_BloccoRischio(bollettino, lingua, Rischio.ALTO_BASSO, "AB", trattamenti_azienda_inclusi, utente, ente);
            var html_BA = GeneraBollettino_BloccoRischio(bollettino, lingua, Rischio.BASSO_ALTO, "BA", trattamenti_azienda_inclusi, utente, ente);
            var html_BB = GeneraBollettino_BloccoRischio(bollettino, lingua, Rischio.BASSO_BASSO, "BB", trattamenti_azienda_inclusi, utente, ente);
            var html_EE = GeneraBollettino_BloccoEsclusi(bollettino, lingua, "EE", trattamenti_azienda_esclusi, utente, ente);

            var fields = new Dictionary<string, string>()
            {
                { "PROGRESSIVO" , bollettino.NumeroBollettino.ToString() },
                { "AZIENDA" , ente.RagioneSociale },
                { "BBCH" , trattamenti_Rischi.Moda(x => x.IdBBCH).ToString() },
                { "TIPOLOGIA_LINEA" , tipologiaLinea?.Descrizione},

                { "PROTEZIONE_OIDIO" , protezione_OIDIO},
                { "PROTEZIONE_PERONOSPORA" , protezione_PERONOSPORA},
                { "PRESSIONE_OIDIO" , pressione_OIDIO},
                { "PRESSIONE_PERONOSPORA" , pressione_PERONOSPORA},
                { "PROBABILITA_OIDIO" , probabilita_OIDIO},
                { "PROBABILITA_PERONOSPORA" , probabilita_PERONOSPORA},

                { "DATA_COMUNICAZIONE", DateTime.Today.ToString("dd/MM/yyyy") },
                { "DATA_CONSIGLIO_TRATTAMENTO", bollettino.GiornoRif.ToString("dd/MM/yyyy") },

                { "METEO_COMUNE_1", meteo_comuni.ElementAtOrDefault(0)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_1",  meteo_comuni.ElementAtOrDefault(0)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_1",  meteo_comuni.ElementAtOrDefault(0)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_2", meteo_comuni.ElementAtOrDefault(1)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_2",  meteo_comuni.ElementAtOrDefault(1)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_2",  meteo_comuni.ElementAtOrDefault(1)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_3", meteo_comuni.ElementAtOrDefault(2)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_3",  meteo_comuni.ElementAtOrDefault(2)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_3",  meteo_comuni.ElementAtOrDefault(2)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_4", meteo_comuni.ElementAtOrDefault(3)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_4",  meteo_comuni.ElementAtOrDefault(3)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_4",  meteo_comuni.ElementAtOrDefault(3)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_5", meteo_comuni.ElementAtOrDefault(4)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_5",  meteo_comuni.ElementAtOrDefault(4)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_5",  meteo_comuni.ElementAtOrDefault(4)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "BLOCCO_AA", html_AA},
                { "BLOCCO_AB", html_AB},
                { "BLOCCO_BA", html_BA},
                { "BLOCCO_BB", html_BB},
                { "BLOCCO_EE", html_EE},
            };



            //------------ TIPOLOGIA LINEA  ---------------




            //var utente = User.GetUtente();
            //var email_path = Server.MapPath("~/Email/TrattamentoOidioPeronospora/Comunicazione_LAYOUT 2021 TRATTAMENTI.htm");

            string email_path;

            if (utente.IsSysAdmin || utente.IsMasterUser || utente.IsUtentePerleuve)
            {
                email_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora.{lingua}.html");
                if (!System.IO.File.Exists(email_path))
                {
                    email_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora.EN.html");
                }
            }
            else if (utente.IsDSSUser)
            {
                email_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_DSS.IT.html");
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            //var fi = EmailUtils.GeneraDizionarioMergeFields(email_path);

            var html = EmailUtils.GeneraEmail(email_path, fields);

            //await EmailUtils.SendAsync("michelestecchina@gmail.com", "Email Trattamento TEST", html, html);

            return html;
        }

        public string GeneraBollettino_AltreAvversita(Bollettino bollettino, string lingua)
        {
            //await EmailUtils.SendAsync("dario.picco@redecode.it;caspel2.0@gmail.com", "Oggetto di prova", "Buongiorno Dario,<br>Ciao!<br><br>4G");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lingua);
            var utente = User.GetUtente();

            var meteo_comuni = GetDatiMeteoComuni(bollettino.IdEnte, bollettino.GiornoRif);

            var ente = DB.Ente.FirstOrDefault(x => x.IdEnte == bollettino.IdEnte);
            var tipologiaLinea = DB.TipologiaLinea.FirstOrDefault(x => x.Id == bollettino.IdTipologiaLinea);

            var trattamenti_azienda = DB.Trattamento.Include(x => x.Rischio).Include(x => x.TransitoNelVigneto).Include("TrattamentoAvversita.LineaDifesa.Rischio").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include(x => x.Vigneto.Vigna).Include(x => x.Vigneto)
                                .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif
                                    && x.Gruppo_IdEnte == bollettino.IdEnte
                                    && (x.Suggerito || x.Effettivo)
                                    && x.IdTipoTrattamento == (int)TipoTrattamento.Enum.ALTRE_AVVERSITA).ToList();

            //var trattamenti_azienda_inclusi = trattamenti_azienda.Where(x => x.Gruppo_Esclusi != true).ToList();
            //var trattamenti_azienda_esclusi = trattamenti_azienda.Where(x => x.Gruppo_Esclusi == true).ToList();                         

            var id_avversita = trattamenti_azienda.SelectMany(x => x.TrattamentoAvversita).Select(x => x.IdAvversitaDaTrattare).ToList();

            var elenco_avversita = DB.AvversitaDaTrattare.Where(x => id_avversita.Contains(x.Id)).ToList();
            string html_avversita = "";

            foreach (var avversita in elenco_avversita)
            {
                html_avversita += GeneraBollettino_AltreAvversita_BloccoAvversita(bollettino, lingua, avversita.Id, trattamenti_azienda, utente, ente);
            }

            //var pressione_PERONOSPORA = Ente.GetTestoPressioneDaValore(ente.GetPressione(DB, bollettino.GiornoRif, AvversitaDaTrattare.PERONOSPORA));
            //var pressione_OIDIO = Ente.GetTestoPressioneDaValore(ente.GetPressione(DB, bollettino.GiornoRif, AvversitaDaTrattare.OIDIO));

            //---- GET DATI INFEZIONE ------

            //var dati_infezione_azienda = DB.ModelloInfezione
            //    .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif && x.Vigneto.Vigna.Ente_IdEnte == bollettino.IdEnte)
            //    .Select(x => new
            //    {
            //        x.IdVigneto,
            //        x.IdAvversitaDaTrattare,
            //        x.PercInfezione,
            //    })
            //    .GroupBy(x => new { x.IdVigneto, x.IdAvversitaDaTrattare })
            //    .Select(x => new
            //    {
            //        x.Key.IdVigneto,
            //        x.Key.IdAvversitaDaTrattare,
            //        MaxInfezione = x.Max(y => y.PercInfezione),
            //    })
            //    .GroupBy(x => new { x.IdAvversitaDaTrattare })
            //    .Select(x => new
            //    {
            //        x.Key.IdAvversitaDaTrattare,
            //        MaxInfezione = x.Max(y => y.MaxInfezione),
            //    })
            //    .ToList();

            //var probabilita_OIDIO = ModelloInfezione.GetTestoDaValore(dati_infezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)?.MaxInfezione);
            //var probabilita_PERONOSPORA = ModelloInfezione.GetTestoDaValore(dati_infezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)?.MaxInfezione);

            //---- GET DATI PROTEZIONE ------

            //var data_prima_trattamento = bollettino.GiornoRif.AddHours(-1);

            //var dati_protezione_azienda = DB.ModelloProtezione
            //    .Where(x => DbFunctions.TruncateTime(x.Data) == bollettino.GiornoRif && x.TrattamentoAvversita.Trattamento.Vigneto.Vigna.Ente_IdEnte == bollettino.IdEnte && x.TrattamentoAvversita.Trattamento.DataCreazione < data_prima_trattamento && x.TrattamentoAvversita.Trattamento.Suggerito)
            //    .Select(x => new
            //    {
            //        x.TrattamentoAvversita.Trattamento.IdVigneto,
            //        x.TrattamentoAvversita.IdAvversitaDaTrattare,
            //        x.PercProtezione,
            //    })
            //    .GroupBy(x => new { x.IdVigneto, x.IdAvversitaDaTrattare })
            //    .Select(x => new
            //    {
            //        x.Key.IdVigneto,
            //        x.Key.IdAvversitaDaTrattare,
            //        PercProtezione = x.Average(y => y.PercProtezione)
            //    })
            //    .GroupBy(x => new { x.IdAvversitaDaTrattare })
            //    .Select(x => new
            //    {
            //        x.Key.IdAvversitaDaTrattare,
            //        PercProtezione = x.Average(y => y.PercProtezione)
            //    })
            //    .ToList();

            //var protezione_OIDIO = ModelloProtezione.GetTestoDaValore(dati_protezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)?.PercProtezione);
            //var protezione_PERONOSPORA = ModelloProtezione.GetTestoDaValore(dati_protezione_azienda.FirstOrDefault(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)?.PercProtezione);

            //var prodotti_ESCLUSI = (trattamenti_azienda_esclusi.SelectMany(x => x.TrattamentoAvversita.ToList() ?? new List<TrattamentoAvversita>())
            //                    .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
            //                    .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
            //                    .OrderBy(x => x.NomeCommerciale)
            //                    .ToList();            

            var fields = new Dictionary<string, string>()
            {
                { "PROGRESSIVO" , bollettino.NumeroBollettino.ToString() },
                { "AZIENDA" , ente.RagioneSociale },
                { "BBCH" , trattamenti_azienda.Moda(x => x.IdBBCH).ToString() },
                { "TIPOLOGIA_LINEA" , tipologiaLinea?.Descrizione},

                //{ "PROTEZIONE_OIDIO" , protezione_OIDIO},
                //{ "PROTEZIONE_PERONOSPORA" , protezione_PERONOSPORA},
                //{ "PRESSIONE_OIDIO" , pressione_OIDIO},
                //{ "PRESSIONE_PERONOSPORA" , pressione_PERONOSPORA},
                //{ "PROBABILITA_OIDIO" , probabilita_OIDIO},
                //{ "PROBABILITA_PERONOSPORA" , probabilita_PERONOSPORA},

                { "DATA_COMUNICAZIONE", DateTime.Today.ToString("dd/MM/yyyy") },
                { "DATA_CONSIGLIO_TRATTAMENTO", bollettino.GiornoRif.ToString("dd/MM/yyyy") },

                { "METEO_COMUNE_1", meteo_comuni.ElementAtOrDefault(0)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_1",  meteo_comuni.ElementAtOrDefault(0)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_1", meteo_comuni.ElementAtOrDefault(0)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_1",  meteo_comuni.ElementAtOrDefault(0)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_1", meteo_comuni.ElementAtOrDefault(0)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_2", meteo_comuni.ElementAtOrDefault(1)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_2",  meteo_comuni.ElementAtOrDefault(1)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_2", meteo_comuni.ElementAtOrDefault(1)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_2",  meteo_comuni.ElementAtOrDefault(1)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_2", meteo_comuni.ElementAtOrDefault(1)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_3", meteo_comuni.ElementAtOrDefault(2)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_3",  meteo_comuni.ElementAtOrDefault(2)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_3", meteo_comuni.ElementAtOrDefault(2)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_3",  meteo_comuni.ElementAtOrDefault(2)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_3", meteo_comuni.ElementAtOrDefault(2)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_4", meteo_comuni.ElementAtOrDefault(3)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_4",  meteo_comuni.ElementAtOrDefault(3)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_4", meteo_comuni.ElementAtOrDefault(3)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_4",  meteo_comuni.ElementAtOrDefault(3)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_4", meteo_comuni.ElementAtOrDefault(3)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "METEO_COMUNE_5", meteo_comuni.ElementAtOrDefault(4)?.Nome  },
                { "METEO_ULTIMI_GG_PIOGGIA1MM_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_ULTIMI_MM_PIOGGIA_5",  meteo_comuni.ElementAtOrDefault(4)?.Ultimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_ULTIMI_T_MIN_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMinAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MED_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMedAVG.ToColumn(1)  },
                { "METEO_ULTIMI_T_MAX_5", meteo_comuni.ElementAtOrDefault(4)?.Ultimi_TMaxAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_GG_PIOGGIA1MM_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_NumGGPioggia1MM.ToColumn()  },
                { "METEO_PROSSIMI_MM_PIOGGIA_5",  meteo_comuni.ElementAtOrDefault(4)?.Prossimi_MMPioggiaCumulati.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MIN_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMinAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MED_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMedAVG.ToColumn(1)  },
                { "METEO_PROSSIMI_T_MAX_5", meteo_comuni.ElementAtOrDefault(4)?.Prossimi_TMaxAVG.ToColumn(1)  },

                { "BLOCCO_AVVERSITA", html_avversita},                
            };



            //------------ TIPOLOGIA LINEA  ---------------




            //var utente = User.GetUtente();
            //var email_path = Server.MapPath("~/Email/TrattamentoOidioPeronospora/Comunicazione_LAYOUT 2021 TRATTAMENTI.htm");

            string email_path;

            if (utente.IsSysAdmin || utente.IsMasterUser || utente.IsUtentePerleuve || utente.IsDSSUser)
            {
                email_path = Server.MapPath($"~/Email/TrattamentoAltreAmpelopatie.{lingua}.html");
                if (!System.IO.File.Exists(email_path))
                {
                    email_path = Server.MapPath($"~/Email/TrattamentoAltreAmpelopatie.EN.html");
                }
            }           
            else
            {
                throw new UnauthorizedAccessException();
            }

            //var fi = EmailUtils.GeneraDizionarioMergeFields(email_path);

            var html = EmailUtils.GeneraEmail(email_path, fields);

            //await EmailUtils.SendAsync("michelestecchina@gmail.com", "Email Trattamento TEST", html, html);

            return html;
        }

        public string GeneraBollettino_AltreAvversita_BloccoAvversita(Bollettino bollettino, string lingua, int id_avversita, List<Trattamento> trattamenti_azienda, Utente utente, Ente ente)
        {
            var trattamento = trattamenti_azienda.FirstOrDefault();

            var elenco_prodotti = (trattamento?.TrattamentoAvversita ?? new List<TrattamentoAvversita>())
                                            .Where(x => x.IdAvversitaDaTrattare == id_avversita && x.Dosaggio > 0)
                                            .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
                                            .OrderBy(x => x.NOME_COMMERCIALE)
                                            .Distinct()
                                            .ToList();

            if (elenco_prodotti.Count == 0)
                return null;

            var elenco_vigneti = trattamenti_azienda.Where(x => x.TrattamentoAvversita.Any(y => y.IdAvversitaDaTrattare == id_avversita && y.Dosaggio > 0))
                                            .OrderBy(x => x.Vigneto.Vigna.NomeVigna)
                                            .Select(x => x.Vigneto)
                                            .ToList();

            //var elenco_note = trattamenti_azienda.Where(x => x.TrattamentoAvversita.Any(y => y.IdAvversitaDaTrattare == id_avversita && y.Dosaggio > 0))
            //                                .Where(x => x.Note.IsNotEmpty())
            //                                .Select(x => x.Note)
            //                                .Distinct()
            //                                .ToList();

            var avversita = DB.AvversitaDaTrattare.Where(x => x.Id == id_avversita).FirstOrDefault();
            var superficie_vigneti = elenco_vigneti.Sum(x => x.Superficie ?? 0) / 10000;

            var righe_prodotti = "";

            foreach (var prodotto in elenco_prodotti)
            {
                var fields_riga_prodotti = new Dictionary<string, string>()
                {
                    { "NOME_PRODOTTO" , prodotto?.NOME_COMMERCIALE },
                    { "DOSE_PRODOTTO" , prodotto?.Dosaggio.ToString("0.###") },
                };

                var riga_prodotto_path = Server.MapPath($"~/Email/TrattamentoAltreAmpelopatie_RIGAPRODOTTI.html");

                var html_riga_prodotti = EmailUtils.GeneraEmail(riga_prodotto_path, fields_riga_prodotti);

                righe_prodotti += html_riga_prodotti + "\r\n";
            }



            var fields_blocco = new Dictionary<string, string>()
            {
                { "AVVERSITA", avversita?.Descrizione },
                { "RIGHE_PRODOTTI" , righe_prodotti },

                { "HIDDEN_NOTE", trattamento?.Note.IsEmpty() == true ? "hidden": null },
                { "NOTE" , trattamento?.Note },
                //{ "TRANSITO_NEL_VIGNETO" , trattamento?.TransitoNelVigneto?.DescrizioneInLingua(lingua)},
                //{ "Preparazione_della_miscela" , ""},                                   
                //{ "VELOCITA_ATOMIZZATORE" , "5-6 km/h"},
                //{ "VELOCITA_NEBULIZZATORE" , "5-6 km/h"},
                //{ "VOLUME_ACQUA_ATOMIZZATORE" , trattamento?.VolumeAcqua.ToString() },
                //{ "VOLUME_ACQUA_NEBULIZZATORE" , (trattamento?.VolumeAcqua * 2 / 3).ToString()}, //-- DUE TERZI DELL'ATOMIZZATORE               
                //{ "PRESSIONE_ESERCIZIO_ATOMIZZATORE" , "7-13"},
                //{ "PRESSIONE_ESERCIZIO_NEBULIZZATORE" , "1-2"},
                //{ "GIRI_MOTORE_ATOMIZZATORE" , "1400-1600"},
                //{ "GIRI_MOTORE_NEBULIZZATORE" , "2000"},

                { "VIGNETI", elenco_vigneti?.Select(x => $"{x.Vigna?.NomeVigna} - {x.NomeVigneto} ({(x.Superficie / 10000)?.ToString("0.00")} ha)").ToList().Join("<br>") },

                { "SUPERFICIE_TOTALE", $"{superficie_vigneti.ToString("0.00")} ha"  },

                { "TOTALE_PRODOTTI", elenco_prodotti?.Select(x => $"{x.NOME_COMMERCIALE} - {(x.Dosaggio * superficie_vigneti).ToString("0.000") }").ToList().Join("<br>") },
            };

            string blocco_path;


            blocco_path = Server.MapPath($"~/Email/TrattamentoAltreAmpelopatie_AVVERSITA.{lingua}.html");
            if (!System.IO.File.Exists(blocco_path))
            {
                blocco_path = Server.MapPath($"~/Email/TrattamentoAltreAmpelopatie_AVVERSITA.EN.html");
            }

            //var fi = EmailUtils.GeneraDizionarioMergeFields(email_path);

            var html_blocco = EmailUtils.GeneraEmail(blocco_path, fields_blocco);

            return html_blocco;

        }

        public string GeneraBollettino_BloccoEsclusi(Bollettino bollettino, string lingua, string blocco, List<Trattamento> trattamenti_azienda_esclusi, Utente utente, Ente ente)
        {
            var trattamenti = trattamenti_azienda_esclusi.ToList();

            var trattamento = trattamenti.FirstOrDefault();

            //------------ RISCHI OIDIO ---------------

            var prodotti_OIDIO = (trattamento?.TrattamentoAvversita ?? new List<TrattamentoAvversita>())
                                .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
                                .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
                                .Distinct()
                                .OrderBy(x => x.NOME_COMMERCIALE)
                                .ToList();

            //var prodotti_ESCLUSI = (trattamenti_azienda_esclusi.SelectMany(x => x.TrattamentoAvversita.ToList() ?? new List<TrattamentoAvversita>())
            //                    .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
            //                    .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
            //                    .OrderBy(x => x.NomeCommerciale)
            //                    .ToList();

            //------------ RISCHI PERONOSPORA ---------------

            var prodotti_PERONOSPORA = (trattamento?.TrattamentoAvversita ?? new List<TrattamentoAvversita>())
                                .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)
                                .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
                                .Distinct()
                                .OrderBy(x => x.NOME_COMMERCIALE)
                                .ToList();

            if (prodotti_PERONOSPORA.Count == 0 && prodotti_OIDIO.Count == 0)
                return null;

            var vigneti = trattamenti.OrderBy(x => x.Vigneto.Vigna.NomeVigna).Select(x => x.Vigneto).ToList();

            var superficie_vigneti = vigneti.Sum(x => x.Superficie ?? 0) / 10000;

            var righe_prodotti = "";

            for (int i = 0; i < prodotti_OIDIO.Count || i < prodotti_PERONOSPORA.Count; i++)
            {
                var fields_riga_prodotti = new Dictionary<string, string>()
                {
                    { "NOME_PRODOTTO_OIDIO" , prodotti_OIDIO?.ElementAtOrDefault(i)?.NOME_COMMERCIALE },
                    { "DOSE_PRODOTTO_OIDIO" , prodotti_OIDIO?.ElementAtOrDefault(i)?.Dosaggio.ToString("0.###") },

                    { "NOME_PRODOTTO_PERONOSPORA" , prodotti_PERONOSPORA?.ElementAtOrDefault(i)?.NOME_COMMERCIALE },
                    { "DOSE_PRODOTTO_PERONOSPORA" , prodotti_PERONOSPORA?.ElementAtOrDefault(i)?.Dosaggio.ToString("0.###") },
                };

                var riga_prodotto_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_RIGAPRODOTTI.html");

                var html_riga_prodotti = EmailUtils.GeneraEmail(riga_prodotto_path, fields_riga_prodotti);

                righe_prodotti += html_riga_prodotti + "\r\n";
            }



            var fields_blocco = new Dictionary<string, string>()
            {
                { "RIGHE_PRODOTTI" , righe_prodotti },
                { "HIDDEN_NOTE", trattamento?.Note.IsEmpty() == true ? "hidden": null },

                { "NOTE" , trattamento?.Note },
                { "TRANSITO_NEL_VIGNETO" , trattamento?.TransitoNelVigneto?.DescrizioneInLingua(lingua)},
                //{ "Preparazione_della_miscela" , ""},                                   
                //{ "VELOCITA_ATOMIZZATORE" , "5-6 km/h"},
                //{ "VELOCITA_NEBULIZZATORE" , "5-6 km/h"},
                //{ "VOLUME_ACQUA_ATOMIZZATORE" , trattamento?.VolumeAcqua.ToString() },
                //{ "VOLUME_ACQUA_NEBULIZZATORE" , (trattamento?.VolumeAcqua * 2 / 3).ToString()}, //-- DUE TERZI DELL'ATOMIZZATORE               
                //{ "PRESSIONE_ESERCIZIO_ATOMIZZATORE" , "7-13"},
                //{ "PRESSIONE_ESERCIZIO_NEBULIZZATORE" , "1-2"},
                //{ "GIRI_MOTORE_ATOMIZZATORE" , "1400-1600"},
                //{ "GIRI_MOTORE_NEBULIZZATORE" , "2000"},

                { "VIGNETI", vigneti?.Select(x => $"{x.Vigna?.NomeVigna} - {x.NomeVigneto} ({(x.Superficie / 10000)?.ToString("0.00")} ha)").ToList().Join("<br>") },

                { "SUPERFICIE_TOTALE", $"{superficie_vigneti.ToString("0.00")} ha"  },

                { "TOTALE_PRODOTTI", prodotti_PERONOSPORA.Union(prodotti_OIDIO)?.Select(x => $"{x.NOME_COMMERCIALE} - {(x.Dosaggio * superficie_vigneti).ToString("0.000") }").ToList().Join("<br>") },
            };

            string blocco_path;

            if (utente.IsSysAdmin || utente.IsMasterUser || utente.IsUtentePerleuve)
            {
                blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_{blocco}.{lingua}.html");
                if (!System.IO.File.Exists(blocco_path))
                {
                    blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_{blocco}.EN.html");
                }
            }
            else if (utente.IsDSSUser)
            {
                blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_DSS_{blocco}.IT.html");
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            //var fi = EmailUtils.GeneraDizionarioMergeFields(email_path);

            var html_blocco = EmailUtils.GeneraEmail(blocco_path, fields_blocco);

            return html_blocco;

        }

        public string GeneraBollettino_BloccoRischio(Bollettino bollettino, string lingua, int id_rischio, string blocco, List<Trattamento> trattamenti_azienda_inclusi, Utente utente, Ente ente)
        {
            var trattamenti = trattamenti_azienda_inclusi.Where(x => x.Gruppo_IdRischio == id_rischio).ToList();

            var trattamento = trattamenti.FirstOrDefault();

            //------------ RISCHI OIDIO ---------------

            var prodotti_OIDIO = (trattamento?.TrattamentoAvversita ?? new List<TrattamentoAvversita>())
                                .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
                                .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
                                .OrderBy(x => x.NOME_COMMERCIALE)
                                .ToList();

            //var prodotti_ESCLUSI = (trattamenti_azienda_esclusi.SelectMany(x => x.TrattamentoAvversita.ToList() ?? new List<TrattamentoAvversita>())
            //                    .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.OIDIO)
            //                    .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
            //                    .OrderBy(x => x.NomeCommerciale)
            //                    .ToList();

            //------------ RISCHI PERONOSPORA ---------------

            var prodotti_PERONOSPORA = (trattamento?.TrattamentoAvversita ?? new List<TrattamentoAvversita>())
                                .Where(x => x.IdAvversitaDaTrattare == AvversitaDaTrattare.PERONOSPORA)
                                .Select(x => new { x.AgrofarmacoUnivoco.NOME_COMMERCIALE, x.Dosaggio })
                                .OrderBy(x => x.NOME_COMMERCIALE)
                                .ToList();

            if (prodotti_PERONOSPORA.Count == 0 && prodotti_OIDIO.Count == 0)
                return null;

            var vigneti = trattamenti.OrderBy(x => x.Vigneto.Vigna.NomeVigna).Select(x => x.Vigneto).ToList();

            var superficie_vigneti = vigneti.Sum(x => x.Superficie ?? 0) / 10000;

            var righe_prodotti = "";

            for (int i = 0; i < prodotti_OIDIO.Count || i < prodotti_PERONOSPORA.Count; i++)
            {
                var fields_riga_prodotti = new Dictionary<string, string>()
                {
                    { "NOME_PRODOTTO_OIDIO" , prodotti_OIDIO?.ElementAtOrDefault(i)?.NOME_COMMERCIALE },
                    { "DOSE_PRODOTTO_OIDIO" , prodotti_OIDIO?.ElementAtOrDefault(i)?.Dosaggio.ToString("0.###") },

                    { "NOME_PRODOTTO_PERONOSPORA" , prodotti_PERONOSPORA?.ElementAtOrDefault(i)?.NOME_COMMERCIALE },
                    { "DOSE_PRODOTTO_PERONOSPORA" , prodotti_PERONOSPORA?.ElementAtOrDefault(i)?.Dosaggio.ToString("0.###") },
                };

                var riga_prodotto_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_RIGAPRODOTTI.html");

                var html_riga_prodotti = EmailUtils.GeneraEmail(riga_prodotto_path, fields_riga_prodotti);

                righe_prodotti += html_riga_prodotti + "\r\n";
            }



            var fields_blocco = new Dictionary<string, string>()
            {
                { "RIGHE_PRODOTTI" , righe_prodotti },

                { "HIDDEN_NOTE", trattamento?.Note.IsEmpty() == true ? "hidden": null },
                { "NOTE" , trattamento?.Note },
                { "TRANSITO_NEL_VIGNETO" , trattamento?.TransitoNelVigneto?.DescrizioneInLingua(lingua)},
                //{ "Preparazione_della_miscela" , ""},                                   
                //{ "VELOCITA_ATOMIZZATORE" , "5-6 km/h"},
                //{ "VELOCITA_NEBULIZZATORE" , "5-6 km/h"},
                //{ "VOLUME_ACQUA_ATOMIZZATORE" , trattamento?.VolumeAcqua.ToString() },
                //{ "VOLUME_ACQUA_NEBULIZZATORE" , (trattamento?.VolumeAcqua * 2 / 3).ToString()}, //-- DUE TERZI DELL'ATOMIZZATORE               
                //{ "PRESSIONE_ESERCIZIO_ATOMIZZATORE" , "7-13"},
                //{ "PRESSIONE_ESERCIZIO_NEBULIZZATORE" , "1-2"},
                //{ "GIRI_MOTORE_ATOMIZZATORE" , "1400-1600"},
                //{ "GIRI_MOTORE_NEBULIZZATORE" , "2000"},

                { "VIGNETI", vigneti?.Select(x => $"{x.Vigna?.NomeVigna} - {x.NomeVigneto} ({(x.Superficie / 10000)?.ToString("0.00")} ha)").ToList().Join("<br>") },

                { "SUPERFICIE_TOTALE", $"{superficie_vigneti.ToString("0.00")} ha"  },

                { "TOTALE_PRODOTTI", prodotti_PERONOSPORA.Union(prodotti_OIDIO)?.Select(x => $"{x.NOME_COMMERCIALE} - {(x.Dosaggio * superficie_vigneti).ToString("0.000") }").ToList().Join("<br>") },
            };

            string blocco_path;

            if (utente.IsSysAdmin || utente.IsMasterUser || utente.IsUtentePerleuve)
            {
                blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_{blocco}.{lingua}.html");
                if (!System.IO.File.Exists(blocco_path))
                {
                    blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_{blocco}.EN.html");
                }
            }
            else if (utente.IsDSSUser)
            {
                blocco_path = Server.MapPath($"~/Email/TrattamentoOidioPeronospora_DSS_{blocco}.IT.html");
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            //var fi = EmailUtils.GeneraDizionarioMergeFields(email_path);

            var html_blocco = EmailUtils.GeneraEmail(blocco_path, fields_blocco);

            return html_blocco;

        }

        [HttpPost]
        public async Task<ActionResult> InviaBollettino(int idAzienda, int idTipologiaLinea, DateTime data)
        {
            var utente = User.GetUtente();

            if (idAzienda == 0 || idTipologiaLinea == 0)
            {
                FlashMessageError("Inserire la tipologia linea");

                return RedirectToAction("TrattamentiAziendaliGiornalieri", "Trattamenti");
            }

            if (!utente.IsMasterUser && !utente.GetIdsEntiAbbinati(DB).Contains(idAzienda))
                throw new UnauthorizedAccessException();

            var bollettino = new Bollettino();

            bollettino.IdEnte = idAzienda;
            bollettino.IdTipoBollettino = (int)TipoBollettino.Enum.PERONOSPORA_OIDIO;
            bollettino.IdTipologiaLinea = idTipologiaLinea;
            bollettino.IdUtente = utente.idUtente;
            bollettino.NumeroBollettino = DB.Bollettino.Where(x => x.IdEnte == idAzienda && x.IdTipoBollettino == (int)TipoBollettino.Enum.PERONOSPORA_OIDIO).Max(x => (int?)x.NumeroBollettino) + 1 ?? 1;
            bollettino.GiornoRif = data.Date;

            DB.Bollettino.Add(bollettino);
            DB.SaveChanges();

            try
            {
                var html_IT = GeneraBollettino(bollettino, "IT");
                var html_EN = GeneraBollettino(bollettino, "EN");

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("IT");

                var utenti_che_ricevono = DB.Utente_Ente.Where(x => x.Ente_IdEnte == idAzienda && x.AbilitaInvioBollettini && x.Utente.StatoEntita == 1).Select(x =>
                new
                {
                    x.Utente.Nome,
                    x.Utente.Cognome,
                    x.Utente.LoginName,
                    x.Utente.Lingua
                }).ToList();

                var utenti_IT = utenti_che_ricevono.Where(x => x.Lingua == "IT").ToList();
                var utenti_EN = utenti_che_ricevono.Except(utenti_IT).ToList();

                if (utenti_IT.Count > 0)
                {
                    await EmailUtils.SendAsync(utenti_IT.Select(x => x.LoginName).Join(";"), "Avviso Trattamento", html_IT, casella: CasellaEmail.STUDIO);
                }

                if (utenti_EN.Count > 0)
                {
                    await EmailUtils.SendAsync(utenti_EN.Select(x => x.LoginName).Join(";"), "Tratment Advice", html_EN, casella: CasellaEmail.STUDIO);
                }

                bollettino.DataInvio = DateTime.Now;
                DB.SaveChanges();
            }
            catch (Exception ex)
            {
                DB.Bollettino.Remove(bollettino);
                DB.SaveChanges();

                throw;
            }

            FlashMessage(Tr.messaggio_bollettino_inviato_correttamente);

            return RedirectToAction("TrattamentiAziendaliGiornalieri", "Trattamenti");
        }

        [HttpPost]
        public async Task<ActionResult> InviaBollettino_AltreAvversita(int idAzienda, DateTime data)
        {
            var utente = User.GetUtente();

            if (idAzienda == 0)
            {                
                return RedirectToAction("TrattamentiAziendaliGiornalieri", "Trattamenti");
            }

            if (!utente.IsMasterUser && !utente.GetIdsEntiAbbinati(DB).Contains(idAzienda))
                throw new UnauthorizedAccessException();

            var bollettino = new Bollettino();

            bollettino.IdEnte = idAzienda;
            bollettino.IdTipoBollettino = (int)TipoBollettino.Enum.ALTRE_AVVERSITA;
            bollettino.IdUtente = utente.idUtente;
            bollettino.NumeroBollettino = DB.Bollettino.Where(x => x.IdEnte == idAzienda && x.IdTipoBollettino == (int)TipoBollettino.Enum.ALTRE_AVVERSITA).Max(x => (int?)x.NumeroBollettino) + 1 ?? 1;
            bollettino.GiornoRif = data.Date;

            DB.Bollettino.Add(bollettino);
            DB.SaveChanges();

            try
            {
                var html_IT = GeneraBollettino_AltreAvversita(bollettino, "IT");
                var html_EN = GeneraBollettino_AltreAvversita(bollettino, "EN");

                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("IT");

                var utenti_che_ricevono = DB.Utente_Ente.Where(x => x.Ente_IdEnte == idAzienda && x.AbilitaInvioBollettini && x.Utente.StatoEntita == 1).Select(x =>
                new
                {
                    x.Utente.Nome,
                    x.Utente.Cognome,
                    x.Utente.LoginName,
                    x.Utente.Lingua
                }).ToList();

                var utenti_IT = utenti_che_ricevono.Where(x => x.Lingua == "IT").ToList();
                var utenti_EN = utenti_che_ricevono.Except(utenti_IT).ToList();

                if (utenti_IT.Count > 0)
                {
                    await EmailUtils.SendAsync(utenti_IT.Select(x => x.LoginName).Join(";"), "Avviso Trattamento", html_IT, casella: CasellaEmail.STUDIO);
                }

                if (utenti_EN.Count > 0)
                {
                    await EmailUtils.SendAsync(utenti_EN.Select(x => x.LoginName).Join(";"), "Tratment Advice", html_EN, casella: CasellaEmail.STUDIO);
                }

                bollettino.DataInvio = DateTime.Now;
                DB.SaveChanges();
            }
            catch (Exception ex)
            {
                DB.Bollettino.Remove(bollettino);
                DB.SaveChanges();

                throw;
            }

            FlashMessage(Tr.messaggio_bollettino_inviato_correttamente);

            return RedirectToAction("TrattamentiAziendaliGiornalieri", "Trattamenti");
        }

        [HttpGet]
        public ActionResult PreviewBollettino(int idAzienda, int idTipologiaLinea, DateTime data, string lingua = null)
        {
            var bollettino = new Bollettino();
            bollettino.IdEnte = idAzienda;
            bollettino.IdTipologiaLinea = idTipologiaLinea;
            bollettino.GiornoRif = data;
            bollettino.NumeroBollettino = DB.Bollettino.Where(x => x.IdEnte == idAzienda && x.IdTipoBollettino == (int)TipoBollettino.Enum.PERONOSPORA_OIDIO).Max(x => (int?)x.NumeroBollettino) + 1 ?? 1;

            var html = GeneraBollettino(bollettino, lingua ?? "IT");
            return Content(html);
        }

        [HttpGet]
        public ActionResult PreviewBollettino_AltreAvversita(int idAzienda, DateTime data, string lingua = null)
        {
            var bollettino = new Bollettino();
            bollettino.IdEnte = idAzienda;
            bollettino.GiornoRif = data;
            bollettino.NumeroBollettino = DB.Bollettino.Where(x => x.IdEnte == idAzienda && x.IdTipoBollettino == (int)TipoBollettino.Enum.ALTRE_AVVERSITA).Max(x => (int?)x.NumeroBollettino) + 1 ?? 1;

            var html = GeneraBollettino_AltreAvversita(bollettino, lingua ?? "IT");
            return Content(html);
        }

        private List<RigaPrevisioneMeteo> GetDatiMeteoComuni(int id_ente, DateTime giorno)
        {
            var righe = new List<RigaPrevisioneMeteo>();

            var ultimi_da_giorno = giorno.AddDays(-7).StartOfDay();
            var ultimi_a_giorno = giorno.AddDays(-1).EndOfDay();

            var prossimi_da_giorno = giorno.StartOfDay();
            var prossimi_a_giorno = giorno.AddDays(5).EndOfDay();

            var comuni = DB.VignetoMaps.Where(x => x.Vigneto.Vigna.Ente_IdEnte == id_ente && x.Vigneto.Carico && !x.Vigneto.Estirpo).Select(x => new { Id = x.Area3, Nome = x.AdministrativeAreaLevel3 }).Distinct().ToList();
            var ids_comuni = comuni.Select(x => x.Id).ToList();

            var ultimi_dati_meteo_comuni = DB.MeteoGiornaliero
                .Where(x => x.Data >= ultimi_da_giorno && x.Data <= ultimi_a_giorno && ids_comuni.Contains(x.Vigneto.VignetoMaps.Area3))
                .Select(x => new
                {
                    x.IdVigneto,
                    x.Vigneto.VignetoMaps.Area3,
                    GGPioggia1MM = x.Precipitazione1 > 0 ? 1 : 0,
                    x.Precipitazione,
                    x.TemperaturaMin,
                    x.TemperaturaMed,
                    x.TemperaturaMax,
                })
                .GroupBy(x => new { x.IdVigneto, x.Area3 })
                .Select(x => new
                {
                    x.Key.IdVigneto,
                    x.Key.Area3,
                    GGPioggia1MM = x.Sum(y => y.GGPioggia1MM),
                    Precipitazione = x.Sum(y => y.Precipitazione),
                    TemperaturaMin = x.Average(y => y.TemperaturaMin),
                    TemperaturaMed = x.Average(y => y.TemperaturaMed),
                    TemperaturaMax = x.Average(y => y.TemperaturaMax),
                })
                .GroupBy(x => x.Area3)
                .Select(x => new
                {
                    IdComune = x.Key,
                    NumGGPioggia1MM = x.Average(y => y.GGPioggia1MM),
                    MMPioggiaCumulati = x.Average(y => y.Precipitazione),
                    TMinAVG = x.Average(y => y.TemperaturaMin),
                    TMedAVG = x.Average(y => y.TemperaturaMed),
                    TMaxAVG = x.Average(y => y.TemperaturaMax),
                })
                .ToList();

            var prossimi_dati_meteo_comuni = DB.MeteoGiornaliero
                .Where(x => x.Data >= prossimi_da_giorno && x.Data <= prossimi_a_giorno && ids_comuni.Contains(x.Vigneto.VignetoMaps.Area3))
                .Select(x => new
                {
                    x.IdVigneto,
                    x.Vigneto.VignetoMaps.Area3,
                    GGPioggia1MM = x.Precipitazione1 > 0 ? 1 : 0,
                    x.Precipitazione,
                    x.TemperaturaMin,
                    x.TemperaturaMed,
                    x.TemperaturaMax,
                })
                .GroupBy(x => new { x.IdVigneto, x.Area3 })
                .Select(x => new
                {
                    x.Key.IdVigneto,
                    x.Key.Area3,
                    GGPioggia1MM = x.Sum(y => y.GGPioggia1MM),
                    Precipitazione = x.Sum(y => y.Precipitazione),
                    TemperaturaMin = x.Average(y => y.TemperaturaMin),
                    TemperaturaMed = x.Average(y => y.TemperaturaMed),
                    TemperaturaMax = x.Average(y => y.TemperaturaMax),
                })
                .GroupBy(x => x.Area3)
                .Select(x => new
                {
                    IdComune = x.Key,
                    NumGGPioggia1MM = x.Average(y => y.GGPioggia1MM),
                    MMPioggiaCumulati = x.Average(y => y.Precipitazione),
                    TMinAVG = x.Average(y => y.TemperaturaMin),
                    TMedAVG = x.Average(y => y.TemperaturaMed),
                    TMaxAVG = x.Average(y => y.TemperaturaMax),
                })
                .ToList();

            foreach (var comune in comuni)
            {
                var ultimi = ultimi_dati_meteo_comuni.Where(x => x.IdComune == comune.Id).FirstOrDefault();
                var prossimi = prossimi_dati_meteo_comuni.Where(x => x.IdComune == comune.Id).FirstOrDefault();

                if (ultimi == null && prossimi == null)
                {
                    continue;
                }

                var riga = new RigaPrevisioneMeteo()
                {
                    Id = comune.Id,
                    Nome = comune.Nome,

                    Ultimi_NumGGPioggia1MM = (int?)ultimi?.NumGGPioggia1MM ?? 0,
                    Ultimi_MMPioggiaCumulati = ultimi?.MMPioggiaCumulati ?? 0,
                    Ultimi_TMinAVG = ultimi?.TMinAVG ?? 0,
                    Ultimi_TMedAVG = ultimi?.TMedAVG ?? 0,
                    Ultimi_TMaxAVG = ultimi?.TMaxAVG ?? 0,

                    Prossimi_NumGGPioggia1MM = (int?)prossimi?.NumGGPioggia1MM ?? 0,
                    Prossimi_MMPioggiaCumulati = prossimi?.MMPioggiaCumulati ?? 0,
                    Prossimi_TMinAVG = prossimi?.TMinAVG ?? 0,
                    Prossimi_TMedAVG = prossimi?.TMedAVG ?? 0,
                    Prossimi_TMaxAVG = prossimi?.TMaxAVG ?? 0,
                };

                righe.Add(riga);
            }

            return righe.OrderBy(x => x.Nome).ToList();
        }
    }    
}
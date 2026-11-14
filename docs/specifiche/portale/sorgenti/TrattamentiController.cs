using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using _4Grapes.Traduzioni;
using _4GrapesAPI.Entity;
using _4GrapesAPI.Models;
using _4GrapesAPI.Properties;

namespace _4GrapesAPI.Controllers
{
    //[AuthorizePortal(IsMasterUser = true)]
    [AuthorizePortal(AbilitazioniAny = new Abilitazione[] { Abilitazione.TRATTAMENTI, Abilitazione.TRATTAMENTI_AZIENDALI, Abilitazione.TRATTAMENTI_AUTOMATICI },
                     OppureIdAppIs = new int[] { App.SARDEGNA })]
    public partial class TrattamentiController : _BaseController
    {

        private Trattamento GetNuovoTrattamento(int id_tipo_trattamento, int? idVigneto)
        {
            var trattamento = new Trattamento();

            trattamento.IdTipoTrattamento = id_tipo_trattamento;
            trattamento.Suggerito = true;
            trattamento.IdUtenteCreazione = User.ID();
            trattamento.DataCreazione = DateTime.Now;
            trattamento.IdVigneto = idVigneto ?? 0;

            trattamento.BBCH = new BBCH();

            return trattamento;
        }

        private Trattamento GetTrattamentoFromId(int? id)
        {
            var trattmento = DB.Trattamento
                .Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.VignetoMaps).Include(x => x.TrattamentoAvversita).Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AvversitaDaTrattare").Include(x => x.BBCH).Include(x => x.Canopy).Include(x => x.Ente).Include(x => x.Rischio)
                .First(x => x.Id == id);

            return trattmento;
        }

        //-- id_tipologia_linea PUO ESSERE NULL PER RETROCOMPATIBILITA'
        private List<Trattamento> GetGruppoTrattamenti(int id_ente, int? id_rischio, int? id_tipologia_linea, bool esclusi, DateTime data, bool suggerito, bool effettivo)
        {
            var trattamenti_gruppo = DB.Trattamento
                .Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.VignetoMaps).Include(x => x.TrattamentoAvversita).Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AvversitaDaTrattare").Include(x => x.BBCH).Include(x => x.Canopy).Include(x => x.Vigneto.Vitigno)
                //.Where(x => x.Data == data && x.Vigneto.Vigna.Ente_IdEnte == id_ente && x.Vigneto.IdRischio == id_rischio && x.Vigneto.Carico && x.Vigneto.Estirpo == false && x.Suggerito == suggerito && x.Effettivo == effettivo).ToList();            
                .Where(x => x.Data == data && x.Gruppo_IdEnte == id_ente && x.Gruppo_IdRischio == id_rischio && (x.Gruppo_IdTipologiaLinea == null || x.Gruppo_IdTipologiaLinea == id_tipologia_linea) && (x.Gruppo_Esclusi ?? false) == esclusi && x.Vigneto.Carico && x.Vigneto.Estirpo == false && x.Suggerito == suggerito && x.Effettivo == effettivo).ToList();

            return trattamenti_gruppo;
        }

        private List<Trattamento> GetGruppoTrattamenti_AUTOMATICI(int id_ente, List<int> id_avversita, List<string> numreg_prodotti, DateTime data)
        {

            var trattamenti_gruppo = DB.Trattamento
                .Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.VignetoMaps).Include(x => x.TrattamentoAvversita).Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AvversitaDaTrattare").Include(x => x.BBCH).Include(x => x.Canopy).Include(x => x.Vigneto.Vitigno)
                //.Where(x => x.Data == data && x.Vigneto.Vigna.Ente_IdEnte == id_ente && x.Vigneto.IdRischio == id_rischio && x.Vigneto.Carico && x.Vigneto.Estirpo == false && x.Suggerito == suggerito && x.Effettivo == effettivo).ToList();            
                .Where(x => x.Data == data && x.Vigneto.Vigna.Ente_IdEnte == id_ente && x.Automatico == true).ToList();

            trattamenti_gruppo = trattamenti_gruppo.Where(x => x.TrattamentoAvversita.Select(y => y.IdAvversitaDaTrattare).OrderBy(t => t).SequenceEqual(id_avversita.OrderBy(t => t)) && x.TrattamentoAvversita.Select(y => y.NumRegProdottoVite).OrderBy(t => t).SequenceEqual(numreg_prodotti.OrderBy(t => t))).ToList();
            //trattamenti_gruppo = trattamenti_gruppo.Where(x => EqualityComparer<TrattamentoSimileComparer>.Equals(x, trattamento)).ToList();

            return trattamenti_gruppo;
        }

        private List<Trattamento> GetNuovoGruppoTrattamenti(int id_tipo_trattamento, int id_ente, int? id_rischio, int? id_tipologia_linea, bool esclusi, DateTime data, bool suggerito, bool effettivo)
        {

            var trattamenti_effettuati = GetGruppoTrattamenti(id_ente, id_rischio, id_tipologia_linea, esclusi, data, suggerito, effettivo);

            var ids_vigneti = DB.Vigneto.Where(x => x.Carico
                                    && !x.Estirpo
                                    && x.IdTipologiaLinea != null
                                    && x.Vigna.Ente_IdEnte == id_ente
                                    && (id_rischio == null || x.IdRischio == id_rischio)
                                    && (id_tipologia_linea == null || x.IdTipologiaLinea == id_tipologia_linea)
                                ).Select(x => x.IdVigneto).ToList();
            var ids_vigneti_trattati = trattamenti_effettuati.Select(x => x.IdVigneto).ToList();
            var ids_vigneti_da_trattare = ids_vigneti.Except(ids_vigneti_trattati).ToList();

            var trattamenti_gruppo = new List<Trattamento>();

            trattamenti_gruppo.AddRange(trattamenti_effettuati);

            var vigneti = DB.Vigneto.Include(x => x.Vitigno).Where(x => ids_vigneti_da_trattare.Contains(x.IdVigneto)).ToList();

            foreach (var id_vigneto in ids_vigneti_da_trattare)
            {
                var trattamento = new Trattamento();

                trattamento.Suggerito = true;
                trattamento.IdUtenteCreazione = User.ID();
                trattamento.DataCreazione = DateTime.Now;
                trattamento.Gruppo_IdEnte = id_ente;
                trattamento.Gruppo_IdRischio = id_rischio;
                trattamento.Gruppo_IdTipologiaLinea = id_tipologia_linea;
                trattamento.Gruppo_Esclusi = false;
                trattamento.IdVigneto = id_vigneto;
                trattamento.Vigneto = vigneti.First(x => x.IdVigneto == id_vigneto);
                trattamento.IdTipoTrattamento = id_tipo_trattamento;

                trattamenti_gruppo.Add(trattamento);
            }

            return trattamenti_gruppo;
        }

        private List<VignetoInGruppo> GetGruppoVigneti(List<int> id_vigneti, DateTime giorno)
        {     
            var vigneti = DB.Vigneto.Where(x => id_vigneti.Contains(x.IdVigneto)).Select(x => new VignetoInGruppo
            {
                IdVigneto = x.IdVigneto,
                Vigneto = x.NomeVigneto,
                Vitigno = x.Vitigno.NomeVitigno
            }).ToList();

            var pressioni = DB.VistaBBCHPressioneProbabilita.Where(x => id_vigneti.Contains(x.IdVigneto) && x.Giorno == giorno).Select(x => new
            {
                x.IdVigneto,
                x.DescrizionePeronospora,
                x.DescrizioneOidio
            }).ToList();

            foreach (var vigneto in vigneti)
            {
                var pressione = pressioni.Where(x => x.IdVigneto == vigneto.IdVigneto).FirstOrDefault();

                vigneto.Pressione_PERONOSPORA = pressione?.DescrizionePeronospora;
                vigneto.Pressione_OIDIO = pressione?.DescrizioneOidio;
            }

            return vigneti;
        }

        [HttpGet]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]        
        public ActionResult Form(int? id, int? IdAzienda, int? IdRischio, int? IdTipologiaLinea, int? IdVigneto, DateTime? data, int? IdTipoTrattamento, bool daLista = false)
        {
            if (IdAzienda != null && IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO && (IdRischio == null || IdTipologiaLinea == null))
            {
                FlashMessageError("Rischio o Tipologia Linea non impostate");
                return Redirect(Request.UrlReferrer.AbsoluteUri);
                //return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri), new { IdAzienda });
            }

            IdTipologiaLinea = IdTipologiaLinea.NullIfZero();
            IdAzienda = IdAzienda.NullIfZero();
            IdVigneto = IdVigneto.NullIfZero();
            IdTipoTrattamento = IdTipoTrattamento.NullIfZero();

            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            Trattamento model;

            if (id == null)
            {
                model = GetNuovoTrattamento(IdTipoTrattamento ?? (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO, IdVigneto);

                model.Gruppo_IdEnte = IdAzienda;
                model.Gruppo_IdRischio = IdRischio;
                model.Gruppo_IdTipologiaLinea = IdTipologiaLinea;
                model.Gruppo_Esclusi = false;
                model.Data = data ?? DateTime.Now;
                model.IdTipoTrattamento = IdTipoTrattamento ?? (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO;

                if (IdVigneto != null)
                {
                    model.IdVigneto = (int)IdVigneto;

                    var anno = model.Data.Year;

                    var bbch = DB.ModelloBBCH
                        .Where(x => x.Data.Year == anno && x.Data <= model.Data && x.IdVigneto == model.IdVigneto)
                        .Max(y => (int?)y.IdBBCH_UltimaSettimana) ?? 0;

                    model.BBCH = DB.BBCH.Where(x => x.IdBBCH == bbch).First();
                    model.Canopy = DB.Canopy.Where(x => x.IdBBCH == bbch).First();

                    model.IdBBCH = model.BBCH.IdBBCH;
                    model.IdCanopy = model.Canopy.Id;

                    model.Vigneto = DB.Vigneto.Include(x => x.Vigna.Ente).Include(x => x.VignetoMaps).Where(x => x.IdVigneto == model.IdVigneto).First();
                }
                else if (model.Gruppo_IdEnte != null)
                {
                    model.BBCH.IdBBCH = -1; //--- AUTOMATICO
                }

                model.Ente = DB.Ente.Find(model.Gruppo_IdEnte);
                model.Rischio = DB.Rischio.Find(model.Gruppo_IdRischio);
                model.TipologiaLinea = DB.TipologiaLinea.Find(model.Gruppo_IdTipologiaLinea);

                if (model.Gruppo_IdEnte != null /*&& model.Gruppo_IdRischio != null*/)
                {
                    var id_vigneti = GetNuovoGruppoTrattamenti(model.IdTipoTrattamento, (int)model.Gruppo_IdEnte, model.Gruppo_IdRischio, model.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, model.Data, model.Suggerito, model.Effettivo).Select(x => x.IdVigneto).ToList();
                    model.Gruppo_Vigneti = GetGruppoVigneti(id_vigneti, model.Data.Date);

                    if (model.Gruppo_Vigneti.Count == 0)
                    {
                        FlashMessageError(Tr.messaggio_non_sono_presenti_vigneti_per_gruppo_selezionato);
                    }
                }
            }
            else
            {
                model = GetTrattamentoFromId(id);

                if (model.Automatico)
                {
                    var id_ente = model.Vigneto?.Vigna?.Ente_IdEnte;
                    var giorno = model.Data.Date;
                    var id_avversita = model.TrattamentoAvversita.Select(x => x.IdAvversitaDaTrattare).ToList();
                    var numreg_prodotti = model.TrattamentoAvversita.Select(x => x.NumRegProdottoVite).ToList();

                    if (id_ente != null /*&& model.Gruppo_IdRischio != null*/)
                    {
                        var id_vigneti = GetGruppoTrattamenti_AUTOMATICI((int)id_ente, id_avversita, numreg_prodotti, model.Data).Select(x => x.IdVigneto).ToList();
                        model.Gruppo_Vigneti = GetGruppoVigneti(id_vigneti, model.Data.Date);
                    }
                }
                else
                {
                    if (model.Gruppo_IdEnte != null /*&& model.Gruppo_IdRischio != null*/)
                    {
                        var id_vigneti = GetGruppoTrattamenti((int)model.Gruppo_IdEnte, model.Gruppo_IdRischio, model.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, model.Data, model.Suggerito, model.Effettivo).Select(x => x.IdVigneto).ToList();
                        model.Gruppo_Vigneti = GetGruppoVigneti(id_vigneti, model.Data.Date);
                    }
                }
            }

            if (daLista)
            {
                Session["Trattamento.ArrivaDa"] = Request.UrlReferrer;
            }

            return View(model);
        }

        [HttpPost]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
        //[AuthorizePortal(Abilitazioni = new Abilitazione[] { Abilitazione.TRATTAMENTI, Abilitazione.TRATTAMENTI_AZIENDALI, Abilitazione.TRATTAMENTI_AUTOMATICI })]
        public async Task<ActionResult> Form(int? id, Trattamento model, string IdVignetiDaRimuovere = "", string IdVignetiDaEscludere = "", bool RimuoviDalGruppo = false)
        {
            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            if (Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso)
            {
                FlashMessageError(Tr.errore_aggiornamento_modelli_in_corso);
                return RedirectToAction(nameof(Form), new { id });
            }

            if (model.Tab_NumRegProdottiVite.Count() != model.Tab_NumRegProdottiVite.Distinct().Count())
            {
                FlashMessageError(Tr.errore_impossibile_inserire_due_volte_stesso_prodotto);
                return RedirectToAction(nameof(Form), new { id });
            }

            var ids_vigneti_da_escludere = IdVignetiDaEscludere?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray() ?? new int[0];
            var ids_vigneti_da_rimuovere = IdVignetiDaRimuovere?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray() ?? new int[0];

            List<Trattamento> trattamenti;
            if (id == null)
            {
                //----- NUOVO TRATTAMENTO
                if (model.Gruppo_IdEnte != null /*&& model.Gruppo_IdRischio != null*/)
                {
                    trattamenti = GetNuovoGruppoTrattamenti(model.IdTipoTrattamento, (int)model.Gruppo_IdEnte, model.Gruppo_IdRischio, model.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, model.Data, model.Suggerito, model.Effettivo);

                    trattamenti.RemoveAll(x => ids_vigneti_da_rimuovere.Contains(x.IdVigneto));

                }
                else
                {
                    var trattamento_attuale = GetNuovoTrattamento(model.IdTipoTrattamento, model.IdVigneto);
                    if (!trattamento_attuale.CanEdit)
                    {
                        throw new UnauthorizedAccessException();
                    }

                    trattamenti = new List<Trattamento> { trattamento_attuale };
                }
            }
            else
            {
                //----- MODIFICA TRATTAMENTO
                var trattamento_attuale = GetTrattamentoFromId(id);
                if (!trattamento_attuale.CanEdit)
                {
                    throw new UnauthorizedAccessException();
                }

                if (trattamento_attuale.Gruppo_IdEnte != null /* && trattamento_attuale.Gruppo_IdRischio != null /*&& trattamento_attuale.Gruppo_IdTipologiaLinea != null*/) // COMMENTATO PER RETROCOMPATIBILITA'
                {
                    if (RimuoviDalGruppo)
                    {
                        trattamento_attuale.Gruppo_IdEnte = null;
                        trattamento_attuale.Gruppo_IdRischio = null;
                        trattamento_attuale.Gruppo_IdTipologiaLinea = null;
                        trattamento_attuale.Gruppo_Esclusi = null;

                        trattamenti = new List<Trattamento> { trattamento_attuale };
                    }
                    else
                    {
                        trattamenti = GetGruppoTrattamenti((int)trattamento_attuale.Gruppo_IdEnte, trattamento_attuale.Gruppo_IdRischio, trattamento_attuale.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, trattamento_attuale.Data, trattamento_attuale.Suggerito, trattamento_attuale.Effettivo);

                        var trattamenti_da_escludere = trattamenti.Where(x => ids_vigneti_da_escludere.Contains(x.IdVigneto)).ToList();
                        var trattamenti_da_rimuovere = trattamenti.Where(x => ids_vigneti_da_rimuovere.Contains(x.IdVigneto)).ToList();

                        var trattamento_gruppo_esclusi = DB.Trattamento.Include(x => x.TrattamentoAvversita).Where(x => x.Data == trattamento_attuale.Data && x.Gruppo_IdEnte == trattamento_attuale.Gruppo_IdEnte && x.Gruppo_Esclusi == true).FirstOrDefault();

                        foreach (var trattamento_da_escludere in trattamenti_da_escludere)
                        {
                            trattamento_da_escludere.Gruppo_IdTipologiaLinea = null;
                            trattamento_da_escludere.Gruppo_IdRischio = null;
                            trattamento_da_escludere.Gruppo_Esclusi = true;

                            if (trattamento_gruppo_esclusi != null)
                            {
                                trattamento_da_escludere.IdBBCH = trattamento_gruppo_esclusi.IdBBCH;
                                trattamento_da_escludere.IdCanopy = trattamento_gruppo_esclusi.IdCanopy;
                                trattamento_da_escludere.VolumeAcqua = trattamento_gruppo_esclusi.VolumeAcqua;
                                trattamento_da_escludere.VolumeAcquaRif = trattamento_gruppo_esclusi.VolumeAcquaRif;
                                trattamento_da_escludere.Note = trattamento_gruppo_esclusi.Note;
                                trattamento_da_escludere.Data = trattamento_gruppo_esclusi.Data;
                                trattamento_da_escludere.Gruppo_IdEnte = trattamento_gruppo_esclusi.Gruppo_IdEnte;
                                trattamento_da_escludere.Gruppo_IdRischio = trattamento_gruppo_esclusi.Gruppo_IdRischio;
                                trattamento_da_escludere.Gruppo_IdTipologiaLinea = trattamento_gruppo_esclusi.Gruppo_IdTipologiaLinea;
                                trattamento_da_escludere.CoefficienteCanopy = trattamento_gruppo_esclusi.CoefficienteCanopy;
                                trattamento_da_escludere.IdTransitoNelVigneto = trattamento_gruppo_esclusi.IdTransitoNelVigneto;
                                trattamento_da_escludere.Automatico = trattamento_gruppo_esclusi.Automatico;
                                trattamento_da_escludere.Suggerito = trattamento_gruppo_esclusi.Suggerito;
                                trattamento_da_escludere.Effettivo = trattamento_gruppo_esclusi.Effettivo;

                                DB.TrattamentoAvversita.RemoveRange(trattamento_da_escludere.TrattamentoAvversita);

                                foreach (var trattamento_avversita_esclusi in trattamento_gruppo_esclusi.TrattamentoAvversita)
                                {
                                    var trattamentoAvversita = new TrattamentoAvversita()
                                    {
                                        IdLineaDifesa = trattamento_avversita_esclusi.IdLineaDifesa,
                                        NumRegProdottoVite = trattamento_avversita_esclusi.NumRegProdottoVite,
                                        IdAvversitaDaTrattare = trattamento_avversita_esclusi.IdAvversitaDaTrattare,
                                        Dosaggio_Rif_Perleuve = trattamento_avversita_esclusi.Dosaggio_Rif_Perleuve,
                                        Dosaggio_OLD = trattamento_avversita_esclusi.Dosaggio_OLD,
                                        Dosaggio_LWA = trattamento_avversita_esclusi.Dosaggio_LWA,
                                        Dosaggio_TRV = trattamento_avversita_esclusi.Dosaggio_TRV,
                                        Dosaggio = trattamento_avversita_esclusi.Dosaggio,
                                    };

                                    trattamento_da_escludere.TrattamentoAvversita.Add(trattamentoAvversita);
                                }
                            }
                        }

                        //foreach (var trattamento_da_rimuovere in trattamenti_da_rimuovere)
                        //{
                        //    trattamento_da_rimuovere.Gruppo_IdEnte = null;
                        //    trattamento_da_rimuovere.Gruppo_IdRischio = null;
                        //    trattamento_da_rimuovere.Gruppo_IdTipologiaLinea = null;
                        //    trattamento_da_rimuovere.Gruppo_Esclusi = null;
                        //}

                        DB.Trattamento.RemoveRange(trattamenti_da_rimuovere);

                        trattamenti.RemoveAll(x => ids_vigneti_da_escludere.Contains(x.IdVigneto));
                        trattamenti.RemoveAll(x => ids_vigneti_da_rimuovere.Contains(x.IdVigneto));

                        if (trattamenti.Count > 0)
                        {
                            id = trattamenti.First().Id;
                        }
                    }
                }
                else
                {
                    trattamenti = new List<Trattamento> { trattamento_attuale };
                }
            }

            var MAX_AltezzaRif = DB.Canopy.Max(x => x.AltezzaRif);
            var MAX_LarghezzaRif = DB.Canopy.Max(x => x.LarghezzaRif);



            foreach (var trattamento in trattamenti)
            {
                Canopy canopy;
                if (trattamento.Id == 0)
                {
                    var anno = trattamento.Data.Year;

                    var bbch = model.IdBBCH;

                    if (bbch == -1)
                    {
                        bbch = DB.ModelloBBCH
                            .Where(x => x.Data.Year == anno && x.Data <= trattamento.Data && x.IdVigneto == trattamento.IdVigneto)
                            .Max(y => (int?)y.IdBBCH_UltimaSettimana) ?? 0;
                    }

                    canopy = DB.Canopy.Where(x => x.IdBBCH == bbch).First();

                    trattamento.IdBBCH = bbch;
                    trattamento.IdCanopy = canopy.Id;

                    DB.Trattamento.Add(trattamento);
                }
                else
                {
                    canopy = DB.Canopy.Where(x => x.IdBBCH == trattamento.IdBBCH).First();
                }

                var coeffCanopy_OLD = canopy.GetCoeffPerc_OLD();
                var coeffCanopy_LWA = canopy.GetCoeffPerc_LWA(trattamento.Vigneto);
                var coeffCanopy_TRV = canopy.GetCoeffPerc_TRV(trattamento.Vigneto, MAX_AltezzaRif, MAX_LarghezzaRif);


                trattamento.Data = model.Data;
                trattamento.Note = model.Note;
                trattamento.IdTransitoNelVigneto = model.IdTransitoNelVigneto;
                trattamento.CoefficienteCanopy = model.CoefficienteCanopy;
                //trattamento.VolumeAcquaRif = model.VolumeAcquaRif;
                //trattamento.VolumeAcqua = trattamento.GetVolumeAcqua(coeffCanopy_OLD, coeffCanopy_LWA, coeffCanopy_TRV);

                //--- RIMUOVO I PRODOTTI ESCLUSI
                var trattamenti_avversita_da_rimuovere = trattamento.TrattamentoAvversita.Where(x => !model.Tab_NumRegProdottiVite.Contains(x.NumRegProdottoVite)).ToList();
                foreach (var trattamento_avversita_da_rimuovere in trattamenti_avversita_da_rimuovere)
                {
                    DB.TrattamentoAvversita.Remove(trattamento_avversita_da_rimuovere);
                }

                //--- AGGIUNGO O MODIFICO I PRODOTTI INSERITI MANUALMENTE
                if (model.Tab_NumRegProdottiVite != null)
                {
                    for (int i = 0; i < model.Tab_NumRegProdottiVite.Length; i++)
                    {
                        var numRegProdottoVite = model.Tab_NumRegProdottiVite[i];
                        var idAvversitaDaTrattare = model.Tab_IdAvversitaDaTrattare[i];
                        var dosaggio_rif = model.Tab_Dosaggi[i];

                        var trattamentoAvversita = trattamento.TrattamentoAvversita.FirstOrDefault(x => x.NumRegProdottoVite == numRegProdottoVite);

                        if (trattamentoAvversita == null)
                        {
                            trattamentoAvversita = new TrattamentoAvversita();
                            trattamentoAvversita.NumRegProdottoVite = numRegProdottoVite;

                            trattamento.TrattamentoAvversita.Add(trattamentoAvversita);
                        }

                        trattamentoAvversita.IdAvversitaDaTrattare = idAvversitaDaTrattare;
                        trattamentoAvversita.Dosaggio_Rif_Perleuve = dosaggio_rif;
                        trattamentoAvversita.Dosaggio_OLD = dosaggio_rif * coeffCanopy_OLD;
                        trattamentoAvversita.Dosaggio_LWA = dosaggio_rif * coeffCanopy_LWA;
                        trattamentoAvversita.Dosaggio_TRV = dosaggio_rif * coeffCanopy_TRV;
                        trattamentoAvversita.Dosaggio = trattamentoAvversita.GetDosaggio(trattamento.CoefficienteCanopy);

                    }
                }

                DB.SaveAndLog(trattamento);
            }

            //if (System.IO.File.Exists(Settings.Default.GeneraModelliProtezione_PathExe))
            //{
            //    //---- AVVIO PROGRAMMA GENERAZIONE MODELLI DI PROTEZIONE
            //    var exe = new ProcessStartInfo();
            //    exe.FileName = Settings.Default.GeneraModelliProtezione_PathExe;
            //    exe.WorkingDirectory = Path.GetDirectoryName(Settings.Default.GeneraModelliProtezione_PathExe);
            //    exe.Arguments = string.Join(";", trattamenti.Select(x => x.IdVigneto).ToList());
            //
            //    Process.Start(exe);
            //
            //    await Task.Delay(4000);
            //}

            FlashMessage(Tr.salvataggio_completato);

            if (id == null && model.Gruppo_IdEnte != null /*&& model.Gruppo_IdRischio != null*/)
            {
                return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri), new { idAzienda = model.Gruppo_IdEnte, IdRischio = model.Gruppo_IdRischio, IdTipologiaLinea = model.Gruppo_IdTipologiaLinea });
            }
            else if (id == null)
            {
                return RedirectToAction(nameof(Form), new { id = trattamenti[0].Id });
            }
            else
            {
                return RedirectToAction(nameof(Form), new { id });
            }
        }

        private Trattamento GetTrattamentoSuggetitoFromIdVigneto(int idVigneto)
        {
            var oggi = DateTime.Today;
            //var vigneto = DB.Vigneto.First(x => x.IdVigneto == idVigneto);

            var trattamenti_automatici = DB.Trattamento
                .Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.VignetoMaps).Include(x => x.BBCH).Include(x => x.Canopy).Include(x => x.TrattamentoAvversita).Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AvversitaDaTrattare")
                .Where(x => x.Automatico && x.IdVigneto == idVigneto && x.Data >= oggi).OrderBy(x => x.Data).ToList();

            /*var trattamento_suggerito = new Trattamento();

            trattamento_suggerito.Suggerito = true;
            trattamento_suggerito.IdVigneto = idVigneto;
            
            trattamento_suggerito.Data = trattamenti_automatici[0].Data;
            trattamento_suggerito.VolumeAcqua = trattamenti_automatici[0].VolumeAcqua;
            trattamento_suggerito.IdBBCH = trattamenti_automatici[0].IdBBCH;
            trattamento_suggerito.BBCH = trattamenti_automatici[0].BBCH;
            trattamento_suggerito.IdCanopy = trattamenti_automatici[0].IdCanopy;*/

            var trattamento_suggerito = trattamenti_automatici[0].CloneEntity();

            trattamento_suggerito.Automatico = false;
            trattamento_suggerito.Suggerito = true;
            trattamento_suggerito.IdUtenteCreazione = User.ID();
            trattamento_suggerito.DataCreazione = DateTime.Now;
            trattamento_suggerito.ModelloProtezioneGenerato = false;

            trattamento_suggerito.Vigneto = trattamenti_automatici[0].Vigneto;
            trattamento_suggerito.BBCH = trattamenti_automatici[0].BBCH;
            trattamento_suggerito.Canopy = trattamenti_automatici[0].Canopy;

            foreach (var trattamento_automatico in trattamenti_automatici.OrderBy(x => x.Data))
            {
                foreach (var trattamento_avversita_automatico in trattamento_automatico.TrattamentoAvversita)
                {
                    if (trattamento_suggerito.TrattamentoAvversita.Any(x => x.NumRegProdottoVite == trattamento_avversita_automatico.NumRegProdottoVite))
                    {
                        continue;
                    }

                    var trattamento_avversita_suggerito = trattamento_avversita_automatico.CloneEntity();

                    trattamento_avversita_suggerito.AvversitaDaTrattare = trattamento_avversita_automatico.AvversitaDaTrattare;
                    //trattamento_avversita_suggerito.ProdottoVite = trattamento_avversita_automatico.ProdottoVite;
                    trattamento_avversita_suggerito.AgrofarmacoUnivoco = trattamento_avversita_automatico.AgrofarmacoUnivoco;

                    trattamento_suggerito.TrattamentoAvversita.Add(trattamento_avversita_suggerito);
                }
            }

            return trattamento_suggerito;
        }

        [HttpGet]
        //[AuthorizePortal(Abilitazioni = new Abilitazione[] { Abilitazione.TRATTAMENTI, Abilitazione.TRATTAMENTI_AZIENDALI, Abilitazione.TRATTAMENTI_AUTOMATICI })]
        public ActionResult GeneraTrattamentoSuggerito(int idVigneto, int? idTrattamentoAutomatico)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            var model = GetTrattamentoSuggetitoFromIdVigneto(idVigneto);

            ViewBag.ElaborazioneModelliProtezioneInCorso = Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso;

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> GeneraTrattamentoSuggerito(Trattamento model, int? idTrattamentoAutomatico)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            if (Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso)
            {
                FlashMessageError(Tr.errore_aggiornamento_modelli_in_corso);
                return RedirectToAction(nameof(GeneraTrattamentoSuggerito), new { idVigneto = model.IdVigneto, idTrattamentoAutomatico }); ;
            }

            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            var MAX_AltezzaRif = DB.Canopy.Max(x => x.AltezzaRif);
            var MAX_LarghezzaRif = DB.Canopy.Max(x => x.LarghezzaRif);

            var trattamento_suggerito = GetTrattamentoSuggetitoFromIdVigneto(model.IdVigneto);

            var coeffCanopy_OLD = trattamento_suggerito.Canopy.GetCoeffPerc_OLD();
            var coeffCanopy_LWA = trattamento_suggerito.Canopy.GetCoeffPerc_LWA(trattamento_suggerito.Vigneto);
            var coeffCanopy_TRV = trattamento_suggerito.Canopy.GetCoeffPerc_TRV(trattamento_suggerito.Vigneto, MAX_AltezzaRif, MAX_LarghezzaRif);

            trattamento_suggerito.Data = model.Data;
            trattamento_suggerito.Note = model.Note;
            trattamento_suggerito.IdTransitoNelVigneto = model.IdTransitoNelVigneto;
            trattamento_suggerito.CoefficienteCanopy = model.CoefficienteCanopy;
            //trattamento_suggerito.VolumeAcquaRif = model.VolumeAcquaRif;
            //trattamento_suggerito.VolumeAcqua = trattamento_suggerito.GetVolumeAcqua(coeffCanopy_OLD, coeffCanopy_LWA, coeffCanopy_TRV);

            //--- RIMUOVO I PRODOTTI ESCLUSI
            trattamento_suggerito.TrattamentoAvversita = trattamento_suggerito.TrattamentoAvversita.Where(x => model.Tab_NumRegProdottiVite.Contains(x.NumRegProdottoVite)).ToList();

            //--- AGGIUNGO O MODIFICO I PRODOTTI INSERITI MANUALMENTE
            for (int i = 0; i < model.Tab_NumRegProdottiVite.Length; i++)
            {
                var numRegProdottoVite = model.Tab_NumRegProdottiVite[i];
                var idAvversitaDaTrattare = model.Tab_IdAvversitaDaTrattare[i];
                var dosaggio_rif = model.Tab_Dosaggi[i];

                var trattamentoAvversita = trattamento_suggerito.TrattamentoAvversita.FirstOrDefault(x => x.NumRegProdottoVite == numRegProdottoVite);

                if (trattamentoAvversita == null)
                {
                    trattamentoAvversita = new TrattamentoAvversita();
                    trattamentoAvversita.NumRegProdottoVite = numRegProdottoVite;

                    trattamento_suggerito.TrattamentoAvversita.Add(trattamentoAvversita);
                }

                trattamentoAvversita.IdAvversitaDaTrattare = idAvversitaDaTrattare;
                trattamentoAvversita.Dosaggio_Rif_Perleuve = dosaggio_rif;
                trattamentoAvversita.Dosaggio_OLD = dosaggio_rif * coeffCanopy_OLD;
                trattamentoAvversita.Dosaggio_LWA = dosaggio_rif * coeffCanopy_LWA;
                trattamentoAvversita.Dosaggio_TRV = dosaggio_rif * coeffCanopy_TRV;
                trattamentoAvversita.Dosaggio = trattamentoAvversita.GetDosaggio(trattamento_suggerito.CoefficienteCanopy);
            }

            DB.Trattamento.Add(trattamento_suggerito);
            DB.SaveAndLog(trattamento_suggerito);

            FlashMessage(Tr.salvataggio_completato);

            /*if (System.IO.File.Exists(Settings.Default.GeneraModelliProtezione_PathExe))
            {
                //---- AVVIO PROGRAMMA GENERAZIONE MODELLI DI PROTEZIONE
                var exe = new ProcessStartInfo();
                exe.FileName = Settings.Default.GeneraModelliProtezione_PathExe;
                exe.WorkingDirectory = Path.GetDirectoryName(Settings.Default.GeneraModelliProtezione_PathExe);
                exe.Arguments = trattamento_suggerito.IdVigneto.ToString();

                Process.Start(exe);

                await Task.Delay(4000);
            } */

            return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri), new { IdVigneto = trattamento_suggerito.IdVigneto });
        }

        [HttpGet]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
        public ActionResult GeneraTrattamentoEffettivo(int? idTrattamentoSuggerito)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            var trattamento_suggerito = GetTrattamentoFromId(idTrattamentoSuggerito);
            var model = GetTrattamentoEffettivoFromSuggerito(trattamento_suggerito);

            if (model.Gruppo_IdEnte != null /*&& model.Gruppo_IdRischio != null*/)
            {
                var id_vigneti = GetGruppoTrattamenti((int)model.Gruppo_IdEnte, model.Gruppo_IdRischio, model.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, model.Data, true, false).Select(x => x.IdVigneto).ToList();
                model.Gruppo_Vigneti = GetGruppoVigneti(id_vigneti, model.Data.Date);
            }

            ViewBag.ElaborazioneModelliProtezioneInCorso = Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso;

            return View(model);
        }

        [HttpPost]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
        public async Task<ActionResult> GeneraTrattamentoEffettivo(Trattamento model, int? idTrattamentoSuggerito)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            if (Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso)
            {
                FlashMessageError(Tr.errore_aggiornamento_modelli_in_corso);
                return RedirectToAction(nameof(GeneraTrattamentoSuggerito), new { idVigneto = model.IdVigneto, idTrattamentoSuggerito }); ;
            }

            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            var trattamento_suggerito_attuale = GetTrattamentoFromId(idTrattamentoSuggerito);

            if (!trattamento_suggerito_attuale.CanEdit)
            {
                throw new UnauthorizedAccessException();
            }

            List<Trattamento> trattamenti_suggeriti;
            if (trattamento_suggerito_attuale.Gruppo_IdEnte != null /*&& trattamento_suggerito_attuale.Gruppo_IdRischio != null*/)
            {
                trattamenti_suggeriti = GetGruppoTrattamenti((int)trattamento_suggerito_attuale.Gruppo_IdEnte, trattamento_suggerito_attuale.Gruppo_IdRischio, trattamento_suggerito_attuale.Gruppo_IdTipologiaLinea, model.Gruppo_Esclusi ?? false, trattamento_suggerito_attuale.Data, true, false);
            }
            else
            {
                trattamenti_suggeriti = new List<Trattamento> { trattamento_suggerito_attuale };
            }

            var MAX_AltezzaRif = DB.Canopy.Max(x => x.AltezzaRif);
            var MAX_LarghezzaRif = DB.Canopy.Max(x => x.LarghezzaRif);

            var trattamenti_effettivi = new List<Trattamento>();
            foreach (var trattamento_suggerito in trattamenti_suggeriti)
            {
                var trattamento_effettivo = GetTrattamentoEffettivoFromSuggerito(trattamento_suggerito);

                var coeffCanopy_OLD = trattamento_effettivo.Canopy.GetCoeffPerc_OLD();
                var coeffCanopy_LWA = trattamento_effettivo.Canopy.GetCoeffPerc_LWA(trattamento_effettivo.Vigneto);
                var coeffCanopy_TRV = trattamento_effettivo.Canopy.GetCoeffPerc_TRV(trattamento_effettivo.Vigneto, MAX_AltezzaRif, MAX_LarghezzaRif);

                trattamento_effettivo.Data = model.Data;
                trattamento_effettivo.Note = model.Note;
                trattamento_effettivo.IdTransitoNelVigneto = model.IdTransitoNelVigneto;
                trattamento_effettivo.CoefficienteCanopy = model.CoefficienteCanopy;
                //trattamento_effettivo.VolumeAcquaRif = model.VolumeAcquaRif;
                //trattamento_effettivo.VolumeAcqua = model.GetVolumeAcqua(coeffCanopy_OLD, coeffCanopy_LWA, coeffCanopy_TRV);



                //--- RIMUOVO I PRODOTTI ESCLUSI
                var trattamenti_avversita_da_rimuovere = trattamento_effettivo.TrattamentoAvversita.Where(x => !model.Tab_NumRegProdottiVite.Contains(x.NumRegProdottoVite)).ToList();
                foreach (var trattamento_avversita_da_rimuovere in trattamenti_avversita_da_rimuovere)
                {
                    trattamento_effettivo.TrattamentoAvversita.Remove(trattamento_avversita_da_rimuovere);
                }

                //--- AGGIUNGO O MODIFICO I PRODOTTI INSERITI MANUALMENTE
                for (int i = 0; i < model.Tab_NumRegProdottiVite.Length; i++)
                {
                    var numRegProdottoVite = model.Tab_NumRegProdottiVite[i];
                    var idAvversitaDaTrattare = model.Tab_IdAvversitaDaTrattare[i];
                    var dosaggio_rif = model.Tab_Dosaggi[i];

                    var trattamentoAvversita = trattamento_effettivo.TrattamentoAvversita.FirstOrDefault(x => x.NumRegProdottoVite == numRegProdottoVite);

                    if (trattamentoAvversita == null)
                    {
                        trattamentoAvversita = new TrattamentoAvversita();
                        trattamentoAvversita.NumRegProdottoVite = numRegProdottoVite;

                        trattamento_effettivo.TrattamentoAvversita.Add(trattamentoAvversita);
                    }

                    trattamentoAvversita.IdAvversitaDaTrattare = idAvversitaDaTrattare;
                    trattamentoAvversita.Dosaggio_Rif_Perleuve = dosaggio_rif;
                    trattamentoAvversita.Dosaggio_OLD = dosaggio_rif * coeffCanopy_OLD;
                    trattamentoAvversita.Dosaggio_LWA = dosaggio_rif * coeffCanopy_LWA;
                    trattamentoAvversita.Dosaggio_TRV = dosaggio_rif * coeffCanopy_TRV;
                    trattamentoAvversita.Dosaggio = trattamentoAvversita.GetDosaggio(trattamento_effettivo.CoefficienteCanopy);
                }

                trattamenti_effettivi.Add(trattamento_effettivo);

                //--- NASCONDO I TRATTAMENTI SUGGERITI
                trattamento_suggerito.Visibile = false;
            }

            foreach (var trattamento_effettivo in trattamenti_effettivi)
            {
                DB.Trattamento.Add(trattamento_effettivo);
                DB.SaveAndLog(trattamento_effettivo);
            }

            FlashMessage(Tr.salvataggio_completato);

            /*if (model.Gruppo_IdEnte != null && model.Gruppo_IdRischio != null)
            {
                return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri), new { idAzienda = model.Gruppo_IdEnte, IdRischio = model.Gruppo_IdRischio });
            }
            else
            {
                return RedirectToAction(nameof(Form), new { id = trattamenti_effettivi[0].Id });
            }  */

            return RedirectToAction(nameof(Form), new { id = trattamenti_effettivi[0].Id });
        }

        private Trattamento GetTrattamentoSuggetitoFromIdEnte(int idAzienda, int idRischio, int idTipologiaLinea)
        {
            var oggi = DateTime.Today;
            //var vigneto = DB.Vigneto.First(x => x.IdVigneto == idVigneto);

            var sql_trattamenti_automatici = DB.Trattamento
                .Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.Rischio).Include(x => x.Vigneto.VignetoMaps).Include(x => x.BBCH).Include(x => x.Canopy).Include(x => x.TrattamentoAvversita).Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.AvversitaDaTrattare")
                .Where(x => x.Automatico && x.Vigneto.Vigna.Ente_IdEnte == idAzienda && x.Vigneto.IdRischio == idRischio && x.Vigneto.IdTipologiaLinea == idTipologiaLinea && x.Data >= oggi);

            //if (idTipologiaLinea != null)
            //{
            //    sql_trattamenti_automatici = sql_trattamenti_automatici.Where(x => x.Vigneto.IdTipologiaLinea == idTipologiaLinea);
            //}

            var trattamenti_automatici = sql_trattamenti_automatici.OrderBy(x => x.Data).ToList();

            /*var trattamento_suggerito = new Trattamento();

            trattamento_suggerito.Suggerito = true;
            trattamento_suggerito.IdVigneto = idVigneto;
            
            trattamento_suggerito.Data = trattamenti_automatici[0].Data;
            trattamento_suggerito.VolumeAcqua = trattamenti_automatici[0].VolumeAcqua;
            trattamento_suggerito.IdBBCH = trattamenti_automatici[0].IdBBCH;
            trattamento_suggerito.BBCH = trattamenti_automatici[0].BBCH;
            trattamento_suggerito.IdCanopy = trattamenti_automatici[0].IdCanopy;*/

            if (trattamenti_automatici.Count == 0)
            {
                return null;
            }

            var trattamento_suggerito = trattamenti_automatici[0].CloneEntity();

            trattamento_suggerito.Id = 0;
            trattamento_suggerito.Automatico = false;
            trattamento_suggerito.Suggerito = true;
            trattamento_suggerito.IdUtenteCreazione = User.ID();
            trattamento_suggerito.DataCreazione = DateTime.Now;
            trattamento_suggerito.ModelloProtezioneGenerato = false;

            trattamento_suggerito.Vigneto = trattamenti_automatici[0].Vigneto;
            trattamento_suggerito.BBCH = trattamenti_automatici[0].BBCH;
            trattamento_suggerito.Canopy = trattamenti_automatici[0].Canopy;

            foreach (var trattamento_automatico in trattamenti_automatici.OrderBy(x => x.Data))
            {
                foreach (var trattamento_avversita_automatico in trattamento_automatico.TrattamentoAvversita)
                {
                    if (trattamento_suggerito.TrattamentoAvversita.Any(x => x.NumRegProdottoVite == trattamento_avversita_automatico.NumRegProdottoVite))
                    {
                        continue;
                    }

                    var trattamento_avversita_suggerito = trattamento_avversita_automatico.CloneEntity();
                    trattamento_avversita_suggerito.Id = 0;

                    trattamento_avversita_suggerito.AvversitaDaTrattare = trattamento_avversita_automatico.AvversitaDaTrattare;
                    //trattamento_avversita_suggerito.ProdottoVite = trattamento_avversita_automatico.ProdottoVite;
                    trattamento_avversita_suggerito.AgrofarmacoUnivoco = trattamento_avversita_automatico.AgrofarmacoUnivoco;

                    trattamento_suggerito.TrattamentoAvversita.Add(trattamento_avversita_suggerito);
                }
            }

            return trattamento_suggerito;
        }

        private Trattamento GetTrattamentoEffettivoFromSuggerito(Trattamento trattamento_suggerito)
        {
            var trattamento_effettivo = trattamento_suggerito.CloneEntity();

            trattamento_effettivo.Id = 0;
            trattamento_effettivo.Automatico = false;
            trattamento_effettivo.Suggerito = false;
            trattamento_effettivo.Effettivo = true;
            trattamento_effettivo.IdUtenteCreazione = User.ID();
            trattamento_effettivo.DataCreazione = DateTime.Now;
            trattamento_effettivo.ModelloProtezioneGenerato = false;
            trattamento_effettivo.GeneratoDa_IdTrattamento = trattamento_suggerito.Id;

            trattamento_effettivo.Vigneto = trattamento_suggerito.Vigneto;
            trattamento_effettivo.BBCH = trattamento_suggerito.BBCH;
            trattamento_effettivo.Canopy = trattamento_suggerito.Canopy;
            trattamento_effettivo.Ente = trattamento_suggerito.Ente;
            trattamento_effettivo.Rischio = trattamento_suggerito.Rischio;

            foreach (var trattamento_avversita_suggerito in trattamento_suggerito.TrattamentoAvversita)
            {
                var trattamento_avversita_effettivo = trattamento_avversita_suggerito.CloneEntity();
                trattamento_avversita_effettivo.Id = 0;

                trattamento_avversita_effettivo.AvversitaDaTrattare = trattamento_avversita_suggerito.AvversitaDaTrattare;
                //trattamento_avversita_effettivo.ProdottoVite = trattamento_avversita_suggerito.ProdottoVite;
                trattamento_avversita_effettivo.AgrofarmacoUnivoco = trattamento_avversita_suggerito.AgrofarmacoUnivoco;

                trattamento_effettivo.TrattamentoAvversita.Add(trattamento_avversita_effettivo);
            }

            return trattamento_effettivo;
        }

        [HttpGet]
        public ActionResult TrattaAzienda(int idAzienda, int idRischio, int idTipologiaLinea)
        {
            if (idAzienda <= 0 || idRischio <= 0 || idTipologiaLinea <= 0)
            {
                FlashMessageError(Tr.errore_selezionare_azienda_rischio_tipologialinea);
                return Redirect(Request.UrlReferrer.AbsoluteUri);
                return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri));
            }

            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            var model = GetTrattamentoSuggetitoFromIdEnte(idAzienda, idRischio, idTipologiaLinea);

            ViewBag.ElaborazioneModelliProtezioneInCorso = Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso;

            if (model == null)
            {
                FlashMessageError(Tr.errore_nessun_trattamento_automatico_azienda_selezionata);
                return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> TrattaAzienda(Trattamento model, int idAzienda, int idRischio, int idTipologiaLinea)
        {
            if (idAzienda <= 0 || idRischio <= 0 || idTipologiaLinea <= 0)
            {
                FlashMessageError(Tr.errore_selezionare_azienda_rischio_tipologialinea);
                return Redirect(Request.UrlReferrer.AbsoluteUri);
                return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri));
            }

            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            if (Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso)
            {
                FlashMessageError(Tr.errore_aggiornamento_modelli_in_corso);
                return RedirectToAction(nameof(GeneraTrattamentoSuggerito), new { idVigneto = model.IdVigneto }); ;
            }

            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            var MAX_AltezzaRif = DB.Canopy.Max(x => x.AltezzaRif);
            var MAX_LarghezzaRif = DB.Canopy.Max(x => x.LarghezzaRif);

            var oggi = DateTime.Today;

            var ids_vigneti = DB.Trattamento.Where(x => x.Automatico && x.Vigneto.Vigna.Ente_IdEnte == idAzienda && x.Vigneto.IdRischio == idRischio && x.Vigneto.IdTipologiaLinea == idTipologiaLinea && x.Data >= oggi).Select(x => x.IdVigneto).Distinct().ToList();

            var trattamento_suggerito = GetTrattamentoSuggetitoFromIdEnte(idAzienda, idRischio, idTipologiaLinea);

            var coeffCanopy_OLD = trattamento_suggerito.Canopy.GetCoeffPerc_OLD();
            var coeffCanopy_LWA = trattamento_suggerito.Canopy.GetCoeffPerc_LWA(trattamento_suggerito.Vigneto);
            var coeffCanopy_TRV = trattamento_suggerito.Canopy.GetCoeffPerc_TRV(trattamento_suggerito.Vigneto, MAX_AltezzaRif, MAX_LarghezzaRif);

            trattamento_suggerito.Data = model.Data;
            trattamento_suggerito.Note = model.Note;
            trattamento_suggerito.IdTransitoNelVigneto = model.IdTransitoNelVigneto;
            trattamento_suggerito.CoefficienteCanopy = model.CoefficienteCanopy;
            //trattamento_suggerito.VolumeAcquaRif = model.VolumeAcquaRif;
            //trattamento_suggerito.VolumeAcqua = model.GetVolumeAcqua(coeffCanopy_OLD, coeffCanopy_LWA, coeffCanopy_TRV);

            //--- RIMUOVO I PRODOTTI ESCLUSI
            trattamento_suggerito.TrattamentoAvversita = trattamento_suggerito.TrattamentoAvversita.Where(x => model.Tab_NumRegProdottiVite.Contains(x.NumRegProdottoVite)).ToList();

            //--- AGGIUNGO O MODIFICO I PRODOTTI INSERITI MANUALMENTE
            for (int i = 0; i < model.Tab_NumRegProdottiVite.Length; i++)
            {
                var numRegProdottoVite = model.Tab_NumRegProdottiVite[i];
                var idAvversitaDaTrattare = model.Tab_IdAvversitaDaTrattare[i];
                var dosaggio_rif = model.Tab_Dosaggi[i];

                var trattamentoAvversita = trattamento_suggerito.TrattamentoAvversita.FirstOrDefault(x => x.NumRegProdottoVite == numRegProdottoVite);

                if (trattamentoAvversita == null)
                {
                    trattamentoAvversita = new TrattamentoAvversita();
                    trattamentoAvversita.NumRegProdottoVite = numRegProdottoVite;

                    trattamento_suggerito.TrattamentoAvversita.Add(trattamentoAvversita);
                }

                trattamentoAvversita.IdAvversitaDaTrattare = idAvversitaDaTrattare;
                trattamentoAvversita.Dosaggio_Rif_Perleuve = dosaggio_rif;
                trattamentoAvversita.Dosaggio_OLD = dosaggio_rif * coeffCanopy_OLD;
                trattamentoAvversita.Dosaggio_LWA = dosaggio_rif * coeffCanopy_LWA;
                trattamentoAvversita.Dosaggio_TRV = dosaggio_rif * coeffCanopy_TRV;
                trattamentoAvversita.Dosaggio = trattamentoAvversita.GetDosaggio(trattamento_suggerito.CoefficienteCanopy);
            }

            foreach (var id_vigneto in ids_vigneti)
            {
                var trattamento = trattamento_suggerito.CloneEntity();
                trattamento.IdVigneto = id_vigneto;
                trattamento.Gruppo_IdEnte = idAzienda;
                trattamento.Gruppo_IdRischio = idRischio;
                trattamento.Gruppo_IdTipologiaLinea = idTipologiaLinea;
                trattamento.Gruppo_Esclusi = false;

                foreach (var trattamento_avversita_suggerita in trattamento_suggerito.TrattamentoAvversita)
                {
                    var trattamento_avversita = trattamento_avversita_suggerita.CloneEntity();

                    trattamento.TrattamentoAvversita.Add(trattamento_avversita);
                }

                DB.Trattamento.Add(trattamento);
                DB.SaveAndLog(trattamento);
            }

            FlashMessage(Tr.salvataggio_completato);

            //if (System.IO.File.Exists(Settings.Default.GeneraModelliProtezione_PathExe))
            //{
            //    //---- AVVIO PROGRAMMA GENERAZIONE MODELLI DI PROTEZIONE
            //    var exe = new ProcessStartInfo();
            //    exe.FileName = Settings.Default.GeneraModelliProtezione_PathExe;
            //    exe.WorkingDirectory = Path.GetDirectoryName(Settings.Default.GeneraModelliProtezione_PathExe);
            //    exe.Arguments = string.Join(";", ids_vigneti);

            //    Process.Start(exe);

            //    await Task.Delay(4000);
            //}

            return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri), new { IdAzienda = trattamento_suggerito.Vigneto.Vigna.Ente_IdEnte });
        }

        [HttpPost]
        public async Task<ActionResult> GeneraModelliProtezione()
        {
            if (!System.IO.File.Exists(Settings.Default.GeneraModelliProtezione_PathExe))
            {
                FlashMessageError("Eseguibile per generazione modelli protezione non trovato");
                return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri));
            }

            var ids_vigneti = GetIdsVigneti_ModelliProtezioneDaGenerare(User.GetUtente());

            //---- AVVIO PROGRAMMA GENERAZIONE MODELLI DI PROTEZIONE
            var exe = new ProcessStartInfo();
            exe.FileName = Settings.Default.GeneraModelliProtezione_PathExe;
            exe.WorkingDirectory = Path.GetDirectoryName(Settings.Default.GeneraModelliProtezione_PathExe);
            exe.Arguments = ids_vigneti.Join(";");

            Process.Start(exe);

            await Task.Delay(4000);

            return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri));
        }

        private List<int> GetIdsVigneti_ModelliProtezioneDaGenerare(Utente utente)
        {
            var ids_vigneti = DB.Trattamento
                .Where(x => x.ModelloProtezioneGenerato == false && x.Vigneto.VignetoMaps != null && x.Vigneto.VignetoMaps.AdministrativeAreaLevel3 != null)
                .Select(x => x.IdVigneto).Distinct().ToList();

            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                ids_vigneti = ids_vigneti.Intersect(ids_vigneti_utente).ToList();
            }

            return ids_vigneti;
        }

        [HttpPost]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
        public ActionResult Elimina(int id)
        {
            var utente = User.GetUtente();

            var trattamento = DB.Trattamento.Find(id);

            if (!utente.IsMasterUser)
            {
                if (!DB.VistaUtentiVigneti.Any(x => x.idUtente == utente.idUtente && x.IdVigneto == trattamento.IdVigneto))
                {
                    throw new UnauthorizedAccessException();
                }
            }

            List<Trattamento> trattamenti_da_eliminare;
            if (trattamento.Gruppo_IdEnte != null /*&& trattamento.Gruppo_IdRischio != null*/)
            {
                trattamenti_da_eliminare = DB.Trattamento.Where(x => x.Gruppo_IdEnte == trattamento.Gruppo_IdEnte && x.Gruppo_IdRischio == trattamento.Gruppo_IdRischio && x.Gruppo_Esclusi == trattamento.Gruppo_Esclusi && x.Data == trattamento.Data).ToList();
            }
            else
            {
                trattamenti_da_eliminare = new List<Trattamento> { trattamento };
            }

            foreach (var trattamento_da_eliminare in trattamenti_da_eliminare)
            {
                DB.Trattamento.Remove(trattamento_da_eliminare);
                DB.SaveAndLog(trattamento_da_eliminare);
            }

            if (trattamento.Automatico)
            {
                return RedirectToAction(nameof(TrattamentiAutomaticiGiornalieri));
            }
            else
            {
                return RedirectToAction(nameof(TrattamentiAziendaliGiornalieri));
            }

        }

        [HttpGet]
        public ActionResult Automatici(ListaTrattamentiModel model)
        {
            return View(model);
        }

        [HttpGet]
        public ActionResult AutomaticiAjax(ListaTrattamentiModel model)
        {
            try
            {
                var utente = User.GetUtente();

                var datatableReturn = new jQueryDataTableReturn();

                var db_items = DB.Trattamento.Include(x => x.Vigneto.Vigna.Ente).Include(x => x.Vigneto.VignetoMaps).Include(x => x.TrattamentoAvversita)
                    .Where(x => x.Automatico).AsQueryable();

                // Il Totale record senza filtri é:
                datatableReturn.iTotalRecords = db_items.Count();

                if (!string.IsNullOrEmpty(model.sSearch) && model.sSearch.Length >= 2)
                {
                    var search = model.sSearch.ToLower();
                    var split = search.Split(' ').Where(x => x != "").Select(x => x.ToLower()).ToList();

                    db_items = db_items.Where(x => split.All(y => (x.IdVigneto + " " + x.Vigneto.Vigna.Ente.RagioneSociale + " " + x.Vigneto.Vigna.NomeVigna + " " + x.Vigneto.NomeVigneto + " " + x.Vigneto.VignetoMaps.Area3).Contains(y)));

                    datatableReturn.iTotalDisplayRecords = db_items.Count();
                }
                else
                {
                    datatableReturn.iTotalDisplayRecords = datatableReturn.iTotalRecords;
                }

                //---- SORT                 
                switch (model.iSortCol_0)
                {
                    default:
                    case 0: db_items = db_items.OrderBy(x => x.Vigneto.Vigna.Ente.RagioneSociale, model.sSortDir_0); break;
                    case 1: db_items = db_items.OrderBy(x => x.Vigneto.Vigna.NomeVigna, model.sSortDir_0); break;
                    case 2: db_items = db_items.OrderBy(x => x.Vigneto.NomeVigneto, model.sSortDir_0); break;
                    case 3: db_items = db_items.OrderBy(x => x.Data, model.sSortDir_0); break;
                        //case 3: db_items = db_items.OrderBy(x => x.Vigneto.VignetoMaps.AdministrativeAreaLevel1, model.sSortDir_0); break;                    
                }

                if (datatableReturn.iTotalDisplayRecords < model.iDisplayStart)
                    model.iDisplayStart = 0;

                db_items = db_items.Skip(model.iDisplayStart).Take(model.iDisplayLength);

                var items = db_items.AsNoTracking().ToList();

                // Costruzione del ResultSet dei soli campi da restituire alla Jquery DataTable            
                string[][] jaggedArray = new string[items.Count][];
                int conta = 0;
                foreach (var item in items)
                {
                    //string imageBase64 = Convert.ToBase64String(c.FotoFronte);                               

                    string[] Riga = new string[6];

                    Riga[0] = item.Vigneto.Vigna.Ente.RagioneSociale.ToColumn();
                    Riga[1] = item.Vigneto.Vigna.NomeVigna.ToColumn();
                    Riga[2] = item.Vigneto.NomeVigneto.ToColumn();
                    Riga[3] = item.Data.ToColumn();
                    Riga[4] = string.Join(" - ", item.TrattamentoAvversita.Select(x => x.AvversitaDaTrattare.DescrizioneInLingua(utente.Lingua)).ToArray()).ToColumn();

                    //--- LAST ROW AS ID
                    Riga[5] = new GridButtons(Url)
                                    .Edit("Form", null, new { item.Id, daLista = true }, "Dettaglio")
                                    .CreateButtons();

                    jaggedArray[conta] = Riga;
                    conta = conta + 1;
                }


                datatableReturn.sEcho = model.sEcho;
                datatableReturn.aaData = jaggedArray;

                // Restituzione del JSON alla pagina chiamante
                return Json(datatableReturn, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        public ActionResult TrattamentiAutomaticiGiornalieri(TrattamentiAutomaticiGiornalieriModel model)
        {
            //--- RICARICO IMPOSTAZIONI PER ElaboraModelliProtezioneInCorso 
            Utils.RicaricaImpostazioni();

            var utente = User.GetUtente();

            //var data = model.Data != null ? DateTime.Parse(model.Data) : DateTime.Today;
            var data = DateTime.Today;

            //---- FILTRI DA / IN SESSIONE
            model.IdRegione = FiltroDaSessione(model.IdRegione, nameof(model.IdRegione));
            model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));
            model.IdTipologiaLinea = FiltroDaSessione(model.IdTipologiaLinea, nameof(model.IdTipologiaLinea));
            model.IdVigneto = FiltroDaSessione(model.IdVigneto, nameof(model.IdVigneto));
            model.IdVitigno = FiltroDaSessione(model.IdVitigno, nameof(model.IdVitigno));
            model.IdRischio = FiltroDaSessione(model.IdRischio, nameof(model.IdRischio));

            //var oggi = DateTime.Today;
            var da_giorno = data.StartOfDay();
            var a_giorno = data.AddDays(3).EndOfDay();

            model.DaGiorno = da_giorno;
            model.AGiorno = a_giorno;

            //--- 10/04/2025 - RIMOSSO FILTRO SU IdTipologiaLinea
            //--- 14/04/2025 - RIMESSO FILTRO SU IdTipologiaLinea
            var sql_vigneti = DB.Vigneto.Include(x => x.VignetoMaps).Include(x => x.Vigna.Ente).Include(x => x.Vitigno).Include(x => x.Rischio).Include(x => x.TipologiaLinea)
                .Where(x => x.VignetoMaps != null && x.IdTipologiaLinea != null && x.VignetoMaps.AdministrativeAreaLevel3 != null);

            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);
                sql_vigneti = sql_vigneti.Where(x => ids_vigneti_utente.Contains(x.IdVigneto));
            }

            var dati_filtrati = false;

            if (model.IdRegione != null && model.IdRegione != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area1 == model.IdRegione);
                dati_filtrati |= true;
            }

            if (model.IdProvincia != null && model.IdProvincia != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area2 == model.IdProvincia);
                dati_filtrati |= true;
            }

            if (model.IdComune != null && model.IdComune != "-")
            {
                sql_vigneti = sql_vigneti.Where(x => x.VignetoMaps.Area3 == model.IdComune);
                dati_filtrati |= true;
            }

            if (model.IdAzienda != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda);
                dati_filtrati |= true;
            }

            if (model.IdGruppo != 0)
            {
                var id_enti_da_filtro = DB.VistaGruppo_Azienda.Where(x => x.IdGruppo == model.IdGruppo).Select(x => x.IdEnte).ToList();
                sql_vigneti = sql_vigneti.Where(x => id_enti_da_filtro.Contains(x.Vigna.Ente_IdEnte));
                dati_filtrati |= true;
            }

            if (model.IdTipologiaLinea != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdTipologiaLinea == model.IdTipologiaLinea);
                dati_filtrati |= true;
            }

            if (model.IdVigneto != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdVigneto == model.IdVigneto);
                dati_filtrati |= true;
            }

            if (model.IdVitigno != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.Vitigno_idVitigno == model.IdVitigno);
                dati_filtrati |= true;
            }

            if (model.IdRischio != 0)
            {
                sql_vigneti = sql_vigneti.Where(x => x.IdRischio == model.IdRischio);
                dati_filtrati |= true;
            }

            List<Vigneto> vigneti = sql_vigneti.ToList();

            var ids_vigneti = vigneti.Select(x => x.IdVigneto).ToList();

            var trattamenti = DB.Trattamento.Include("TrattamentoAvversita.AvversitaDaTrattare").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.LineaDifesa")
                .Where(x => x.Data >= data && ids_vigneti.Contains(x.IdVigneto) && x.Visibile == true)
                .ToList();

            /*if (dati_filtrati)
            {
                //---- VISUALIZZO TUTTI I VIGNETI ANCHE QUELLI CHE NON HANNO I TRATTAMENTI
                vigneti = sql_vigneti.ToList();
            }
            else
            {
                //---- SE NON FILTRO VEDO SOLO I VIGNETI CHE HANNO ALMENO UN TRATTAMENTO
                var ids_vigneti_trattamenti = trattamenti_automatici.Select(x => x.IdVigneto).ToList();
                vigneti = sql_vigneti.Where(x=> ids_vigneti_trattamenti.Contains(x.IdVigneto)).ToList();
            }  */

            var filtro_regioni = vigneti.Select(x => new { Id = x.VignetoMaps.Area1, Nome = x.VignetoMaps.AdministrativeAreaLevel1 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_province = vigneti.Select(x => new { Id = x.VignetoMaps.Area2, Nome = x.VignetoMaps.AdministrativeAreaLevel2 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_comuni = vigneti.Select(x => new { Id = x.VignetoMaps.Area3, Nome = x.VignetoMaps.AdministrativeAreaLevel3 }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_aziende = vigneti.Where(x => x.Vigna?.Ente != null).Select(x => new { Id = x.Vigna.Ente.IdEnte, Nome = x.Vigna.Ente.RagioneSociale }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_tipologialinea = vigneti.Where(x => x.TipologiaLinea != null).Select(x => new { Id = x.TipologiaLinea.Id, Nome = x.TipologiaLinea.Descrizione }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_vigneti = vigneti.Select(x => new { Id = x.IdVigneto, Nome = x.NomeVigneto }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_vitigni = vigneti.Where(x => x.Vitigno != null).Select(x => new { Id = x.Vitigno.idVitigno, Nome = x.Vitigno.NomeVitigno }).Distinct().ToList().OrderBy(x => x.Nome).ToList();
            var filtro_rischi = vigneti.Where(x => x.Rischio != null).Select(x => new { Id = x.Rischio.Id, Nome = x.Rischio.DescrizioneInLingua(utente.Lingua) }).Distinct().ToList().OrderBy(x => x.Id).ToList();

            var id_enti_per_filtro = filtro_aziende.Select(x => x.Id).ToList();
            var filtro_gruppi = DB.VistaGruppo_Azienda.Where(x => id_enti_per_filtro.Contains(x.IdEnte)).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().ToList().OrderBy(x => x.Nome).ToList();

            //--------------- CARICO TABELLA VIGNETI            
            foreach (var vigneto in vigneti)
            {
                var trattamenti_vigneto = trattamenti.Where(x => x.IdVigneto == vigneto.IdVigneto).ToList();

                var riga = new RigaTrattamentiAutomaticiGiornalieri()
                {
                    Id = vigneto.IdVigneto,
                    Nome = vigneto.NomeVigneto,
                };

                for (int i = 0; i <= 4; i++)
                {
                    var da_data_giorno = data.AddDays(i).StartOfDay();
                    var a_data_giorno = data.AddDays(i).EndOfDay();

                    var trattamenti_giorno = trattamenti_vigneto.Where(x => x.Data >= da_data_giorno && x.Data <= a_data_giorno).ToList();
                    //var avversita_da_trattare = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.AvversitaDaTrattare).Distinct().ToList();
                    //var prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.ProdottoVite).Distinct().ToList();

                    var pulsanti = new List<string>();

                    foreach (var trattamento in trattamenti_giorno)
                    {
                        var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                        var text = string.Join(" | ", trattamento.TrattamentoAvversita.Select(x => x.AvversitaDaTrattare.DescrizioneInLingua(utente.Lingua)).Distinct().ToList());
                        var tooltip = $"{Tr.alle}: {trattamento.Data.ToString("HH:mm")}<br><br>";
                        tooltip += string.Join("<br>", trattamento.TrattamentoAvversita.Select(x => x.AgrofarmacoUnivoco.NOME_COMMERCIALE + (x.LineaDifesa != null ? $" [{x.LineaDifesa.Dosaggio_Rif_Perleuve}]" : "")).Distinct().ToList());


                        var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-warning" : trattamento.Effettivo ? "btn-success" : null;

                        pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                    }

                    //var tootip_prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => $"{x.AgrofarmacoUnivoco.NOME_COMMERCIALE} [{x.ProdottoVite.NumReg}]").ToList();

                    //var pulsanti_trattamenti = avversita_da_trattare.Select(x => x.Descrizione.EscapeHtmlAttr()).ToList();                    


                    riga.Trattamenti[i] = trattamenti_giorno;
                    riga.Colore[i] = trattamenti_giorno.Count > 0 ? "red" : null;
                    riga.Testo[i] = trattamenti_giorno.Count > 0 ? string.Join(" ", pulsanti) : null;
                    //riga.Tooltips[i] = string.Join("<br>", tootip_prodotti);
                }

                model.RigheVigneti.Add(riga);
            }

            //--------------- CARICO TABELLA ENTI
            var enti = vigneti.Select(x => x.Vigna.Ente).Distinct().ToList();
            foreach (var ente in enti)
            {
                var ids_vigneti_ente = vigneti.Where(x => x.Vigna.Ente_IdEnte == ente.IdEnte).Select(x => x.IdVigneto).ToList();
                var trattamenti_ente = trattamenti.Where(x => ids_vigneti_ente.Contains(x.IdVigneto)).ToList();

                var riga = new RigaTrattamentiAutomaticiGiornalieri()
                {
                    Id = ente.IdEnte,
                    Nome = ente.RagioneSociale,
                };

                for (int i = 0; i <= 4; i++)
                {
                    var da_data_giorno = data.AddDays(i).StartOfDay();
                    var a_data_giorno = data.AddDays(i).EndOfDay();

                    var trattamenti_giorno = trattamenti_ente.Where(x => x.Data >= da_data_giorno && x.Data <= a_data_giorno).ToList();
                    //var avversita_da_trattare = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.AvversitaDaTrattare).Distinct().ToList();
                    //var prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.ProdottoVite).Distinct().ToList();
                    trattamenti_giorno = trattamenti_giorno.Distinct(new TrattamentoSimileComparer()).ToList();

                    var pulsanti = new List<string>();

                    foreach (var trattamento in trattamenti_giorno)
                    {
                        var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                        var text = string.Join(" | ", trattamento.TrattamentoAvversita.Select(x => x.AvversitaDaTrattare.DescrizioneInLingua(utente.Lingua)).Distinct().ToList());
                        var tooltip = $"{Tr.alle}: {trattamento.Data.ToString("HH:mm")}<br><br>";
                        tooltip += string.Join("<br>", trattamento.TrattamentoAvversita.Select(x => x.AgrofarmacoUnivoco.NOME_COMMERCIALE + (x.LineaDifesa != null ? $" [{x.LineaDifesa.Dosaggio_Rif_Perleuve}]" : "")).Distinct().ToList());


                        var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-warning" : trattamento.Effettivo ? "btn-success" : null;

                        pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
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
            filtro_tipologialinea.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vigneti.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vitigni.Insert(0, new { Id = 0, Nome = "-" });
            filtro_rischi.Insert(0, new { Id = 0, Nome = "-" });

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
            model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);
            model.TipologieLinee = new SelectList(filtro_tipologialinea, "Id", "Nome", model.IdTipologiaLinea);
            model.Vigneti = new SelectList(filtro_vigneti, "Id", "Nome", model.IdVigneto);
            model.Vitigni = new SelectList(filtro_vitigni, "Id", "Nome", model.IdVitigno);
            model.Rischi = new SelectList(filtro_rischi, "Id", "Nome", model.IdRischio);

            model.NumModelliProtezioneDaGenerare = GetIdsVigneti_ModelliProtezioneDaGenerare(utente).Count;

            return View(model);
        }

        [HttpGet]
        //[AuthorizePortal(IsMasterUser = true, IsUtentePerleuve = true)]
        //----- PAGINA VISIBILE A TUTTI GLI UTENTI!
        public ActionResult TrattamentiAziendaliGiornalieri(TrattamentiAziendaliGiornalieriModel model)
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
            //model.IdProvincia = FiltroDaSessione(model.IdProvincia, nameof(model.IdProvincia));
            model.IdComune = FiltroDaSessione(model.IdComune, nameof(model.IdComune));
            model.IdAzienda = FiltroDaSessione(model.IdAzienda, nameof(model.IdAzienda));
            model.IdTipologiaLinea = FiltroDaSessione(model.IdTipologiaLinea, nameof(model.IdTipologiaLinea));
            model.IdVigneto = FiltroDaSessione(model.IdVigneto, nameof(model.IdVigneto));
            model.IdVitigno = FiltroDaSessione(model.IdVitigno, nameof(model.IdVitigno));
            model.IdRischio = FiltroDaSessione(model.IdRischio, nameof(model.IdRischio));
            model.IdConsulente = FiltroDaSessione(model.IdConsulente, nameof(model.IdConsulente));

            //var oggi = DateTime.Today;
            var da_giorno = model.Data.AddDays(-3).StartOfDay();
            var a_giorno = model.Data.AddDays(+3).EndOfDay();

            model.DaGiorno = da_giorno;
            model.AGiorno = a_giorno;

            //var vvv = DB.Vigneto.Include(x => x.VignetoMaps).Include(x => x.Vigna.Ente).Include(x => x.Vitigno).Include(x => x.Rischio).Include(x => x.TipologiaLinea)
            //    .Where(x => x.Vigna.Ente_IdEnte == model.IdAzienda && x.VignetoMaps != null && x.VignetoMaps.AdministrativeAreaLevel3 != null)
            //    //.Where(x => !x.Estirpo)
            //    .ToList();

            //--- 10/04/2025 - RIMOSSO FILTRO SU IdRischio
            //--- 14/04/2025 - RIMESSO FILTRO SU IdTipologiaLinea
            var sql_vigneti = DB.Vigneto.Include(x => x.VignetoMaps).Include(x => x.Vigna.Ente).Include(x => x.Vitigno).Include(x => x.Rischio).Include(x => x.TipologiaLinea)
            .Where(x => x.Carico && !x.Estirpo && x.IdTipologiaLinea != null);
            //.Where(x => x.VignetoMaps != null /* && x.IdTipologiaLinea != null && x.IdRischio != null*/ && x.VignetoMaps.AdministrativeAreaLevel3 != null && !x.Estirpo);

            if (!utente.IsMasterUser)
            {
                var ids_vigneti_utente = DB.VistaUtentiVigneti.Where(x => x.idUtente == utente.idUtente).Select(x => x.IdVigneto).ToList();
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
                var id_enti_da_filtro = DB.VistaGruppo_Azienda.Where(x => x.IdGruppo == model.IdGruppo).Select(x => x.IdEnte).Distinct().ToList();
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

            if (utente.IsMasterUser)
            {
                if (model.IdConsulente != 0)
                {
                    var ids_vigneti_consulente = DB.VistaConsulentiVigneti.Where(x => x.idUtente == model.IdConsulente).Select(x => x.IdVigneto).ToList();
                    sql_vigneti = sql_vigneti.Where(x => ids_vigneti_consulente.Contains(x.IdVigneto));
                }
            }

            List<Vigneto> vigneti = sql_vigneti.ToList();

            var ids_vigneti = vigneti.Select(x => x.IdVigneto).ToList();

            var sql_trattamenti = DB.Trattamento.Include("TrattamentoAvversita.AvversitaDaTrattare").Include("TrattamentoAvversita.AgrofarmacoUnivoco").Include("TrattamentoAvversita.LineaDifesa").Include(x => x.TipoTrattamento)
                .Where(x => x.Data >= da_giorno && ids_vigneti.Contains(x.IdVigneto) && (x.Suggerito || x.Effettivo) && x.Visibile == true);

            if (model.IdTipoTrattamento != 0)
            {
                sql_trattamenti = sql_trattamenti.Where(x => x.IdTipoTrattamento == model.IdTipoTrattamento);
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

            var filtro_tipitrattamento = trattamenti.Where(x => x.TipoTrattamento != null).Select(x => new { Id = x.TipoTrattamento.IdTipoTrattamento, Nome = x.TipoTrattamento.Descrizione }).Distinct().ToList().OrderBy(x => x.Id).ToList();

            var id_enti_per_filtro = filtro_aziende.Select(x => x.Id).ToList();
            var filtro_gruppi = DB.VistaGruppo_Azienda.Where(x => id_enti_per_filtro.Contains(x.IdEnte)).Select(x => new { Id = x.IdGruppo, Nome = x.NomeGruppo }).Distinct().ToList().OrderBy(x => x.Nome).ToList();

            var id_consulenti = DB.VistaConsulentiVigneti.Select(x => x.idUtente).Distinct().ToList();
            var filtro_consulenti = DB.Utente.Where(x => id_consulenti.Contains(x.idUtente)).Select(x => new { Id = x.idUtente, Nome = x.Nome + " " + x.Cognome }).ToList().OrderBy(x => x.Nome).ToList();

            var ids_trattamenti_suggeriti_da_nascondere = trattamenti.Select(x => x.GeneratoDa_IdTrattamento).Where(x => x != null).Distinct().ToList();

            trattamenti = trattamenti.Where(x => !ids_trattamenti_suggeriti_da_nascondere.Contains(x.Id)).ToList();


            //--------------- CARICO TABELLE VIGNETI            
            foreach (var vigneto in vigneti)
            {
                var trattamenti_vigneto = trattamenti.Where(x => x.IdVigneto == vigneto.IdVigneto).ToList();

                var riga = new RigaTrattamentiAziendaliGiornalieri()
                {
                    Id = vigneto.IdVigneto,
                    Nome = vigneto.NomeVigneto,
                };

                for (int i = 0; i <= 3 + 4; i++)
                {
                    var da_data_giorno = da_giorno.AddDays(i).StartOfDay();
                    var a_data_giorno = da_giorno.AddDays(i).EndOfDay();

                    var trattamenti_giorno = trattamenti_vigneto.Where(x => x.Data >= da_data_giorno && x.Data <= a_data_giorno).ToList();
                    //var avversita_da_trattare = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.AvversitaDaTrattare).Distinct().ToList();
                    //var prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => x.ProdottoVite).Distinct().ToList();

                    var pulsanti = new List<string>();

                    foreach (var trattamento in trattamenti_giorno)
                    {
                        if (trattamento.IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO)
                        {

                            if (trattamento.Gruppo_Esclusi == true)
                            {
                                var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                                var text = Tr.esclusi.ToUpper();
                                var tooltip = Tr.esclusi.ToUpper();
                                var colore = "btn-dark";

                                //pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                                pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                            }
                            else
                            {

                                var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                                var text = string.Join(" | ", trattamento.TrattamentoAvversita.Select(x => x.AvversitaDaTrattare.DescrizioneInLingua(utente.Lingua)).Distinct().ToList());
                                var tooltip = $"{Tr.alle}: {trattamento.Data.ToString("HH:mm")}<br><br>";
                                tooltip += string.Join("<br>", trattamento.TrattamentoAvversita.Select(x => x.AgrofarmacoUnivoco.NOME_COMMERCIALE + (x.LineaDifesa != null ? $" [{x.LineaDifesa?.Dosaggio_Rif_Perleuve}]" : "")).Distinct().ToList());

                                var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-warning" : trattamento.Effettivo ? "btn-success" : null;

                                pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                            }
                        }
                        else if (trattamento.IdTipoTrattamento == (int)TipoTrattamento.Enum.ALTRE_AVVERSITA)
                        {
                            var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                            var text = string.Join(" | ", trattamento.TrattamentoAvversita.Select(x => x.AvversitaDaTrattare.DescrizioneInLingua(utente.Lingua)).Distinct().ToList());
                            var tooltip = $"{Tr.alle}: {trattamento.Data.ToString("HH:mm")}<br><br>";
                            tooltip += string.Join("<br>", trattamento.TrattamentoAvversita.Select(x => x.AgrofarmacoUnivoco.NOME_COMMERCIALE + (x.LineaDifesa != null ? $" [{x.LineaDifesa?.Dosaggio_Rif_Perleuve}]" : "")).Distinct().ToList());

                            var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-info" : trattamento.Effettivo ? "btn-success" : null;

                            pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                        }


                    }

                    //var tootip_prodotti = trattamenti_giorno.SelectMany(x => x.TrattamentoAvversita).Select(x => $"{x.AgrofarmacoUnivoco.NOME_COMMERCIALE} [{x.ProdottoVite.NumReg}]").ToList();

                    //var pulsanti_trattamenti = avversita_da_trattare.Select(x => x.Descrizione.EscapeHtmlAttr()).ToList();                    


                    riga.Trattamenti[i] = trattamenti_giorno;
                    riga.Colore[i] = trattamenti_giorno.Count > 0 ? "red" : null;
                    riga.Testo[i] = trattamenti_giorno.Count > 0 ? string.Join(" ", pulsanti) : null;
                    //riga.Tooltips[i] = string.Join("<br>", tootip_prodotti);
                }

                model.RigheVigneti.Add(riga);
            }

            //--------------- CARICO TABELLA ENTI
            var enti = vigneti.Select(x => x.Vigna.Ente).Distinct().ToList();
            foreach (var ente in enti)
            {
                var vigneti_ente = vigneti.Where(x => x.Vigna.Ente_IdEnte == ente.IdEnte).ToList();
                var ids_vigneti_ente = vigneti_ente.Select(x => x.IdVigneto).ToList();

                var trattamenti_ente = trattamenti.Where(x => ids_vigneti_ente.Contains(x.IdVigneto)).ToList();
                var ids_vigneti_trattamenti = trattamenti_ente.Select(x => x.IdVigneto).ToList();

                //var tipologie_linee_ente = vigneti_ente.Where(x => ids_vigneti_trattamenti.Contains(x.IdVigneto)).Select(x => x.TipologiaLinea).Distinct().ToList();

                var riga = new RigaTrattamentiAziendaliGiornalieri()
                {
                    Id = ente.IdEnte,
                    Nome = ente.RagioneSociale,
                };

                for (int i = 0; i <= 3 + 4; i++)
                {
                    var da_data_giorno = da_giorno.AddDays(i).StartOfDay();
                    var a_data_giorno = da_giorno.AddDays(i).EndOfDay();

                    var trattamenti_giorno = trattamenti_ente.Where(x => x.Data >= da_data_giorno && x.Data <= a_data_giorno).ToList();
                    var trattamenti_raggruppati = Trattamento.RaggruppaTrattamenti(trattamenti_giorno);

                    var pulsanti = new List<string>();

                    foreach (var item in trattamenti_raggruppati.OrderBy(x => x.Key.Gruppo_IdRischio ?? 999999))
                    {
                        var trattamento = item.Key;
                        var num = item.Value;
                        var vigneto = vigneti_ente.First(x => x.IdVigneto == trattamento.IdVigneto);

                        var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });

                        if (trattamento.IdTipoTrattamento == (int)TipoTrattamento.Enum.PERONOSPORA_OIDIO)
                        {
                            if (trattamento.Gruppo_Esclusi == true)
                            {
                                var text = Tr.esclusi.ToUpper();
                                var tooltip = Tr.esclusi.ToUpper();
                                var colore = "btn-dark";

                                //pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                                pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                            }
                            else
                            {
                                var text = trattamento.Vigneto.Rischio.DescrizioneInLingua(utente.Lingua);
                                var tooltip = trattamento.Rischio?.DescrizioneInLingua(utente.Lingua);

                                if (trattamento.Gruppo_IdTipologiaLinea != null)
                                {
                                    text += " " + trattamento.TipologiaLinea?.Descrizione;
                                    tooltip += " " + trattamento.TipologiaLinea?.Descrizione;
                                }

                                var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-warning" : trattamento.Effettivo ? "btn-success" : null;

                                //pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                                pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                            }
                        }
                        else if (trattamento.IdTipoTrattamento == (int)TipoTrattamento.Enum.ALTRE_AVVERSITA)
                        {
                            var text = Tr.altre_avv.ToUpper();
                            //var tooltip = tipologia_linea.Descrizione;
                            var tooltip = Tr.altre_avv.ToUpper();
                            var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-info" : trattamento.Effettivo ? "btn-success" : null;

                            //pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                            pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                        }




                    }

                    //foreach (var tipologia_linea in tipologie_linee_ente)
                    //{
                    //    //var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });
                    //    var ids_vigneti_tipologia = vigneti_ente.Where(x => x.IdTipologiaLinea == tipologia_linea.Id).Select(x => x.IdVigneto).ToList();
                    //    var trattamenti_tipologia = trattamenti_giorno.Where(x => ids_vigneti_tipologia.Contains(x.IdVigneto)).ToList();

                    //    if (trattamenti_tipologia.Count == 0)
                    //        continue;

                    //    var trattamenti_raggruppati = Trattamento.RaggruppaTrattamenti(trattamenti_tipologia);

                    //    foreach(var trattamento in trattamenti_raggruppati)
                    //    {                            
                    //        var url = Url.Action("Form", "Trattamenti", new { Id = trattamento.Id, DaLista = true });

                    //        var text = tipologia_linea.Descrizione;
                    //        var tooltip = tipologia_linea.Descrizione;
                    //        var colore = trattamento.Automatico ? "btn-danger" : trattamento.Suggerito ? "btn-warning" : trattamento.Effettivo ? "btn-success" : null;

                    //        //pulsanti.Add(HtmlUtils.Button("", text, $"BtnInviaBollettino btn {colore} btn-pill mt-1 mb-1", null, tooltip: tooltip, htmlAttributes: attributes));
                    //        pulsanti.Add(HtmlUtils.AnchorButton(url, text, $"btn {colore} btn-pill mt-1 mb-1", tooltip: tooltip));
                    //    }                        
                    //}

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
            filtro_consulenti.Insert(0, new { Id = 0, Nome = "-" });
            filtro_tipologielinee.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vigneti.Insert(0, new { Id = 0, Nome = "-" });
            filtro_vitigni.Insert(0, new { Id = 0, Nome = "-" });
            filtro_rischi.Insert(0, new { Id = 0, Nome = "-" });
            filtro_tipitrattamento.Insert(0, new { Id = 0, Nome = "-" });
            filtro_gruppi.Insert(0, new { Id = 0, Nome = "-" });            

            model.Regioni = new SelectList(filtro_regioni, "Id", "Nome", model.IdRegione);
            model.Province = new SelectList(filtro_province, "Id", "Nome", model.IdProvincia);
            model.Comuni = new SelectList(filtro_comuni, "Id", "Nome", model.IdComune);
            model.Aziende = new SelectList(filtro_aziende, "Id", "Nome", model.IdAzienda);
            model.Consulenti = new SelectList(filtro_consulenti, "Id", "Nome", model.IdConsulente);
            model.TipologieLinee = new SelectList(filtro_tipologielinee, "Id", "Nome", model.IdTipologiaLinea);
            model.Vigneti = new SelectList(filtro_vigneti, "Id", "Nome", model.IdVigneto);
            model.Vitigni = new SelectList(filtro_vitigni, "Id", "Nome", model.IdVitigno);
            model.Rischi = new SelectList(filtro_rischi, "Id", "Nome", model.IdRischio);
            model.TipiTrattamento = new SelectList(filtro_tipitrattamento, "Id", "Nome", model.IdTipoTrattamento);
            model.Gruppi = new SelectList(filtro_gruppi, "Id", "Nome", model.IdGruppo);

            var elenco_rischi_completo = DB.Rischio.Select(x => new { Id = x.Id.ToString(), Nome = x.Descrizione }).ToList();
            elenco_rischi_completo.Insert(0, new { Id = "-", Nome = "-" });

            model.Elenco_Rischi_Completo = new SelectList(elenco_rischi_completo, "Id", "Nome", null);

            model.NumModelliProtezioneDaGenerare = GetIdsVigneti_ModelliProtezioneDaGenerare(utente).Count;

            return View(model);
        }




        [HttpGet]
        [AllowAnonymous]
        public ActionResult ElaborazioneModelliInCorso()
        {
            Utils.RicaricaImpostazioni();
            return Content(Utils.Impostazioni.ElaborazioneModelliProtezioneInCorso.ToString());
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult GetNote(Trattamento trattamento)
        {
            if (trattamento.Tab_IdAvversitaDaTrattare?.Contains(AvversitaDaTrattare.SCAFOIDEO) == true)
            {
                var path = Request.MapPath($"~\\note_trattamento_scafoideo.txt");
                return Content(System.IO.File.ReadAllText(path, Encoding.Default));
            }
            else
            {
                return Content("");
            }
        }

        [HttpGet]
        public JsonResult GetProdottiAjax(Select2AjaxRequestModel request, int idAvversita, int BBCH)
        {
            using (var db = new DB())
            {
                var response = new Select2AjaxResponseModel();

                var sql_prodotti = db.VistaAgrofarmaci
                    .Where(x => x.IdAvversitaDaTrattare == idAvversita)
                    .Select(x => new Select2AjaxItem
                    {
                        id = x.NUM_REG,
                        text = x.NOME_COMMERCIALE
                    });

                if (request.term.IsNotEmpty())
                {
                    sql_prodotti = sql_prodotti.Where(x => x.text.Contains(request.term));
                }

                response.results = sql_prodotti.ToList();

                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }

        //[HttpPost]
        //public ActionResult EliminaVignetiGruppo(int idTrattamento, string IdVignetiSelezionati)
        //{
        //    var trattamento = DB.Trattamento.First(x => x.Id == idTrattamento);

        //    var trattamenti = GetGruppoTrattamenti((int)trattamento.Gruppo_IdEnte, (int)trattamento.Gruppo_IdRischio, trattamento.Data, trattamento.Suggerito, trattamento.Effettivo);

        //    var ids_vigneti_da_rimuovere = IdVignetiSelezionati.Split(';').Select(x => x.ToInt()).ToArray();

        //    var trattamenti_da_rimuovere = trattamenti.Where(x => ids_vigneti_da_rimuovere.Contains(x.IdVigneto)).ToList();

        //    foreach(var trattamento_da_rimuovere in trattamenti_da_rimuovere)
        //    {
        //        trattamento_da_rimuovere.Gruppo_IdEnte = null;
        //        trattamento_da_rimuovere.Gruppo_IdRischio = null;
        //    }

        //    DB.SaveChanges();

        //    var primo_trattamento = trattamenti.Where(x => x.Gruppo_IdEnte != null && x.Gruppo_IdRischio != null).FirstOrDefault();

        //    return RedirectToAction(nameof(Trattamento), new { Id = primo_trattamento?.Id ?? trattamento.Id });
        //}


    }
}

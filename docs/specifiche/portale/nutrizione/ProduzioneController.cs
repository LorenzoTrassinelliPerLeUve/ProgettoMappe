using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using _4Grapes.Traduzioni;
using _4GrapesAPI.Entity;
using _4GrapesAPI.Models;

namespace _4GrapesAPI.Controllers
{
    [AuthorizePortal(Abilitazione = Abilitazione.PRODUZIONE)]
    public class ProduzioneController : _BaseController
    {

        [HttpGet]
        public ActionResult RilievoProduzione(long? id)
        {
            var model = new RilievoProduzioneModel();
            if (id != null)
            {
                var id_rilievo = new DateTime((long)id);
                var rilievo = DB.Rilievo.Where(x => x.IdRilievo == id_rilievo).FirstOrDefault();

                var vigneto = DB.Vigneto.Where(x => x.IdVigneto == rilievo.Vigneto_IdVigneto).Select(x => new
                {
                    IdVigneto = x.IdVigneto,
                    IdVigna = x.Vigna_IdVigna,
                    IdEnte = x.Vigna.Ente_IdEnte,
                    IdVitigno = x.Vitigno_idVitigno,
                    Latitudine = x.Latitudine,
                    Longitudine = x.Longitudine,
                    IdConduzione = x.Conduzione_idConduzione,
                }).FirstOrDefault() ?? throw new Exception("Vigneto non trovato");

                model.Id = id;
                //model.InizioRilievo = rilievo.InizioRilievo;
                //model.FineRilievo = rilievo.FineRilievo;

                model.ProduzioneTotaleDelVigneto = rilievo.ProduzioneTotaleDelVigneto;
                model.PesoMedioGrappoloPesato = rilievo.PesoMedioGrappoloPesato;
                model.DataDiVendemmia = rilievo.DataDiVendemmia;
                //model.NrOrganiControllati = rilievo.NrOrganiControllati;

                model.IdAzienda = vigneto.IdEnte;
                model.IdVigna = vigneto.IdVigna;
                model.IdVigneto = vigneto.IdVigneto;
                model.IdVitigno = vigneto.IdVitigno;
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult RilievoProduzione(RilievoProduzioneModel model)
        {
            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            if (Request.Form["Submit"] == "Salva")
            {
                var utente = User.GetUtente();



                var now = DateTime.Now;

                Rilievo rilievo;

                if (model.Id == null)
                {
                    rilievo = new Rilievo();
                    rilievo.IdRilievo = now;
                    rilievo.IdUtente = utente.idUtente;
                    rilievo.TipoRilievo_idTipoRilievo = (int)TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH;
                    rilievo.BBCH = 89;

                    var vigneto = DB.Vigneto.Where(x => x.IdVigneto > 0 && x.IdVigneto == model.IdVigneto).Select(x => new
                    {
                        IdVigneto = x.IdVigneto,
                        IdVigna = x.Vigna_IdVigna,
                        IdEnte = x.Vigna.Ente_IdEnte,
                        IdVitigno = x.Vitigno_idVitigno,
                        Latitudine = x.Latitudine,
                        Longitudine = x.Longitudine,
                        IdConduzione = x.Conduzione_idConduzione,
                    }).FirstOrDefault() ?? throw new Exception("Vigneto non trovato");

                    rilievo.Vigneto_IdVigneto = vigneto.IdVigneto;
                    rilievo.IdAziendaDaApp = vigneto.IdEnte;
                    rilievo.IdVignaDaApp = vigneto.IdVigna;
                    rilievo.IdVignetoDaApp = vigneto.IdVigneto;
                    rilievo.IdVitignoDaApp = vigneto.IdVitigno;
                    rilievo.Latitudine = vigneto.Latitudine;
                    rilievo.Longitudine = vigneto.Longitudine;
                    rilievo.Conduzione_idConduzione = vigneto.IdConduzione;
                    rilievo.InizioRilievo = DateTime.Now;
                    rilievo.FineRilievo = rilievo.InizioRilievo;

                    DB.Rilievo.Add(rilievo);
                }
                else
                {
                    var id_rilievo = new DateTime((long)model.Id);
                    rilievo = DB.Rilievo.Where(x => x.IdRilievo == id_rilievo && x.TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH).FirstOrDefault();
                }

                //-- AGGIORNO DATA INIZIO E FINE SOLO SE SONO CAMBIATI AL MINUTO
                //if (rilievo.InizioRilievo.Date != model.InizioRilievo?.Date)
                //{
                //    rilievo.InizioRilievo = model.InizioRilievo ?? throw new Exception("Campo obbligatorio");
                //}
                //if (rilievo.FineRilievo.Date != model.FineRilievo?.Date)
                //{
                //    rilievo.FineRilievo = model.FineRilievo ?? throw new Exception("Campo obbligatorio");
                //}

                rilievo.ProduzioneTotaleDelVigneto = model.ProduzioneTotaleDelVigneto;
                rilievo.PesoMedioGrappoloPesato = model.PesoMedioGrappoloPesato;
                rilievo.DataDiVendemmia = model.DataDiVendemmia;
                //rilievo.SistemaMetrico = utente.SistemaMetrico;


                DB.SaveAndLog(rilievo);

                FlashMessage(Tr.messaggio_inserimento_rilievo_completato);

                return RedirectToAction(nameof(ListaRilievi));
            }
            else
            {

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult EliminaRilievo(long id)
        {
            var utente = User.GetUtente();
            var id_rilievo = new DateTime(id);

            var sql_rilievo = DB.Rilievo.Where(x => x.IdRilievo == id_rilievo && x.TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH).AsQueryable();

            if (!utente.IsMasterUser)
            {
                var ids_enti = utente.GetIdsEntiAbbinati(DB);
                sql_rilievo = sql_rilievo.Where(x => ids_enti.Contains(x.Vigneto.Vigna.Ente_IdEnte));
            }

            var rilievo = sql_rilievo.FirstOrDefault();

            if (rilievo == null)
                throw new UnauthorizedAccessException();

            DB.Rilievo.Remove(rilievo);
            DB.SaveChanges();


            FlashMessage(Tr.messaggio_rilievo_analisi_eliminato_correttamente);

            return RedirectToAction(nameof(ListaRilievi));
        }

        [HttpGet]
        public ActionResult ListaRilievi(ListaRilieviProduzioneModel model)
        {
            var utente = User.GetUtente();

            var sql_rilievi = DB.VistaDatiProduzione.AsQueryable();
            //var sql_aziende = DB.Ente.Select(x => new { Id = x.IdEnte, Text = x.RagioneSociale });

            if (!utente.IsMasterUser)
            {
                var ids_enti = utente.GetIdsEntiAbbinati(DB);
                //sql_aziende = sql_aziende.Where(x => ids_enti.Contains(x.Id));
                sql_rilievi = sql_rilievi.Where(x => ids_enti.Contains((int)x.IdEnte));
            }

            if (model.Anno != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.Anno == model.Anno);
            }

            if (model.IdAzienda != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdEnte == model.IdAzienda);
            }

            if (model.IdVigna != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVigna == model.IdVigna);
            }

            if (model.IdVigneto != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVigneto == model.IdVigneto);
            }

            if (model.IdVitigno != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVitigno == model.IdVitigno);
            }

            var anni = sql_rilievi.Select(x => new { Id = x.Anno, Text = x.Anno }).Distinct().OrderByDescending(x => x.Id).ToList();
            model.ElencoAnni = new SelectList(anni, "Id", "Text", model.Anno);

            //var aziende = sql_rilievi.Select(x => new { Id = x.IdEnte, Text = x.RagioneSociale }).Distinct().OrderBy(x => x.Text).ToList();
            //model.ElencoAziende = new SelectList(aziende, "Id", "Text", model.IdAzienda);
            model.ElencoAziende = model.GetAziende(utente);

            var vigne = sql_rilievi.Select(x => new { Id = x.IdVigna, Text = x.Vigna }).Distinct().OrderBy(x => x.Text).ToList();
            model.ElencoVigne = new SelectList(vigne, "Id", "Text", model.IdVigna);

            var vigneti = sql_rilievi.Select(x => new { Id = x.IdVigneto, Text = x.NomeVigneto }).Distinct().OrderBy(x => x.Text).ToList();
            model.ElencoVigneti = new SelectList(vigneti, "Id", "Text", model.IdVigneto);

            var vitigni = sql_rilievi.Select(x => new { Id = x.IdVitigno, Text = x.Vitigno }).Distinct().OrderBy(x => x.Text).ToList();
            model.ElencoVitigni = new SelectList(vitigni, "Id", "Text", model.IdVitigno);

            return View(model);
        }

        [HttpGet]
        public ActionResult ListaRilieviAjax(ListaRilieviProduzioneModel model)
        {
            try
            {
                var utente = User.GetUtente();
                var datatableReturn = new jQueryDataTableReturn();

                //var db_items = DB.User.Where(x => x.IdAccount == idAccount).AsEnumerable();
                var db_items = DB.VistaDatiProduzione.Select(x => new
                {
                    x.IdRilievo,
                    x.InizioRilievo,
                    x.Anno,
                    x.IdVigna,
                    x.IdVigneto,
                    x.IdVitigno,
                    x.IdEnte,
                    Ente = x.RagioneSociale,
                    Vigna = x.Vigna,
                    Vigneto = x.NomeVigneto,
                    Vitigno = x.Vitigno,
                    x.ProduzioneTotaleDelVigneto,
                    x.PesoMedioGrappoloPesato,
                    x.DataDiVendemmia
                });

                //--- SE NON SEI UN UTENTE MASTER VEDI SOLO I TUOI VIGNETI
                if (!utente.IsMasterUser)
                {
                    var ids_enti = utente.GetIdsEntiAbbinati(DB);
                    //--- ESTIRPO E CARICO GESTITI COME IN VISTA VistaUtentiVigneti
                    db_items = db_items.Where(x => x.IdEnte != null && ids_enti.Contains((int)x.IdEnte));
                }

                if (model.Anno != null)
                {
                    db_items = db_items.Where(x => x.Anno == model.Anno);
                }

                if (model.IdAzienda != null)
                {
                    db_items = db_items.Where(x => x.IdEnte == model.IdAzienda);
                }

                if (model.IdVigna != null)
                {
                    db_items = db_items.Where(x => x.IdVigna == model.IdVigna);
                }

                if (model.IdVigneto != null)
                {
                    db_items = db_items.Where(x => x.IdVigneto == model.IdVigneto);
                }

                if (model.IdVitigno != null)
                {
                    db_items = db_items.Where(x => x.IdVitigno == model.IdVitigno);
                }

                // Il Totale record senza filtri é:
                datatableReturn.iTotalRecords = db_items.Count();

                if (model.sSearch.IsNotEmpty() && model.sSearch.Length >= 2)
                {
                    var search = model.sSearch.ToLower();
                    var split = search.Split(' ').Where(x => x != "").Select(x => x.ToLower()).ToList();

                    db_items = db_items.Where(x => split.All(y => (x.IdVigneto + " " + x.Ente + " " + x.Vigna + " " + x.Vigneto).Contains(y)));

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
                    case 0: db_items = db_items.OrderBy(x => x.InizioRilievo, model.sSortDir_0); break;
                    case 1: db_items = db_items.OrderBy(x => x.Ente, model.sSortDir_0); break;
                    case 2: db_items = db_items.OrderBy(x => x.Vigna, model.sSortDir_0); break;
                    case 3: db_items = db_items.OrderBy(x => x.Vigneto, model.sSortDir_0); break;
                    case 4: db_items = db_items.OrderBy(x => x.Vitigno, model.sSortDir_0); break;
                    case 5: db_items = db_items.OrderBy(x => x.ProduzioneTotaleDelVigneto, model.sSortDir_0); break;
                    case 6: db_items = db_items.OrderBy(x => x.PesoMedioGrappoloPesato, model.sSortDir_0); break;
                    case 7: db_items = db_items.OrderBy(x => x.DataDiVendemmia, model.sSortDir_0); break;
                }

                if (datatableReturn.iTotalDisplayRecords < model.iDisplayStart)
                    model.iDisplayStart = 0;

                db_items = db_items.Skip(model.iDisplayStart).Take(model.iDisplayLength);

                var items = db_items.ToList();

                var ids_enti_modificabili = utente.GetIdsEntiConAggiornamentiAbilitati(DB);

                // Costruzione del ResultSet dei soli campi da restituire alla Jquery DataTable            
                string[][] jaggedArray = new string[items.Count][];
                int conta = 0;
                foreach (var item in items)
                {
                    //string imageBase64 = Convert.ToBase64String(c.FotoFronte);                               

                    var Riga = new string[10];

                    Riga[0] = item.InizioRilievo.ToColumn();
                    Riga[1] = item.Ente.ToColumn();
                    Riga[2] = item.Vigna.ToColumn();
                    Riga[3] = item.Vigneto.ToColumn();
                    Riga[4] = item.Vitigno.ToColumn();
                    Riga[5] = item.ProduzioneTotaleDelVigneto.ToColumn();
                    Riga[6] = item.PesoMedioGrappoloPesato.ToColumn();
                    Riga[7] = item.DataDiVendemmia.ToColumn();

                    if (utente.IsMasterUser || item.IdEnte.In(ids_enti_modificabili))
                    {
                        Riga[8] = new GridButtons(Url)
                                        .Edit(nameof(RilievoProduzione), null, new { id = item.IdRilievo.Ticks })
                                        .CreateButtons();
                    }

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
        public ActionResult ObiettivoProduttivo(ObiettivoProduttivoModel model)
        {
            var utente = User.GetUtente();

            var sql_rilievi = DB.VistaVignetiConProduzioni.AsQueryable();
            //var sql_aziende = DB.Ente.Select(x => new { Id = x.IdEnte, Text = x.RagioneSociale });

            if (!utente.IsMasterUser)
            {
                var ids_enti = utente.GetIdsEntiAbbinati(DB);
                //sql_aziende = sql_aziende.Where(x => ids_enti.Contains(x.Id));
                sql_rilievi = sql_rilievi.Where(x => ids_enti.Contains((int)x.IdEnte));
            }

            if (model.Anno != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.AnnoUltimo == model.Anno);
            }

            if (model.IdAzienda != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdEnte == model.IdAzienda);
            }

            if (model.IdVigna != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVigna == model.IdVigna);
            }

            if (model.IdVigneto != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVigneto == model.IdVigneto);
            }

            if (model.IdVitigno != null)
            {
                sql_rilievi = sql_rilievi.Where(x => x.IdVitigno == model.IdVitigno);
            }

            var anni = sql_rilievi.Select(x => new { Id = x.AnnoUltimo, Text = x.AnnoUltimo }).Distinct().OrderByDescending(x => x.Id).ToList();
            model.ElencoAnni = new SelectList(anni, "Id", "Text", model.Anno);

            //var aziende = sql_rilievi.Select(x => new { Id = x.IdEnte, Text = x.RagioneSociale }).Distinct().OrderBy(x => x.Text).ToList();
            //model.ElencoAziende = new SelectList(aziende, "Id", "Text", model.IdAzienda);
            model.ElencoAziende = model.GetAziende(utente);

            model.ElencoVigne = model.GetVigne();
            model.ElencoVigneti = model.GetVigneti();
            model.ElencoVitigni = model.GetVitigni();
            model.ElencoTarget = model.GetTarget();


            //var vigne = sql_rilievi.Select(x => new { Id = x.IdVigna, Text = x.Vigna }).Distinct().OrderBy(x => x.Text).ToList();
            //model.ElencoVigne = new SelectList(vigne, "Id", "Text", model.IdVigna);

            //var vigneti = sql_rilievi.Select(x => new { Id = x.IdVigneto, Text = x.NomeVigneto }).Distinct().OrderBy(x => x.Text).ToList();
            //model.ElencoVigneti = new SelectList(vigneti, "Id", "Text", model.IdVigneto);

            //var vitigni = sql_rilievi.Select(x => new { Id = x.IdVitigno, Text = x.Vitigno }).Distinct().OrderBy(x => x.Text).ToList();
            //model.ElencoVitigni = new SelectList(vitigni, "Id", "Text", model.IdVitigno);

            if (model.IdVigna != null && !model.ElencoVigne.Select(x => x.Value.ToInt()).ToList().Contains(model.IdVigna))
            {
                model.IdVigna = null;
            }

            if (model.IdVigneto != null && !model.ElencoVigneti.Select(x => x.Value.ToInt()).ToList().Contains((int)model.IdVigneto))
            {
                model.IdVigna = null;
            }

            if (model.IdVitigno != null && !model.ElencoVitigni.Select(x => x.Value.ToInt()).ToList().Contains((int)model.IdVitigno))
            {
                model.IdVitigno = null;
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult ObiettivoProduttivoAjax(ObiettivoProduttivoModel model)
        {
            try
            {
                var utente = User.GetUtente();
                var datatableReturn = new jQueryDataTableReturn();

                //var db_items = DB.User.Where(x => x.IdAccount == idAccount).AsEnumerable();
                var db_items = DB.VistaVignetiConProduzioni.Select(x => new
                {
                    x.IdVigneto,
                    x.IdVigna,
                    x.IdVitigno,
                    x.IdEnte,
                    x.IdTarget,
                    Ente = x.RagioneSociale,
                    Vigna = x.Vigna,
                    Vigneto = x.NomeVigneto,
                    Vitigno = x.Vitigno,
                    Target = x.TargetEnologico,
                    x.ProduzioneRealeVigneto,
                    x.PercFallanze,
                    x.PercInAllevamento,
                    x.AnnoUltimo,
                    x.ProduzioneUltimoAnnoVigneto,
                    x.ProduzioneMediaAnnuaVigneto,
                    x.ObiettivoProduttivoVigneto,
                    x.ObiettivoProduttivoPerPianta,
                    x.ObiettivoProduttivoPerEttaro,
                    x.ProduzionePerPiantaUltimoAnno,
                    x.ProduzioneMediaAnnuaPerPianta,
                    x.ProduzioneUltimoAnnoPerEttaro,
                    x.ProduzioneMediaAnnuaPerEttaro
                });

                //--- SE NON SEI UN UTENTE MASTER VEDI SOLO I TUOI VIGNETI
                if (!utente.IsMasterUser)
                {
                    var ids_enti = utente.GetIdsEntiAbbinati(DB);
                    //--- ESTIRPO E CARICO GESTITI COME IN VISTA VistaUtentiVigneti
                    db_items = db_items.Where(x => x.IdEnte != null && ids_enti.Contains((int)x.IdEnte));
                }

                if (model.Anno != null)
                {
                    db_items = db_items.Where(x => x.AnnoUltimo == model.Anno);
                }

                if (model.IdAzienda != null)
                {
                    db_items = db_items.Where(x => x.IdEnte == model.IdAzienda);
                }

                if (model.IdVigna != null)
                {
                    db_items = db_items.Where(x => x.IdVigna == model.IdVigna);
                }

                if (model.IdVigneto != null)
                {
                    db_items = db_items.Where(x => x.IdVigneto == model.IdVigneto);
                }

                if (model.IdVitigno != null)
                {
                    db_items = db_items.Where(x => x.IdVitigno == model.IdVitigno);
                }

                if (model.IdTarget != null)
                {
                    db_items = db_items.Where(x => x.IdTarget == model.IdTarget);
                }

                // Il Totale record senza filtri é:
                datatableReturn.iTotalRecords = db_items.Count();

                if (model.sSearch.IsNotEmpty() && model.sSearch.Length >= 2)
                {
                    var search = model.sSearch.ToLower();
                    var split = search.Split(' ').Where(x => x != "").Select(x => x.ToLower()).ToList();

                    db_items = db_items.Where(x => split.All(y => (x.IdVigneto + " " + x.Ente + " " + x.Vigna + " " + x.Vigneto).Contains(y)));

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
                    case 0: db_items = db_items.OrderBy(x => x.Vigna, model.sSortDir_0); break;
                    case 1: db_items = db_items.OrderBy(x => x.Vigneto, model.sSortDir_0); break;
                    case 2: db_items = db_items.OrderBy(x => x.AnnoUltimo, model.sSortDir_0); break;
                    case 3: db_items = db_items.OrderBy(x => x.Target, model.sSortDir_0); break;                    
                    case 4: db_items = db_items.OrderBy(x => x.ObiettivoProduttivoVigneto, model.sSortDir_0); break;
                    case 5: db_items = db_items.OrderBy(x => x.ObiettivoProduttivoPerPianta, model.sSortDir_0); break;
                    case 6: db_items = db_items.OrderBy(x => x.ObiettivoProduttivoPerEttaro, model.sSortDir_0); break;
                    case 7: db_items = db_items.OrderBy(x => x.ProduzioneUltimoAnnoVigneto, model.sSortDir_0); break;
                    case 8: db_items = db_items.OrderBy(x => x.ProduzionePerPiantaUltimoAnno, model.sSortDir_0); break;
                    case 9: db_items = db_items.OrderBy(x => x.ProduzioneUltimoAnnoPerEttaro, model.sSortDir_0); break;
                    case 10: db_items = db_items.OrderBy(x => x.ProduzioneMediaAnnuaVigneto, model.sSortDir_0); break;
                    case 11: db_items = db_items.OrderBy(x => x.ProduzioneMediaAnnuaPerPianta, model.sSortDir_0); break;
                    case 12: db_items = db_items.OrderBy(x => x.ProduzioneMediaAnnuaPerEttaro, model.sSortDir_0); break;
                    case 13: db_items = db_items.OrderBy(x => x.PercFallanze, model.sSortDir_0); break;
                    case 14: db_items = db_items.OrderBy(x => x.PercInAllevamento, model.sSortDir_0); break;
                }

                if (datatableReturn.iTotalDisplayRecords < model.iDisplayStart)
                    model.iDisplayStart = 0;

                db_items = db_items.Skip(model.iDisplayStart).Take(model.iDisplayLength);

                var items = db_items.ToList();

                var ids_enti_modificabili = utente.GetIdsEntiConAggiornamentiAbilitati(DB);
                var anno = DateTime.Now.Year;

                // Costruzione del ResultSet dei soli campi da restituire alla Jquery DataTable            
                string[][] jaggedArray = new string[items.Count][];
                int conta = 0;
                foreach (var item in items)
                {
                    //string imageBase64 = Convert.ToBase64String(c.FotoFronte);                               

                    var Riga = new string[16];

                    Riga[0] = item.Vigna.ToColumn();
                    Riga[1] = item.Vigneto.ToColumn();
                    Riga[2] = item.AnnoUltimo?.ToString();
                    Riga[3] = item.Target.ToColumn();
                    Riga[4] = item.ObiettivoProduttivoVigneto.ToColumn();
                    Riga[5] = item.ObiettivoProduttivoPerPianta.ToColumn();
                    Riga[6] = item.ObiettivoProduttivoPerEttaro.ToColumn();
                    Riga[7] = item.ProduzioneUltimoAnnoVigneto.ToColumn();
                    Riga[8] = item.ProduzionePerPiantaUltimoAnno.ToColumn();
                    Riga[9] = item.ProduzioneUltimoAnnoPerEttaro.ToColumn();
                    Riga[10] = item.ProduzioneMediaAnnuaVigneto.ToColumn();
                    Riga[11] = item.ProduzioneMediaAnnuaPerPianta.ToColumn();
                    Riga[12] = item.ProduzioneMediaAnnuaPerEttaro.ToColumn();
                    Riga[13] = item.PercFallanze.ToColumn();
                    Riga[14] = item.PercInAllevamento.ToColumn();

                    if (utente.IsMasterUser || item.IdEnte.In(ids_enti_modificabili))
                    {
                        Riga[15] = new GridButtons(Url)
                                        .OnClick("pe-7s-edit", $"CambiaTargetProduttivo({item.IdVigneto.ToJsParam()},{(item.AnnoUltimo ?? anno).ToJsParam()})")
                                        .CreateButtons();
                    }

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
        public ActionResult GetTargetProduttivo(int anno, int idVigneto)
        {
            var target = DB.TargetProduttivi.Where(x => x.Anno == anno && x.IdVigneto == idVigneto).FirstOrDefault();
            var vigneto = DB.Vigneto.Where(x => x.IdVigneto == idVigneto).Select(x => new { x.NumeroPiante, x.Superficie }).FirstOrDefault();

            if (target != null)
            {
                var data = new
                {
                    target.Anno,
                    target.IdVigneto,
                    target.ObiettivoProduttivoVigneto,
                    target.ObiettivoProduttivoPerPianta,
                    vigneto.NumeroPiante,
                    vigneto.Superficie,
                };

                return Json(data, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var data = new
                {
                    Anno = anno,
                    IdVigneto = idVigneto,
                    vigneto.NumeroPiante,
                    vigneto.Superficie
                };

                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CambiaTargetProduttivo(ObiettivoProduttivoModel model)
        {
            var riga = DB.TargetProduttivi.Where(x => x.Anno == model.CambiaTargetProduttivo_Anno && x.IdVigneto == model.CambiaTargetProduttivo_IdVigneto).FirstOrDefault();

            if (riga != null)
            {
                riga.ObiettivoProduttivoVigneto = model.CambiaTargetProduttivo_ObiettivoProduttivoVigneto ?? throw new Exception("ObiettivoProduttivoVigneto non valido");
                riga.ObiettivoProduttivoPerPianta = model.CambiaTargetProduttivo_ObiettivoProduttivoPerPianta ?? throw new Exception("ObiettivoProduttivoPerPianta non valido");
                riga.ObiettivoProduttivoPerEttaro = model.CambiaTargetProduttivo_ObiettivoProduttivoPerEttaro ?? throw new Exception("ObiettivoProduttivoPerEttaro non valido");

                DB.SaveChanges();
            }
            else
            {
                riga = new TargetProduttivo();

                riga.Anno = model.CambiaTargetProduttivo_Anno ?? throw new Exception("CambiaTargetProduttivo_Anno non valido");
                riga.IdVigneto = model.CambiaTargetProduttivo_IdVigneto ?? throw new Exception("CambiaTargetProduttivo_IdVigneto non valido");
                riga.ObiettivoProduttivoVigneto = model.CambiaTargetProduttivo_ObiettivoProduttivoVigneto ?? throw new Exception("ObiettivoProduttivoVigneto non valido");
                riga.ObiettivoProduttivoPerPianta = model.CambiaTargetProduttivo_ObiettivoProduttivoPerPianta ?? throw new Exception("ObiettivoProduttivoPerPianta non valido");
                riga.ObiettivoProduttivoPerEttaro = model.CambiaTargetProduttivo_ObiettivoProduttivoPerEttaro ?? throw new Exception("ObiettivoProduttivoPerEttaro non valido");

                DB.TargetProduttivi.Add(riga);

                DB.SaveChanges();
            }



            //DB.Database.ExecuteSqlCommand(@"
            //    UPDATE UnitaBaseConcimazione
            //    SET IdTarget = @IdTargetNuovo
            //    WHERE Anno = @Anno AND IdVigneto = @IdVigneto AND IdTarget = @IdTarget",

            //    new SqlParameter("@Anno", model.CambiaTarget_Anno),
            //    new SqlParameter("@IdVigneto", model.CambiaTarget_IdVigneto),
            //    new SqlParameter("@IdTarget", ),
            //    new SqlParameter("@IdTargetNuovo", )
            //);

            FlashMessage(Tr.salvataggio_completato);

            return RedirectToAction(nameof(ObiettivoProduttivo), new { model.Anno, model.IdAzienda, model.IdVigneto });
        }
    }
}
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
using _4Grapes.Traduzioni;

namespace _4GrapesAPI.Controllers
{
    [AuthorizePortal(Abilitazione = Abilitazione.VIGNETI)]
    public class VignetiController : _BaseController
    {
        private Vigneto GetVignetoFromId(int? id)
        {
            if (id == null)
            {
                //var vigneto = new Vigneto();
                //return vigneto;
                throw new Exception();
            }
            else
            {
                var utente = User.GetUtente();
                if (!utente.IsMasterUser)
                {                    
                    var ids_vigneti_utente = utente.GetIdsVignetiAbbinati(DB);

                    if (!ids_vigneti_utente.Contains((int)id))
                        throw new UnauthorizedAccessException();
                    
                    if (!utente.IsVignetoModificabile(DB, (int)id))
                        throw new UnauthorizedAccessException();
                }

                var vigneto = DB.Vigneto.Include(x => x.VignetoMaps).Include(x => x.Vigna.Ente).First(x => x.IdVigneto == id);                

                return vigneto;
            }

        }

        [HttpGet]
        public ActionResult Form(int? id)
        {
            var model = GetVignetoFromId(id);

            if (model.DataEstirpo == null)
            {
                model.DataEstirpo = DateTime.Now;
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Form(int? id, Vigneto model)
        {
            if (!ModelState.IsValid)
                throw new Exception("Model not valid");

            var utente = User.GetUtente();

            if (model.Latitudine == 0)
            {
                model.Latitudine = null;
            }
            if (model.Longitudine == 0)
            {
                model.Longitudine = null;
            }

            var vigneto = GetVignetoFromId(id);

            vigneto.DataAggiornamentoStato = DateTime.Now;
            vigneto.NomeVigneto = model.NomeVigneto;
            vigneto.Vitigno_idVitigno = model.Vitigno_idVitigno;
            vigneto.AnnoImpianto = model.AnnoImpianto;
            vigneto.SullaFila = model.SullaFila;
            vigneto.TraFila = model.TraFila;
            vigneto.Estirpo = model.Estirpo;
            if (vigneto.Estirpo)
            {
                vigneto.DataEstirpo = model.DataEstirpo;
                vigneto.AnnoEstirpo = vigneto.DataEstirpo?.Year;
            }

            vigneto.Carico = model.Carico;
            if (utente.IsMasterUser || utente.IsUtentePerleuve || utente.IsDSSUser)
            {
                vigneto.IdTipologiaLinea = model.IdTipologiaLinea;
                vigneto.IdRischio = model.IdRischio;
            }
            if (vigneto.CanEdit_ElaboraMeteo(utente))
            {
                vigneto.ElaboraMeteo = model.ElaboraMeteo;
            }            
            vigneto.PesoMedioGrappolo = model.PesoMedioGrappolo;
            vigneto.Note = model.Note;
            vigneto.Irrigazione_idIrrigazione = model.Irrigazione_idIrrigazione;
            vigneto.Giacitura_idGiacitura = model.Giacitura_idGiacitura;
            vigneto.FormaAllevamento_idFormaAllevamento = model.FormaAllevamento_idFormaAllevamento;
            vigneto.Portainnesto_idPortainnesto = model.Portainnesto_idPortainnesto;
            vigneto.Clone_IdClone = model.Clone_IdClone;
            vigneto.Conduzione_idConduzione = model.Conduzione_idConduzione;
            vigneto.Esposizione = model.Esposizione;
            vigneto.QuotaSLM = model.QuotaSLM;
            vigneto.Pendenza = model.Pendenza;
            vigneto.Orientamento = model.Orientamento;
            vigneto.NumeroPianteContate = model.NumeroPianteContate;
            vigneto.SuperficieDichiarata = model.SuperficieDichiarata;
            vigneto.SuperficieMisurata = model.SuperficieMisurata;

            var latitudine = vigneto.Latitudine != null ? Math.Truncate((decimal)vigneto.Latitudine * 1000000000000) / 1000000000000 : (decimal?)null;
            var longitudine = vigneto.Longitudine != null ? Math.Truncate((decimal)vigneto.Longitudine * 1000000000000) / 1000000000000 : (decimal?)null;

            var coordinate_modificate = latitudine != model.Latitudine || longitudine != model.Longitudine;

            if (coordinate_modificate)
            {
                vigneto.Latitudine = model.Latitudine;
                vigneto.Longitudine = model.Longitudine;
                vigneto.Maps = null;

                if (vigneto.VignetoMaps != null)
                {                    
                    DB.VignetoMaps.Remove(vigneto.VignetoMaps);
                    vigneto.VignetoMaps = null;
                }                
            }

            if (DB.Vigneto.Count(x => x.NomeVigneto == vigneto.NomeVigneto && x.Vigna_IdVigna == vigneto.Vigna_IdVigna && x.IdVigneto != vigneto.IdVigneto) > 0)
            {
                ModelState.AddModelError(nameof(model.NomeVigneto), Tr.errore_nome_gia_esistente);

                return View(model);
            }

            if (id == null)
            {
                DB.Vigneto.Add(vigneto);
            }

            DB.SaveAndLog(vigneto);

            FlashMessage(Tr.salvataggio_completato);

            if (coordinate_modificate && vigneto.Latitudine != null && vigneto.Longitudine != null)
            {
                var georef = new ProcessStartInfo();
                georef.FileName = Settings.Default.GeoRef_PathExe;
                georef.WorkingDirectory = Path.GetDirectoryName(Settings.Default.GeoRef_PathExe);

                Process.Start(georef);
            }

            return RedirectToAction(nameof(Form), new { id = vigneto.IdVigneto });
        }

        [HttpPost]
        public ActionResult Elimina(int id)
        {            
            var vigneto = GetVignetoFromId(id);

            DB.Vigneto.Remove(vigneto);

            DB.SaveAndLog(vigneto);

            return RedirectToAction(nameof(Lista));
        }

        [HttpGet]
        public ActionResult Lista(ListaVignetiModel model)
        {
            var utente = User.GetUtente();

            var sql_aziende = DB.Ente.Select(x => new { Id = x.IdEnte, Text = x.RagioneSociale });

            if (!utente.IsMasterUser)
            {
                var ids_enti = utente.GetIdsEntiAbbinati(DB);
                sql_aziende = sql_aziende.Where(x => ids_enti.Contains(x.Id));
            }

            var aziende = sql_aziende.ToList();
            model.ElencoAziende = new SelectList(aziende, "Id", "Text", model.IdAzienda);

            return View(model);
        }

        [HttpGet]
        public ActionResult ListaAjax(ListaVignetiModel model)
        {
            try
            {
                var utente = User.GetUtente();
                var datatableReturn = new jQueryDataTableReturn();

                //var db_items = DB.User.Where(x => x.IdAccount == idAccount).AsEnumerable();
                var db_items = DB.Vigneto.Where(x => x.IdVigneto > 0).Select(x => new { 
                    IdVigneto = x.IdVigneto,
                    IdEnte = x.Vigna.Ente_IdEnte,
                    Ente = x.Vigna.Ente.RagioneSociale,
                    Vigna = x.Vigna.NomeVigna,
                    Vigneto = x.NomeVigneto,
                    Regione = x.VignetoMaps.AdministrativeAreaLevel1,
                    Provincia = x.VignetoMaps.AdministrativeAreaLevel2,
                    Comune = x.VignetoMaps.AdministrativeAreaLevel3,  
                    Estirpo = x.Estirpo,
                    Carico = x.Carico,
                });

                //--- SE NON SEI UN UTENTE MASTER VEDI SOLO I TUOI VIGNETI
                if (!utente.IsMasterUser)
                {
                    var ids_enti = utente.GetIdsEntiAbbinati(DB);
                    //--- ESTIRPO E CARICO GESTITI COME IN VISTA VistaUtentiVigneti
                    db_items = db_items.Where(x => ids_enti.Contains(x.IdEnte) && !x.Estirpo && x.Carico);
                }

                if (model.IdAzienda != null)
                {
                    db_items = db_items.Where(x => x.IdEnte == model.IdAzienda);
                }

                // Il Totale record senza filtri é:
                datatableReturn.iTotalRecords = db_items.Count();

                if (!string.IsNullOrEmpty(model.sSearch) && model.sSearch.Length >= 2)
                {
                    var search = model.sSearch.ToLower();
                    var split = search.Split(' ').Where(x => x != "").Select(x => x.ToLower()).ToList();

                    db_items = db_items.Where(x => split.All(y => (x.IdVigneto + " " + x.Ente + " " + x.Vigna + " " + x.Vigneto + " " + x.Regione + " " + x.Provincia + " " + x.Comune).Contains(y)));

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
                    case 0: db_items = db_items.OrderBy(x => x.IdVigneto, model.sSortDir_0); break;
                    case 1: db_items = db_items.OrderBy(x => x.Ente, model.sSortDir_0); break;
                    case 2: db_items = db_items.OrderBy(x => x.Vigna, model.sSortDir_0); break;
                    case 3: db_items = db_items.OrderBy(x => x.Vigneto, model.sSortDir_0); break;
                    case 4: db_items = db_items.OrderBy(x => x.Regione, model.sSortDir_0); break;
                    case 5: db_items = db_items.OrderBy(x => x.Provincia, model.sSortDir_0); break;
                    case 6: db_items = db_items.OrderBy(x => x.Comune, model.sSortDir_0); break;
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

                    string[] Riga = new string[8];

                    Riga[0] = item.IdVigneto.ToColumn();
                    Riga[1] = item.Ente.ToColumn();
                    Riga[2] = item.Vigna.ToColumn();
                    Riga[3] = item.Vigneto.ToColumn();
                    Riga[4] = item.Regione.ToColumn();
                    Riga[5] = item.Provincia.ToColumn();
                    Riga[6] = item.Comune.ToColumn();

                    if (utente.IsMasterUser || ids_enti_modificabili.Contains(item.IdEnte))
                    {
                        Riga[7] = new GridButtons(Url)
                                        .Edit(item.IdVigneto)
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
        public ActionResult DownloadVigneti(int? IdAzienda)
        {
            var utente = User.GetUtente();            

            //var ids_vigneti = DB.VistaUtentiVigneti.Where(x => x.idUtente == utente.idUtente).Select(x => x.IdVigneto).ToList();

            var where = "";

            if (!utente.IsMasterUser)
            {
                var ids_enti = utente.GetIdsEntiAbbinati(DB);
                where = $" AND _IdAzienda IN ({ids_enti.Join(",")})";
            }

            if (IdAzienda != null)
            {
                where = $" AND _IdAzienda = {IdAzienda}";
            }

            var lingua = utente.Lingua ?? "IT";

            var sql = $"SELECT * FROM VistaExportVigneti_{lingua} WHERE 1=1 {where} ORDER BY _Azienda,_Vigneto";
            //var sql = $"SELECT * FROM dbo.ExportVigneti() WHERE 1=1 {where} ORDER BY Azienda,Vigneto";
            var datatable = DB.SqlViewToDataTable(sql);

            //datatable.Columns.RemoveIfExists("IdAzienda");
            //datatable.Columns.RemoveIfExists("IdVigneto");

            var memoryStream = new MemoryStream();
            using (var excel = new ExcelPackage())
            {
                //var sql = $"SELECT * FROM VistaExportDatiMeteoGiornalieri WHERE IdVigneto IN ({in_ids_vigenti}) AND Data >= '{str_data_da}' AND Data <= '{str_data_a}' ORDER BY Vigneto,Data";                                                                
                excel.InsertDataInSheet(datatable, Tr.vigneti, "dd/MM/yyyy HH:mm:ss");
                excel.SaveAs(memoryStream);
            }

            memoryStream.Position = 0;

            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Export {Tr.vigneti}.xlsx");
        }
    }
}
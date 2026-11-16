using _4Grapes.Traduzioni;
using _4GrapesAPI.Entity;
using _4GrapesAPI.Models;
using _4GrapesAPI.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace _4GrapesAPI.Controllers
{
    [AuthorizePortal(Abilitazione = Abilitazione.DASHBOARD)]
    public class DashboardNutrizioneController : _BaseController
    {

        //------------------------------------- DASHBOARD MASTER ---------------------------------------------
        [HttpGet]
        public ActionResult MyDashboard(DashboardModel model)
        {
            //var html = RenderActionToHTML(Report());

            //var pdf = PdfGenerator.GeneratePdf(html, PageSize.A4);
            //pdf.Save(Server.MapPath("~/document.pdf"));

            var now = DateTime.Now;
            if (model.Anno == 0 && model.Periodo == 0)
            {
                model.Anno = now.Year;
                model.Periodo = DashboardModel.ULTIMI_15_GIORNI;
            }

            var data_da_a = GetPeriodo(model.Anno, model.Periodo);
            var data_da = data_da_a[0];
            var data_a = data_da_a[1];

            var idUser = User.ID();
            //var idEntiMaster = DB.EntiUtente.Where(x => x.IdUtente == idUser).Select(x => x.IdEnte).ToList();

            //var ordini_totali = DB.Ordine.Where(x => idEntiMaster.Contains(x.IdEnteMaster) && x.DataOrdine >= data_da && x.DataOrdine <= data_a && x.IdStatoOrdine > 0);
            //var ordini_cliente = ordini_totali.Where(x => x.TipoDocumento.IdClasseDocumento == ClasseDocumento.VENDITA);
            //var ordini_fornitore = ordini_totali.Where(x => x.TipoDocumento.IdClasseDocumento == ClasseDocumento.ACQUISTO);

            //---------- UTENTI
            {                
                var box = new DashboardBox();
                box.Titolo = Tr.utenti.ToUpper();
                //box.Numero = DB.Utente.Count();
                box.Numero = DB.CountFast<Utente>();

                model.Boxes.Add(box);
            }
            //---------- VIGNETI
            {
                var box = new DashboardBox();
                box.Titolo = Tr.vigneti.ToUpper();
                //box.Numero = DB.Vigneto.Count();
                box.Numero = DB.CountFast<Vigneto>();

                model.Boxes.Add(box);
            }
            //---------- RILIEVI
            {
                var box = new DashboardBox();
                box.Titolo = Tr.rilievi.ToUpper();
                //box.Numero = DB.Rilievo.Count();
                box.Numero = DB.CountFast<Rilievo>();

                model.Boxes.Add(box);
            }
            //---------- COLLEZIONI
            {
                var box = new DashboardBox();
                box.Titolo = Tr.collezioni.ToUpper();
                box.Numero = DB.CountFast<Collezione>();                

                model.Boxes.Add(box);
            }
            //---------- BBCH
            {
                var bbch = DB.BBCH.Find(8);
                var vitigno = DB.Vitigno.Find(8);


                var box = new DashboardBox();
                box.Titolo = "BBCH";
                //box.Numero = DB.Rilievo.Count();

                //model.Boxes.Add(box);
            }

            //---------- TOP VINI
            {               
                //var righe_ordini = ordini_totali.SelectMany(x => x.OrdineRiga).Where(x => x.IdStatoRiga > 0);

                //var totale = righe_ordini.Sum(x => (int?)x.QuantitaOrdinata) ?? 0;

                //var top_vini = righe_ordini.GroupBy(x => x.IdReferenza).OrderByDescending(x => x.Sum(y => y.QuantitaOrdinata)).Select(x => new DashboardTopVino()
                //{
                //    IdReferenza = x.Key,
                //    Valore = x.Sum(y => y.QuantitaOrdinata)
                //}).Take(3).ToList();

                //var idReferenze = top_vini.Select(x => x.IdReferenza).ToList();
                //var referenze = DB.vw_ReferenzeDescrizioneCompatta.Where(x => idReferenze.Contains(x.IdReferenza)).ToList();

                //foreach (var top_vino in top_vini)
                //{
                //    top_vino.Nome = referenze.FirstOrDefault(x => x.IdReferenza == top_vino.IdReferenza)?.Descrizione;
                //    top_vino.Sottotitolo = "TOP {0}".FormatWith(top_vini.IndexOf(top_vino) + 1);
                //    top_vino.Totale = totale;
                //    if (top_vino.Totale > 0)
                //    {
                //        top_vino.Percentuale = top_vino.Valore * 100 / top_vino.Totale;
                //    }
                //
                //    model.TopVini.Add(top_vino);
                //}
            }
            //----- PROMO
            {
                //model.Promo = DB.Database.SqlQuery<vw_ConteggioPromo>("SELECT * FROM vw_ConteggioPromo").ToList().FirstOrDefault() ?? new vw_ConteggioPromo();
            }

            return View(model);
        }        

        //------------------------------------------------------------------------------------------------------
        public DateTime[] GetPeriodo(int anno, int periodo)
        {
            var date_da_a = new DateTime[2];

            DateTime data_da, data_a;
            if (periodo >= 1 && periodo <= 12)
            {
                data_da = new DateTime(anno, periodo, 1);
                data_a = new DateTime(anno, periodo, DateTime.DaysInMonth(anno, periodo));
            }
            else
            {
                data_a = DateTime.Now.Date.AddYears(anno - DateTime.Now.Year);
                data_da = data_a.AddDays(-DashboardModel.GetGiorniDaPeriodo(periodo));
            }

            //--- NORMALIZZO LE DATE DA 00:00 a 24:59
            date_da_a[1] = data_a.Date.AddDays(1).AddTicks(-1);
            date_da_a[0] = data_da.Date;

            return date_da_a;
        }
    }
}

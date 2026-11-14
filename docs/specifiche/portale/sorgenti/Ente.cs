using _4Grapes.Traduzioni;
using _4GrapesAPI.Controllers;
using _4GrapesAPI.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace _4GrapesAPI.Entity
{   
    public partial class Ente
    {
        public const int AZIENDA_TEST = 1;

        public decimal GetPressione(DB DB, DateTime data, int idAvversita)
        {
            var elenco_bbch = DB.BBCH.ToList();

            var anno = data.Year;
            var a_data = data.EndOfDay();

            var vigneti_azienda = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente_IdEnte == IdEnte).Select(x => new
            {
                IdVigneto = x.IdVigneto,
                IdComune = x.VignetoMaps.Area3,
                IdVitigno = x.Vitigno_idVitigno,
                IdRischio = x.IdRischio,
                IdTipologiaLinea = x.IdTipologiaLinea,
            }).ToList();

            var ids_comuni = vigneti_azienda.Where(x => x.IdComune != null).Select(x => x.IdComune).Distinct().ToList();

            var vigneti_intorno_a_me = DB.Vigneto.Where(x => x.Carico && !x.Estirpo && x.Vigna.Ente.TipoEnte_idTipoEnte == TipoEnte.AZIENDA_VITIVINICOLA && ids_comuni.Contains(x.VignetoMaps.Area3)).Select(x => new
            {
                IdVigneto = x.IdVigneto,
                IdRegione = x.VignetoMaps.Area1,
                IdComune = x.VignetoMaps.Area3,
                IdVitigno = x.Vitigno_idVitigno,
                IdAzienda = x.Vigna.Ente_IdEnte,
                IdRischio = x.IdRischio,
                IdTipologiaLinea = x.IdTipologiaLinea,
            }).ToList();

            var ids_vigneti_azienda = vigneti_azienda.Select(x => x.IdVigneto).ToList();
           
            var vigneti_R = DB.Rilievo
                .Where(x => ids_vigneti_azienda.Contains((int)x.Vigneto_IdVigneto) && x.TipoRilievo_idTipoRilievo == idAvversita && x.PercDiffusione != null && x.PercIntensità != null && x.InizioRilievo.Year == anno && x.InizioRilievo <= a_data)
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
                   Fenologia = GetFenologia(elenco_bbch, x.BBCH, idAvversita),                   
               })
               .ToList();

            var vigneti_IntornoAMe = vigneti_intorno_a_me.Select(x => new
            {
                x.IdVigneto,
                x.IdComune,
                x.IdRegione,
                x.IdRischio,
                x.IdAzienda,
                x.IdTipologiaLinea,
                x.IdVitigno,
                Controllo = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto),               
                Fenologia = vigneti_F.Find(y => y.IdVigneto == x.IdVigneto)?.Fenologia,
            }).ToList();

            var vigneti_MioMonitoraggio = vigneti_azienda.Select(x => new
            {
                x.IdVigneto,
                x.IdComune,
                x.IdRischio,
                x.IdTipologiaLinea,
                x.IdVitigno,
                Controllo = vigneti_R.Find(y => y.IdVigneto == x.IdVigneto),
                Fenologia = vigneti_F.Find(y => y.IdVigneto == x.IdVigneto)?.Fenologia,
            }).ToList();



            //--------------------- COMUNI 
            var ids_comuni_IntornoAMe = vigneti_IntornoAMe.Select(x => x.IdComune).Distinct().ToList();

            var righe_comuni = new Dictionary<string, decimal>();
            foreach (var id_comune in ids_comuni_IntornoAMe)
            {                
                var controlli = vigneti_IntornoAMe.Where(x => x.IdComune == id_comune && x.Controllo != null).Select(x => x.Controllo).ToList();

                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    var intorno_a_me = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_INTORNO_A_ME;
                    righe_comuni.Add(id_comune, intorno_a_me);
                }
            }

            decimal IntornoAMe = 0;
            decimal Fenologia = 0;
            decimal MioMonitoraggio = 0;


            {                
                var controlli = vigneti_MioMonitoraggio.Where(x => x.Controllo != null).Select(x => x.Controllo).ToList();

                var num_vigneti_controllati_totali = controlli.Count;
                var num_vigneti_controllati_con_infezione = controlli.Where(x => x.ConInfezione > 0).Count();

                if (controlli.Count > 0)
                {
                    var perc_presenza_avversita = controlli.Count(x => x.ConInfezione > 0) / (decimal)controlli.Count * 100;

                    var media_max_diffusione = controlli.Average(x => x.MaxDiffusione);
                    var media_max_intensita = controlli.Average(x => x.MaxIntensita);

                    MioMonitoraggio = Utils.RadiceCubica(perc_presenza_avversita * media_max_diffusione * media_max_intensita) * TrattamentiController.PESO_MIO_MONITORAGGIO;
                }
                //riga_azienda.IdsVignetiControllati = vigneti_controllati.Select(x => x.IdVigneto).ToList();

                var ids_comuni_controllati = vigneti_MioMonitoraggio.Where(x => x.Controllo != null).Select(x => x.IdComune).Distinct().ToList();

                IntornoAMe = righe_comuni.Where(x => ids_comuni_controllati.ContainsIgnoreCase(x.Key)).Average(x => (decimal?)x.Value) ?? 0;

                Fenologia = ((decimal?)vigneti_MioMonitoraggio.Where(x => x.Fenologia != null).Average(x => x.Fenologia) * TrattamentiController.PESO_BBCH ) ?? 0;              
            }


            //--- PRESSIONI AZIENDE           
            return IntornoAMe + MioMonitoraggio + Fenologia;
        }

        private static int? GetFenologia(List<BBCH> elenco_bbch, int? moda_BBCH, int idAvversita)
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

        public static string GetTestoPressioneDaValore(decimal? valore)
        {
            if (valore == null || valore <= 0) return Tr.nulla.ToUpper();
            else if (valore < 5) return Tr.molto_bassa.ToUpper();
            else if (valore < 10) return Tr.bassa.ToUpper();
            else if (valore < 20) return Tr.media.ToUpper();
            else if (valore < 40) return Tr.alta.ToUpper();
            else if (valore < 70) return Tr.molto_alta.ToUpper();
            else return Tr.elevata.ToUpper();
        }
    }

   

}
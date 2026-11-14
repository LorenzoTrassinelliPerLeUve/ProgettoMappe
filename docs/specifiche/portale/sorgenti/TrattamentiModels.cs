using _4GrapesAPI.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _4GrapesAPI.Models
{    
    public class PressioneModel
    {        
        public int IdAvversita { get; set; }
        public int IdTipologiaLinea { get; set; }
        public string IdRegione { get; set; } = "-";
        public string IdComune { get; set; } = "-";
        public int IdGruppo { get; set; }
        public int IdAzienda { get; set; }
        public int IdRischio { get; set; }
        public int IdVitigno { get; set; }
        public string Data { get; set; } = DateTime.Today.ToShortDateString();


        public SelectList Avversita { get; set; }
        public SelectList TipologieLinea { get; set; }
        public SelectList Regioni { get; set; }
        public SelectList Comuni { get; set; }
        public SelectList Gruppi { get; set; }
        public SelectList Aziende { get; set; }
        public SelectList Rischi { get; set; }
        public SelectList Vitigni { get; set; }


      
        public List<RigaComune> RigheComune { get; set; } = new List<RigaComune>();
        public List<RigaRegione> RigheRegione { get; set; } = new List<RigaRegione>();
        public List<RigaAzienda> RigheAzienda { get; set; } = new List<RigaAzienda>();
        public List<RigaRischio> RigheRischio { get; set; } = new List<RigaRischio>();
        public List<RigaVitigno> RigheVitigno { get; set; } = new List<RigaVitigno>();
        public List<RigaVigneto> RigheVigneto { get; set; } = new List<RigaVigneto>();
    }

    public class RigaPressione
    {
        public string Chiave { get; set; }
        public decimal Valore { get; set; }
    }

    public class RigaAzienda
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal? MioMonitoraggio { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }
        public bool FenologiaDaComuni { get; set; }
        public List<int> IdsVignetiControllati { get; set; }


        public decimal? Pressione
        {
            get
            {
                return MioMonitoraggio + IntornoAMe + Fenologia;
            }
        }

        public string Colore 
        {
            get 
            {
                if (MioMonitoraggio <= 0) return "black";
                if (MioMonitoraggio <= 3) return "#999900";
                if (MioMonitoraggio <= 8) return "orange";
                if (MioMonitoraggio > 8) return "red";

                return "black";
            }
        }
    }

    public class RigaComune
    {
        public string Id { get; set; }
        public string Nome { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }

        public string IdProvincia { get; set; }
        public string IdRegione { get; set; }

        public string Colore
        {
            get
            {
                if (IntornoAMe <= 0) return "black";
                if (IntornoAMe <= 3) return "#999900";
                if (IntornoAMe <= 8) return "orange";
                if (IntornoAMe > 8) return "red";

                return "black";
            }
        }
    }

    public class RigaRegione
    {
        public string Id { get; set; }
        public string Nome { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }

        public string IdProvincia { get; set; }
        public string IdRegione { get; set; }

        public string Colore
        {
            get
            {
                if (IntornoAMe <= 0) return "black";
                if (IntornoAMe > 0) return "red";

                return "black";
            }
        }
    }

    public class RigaRischio
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal? MioMonitoraggio { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }
        public List<int> IdsVignetiControllati { get; set; }

        public decimal? Pressione
        {
            get
            {
                return MioMonitoraggio + IntornoAMe + Fenologia;
            }
        }
    }

    public class RigaVitigno
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal? MioMonitoraggio { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }
        public bool FenologiaDaComuni { get; set; }

        public decimal? Pressione
        {
            get
            {
                return MioMonitoraggio + IntornoAMe + Fenologia;
            }
        }
    }

    public class RigaVigneto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal? MioMonitoraggio { get; set; }
        public decimal? IntornoAMe { get; set; }
        public decimal? Fenologia { get; set; }
        public bool FenologiaDaComuni { get; set; }

        public decimal? Pressione
        {
            get
            {
                return MioMonitoraggio + IntornoAMe + Fenologia;
            }
        }
    }

    //------------------------ PROBABILITA ------------------------------
    public class ProbabilitaModel
    {        
        public int IdAvversita { get; set; }
        public string IdRegione { get; set; } = "-";
        public string IdProvincia { get; set; } = "-";
        public string IdComune { get; set; } = "-";
        public int IdAzienda { get; set; }
        public int IdGruppo { get; set; }
        public string Data { get; set; } = DateTime.Today.ToShortDateString();

        public SelectList Avversita { get; set; }
        public SelectList Regioni { get; set; }
        public SelectList Province { get; set; }
        public SelectList Comuni { get; set; }
        public SelectList Aziende { get; set; }
        public SelectList Gruppi { get; set; }

        public List<RigaProbabilita> Righe_PERONOSPORA { get; set; } = new List<RigaProbabilita>();
        public List<RigaProbabilita> Righe_OIDIO { get; set; } = new List<RigaProbabilita>();

        public DateTime DaGiorno { get; set; }
        public DateTime AGiorno { get; set; }
    }

    public class RigaProbabilita
    {
        public string IdComune { get; set; }
        public string Nome { get; set; }
        public decimal?[] Valori { get; set; } = new decimal?[5];
        public string[] Tooltips { get; set; } = new string[5];
    }

    //------------------------ PROTEZIONE ------------------------------
    public class ProtezioneModel
    {
        public string IdRegione { get; set; } = "-";
        public string IdProvincia { get; set; } = "-";
        public string IdComune { get; set; } = "-";
        public int IdAzienda { get; set; }
        public int IdGruppo { get; set; }
        public int IdVigna { get; set; }
        public int IdVigneto { get; set; }
        public int IdVitigno { get; set; }
        public int IdRischio { get; set; }
        public string Data { get; set; } = DateTime.Today.ToShortDateString();

        public SelectList Avversita { get; set; }
        public SelectList Regioni { get; set; }
        public SelectList Province { get; set; }
        public SelectList Comuni { get; set; }
        public SelectList Aziende { get; set; }
        public SelectList Gruppi { get; set; }
        public SelectList Vigne { get; set; }
        public SelectList Vigneti { get; set; }
        public SelectList Vitigni { get; set; }
        public SelectList Rischi { get; set; }

        public List<RigaProtezione> Righe_Aziende_PERONOSPORA { get; set; } = new List<RigaProtezione>();
        public List<RigaProtezione> Righe_Aziende_OIDIO { get; set; } = new List<RigaProtezione>();

        public List<RigaProtezione> Righe_Vigneti_PERONOSPORA { get; set; } = new List<RigaProtezione>();
        public List<RigaProtezione> Righe_Vigneti_OIDIO { get; set; } = new List<RigaProtezione>();

        public List<RigaProtezione> Righe_Rischio_PERONOSPORA { get; set; } = new List<RigaProtezione>();
        public List<RigaProtezione> Righe_Rischio_OIDIO { get; set; } = new List<RigaProtezione>();

        public DateTime DaGiorno { get; set; }
        public DateTime AGiorno { get; set; }
    }

    public class RigaProtezione
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal?[] Valori { get; set; } = new decimal?[7] { 0,0,0,0,0,0,0 };
        public string[] Tooltips { get; set; } = new string[7] { "0", "0", "0", "0", "0", "0", "0" };
    }

    public class ListaTrattamentiModel : jQueryDataTableParamModel
    {

    }

    //------------------------ TRATTAMENTI AUTOMATICI GIORNALIERI ------------------------------
    public class TrattamentiAutomaticiGiornalieriModel
    {
        public string IdRegione { get; set; } = "-";
        public string IdProvincia { get; set; } = "-";
        public string IdComune { get; set; } = "-";
        public int IdGruppo { get; set; }
        public int IdAzienda { get; set; }
        public int IdTipologiaLinea { get; set; }
        public int IdVigneto { get; set; }
        public int IdVitigno { get; set; }
        public int IdRischio { get; set; }
        //public string Data { get; set; } = DateTime.Today.ToShortDateString();

        public SelectList Avversita { get; set; }
        public SelectList Regioni { get; set; }
        public SelectList Province { get; set; }
        public SelectList Comuni { get; set; }
        public SelectList Aziende { get; set; }
        public SelectList Gruppi { get; set; }
        public SelectList TipologieLinee { get; set; }
        public SelectList Vigneti { get; set; }
        public SelectList Vitigni { get; set; }
        public SelectList Rischi { get; set; }        

        public List<RigaTrattamentiAutomaticiGiornalieri> RigheVigneti { get; set; } = new List<RigaTrattamentiAutomaticiGiornalieri>();
        public List<RigaTrattamentiAutomaticiGiornalieri> RigheEnti { get; set; } = new List<RigaTrattamentiAutomaticiGiornalieri>();

        public DateTime DaGiorno { get; set; }
        public DateTime AGiorno { get; set; }

        public int NumModelliProtezioneDaGenerare { get; set; }
    }

    public class RigaTrattamentiAutomaticiGiornalieri
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public List<Trattamento>[] Trattamenti { get; set; } = new List<Trattamento>[5];
        public string[] Testo { get; set; } = new string[5];
        public string[] Tooltips { get; set; } = new string[5];
        public string[] Colore { get; set; } = new string[5];
    }

    //------------------------ TRATTAMENTI SUGGERITI GIORNALIERI ------------------------------
    public class TrattamentiAziendaliGiornalieriModel
    {
        public string IdRegione { get; set; } = "-";
        public string IdProvincia { get; set; } = "-";
        public string IdComune { get; set; } = "-";
        public int IdAzienda { get; set; }
        public int IdTipologiaLinea { get; set; }
        public int IdVigneto { get; set; }
        public int IdVitigno { get; set; }
        public int IdRischio { get; set; }
        public int IdTipoTrattamento { get; set; }
        public int IdGruppo { get; set; }
        public int IdConsulente { get; set; }
        //public string Data { get; set; } = DateTime.Today.ToShortDateString();

        public SelectList Avversita { get; set; }
        public SelectList Regioni { get; set; }
        public SelectList Province { get; set; }
        public SelectList Comuni { get; set; }
        public SelectList Aziende { get; set; }
        public SelectList Consulenti { get; set; }
        public SelectList TipologieLinee { get; set; }
        public SelectList Vigneti { get; set; }
        public SelectList Vitigni { get; set; }
        public SelectList Rischi { get; set; }
        public SelectList TipiTrattamento { get; set; }
        public SelectList Gruppi { get; set; }        

        public SelectList Elenco_Rischi_Completo { get; set; }

        public List<RigaTrattamentiAziendaliGiornalieri> RigheVigneti { get; set; } = new List<RigaTrattamentiAziendaliGiornalieri>();
        public List<RigaTrattamentiAziendaliGiornalieri> RigheEnti { get; set; } = new List<RigaTrattamentiAziendaliGiornalieri>();

        public DateTime DaGiorno { get; set; }
        public DateTime AGiorno { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;
        public int NumModelliProtezioneDaGenerare { get; set; }
    }

    public class RigaTrattamentiAziendaliGiornalieri
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public List<Trattamento>[] Trattamenti { get; set; } = new List<Trattamento>[8];
        public string[] Testo { get; set; } = new string[8];
        public string[] Tooltips { get; set; } = new string[8];
        public string[] Colore { get; set; } = new string[8];
    }

    public class TrattamentoSimileComparer : IEqualityComparer<Trattamento>
    {
        public bool Equals(Trattamento x, Trattamento y)
        {
            var avversita_x = x.TrattamentoAvversita.Select(t => t.IdAvversitaDaTrattare).OrderBy(t => t).ToList();
            var avversita_y = y.TrattamentoAvversita.Select(t => t.IdAvversitaDaTrattare).OrderBy(t => t).ToList();

            var numreg_prdotti_x = x.TrattamentoAvversita.Select(t => t.AgrofarmacoUnivoco.NUM_REG).OrderBy(t => t).ToList();
            var numreg_prdotti_y = x.TrattamentoAvversita.Select(t => t.AgrofarmacoUnivoco.NUM_REG).OrderBy(t => t).ToList();

            return x.Data == y.Data && avversita_x.SequenceEqual(avversita_y) && numreg_prdotti_x.SequenceEqual(numreg_prdotti_y);
        }

        public int GetHashCode(Trattamento obj)
        {
            //var numreg_prdotti = obj.TrattamentoAvversita.Select(t => t.ProdottoVite.NumReg).ToList();            

            //return obj.Data.get;

            //return obj.Id.GetHashCode();
            return 0;
        }
    }
}
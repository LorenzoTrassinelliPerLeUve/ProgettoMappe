using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace _4GrapesAPI
{    
    public class RilievoLight
    {
        public int IdUtente { get; set; }
        public int IdEnte { get; set; }
        public DateTime IdRilievo { get; set; }
        public Nullable<int> Vigneto_IdVigneto { get; set; }
        public int IdTipoRilievo { get; set; }
        public int? IdMisuraConsiderata { get; set; }

        public Nullable<int> BBCH { get; set; }
        public Nullable<decimal> CatturePerTrappola { get; set; }
        public Nullable<decimal> NrGemmePerVite { get; set; }
        public Nullable<decimal> NrGermogliPerVite { get; set; }
        public Nullable<decimal> NrGrappoliPerVite { get; set; }
        public Nullable<decimal> LungMediaGermogliPerVite { get; set; }
        public Nullable<decimal> PesoLegnoPotaturaPerVite { get; set; }
        public Nullable<decimal> SFEPerVite { get; set; }
        public Nullable<decimal> PercGrappoliEspostiPerVite { get; set; }
        public Nullable<decimal> PotenzialeIdricoPerVite { get; set; }
        public Nullable<int> ValutazioneGlobaleVigneto { get; set; }
        public Nullable<decimal> PercFallanze { get; set; }
        public Nullable<decimal> Zuccheri { get; set; }
        public Nullable<decimal> PercDiffusione { get; set; }
        public Nullable<decimal> PercIntensità { get; set; }
        public Nullable<decimal> NrFormeMobiliNidi { get; set; }
        public Nullable<int> EtaEpocaGenerazione_idEtaEpocaGenerazione { get; set; }
        public Nullable<int> IdMaturitaTecnologicaDellaBacca { get; set; }
        public Nullable<int> IdMaturitaAromaticaDellaPolpa { get; set; }
        public Nullable<int> IdMaturitaDellaBuccia { get; set; }
        public Nullable<int> IdMaturitaDeiVinaccioli { get; set; }
        public Nullable<int> IdMorfologiaGrappolo { get; set; }
        public Nullable<int> IdStatoVegetativo { get; set; }
        public bool PresenzaAvversitaCarenza { get; set; }
        public Nullable<int> IdValutazioneStatoIdrico { get; set; }


        //---- COPIATA DA RILIEVO
        public decimal? Valore
        {
            get
            {
                //----- MI BASO SUL TIPO RILIEVO
                if (IdTipoRilievo == (int)TipoRilievo.Enum.MONITORAGGIO_CON_TRAPPOLE)
                {
                    return CatturePerTrappola;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.STADIO_FENOLOGICO ||
                    IdTipoRilievo == (int)TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH)
                {
                    return BBCH;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.GEMME_VITE)
                {
                    return NrGemmePerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.GERMOGLI_VITE)
                {
                    return NrGermogliPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.GRAPPOLI_VITE)
                {
                    return NrGrappoliPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.FALLANZE)
                {
                    return PercFallanze;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.SUPERFICIE_FOGLIARIA_ESPOSTA)
                {
                    return SFEPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.GRAPPOLI_ESPOSTI_AL_SOLE)
                {
                    return PercGrappoliEspostiPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.POTENZIALE_IDRICO_PIANTA_MPA)
                {
                    return PotenzialeIdricoPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.STATO_IDRICO_PIANTA)
                {
                    return IdValutazioneStatoIdrico;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.PESO_MEDIO_LEGNO_DI_POTATURA)
                {
                    return PesoLegnoPotaturaPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.LUNGHEZZA_GERMOGLIO)
                {
                    return LungMediaGermogliPerVite;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.VALUTAZIONE_GLOBALE_DEL_VIGNETO)
                {
                    return ValutazioneGlobaleVigneto;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.MATURAZIONE_UVE)
                {
                    return Zuccheri;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.ANALISI_SENSORIALE_ACINI)
                {
                    return ((IdMaturitaDellaBuccia ?? 1) + (IdMaturitaDeiVinaccioli ?? 1) + (IdMaturitaAromaticaDellaPolpa ?? 1) + (IdMaturitaTecnologicaDellaBacca ?? 1)) / 4m;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.MORFOLOGIA_GRAPPOLO)
                {
                    return IdMorfologiaGrappolo;
                }
                else if (IdTipoRilievo == (int)TipoRilievo.Enum.VIGORIA_DEL_VIGNETO)
                {
                    return IdStatoVegetativo;
                }
                //----- MI BASO SULLE MISURE CONSIDERATE
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.PERC_FALLANZE)
                {
                    return PercFallanze;
                }               
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.INTENSITA_PERC)
                {
                    return PercIntensità;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GEMME)
                {
                    return NrGemmePerVite;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GERMOGLI)
                {
                    return NrGermogliPerVite;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GRAPPOLI)
                {
                    return NrGrappoliPerVite;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_CATTURE)
                {
                    return CatturePerTrappola;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_FORME_MOBILI)
                {
                    return NrFormeMobiliNidi;
                }
                else if (IdMisuraConsiderata == (int)MisuraConsiderata.Enum.POTENZIALE)
                {
                    return PotenzialeIdricoPerVite;
                }
                else
                {
                    return PercDiffusione;
                }
            }
        }
    }

}
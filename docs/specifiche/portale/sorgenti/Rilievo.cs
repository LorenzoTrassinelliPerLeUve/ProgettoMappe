using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _4GrapesAPI.Entity
{    
    public partial class Rilievo
    {
        //--- IN CASO DI AGGIORNAMENTO DELLA FUNZIONE COPIARLA ANCHE IN RilievoLight
        [JsonIgnore]
        public decimal? Valore
        {
            get
            {
                if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.MONITORAGGIO_CON_TRAPPOLE)
                {
                    return CatturePerTrappola;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.STADIO_FENOLOGICO ||
                    TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH)
                {
                    return BBCH;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.GEMME_VITE)
                {
                    return NrGemmePerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.GERMOGLI_VITE)
                {
                    return NrGermogliPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.GRAPPOLI_VITE)
                {
                    return NrGrappoliPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.FALLANZE)
                {
                    return PercFallanze;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.SUPERFICIE_FOGLIARIA_ESPOSTA)
                {
                    return SFEPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.GRAPPOLI_ESPOSTI_AL_SOLE)
                {
                    return PercGrappoliEspostiPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.STATO_IDRICO_PIANTA_MIDDAY_MPA)
                {
                    return PotenzialeIdricoPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.PESO_MEDIO_LEGNO_DI_POTATURA)
                {
                    return PesoLegnoPotaturaPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.LUNGHEZZA_GERMOGLIO)
                {
                    return LungMediaGermogliPerVite;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.VALUTAZIONE_GLOBALE_DEL_VIGNETO)
                {
                    return ValutazioneGlobaleVigneto;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.MATURAZIONE_UVE)
                {
                    return Zuccheri;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.ANALISI_SENSORIALE_ACINI)
                {
                    return ((IdMaturitaDellaBuccia ?? 1) + (IdMaturitaDeiVinaccioli ?? 1) + (IdMaturitaAromaticaDellaPolpa ?? 1) + (IdMaturitaTecnologicaDellaBacca ?? 1)) / 4m;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.MORFOLOGIA_GRAPPOLO)
                {
                    return IdMorfologiaGrappolo;
                }
                else if (TipoRilievo_idTipoRilievo == (int)TipoRilievo.Enum.VIGORIA_DEL_VIGNETO)
                {
                    return StatoVegetativo_idStatoVegetativo;
                }
                //----- MI BASO SULLE MISURE CONSIDERATE
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.PERC_FALLANZE)
                {
                    return PercFallanze;
                }               
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.INTENSITA_PERC)
                {
                    return PercIntensità;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GEMME)
                {
                    return NrGemmePerVite;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GERMOGLI)
                {
                    return NrGermogliPerVite;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_GRAPPOLI)
                {
                    return NrGrappoliPerVite;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_CATTURE)
                {
                    return CatturePerTrappola;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.NUM_FORME_MOBILI)
                {
                    return NrFormeMobiliNidi;
                }
                else if (TipoRilievo?.IdMisuraConsiderata == (int)MisuraConsiderata.Enum.POTENZIALE)
                {
                    return PotenzialeIdricoPerVite;
                }      
                else
                {
                    return PercDiffusione;
                }
            }
        }

        public decimal PercCollezioneAssente()
        {
            if (Collezione.Count == 0)
            {
                return 0;
            }

            return Collezione.Count(x => x.ValoreConteggiato == 0) * 100.0m / Collezione.Count;
        }

        public decimal PercCollezioneInProduzione()
        {
            if (Collezione.Count == 0)
            {
                return 0;
            }

            return Collezione.Count(x => x.ValoreConteggiato == 1) * 100.0m / Collezione.Count;
        }

        public decimal PercCollezioneInAllevamento()
        {
            if (Collezione.Count == 0)
            {
                return 0;
            }

            return Collezione.Count(x => x.ValoreConteggiato == 2) * 100.0m / Collezione.Count;
        }

        public int? NumeroVitiMancanti()
        {
            if (IdSubVigneto != null)
            {
                return (int?)(SubVigneto?.NumeroDiPiante() * PercCollezioneAssente() / 100m);
            }
            else
            {
                return (int?)(Vigneto?.NumeroPiante * PercCollezioneAssente() / 100m);
            }            
        }

        public int? NumeroVitiInProduzione()
        {
            
            if (IdSubVigneto != null)
            {
                return (int?)(SubVigneto?.NumeroDiPiante() * PercCollezioneInProduzione() / 100m);
            }
            else
            {
                return (int?)(Vigneto?.NumeroPiante * PercCollezioneInProduzione() / 100m);
            }
        }

        public int? NumeroVitiInAllevamento()
        {            
            if (IdSubVigneto != null)
            {
                return (int?)(SubVigneto?.NumeroDiPiante() * PercCollezioneInAllevamento() / 100m);
            }
            else
            {
                return (int?)(Vigneto?.NumeroPiante * PercCollezioneInAllevamento() / 100m);
            }
        }

        public decimal? GrappoliStimati()
        {
            return NrGemmePerVite * FertilitaCaratteristica;
        }

        public decimal? ProduzioneStimataPerCeppoCalcolata()
        {
            var grappoli = NrGrappoliPerVite ?? GrappoliStimati();

            var num = grappoli * PesoMedioGrappolo / 1000m;
            if (num == null)
            {
                return null;                
            }

            return Math.Round((decimal)num, 2);
        }

        public decimal? ProduzioneStimataPerHaCalcolata()
        {
            var num = ProduzioneStimataPerCeppoCalcolata() * Vigneto?.DensitaPerEttaro();
            if (num == null)
            {
                return null;
            }

            return Math.Round((decimal)num);
        }

        public decimal? ProduzioneStimataPerVignetoCalcolata()
        {
            var num = ProduzioneStimataPerCeppoCalcolata() * Vigneto?.NumeroPiante;
            if (num == null)
            {
                return null;
            }

            return Math.Round((decimal)num);
        }

        public decimal? ProduzioneStimataPerHaSubVignetoCalcolata()
        {
            var num = ProduzioneStimataPerCeppoCalcolata() * SubVigneto?.DensitaPerEttaro();
            if (num == null)
            {
                return null;
            }

            return Math.Round((decimal)num);
        }

        public decimal? ProduzioneStimataPerSubVignetoCalcolata()
        {
            var num = ProduzioneStimataPerCeppoCalcolata() * SubVigneto?.NumeroDiPiante();
            if (num == null)
            {
                return null;
            }

            return Math.Round((decimal)num);
        }

        public SelectList GetVigneti()
        {
            using (var db = new DB())
            {
                var items = db.Vigneto.OrderBy(x => x.NomeVigneto).Select(x => new { Value = x.IdVigneto, Text = x.NomeVigneto }).ToList();
                return new SelectList(items, "Value", "Text", Vigneto_IdVigneto);
            }
        }
    }

}
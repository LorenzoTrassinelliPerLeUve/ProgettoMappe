using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using _4GrapesAPI.Entity;

namespace _4GrapesAPI
{

    public class ScaleColori
    {
        public enum Colore
        {
            Verde,
            VerdeScuro,
            VerdeChiaro,
            Giallo,
            Arancione,
            Rosso,
            RossoChiaro,
            RossoScuro,
            Viola,
            Bianco,
            Nero,
            Blu,
            Azzurro,
            Marrone,
        }

        public static class ColorePin
        {
            public static string VerdeChiaro = "green_2";
            public static string VerdeScuro = "green_1";
            public static string Arancione = "orange";
            public static string RossoChiaro = "red_1";
            public static string RossoScuro = "red_2";
            public static string Viola = "violet";
            public static string Giallo = "yellow";
            public static string Bianco = "white";
            public static string Nero = "black";
        }

        public static class TipoPin
        {
            public static string AmpelopatiePrincipali = "ampelopatie_cat_main";
            public static string AmpelopatieSecondarie = "ampelopatie_cat_sec";
            public static string AmpelopatieRare = "ampelopatie_cat_rare";
            public static string Fenologia = "fenologia";
            public static string Nutrizionale = "nutrizionale";
            public static string Organismi = "organismi";
            public static string Parametri = "parametri";
            public static string Sensori = "sensori";
            public static string Valutazione = "valutazione";
            public static string Generico = "allfilters";
        }

        public static class ColoreRGB
        {
            public static string VerdeChiaro = "#8ac53e";
            public static string VerdeScuro = "#009245";
            public static string Arancione = "#f6921e";
            public static string RossoChiaro = "#ef5b5b";
            public static string RossoScuro = "#d61820";
            public static string Viola = "#642c90";
            public static string Giallo = "#f8eb30";
            public static string Bianco = "#ffffff";
            public static string Nero = "#000000";

            public static string Fucsia = "#ea0089";
            public static string ArancioneScuro = "#fb5200";
            public static string Ocra = "#f8a701";
            public static string Azzurro = "#80d5fd";
            public static string Acquamarina = "#00a9d2";
            public static string Blu = "#004ddf";

            public static string Marrone = "#654321";
        }

        public static class ColoreVigoria
        {
            public static string Marroncino = "#c00000";
            public static string Rosso = "#fe0000";
            public static string Arancione = "#ffc000";
            public static string Giallo = "#ffff00";
            //public static string VerdeChiaro1 = "#9cd972";
            public static string VerdeChiaro2 = "#92d14f";
            public static string VerdeScuro1 = "#00af50";
            public static string VerdeScuro2 = "#076e13";
            public static string Viola = "#7030a0";

            public static string Bianco = "#ffffff";
        }

        public static readonly List<TipoRilievo.Enum> IdTipiRilievoMinMax = new List<TipoRilievo.Enum> { TipoRilievo.Enum.MONITORAGGIO_CON_TRAPPOLE, TipoRilievo.Enum.MATURAZIONE_UVE, TipoRilievo.Enum.STADIO_FENOLOGICO, TipoRilievo.Enum.GRAPPOLI_VITE, TipoRilievo.Enum.GERMOGLI_VITE, TipoRilievo.Enum.GEMME_VITE, TipoRilievo.Enum.PESO_MEDIO_LEGNO_DI_POTATURA, TipoRilievo.Enum.LUNGHEZZA_GERMOGLIO, TipoRilievo.Enum.FASI_FENOLOGICHE_BBCH,
            TipoRilievo.Enum.MONITORAGGIO_CICALINE, TipoRilievo.Enum.MONITORAGGIO_TIGNOLETTE, TipoRilievo.Enum.MONITORAGGIO_TIGNOLA, TipoRilievo.Enum.MONITORAGGIO_SCAFOIDEO, TipoRilievo.Enum.MONITORAGGIO_COCCINIGLIA_FARINOSA, TipoRilievo.Enum.MONITORAGGIO_TROPINOTA, TipoRilievo.Enum.MONITORAGGIO_CRYPTOBLABES, TipoRilievo.Enum.MONITORAGGIO_CASMOTECI, TipoRilievo.Enum.MONITORAGGIO_EULIA};

        /*decimal? CatturePerTrappola_Min;
        decimal? CatturePerTrappola_Max;
        decimal? Zuccheri_Min;
        decimal? Zuccheri_Max;
        decimal? BBCH_Min;
        decimal? BBCH_Max;
        decimal? NrGrappoliPerVite_Min;
        decimal? NrGrappoliPerVite_Max;
        decimal? NrGemmePerVite_Min;
        decimal? NrGemmePerVite_Max;
        decimal? NrGermogliPerVite_Min;
        decimal? NrGermogliPerVite_Max;
        decimal? PesoLegnoPotaturaPerVite_Min;
        decimal? PesoLegnoPotaturaPerVite_Max;
        decimal? LungMediaGermogliPerVite_Min;
        decimal? LungMediaGermogliPerVite_Max;*/

        //---- DA RIPORTARE IN RILIEVI LIGHT
        public Colore GetColore(Rilievo rilievo, List<Rilievo> rilievi)
        {
            switch ((TipoRilievo.Enum)rilievo.TipoRilievo_idTipoRilievo)
            {
                case TipoRilievo.Enum.OIDIO:
                case TipoRilievo.Enum.PERONOSPORA:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BOTRITE:
                case TipoRilievo.Enum.MARCIUME_NERO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 2.5m, 5, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MAL_DELL_ESCA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 2.5m, 5, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ESCORIOSI:
                case TipoRilievo.Enum.DANNI_DA_GELO:
                case TipoRilievo.Enum.DANNI_DA_GRANDINE:
                case TipoRilievo.Enum.AGENTE_DI_FITOTOSSICITA:
                case TipoRilievo.Enum.MELATA:
                case TipoRilievo.Enum.SCOTTATURA:
                case TipoRilievo.Enum.ALTRO_33:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BERRY_SHRIVEL:
                case TipoRilievo.Enum.DISSECCAMENTO_RACHIDE:
                case TipoRilievo.Enum.TUMORE_BATTERICO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 0.5m, 2, 5, 10, 25, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DANNO_DA_VENTO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DANNO_DA_CICALINA_VERDE:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 20, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.APPASSIMENTO:
                case TipoRilievo.Enum.ESTRUSIONE:
                case TipoRilievo.Enum.DANNO_DA_UCCELLI:
                case TipoRilievo.Enum.DANNO_DA_UNGULATI:
                case TipoRilievo.Enum.DANNO_MECCANICO:
                case TipoRilievo.Enum.DANNO_DA_DISERBO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BATTERIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 100, 100, 100, Colore.VerdeScuro, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola, Colore.Viola, Colore.Viola);
                case TipoRilievo.Enum.CARIE_BIANCA:
                case TipoRilievo.Enum.EUTIPIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ARMILLARIA_SPP:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 100, 100, 100, Colore.VerdeScuro, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola, Colore.Viola, Colore.Viola);

                case TipoRilievo.Enum.INSETTI_GEMMIVORI:
                case TipoRilievo.Enum.CIMICE_VERDE_DELLA_VITE:
                case TipoRilievo.Enum.OZIORRINCO:
                case TipoRilievo.Enum.BOSTRICO_DELLA_VITE:
                case TipoRilievo.Enum.SIGARAIO_DELLA_VITE:
                case TipoRilievo.Enum.FILLOSSERA_DELLA_VITE:
                case TipoRilievo.Enum.METCALFA:
                case TipoRilievo.Enum.ERINEUM_MITE:
                case TipoRilievo.Enum.WILLAMETTE_MITE:
                case TipoRilievo.Enum.PACIFIC_MITE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.TIGNOLE:
                    switch ((EtaEpocaGenerazione.Enum)(rilievo.EtaEpocaGenerazione_idEtaEpocaGenerazione ?? -1))
                    {
                        case EtaEpocaGenerazione.Enum.TIGNOLE_I_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 50, 100, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                        case EtaEpocaGenerazione.Enum.TIGNOLE_II_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 2, 4, 8, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                        case EtaEpocaGenerazione.Enum.TIGNOLE_III_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 1, 2, 4, 5, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                    }
                    break;
                case TipoRilievo.Enum.RAGNETTO_ROSSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 2, 5, 10, 15, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.RAGNETTO_GIALLO:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 15, 30, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERIOFIDE_ACARIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 10, 25, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERIOFIDE_ERINOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 25, 70, 80, 100, 100, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoChiaro, Colore.RossoChiaro);
                case TipoRilievo.Enum.TRIPIDE_DELLA_VITE:
                case TipoRilievo.Enum.ASPERGILLUS_SUPP:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 15, 30, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_ACIDO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.CICALINA_VERDE:
                    return GetColorePin(rilievo.PercIntensità, 0, 0.2m, 0.5m, 1, 2, 4, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.CICALINA_GIALLA:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 3, 5, 7, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.EULIA:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_PHYLLOCNISTIS_VITEGENELLA:
                case TipoRilievo.Enum.MAGGIOLINO:
                case TipoRilievo.Enum.ALTRO_59:
                case TipoRilievo.Enum.TROPINOTA_HIRTA:
                case TipoRilievo.Enum.TORTRICE_DELLA_VITE:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_HOLOCACISTA_RIVILLEI:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_ANTISPILA_OINOPHYLLA:
                case TipoRilievo.Enum.CIMICE_MARMORATA_asiatica:
                case TipoRilievo.Enum.DANNO_DA_APOIDEI:
                case TipoRilievo.Enum.IFANTRIA:
                case TipoRilievo.Enum.FORMICHE:
                case TipoRilievo.Enum.LACNAIA:
                case TipoRilievo.Enum.AFIDI:
                case TipoRilievo.Enum.CRYPTOBLABES_GNIDIELLA:
                case TipoRilievo.Enum.ZIGENA_DELLA_VITE:
                case TipoRilievo.Enum.CICADELLA_BUFALO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.SCAFOIDEO:
                    return GetColorePin(rilievo.NrFormeMobiliNidi, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERYTHRONEURA_VULNERATA:
                case TipoRilievo.Enum.COCCINIGLIA_DEL_CORNIOLO:
                case TipoRilievo.Enum.COCCINIGLIA_FARINOSA:
                case TipoRilievo.Enum.COCCINIGLIA_DELLA_VITE:
                case TipoRilievo.Enum.PARTHENOLECANIUM_PERSICAE:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ELIOCOCCO_DI_BOEMIA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 2, 10, 25, 50, 75, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VIROSI_DEL_PINOT_GRIGIO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 2, 5, 10, 25, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                //case TipoRilievo.Enum.GIALLUME_DELLA_VITE:
                case TipoRilievo.Enum.LEGNO_NERO_FLAVESCENZA_DORATA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 0.5m, 1, 2.5m, 5, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MOSAICO_GIALLO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ACCARTOCCIAMENTO_FOGLIARE_GLRV:
                case TipoRilievo.Enum.ARRICCIAMENTO_FOGLIARE:
                    return GetColorePin(rilievo.PercDiffusione, 0, 2, 10, 25, 50, 75, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STATO_NUTRIZIONALE_E_CARENZE:
                    return rilievo.PresenzaAvversitaCarenza ? Colore.VerdeScuro : Colore.RossoScuro;
                case TipoRilievo.Enum.MONITORAGGIO_CON_TRAPPOLE:
                    {
                        /*if (CatturePerTrappola_Min == null)
                        {
                            CatturePerTrappola_Min = rilievi.Where(x => x.CatturePerTrappola != null).Min(x => (decimal)x.CatturePerTrappola);
                            CatturePerTrappola_Max = rilievi.Where(x => x.CatturePerTrappola != null).Max(x => (decimal)x.CatturePerTrappola);
                        }
                        
                        return GetColorePinMinMax(rilievo.CatturePerTrappola, (decimal)CatturePerTrappola_Min, (decimal)CatturePerTrappola_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.TRIPIDI_PREDATORI:
                case TipoRilievo.Enum.FITOSEIDI:
                case TipoRilievo.Enum.COCCINELLIDI:
                case TipoRilievo.Enum.CRISOPIDI:
                case TipoRilievo.Enum.ICNEUMONIDI:
                case TipoRilievo.Enum.RAGNI:
                case TipoRilievo.Enum.ORIUS:
                case TipoRilievo.Enum.CANTARIDI:
                case TipoRilievo.Enum.ANAGYRUS:
                case TipoRilievo.Enum.ALTRO_104:
                case TipoRilievo.Enum.TROMBIDIDI:
                    return GetColorePin(rilievo.NrFormeMobiliNidi, 0, 1, 2, 3, 4, 5, 6, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MATURAZIONE_UVE:
                    {
                        /*if (Zuccheri_Min == null)
                        {
                            Zuccheri_Min = rilievi.Where(x => x.Zuccheri != null).Min(x => (decimal)x.Zuccheri);
                            Zuccheri_Max = rilievi.Where(x => x.Zuccheri != null).Max(x => (decimal)x.Zuccheri);
                        }

                        return GetColorePinMinMax(rilievo.Zuccheri, (decimal)Zuccheri_Min, (decimal)Zuccheri_Max, Colore.Viola, Colore.RossoScuro, Colore.RossoChiaro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);*/
                        break;
                    }
                case TipoRilievo.Enum.ANALISI_SENSORIALE_ACINI:
                    {
                        if (rilievo.IdMaturitaTecnologicaDellaBacca == null || rilievo.IdMaturitaAromaticaDellaPolpa == null || rilievo.IdMaturitaDellaBuccia == null || rilievo.IdMaturitaDeiVinaccioli == null)
                        {
                            return Colore.Viola;
                        }
                        else if (rilievo.IdMaturitaTecnologicaDellaBacca == 5 || rilievo.IdMaturitaAromaticaDellaPolpa == 5 || rilievo.IdMaturitaDellaBuccia == 5 || rilievo.IdMaturitaDeiVinaccioli == 5)
                        {
                            return Colore.Viola;
                        }
                        else
                        {
                            var media = (int)Math.Round(((int)rilievo.IdMaturitaTecnologicaDellaBacca + (int)rilievo.IdMaturitaAromaticaDellaPolpa + (int)rilievo.IdMaturitaDellaBuccia + (int)rilievo.IdMaturitaDeiVinaccioli) / 4d);
                            switch(media)
                            {
                                case 1: return Colore.VerdeScuro;
                                case 2: return Colore.VerdeChiaro;
                                case 3: return Colore.Giallo;
                                case 4: return Colore.Arancione;
                            }
                        }

                        break;
                    }
                case TipoRilievo.Enum.MORFOLOGIA_GRAPPOLO:
                    return GetColorePin(rilievo.IdMorfologiaGrappolo, 1, 2, 3, 4, 5, 6, 7, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STADIO_FENOLOGICO:
                    {
                        /*if (BBCH_Min == null)
                        {
                            BBCH_Min = rilievi.Where(x => x.BBCH != null).Min(x => (decimal)x.BBCH);
                            BBCH_Max = rilievi.Where(x => x.BBCH != null).Max(x => (decimal)x.BBCH);
                        }

                        return GetColorePinMinMax(rilievo.BBCH, (decimal)BBCH_Min, (decimal)BBCH_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GRAPPOLI_VITE:
                    {
                        /*if (NrGrappoliPerVite_Min == null)
                        {
                            NrGrappoliPerVite_Min = rilievi.Where(x => x.NrGrappoliPerVite != null).Min(x => (decimal)x.NrGrappoliPerVite);
                            NrGrappoliPerVite_Max  = rilievi.Where(x => x.NrGrappoliPerVite != null).Max(x => (decimal)x.NrGrappoliPerVite);
                        }
                        return GetColorePinMinMax(rilievo.NrGrappoliPerVite, (decimal)NrGrappoliPerVite_Min, (decimal)NrGrappoliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GEMME_VITE:
                    {
                        /*if (NrGrappoliPerVite_Min == null)
                        {
                            NrGrappoliPerVite_Min = rilievi.Where(x => x.NrGemmePerVite != null).Min(x => (decimal)x.NrGemmePerVite);
                            NrGrappoliPerVite_Max = rilievi.Where(x => x.NrGemmePerVite != null).Max(x => (decimal)x.NrGemmePerVite);
                        }
                        return GetColorePinMinMax(rilievo.NrGemmePerVite, (decimal)NrGrappoliPerVite_Min, (decimal)NrGrappoliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GERMOGLI_VITE:
                    {
                        /*if (NrGermogliPerVite_Min == null)
                        {
                            NrGermogliPerVite_Min = rilievi.Where(x => x.NrGermogliPerVite != null).Min(x => (decimal)x.NrGermogliPerVite);
                            NrGermogliPerVite_Max = rilievi.Where(x => x.NrGermogliPerVite != null).Max(x => (decimal)x.NrGermogliPerVite);
                        }
                        return GetColorePinMinMax(rilievo.NrGermogliPerVite, (decimal)NrGermogliPerVite_Min, (decimal)NrGermogliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.SUPERFICIE_FOGLIARIA_ESPOSTA:
                    return GetColorePin(rilievo.SFEPerVite, 0.5m, 1, 2, 3, 4, 5, 6, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione);
                case TipoRilievo.Enum.GRAPPOLI_ESPOSTI_AL_SOLE:
                    return GetColorePin(rilievo.PercGrappoliEspostiPerVite, 0, 10, 25, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);                
                case TipoRilievo.Enum.FALLANZE:
                    return GetColorePin(rilievo.PercFallanze, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.PESO_MEDIO_LEGNO_DI_POTATURA:
                    {
                        /*if (NrGermogliPerVite_Min == null)
                        {
                            PesoLegnoPotaturaPerVite_Min = rilievi.Where(x => x.PesoLegnoPotaturaPerVite != null).Min(x => (decimal)x.PesoLegnoPotaturaPerVite);
                            PesoLegnoPotaturaPerVite_Max = rilievi.Where(x => x.PesoLegnoPotaturaPerVite != null).Max(x => (decimal)x.PesoLegnoPotaturaPerVite);
                        }
                        return GetColorePinMinMax(rilievo.PesoLegnoPotaturaPerVite, (decimal)PesoLegnoPotaturaPerVite_Min, (decimal)PesoLegnoPotaturaPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.LUNGHEZZA_GERMOGLIO:
                    {
                        /*if (LungMediaGermogliPerVite_Min == null)
                        {
                            LungMediaGermogliPerVite_Min = rilievi.Where(x => x.LungMediaGermogliPerVite != null).Min(x => (decimal)x.LungMediaGermogliPerVite);
                            LungMediaGermogliPerVite_Max = rilievi.Where(x => x.LungMediaGermogliPerVite != null).Max(x => (decimal)x.LungMediaGermogliPerVite);
                        }
                        return GetColorePinMinMax(rilievo.LungMediaGermogliPerVite, (decimal)LungMediaGermogliPerVite_Min, (decimal)LungMediaGermogliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.VIGORIA_DEL_VIGNETO:
                    {
                        switch ((StatoVegetativo.Enum)rilievo.StatoVegetativo_idStatoVegetativo)
                        {
                            case StatoVegetativo.Enum.VIROGIA_MOLTO_BASSA:
                                return Colore.RossoScuro;
                            case StatoVegetativo.Enum.VIGORIA_BASSA:
                                return Colore.Arancione;
                            case StatoVegetativo.Enum.VIGORIA_MEDIO_BASSA:
                                return Colore.Giallo;
                            case StatoVegetativo.Enum.VIGORIA_MEDIA:
                                return Colore.VerdeChiaro;
                            case StatoVegetativo.Enum.VIGORIA_MEDIO_ALTA:
                                return Colore.VerdeScuro;
                            case StatoVegetativo.Enum.VIGORIA_ALTA:
                                return Colore.VerdeChiaro;
                            case StatoVegetativo.Enum.VIGORIA_ECCESSIVA:
                                return Colore.Viola;
                            default:
                                return GetColorePin(rilievo.StatoVegetativo_idStatoVegetativo, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                        }
                    }                    
                case TipoRilievo.Enum.IMMISSIONE_UTILI:
                    return Colore.VerdeScuro;
                case TipoRilievo.Enum.MONITORAGGIO_CON_SENSORI:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.APPLICAZIONE_CONFUSIONE_SESSUALE:
                    return Colore.VerdeScuro;
                case TipoRilievo.Enum.GESTIONE_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_INTERFILA:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_SOTTOFILA:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BIODIVERSITA_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VALUTAZIONE_GLOBALE_DEL_VIGNETO:
                    return GetColorePin(rilievo.ValutazioneGlobaleVigneto, 1, 3, 5, 6, 8, 9, 10, Colore.Viola, Colore.RossoScuro, Colore.RossoChiaro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);
                case TipoRilievo.Enum.STORY_TELLING:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ANTRACNOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUMI_SECONDARI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_RADICALE_FIBROSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_RADICALE_LANOSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MELANOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.PIEDE_NERO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ROSSORE_PARASSITARIO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.RUGGINE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VERTICILLIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MALATTIA_DI_PIERCE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA:
                {
                    switch ((ValutazioneStatoIdrico.Enum)rilievo.IdValutazioneStatoIdrico)
                    {
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_ASSENTE:
                            return Colore.VerdeScuro;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_LIEVE:
                            return Colore.VerdeChiaro;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_MEDIO:
                            return Colore.Giallo;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_SEVERO:
                            return Colore.Arancione;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_MOLTO_SEVERO:
                            return Colore.RossoScuro;                        
                        default:
                            return Colore.Bianco;
                    }
                }
                //case TipoRilievo.Enum.STATO_IDRICO_PIANTA_MIDDAY_MPA:
                //case TipoRilievo.Enum.STATO_IDRICO_PIANTA_PREDAWN_MPA:
                //return GetColorePin(rilievo.PotenzialeIdricoPerVite, 0.3m, 0.5m, 0.8m, 1, 1.3m, 1.5m, 1.8m, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                //return GetColorePin(rilievo.PotenzialeIdricoPerVite, -0.6m, -0.4m, -0.3m, -0.2m, 0, Colore.RossoScuro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA_MIDDAY_MPA:
                    return GetColorePin(rilievo.PotenzialeIdricoPerVite, -1.4m, -1.1m, -0.8m, -0.5m, 0, Colore.RossoScuro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA_PREDAWN_MPA:
                    return GetColorePin(rilievo.PotenzialeIdricoPerVite, -0.8m, -0.6m, -0.4m, -0.2m, 0, Colore.RossoScuro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);

            }
            
            if (rilievo.PercDiffusione != null)
            {
                return GetColorePin(rilievo.PercDiffusione, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
            }

            return Colore.VerdeScuro; //--- DA VERIFICARE
        }

        //---- RICORDARSI DI RIPORTARE IN GetColore (RILIEVO)
        public Colore GetColore(RilievoLight rilievo, List<RilievoLight> rilievi)
        {
            switch ((TipoRilievo.Enum)rilievo.IdTipoRilievo)
            {
                case TipoRilievo.Enum.OIDIO:
                case TipoRilievo.Enum.PERONOSPORA:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BOTRITE:
                case TipoRilievo.Enum.MARCIUME_NERO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 2.5m, 5, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MAL_DELL_ESCA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 2.5m, 5, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ESCORIOSI:
                case TipoRilievo.Enum.DANNI_DA_GELO:
                case TipoRilievo.Enum.DANNI_DA_GRANDINE:
                case TipoRilievo.Enum.AGENTE_DI_FITOTOSSICITA:
                case TipoRilievo.Enum.MELATA:
                case TipoRilievo.Enum.SCOTTATURA:
                case TipoRilievo.Enum.ALTRO_33:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BERRY_SHRIVEL:
                case TipoRilievo.Enum.DISSECCAMENTO_RACHIDE:
                case TipoRilievo.Enum.TUMORE_BATTERICO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 0.5m, 2, 5, 10, 25, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DANNO_DA_VENTO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DANNO_DA_CICALINA_VERDE:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 20, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.APPASSIMENTO:
                case TipoRilievo.Enum.ESTRUSIONE:
                case TipoRilievo.Enum.DANNO_DA_UCCELLI:
                case TipoRilievo.Enum.DANNO_DA_UNGULATI:
                case TipoRilievo.Enum.DANNO_MECCANICO:
                case TipoRilievo.Enum.DANNO_DA_DISERBO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BATTERIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 100, 100, 100, Colore.VerdeScuro, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola, Colore.Viola, Colore.Viola);
                case TipoRilievo.Enum.CARIE_BIANCA:
                case TipoRilievo.Enum.EUTIPIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ARMILLARIA_SPP:
                    return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 100, 100, 100, Colore.VerdeScuro, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola, Colore.Viola, Colore.Viola);

                case TipoRilievo.Enum.INSETTI_GEMMIVORI:
                case TipoRilievo.Enum.CIMICE_VERDE_DELLA_VITE:
                case TipoRilievo.Enum.OZIORRINCO:
                case TipoRilievo.Enum.BOSTRICO_DELLA_VITE:
                case TipoRilievo.Enum.SIGARAIO_DELLA_VITE:
                case TipoRilievo.Enum.FILLOSSERA_DELLA_VITE:
                case TipoRilievo.Enum.METCALFA:
                case TipoRilievo.Enum.ERINEUM_MITE:
                case TipoRilievo.Enum.WILLAMETTE_MITE:
                case TipoRilievo.Enum.PACIFIC_MITE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.TIGNOLE:
                    switch ((EtaEpocaGenerazione.Enum)(rilievo.EtaEpocaGenerazione_idEtaEpocaGenerazione ?? -1))
                    {
                        case EtaEpocaGenerazione.Enum.TIGNOLE_I_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 5, 10, 25, 50, 100, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                        case EtaEpocaGenerazione.Enum.TIGNOLE_II_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 2, 4, 8, 10, 20, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                        case EtaEpocaGenerazione.Enum.TIGNOLE_III_GENERAZIONE:
                            return GetColorePin(rilievo.PercIntensità, 0, 1, 2, 4, 5, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                    }
                    break;
                case TipoRilievo.Enum.RAGNETTO_ROSSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 2, 5, 10, 15, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.RAGNETTO_GIALLO:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 15, 30, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERIOFIDE_ACARIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 10, 25, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERIOFIDE_ERINOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 25, 70, 80, 100, 100, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoChiaro, Colore.RossoChiaro);
                case TipoRilievo.Enum.TRIPIDE_DELLA_VITE:
                case TipoRilievo.Enum.ASPERGILLUS_SUPP:
                    return GetColorePin(rilievo.PercIntensità, 0, 2, 5, 10, 15, 30, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_ACIDO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.CICALINA_VERDE:
                    return GetColorePin(rilievo.PercIntensità, 0, 0.2m, 0.5m, 1, 2, 4, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.CICALINA_GIALLA:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 3, 5, 7, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.EULIA:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_PHYLLOCNISTIS_VITEGENELLA:
                case TipoRilievo.Enum.MAGGIOLINO:
                case TipoRilievo.Enum.ALTRO_59:
                case TipoRilievo.Enum.TROPINOTA_HIRTA:
                case TipoRilievo.Enum.TORTRICE_DELLA_VITE:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_HOLOCACISTA_RIVILLEI:
                case TipoRilievo.Enum.MINATORE_FOGLIARE_ANTISPILA_OINOPHYLLA:
                case TipoRilievo.Enum.CIMICE_MARMORATA_asiatica:
                case TipoRilievo.Enum.DANNO_DA_APOIDEI:
                case TipoRilievo.Enum.IFANTRIA:
                case TipoRilievo.Enum.FORMICHE:
                case TipoRilievo.Enum.LACNAIA:
                case TipoRilievo.Enum.AFIDI:
                case TipoRilievo.Enum.CRYPTOBLABES_GNIDIELLA:
                case TipoRilievo.Enum.ZIGENA_DELLA_VITE:
                case TipoRilievo.Enum.CICADELLA_BUFALO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.SCAFOIDEO:
                    return GetColorePin(rilievo.NrFormeMobiliNidi, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ERYTHRONEURA_VULNERATA:
                case TipoRilievo.Enum.COCCINIGLIA_DEL_CORNIOLO:
                case TipoRilievo.Enum.COCCINIGLIA_FARINOSA:
                case TipoRilievo.Enum.COCCINIGLIA_DELLA_VITE:
                case TipoRilievo.Enum.PARTHENOLECANIUM_PERSICAE:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ELIOCOCCO_DI_BOEMIA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 2, 10, 25, 50, 75, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VIROSI_DEL_PINOT_GRIGIO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 2, 5, 10, 25, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                //case TipoRilievo.Enum.GIALLUME_DELLA_VITE:
                case TipoRilievo.Enum.LEGNO_NERO_FLAVESCENZA_DORATA:
                    return GetColorePin(rilievo.PercDiffusione, 0, 0.5m, 1, 2.5m, 5, 10, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MOSAICO_GIALLO:
                    return GetColorePin(rilievo.PercDiffusione, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ACCARTOCCIAMENTO_FOGLIARE_GLRV:
                case TipoRilievo.Enum.ARRICCIAMENTO_FOGLIARE:
                    return GetColorePin(rilievo.PercDiffusione, 0, 2, 10, 25, 50, 75, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STATO_NUTRIZIONALE_E_CARENZE:
                    return rilievo.PresenzaAvversitaCarenza ? Colore.VerdeScuro : Colore.RossoScuro;
                case TipoRilievo.Enum.MONITORAGGIO_CON_TRAPPOLE:
                    {
                        /*if (CatturePerTrappola_Min == null)
                        {
                            CatturePerTrappola_Min = rilievi.Where(x => x.CatturePerTrappola != null).Min(x => (decimal)x.CatturePerTrappola);
                            CatturePerTrappola_Max = rilievi.Where(x => x.CatturePerTrappola != null).Max(x => (decimal)x.CatturePerTrappola);
                        }

                        return GetColorePinMinMax(rilievo.CatturePerTrappola, (decimal)CatturePerTrappola_Min, (decimal)CatturePerTrappola_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.TRIPIDI_PREDATORI:
                case TipoRilievo.Enum.FITOSEIDI:
                case TipoRilievo.Enum.COCCINELLIDI:
                case TipoRilievo.Enum.CRISOPIDI:
                case TipoRilievo.Enum.ICNEUMONIDI:
                case TipoRilievo.Enum.RAGNI:
                case TipoRilievo.Enum.ORIUS:
                case TipoRilievo.Enum.CANTARIDI:
                case TipoRilievo.Enum.ANAGYRUS:
                case TipoRilievo.Enum.ALTRO_104:
                case TipoRilievo.Enum.TROMBIDIDI:
                    return GetColorePin(rilievo.NrFormeMobiliNidi, 0, 1, 2, 3, 4, 5, 6, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MATURAZIONE_UVE:
                    {
                        /*if (Zuccheri_Min == null)
                        {
                            Zuccheri_Min = rilievi.Where(x => x.Zuccheri != null).Min(x => (decimal)x.Zuccheri);
                            Zuccheri_Max = rilievi.Where(x => x.Zuccheri != null).Max(x => (decimal)x.Zuccheri);
                        }

                        return GetColorePinMinMax(rilievo.Zuccheri, (decimal)Zuccheri_Min, (decimal)Zuccheri_Max, Colore.Viola, Colore.RossoScuro, Colore.RossoChiaro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);*/
                        break;
                    }
                case TipoRilievo.Enum.ANALISI_SENSORIALE_ACINI:
                    { 
                        if (rilievo.IdMaturitaTecnologicaDellaBacca == null || rilievo.IdMaturitaAromaticaDellaPolpa == null || rilievo.IdMaturitaDellaBuccia == null || rilievo.IdMaturitaDeiVinaccioli == null)
                        {
                            return Colore.Viola;
                        }
                        else if (rilievo.IdMaturitaTecnologicaDellaBacca == 5 || rilievo.IdMaturitaAromaticaDellaPolpa == 5 || rilievo.IdMaturitaDellaBuccia == 5 || rilievo.IdMaturitaDeiVinaccioli == 5)
                        {
                            return Colore.Viola;
                        }
                        else
                        {
                            var media = (int)Math.Round(((int)rilievo.IdMaturitaTecnologicaDellaBacca + (int)rilievo.IdMaturitaAromaticaDellaPolpa + (int)rilievo.IdMaturitaDellaBuccia + (int)rilievo.IdMaturitaDeiVinaccioli) / 4d);
                            switch (media)
                            {
                                case 1: return Colore.VerdeScuro;
                                case 2: return Colore.VerdeChiaro;
                                case 3: return Colore.Giallo;
                                case 4: return Colore.Arancione;
                            }
                        }

                        break;
                    }
                case TipoRilievo.Enum.MORFOLOGIA_GRAPPOLO:
                    return GetColorePin(rilievo.IdMorfologiaGrappolo, 1, 2, 3, 4, 5, 6, 7, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STADIO_FENOLOGICO:
                    {
                        /*if (BBCH_Min == null)
                        {
                            BBCH_Min = rilievi.Where(x => x.BBCH != null).Min(x => (decimal)x.BBCH);
                            BBCH_Max = rilievi.Where(x => x.BBCH != null).Max(x => (decimal)x.BBCH);
                        }

                        return GetColorePinMinMax(rilievo.BBCH, (decimal)BBCH_Min, (decimal)BBCH_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GRAPPOLI_VITE:
                    {
                        /*if (NrGrappoliPerVite_Min == null)
                        {
                            NrGrappoliPerVite_Min = rilievi.Where(x => x.NrGrappoliPerVite != null).Min(x => (decimal)x.NrGrappoliPerVite);
                            NrGrappoliPerVite_Max = rilievi.Where(x => x.NrGrappoliPerVite != null).Max(x => (decimal)x.NrGrappoliPerVite);
                        }
                        return GetColorePinMinMax(rilievo.NrGrappoliPerVite, (decimal)NrGrappoliPerVite_Min, (decimal)NrGrappoliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GEMME_VITE:
                    {
                        /*if (NrGrappoliPerVite_Min == null)
                        {
                            NrGrappoliPerVite_Min = rilievi.Where(x => x.NrGemmePerVite != null).Min(x => (decimal)x.NrGemmePerVite);
                            NrGrappoliPerVite_Max = rilievi.Where(x => x.NrGemmePerVite != null).Max(x => (decimal)x.NrGemmePerVite);
                        }
                        return GetColorePinMinMax(rilievo.NrGemmePerVite, (decimal)NrGrappoliPerVite_Min, (decimal)NrGrappoliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.GERMOGLI_VITE:
                    {
                        /*if (NrGermogliPerVite_Min == null)
                        {
                            NrGermogliPerVite_Min = rilievi.Where(x => x.NrGermogliPerVite != null).Min(x => (decimal)x.NrGermogliPerVite);
                            NrGermogliPerVite_Max = rilievi.Where(x => x.NrGermogliPerVite != null).Max(x => (decimal)x.NrGermogliPerVite);
                        }
                        reurn GetColorePinMinMax(rilievo.NrGermogliPerVite, (decimal)NrGermogliPerVite_Min, (decimal)NrGermogliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.SUPERFICIE_FOGLIARIA_ESPOSTA:
                    return GetColorePin(rilievo.SFEPerVite, 0.5m, 1, 2, 3, 4, 5, 6, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione);
                case TipoRilievo.Enum.GRAPPOLI_ESPOSTI_AL_SOLE:
                    return GetColorePin(rilievo.PercGrappoliEspostiPerVite, 0, 10, 25, 50, 75, 90, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);                
                case TipoRilievo.Enum.FALLANZE:
                    return GetColorePin(rilievo.PercFallanze, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.PESO_MEDIO_LEGNO_DI_POTATURA:
                    {
                        /*if (PesoLegnoPotaturaPerVite_Min == null)
                        {
                            PesoLegnoPotaturaPerVite_Min = rilievi.Where(x => x.PesoLegnoPotaturaPerVite != null).Min(x => (decimal)x.PesoLegnoPotaturaPerVite);
                            PesoLegnoPotaturaPerVite_Max = rilievi.Where(x => x.PesoLegnoPotaturaPerVite != null).Max(x => (decimal)x.PesoLegnoPotaturaPerVite);
                        }
                        return GetColorePinMinMax(rilievo.PesoLegnoPotaturaPerVite, (decimal)PesoLegnoPotaturaPerVite_Min, (decimal)PesoLegnoPotaturaPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.LUNGHEZZA_GERMOGLIO:
                    {
                        /*if (LungMediaGermogliPerVite_Min == null)
                        {
                            LungMediaGermogliPerVite_Min = rilievi.Where(x => x.LungMediaGermogliPerVite != null).Min(x => (decimal)x.LungMediaGermogliPerVite);
                            LungMediaGermogliPerVite_Max = rilievi.Where(x => x.LungMediaGermogliPerVite != null).Max(x => (decimal)x.LungMediaGermogliPerVite);
                        }
                        return GetColorePinMinMax(rilievo.LungMediaGermogliPerVite, (decimal)LungMediaGermogliPerVite_Min, (decimal)LungMediaGermogliPerVite_Max, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);*/
                        break;
                    }
                case TipoRilievo.Enum.VIGORIA_DEL_VIGNETO:
                    {
                        switch ((StatoVegetativo.Enum)rilievo.IdStatoVegetativo)
                        {
                            case StatoVegetativo.Enum.VIROGIA_MOLTO_BASSA:
                                return Colore.RossoScuro;
                            case StatoVegetativo.Enum.VIGORIA_BASSA:
                                return Colore.Arancione;
                            case StatoVegetativo.Enum.VIGORIA_MEDIO_BASSA:
                                return Colore.Giallo;
                            case StatoVegetativo.Enum.VIGORIA_MEDIA:
                                return Colore.VerdeChiaro;
                            case StatoVegetativo.Enum.VIGORIA_MEDIO_ALTA:
                                return Colore.VerdeScuro;
                            case StatoVegetativo.Enum.VIGORIA_ALTA:
                                return Colore.VerdeChiaro;
                            case StatoVegetativo.Enum.VIGORIA_ECCESSIVA:
                                return Colore.Viola;
                            default:
                                return GetColorePin(rilievo.IdStatoVegetativo, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                    }
                    }                    
                case TipoRilievo.Enum.IMMISSIONE_UTILI:
                    return Colore.VerdeScuro;
                case TipoRilievo.Enum.MONITORAGGIO_CON_SENSORI:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.APPLICAZIONE_CONFUSIONE_SESSUALE:
                    return Colore.VerdeScuro;
                case TipoRilievo.Enum.GESTIONE_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_INTERFILA:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_SOTTOFILA:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.BIODIVERSITA_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.DESCRIZIONE_DEL_SUOLO:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VALUTAZIONE_GLOBALE_DEL_VIGNETO:
                    return GetColorePin(rilievo.ValutazioneGlobaleVigneto, 1, 3, 5, 6, 8, 9, 10, Colore.Viola, Colore.RossoScuro, Colore.RossoChiaro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);
                case TipoRilievo.Enum.STORY_TELLING:
                //return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ANTRACNOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUMI_SECONDARI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_RADICALE_FIBROSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MARCIUME_RADICALE_LANOSO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MELANOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.PIEDE_NERO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.ROSSORE_PARASSITARIO:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.RUGGINE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.VERTICILLIOSI:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.MALATTIA_DI_PIERCE:
                    return GetColorePin(rilievo.PercIntensità, 0, 1, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA:
                {
                    switch ((ValutazioneStatoIdrico.Enum)rilievo.IdValutazioneStatoIdrico)
                    {
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_ASSENTE:
                            return Colore.VerdeScuro;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_LIEVE:
                            return Colore.VerdeChiaro;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_MEDIO:
                            return Colore.Giallo;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_SEVERO:
                            return Colore.Arancione;
                        case ValutazioneStatoIdrico.Enum.STRESS_IDRICO_MOLTO_SEVERO:
                            return Colore.RossoScuro;
                        default:
                            return Colore.Bianco;
                    }
                }
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA_MIDDAY_MPA:
                case TipoRilievo.Enum.STATO_IDRICO_PIANTA_PREDAWN_MPA:
                    //return GetColorePin(rilievo.PotenzialeIdricoPerVite, 0.3m, 0.5m, 0.8m, 1, 1.3m, 1.5m, 1.8m, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
                    return GetColorePin(rilievo.PotenzialeIdricoPerVite, -0.6m, -0.4m, -0.3m, -0.2m, 0, Colore.RossoScuro, Colore.Arancione, Colore.Giallo, Colore.VerdeChiaro, Colore.VerdeScuro);
            }

            if (rilievo.PercDiffusione != null)
            {
                return GetColorePin(rilievo.PercDiffusione, 0, 2, 5, 10, 25, 50, 100, Colore.VerdeScuro, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
            }

            return Colore.VerdeScuro; //--- DA VERIFICARE
        }

        public static Colore GetColorePinMinMax(decimal value, decimal min, decimal max)
        {
            if (value == 0)
            {
                return Colore.VerdeScuro;
            }

            return GetColorePinMinMax(value, min, max, Colore.VerdeChiaro, Colore.Giallo, Colore.Arancione, Colore.RossoChiaro, Colore.RossoScuro, Colore.Viola);
        }

        public static Colore GetColorePin(decimal? valore, decimal v1, decimal v2, decimal v3, decimal v4, decimal v5, Colore c1, Colore c2, Colore c3, Colore c4, Colore c5)
        {
            if (valore == null || valore <= v1)
            {
                return c1;
            }
            else if (valore <= v2)
            {
                return c2;
            }
            else if (valore <= v3)
            {
                return c3;
            }
            else if (valore <= v4)
            {
                return c4;
            }
            else
            {
                return c5;
            }
            
        }

        public static Colore GetColorePin(decimal? valore, decimal v1, decimal v2, decimal v3, decimal v4, decimal v5, decimal v6, decimal v7, Colore c1, Colore c2, Colore c3, Colore c4, Colore c5, Colore c6, Colore c7)
        {
            if (valore == null || valore <= v1)
            {
                return c1;
            }
            else if (valore <= v2)
            {
                return c2;
            }
            else if (valore <= v3)
            {
                return c3;
            }
            else if (valore <= v4)
            {
                return c4;
            }
            else if (valore <= v5)
            {
                return c5;
            }
            else if (valore <= v6)
            {
                return c6;
            }
            else
            {
                return c7;
            }
        }        

        static Colore GetColorePinMinMax(decimal? valore, decimal min, decimal max, Colore c1, Colore c2, Colore c3, Colore c4, Colore c5, Colore c6)
        {
            if (valore == null || valore <= min + (max - min) / 6 * 1)
            {
                return c1;
            }
            else if (valore <= min + (max - min) / 6 * 2)
            {
                return c2;
            }
            else if (valore <= min + (max - min) / 6 * 3)
            {
                return c3;
            }
            else if (valore <= min + (max - min) / 6 * 4)
            {
                return c4;
            }
            else if (valore <= min + (max - min) / 6 * 5)
            {
                return c5;
            }
            else
            {
                return c6;
            }            
        }        

        public static string ToTipoPin(int? idCategoriaRilievo)
        {
            if (idCategoriaRilievo == null)
            {
                //return null;
                return TipoPin.Generico;
            }

            switch((CategoriaRilievo.Enum)(idCategoriaRilievo))
            {
                case CategoriaRilievo.Enum.AMPELOPATIE_PRINCIPALI:
                    return TipoPin.AmpelopatiePrincipali;
                case CategoriaRilievo.Enum.AMPELOPATIE_SECONDARIE:
                    return TipoPin.AmpelopatieSecondarie;
                case CategoriaRilievo.Enum.AMPELOPATIE_RARE:
                    return TipoPin.AmpelopatieRare;
                case CategoriaRilievo.Enum.FENOLOGIA:
                    return TipoPin.Fenologia;
                case CategoriaRilievo.Enum.BIODIVERSITA_ORGANISMI_UTILI:
                    return TipoPin.Organismi;
                case CategoriaRilievo.Enum.GESTIONE_SUOLO:
                    return TipoPin.Nutrizionale;
                case CategoriaRilievo.Enum.PARAMETRI_PRODUTTIVI:
                    return TipoPin.Parametri;
                case CategoriaRilievo.Enum.MONITORAGGIO_TRAPPOLE_SENSORI:
                    return TipoPin.Sensori;
                case CategoriaRilievo.Enum.VALUTAZIONE_GLOBALE:
                    return TipoPin.Valutazione;
                default:
                    return null;
            }
        }
    }

    public static class ScaleColoriExtensions
    {
        public static string ToRGB(this ScaleColori.Colore colore)
        {

            switch (colore)
            {
                case ScaleColori.Colore.VerdeScuro: return ScaleColori.ColoreRGB.VerdeScuro;
                case ScaleColori.Colore.VerdeChiaro: return ScaleColori.ColoreRGB.VerdeChiaro;
                case ScaleColori.Colore.Giallo: return ScaleColori.ColoreRGB.Giallo;
                case ScaleColori.Colore.Arancione: return ScaleColori.ColoreRGB.Arancione;
                case ScaleColori.Colore.RossoChiaro: return ScaleColori.ColoreRGB.RossoChiaro;
                case ScaleColori.Colore.RossoScuro: return ScaleColori.ColoreRGB.RossoScuro;
                case ScaleColori.Colore.Viola: return ScaleColori.ColoreRGB.Viola;
                default: throw new Exception();
            }
        }

        public static string ToPin(this ScaleColori.Colore colore, int? idCategoriaRilievo)
        {
            return colore.ToPin(ScaleColori.ToTipoPin(idCategoriaRilievo));
            //return idCategoriaRilievo != null ? ToPin(colore, ScaleColori.ToTipoPin(idCategoriaRilievo)) : ToPin(colore);            
        }

        public static string ToPin(this ScaleColori.Colore colore, string tipoPin)
        {
            if (tipoPin == null)
            {
                return ToPin(colore);
            }

            //--- FIX TEMPORANEO PERCHE' MANCA IMMAGINE SU MOBILE
            if (colore == ScaleColori.Colore.Bianco && tipoPin == ScaleColori.TipoPin.Generico)
            {
                return ScaleColori.Colore.Bianco.ToPin();
            }

            switch (colore)
            {
                case ScaleColori.Colore.VerdeScuro: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.VerdeScuro);
                case ScaleColori.Colore.VerdeChiaro: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.VerdeChiaro);
                case ScaleColori.Colore.Giallo: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.Giallo);
                case ScaleColori.Colore.Arancione: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.Arancione);
                case ScaleColori.Colore.RossoChiaro: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.RossoChiaro);
                case ScaleColori.Colore.RossoScuro: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.RossoScuro);
                case ScaleColori.Colore.Viola: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.Viola);
                case ScaleColori.Colore.Bianco: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.Bianco);
                case ScaleColori.Colore.Nero: return "pin_{0}_{1}".FormatWith(tipoPin, ScaleColori.ColorePin.Nero);
                default: throw new Exception("Colore pin mancante");
            }
        }

        public static string ToPin(this ScaleColori.Colore colore)
        {

            switch (colore)
            {
                case ScaleColori.Colore.VerdeScuro: return "pin_{0}".FormatWith(ScaleColori.ColorePin.VerdeScuro);
                case ScaleColori.Colore.VerdeChiaro: return "pin_{0}".FormatWith(ScaleColori.ColorePin.VerdeChiaro);
                case ScaleColori.Colore.Giallo: return "pin_{0}".FormatWith(ScaleColori.ColorePin.Giallo);
                case ScaleColori.Colore.Arancione: return "pin_{0}".FormatWith(ScaleColori.ColorePin.Arancione);
                case ScaleColori.Colore.RossoChiaro: return "pin_{0}".FormatWith(ScaleColori.ColorePin.RossoChiaro);
                case ScaleColori.Colore.RossoScuro: return "pin_{0}".FormatWith(ScaleColori.ColorePin.RossoScuro);
                case ScaleColori.Colore.Viola: return "pin_{0}".FormatWith(ScaleColori.ColorePin.Viola);
                case ScaleColori.Colore.Bianco: return "pin_{0}".FormatWith(ScaleColori.ColorePin.Bianco);
                case ScaleColori.Colore.Nero: return "pin_{0}".FormatWith(ScaleColori.ColorePin.Nero);
                default: throw new Exception("Colore pin mancante");
            }
        }        
    }
}
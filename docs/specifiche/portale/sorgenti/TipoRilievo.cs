using System;

using System.Collections.Generic;


namespace _4GrapesAPI.Entity
{
	public class _TipoRilievo
	{		
        public enum Enum
        {
            ACARI_DANNOSI = 1,
            ALTRE_AVVERSITA = 2,
            ALTRI_FUNGHI = 3,
            ALTRI_INSETTI = 4,
            BOTRITE = 5,
            CICALINE = 6, 
            COCCINIGLIE = 7,
            ESCORIOSI = 8,
            MAL_DELL_ESCA = 9,
            MARCIUMI_SECONDARI = 10,
            OIDIO = 11,
            PERONOSPORA = 12,
            TIGNOLE = 13,
            TRIPIDI = 14,
            VIROSI_E_FITOPLASMI = 15,   
            STATO_NUTRIZIONALE_E_CARENZE = 16,
            PARAMETRI_PRODUTTIVI = 17,
            PARAMETRI_VEGETATIVI = 18,
            MONITORAGGIO_CON_TRAPPOLE = 19,
            ORGANISMI_UTILI = 20,
            STADIO_FENOLOGICO = 21,
            ERIOFIDE_ACARIOSI = 22,
            ERIOFIDE_ERINOSI = 23,
            RAGNETTO_ROSSO = 24,
            ERINEUM_MITE = 25,
            WILLAMETTE_MITE = 26,
            PACIFIC_MITE = 27,
            DANNI_DA_GELO = 28,
            DANNI_DA_GRANDINE = 29,
            AGENTE_DI_FITOTOSSICITA = 30,
            MELATA = 31,
            SCOTTATURA = 32,
            ALTRA_AVVERSITA = 33,
            BERRY_SHRIVEL = 34,
            DISSECCAMENTO_RACHIDE = 35,
            TUMORE_BATTERICO = 36,
            DANNO_DA_VENTO = 37,
            DANNO_DA_CICALINA_VERDE = 38,
            APPASSIMENTO = 39,
            ESTRUSIONE = 40,
            DANNO_DA_UCCELLI = 41,
            DANNO_DA_UNGULATI = 42,
            DANNO_MECCANICO = 43,
            DANNO_DA_DISERBO = 44,
            BATTERIOSI = 45,
            CARIE_BIANCA = 46,
            EUTIPIOSI = 47,
            ARMILLARIA_SPP = 48,
            NOTTUE = 49,
            CIMICE_VERDE_DELLA_VITE = 50,
            OZIORRINCO = 51,
            BOSTRICO_DELLA_VITE = 52,
            SIGARAIO_DELLA_VITE = 53,
            FILLOSSERA_DELLA_VITE = 54,
            METCALFA = 55,
            EULIA = 56,
            MINATORE_FOGLIARE_PHYLLOCNISTIS_VITEGENELLA = 57,
            MAGGIOLINO = 58,
            ALTRA_AVVERSITA_RARA = 59,
            TROPINOTA_HIRTA = 60,
            TORTRICE_DELLA_VITE = 61,
            MINATORE_FOGLIARE_HOLOCACISTA_RIVILLEI = 62,
            MINATORE_FOGLIARE_ANTISPILA_OINOPHYLLA = 63,
            CIMICE_MARMORATA_asiatica = 64,
            DANNO_DA_APOIDEI = 65,
            IFANTRIA = 66,
            FORMICHE = 67,
            LACNAIA = 68,
            AFIDI = 69,
            CRYPTOBLABES_GNIDIELLA = 70,
            ZIGENA_DELLA_VITE = 71,
            CICADELLA_BUFALO = 72,
            MUFFA_GRIGIA_O_BOTRITE = 73,
            CICALINA_VERDE = 74,
            CICALINA_GIALLA = 75,
            SCAFOIDEO = 76,
            ERYTHRONEURA_VULNERATA = 77,
            COCCINIGLIA_DEL_CORNIOLO = 78,
            COCCINIGLIA_FARINOSA = 79,
            COCCINIGLIA_COTONOSA = 80,
            COCCINIGLIA_DELLA_VITE = 81,
            PARTHENOLECANIUM_PERSICAE = 82,
            ELIOCOCCO_DI_BOEMIA = 83,
            MARCIUME_NERO = 84,
            ASPERGILLUS_SUPP = 85,
            MARCIUME_ACIDO = 86,
            TRIPIDE_DELLA_VITE = 87,
            APLOTRIPS = 88,
            VIROSI_DEL_PINOT_GRIGIO = 89,
            GIALLUME_DELLA_VITE = 90,
            LEGNO_NERO_FLAVESCENZA_DORATA = 91,
            MOSAICO_GIALLO = 92,
            ACCARTOCCIAMENTO_FOGLIARE_GLRV = 93,
            ARRICCIAMENTO_FOGLIARE = 94,
            TRIPIDI_PREDATORI = 95,
            FITOSEIDI = 96,
            COCCINELLIDI = 97,
            CRISOPIDI = 98,
            ICNEUMONIDI = 99,
            RAGNI = 100,
            ORIUS = 101,
            CANTARIDI = 102,
            ANAGYRUS = 103,
            ALTRI_ORGANISMI_UTILI = 104,
            TROMBIDIDI = 105,
            GEMME_VITE = 106,
            GERMOGLI_VITE = 107,
            GRAPPOLI_VITE = 108,            
            FALLANZE = 109,
            MATURAZIONE_UVE = 110,
            ANALISI_SENSORIALE_ACINI = 111,
            MORFOLOGIA_GRAPPOLO = 112,
            LUNGHEZZA_GERMOGLIO = 113,
            STATO_IDRICO_PIANTA_MPA = 114,
            PESO_MEDIO_LEGNO_DI_POTATURA = 115,
            GRAPPOLI_ESPOSTI_AL_SOLE = 116,
            SUPERFICIE_FOGLIARIA_ESPOSTA = 117,
            VIGORIA_DEL_VIGNETO = 118,
            FASI_FENOLOGICHE_BBCH = 119,
            IMMISSIONE_UTILI = 120,
            MONITORAGGIO_CON_SENSORI = 121,
            APPLICAZIONE_CONFUSIONE_SESSUALE = 122,
            VALUTAZIONE_GLOBALE_DEL_VIGNETO = 128,
            STORY_TELLING = 129,
            ANTRACNOSI = 130,
            MARCIUMI_SECONDARI_DEL_GRAPPOLO = 131,
            MARCIUME_RADICALE_FIBROSO = 132,
            MARCIUME_RADICALE_LANOSO = 133,
            MELANOSI = 134,
            PIEDE_NERO = 135,
            ROSSORE_PARASSITARIO = 136,
            RUGGINE = 137,
            VERTICILLIOSI = 138,
            MALATTIA_DI_PIERCE = 139,
            GESTIONE_DEL_SUOLO = 140,
            DESCRIZIONE_DEL_INTERFILA = 141,
            DESCRIZIONE_DEL_SOTTOFILA = 142,
            BIODIVERSITA_DEL_SUOLO = 143,
            DESCRIZIONE_DEL_SUOLO = 144,
        }

		
		public int Id { get; set; }

        public int IdCategoriaRilievo { get; set; }

        public string Descrizione { get; set; }

		public string TemplateRilievo { get; set; }

        public int Ordinamento { get; set; }

        public int? RisoluzioneFoto { get; set; }

        public int? QualitaFoto { get; set; }
        public bool DaMonitorare { get; set; }

        public bool ValutazioneAnalitica { get; set; }
        public bool ValutazioneSintetica { get; set; }
        public decimal? StimaBassaMin { get; set; }
        public decimal? StimaBassaMed { get; set; }
        public decimal? StimaBassaMax { get; set; }
        public decimal? StimaMediaMin { get; set; }
        public decimal? StimaMediaMed { get; set; }
        public decimal? StimaMediaMax { get; set; }
        public decimal? StimaAltaMin { get; set; }
        public decimal? StimaAltaMed { get; set; }
        public decimal? StimaAltaMax { get; set; }

        public bool? CalcolatriceClassi { get; set; }
        public string IdOrgani { get; set; }
        public int? IdOrganoDefault { get; set; }
        public int? MinCollezioneSuggerita { get; set; }


        public override string ToString ()
		{
			return Descrizione;
		}        
	}
}


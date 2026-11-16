using System.Globalization;
using ProgettoMappe.Contracts;

namespace ProgettoMappe.Api.Portale;

/// <summary>Un campo di un rilievo: colonna di <c>dbo.Rilievo</c> (e della vista), etichetta del portale, tipo, limiti.</summary>
/// <param name="Massimo">Valore assoluto massimo (la precisione della colonna): oltre, «Valore troppo grande.».</param>
public sealed record CampoRilievo(string Colonna, string Etichetta, string Tipo, bool Obbligatorio = false, bool InLista = true,
    decimal Massimo = 1_000_000m, int Lunghezza = 4000)
{
    /// <summary>«sottovigneto» = un intero scelto fra i sottovigneti del vigneto (menù nella scheda).</summary>
    public const string Numero = "numero", Intero = "intero", Testo = "testo", Data = "data", Sottovigneto = "sottovigneto";

    public CampoRilievoDto Dto() => new(Colonna, Etichetta, Tipo, Obbligatorio, InLista);
}

/// <summary>
/// Una sezione dei rilievi della nutrizione come il suo controller del portale (FallanzeController, AnalisiUveController…):
/// tipo di rilievo, vista della lista, campi. <paramref name="ConData"/> = la scheda ha la data (InizioRilievo/FineRilievo
/// cambiano solo se cambia il giorno); senza, InizioRilievo = adesso all'inserimento. <paramref name="FissiNuovo"/> = colonne
/// scritte solo all'inserimento (es. BBCH 89 della produzione).
/// </summary>
/// <param name="Vista">La vista della lista; null = direttamente <c>dbo.Rilievo</c> del tipo (analisi dei terreni).</param>
/// <param name="Terreni">Le regole in più delle analisi dei terreni: tessitura dalla terna Sabbia/Limo/Argilla (colonne che non
/// sono di Rilievo), valori del suolo ricavati con <see cref="CalcoloSuolo"/>, sottovigneto 0 = nessuno.</param>
public sealed record DefinizioneRilievo(string Sezione, int Tipo, string? Vista, bool ConData, IReadOnlyList<CampoRilievo> Campi,
    IReadOnlyDictionary<string, object?>? FissiNuovo = null, bool VignetoModificabile = false, bool Eliminabile = true,
    bool Terreni = false);

public static class RilieviNutrizione
{
    /// <summary>7 Fallanze: <c>TipoRilievo.Enum.FALLANZE</c> = 109, percentuali numeric(8,2) (dba 16/11).</summary>
    public static readonly DefinizioneRilievo Fallanze = new("fallanze", 109, "VistaDatiFallanze", ConData: true,
    [
        new("PercInAllevamento", "% in Allevamento", CampoRilievo.Numero),
        new("PercCapitozzata", "% Capitozzata", CampoRilievo.Numero),
        new("PercPresenteMorta", "% Presente Morta", CampoRilievo.Numero),
        new("PercFallanze", "% fallanze", CampoRilievo.Numero),
        new("NrOrganiControllati", "N° Organi Controllati", CampoRilievo.Intero),
    ]);

    /// <summary>8 Analisi uve: <c>MATURAZIONE_UVE</c> = 110, valori numeric(15,2) (dba 16/11).</summary>
    public static readonly DefinizioneRilievo AnalisiUve = new("analisi-uve", 110, "VistaAnalisiUve", ConData: true,
    [
        new("Zuccheri", "Zuccheri (brix)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("AciditaTotale", "Acidità totale (g/L acido tartarico)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("PH", "PH", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("AcMalico", "AC Malico (g/L)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("Tinta", "Tinta (H°)", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
    ]);

    /// <summary>
    /// 6 Dati produzione: rilievo di vendemmia = <c>FASI_FENOLOGICHE_BBCH</c> 119 con BBCH 89 (la vista mostra solo quelli con
    /// la produzione); la scheda non ha la data del rilievo (InizioRilievo = adesso all'inserimento), ma la data di vendemmia.
    /// </summary>
    public static readonly DefinizioneRilievo Produzione = new("produzione", 119, "VistaDatiProduzione", ConData: false,
    [
        new("ProduzioneTotaleDelVigneto", "Produzione totale del vigneto", CampoRilievo.Numero, Massimo: 1_000_000_000_000m),
        new("PesoMedioGrappoloPesato", "Peso medio grappolo pesato", CampoRilievo.Numero),
        new("DataDiVendemmia", "Data di vendemmia", CampoRilievo.Data),
    ], FissiNuovo: new Dictionary<string, object?> { ["BBCH"] = 89 });

    /// <summary>Un valore del suolo: numeric(12,3), facoltativo, non in lista; l'unità di misura come nella scheda del portale.</summary>
    private static CampoRilievo Suolo(string colonna, string etichetta, string? um = null) =>
        new(colonna, um is null ? etichetta : $"{etichetta} · {um}", CampoRilievo.Numero, InLista: false, Massimo: 1_000_000_000m);

    /// <summary>
    /// 9 Analisi dei terreni (TessituraController): <c>DESCRIZIONE_DEL_SUOLO</c> = 144, lista letta da <c>dbo.Rilievo</c>
    /// (codice campione, codice di analisi, data prelievo), vigneto modificabile anche dopo, nessuna cancellazione (nel portale
    /// «Elimina» dice «eliminato» ma il DELETE è commentato). Sabbia, Limo, Argilla non sono colonne di Rilievo: servono a trovare
    /// la tessitura. Tipi dal dba (16/11).
    /// </summary>
    public static readonly DefinizioneRilievo Terreni = new("analisi-terreni", 144, Vista: null, ConData: false,
    [
        new("IdSubVigneto", "SubVigneto", CampoRilievo.Sottovigneto, InLista: false),
        new("CodCampione", "Codice campione", CampoRilievo.Testo, Obbligatorio: true, Lunghezza: 50),
        new("DataPrelievoCampione", "Data prelievo campione", CampoRilievo.Data, Obbligatorio: true),
        new("ProfonditaDelCampione", "Profondità del campione (cm)", CampoRilievo.Intero, Obbligatorio: true, InLista: false),
        new("CodiceDiAnalisi", "Codice di analisi", CampoRilievo.Testo, Lunghezza: 256),
        new("LaboratorioDiAnalisi", "Laboratorio di analisi", CampoRilievo.Testo, InLista: false, Lunghezza: 256),
        new("Sabbia", "Sabbia", CampoRilievo.Intero, Obbligatorio: true, InLista: false),
        new("Limo", "Limo", CampoRilievo.Intero, Obbligatorio: true, InLista: false),
        new("Argilla", "Argilla", CampoRilievo.Intero, Obbligatorio: true, InLista: false),
        Suolo("Suolo_ph_h20", "pH in H2O"),
        Suolo("Suolo_CO_perc", "Carbonio organico (C)", "%"),
        Suolo("Suolo_SO_perc", "Sostanza organica", "%"),
        Suolo("Suolo_N_totale_perc", "Azoto totale (N)", "‰"),
        Suolo("Suolo_C_N", "Rapporto C/N"),
        Suolo("Suolo_CaCO3_totali_perc", "Carbonati totali (CaCO3)", "%"),
        Suolo("Suolo_CaCO3_attivo_perc", "Calcare attivo (CaCO3)", "%"),
        Suolo("Suolo_Al_assimilabile_ppm", "Alluminio assimilabile (Al)", "ppm"),
        Suolo("Suolo_B_assimilabile_ppm", "Boro assimilabile (B)", "ppm"),
        Suolo("Suolo_Ca_idrosolubile_ppm", "Calcio idrosolubile (Ca)", "ppm"),
        Suolo("Suolo_Ca_scambiabile_ppm", "Calcio scambiabile (Ca)", "ppm"),
        Suolo("Suolo_Cl_idrosolubile_ppm", "Cloro idrosolubile (Cl)", "ppm"),
        Suolo("Suolo_Fe_assimilabile_ppm", "Ferro assimilabile (Fe)", "ppm"),
        Suolo("Suolo_P_assimilabile_Olsen_ppm", "Fosforo assimilabile (P) Olsen", "ppm"),
        Suolo("Suolo_P205_assimilabile_Olsen_ppm", "Fosforo assimilabile (P₂O₅) Olsen", "ppm"),
        Suolo("Suolo_P2O5_assimilabile_TerreniAcidi_ppm", "Fosforo assimilabile (P₂O₅) Terreni Acidi", "ppm"),
        Suolo("Suolo_P_acqua_ppm", "Fosforo solubile in acqua (P)", "ppm"),
        Suolo("Suolo_P_Totale_ppm", "Fosforo Totale (P)", "ppm"),
        Suolo("Suolo_Mg_idrosolubile_ppm", "Magnesio idrosolubile (Mg)", "ppm"),
        Suolo("Suolo_Scambiabile_Mgo_ppm", "Magnesio Ossido Scambiabile (Mgo)", "ppm"),
        Suolo("Suolo_Mg_scambiabile_ppm", "Magnesio scambiabile (Mg)", "ppm"),
        Suolo("Suolo_Mn_assimilabile_ppm", "Manganese assimilabile (Mn)", "ppm"),
        Suolo("Suolo_K_idrosolubile_ppm", "Potassio idrosolubile (K)", "ppm"),
        Suolo("Suolo_K_scambiabile_ppm", "Potassio scambiabile (K)", "ppm"),
        Suolo("Suolo_K2O_ppm", "Potassio scambiabile (K₂O)", "ppm"),
        Suolo("Suolo_K_totale_ppm", "Potassio totale (K)", "ppm"),
        Suolo("Suolo_Cu_assimilabile_ppm", "Rame assimilabile (Cu)", "ppm"),
        Suolo("Suolo_Cu_totale_ppm", "Rame totale (Cu)", "ppm"),
        Suolo("Suolo_Na_idrosolubile_ppm", "Sodio idrosolubile (Na)", "ppm"),
        Suolo("Suolo_Na_scambiabile_ppm", "Sodio scambiabile (Na)", "ppm"),
        Suolo("Suolo_Zn_assimilabile_ppm", "Zinco assimilabile (Zn)", "ppm"),
        Suolo("Suolo_Zn_totale_ppm", "Zinco totale (Zn)", "ppm"),
        Suolo("Suolo_S_assimilabile_ppm", "Zolfo assimilabile (S)", "ppm"),
        Suolo("Suolo_CSC", "Capacità di Sc. Cation. (CSC)", "meq/100g"),
        Suolo("Suolo_CSC_K", "Potassio scambiabile (K) % su CSC", "%"),
        Suolo("Suolo_CSC_Na", "Sodio scambiabile (Na) % su CSC", "%"),
        Suolo("Suolo_CSC_Ca", "Calcio scambiabile (Ca) % su CSC", "%"),
        Suolo("Suolo_CSC_Mg", "Magnesio scambiabile (Mg) % su CSC", "%"),
        Suolo("Suolo_aciditascambio_perc", "Acidità di scambio %", "%"),
        Suolo("Suolo_aciditascambio_meq", "Acidità di scambio", "meq/100g"),
        Suolo("Suolo_saturazionebasica_perc", "Saturazione basica %", "%"),
        Suolo("Suolo_Ca_Mg_meq", "Rapporto Ca/Mg", "meq"),
        Suolo("Suolo_Mg_K_meq", "Rapporto Mg/K", "meq"),
        Suolo("Suolo_Ca_K_meq", "Rapporto Ca/K", "meq"),
        Suolo("Suolo_K_Mg_meq", "Rapporto K/Mg", "meq"),
        Suolo("Suolo_Mg_Ca_meq", "Rapporto Mg/Ca", "meq"),
        Suolo("Suolo_K_Ca_meq", "Rapporto K/Ca", "meq"),
        Suolo("Suolo_SO_Argilla", "Percentuale sost. organica su argilla (%)", "%"),
        Suolo("Suolo_K2", "K2 coeff mineralizzazione"),
    ], VignetoModificabile: true, Eliminabile: false, Terreni: true);

    /// <summary>I campi delle analisi dei terreni che non sono colonne di Rilievo (servono a trovare la tessitura).</summary>
    public static readonly IReadOnlyList<string> CampiTessitura = ["Sabbia", "Limo", "Argilla"];

    public static readonly IReadOnlyList<DefinizioneRilievo> Tutte = [Fallanze, AnalisiUve, Produzione, Terreni];

    /// <summary>Le sezioni per il vincolo della rotta dell'Api.</summary>
    public const string RegexSezioni = "^(fallanze|analisi-uve|produzione|analisi-terreni)$";

    public static DefinizioneRilievo? Trova(string sezione) => Tutte.FirstOrDefault(d => d.Sezione == sezione);

    /// <summary>
    /// I valori della scheda convertiti per le colonne (testo invariante → decimal, int, DateTime, string; vuoto = null) e gli
    /// errori per campo. Nomi sconosciuti si ignorano: le colonne vengono solo dalla definizione.
    /// </summary>
    public static (Dictionary<string, object?> Valori, Dictionary<string, string[]> Errori) Converti(DefinizioneRilievo definizione,
        IReadOnlyDictionary<string, string?>? valori)
    {
        var colonne = new Dictionary<string, object?>();
        var errori = new Dictionary<string, string[]>();
        foreach (var campo in definizione.Campi)
        {
            var testo = valori?.GetValueOrDefault(campo.Colonna)?.Trim();
            if (string.IsNullOrEmpty(testo))
            {
                if (campo.Obbligatorio) errori[campo.Colonna] = [Controllers.PortaleDifesaController.Obbligatorio];
                colonne[campo.Colonna] = null;
                continue;
            }

            object? valore = campo.Tipo switch
            {
                CampoRilievo.Numero when decimal.TryParse(testo, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
                CampoRilievo.Intero or CampoRilievo.Sottovigneto when int.TryParse(testo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
                CampoRilievo.Data when DateTime.TryParse(testo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var g) => g,
                CampoRilievo.Testo => testo,
                _ => null,
            };
            if (valore is null)
            {
                errori[campo.Colonna] = [campo.Tipo == CampoRilievo.Data ? "Data non valida." : "Numero non valido."];
            }
            else if (valore is decimal n && Math.Abs(n) >= campo.Massimo || valore is int k && Math.Abs((decimal)k) >= campo.Massimo)
            {
                errori[campo.Colonna] = ["Valore troppo grande."];
            }
            else if (valore is string s && s.Length > campo.Lunghezza)
            {
                errori[campo.Colonna] = [$"Al massimo {campo.Lunghezza} caratteri."];
            }

            colonne[campo.Colonna] = valore;
        }

        return (colonne, errori);
    }

}

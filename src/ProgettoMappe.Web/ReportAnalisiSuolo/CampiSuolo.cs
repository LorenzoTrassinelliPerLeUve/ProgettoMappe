using ProgettoMappe.Contracts;

namespace ProgettoMappe.Web.ReportAnalisiSuolo;

/// <summary>
/// I campi dell'Excel di Qlik ricavati da un rilievo 144 (task #317, <c>docs/specifiche/report-analisi-suolo/regole-dati.md</c>).
/// Le 13 colonne nascoste nel portale le calcola sempre Mappe (decisione del proprietario, 17/11); il valore del laboratorio
/// si usa solo quando mancano i dati per calcolarle.
/// </summary>
public static class CampiSuolo
{
    // meq/100g (= cmol(+)/kg) dai ppm, con i divisori del foglio «Composizione CSC» di Qlik.
    public const double DivisoreCa = 200.4, DivisoreMg = 121.5, DivisoreK = 391, DivisoreNa = 229.9;

    /// <summary>Carbonio organico → sostanza organica (Van Bemmelen).</summary>
    public const double FattoreSo = 1.724;

    /// <summary>Sotto questa soglia <c>Suolo_N_totale_perc</c> è in %, sopra già in ‰ (dba #319, confermato il 17/11).</summary>
    public const double SogliaAzotoPercento = 0.4;

    // Nomi dei campi (chiavi di Valori del rilievo calcolato).
    public const string PhH2o = "ph_h2o", PhKcl = "ph_kcl", Carbonati = "carbonati", Calcare = "calcare", Conducibilita = "conducibilita",
        Co = "co", So = "so", SoArgilla = "so_argilla", Azoto = "azoto", CN = "c_n", Csc = "csc", CaPpm = "ca_ppm", MgPpm = "mg_ppm",
        KPpm = "k_ppm", NaPpm = "na_ppm", CaMeq = "ca_meq", MgMeq = "mg_meq", KMeq = "k_meq", NaMeq = "na_meq", AciditaMeq = "acidita_meq",
        Saturazione = "saturazione", CaCsc = "ca_csc", MgCsc = "mg_csc", KCsc = "k_csc", NaCsc = "na_csc", AciditaCsc = "acidita_csc",
        MgK = "mg_k", CaMg = "ca_mg", CaK = "ca_k", POlsen = "p_olsen", PBray = "p_bray", Zolfo = "zolfo", FeDtpa = "fe_dtpa",
        MnDtpa = "mn_dtpa", ZnDtpa = "zn_dtpa", CuDtpa = "cu_dtpa", FeEdta = "fe_edta", MnEdta = "mn_edta", ZnEdta = "zn_edta",
        CuEdta = "cu_edta", Boro = "boro", Molibdeno = "molibdeno", CaIdro = "ca_idro", MgIdro = "mg_idro", KIdro = "k_idro",
        NaIdro = "na_idro", ClIdro = "cl_idro", K2 = "k2";

    /// <summary>Colonne del 4g copiate così come sono.</summary>
    private static readonly (string Campo, string Colonna)[] Dirette =
    [
        (PhH2o, "Suolo_ph_h20"), (PhKcl, "Suolo_ph_kcl"), (Carbonati, "Suolo_CaCO3_totali_perc"), (Calcare, "Suolo_CaCO3_attivo_perc"),
        (Conducibilita, "Suolo_conducibilita_mS_cm"), (Co, "Suolo_CO_perc"), (Csc, "Suolo_CSC"), (CaPpm, "Suolo_Ca_scambiabile_ppm"),
        (MgPpm, "Suolo_Mg_scambiabile_ppm"), (KPpm, "Suolo_K_scambiabile_ppm"), (NaPpm, "Suolo_Na_scambiabile_ppm"),
        (AciditaMeq, "Suolo_aciditascambio_meq"), (POlsen, "Suolo_P_assimilabile_Olsen_ppm"), (PBray, "Suolo_P_assimilabile_TerreniAcidi_ppm"),
        (Zolfo, "Suolo_S_assimilabile_ppm"), (FeDtpa, "Suolo_Fe_assimilabile_ppm"), (MnDtpa, "Suolo_Mn_assimilabile_ppm"),
        (ZnDtpa, "Suolo_Zn_assimilabile_ppm"), (CuDtpa, "Suolo_Cu_assimilabile_ppm"), (FeEdta, "Suolo_Fe_assimilabile_ppm_EDTA"),
        (MnEdta, "Suolo_Mn_assimilabile_ppm_EDTA"), (ZnEdta, "Suolo_Zn_assimilabile_ppm_EDTA"), (CuEdta, "Suolo_Cu_assimilabile_ppm_EDTA"),
        (Boro, "Suolo_B_assimilabile_ppm"), (Molibdeno, "Suolo_Mo_assimilabile_ppm"), (CaIdro, "Suolo_Ca_idrosolubile_ppm"),
        (MgIdro, "Suolo_Mg_idrosolubile_ppm"), (KIdro, "Suolo_K_idrosolubile_ppm"), (NaIdro, "Suolo_Na_idrosolubile_ppm"),
        (ClIdro, "Suolo_Cl_idrosolubile_ppm"),
    ];

    /// <summary>Tutti i campi del rilievo: quelli diretti, l'azoto in ‰, i meq e le colonne calcolate.</summary>
    public static IReadOnlyDictionary<string, double> Calcola(AnalisiSuoloDto a, TessituraSuoloDto? tessitura)
    {
        var v = a.Valori;
        double? L(string colonna) => v.TryGetValue(colonna, out var x) ? x : null;
        var c = new Dictionary<string, double>();
        foreach (var (campo, colonna) in Dirette)
        {
            if (L(colonna) is { } x)
            {
                c[campo] = x;
            }
        }
        double? G(string campo) => c.TryGetValue(campo, out var x) ? x : null;
        void Metti(string campo, double? calcolato, string? colonnaLaboratorio = null)
        {
            if ((calcolato ?? (colonnaLaboratorio is null ? null : L(colonnaLaboratorio))) is { } x && double.IsFinite(x))
            {
                c[campo] = x;
            }
        }
        static double? Diviso(double? a, double? b) => a is { } x && b is { } y && y > 0 ? x / y : null;

        Metti(Azoto, AzotoPermille(L("Suolo_N_totale_permille"), L("Suolo_N_totale_perc")));
        Metti(CaMeq, G(CaPpm) / DivisoreCa);
        Metti(MgMeq, G(MgPpm) / DivisoreMg);
        Metti(KMeq, G(KPpm) / DivisoreK);
        Metti(NaMeq, G(NaPpm) / DivisoreNa);

        Metti(So, G(Co) * FattoreSo, "Suolo_SO_perc");
        var carbonio = G(Co) ?? L("Suolo_SO_perc") / FattoreSo;
        Metti(CN, Diviso(carbonio, G(Azoto) / 10), "Suolo_C_N");
        Metti(CaCsc, Diviso(G(CaMeq) * 100, G(Csc)), "Suolo_CSC_Ca");
        Metti(MgCsc, Diviso(G(MgMeq) * 100, G(Csc)), "Suolo_CSC_Mg");
        Metti(KCsc, Diviso(G(KMeq) * 100, G(Csc)), "Suolo_CSC_K");
        Metti(NaCsc, Diviso(G(NaMeq) * 100, G(Csc)), "Suolo_CSC_Na");
        Metti(Saturazione, Diviso((G(CaMeq) + G(MgMeq) + G(KMeq) + G(NaMeq)) * 100, G(Csc)), "Suolo_saturazionebasica_perc");
        Metti(AciditaCsc, Diviso(G(AciditaMeq) * 100, G(Csc)), "Suolo_aciditascambio_perc");
        Metti(MgK, Diviso(G(MgMeq), G(KMeq)), "Suolo_Mg_K_meq");
        Metti(CaMg, Diviso(G(CaMeq), G(MgMeq)), "Suolo_Ca_Mg_meq");
        Metti(CaK, Diviso(G(CaMeq), G(KMeq)), "Suolo_Ca_K_meq");
        Metti(SoArgilla, Diviso(G(So) * 100, tessitura?.Argilla), "Suolo_SO_Argilla");
        Metti(K2, CoefficienteK2(tessitura?.Argilla, G(Carbonati)), "Suolo_K2");
        return c;
    }

    /// <summary>Azoto totale in ‰: la colonna del 2026, altrimenti quella degli import, che sotto 0,4 è in %.</summary>
    public static double? AzotoPermille(double? permille, double? perc) =>
        permille ?? (perc is { } p ? p < SogliaAzotoPercento ? p * 10 : p : null);

    /// <summary>
    /// Hénin-Dupuis (decisione del 17/11): K2 = 1200 / ((A + 200)(0,3·C + 200)) × 100, argilla A e calcare totale C in g/kg.
    /// Sui rilievi storici dà in media valori più alti del 28% (il laboratorio usava un'altra argilla).
    /// </summary>
    public static double? CoefficienteK2(double? argillaPercento, double? calcarePercento) =>
        argillaPercento is { } a && calcarePercento is { } cc ? 1200 / ((a * 10 + 200) * (0.3 * cc * 10 + 200)) * 100 : null;
}

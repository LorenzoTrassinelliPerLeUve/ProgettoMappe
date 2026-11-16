namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Sezione 9 «Analisi dei terreni»: i valori del suolo che <c>TessituraController.RilievoTessitura</c> (POST) del portale scrive
/// in <c>dbo.Rilievo</c>. Un valore lasciato vuoto si ricava dagli altri con le formule del portale, **nello stesso ordine**:
/// alcune formule usano il valore scritto nella scheda (<c>model.X</c>), altre quello già ricalcolato (<c>rilievo.X</c>), e la
/// differenza conta. <c>NullIfZero</c> del portale = un divisore 0 dà null.
/// </summary>
public static class CalcoloSuolo
{
    /// <summary>Le colonne Suolo_* della scheda, nell'ordine del modello del portale (RilievoTessituraModel).</summary>
    public static readonly IReadOnlyList<string> Colonne =
    [
        "Suolo_ph_h20", "Suolo_CO_perc", "Suolo_SO_perc", "Suolo_N_totale_perc", "Suolo_C_N", "Suolo_CaCO3_totali_perc",
        "Suolo_CaCO3_attivo_perc", "Suolo_Al_assimilabile_ppm", "Suolo_B_assimilabile_ppm", "Suolo_Ca_idrosolubile_ppm",
        "Suolo_Ca_scambiabile_ppm", "Suolo_Cl_idrosolubile_ppm", "Suolo_Fe_assimilabile_ppm", "Suolo_P205_assimilabile_Olsen_ppm",
        "Suolo_P2O5_assimilabile_TerreniAcidi_ppm", "Suolo_P_acqua_ppm", "Suolo_P_Totale_ppm", "Suolo_Mg_idrosolubile_ppm",
        "Suolo_Scambiabile_Mgo_ppm", "Suolo_Mg_scambiabile_ppm", "Suolo_Mn_assimilabile_ppm", "Suolo_K_idrosolubile_ppm",
        "Suolo_K_scambiabile_ppm", "Suolo_K2O_ppm", "Suolo_K_totale_ppm", "Suolo_Cu_assimilabile_ppm", "Suolo_Cu_totale_ppm",
        "Suolo_Na_idrosolubile_ppm", "Suolo_Na_scambiabile_ppm", "Suolo_Zn_assimilabile_ppm", "Suolo_Zn_totale_ppm",
        "Suolo_S_assimilabile_ppm", "Suolo_CSC", "Suolo_CSC_K", "Suolo_CSC_Na", "Suolo_CSC_Ca", "Suolo_CSC_Mg",
        "Suolo_aciditascambio_perc", "Suolo_aciditascambio_meq", "Suolo_saturazionebasica_perc", "Suolo_Ca_Mg_meq",
        "Suolo_Mg_K_meq", "Suolo_Ca_K_meq", "Suolo_K_Mg_meq", "Suolo_Mg_Ca_meq", "Suolo_K_Ca_meq", "Suolo_SO_Argilla", "Suolo_K2",
        "Suolo_P_assimilabile_Olsen_ppm",
    ];

    /// <summary>I valori da scrivere: <paramref name="scheda"/> = quelli della scheda (null = vuoto), <paramref name="argilla"/> = % di argilla.</summary>
    public static Dictionary<string, decimal?> Completa(IReadOnlyDictionary<string, decimal?> scheda, int? argilla)
    {
        decimal? M(string colonna) => scheda.GetValueOrDefault(colonna);
        var r = Colonne.ToDictionary(c => c, M);
        decimal? R(string colonna) => r[colonna];
        static decimal? Nz(decimal? valore) => valore == 0 ? null : valore;
        decimal? argillaM = argilla;

        r["Suolo_CO_perc"] = M("Suolo_CO_perc") ?? M("Suolo_SO_perc") / 1.724m;
        r["Suolo_SO_perc"] = M("Suolo_SO_perc") ?? M("Suolo_CO_perc") * 1.724m;
        r["Suolo_C_N"] = M("Suolo_C_N") ?? (M("Suolo_N_totale_perc") != 0 ? M("Suolo_CO_perc") / (M("Suolo_N_totale_perc") * 0.1m) : null);
        r["Suolo_Scambiabile_Mgo_ppm"] = M("Suolo_Scambiabile_Mgo_ppm") ?? M("Suolo_Mg_scambiabile_ppm") * 0.602m;
        r["Suolo_Mg_scambiabile_ppm"] = M("Suolo_Mg_scambiabile_ppm") ?? M("Suolo_Scambiabile_Mgo_ppm") / 0.602m;
        r["Suolo_K_scambiabile_ppm"] = M("Suolo_K_scambiabile_ppm") ?? M("Suolo_K2O_ppm") / 1.2m;
        r["Suolo_K2O_ppm"] = M("Suolo_K2O_ppm") ?? M("Suolo_K_scambiabile_ppm") * 1.2m;
        r["Suolo_CSC_K"] = M("Suolo_CSC_K") ?? R("Suolo_K_scambiabile_ppm") / 391m / Nz(R("Suolo_CSC")) * 100;
        r["Suolo_CSC_Na"] = M("Suolo_CSC_Na") ?? R("Suolo_Na_scambiabile_ppm") / 229.9m / Nz(R("Suolo_CSC")) * 100;
        r["Suolo_CSC_Ca"] = M("Suolo_CSC_Ca") ?? R("Suolo_Ca_scambiabile_ppm") / 200.4m / Nz(R("Suolo_CSC")) * 100;
        r["Suolo_CSC_Mg"] = M("Suolo_CSC_Mg") ?? R("Suolo_Mg_scambiabile_ppm") / 121.5m / Nz(R("Suolo_CSC")) * 100;
        r["Suolo_aciditascambio_perc"] = M("Suolo_aciditascambio_perc") ?? 100 - M("Suolo_saturazionebasica_perc");
        r["Suolo_saturazionebasica_perc"] = M("Suolo_saturazionebasica_perc") ?? 100 - M("Suolo_aciditascambio_perc");
        r["Suolo_Ca_Mg_meq"] = M("Suolo_Ca_Mg_meq") ?? R("Suolo_Ca_scambiabile_ppm") / 200.4m / Nz(R("Suolo_Mg_scambiabile_ppm") / 121.5m);
        r["Suolo_Mg_K_meq"] = M("Suolo_Mg_K_meq") ?? R("Suolo_Mg_scambiabile_ppm") / 121.5m / Nz(R("Suolo_K_scambiabile_ppm") / 391m);
        r["Suolo_Ca_K_meq"] = M("Suolo_Ca_K_meq") ?? R("Suolo_Ca_scambiabile_ppm") / 200.4m / Nz(R("Suolo_K_scambiabile_ppm") / 391m);
        r["Suolo_K_Mg_meq"] = M("Suolo_K_Mg_meq") ?? R("Suolo_K_scambiabile_ppm") / 391m / Nz(R("Suolo_Mg_scambiabile_ppm") / 121.5m);
        r["Suolo_Mg_Ca_meq"] = M("Suolo_Mg_Ca_meq") ?? R("Suolo_Mg_scambiabile_ppm") / 121.5m / Nz(R("Suolo_Ca_scambiabile_ppm") / 200.4m);
        r["Suolo_K_Ca_meq"] = M("Suolo_K_Ca_meq") ?? R("Suolo_K_scambiabile_ppm") / 391m / Nz(R("Suolo_Ca_scambiabile_ppm") / 200.4m);
        r["Suolo_SO_Argilla"] = M("Suolo_SO_Argilla") ?? R("Suolo_SO_perc") * 100 / Nz(argillaM);
        r["Suolo_K2"] = M("Suolo_K2") ?? 1200 / Nz(argillaM + 20) / Nz(R("Suolo_CaCO3_totali_perc") + 20);
        return r;
    }
}

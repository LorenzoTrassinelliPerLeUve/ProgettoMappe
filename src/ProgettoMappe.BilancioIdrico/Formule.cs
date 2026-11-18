namespace ProgettoMappe.BilancioIdrico;

/// <summary>Le formule di un giorno (REGOLE.md § 1), senza stato. Un valore <c>null</c> è un dato che manca.</summary>
public static class Formule
{
    /// <summary>§ 1.1: acqua disponibile massima (mm). <c>null</c> senza analisi del suolo.</summary>
    public static double? Awc(AnalisiSuolo? analisi, RilievoRadici? radici, double? distanzaFile, Parametri p)
    {
        if (analisi?.AcquaDisponibileMmM is not { } acquaMmM)
            return null;
        var profondita = analisi.ProfonditaSuolo is { } ps && ps != 0 ? ps * p.ProfonditaSuoloScala : p.ProfonditaSuoloRipiegoM;
        var pietrosita = (analisi.Pietrosita ?? 0) * p.PietrositaScala;
        return acquaMmM * profondita * (100 - pietrosita) / 100 * FattoreRadici(radici, distanzaFile, p);
    }

    /// <summary>(larghezza radici / distanza fra le file) × profondità radici; 1 se manca qualcosa.</summary>
    public static double FattoreRadici(RilievoRadici? radici, double? distanzaFile, Parametri p)
    {
        if (radici is { LarghezzaM: { } larghezza, ProfonditaM: { } profondita } && distanzaFile is { } d && d * p.DistanzaFileScala > 0)
            return larghezza / (d * p.DistanzaFileScala) * profondita;
        return 1;
    }

    /// <summary>§ 1.2: Kc interpolato in linea retta sulla curva della gestione del suolo; fuori dai punti vale il più vicino.</summary>
    public static double? Kc(int? bbch, int? gestione, Parametri p)
    {
        if (bbch is not { } b || gestione is not { } g || !p.CurveKc.TryGetValue(g, out var punti) || punti.Count == 0)
            return null;
        if (b <= punti[0].Bbch)
            return punti[0].Kc;
        for (var i = 1; i < punti.Count; i++)
        {
            var (sotto, sopra) = (punti[i - 1], punti[i]);
            if (b == sopra.Bbch)
                return sopra.Kc;
            if (b < sopra.Bbch)
                return sotto.Kc + (sopra.Kc - sotto.Kc) * (b - sotto.Bbch) / (sopra.Bbch - sotto.Bbch);
        }
        return punti[^1].Kc;
    }

    /// <summary>§ 1.3: fase fenologica (0–7) dal BBCH.</summary>
    public static int? Fase(int? bbch, Parametri p)
    {
        if (bbch is not { } b)
            return null;
        int? fase = null;
        foreach (var intervallo in p.Fasi)
            if (intervallo.BbchDa <= b)
                fase = intervallo.Fase;
        return fase;
    }

    /// <summary>§ 1.5: coefficiente di stress. <c>null</c> se AWC manca o è 0.</summary>
    public static double? Ks(double aw, double? awc, Parametri p)
    {
        if (awc is not { } c || c <= 0)
            return null;
        var sogliaStress = c * (1 - p.FrazioneRawAwc);
        if (aw > sogliaStress)
            return 1;
        return sogliaStress > 0 ? Math.Clamp(aw / sogliaStress, 0, 1) : 1;
    }

    /// <summary>§ 1.6: ETa = ET0 × Kc × Ks (Ks vuoto conta 0). <c>null</c> se manca ET0 o Kc.</summary>
    public static double? Eta(double? et0, double? kc, double? ks) =>
        et0 is { } e && kc is { } k ? e * k * (ks ?? 0) : null;

    /// <summary>Frazione dell'acqua disponibile (AW / AWC).</summary>
    public static double? Faw(double aw, double? awc) => awc is { } c && c > 0 ? aw / c : null;

    /// <summary>§ 1.7: potenziale idrico fogliare a fine notte (MPa). <c>null</c> se il suolo è vuoto.</summary>
    public static double? Pdlwp(double? faw, Parametri p) =>
        faw is { } f && f > 0 ? Math.Log(f / p.PdlwpNumeratore) / p.PdlwpDivisore : null;

    /// <summary>La FAW che dà esattamente il Pdlwp di soglia.</summary>
    public static double FawObiettivo(double sogliaMpa, Parametri p) => p.PdlwpNumeratore * Math.Exp(p.PdlwpDivisore * sogliaMpa);

    /// <summary>§ 1.9: acqua (mm) che manca per tornare alla soglia. <c>null</c> senza AWC o senza soglia.</summary>
    public static double? Deficit(double? faw, double? awc, double? sogliaMpa, Parametri p)
    {
        if (awc is not { } c || c <= 0 || faw is not { } f || sogliaMpa is not { } s)
            return null;
        var obiettivo = FawObiettivo(s, p);
        return f >= obiettivo ? 0 : (obiettivo - f) * c;
    }

    /// <summary>§ 1.11: ore = mm / (tasso × efficienza). <c>null</c> se il tasso manca o è 0.</summary>
    public static double? Ore(double mm, double? tassoMmOra, double efficienza) =>
        tassoMmOra is { } t && t != 0 && efficienza != 0 ? mm / (t * efficienza) : null;

    /// <summary>§ 1.13: turno irriguo dalle regole per classe di tessitura e scheletro.</summary>
    public static int TurnoIrriguo(int? idTessitura, double? scheletroPerc, Parametri p)
    {
        if (idTessitura is { } t && p.ClasseTessitura.TryGetValue(t, out var classe))
        {
            var scheletro = scheletroPerc ?? 0;
            foreach (var regola in p.RegoleTurno)
                if (regola.ClasseTessitura == classe && scheletro >= regola.ScheletroDaPerc && scheletro <= regola.ScheletroAPerc)
                    return regola.TurnoIrriguoGiorni;
        }
        return p.TurnoIrriguoPredefinito;
    }
}

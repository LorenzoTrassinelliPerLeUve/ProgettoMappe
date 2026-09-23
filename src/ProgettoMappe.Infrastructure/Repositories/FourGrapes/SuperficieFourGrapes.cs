namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Colonna di <c>Vigneto</c> da cui proviene la superficie scelta.</summary>
public enum SorgenteSuperficie
{
    Misurata,
    Dichiarata,
    CalcolataDaGis,
    Superficie,
}

/// <summary>Come è stato interpretato un valore grezzo di superficie.</summary>
public enum InterpretazioneSuperficie
{
    /// <summary>Coerente con l'area del poligono se letto in m².</summary>
    MetriQuadrati,

    /// <summary>Coerente con l'area del poligono se letto in ettari (minoranza dei dati reali).</summary>
    Ettari,

    /// <summary>Nessun poligono utilizzabile: assunto in m², la convenzione largamente prevalente.</summary>
    MetriQuadratiPresunti,

    /// <summary>Poligono presente ma valore coerente con nessuna delle due unità.</summary>
    Ambigua,
}

/// <summary>Valore grezzo così come letto da 4Grapes, con la colonna di provenienza.</summary>
public readonly record struct SuperficieGrezza(decimal Valore, SorgenteSuperficie Sorgente);

/// <summary>
/// Esito della risoluzione. <see cref="Ettari"/> è null sia se non c'è alcun valore positivo
/// (<see cref="Interpretazione"/> null) sia se, con un poligono, nessun candidato è coerente
/// (<see cref="InterpretazioneSuperficie.Ambigua"/>, <see cref="Sorgente"/> null).
/// </summary>
public readonly record struct SuperficieRisolta(
    decimal? Ettari, SorgenteSuperficie? Sorgente, InterpretazioneSuperficie? Interpretazione)
{
    /// <summary>Descrizione breve per la diagnostica interna (es. "Misurata (MetriQuadrati)").</summary>
    public string? Descrizione => Interpretazione switch
    {
        null => null,
        InterpretazioneSuperficie.Ambigua => "Ambigua",
        _ => $"{Sorgente} ({Interpretazione})",
    };
}

/// <summary>
/// Scelta e normalizzazione in ettari della superficie di un vigneto 4Grapes. Passi separati e
/// puri, testabili senza database:
/// <list type="number">
/// <item><see cref="Candidati"/>: valori &gt; 0 (NULL, 0 e negativi sono ignorati) nell'ordine
/// del gestionale, Misurata → Dichiarata → CalcolataDaGis → Superficie. <c>Superficie</c> vale
/// <c>COALESCE(Misurata, Dichiarata, CalcolataDaGis)</c> nel 97% delle righe osservate, e resta
/// l'unica valorizzata su ~2.600 vigneti.</item>
/// <item><see cref="Interpreta"/>: nei dati reali ~95% dei valori di ogni colonna è in m²
/// (rapporto ≈ 1 con l'area del poligono), una piccola minoranza (~24 vigneti) è già in ettari.
/// L'area del poligono è il controllo di coerenza: mai una soglia assoluta sul valore.</item>
/// <item><see cref="Risolvi"/>: con un poligono, il primo candidato coerente (in m² o in ettari);
/// i candidati ambigui si saltano e, se nessuno è coerente, la superficie resta null — meglio
/// mancante che palesemente errata. Senza poligono non c'è modo di verificare: si usa il primo
/// candidato assumendolo in m².</item>
/// </list>
/// TODO qualità dati geografici: poligoni enormi (es. vigneti 16792/16793, ~30.000 ha) non
/// vengono filtrati né corretti qui: sono da gestire con controlli di qualità espliciti, non
/// implicitamente nel visualizzatore.
/// </summary>
public static class SuperficieFourGrapes
{
    public const decimal MetriQuadratiPerEttaro = 10_000m;

    /// <summary>
    /// Scarto massimo (moltiplicativo, in entrambe le direzioni) fra il valore e l'area del
    /// poligono per considerarli coerenti: 2 → rapporto fra 0,5 e 2. Largo abbastanza per le
    /// differenze fra superficie dichiarata/misurata e geometrica (nei dati reali P10–P90 del
    /// rapporto è ~0,75–1,35), mentre m² ed ettari differiscono di un fattore 10.000: le due
    /// bande non si sovrappongono mai.
    /// </summary>
    public const double TolleranzaCoerenza = 2.0;

    public static IEnumerable<SuperficieGrezza> Candidati(
        decimal? misurata, decimal? dichiarata, decimal? calcolataDaGis, decimal? superficie)
    {
        if (misurata > 0) yield return new SuperficieGrezza(misurata.Value, SorgenteSuperficie.Misurata);
        if (dichiarata > 0) yield return new SuperficieGrezza(dichiarata.Value, SorgenteSuperficie.Dichiarata);
        if (calcolataDaGis > 0) yield return new SuperficieGrezza(calcolataDaGis.Value, SorgenteSuperficie.CalcolataDaGis);
        if (superficie > 0) yield return new SuperficieGrezza(superficie.Value, SorgenteSuperficie.Superficie);
    }

    /// <summary>Primo candidato positivo, senza controllo di coerenza col poligono.</summary>
    public static SuperficieGrezza? SelezionaGrezza(
        decimal? misurata, decimal? dichiarata, decimal? calcolataDaGis, decimal? superficie)
    {
        foreach (var candidato in Candidati(misurata, dichiarata, calcolataDaGis, superficie))
        {
            return candidato;
        }

        return null;
    }

    /// <summary>
    /// Interpreta un valore grezzo positivo rispetto all'area del poligono: coerente in m²
    /// (valore / area fra 0,5 e 2), altrimenti coerente in ettari (valore × 10.000 / area fra 0,5
    /// e 2), altrimenti ambiguo. Senza un'area utilizzabile (null, non finita o ≤ 0) →
    /// <see cref="InterpretazioneSuperficie.MetriQuadratiPresunti"/>.
    /// </summary>
    public static InterpretazioneSuperficie Interpreta(decimal grezza, double? areaPoligonoMq)
    {
        if (!AreaUtilizzabile(areaPoligonoMq))
        {
            return InterpretazioneSuperficie.MetriQuadratiPresunti;
        }

        var area = areaPoligonoMq!.Value;
        if (Coerente((double)grezza, area))
        {
            return InterpretazioneSuperficie.MetriQuadrati;
        }

        if (Coerente((double)grezza * (double)MetriQuadratiPerEttaro, area))
        {
            return InterpretazioneSuperficie.Ettari;
        }

        return InterpretazioneSuperficie.Ambigua;
    }

    /// <summary>Converte in ettari un singolo valore; null se il valore è null, ≤ 0 o ambiguo.</summary>
    public static decimal? NormalizzaInEttari(decimal? grezza, double? areaPoligonoMq)
    {
        if (grezza is not > 0)
        {
            return null;
        }

        return Interpreta(grezza.Value, areaPoligonoMq) switch
        {
            InterpretazioneSuperficie.MetriQuadrati or InterpretazioneSuperficie.MetriQuadratiPresunti
                => grezza.Value / MetriQuadratiPerEttaro,
            InterpretazioneSuperficie.Ettari => grezza.Value,
            _ => null,
        };
    }

    /// <summary>Primo candidato coerente con il poligono (vedi descrizione della classe).</summary>
    public static SuperficieRisolta Risolvi(
        decimal? misurata, decimal? dichiarata, decimal? calcolataDaGis, decimal? superficie, double? areaPoligonoMq)
    {
        var almenoUnCandidato = false;
        foreach (var candidato in Candidati(misurata, dichiarata, calcolataDaGis, superficie))
        {
            almenoUnCandidato = true;
            var interpretazione = Interpreta(candidato.Valore, areaPoligonoMq);
            if (interpretazione != InterpretazioneSuperficie.Ambigua)
            {
                return new SuperficieRisolta(
                    NormalizzaInEttari(candidato.Valore, areaPoligonoMq), candidato.Sorgente, interpretazione);
            }
        }

        return almenoUnCandidato
            ? new SuperficieRisolta(null, null, InterpretazioneSuperficie.Ambigua)
            : new SuperficieRisolta(null, null, null);
    }

    private static bool AreaUtilizzabile(double? areaPoligonoMq)
    {
        return areaPoligonoMq is { } area && double.IsFinite(area) && area > 0;
    }

    private static bool Coerente(double valoreMq, double areaMq)
    {
        var rapporto = valoreMq / areaMq;
        return rapporto >= 1 / TolleranzaCoerenza && rapporto <= TolleranzaCoerenza;
    }
}

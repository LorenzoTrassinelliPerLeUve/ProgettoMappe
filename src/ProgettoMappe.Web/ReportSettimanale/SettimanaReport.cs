using System.Globalization;
using ProgettoMappe.Web.ReportFineStagione;

namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>
/// Una settimana del report settimanale (task #213): da domenica a sabato, numerata come <c>Week_Ref</c> di Qlik (la
/// settimana ISO del giorno dopo, vedi <see cref="PioggiaCumulata.SpostamentoSettimana"/>). Nell'indirizzo è
/// «AAAA-SS» (<see cref="Codice"/>). Le etichette di <c>WEEK_REF</c> dell'app Qlik arrivano col lavoro del principale
/// (task #211): finché non ci sono, la settimana si mostra col numero e le date.
/// </summary>
public sealed record SettimanaReport(int Anno, int Numero)
{
    /// <summary>La domenica con cui comincia.</summary>
    public DateOnly Da => DateOnly.FromDateTime(ISOWeek.ToDateTime(Anno, Numero, DayOfWeek.Monday)).AddDays(-PioggiaCumulata.SpostamentoSettimana);

    /// <summary>Il sabato con cui finisce.</summary>
    public DateOnly A => Da.AddDays(6);

    public string Codice => $"{Anno}-{Numero:00}";

    /// <summary>La settimana che contiene il giorno.</summary>
    public static SettimanaReport Di(DateOnly giorno)
    {
        var spostato = giorno.ToDateTime(TimeOnly.MinValue).AddDays(PioggiaCumulata.SpostamentoSettimana);
        return new SettimanaReport(ISOWeek.GetYear(spostato), ISOWeek.GetWeekOfYear(spostato));
    }

    /// <summary>«2026-46» → settimana; null se il testo non è una settimana esistente.</summary>
    public static SettimanaReport? DaCodice(string? codice)
    {
        var parti = codice?.Split('-');
        return parti is [var a, var s]
            && int.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var anno) && anno is >= 2000 and <= 2100
            && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var numero)
            && numero >= 1 && numero <= ISOWeek.GetWeeksInYear(anno)
                ? new SettimanaReport(anno, numero)
                : null;
    }

    /// <summary>Le settimane da scegliere: dalla più recente (quella di oggi, come Qlik) indietro fino all'inizio dell'anno prima.</summary>
    public static IReadOnlyList<SettimanaReport> Elenco(DateOnly oggi)
    {
        var lista = new List<SettimanaReport>();
        for (var s = Di(oggi); s.Anno >= Di(oggi).Anno - 1; s = Di(s.Da.AddDays(-1)))
        {
            lista.Add(s);
        }
        return lista;
    }

    public string Etichetta(LinguaReport lingua) =>
        TestiSettimanale.Testo(lingua, "settimana.etichetta", Numero, Anno, Da.ToString("dd/MM", CultureInfo.InvariantCulture),
            A.ToString("dd/MM", CultureInfo.InvariantCulture));
}

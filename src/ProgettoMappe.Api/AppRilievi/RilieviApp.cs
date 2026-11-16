using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.AppRilievi;

/// <summary>
/// Una colonna della tabella del tipo (script del dba del 16/11): il nome della proprietà del DTO è il nome della colonna.
/// Testo: <paramref name="Lunghezza"/> e, se ci sono, i valori <paramref name="Ammessi"/> (CHECK della tabella, si accettano
/// in maiuscolo o minuscolo e si scrivono come nel CHECK). Numeri: da 0 a <paramref name="Massimo"/>; i decimali si
/// arrotondano a <paramref name="Scala"/> cifre come la colonna.
/// </summary>
public sealed record CampoRilievoApp(string Colonna, string Descrizione, int? Lunghezza = null, decimal? Massimo = null, int Scala = 0,
    IReadOnlyList<string>? Ammessi = null, bool Obbligatorio = false)
{
    public string NomeJson => JsonNamingPolicy.CamelCase.ConvertName(Colonna);

    public string Etichetta => $"{Descrizione} ({NomeJson})";

    public static CampoRilievoApp Testo(string colonna, string descrizione, int lunghezza, IReadOnlyList<string>? ammessi = null,
        bool obbligatorio = false) => new(colonna, descrizione, Lunghezza: lunghezza, Ammessi: ammessi, Obbligatorio: obbligatorio);

    public static CampoRilievoApp Intero(string colonna, string descrizione, int massimo) => new(colonna, descrizione, Massimo: massimo);

    /// <summary>decimal(<paramref name="precisione"/>, <paramref name="scala"/>); <paramref name="massimo"/> più stretto se serve (le %).</summary>
    public static CampoRilievoApp Decimale(string colonna, string descrizione, int precisione, int scala, decimal? massimo = null) =>
        new(colonna, descrizione, Massimo: massimo ?? MassimoDecimale(precisione, scala), Scala: scala);

    public static CampoRilievoApp Bit(string colonna, string descrizione) => new(colonna, descrizione);

    /// <summary>Il più grande valore di un decimal(p, s): 10^(p−s) − 10^−s.</summary>
    public static decimal MassimoDecimale(int precisione, int scala)
    {
        var passo = 1m;
        for (var i = 0; i < scala; i++) passo /= 10m;
        var intero = 1m;
        for (var i = 0; i < precisione - scala; i++) intero *= 10m;
        return intero - passo;
    }
}

/// <summary>Un corpo di POST già controllato: i campi comuni e le colonne della tabella del tipo (senza codice e IdRilievo).</summary>
public sealed record RilievoAppValidato(string Codice, int IdAzienda, int IdVigneto, DateTime Data, string? Note,
    IReadOnlyDictionary<string, object?> Valori);

/// <summary>
/// Un tipo di rilievo delle app di Jan: il tipo in 4Grapes, chi lo può scrivere, le colonne e come si passa dal DTO
/// <typeparamref name="T"/> (corpo del POST) alle colonne e indietro. Campi comuni del DTO, che non sono colonne della tabella
/// del tipo: il codice (colonna <see cref="TipoRilievoApp.ColonnaCodice"/>), IdAzienda, IdVigneto, Data, Note.
/// </summary>
public sealed class DefinizioneRilievoApp<T> where T : class
{
    public const int LunghezzaNote = 2000;

    private static readonly string[] Comuni = ["IdAzienda", "IdVigneto", "Data", "Note"];

    public DefinizioneRilievoApp(TipoRilievoApp tipo, string descrizioneCodice, int lunghezzaCodice, string? codicePortale,
        IReadOnlyList<CampoRilievoApp> campi)
    {
        Tipo = tipo;
        DescrizioneCodice = descrizioneCodice;
        LunghezzaCodice = lunghezzaCodice;
        CodicePortale = codicePortale;
        Campi = campi;
    }

    public TipoRilievoApp Tipo { get; }

    public string DescrizioneCodice { get; }

    public int LunghezzaCodice { get; }

    /// <summary>Abilitazione del portale che vale come quella dell'app (IRRORATRICI per le prove), null = solo quella dell'app.</summary>
    public string? CodicePortale { get; }

    public IReadOnlyList<CampoRilievoApp> Campi { get; }

    /// <summary>Le proprietà del DTO che non sono né comuni né colonne: devono essere zero (lo controlla un test).</summary>
    public IEnumerable<string> ProprietaSenzaColonna() =>
        typeof(T).GetProperties().Select(p => p.Name)
            .Where(n => n != Tipo.ColonnaCodice && !Comuni.Contains(n) && Campi.All(c => c.Colonna != n));

    private static object? Leggi(T dati, string proprieta) => typeof(T).GetProperty(proprieta)!.GetValue(dati);

    public static string? Pulito(string? testo) => string.IsNullOrWhiteSpace(testo) ? null : testo.Trim();

    public string? Codice(T dati) => Pulito(Leggi(dati, Tipo.ColonnaCodice) as string);

    /// <summary>
    /// Controlla il corpo del POST. Restituisce le colonne della tabella del tipo già pulite (testi senza spazi ai bordi, valori
    /// ammessi scritti come nel CHECK, decimali arrotondati, Definitiva null = false) e i problemi, una frase per campo.
    /// La data si porta in ora italiana se arriva con il fuso; ammessa dal 2000 a domani.
    /// </summary>
    public (RilievoAppValidato? Rilievo, List<string> Problemi) Valida(T dati, DateTime oggiItalia)
    {
        var problemi = new List<string>();
        var etichettaCodice = $"{DescrizioneCodice} ({JsonNamingPolicy.CamelCase.ConvertName(Tipo.ColonnaCodice)})";
        if (Codice(dati) is not { } codice) problemi.Add($"{etichettaCodice}: campo obbligatorio.");
        else if (codice.Length > LunghezzaCodice) problemi.Add($"{etichettaCodice}: al massimo {LunghezzaCodice} caratteri.");
        if (Leggi(dati, "IdAzienda") is not int azienda) problemi.Add("Azienda (idAzienda): campo obbligatorio.");
        else if (azienda <= 0) problemi.Add("Azienda (idAzienda): l'IdEnte dell'azienda in 4Grapes.");
        if (Leggi(dati, "IdVigneto") is not int vigneto) problemi.Add("Vigneto (idVigneto): campo obbligatorio.");
        else if (vigneto <= 0) problemi.Add("Vigneto (idVigneto): l'IdVigneto in 4Grapes.");
        var data = Leggi(dati, "Data") is DateTime d ? OraItaliana(d) : (DateTime?)null;
        if (data is null) problemi.Add("Data (data): campo obbligatorio.");
        else if (data.Value.Year < 2000 || data.Value.Date > oggiItalia.Date.AddDays(1))
            problemi.Add("Data (data): dal 2000 a oggi.");
        if (Pulito(Leggi(dati, "Note") as string) is { Length: > LunghezzaNote })
            problemi.Add("Note (note): al massimo 2.000 caratteri.");

        var valori = new Dictionary<string, object?>();
        foreach (var campo in Campi)
        {
            var (valore, problema) = Converti(campo, Leggi(dati, campo.Colonna));
            if (problema is not null) problemi.Add($"{campo.Etichetta}: {problema}");
            valori[campo.Colonna] = valore;
        }

        return problemi.Count > 0
            ? (null, problemi)
            : (new RilievoAppValidato(Codice(dati)!, (int)Leggi(dati, "IdAzienda")!, (int)Leggi(dati, "IdVigneto")!, data!.Value,
                Pulito(Leggi(dati, "Note") as string), valori), problemi);
    }

    private static (object? Valore, string? Problema) Converti(CampoRilievoApp campo, object? valore)
    {
        switch (valore)
        {
            case null when campo.Obbligatorio:
                return (null, "campo obbligatorio.");
            case null when campo.Lunghezza is null && campo.Massimo is null:
                return (false, null); // bit NOT NULL con default 0
            case null:
                return (null, null);
            case string testo:
                var pulito = Pulito(testo);
                if (pulito is null) return (null, campo.Obbligatorio ? "campo obbligatorio." : null);
                if (campo.Ammessi is { } ammessi)
                {
                    var scelto = ammessi.FirstOrDefault(a => string.Equals(a, pulito, StringComparison.OrdinalIgnoreCase));
                    return scelto is null ? (null, $"uno fra {string.Join(", ", ammessi)}.") : (scelto, null);
                }

                return pulito.Length > campo.Lunghezza ? (null, $"al massimo {campo.Lunghezza} caratteri.") : (pulito, null);
            case int intero:
                return intero < 0 || intero > campo.Massimo ? (null, $"fra 0 e {Numero(campo.Massimo!.Value)}.") : (intero, null);
            case decimal numero:
                var arrotondato = Math.Round(numero, campo.Scala, MidpointRounding.AwayFromZero);
                return arrotondato < 0 || arrotondato > campo.Massimo ? (null, $"fra 0 e {Numero(campo.Massimo!.Value)}.") : (arrotondato, null);
            default:
                return (valore, null);
        }
    }

    private static string Numero(decimal n) => n.ToString("#,##0.###", CultureInfo.GetCultureInfo("it-IT"));

    /// <summary>Una data con il fuso (Utc o Local) diventa ora italiana; senza fuso resta com'è.</summary>
    public static DateTime OraItaliana(DateTime data) => data.Kind == DateTimeKind.Unspecified
        ? data
        : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(data.ToUniversalTime(), TimeZoneInfo.Utc, FusoItalia), DateTimeKind.Unspecified);

    public static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    /// <summary>Il rilievo letto da 4Grapes nella forma del POST (le colonne che non sono nel DTO si ignorano).</summary>
    public T Dati(RilievoApp rilievo)
    {
        var costruttore = typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var argomenti = costruttore.GetParameters().Select(p => p.Name switch
        {
            "IdAzienda" => rilievo.IdEnte,
            "IdVigneto" => rilievo.IdVigneto,
            "Data" => rilievo.InizioRilievo,
            "Note" => rilievo.Note,
            _ => ComeParametro(rilievo.Valori.GetValueOrDefault(p.Name!), p),
        }).ToArray();
        return (T)costruttore.Invoke(argomenti);
    }

    private static object? ComeParametro(object? valore, ParameterInfo parametro)
    {
        if (valore is null) return null;
        var tipo = Nullable.GetUnderlyingType(parametro.ParameterType) ?? parametro.ParameterType;
        return tipo.IsInstanceOfType(valore) ? valore : Convert.ChangeType(valore, tipo, CultureInfo.InvariantCulture);
    }
}

/// <summary>I due tipi del 16/11 (task #261/#265): colonne e limiti dallo script del dba.</summary>
public static class RilieviApp
{
    private const decimal Cento = 100m;

    /// <summary>Prova irroratrice (4Sprayers): vale l'abilitazione dell'app o IRRORATRICI del portale, come per le irroratrici.</summary>
    public static readonly DefinizioneRilievoApp<ProvaIrroratriceAppDto> ProveIrroratrice = new(
        TipiRilievoApp.ProvaIrroratrice, "Codice della prova", 30, "IRRORATRICI",
        [
            CampoRilievoApp.Testo("TipoProva", "Tipo di prova", 20),
            CampoRilievoApp.Intero("IdIrroratrice", "Irroratrice", int.MaxValue),
            CampoRilievoApp.Intero("StadioBBCH", "Fase BBCH", 99),
            CampoRilievoApp.Decimale("VelocitaKmh", "Velocità (km/h)", 5, 2),
            CampoRilievoApp.Decimale("PressioneBar", "Pressione (bar)", 6, 2),
            CampoRilievoApp.Decimale("VolumeLHa", "Volume (l/ha)", 7, 1),
            CampoRilievoApp.Intero("NumeroUgelli", "Numero di ugelli", 10_000),
            CampoRilievoApp.Decimale("PortataLMin", "Portata per ugello (l/min)", 6, 2),
            CampoRilievoApp.Decimale("CoperturaMediaPerc", "Copertura media (%)", 5, 2, Cento),
            CampoRilievoApp.Testo("ClasseCopertura", "Classe di copertura", 20, ["SCARSO", "DISCRETO", "OTTIMALE", "ECCESSIVO"]),
            CampoRilievoApp.Decimale("CoperturaBassaPerc", "Copertura in basso (%)", 5, 2, Cento),
            CampoRilievoApp.Decimale("CoperturaAltaPerc", "Copertura in alto (%)", 5, 2, Cento),
            CampoRilievoApp.Decimale("CoperturaSopraPerc", "Copertura sopra la foglia (%)", 5, 2, Cento),
            CampoRilievoApp.Decimale("CoperturaSottoPerc", "Copertura sotto la foglia (%)", 5, 2, Cento),
            CampoRilievoApp.Intero("CartineAnalizzate", "Cartine analizzate", 100_000),
            CampoRilievoApp.Bit("Definitiva", "Prova chiusa"),
            CampoRilievoApp.Decimale("DensitaGocceCm2", "Densità delle gocce (gocce/cm²)", 8, 1),
            CampoRilievoApp.Decimale("VmdMicron", "VMD (µm)", 7, 1),
            CampoRilievoApp.Decimale("UniformitaCvPerc", "Uniformità (CV %)", 6, 2),
            CampoRilievoApp.Testo("Esito", "Esito", 400),
            CampoRilievoApp.Testo("Consiglio", "Consiglio", 1000),
        ]);

    /// <summary>Riassunto potatura (App potatura): vale solo l'abilitazione dell'app.</summary>
    public static readonly DefinizioneRilievoApp<RiassuntoPotaturaAppDto> RiassuntiPotatura = new(
        TipiRilievoApp.RiassuntoPotatura, "Codice del rilievo nell'app", 50, null,
        [
            CampoRilievoApp.Testo("TipoConteggio", "Tipo di conteggio", 20, ["Infiorescenze", "Grappoli"], obbligatorio: true),
            CampoRilievoApp.Testo("Struttura", "Struttura (sperone o capo)", 20),
            CampoRilievoApp.Intero("PianteRilevate", "Piante rilevate", 100_000),
            CampoRilievoApp.Decimale("GemmeLasciatePerPianta", "Gemme lasciate per pianta", 6, 2),
            CampoRilievoApp.Decimale("GermogliPerPianta", "Germogli per pianta", 6, 2),
            CampoRilievoApp.Decimale("GermogliamentoPerc", "Germogliamento (%)", 6, 2),
            CampoRilievoApp.Decimale("FertilitaPerGemma", "Fertilità per gemma", 6, 3),
            CampoRilievoApp.Decimale("FertilitaPerGermoglio", "Fertilità per germoglio", 6, 3),
            CampoRilievoApp.Decimale("StimaPerPianta", "Stima per pianta", 7, 2),
            CampoRilievoApp.Decimale("KgPerCeppo", "Kg per ceppo", 7, 3),
            CampoRilievoApp.Intero("PesoGrappoloG", "Peso del grappolo (g)", 100_000),
            CampoRilievoApp.Decimale("LunghezzaMediaGermogliCm", "Lunghezza media dei germogli (cm)", 6, 1),
            CampoRilievoApp.Decimale("UniformitaCvPerc", "Uniformità dei germogli (CV %)", 6, 2),
        ]);
}

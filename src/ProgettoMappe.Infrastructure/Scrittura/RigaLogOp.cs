namespace ProgettoMappe.Infrastructure.Scrittura;

/// <summary>
/// Una riga di <c>dbo.LogOp</c>, con le regole di <c>LogOpsExtension.LogOp</c> del portale (verificate dal dba sulle
/// 91.184 righe esistenti, 2026-11-14):
/// <list type="bullet">
/// <item><c>Operazione</c> in minuscolo: <c>nuovo</c>, <c>modificato</c>, <c>cancellato</c>;</item>
/// <item><c>Tabella</c> = nome della tabella;</item>
/// <item><c>IdRiga</c> = valore della colonna <c>Id</c> se è un intero, altrimenti 0 (mai NULL): per Rilievo
///   (IdRilievo datetime2) e Vigneto (IdVigneto) il portale scrive 0;</item>
/// <item><c>Data</c> = ora locale italiana (il portale usa <c>DateTime.Now</c> del server in Italia);</item>
/// <item><c>Json</c> = la riga intera dopo un inserimento o una modifica, quella di prima per una cancellazione.</item>
/// </list>
/// </summary>
public sealed record RigaLogOp(int IdUtente, DateTime Data, string Operazione, string Tabella, int IdRiga, string Json)
{
    public const string Nuovo = "nuovo";
    public const string Modificato = "modificato";
    public const string Cancellato = "cancellato";

    private static readonly TimeZoneInfo FusoItalia = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    public static RigaLogOp Crea(int idUtente, DateTimeOffset adesso, string operazione, string tabella,
        IReadOnlyList<KeyValuePair<string, object?>> riga)
    {
        var data = DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(adesso, FusoItalia).DateTime, DateTimeKind.Unspecified);
        var id = riga.FirstOrDefault(c => c.Key == "Id").Value;
        return new RigaLogOp(idUtente, data, operazione, tabella, id is int n ? n : 0, JsonLogOp.Scrivi(riga));
    }
}

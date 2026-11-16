namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Un controllo del portale fallito dentro la transazione di un salvataggio (es. «Nome già esistente»): la transazione si
/// annulla e <see cref="PortaleControllerBase.ScriviAsync"/> risponde 400 con il messaggio (sul campo, se c'è).
/// </summary>
public class RifiutoPortaleException(string messaggio, params string[] campi) : InvalidOperationException(messaggio)
{
    public IReadOnlyList<string> Campi { get; } = campi;
}

namespace ProgettoMappe.Domain;

/// <summary>
/// Valori noti (ma non esaustivi) per <see cref="Entities.LayerDataset.Tipo"/>. Un nuovo tipo
/// di layer agronomico si aggiunge come nuova stringa, senza modificare questa lista né
/// ricompilare l'applicazione: qui sono raccolte solo le costanti più comuni, per comodità.
/// </summary>
public static class LayerTipi
{
    public const string Confine = "confine";
    public const string Ndvi = "ndvi";
    public const string Vigore = "vigore";
    public const string Produzione = "produzione";
    public const string Zonazione = "zonazione";
    public const string MappaPrescrittiva = "mappa-prescrittiva";
    public const string Meteo = "meteo";
    public const string Suolo = "suolo";
}

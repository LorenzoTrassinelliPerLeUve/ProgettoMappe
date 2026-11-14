using System.Globalization;
using System.Text;

namespace ProgettoMappe.Infrastructure.Scrittura;

/// <summary>
/// Il campo <c>LogOp.Json</c> scritto come lo scrive il portale (<c>LogOpsExtension.LogOp</c>: un <c>JObject</c> con
/// tutte le proprietà della riga, <c>ToString(Formatting.None)</c> di Newtonsoft con le impostazioni predefinite):
/// compatto, testo non ASCII lasciato com'è, date ISO senza fuso e senza zeri finali nei decimali di secondo
/// (<c>2026-11-11T19:03:18.495254</c>), decimali sempre con il punto (<c>12.0</c>), byte in base64.
/// Le proprietà sono le colonne della tabella, nell'ordine della SELECT.
/// </summary>
public static class JsonLogOp
{
    public static string Scrivi(IEnumerable<KeyValuePair<string, object?>> colonne)
    {
        var testo = new StringBuilder("{");
        var prima = true;
        foreach (var (nome, valore) in colonne)
        {
            if (!prima)
            {
                testo.Append(',');
            }

            prima = false;
            Stringa(testo, nome);
            testo.Append(':');
            Valore(testo, valore);
        }

        return testo.Append('}').ToString();
    }

    private static void Valore(StringBuilder testo, object? valore)
    {
        switch (valore)
        {
            case null or DBNull:
                testo.Append("null");
                break;
            case string s:
                Stringa(testo, s);
                break;
            case bool b:
                testo.Append(b ? "true" : "false");
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                testo.Append(Convert.ToString(valore, CultureInfo.InvariantCulture));
                break;
            case decimal d:
                testo.Append(ConPunto(d.ToString(CultureInfo.InvariantCulture)));
                break;
            case double d:
                Reale(testo, d, d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                Reale(testo, f, f.ToString("R", CultureInfo.InvariantCulture));
                break;
            case DateTime dt:
                testo.Append('"').Append(Data(dt)).Append('"');
                break;
            case DateTimeOffset dto:
                testo.Append('"').Append(dto.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture)).Append('"');
                break;
            case Guid g:
                Stringa(testo, g.ToString("D"));
                break;
            case byte[] dati:
                Stringa(testo, Convert.ToBase64String(dati));
                break;
            case TimeSpan t:
                Stringa(testo, t.ToString("c", CultureInfo.InvariantCulture));
                break;
            default:
                Stringa(testo, Convert.ToString(valore, CultureInfo.InvariantCulture) ?? "");
                break;
        }
    }

    /// <summary>Data come <c>DateTimeUtils.WriteDateTimeString</c> di Newtonsoft (IsoDateFormat, RoundtripKind).</summary>
    public static string Data(DateTime dt)
    {
        var testo = dt.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
        return dt.Kind switch
        {
            DateTimeKind.Utc => testo + "Z",
            DateTimeKind.Local => testo + dt.ToString("zzz", CultureInfo.InvariantCulture),
            _ => testo,
        };
    }

    private static void Reale(StringBuilder testo, double valore, string formattato)
    {
        if (double.IsNaN(valore) || double.IsInfinity(valore))
        {
            // FloatFormatHandling.String, il predefinito di Newtonsoft.
            Stringa(testo, formattato);
            return;
        }

        testo.Append(ConPunto(formattato));
    }

    /// <summary>Come <c>JsonConvert.EnsureDecimalPlace</c>: 12 → 12.0, 1E+20 resta com'è.</summary>
    private static string ConPunto(string numero) =>
        numero.Contains('.') || numero.Contains('E') || numero.Contains('e') ? numero : numero + ".0";

    /// <summary>Stringa con gli stessi escape di Newtonsoft (StringEscapeHandling.Default).</summary>
    private static void Stringa(StringBuilder testo, string valore)
    {
        testo.Append('"');
        foreach (var c in valore)
        {
            switch (c)
            {
                case '"': testo.Append("\\\""); break;
                case '\\': testo.Append("\\\\"); break;
                case '\b': testo.Append("\\b"); break;
                case '\f': testo.Append("\\f"); break;
                case '\n': testo.Append("\\n"); break;
                case '\r': testo.Append("\\r"); break;
                case '\t': testo.Append("\\t"); break;
                case < ' ' or (char)0x85 or (char)0x2028 or (char)0x2029:
                    testo.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default: testo.Append(c); break;
            }
        }

        testo.Append('"');
    }
}

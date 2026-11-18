using System.Globalization;

namespace ProgettoMappe.BilancioIdrico.Parita;

/// <summary>
/// I csv del dataset di riferimento (docs/specifiche/bilancio-idrico/dati/LEGGIMI.md): UTF-8, separatore «;», punto
/// decimale, date AAAA-MM-GG, cella vuota = NULL, True/False.
/// </summary>
public sealed class TabelleCsv(string cartella)
{
    private readonly Dictionary<string, IReadOnlyList<Riga>> _tabelle = [];

    public string Cartella { get; } = cartella;

    public IReadOnlyList<Riga> this[string tabella]
    {
        get
        {
            if (!_tabelle.TryGetValue(tabella, out var righe))
                _tabelle[tabella] = righe = Leggi(Path.Combine(Cartella, tabella + ".csv"));
            return righe;
        }
    }

    private static List<Riga> Leggi(string percorso)
    {
        if (!File.Exists(percorso))
            return [];
        using var lettore = new StreamReader(percorso);
        var intestazione = lettore.ReadLine()?.TrimStart('﻿').Split(';') ?? [];
        var colonne = intestazione.Select((nome, i) => (nome, i)).ToDictionary(c => c.nome, c => c.i);
        var righe = new List<Riga>();
        while (lettore.ReadLine() is { } linea)
            if (linea.Length > 0)
                righe.Add(new Riga(colonne, linea.Split(';')));
        return righe;
    }
}

public sealed class Riga(Dictionary<string, int> colonne, string[] celle)
{
    public string? Testo(string colonna) =>
        colonne.TryGetValue(colonna, out var i) && i < celle.Length && celle[i].Length > 0 ? celle[i] : null;

    public double? Numero(string colonna) =>
        Testo(colonna) is { } t ? double.Parse(t, NumberStyles.Float, CultureInfo.InvariantCulture) : null;

    public int? Intero(string colonna) => Numero(colonna) is { } n ? (int)n : null;

    public int IntObbl(string colonna) => Intero(colonna) ?? throw new InvalidDataException($"{colonna} vuota");

    public bool Vero(string colonna) => string.Equals(Testo(colonna), "True", StringComparison.OrdinalIgnoreCase);

    /// <summary>Data (anche con l'ora, che si ignora).</summary>
    public DateOnly? Data(string colonna) =>
        Testo(colonna) is { } t ? DateOnly.ParseExact(t[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
}

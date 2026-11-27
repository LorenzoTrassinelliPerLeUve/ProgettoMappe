namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Trattamenti di esempio per l'Api senza 4Grapes (foto e prove UX del Portale, task #387): due aziende inventate, una
/// settimana di trattamenti attorno a oggi con tutti gli stati (fatto, da fare, altre avversità, automatico). Mai con 4Grapes.
/// </summary>
public static class EsempioTrattamenti
{
    private static readonly (int Id, string Nome, int Azienda)[] Vigneti =
    [
        (9001, "Poggio alto", 9101), (9002, "Poggio basso", 9101), (9003, "Le Fornaci", 9101), (9004, "Campo del pozzo", 9101),
        (9005, "Vigna vecchia", 9101), (9006, "Sant'Anna", 9101), (9007, "Il Fosso", 9101),
        (9011, "Marina nord", 9102), (9012, "Marina sud", 9102), (9013, "La Pineta", 9102), (9014, "Dune", 9102),
    ];

    private static readonly Dictionary<int, string> Aziende = new()
    {
        [9101] = "Azienda di esempio Collina", [9102] = "Tenuta di esempio Mare", [9103] = "Azienda di esempio Grande",
    };

    // Task #389: un'azienda grande come la giornata più piena di Mirafiore (dba, 30/5/2025): 88 vigneti in 26 vigne, nomi
    // fino a 40 caratteri (media 21), 4 trattamenti per vigneto nello stesso giorno (2 effettivi, 2 suggeriti), 2 prodotti.
    private static readonly string[] Vigne =
    [
        "Bussia", "Ginestra", "Sorì Ginestra", "Mosconi", "Gattera", "Cerequio", "Brunate", "Castelletto", "Ravera", "Perno",
        "Villero", "Rocche", "Monprivato", "Lazzarito", "Serralunga", "Baudana", "Bricco Rocche", "Cannubi", "Sarmassa",
        "Liste", "Fossati", "Coste di Rose", "Mondoca", "Pira", "Arborina", "Annunziata",
    ];

    private static readonly string[] Parti = ["Alto", "Basso", "Nuovo impianto 2018", "Sotto la strada", "Est", "Ovest - reimpianto barbera"];

    private static readonly (int Id, string Nome, int Azienda)[] VignetiGrandi = Enumerable.Range(0, 88)
        .Select(i => (9201 + i, $"{Vigne[i % Vigne.Length]} {Parti[i / Vigne.Length % Parti.Length]}", 9103)).ToArray();

    private static readonly ProdottoTrattamento[] Prodotti =
    [
        new(12, "PERONOSPORA", "11234", "Rame idrossido 30%", 1.5m),
        new(11, "OIDIO", "10876", "Zolfo bagnabile 80%", 3m),
        new(31, "BOTRITE", "12001", "Fenexamid", 1m),
    ];

    public static DatiTrattamenti Dati(DateTime dal)
    {
        var vigneti = Vigneti.Concat(VignetiGrandi).Select(v => new VignetoTrattamenti(v.Id, v.Nome, v.Azienda, Aziende[v.Azienda], true, "Toscana", "Toscana",
            "LI", "Livorno", "Castagneto Carducci", "Castagneto Carducci", 1, "Linea standard", 1, v.Id % 2 == 0 ? "Sangiovese" : "Merlot",
            1, "ALTO / BASSO")).ToList();
        var oggi = dal.Date.AddDays(CalcoloTrattamenti.GiorniPrima);
        var trattamenti = new List<TrattamentoLetto>();
        var id = 70000;
        foreach (var (v, i) in Vigneti.Select((v, i) => (v, i)))
        {
            // Fatto tre o due giorni fa; da fare oggi o domani; i vigneti pari anche l'oidio; ogni tanto altre avversità o automatico.
            trattamenti.Add(Nuovo(++id, v.Id, oggi.AddDays(-3 + i % 2).AddHours(7), effettivo: true, prodotti: [Prodotti[0], Prodotti[1]]));
            var daFare = oggi.AddDays(i % 3 == 2 ? 1 : 0).AddHours(8);
            trattamenti.Add(i % 4 == 3
                ? Nuovo(++id, v.Id, daFare, automatico: true, prodotti: [Prodotti[0]])
                : Nuovo(++id, v.Id, daFare, suggerito: true, prodotti: i % 2 == 0 ? [Prodotti[0], Prodotti[1]] : [Prodotti[0]]));
            if (i % 5 == 1) trattamenti.Add(Nuovo(++id, v.Id, oggi.AddDays(2).AddHours(8), suggerito: true, tipo: CalcoloTrattamenti.AltreAvversita, prodotti: [Prodotti[2]]));
            if (i % 3 == 0) trattamenti.Add(Nuovo(++id, v.Id, oggi.AddDays(4).AddHours(8), suggerito: true, prodotti: [Prodotti[0]]));
        }

        foreach (var v in VignetiGrandi)
        {
            trattamenti.Add(Nuovo(++id, v.Id, oggi.AddHours(6), effettivo: true, prodotti: [Prodotti[0], Prodotti[1]]));
            trattamenti.Add(Nuovo(++id, v.Id, oggi.AddHours(7), effettivo: true, prodotti: [Prodotti[0]]));
            trattamenti.Add(Nuovo(++id, v.Id, oggi.AddHours(8), suggerito: true, prodotti: [Prodotti[0], Prodotti[1]]));
            trattamenti.Add(Nuovo(++id, v.Id, oggi.AddHours(9), suggerito: true, tipo: CalcoloTrattamenti.AltreAvversita, prodotti: [Prodotti[2]]));
        }

        return new DatiTrattamenti(vigneti, trattamenti, [], []);
    }

    private static TrattamentoLetto Nuovo(int id, int vigneto, DateTime data, bool effettivo = false, bool suggerito = false, bool automatico = false,
        int tipo = CalcoloTrattamenti.PeronosporaOidio, ProdottoTrattamento[]? prodotti = null) =>
        new(id, vigneto, data, automatico, suggerito || automatico, effettivo, tipo, tipo == CalcoloTrattamenti.PeronosporaOidio ? "Peronospora e oidio" : "Altre avversità",
            null, null, null, null, null, null, null, prodotti ?? []);
}

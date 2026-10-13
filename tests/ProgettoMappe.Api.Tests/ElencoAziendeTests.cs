using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Filtri;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class ElencoAziendeTests
{
    private static RiepilogoAziendaDto Azienda(int id, string nome, decimal? punteggio = null) =>
        new(id, nome, 1, 1m, 0, punteggio, punteggio is null ? 0 : 1);

    private static readonly IReadOnlyList<RiepilogoAziendaDto> Aziende =
    [
        Azienda(1, "Cantina Sociale", 60m),
        Azienda(2, "Società Agricola Bèrtoli", 80m),
        Azienda(3, "azienda Zorzi"),
        Azienda(4, "Bertoldi Vini", 80m),
    ];

    private static readonly IReadOnlyList<GruppoDto> Gruppi =
    [
        new(4, "VTS Estero", false, [3]),
        new(1, "VTS FVG", false, [1, 2]),
        new(3, "VTS extra FVG", false, [2, 4]),
        new(9, "Vuoto", false, [99]),
    ];

    [Fact]
    public void Ricerca_parziale_senza_maiuscole_ne_accenti()
    {
        var trovate = ElencoAziende.Filtra(Aziende, "  berto ", null, OrdineAziende.Nome);

        Assert.Equal([4, 2], trovate.Select(a => a.Id));
    }

    [Fact]
    public void Per_nome_senza_badare_alle_maiuscole()
    {
        var elenco = ElencoAziende.Filtra(Aziende, null, null, OrdineAziende.Nome);

        Assert.Equal([3, 4, 1, 2], elenco.Select(a => a.Id));
    }

    [Fact]
    public void Per_punteggio_dal_piu_alto_e_senza_punteggio_in_fondo()
    {
        var elenco = ElencoAziende.Filtra(Aziende, null, null, OrdineAziende.Punteggio);

        Assert.Equal([4, 2, 1, 3], elenco.Select(a => a.Id));
    }

    [Fact]
    public void Il_gruppo_restringe_l_elenco()
    {
        var elenco = ElencoAziende.Filtra(Aziende, null, Gruppi[2], OrdineAziende.Nome);

        Assert.Equal([4, 2], elenco.Select(a => a.Id));
    }

    [Fact]
    public void Linguette_in_ordine_di_gruppo_con_i_conteggi_della_ricerca()
    {
        var linguette = ElencoAziende.Linguette(Aziende, Gruppi, "bert");

        Assert.Equal(
            [new LinguettaGruppo(null, "Tutte", 2), new(1, "VTS FVG", 1), new(3, "VTS extra FVG", 2), new(4, "VTS Estero", 0)],
            linguette);
    }

    [Fact]
    public void Numeri_all_italiana()
    {
        Assert.Equal("1.234,6 ha", ElencoAziende.Ettari(1234.56m));
        Assert.Equal("0 ha", ElencoAziende.Ettari(0m));
        Assert.Equal("72,5", ElencoAziende.Punteggio(72.5m));
        Assert.Equal("72", ElencoAziende.Punteggio(72.0m));
    }

    [Fact]
    public void Il_pallino_usa_la_scala_del_punteggio_della_mappa()
    {
        var punteggio = TematismiBigot.Tutti.Single(t => t.Id == "punteggio");

        Assert.Equal(punteggio.Classi[0], TematismiBigot.ClassePunteggio(10m));
        Assert.Equal(punteggio.Classi[4], TematismiBigot.ClassePunteggio(95m));
        Assert.False(TematismiBigot.ClassePunteggio(72.5m).Speciale);
    }
}

using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Filtri;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class RicercaMappaTests
{
    private static VignetoDto V(int id, string nome, int? vignaId = null, string? vigna = null) =>
        new(id, 1, nome, null, null, true, vignaId, vigna);

    private static readonly AziendaDto[] Aziende =
    [
        new(1, "Cantina Collina", null, null),
        new(2, "Tenuta Bassa Collina", null, null),
        new(3, "Società Agricola Né", null, null),
    ];

    private static readonly VignetoDto[] Vigneti =
    [
        V(10, "Collina Est", 100, "Vigna del Colle"),
        V(11, "Piana", 100, "Vigna del Colle"),
        V(12, "Ricollina", 101, "Vigna Grande"),
        V(13, "Senza vigna"),
    ];

    private static IReadOnlyList<RisultatoRicerca> Cerca(string testo) =>
        RicercaMappa.Cerca(testo, Aziende, Vigneti, 1, "Cantina Collina");

    [Theory]
    [InlineData("")]
    [InlineData("co")]
    [InlineData("  c o ")]
    public void Sotto_le_tre_lettere_non_propone_nulla(string testo)
    {
        Assert.Empty(Cerca(testo));
    }

    [Fact]
    public void Prima_chi_comincia_col_testo_poi_chi_ha_una_parola_poi_chi_lo_contiene()
    {
        var risultati = Cerca("collina");

        Assert.Equal(
            [
                (TipoRisultato.Vigneto, 10), // comincia
                (TipoRisultato.Azienda, 1),  // parola
                (TipoRisultato.Azienda, 2),  // parola
                (TipoRisultato.Vigneto, 12), // contiene
            ],
            risultati.Select(r => (r.Tipo, r.Id)));
    }

    [Fact]
    public void Maiuscole_accenti_e_punteggiatura_non_contano()
    {
        Assert.Contains(Cerca("SOCIETA"), r => r.Tipo == TipoRisultato.Azienda && r.Id == 3);
        Assert.Contains(Cerca("agricola ne"), r => r.Tipo == TipoRisultato.Azienda && r.Id == 3);
        Assert.Contains(Cerca("del-colle"), r => r.Tipo == TipoRisultato.Vigna && r.Id == 100);
    }

    [Fact]
    public void Vigneti_e_vigne_dicono_vigna_e_azienda()
    {
        var vigneto = Assert.Single(Cerca("piana"));
        Assert.Equal("Vigna del Colle · Cantina Collina", vigneto.Dettaglio);

        var vigna = Assert.Single(Cerca("grande"));
        Assert.Equal((TipoRisultato.Vigna, 101, "Cantina Collina · 1 vigneto"), (vigna.Tipo, vigna.Id, vigna.Dettaglio));
    }

    [Fact]
    public void L_azienda_aperta_e_segnalata_e_i_vigneti_senza_vigna_non_creano_vigne()
    {
        Assert.Equal("azienda aperta", Cerca("cantina").Single(r => r.Id == 1).Dettaglio);
        Assert.DoesNotContain(Cerca("specificato"), r => r.Tipo == TipoRisultato.Vigna);
        Assert.Equal("Cantina Collina", Assert.Single(Cerca("senza")).Dettaglio);
    }

    [Fact]
    public void I_risultati_sono_al_massimo_quindici()
    {
        var molti = Enumerable.Range(1, 40).Select(i => V(i, $"Filare {i}")).ToList();

        Assert.Equal(RicercaMappa.RisultatiMassimi, RicercaMappa.Cerca("filare", [], molti, 1, null).Count);
    }
}

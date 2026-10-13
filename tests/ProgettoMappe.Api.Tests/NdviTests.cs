using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Api.Esportazione;
using ProgettoMappe.Contracts;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;
using ProgettoMappe.Web.Tematismi;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// NDVI da satellite (task #84): lettura del database SQLite (uno di esempio, creato qui con lo
/// schema del programma notturno ndvi/), risposta dell'endpoint, classi, righe al passaggio scelto,
/// legenda, confronto fra anni, grafico della scheda ed esportazione.
/// </summary>
public sealed class NdviTests : IDisposable
{
    private readonly string _cartella = Path.Combine(Path.GetTempPath(), "ndvi-test-" + Guid.NewGuid().ToString("N"));

    public NdviTests() => Directory.CreateDirectory(_cartella);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_cartella, recursive: true);
    }

    // Lo schema di ndvi/ndvilib/archivio.py.
    private const string Schema = """
        CREATE TABLE ndvi (id_vigneto INTEGER NOT NULL, data TEXT NOT NULL, media REAL, mediana REAL,
            pixel_validi INTEGER NOT NULL, pixel_totali INTEGER NOT NULL, nuvole_pct REAL, PRIMARY KEY (id_vigneto, data));
        CREATE TABLE passaggi (id_azienda INTEGER NOT NULL, data TEXT NOT NULL, nuvole_pct REAL, valido INTEGER NOT NULL,
            file TEXT NOT NULL, elaborato_il TEXT NOT NULL, PRIMARY KEY (id_azienda, data));
        CREATE TABLE aziende (id_azienda INTEGER PRIMARY KEY, nome TEXT NOT NULL);
        """;

    /// <summary>Database di esempio: azienda 7 (vigneti 1, 2) e azienda 8 (vigneto 3), stessi giorni.</summary>
    private string CreaDatabase()
    {
        var percorso = Path.Combine(_cartella, "ndvi.db");
        using var connessione = new SqliteConnection($"Data Source={percorso};Pooling=False");
        connessione.Open();
        using var comando = connessione.CreateCommand();
        comando.CommandText = Schema + """
            INSERT INTO passaggi VALUES (7, '2026-10-11', 5.2, 1, 'a.tif', '2026-10-12T02:00:00+00:00');
            INSERT INTO passaggi VALUES (7, '2026-10-06', 85, 0, 'b.tif', '2026-10-07T02:00:00+00:00');
            INSERT INTO passaggi VALUES (7, '2025-10-10', 12, 1, 'c.tif', '2025-10-11T02:00:00+00:00');
            INSERT INTO passaggi VALUES (8, '2026-10-11', 3, 1, 'd.tif', '2026-10-12T02:00:00+00:00');
            INSERT INTO ndvi VALUES (1, '2026-10-11', 0.62, 0.6, 40, 42, 4.7);
            INSERT INTO ndvi VALUES (1, '2026-10-06', 0.20, 0.2, 5, 42, 88);
            INSERT INTO ndvi VALUES (1, '2025-10-10', 0.55, 0.5, 42, 42, 0);
            INSERT INTO ndvi VALUES (2, '2026-10-11', NULL, NULL, 0, 30, 100);
            INSERT INTO ndvi VALUES (2, '2025-10-10', 0.41, 0.4, 30, 30, 0);
            INSERT INTO ndvi VALUES (3, '2026-10-11', 0.70, 0.7, 10, 10, 0);
            """;
        comando.ExecuteNonQuery();
        return percorso;
    }

    private static LettoreNdvi Lettore(string percorso) => new(Options.Create(new NdviOptions { Database = percorso }));

    // --- Lettura dal database SQLite ---

    [Fact]
    public async Task Si_leggono_solo_i_passaggi_puliti_con_un_valore_e_i_vigneti_chiesti()
    {
        var letto = await Lettore(CreaDatabase()).LeggiAziendaAsync(7, [1, 2], CancellationToken.None);

        Assert.True(letto.Calcolato);
        Assert.Equal([new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 6), new DateOnly(2025, 10, 10)], letto.Passaggi.Select(p => p.Data));
        Assert.False(letto.Passaggi[1].Valido);
        Assert.Equal(5.2m, letto.Passaggi[0].NuvolePct);
        // Il 6/10 è nuvoloso (scartato), il vigneto 2 l'11/10 non ha pixel validi, il vigneto 3 è di un'altra azienda.
        Assert.Equal(
            [(1, new DateOnly(2025, 10, 10), 0.55m), (1, new DateOnly(2026, 10, 11), 0.62m), (2, new DateOnly(2025, 10, 10), 0.41m)],
            letto.Punti.Select(p => (p.IdVigneto, p.Data, p.Media)));
        Assert.Equal(4.7m, letto.Punti[1].NuvolePct);
    }

    [Fact]
    public async Task Senza_il_file_l_ndvi_non_e_ancora_calcolato_e_il_file_non_si_crea()
    {
        var percorso = Path.Combine(_cartella, "manca.db");

        var letto = await Lettore(percorso).LeggiAziendaAsync(7, [1], CancellationToken.None);

        Assert.False(letto.Calcolato);
        Assert.Empty(letto.Punti);
        Assert.False(File.Exists(percorso));
    }

    [Fact]
    public async Task Il_database_si_apre_in_sola_lettura()
    {
        var percorso = CreaDatabase();
        await Lettore(percorso).LeggiAziendaAsync(7, [1], CancellationToken.None);

        var stringa = new SqliteConnectionStringBuilder { DataSource = percorso, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        using var connessione = new SqliteConnection(stringa.ToString());
        connessione.Open();
        using var comando = connessione.CreateCommand();
        comando.CommandText = "DELETE FROM ndvi";
        Assert.Throws<SqliteException>(() => comando.ExecuteNonQuery());
    }

    [Fact]
    public void Date_e_numeri_sporchi_diventano_null()
    {
        Assert.Equal(new DateOnly(2026, 10, 11), LettoreNdvi.Giorno("2026-10-11"));
        Assert.Equal(new DateOnly(2026, 10, 11), LettoreNdvi.Giorno("2026-10-11T10:00:00"));
        Assert.Null(LettoreNdvi.Giorno("11/10/2026"));
        Assert.Null(LettoreNdvi.Giorno(20261011L));
        Assert.Equal(0.5m, LettoreNdvi.Numero(0.5));
        Assert.Equal(1m, LettoreNdvi.Numero(1L));
        Assert.Null(LettoreNdvi.Numero(double.NaN));
        Assert.Null(LettoreNdvi.Numero("0,5"));
        Assert.Null(LettoreNdvi.Numero(DBNull.Value));
    }

    [Fact]
    public void La_tilde_diventa_la_cartella_dell_utente()
    {
        var casa = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.Equal(Path.Combine(casa, "progetti/ndvi.db"), LettoreNdvi.Espandi("~/progetti/ndvi.db"));
        Assert.Equal("/srv/ndvi.db", LettoreNdvi.Espandi("/srv/ndvi.db"));
    }

    // --- Endpoint ---

    private static NdviAzienda Esempio() => new(true,
        [new PassaggioNdvi(new DateOnly(2026, 10, 11), 5m, true), new PassaggioNdvi(new DateOnly(2025, 10, 10), 12m, true)],
        [
            new PuntoNdvi(1, new DateOnly(2024, 9, 1), 0.5m, 0m),
            new PuntoNdvi(1, new DateOnly(2025, 10, 10), 0.55m, 0m),
            new PuntoNdvi(1, new DateOnly(2026, 9, 21), 0.6m, 1m),
            new PuntoNdvi(1, new DateOnly(2026, 10, 11), 0.62m, 4.7m),
            new PuntoNdvi(2, new DateOnly(2025, 10, 10), 0.41m, 0m),
        ]);

    [Fact]
    public void L_endpoint_da_ultimo_passaggio_e_serie_dell_anno_e_del_precedente_per_ogni_vigneto()
    {
        var dto = NdviController.Componi([2, 1, 9], Esempio(), 2026);

        Assert.True(dto.Calcolato);
        Assert.Equal(2026, dto.Anno);
        Assert.Equal([1, 2, 9], dto.Vigneti.Select(v => v.IdVigneto));
        var uno = dto.Vigneti[0];
        Assert.Equal((new DateOnly(2026, 10, 11), 0.62m, 4.7m), (uno.Data!.Value, uno.Media!.Value, uno.NuvolePct!.Value));
        Assert.Equal([0.6m, 0.62m], uno.Serie.Select(p => p.Media));
        Assert.Equal([new DateOnly(2025, 10, 10)], uno.SeriePrecedente.Select(p => p.Data));
        // Il vigneto 2 non ha valori quest'anno: l'ultimo è dell'anno scorso. Il 9 non ha nulla.
        Assert.Equal(new DateOnly(2025, 10, 10), dto.Vigneti[1].Data);
        Assert.Empty(dto.Vigneti[1].Serie);
        Assert.Null(dto.Vigneti[2].Data);
        Assert.Empty(dto.Vigneti[2].SeriePrecedente);
    }

    [Fact]
    public void Senza_database_l_endpoint_risponde_non_calcolato_senza_errore()
    {
        var dto = NdviController.Componi([1], NdviAzienda.NonCalcolato, 2026);
        var passaggi = NdviController.Passaggi(NdviAzienda.NonCalcolato);

        Assert.True(dto.Pronto);
        Assert.False(dto.Calcolato);
        Assert.Null(dto.Vigneti.Single().Media);
        Assert.False(passaggi.Calcolato);
        Assert.Empty(passaggi.Passaggi);
    }

    // --- Tematismo ---

    private static readonly Tematismo Tema = TematismiNdvi.Crea();

    [Theory]
    [InlineData("0.19", 0)]
    [InlineData("0.2", 1)]
    [InlineData("0.349", 1)]
    [InlineData("0.35", 2)]
    [InlineData("0.5", 3)]
    [InlineData("0.65", 4)]
    [InlineData("0.9", 4)]
    [InlineData("-0.1", 0)]
    public void Cinque_classi_fisse_dal_beige_al_verde_scuro(string media, int classe)
    {
        var riga = new RigaNdvi(1, 2026, new DateOnly(2026, 10, 11), decimal.Parse(media, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(classe, Tema.Classifica(riga));
    }

    [Fact]
    public void Senza_passaggio_pulito_il_vigneto_e_grigio_con_la_sua_voce()
    {
        var riga = new RigaNdvi(1, 2026, null, null);

        Assert.Equal(TematismiNdvi.ClasseNessunPassaggio, Tema.Classifica(riga));
        Assert.Equal("Nessun passaggio pulito", Tema.Classi[TematismiNdvi.ClasseNessunPassaggio].Etichetta);
        Assert.True(Tema.Classi[TematismiNdvi.ClasseNessunPassaggio].Speciale);
        Assert.Null(Tema.ValoreConfronto(riga));
        Assert.Equal(["< 0,2", "0,2 – 0,35", "0,35 – 0,5", "0,5 – 0,65", "≥ 0,65"], Tema.Classi.Take(5).Select(c => c.Etichetta));
        Assert.Equal("Satellite", Tema.Gruppo);
        Assert.True(Tema.Confrontabile);
    }

    [Fact]
    public void Il_valore_si_legge_con_la_data_del_passaggio()
    {
        Assert.Equal("0,62 (11/10)", Tema.TestoValore(new RigaNdvi(1, 2026, new DateOnly(2026, 10, 11), 0.62m)));
        Assert.Equal("Nessun passaggio pulito", Tema.TestoValore(new RigaNdvi(1, 2026, null, null)));
    }

    private static NdviAziendaDto Dto() => NdviController.Componi([1, 2], Esempio(), 2026);

    [Fact]
    public void Le_righe_prendono_l_ultimo_passaggio_pulito_e_lo_stesso_giorno_dell_anno_prima()
    {
        var righe = TematismiNdvi.Righe(Dto(), new DateOnly(2026, 10, 11));

        Assert.Equal(new RigaNdvi(1, 2026, new DateOnly(2026, 10, 11), 0.62m), righe.Single(r => r.IdVigneto == 1 && r.Anno == 2026));
        Assert.Equal(new RigaNdvi(2, 2026, null, null), righe.Single(r => r.IdVigneto == 2 && r.Anno == 2026));
        Assert.Equal(new RigaNdvi(2, 2025, new DateOnly(2025, 10, 10), 0.41m), righe.Single(r => r.IdVigneto == 2 && r.Anno == 2025));
    }

    [Fact]
    public void Tornando_indietro_si_vede_il_passaggio_scelto_e_un_valore_troppo_vecchio_non_vale()
    {
        var indietro = TematismiNdvi.Righe(Dto(), new DateOnly(2026, 10, 1));
        Assert.Equal(0.6m, indietro.Single(r => r.IdVigneto == 1 && r.Anno == 2026).Media);
        // Lo stesso giorno del 2025 viene prima del passaggio del 10/10/2025.
        Assert.Null(indietro.Single(r => r.IdVigneto == 1 && r.Anno == 2025).Media);

        var tardi = TematismiNdvi.Righe(Dto(), new DateOnly(2026, 11, 20));
        Assert.Null(tardi.Single(r => r.IdVigneto == 1 && r.Anno == 2026).Media); // 40 giorni dopo l'ultimo
    }

    [Fact]
    public void Un_anno_senza_nessun_punto_non_ha_righe()
    {
        var dto = new NdviAziendaDto(true, true, 2026,
            [new NdviVignetoDto(1, null, null, null, [new PuntoNdviDto(new DateOnly(2026, 5, 1), 0.4m)], [])]);

        Assert.Equal([2026], TematismiNdvi.Righe(dto, new DateOnly(2026, 5, 1)).Select(r => r.Anno).Distinct());
    }

    [Fact]
    public void Il_29_febbraio_nell_anno_prima_diventa_il_28()
    {
        Assert.Equal(new DateOnly(2027, 2, 28), TematismiNdvi.StessoGiorno(new DateOnly(2028, 2, 29), 2027));
    }

    [Fact]
    public void Il_selettore_offre_gli_ultimi_otto_passaggi_puliti()
    {
        var passaggi = Enumerable.Range(1, 12)
            .Select(g => new PassaggioNdviDto(new DateOnly(2026, 9, g), g, g % 4 != 0))
            .ToList();

        var ultimi = TematismiNdvi.UltimiPassaggi(passaggi);

        Assert.Equal(8, ultimi.Count);
        Assert.Equal(new DateOnly(2026, 9, 11), ultimi[0].Data);
        Assert.All(ultimi, p => Assert.True(p.Valido));
        Assert.Equal("11/09 · nuvole 11 %", TematismiNdvi.TestoOpzione(ultimi[0], 2026));
        Assert.Equal(new DateOnly(2026, 9, 11), TematismiNdvi.PassaggioPredefinito(passaggi, Dto(), new DateOnly(2026, 10, 13)));
    }

    [Fact]
    public void La_legenda_dice_il_passaggio_e_le_nuvole()
    {
        var righe = TematismiNdvi.Righe(Dto(), new DateOnly(2026, 10, 11));
        var passaggi = NdviController.Passaggi(Esempio()).Passaggi;

        Assert.Equal("Passaggio Sentinel-2 del 11/10, nuvole 5 %", TematismiNdvi.TestoPassaggio(righe, 2026, passaggi, 2026));
        Assert.Equal("Passaggio Sentinel-2 del 10/10/2025, nuvole 12 %", TematismiNdvi.TestoPassaggio(righe, 2025, passaggi, 2026));
        Assert.Null(TematismiNdvi.TestoPassaggio([], 2026, passaggi, 2026));
    }

    [Fact]
    public void Nel_confronto_fra_anni_un_ndvi_piu_alto_e_un_miglioramento()
    {
        var regola = ConfrontoBigot.Predefinite[TematismiNdvi.Id];
        var righe = TematismiNdvi.Righe(Dto(), new DateOnly(2026, 10, 11));

        var (variazioni, conRiga) = ConfrontoBigot.Variazioni(Tema, regola, righe, 2025, 2026);

        Assert.Equal(VersoBuono.Alto, regola.Verso);
        Assert.Equal(ConfrontoBigot.ClasseMeglio, variazioni[1].Classe); // 0,55 → 0,62
        Assert.False(variazioni.ContainsKey(2)); // senza valore nel 2026
        Assert.Contains(2, conRiga);
    }

    [Fact]
    public void Il_catalogo_ha_il_tematismo_ndvi_e_non_fra_le_statistiche()
    {
        var catalogo = new CatalogoTematismi([.. TematismiBigot.Tutti, Tema]);

        Assert.Same(Tema, catalogo.Trova("ndvi"));
        Assert.DoesNotContain(Tema, catalogo.Statistiche);
    }

    // --- Grafico della scheda ---

    [Fact]
    public void Il_grafico_va_da_gennaio_a_dicembre_e_da_0_a_1()
    {
        Assert.Equal(GraficoNdvi.MargineSinistro, GraficoNdvi.X(new DateOnly(2026, 1, 1)), 3);
        Assert.Equal(GraficoNdvi.Larghezza - GraficoNdvi.MargineDestro, GraficoNdvi.X(new DateOnly(2026, 12, 31)), 3);
        Assert.Equal(GraficoNdvi.Larghezza - GraficoNdvi.MargineDestro, GraficoNdvi.X(new DateOnly(2028, 12, 31)), 3);
        Assert.Equal(GraficoNdvi.Altezza - GraficoNdvi.MargineBasso, GraficoNdvi.Y(-0.2m), 3);
        Assert.Equal(GraficoNdvi.MargineAlto, GraficoNdvi.Y(1.4m), 3);

        var punti = GraficoNdvi.Punti([new PuntoNdviDto(new DateOnly(2026, 12, 31), 1m), new PuntoNdviDto(new DateOnly(2026, 1, 1), 0m)]);
        Assert.Equal("22,94 294,6", GraficoNdvi.Linea(punti));
    }

    // --- Esportazione ---

    [Fact]
    public void L_esportazione_ha_le_colonne_ndvi_e_ndvi_data()
    {
        var ndvi = EsportazioneVigneti.Campi.Single(c => c.Nome == "ndvi");
        var data = EsportazioneVigneti.Campi.Single(c => c.Nome == "ndvi_data");
        var vigneto = new VignetoEsportato(new Vigneto { Id = 1, Nome = "Uno" }, "Azienda", null, null,
            Ndvi: new PuntoNdvi(1, new DateOnly(2026, 10, 11), 0.62m, 4.7m));

        Assert.Equal(TipoCampo.Decimale, ndvi.Tipo);
        Assert.Equal(0.62m, ndvi.Valore(vigneto));
        Assert.Equal("2026-10-11", data.Valore(vigneto));
        Assert.Null(ndvi.Valore(vigneto with { Ndvi = null }));
    }
}

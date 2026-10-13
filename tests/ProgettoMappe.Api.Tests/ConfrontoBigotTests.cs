using ProgettoMappe.Web.Tematismi;
using Xunit;
using IndiceWeb = ProgettoMappe.Contracts.IndiceBigotDto;

namespace ProgettoMappe.Api.Tests;

public class ConfrontoBigotTests
{
    private static IndiceWeb Riga(int anno, int id, int? numParametri = 9, int? punteggio = 80, string? vigoria = null,
        decimal? produzione = null, int? eta = null) =>
        new(anno, id, numParametri, punteggio, 223, vigoria, null, produzione, null, eta, null, null, null, null);

    private static Tematismo Tema(string id) => TematismiBigot.Trova(id)!;

    [Fact]
    public void Il_punteggio_si_colora_per_differenza_fra_i_due_anni()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, punteggio: 70), Riga(2025, 1, punteggio: 60),
            Riga(2024, 2, punteggio: 70), Riga(2025, 2, punteggio: 70),
            Riga(2024, 3, punteggio: 70), Riga(2025, 3, punteggio: 85),
        ];

        var confronto = ConfrontoBigot.Classifica(Tema("punteggio"), righe, 2024, 2025);

        Assert.Equal(ConfrontoBigot.ClasseCalo, confronto.ClasseDi(1));
        Assert.Equal(ConfrontoBigot.ClasseUguale, confronto.ClasseDi(2));
        Assert.Equal(ConfrontoBigot.ClasseAumento, confronto.ClasseDi(3));
        Assert.Equal(["Peggiorato", "Uguale", "Migliorato"], ConfrontoBigot.Classi(Tema("punteggio")).Select(c => c.Etichetta));
    }

    [Fact]
    public void Senza_dato_in_un_anno_o_incompleto_non_si_confronta()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, punteggio: 70), // 2025 assente
            Riga(2024, 2, numParametri: 8, punteggio: 70), Riga(2025, 2, punteggio: 75), // incompleto nel 2024
            Riga(2024, 3, punteggio: null), Riga(2025, 3, punteggio: 75),
        ];

        var confronto = ConfrontoBigot.Classifica(Tema("punteggio"), righe, 2024, 2025);

        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(1));
        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(3));
        Assert.Equal(TematismiBigot.ClasseFuoriPerimetro, confronto.ClasseDi(99)); // nessuna riga nei due anni
    }

    [Fact]
    public void Le_classi_a_parole_si_confrontano_sulla_posizione_nella_scala()
    {
        IndiceWeb[] righe =
        [
            Riga(2024, 1, vigoria: "BASSA"), Riga(2025, 1, vigoria: "MEDIA"),
            Riga(2024, 2, vigoria: "MEDIA"), Riga(2025, 2, vigoria: "media"),
            Riga(2024, 3, vigoria: "BASSA"), Riga(2025, 3, vigoria: "SCONOSCIUTA"), // "Altro": non confrontabile
        ];

        var confronto = ConfrontoBigot.Classifica(Tema("vigoria"), righe, 2024, 2025);

        Assert.Equal(ConfrontoBigot.ClasseAumento, confronto.ClasseDi(1));
        Assert.Equal(ConfrontoBigot.ClasseUguale, confronto.ClasseDi(2));
        Assert.Equal(TematismiBigot.ClasseNessunDato, confronto.ClasseDi(3));
    }

    [Fact]
    public void Senza_verso_noto_niente_rosso_e_verde()
    {
        var classi = ConfrontoBigot.Classi(Tema("vigoria"));

        Assert.Equal(["Verso «Molto bassa»", "Uguale", "Verso «Eccessiva»"], classi.Select(c => c.Etichetta));
        Assert.DoesNotContain(classi, c => c.Colore is "#d73027" or "#1a9850");
        Assert.Equal(["Diminuito", "Uguale", "Aumentato"], ConfrontoBigot.Classi(Tema("produzione")).Select(c => c.Etichetta));
        Assert.Equal("Migliorato", ConfrontoBigot.Classi(Tema("ampelopatie"))[ConfrontoBigot.ClasseAumento].Etichetta);
    }

    [Fact]
    public void Differenze_sotto_il_centesimo_contano_come_uguali()
    {
        IndiceWeb[] righe = [Riga(2024, 1, produzione: 1.501m), Riga(2025, 1, produzione: 1.499m)];

        Assert.Equal(ConfrontoBigot.ClasseUguale, ConfrontoBigot.Classifica(Tema("produzione"), righe, 2024, 2025).ClasseDi(1));
    }

    [Fact]
    public void La_scheda_mostra_i_due_valori()
    {
        IndiceWeb[] righe = [Riga(2024, 1, produzione: 3.2m), Riga(2025, 1, produzione: 2.8m), Riga(2025, 2, vigoria: "BASSA")];

        Assert.Equal("2024 → 2025: 3,2 kg/ceppo → 2,8 kg/ceppo", ConfrontoBigot.RigaScheda(Tema("produzione"), righe, 1, 2024, 2025));
        Assert.Equal("2024 → 2025: – → Bassa", ConfrontoBigot.RigaScheda(Tema("vigoria"), righe, 2, 2024, 2025));
        Assert.Equal("2024 → 2025: 80 → 80", ConfrontoBigot.RigaScheda(Tema("punteggio"), righe, 1, 2024, 2025));
    }

    [Fact]
    public void Eta_anomala_non_si_confronta_e_si_dice_nella_scheda()
    {
        IndiceWeb[] righe = [Riga(2024, 1, eta: 1998), Riga(2025, 1, eta: 30)];

        Assert.Equal(TematismiBigot.ClasseNessunDato, ConfrontoBigot.Classifica(Tema("eta"), righe, 2024, 2025).ClasseDi(1));
        Assert.Equal("2024 → 2025: 1998 (dato anomalo) → 30 anni", ConfrontoBigot.RigaScheda(Tema("eta"), righe, 1, 2024, 2025));
    }

    [Theory]
    [InlineData(2025, 2024)] // il precedente disponibile
    [InlineData(2024, 2021)] // salta gli anni che mancano
    [InlineData(2019, 2021)] // il primo: il successivo
    public void L_anno_Dal_proposto_e_il_precedente_disponibile(int annoA, int atteso)
    {
        Assert.Equal(atteso, ConfrontoBigot.AnnoDaPredefinito([2021, 2019, 2024, 2025], annoA));
    }

    [Fact]
    public void I_tasti_del_cursore_passano_all_anno_disponibile_vicino()
    {
        int[] anni = [2026, 2025, 2023, 2019];

        Assert.Equal(2023, ConfrontoBigot.AnnoVicino(anni, 2025, -1));
        Assert.Equal(2025, ConfrontoBigot.AnnoVicino(anni, 2023, +1));
        Assert.Null(ConfrontoBigot.AnnoVicino(anni, 2019, -1));
        Assert.Null(ConfrontoBigot.AnnoVicino(anni, 2026, +1));
    }
}

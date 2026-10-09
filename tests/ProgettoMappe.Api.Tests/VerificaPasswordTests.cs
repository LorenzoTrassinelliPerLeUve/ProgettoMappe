using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ProgettoMappe.Api.Contracts;
using ProgettoMappe.Api.Controllers;
using ProgettoMappe.Infrastructure.Repositories;
using Xunit;

namespace ProgettoMappe.Api.Tests;

public class VerificaPasswordTests
{
    // SHA-256 di "test" in UTF-8 (valore di riferimento noto).
    private const string HashTest = "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08";

    private static CredenzialiUtente Utente(int id, string sha, string nome = "Mario", string cognome = "Rossi")
        => new(id, nome, cognome, sha);

    [Fact]
    public void Password_giusta_corrisponde_con_hash_maiuscolo_o_minuscolo()
    {
        Assert.True(VerificaPassword.Corrisponde("test", HashTest));
        Assert.True(VerificaPassword.Corrisponde("test", HashTest.ToLowerInvariant()));
        Assert.True(VerificaPassword.Corrisponde("test", " " + HashTest + " "));
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("test ")]
    [InlineData("")]
    public void Password_diversa_non_corrisponde(string password)
    {
        Assert.False(VerificaPassword.Corrisponde(password, HashTest));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData(HashTest + "0")]
    [InlineData(HashTest + HashTest)]
    [InlineData("ZZ86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08")]
    public void Hash_con_formato_anomalo_non_corrisponde_mai(string? sha)
    {
        Assert.False(VerificaPassword.Corrisponde("test", sha));
    }

    [Fact]
    public void Con_LoginName_ripetuto_entra_l_unica_riga_con_la_password_giusta()
    {
        var altroHash = new string('A', 64);
        var scelto = VerificaPassword.Scegli([Utente(1, altroHash), Utente(2, HashTest)], "test");
        Assert.Equal(2, scelto?.Id);
    }

    [Fact]
    public void Con_la_stessa_password_su_piu_righe_l_accesso_e_negato()
    {
        Assert.Null(VerificaPassword.Scegli([Utente(1, HashTest), Utente(2, HashTest.ToLowerInvariant())], "test"));
    }

    [Fact]
    public void Senza_righe_l_accesso_e_negato()
    {
        Assert.Null(VerificaPassword.Scegli([], "test"));
    }

    [Theory]
    [InlineData(" Mario ", " Rossi ", "Mario Rossi")]
    [InlineData("Mario", "", "Mario")]
    [InlineData("", "", "mario@esempio.it")]
    public void Nome_visualizzato_usa_nome_e_cognome_o_il_login(string nome, string cognome, string atteso)
    {
        Assert.Equal(atteso, VerificaPassword.NomeVisualizzato(Utente(1, HashTest, nome, cognome), "mario@esempio.it"));
    }

    private sealed class UtentiFinti(params CredenzialiUtente[] righe) : IUtentiRepository
    {
        public string? UltimoLogin { get; private set; }

        public Task<IReadOnlyList<CredenzialiUtente>> GetCredenzialiAsync(string loginName, CancellationToken ct = default)
        {
            UltimoLogin = loginName;
            return Task.FromResult<IReadOnlyList<CredenzialiUtente>>(righe);
        }
    }

    [Fact]
    public async Task Endpoint_restituisce_l_utente_senza_hash_se_la_password_e_giusta()
    {
        var utenti = new UtentiFinti(Utente(7, HashTest));
        var controller = new AccessoController(utenti, NullLogger<AccessoController>.Instance);

        var risultato = await controller.Verifica(new RichiestaAccessoDto("  mario@esempio.it ", "test"), default);

        var ok = Assert.IsType<OkObjectResult>(risultato.Result);
        Assert.Equal(new UtenteAccessoDto(7, "Mario Rossi"), ok.Value);
        Assert.Equal("mario@esempio.it", utenti.UltimoLogin);
    }

    [Theory]
    [InlineData("mario@esempio.it", "sbagliata")]
    [InlineData("", "test")]
    [InlineData(null, "test")]
    [InlineData("mario@esempio.it", null)]
    public async Task Endpoint_risponde_401_con_credenziali_errate_o_mancanti(string? login, string? password)
    {
        var controller = new AccessoController(new UtentiFinti(Utente(7, HashTest)), NullLogger<AccessoController>.Instance);

        var risultato = await controller.Verifica(new RichiestaAccessoDto(login, password), default);

        Assert.IsType<UnauthorizedResult>(risultato.Result);
    }
}

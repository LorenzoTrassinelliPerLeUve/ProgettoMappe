using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Infrastructure.BilancioIdrico;
using ProgettoMappe.Api.AccessoUnico;
using ProgettoMappe.Api.Avvisi;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.IndiceBigot;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Infrastructure.Repositories.Filari;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;
using ProgettoMappe.Infrastructure.Repositories.ReportFineStagione;

var builder = WebApplication.CreateBuilder(args);

const string webAppCorsPolicy = "WebApp";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4Grapes è un sistema esistente e va trattato in sola lettura (nessuna migration,
// EnsureCreated o seed). Quando è configurata una connection string si usano i repository
// reali (FourGrapesAziendeRepository/FourGrapesVignetiRepository, query dirette su Ente/
// Vigneto — vedi i commenti in quelle classi per il mapping confermato); altrimenti si
// resta sui repository con dati di esempio (Repositories/InMemory), utili per sviluppare e
// testare senza un database reale a disposizione.
var fourGrapesConnectionString = builder.Configuration.GetConnectionString("FourGrapes");
if (!string.IsNullOrWhiteSpace(fourGrapesConnectionString))
{
    builder.Services.AddDbContext<FourGrapesDbContext>(options =>
        options.UseSqlServer(fourGrapesConnectionString));

    builder.Services.AddScoped<IAziendeRepository, FourGrapesAziendeRepository>();
    builder.Services.AddScoped<IVignetiRepository, FourGrapesVignetiRepository>();
    // Poligoni oltre QualitaGeometrie:SogliaEttari: fuori dalla mappa, segnalati (vedi QualitaGeometrie).
    builder.Services.Configure<QualitaGeometrieOptions>(builder.Configuration.GetSection(QualitaGeometrieOptions.Sezione));
    builder.Services.AddScoped<IGruppiRepository, FourGrapesGruppiRepository>();
    builder.Services.AddScoped<IUtentiRepository, FourGrapesUtentiRepository>();
    builder.Services.AddScoped<IPermessiRepository, FourGrapesPermessiRepository>();
    builder.Services.AddScoped<ICategorieUtentiRepository, FourGrapesCategorieUtentiRepository>();
    builder.Services.AddScoped<IAccessoUnicoRepository, FourGrapesAccessoUnicoRepository>();
    builder.Services.AddScoped<ILetturePortale, FourGrapesLetturePortale>();
    builder.Services.AddScoped<ILettureDifesaPortale, FourGrapesLettureDifesaPortale>();
    builder.Services.AddScoped<ILettureAmministrazionePortale, FourGrapesLettureAmministrazionePortale>();
    builder.Services.AddScoped<ILettureNutrizionePortale, FourGrapesLettureNutrizionePortale>();
    builder.Services.AddScoped<ILettureVignetiPortale, FourGrapesLettureVignetiPortale>();
    builder.Services.AddScoped<ILettureRilieviApp, FourGrapesLettureRilieviApp>();
    builder.Services.AddScoped<ILettureConcimazioniPortale, FourGrapesLettureConcimazioniPortale>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureIndicatori, ProgettoMappe.Api.Portale.FourGrapesLettureIndicatori>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureTrattamenti, ProgettoMappe.Api.Portale.FourGrapesLettureTrattamenti>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureScrittureTrattamenti, ProgettoMappe.Api.Portale.FourGrapesLettureScrittureTrattamenti>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureBollettini, ProgettoMappe.Api.Portale.FourGrapesLettureBollettini>();

    // VistaIndiceBIGOT impiega ~40 s per qualsiasi query: copia in memoria caricata in
    // background all'avvio e riletta periodicamente (vedi CacheIndiciBigot).
    builder.Services.AddSingleton<CacheIndiciBigot>();
    builder.Services.AddHostedService<AggiornamentoCopia<CacheIndiciBigot>>();
    builder.Services.AddScoped<IIndiciBigotRepository, FourGrapesIndiciBigotRepository>();

    // Rischi, vigoria e potenziale idrico: tre viste veloci (pochi secondi), stesso meccanismo.
    builder.Services.AddSingleton<CacheStatisticheVigneti>();
    builder.Services.AddHostedService<AggiornamentoCopia<CacheStatisticheVigneti>>();
    builder.Services.AddScoped<IStatisticheVignetiRepository, FourGrapesStatisticheVignetiRepository>();

    // Fenologia: rilievi BBCH (~200.000 righe, pochi secondi), riassunti per vigneto e anno.
    builder.Services.AddSingleton<CacheFenologia>();
    builder.Services.AddHostedService<AggiornamentoCopia<CacheFenologia>>();
    builder.Services.AddScoped<IFenologiaRepository, FourGrapesFenologiaRepository>();

    // Ultimo rilievo per vigneto: Rilievo è troppo grande per una copia, si legge per azienda
    // (in memoria 10 minuti, vedi CacheVignetiAziende).
    builder.Services.AddScoped<IRilieviRepository, FourGrapesRilieviRepository>();

    // Sottovigneti (V_SubVigneto): per azienda come i vigneti, in memoria un minuto.
    builder.Services.AddScoped<ISottovignetiRepository, FourGrapesSottovignetiRepository>();

    // Meteo e probabilità di infezione: tutte le aziende in una lettura, in memoria 30 minuti.
    builder.Services.AddScoped<IMeteoRepository, FourGrapesMeteoRepository>();

    // Ultimo trattamento e linea di difesa: tabelle senza indici per vigneto, tutte le aziende in una
    // lettura, in memoria 10 minuti.
    builder.Services.AddScoped<IDifesaRepository, FourGrapesDifesaRepository>();

    // Stazioni meteo, dispositivi e trappole: poche righe, tutte le aziende in una lettura, in memoria
    // 10 minuti.
    builder.Services.AddScoped<IDispositiviRepository, FourGrapesDispositiviRepository>();

    // Indice Bigot (task #189): le viste per parametro lette di notte in uno snapshot SQLite.
    builder.Services.AddSingleton<IFonteIndiceBigot, FourGrapesFonteIndiceBigot>();

    // Report fine stagione (task #201): meteo per vigneto dell'azienda, letto a richiesta.
    builder.Services.AddScoped<IFonteMeteoReport, FourGrapesFonteMeteoReport>();

    // Report settimanale (task #213): rilievi della settimana quando lo snapshot non c'è.
    builder.Services.AddScoped<IFonteRilieviSettimana, FourGrapesFonteRilieviSettimana>();

    // Report analisi del suolo (task #317): rilievi 144 con analisi di laboratorio, letti a richiesta.
    builder.Services.AddScoped<ProgettoMappe.Api.AnalisiSuolo.ILettureAnalisiSuolo, ProgettoMappe.Api.AnalisiSuolo.FourGrapesLettureAnalisiSuolo>();
}
else
{
    builder.Services.AddScoped<IAziendeRepository, InMemoryAziendeRepository>();
    builder.Services.AddScoped<IVignetiRepository, InMemoryVignetiRepository>();
    builder.Services.AddScoped<IGruppiRepository, InMemoryGruppiRepository>();
    builder.Services.AddScoped<IUtentiRepository, InMemoryUtentiRepository>();
    // Foto e prove UX del Portale sui dati di esempio (task #387): un Id con tutti i permessi, mai con 4Grapes.
    var utenteDiProva = builder.Configuration.GetValue<int?>("Portale:UtenteDiProva");
    builder.Services.AddScoped<IPermessiRepository>(_ => new InMemoryPermessiRepository(utenteDiProva));
    builder.Services.AddScoped<ICategorieUtentiRepository>(_ => new InMemoryCategorieUtentiRepository(utenteDiProva));
    builder.Services.AddScoped<IAccessoUnicoRepository, InMemoryAccessoUnicoRepository>();
    builder.Services.AddScoped<ILetturePortale, InMemoryLetturePortale>();
    builder.Services.AddScoped<ILettureDifesaPortale, InMemoryLettureDifesaPortale>();
    builder.Services.AddScoped<ILettureAmministrazionePortale, InMemoryLettureAmministrazionePortale>();
    builder.Services.AddScoped<ILettureNutrizionePortale, InMemoryLettureNutrizionePortale>();
    builder.Services.AddScoped<ILettureVignetiPortale, InMemoryLettureVignetiPortale>();
    builder.Services.AddScoped<ILettureRilieviApp, InMemoryLettureRilieviApp>();
    builder.Services.AddScoped<ILettureConcimazioniPortale, InMemoryLettureConcimazioniPortale>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureIndicatori, ProgettoMappe.Api.Portale.InMemoryLettureIndicatori>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureTrattamenti, ProgettoMappe.Api.Portale.InMemoryLettureTrattamenti>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureScrittureTrattamenti, ProgettoMappe.Api.Portale.InMemoryLettureScrittureTrattamenti>();
    builder.Services.AddScoped<ProgettoMappe.Api.Portale.ILettureBollettini, ProgettoMappe.Api.Portale.InMemoryLettureBollettini>();
    builder.Services.AddScoped<IIndiciBigotRepository, InMemoryIndiciBigotRepository>();
    builder.Services.AddScoped<IStatisticheVignetiRepository, InMemoryStatisticheVignetiRepository>();
    builder.Services.AddScoped<IFenologiaRepository, InMemoryFenologiaRepository>();
    builder.Services.AddScoped<IRilieviRepository, InMemoryRilieviRepository>();
    builder.Services.AddScoped<ISottovignetiRepository, InMemorySottovignetiRepository>();
    builder.Services.AddScoped<IMeteoRepository, InMemoryMeteoRepository>();
    builder.Services.AddScoped<IDifesaRepository, InMemoryDifesaRepository>();
    builder.Services.AddScoped<IDispositiviRepository, InMemoryDispositiviRepository>();
    builder.Services.AddSingleton<IFonteIndiceBigot, InMemoryFonteIndiceBigot>();
    builder.Services.AddSingleton<IFonteMeteoReport, InMemoryFonteMeteoReport>();
    builder.Services.AddSingleton<IFonteRilieviSettimana, InMemoryFonteRilieviSettimana>();
    builder.Services.AddSingleton<ProgettoMappe.Api.AnalisiSuolo.ILettureAnalisiSuolo, ProgettoMappe.Api.AnalisiSuolo.InMemoryLettureAnalisiSuolo>();
}

// Report «Indice Bigot» (task #189): snapshot notturno (IndiceBigot:Database), la pagina legge solo quello.
builder.Services.Configure<IndiceBigotOptions>(builder.Configuration.GetSection(IndiceBigotOptions.Sezione));
builder.Services.AddSingleton<SnapshotIndiceBigot>();
builder.Services.AddSingleton<LetturaSnapshotIndice>();
builder.Services.AddHostedService<AggiornamentoSnapshotIndice>();

// Report fine stagione (task #201): snapshot del dba (ReportFineStagione:Snapshot), letto soltanto.
builder.Services.Configure<ReportFineStagioneOptions>(builder.Configuration.GetSection(ReportFineStagioneOptions.Sezione));
builder.Services.AddSingleton<SnapshotReportFineStagione>();

// Solo le aziende dei gruppi in Perimetro:Gruppi sono visibili, in tutti gli endpoint di dati.
builder.Services.Configure<PerimetroOptions>(builder.Configuration.GetSection(PerimetroOptions.Sezione));
builder.Services.AddScoped<PerimetroAziende>();

// NDVI da satellite (task #84): database SQLite del programma notturno ndvi/, in sola lettura,
// letto per azienda e tenuto in memoria 30 minuti (vedi CacheVignetiAziende).
builder.Services.Configure<NdviOptions>(builder.Configuration.GetSection(NdviOptions.Sezione));
builder.Services.AddSingleton<LettoreNdvi>();

// Filari disegnati (task #95): database SQLite di Mappe (Disegni:Database), l'unico che l'Api scrive.
builder.Services.Configure<DisegniOptions>(builder.Configuration.GetSection(DisegniOptions.Sezione));
builder.Services.AddSingleton<ArchivioFilari>();

// DSS Bilancio Idrico (task #333): configurazione in un SQLite di Mappe (BilancioIdrico:Database), dati di campo da
// 4Grapes in sola lettura; il calcolo c'è solo con la connessione a 4Grapes.
builder.Services.Configure<BilancioIdricoOptions>(builder.Configuration.GetSection(BilancioIdricoOptions.Sezione));
builder.Services.AddSingleton<ArchivioConfigurazioneBilancio>();
// Fase 3 (task #335): risultati nello stesso file, ricalcolo di tutto il perimetro ogni notte (BilancioIdrico:OraNotturna).
builder.Services.AddSingleton<ArchivioRisultatiBilancio>();
builder.Services.AddSingleton<IPotenzialiMisurati, PotenzialiMisuratiSnapshot>();
builder.Services.Configure<RicalcoloBilancioOptions>(builder.Configuration.GetSection(BilancioIdricoOptions.Sezione));
if (!string.IsNullOrWhiteSpace(fourGrapesConnectionString))
{
    builder.Services.AddSingleton<IFonteBilancio>(new FourGrapesFonteBilancio(fourGrapesConnectionString));
    // Task #349: preset dei parametri da BilancioIdrico:Preset (ComeMatteo, predefinito, o ComeOggi).
    builder.Services.AddSingleton(sp => new CalcoloBilancioAzienda(sp.GetRequiredService<IFonteBilancio>(),
        sp.GetRequiredService<ArchivioConfigurazioneBilancio>(),
        ProgettoMappe.BilancioIdrico.Parametri.Preset(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BilancioIdricoOptions>>().Value.Preset)));
    builder.Services.AddSingleton<RicalcoloBilancioIdrico>();
    builder.Services.AddHostedService<AggiornamentoBilancioIdrico>();
}

// Vigneti per azienda in memoria per un minuto: elenco/GeoJSON e indici BIGOT fanno una query sola.
builder.Services.AddMemoryCache();

// Portale (task #225): scritture su 4Grapes solo con ConnectionStrings:FourGrapesScrittura e Portale:Scritture:Modo
// = prova (tutto annullato alla fine) o vere. Vuota = spente. Vedi docs/portale/LEGGIMI.md.
Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton(builder.Services, TimeProvider.System);
builder.Services.AddSingleton(sp => new ProgettoMappe.Infrastructure.Scrittura.ScritturaFourGrapes(
    builder.Configuration.GetConnectionString("FourGrapesScrittura"),
    ProgettoMappe.Infrastructure.Scrittura.ScritturaFourGrapes.LeggiModo(builder.Configuration["Portale:Scritture:Modo"]),
    sp.GetService<TimeProvider>() ?? TimeProvider.System));
builder.Services.AddSingleton<CacheVignetiAziende>();

// Posta Perleuve (bollettini, task #280): Graph con Posta:FileSegreti, Posta:Mittente, Posta:Modo (prova se non «vere»).
builder.Services.Configure<ProgettoMappe.Api.Posta.PostaOptions>(builder.Configuration.GetSection("Posta"));
builder.Services.AddHttpClient(ProgettoMappe.Api.Posta.PostaGraph.NomeClient, c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<ProgettoMappe.Api.Posta.IPosta, ProgettoMappe.Api.Posta.PostaGraph>();
builder.Services.Configure<ProgettoMappe.Api.Posta.FirebaseOptions>(builder.Configuration.GetSection("Notifiche:Firebase"));
builder.Services.AddHttpClient(ProgettoMappe.Api.Posta.PushFirebase.NomeClient, c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<ProgettoMappe.Api.Posta.IPush, ProgettoMappe.Api.Posta.PushFirebase>();
builder.Services.AddSingleton<ProgettoMappe.Api.Portale.IModelliBollettino, ProgettoMappe.Api.Portale.ModelliBollettinoIncorporati>();

// Avvisi dalle regole di Avvisi:Regole, ricalcolati ogni ora in background (e scritti in Avvisi:File).
builder.Services.Configure<AvvisiOptions>(builder.Configuration.GetSection(AvvisiOptions.Sezione));
builder.Services.AddSingleton<CalcoloAvvisi>();
builder.Services.AddHostedService<AggiornamentoAvvisi>();

// Accesso unico per le app esterne (token firmati, chiave per app): docs/accesso-unico.md.
builder.Services.Configure<AccessoUnicoOptions>(builder.Configuration.GetSection(AccessoUnicoOptions.Sezione));
builder.Services.AddSingleton<ChiaviFirma>();
builder.Services.AddSingleton<TokenAccesso>();
builder.Services.AddSingleton<LimitiAccessoUnico>();
builder.Services.AddSingleton<SegretiApp>();
builder.Services.AddScoped<ServizioProfilo>();
// «Password dimenticata» (sezione 22, task #289): collegamenti firmati con le chiavi di ASP.NET Core (quelle dell'accesso se OIDC
// è acceso, altrimenti le predefinite: un riavvio può invalidare i collegamenti, che valgono comunque un'ora).
builder.Services.AddDataProtection();
builder.Services.AddSingleton<RecuperoPassword>();
// OpenID Connect (fornitore di identità Perleuve), solo con un AccessoUnico:Database usabile.
var oidcAttivo = OidcPerleuve.Aggiungi(builder);

// Chiave condivisa con il Web (intestazione X-Mappe-Chiave), vedi ControlloChiaveApi.
builder.Services.Configure<ChiaveApiOptions>(builder.Configuration.GetSection(ChiaveApiOptions.Sezione));

builder.Services.AddCors(options =>
{
    options.AddPolicy(webAppCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Le pagine e gli endpoint dell'accesso Perleuve arrivano dal proxy del Web (sulla macchina): schema, host e IP del
// browser dalle intestazioni X-Forwarded-*, accettate solo dal loopback (predefinito), così l'issuer è quello pubblico.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
});
app.UseHttpsRedirection();
app.UseCors(webAppCorsPolicy);
app.UseMiddleware<ControlloChiaveApi>();
if (!oidcAttivo)
{
    // Senza database dell'accesso: niente OpenID Connect né pagine Perleuve, tranne la password dimenticata (non usa OIDC).
    app.Use((contesto, avanti) =>
        contesto.Request.Path.StartsWithSegments("/connect") || contesto.Request.Path.StartsWithSegments("/.well-known")
        || contesto.Request.Path.StartsWithSegments("/perleuve") && !contesto.Request.Path.StartsWithSegments("/perleuve/password-dimenticata")
                                                                 && !contesto.Request.Path.StartsWithSegments("/perleuve/reimposta")
            ? Results.NotFound().ExecuteAsync(contesto)
            : avanti(contesto));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Salute per il contenitore (HEALTHCHECK, Coolify): fuori da /api, quindi senza chiave.
app.MapGet("/salute", () => Results.Text("ok"));
// Modo delle scritture su 4Grapes (spente / prova / vere), senza segreti: per controllare un'anteprima o un rilascio.
app.MapGet("/salute/scritture", (ProgettoMappe.Infrastructure.Scrittura.ScritturaFourGrapes s) =>
    Results.Text("scritture: " + s.Modo.ToString().ToLowerInvariant()));

app.Run();

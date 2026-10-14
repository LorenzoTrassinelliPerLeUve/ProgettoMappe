using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Api.Avvisi;
using ProgettoMappe.Api.Servizi;
using ProgettoMappe.Infrastructure.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories;
using ProgettoMappe.Infrastructure.Repositories.FourGrapes;
using ProgettoMappe.Infrastructure.Repositories.InMemory;
using ProgettoMappe.Infrastructure.Repositories.Filari;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

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
}
else
{
    builder.Services.AddScoped<IAziendeRepository, InMemoryAziendeRepository>();
    builder.Services.AddScoped<IVignetiRepository, InMemoryVignetiRepository>();
    builder.Services.AddScoped<IGruppiRepository, InMemoryGruppiRepository>();
    builder.Services.AddScoped<IUtentiRepository, InMemoryUtentiRepository>();
    builder.Services.AddScoped<IIndiciBigotRepository, InMemoryIndiciBigotRepository>();
    builder.Services.AddScoped<IStatisticheVignetiRepository, InMemoryStatisticheVignetiRepository>();
    builder.Services.AddScoped<IFenologiaRepository, InMemoryFenologiaRepository>();
    builder.Services.AddScoped<IRilieviRepository, InMemoryRilieviRepository>();
    builder.Services.AddScoped<ISottovignetiRepository, InMemorySottovignetiRepository>();
    builder.Services.AddScoped<IMeteoRepository, InMemoryMeteoRepository>();
    builder.Services.AddScoped<IDifesaRepository, InMemoryDifesaRepository>();
    builder.Services.AddScoped<IDispositiviRepository, InMemoryDispositiviRepository>();
}

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

// Vigneti per azienda in memoria per un minuto: elenco/GeoJSON e indici BIGOT fanno una query sola.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CacheVignetiAziende>();

// Avvisi dalle regole di Avvisi:Regole, ricalcolati ogni ora in background (e scritti in Avvisi:File).
builder.Services.Configure<AvvisiOptions>(builder.Configuration.GetSection(AvvisiOptions.Sezione));
builder.Services.AddSingleton<CalcoloAvvisi>();
builder.Services.AddHostedService<AggiornamentoAvvisi>();

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

app.UseHttpsRedirection();
app.UseCors(webAppCorsPolicy);
app.UseMiddleware<ControlloChiaveApi>();
app.UseAuthorization();
app.MapControllers();

app.Run();

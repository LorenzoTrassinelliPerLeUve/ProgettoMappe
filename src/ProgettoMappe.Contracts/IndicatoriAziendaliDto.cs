namespace ProgettoMappe.Contracts;

/// <summary>Una voce di un filtro degli indicatori (regione, provincia, comune: Id testuali di VignetoMaps; azienda: IdEnte).</summary>
public record VoceFiltroDto(string Id, string Nome);

/// <summary>Riga di probabilità d'infezione: testo della classe come il portale («MOLTO BASSO» … «MOLTO ALTO», «-»).</summary>
public record RigaProbabilitaDto(string Id, string Nome, string Peronospora, string Oidio);

/// <summary>Riga della pressione per azienda: somma di monitoraggio + intorno + fenologia (null se manca un pezzo, come il portale).</summary>
public record RigaPressioneDto(int Id, string Nome, decimal? Peronospora, decimal? Oidio);

/// <summary>Protezione media dei vigneti filtrati, giorno per giorno (da oggi a 7 giorni), in % intera come il portale.</summary>
public record GiornoProtezioneDto(DateTime Giorno, int Peronospora, int Oidio);

/// <summary>La pagina «Indicatori aziendali» (sezione 3): filtri possibili, tabelle e grafico.</summary>
public record IndicatoriAziendaliDto(
    DateTime Data,
    IReadOnlyList<VoceFiltroDto> Regioni,
    IReadOnlyList<VoceFiltroDto> Province,
    IReadOnlyList<VoceFiltroDto> Comuni,
    IReadOnlyList<VoceFiltroDto> Aziende,
    IReadOnlyList<RigaProbabilitaDto> ProbabilitaAziende,
    IReadOnlyList<RigaProbabilitaDto> ProbabilitaComuni,
    IReadOnlyList<RigaPressioneDto> Pressione,
    IReadOnlyList<GiornoProtezioneDto> Protezione);

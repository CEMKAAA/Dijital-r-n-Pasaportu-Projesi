namespace MiniDpp.Api.Importing;

// CSV'den gelen ham değerlerdir. Sayıya dönüştürülemeyen alanlar da
// hata raporuna girebilsin diye doğrulamaya kadar metin olarak tutulur.
public sealed record CsvProductRow(
    string? Gtin,
    string? ModelName,
    string? BatchNumber,
    string? CottonPercentage,
    string? ElastanePercentage,
    string? ProductionLocation,
    string? CarbonKgCo2e);

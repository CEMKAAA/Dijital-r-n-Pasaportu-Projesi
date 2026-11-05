namespace MiniDpp.Api.Importing;

// Yalnızca bütün alan kuralları başarıyla geçildiğinde oluşturulur.
public sealed record ValidatedProduct(
    string Gtin,
    string ModelName,
    string BatchNumber,
    decimal CottonPercentage,
    decimal ElastanePercentage,
    string ProductionLocation,
    decimal CarbonKgCo2e);

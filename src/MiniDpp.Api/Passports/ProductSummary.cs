namespace MiniDpp.Api.Passports;

// Liste ekranına yalnızca tüketiciye açık temel bilgiler gönderilir.
public sealed record ProductSummary(
    string Gtin, string ModelName, string BatchNumber, string ProductionLocation);

namespace MiniDpp.Api.Importing;

public sealed record CsvImportReport(
    int TotalRows,
    int ImportedCount,
    int UnchangedCount,
    int RejectedCount,
    IReadOnlyList<CsvImportRowError> Errors,
    IReadOnlyList<ImportedProduct> Products);

public sealed record CsvImportRowError(int RowNumber, IReadOnlyList<string> Errors);

public sealed record ImportedProduct(
    string Gtin, string ModelName, string BatchNumber, string Status);

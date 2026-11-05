namespace MiniDpp.Api.Importing;

public sealed record CsvInputRow(
    int RowNumber,
    CsvProductRow? Product,
    IReadOnlyList<string> Errors);

using CsvHelper;

namespace MiniDpp.Api.Importing;

public static class CsvProductReader
{
    private static readonly string[] RequiredHeaders =
    [
        "gtin", "model_adi", "batch_no", "elyaf_pamuk_yuzde",
        "elyaf_elastan_yuzde", "uretim_yeri", "karbon_kg_co2e"
    ];

    public static IReadOnlyList<CsvInputRow> Read(string text, CancellationToken cancellationToken = default)
    {
        var malformedRecord = false;
        var options = new CsvReaderOptions
        {
            HasHeader = true,
            Separator = ',',
            SkipEmptyLines = false,
            TrackLineNumbers = true,
            AllowLineBreaksInQuotes = true,
            MalformedData = CsvMalformedDataHandling.Recover,
            FieldCountMismatch = CsvFieldCountHandling.Allow,
            IncludeValuesInExceptions = false,
            OnMalformedRecord = _ => malformedRecord = true
        };

        // UTF-8 BOM, alanın parçası değildir. Tırnak ve virgül ayrıştırmasını
        // CsvHelper yapar; metni Split(',') ile bölmek doğru sonuç vermez.
        using var csv = CsvReader.FromText(text.TrimStart('\uFEFF'), options);
        try
        {
            csv.ReadHeader();
        }
        catch (CsvFormatException)
        {
            throw new InvalidCsvException("CSV başlığı okunamadı; tırnakları ve sütunları kontrol edin.");
        }

        var headers = csv.Headers.Select(header => header.Trim()).ToArray();
        if (malformedRecord || headers.Length != RequiredHeaders.Length
            || headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length
            || RequiredHeaders.Except(headers, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new InvalidCsvException(
                $"CSV başlığı tam olarak şu sütunları içermelidir: {string.Join(", ", RequiredHeaders)}.");
        }

        var columns = headers.Select((name, index) => (name, index))
            .ToDictionary(column => column.name, column => column.index, StringComparer.OrdinalIgnoreCase);
        var rows = new List<CsvInputRow>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            malformedRecord = false;

            try
            {
                if (!csv.Read())
                {
                    break;
                }
            }
            catch (CsvFormatException exception)
            {
                rows.Add(new CsvInputRow(checked((int)exception.LineNumber), null,
                    ["CSV satırı okunamadı; tırnakları ve alan sınırlarını kontrol edin."]));
                break;
            }

            // ÖNEMLİ: RowNumber veri kaydı sayısıdır ve başlığı saymaz.
            // LineNumber ise başlığı 1 sayar; boş ve çok satırlı alanları da izler.
            var rowNumber = checked((int)csv.LineNumber);
            if (malformedRecord)
            {
                rows.Add(new CsvInputRow(rowNumber, null,
                    ["CSV satırında hatalı tırnak kullanımı var."]));
                continue;
            }

            if (csv.FieldCount != headers.Length)
            {
                rows.Add(new CsvInputRow(rowNumber, null,
                    [$"CSV satırında {headers.Length} sütun bekleniyor; {csv.FieldCount} sütun bulundu."]));
                continue;
            }

            var product = new CsvProductRow(
                csv.GetString(columns["gtin"]),
                csv.GetString(columns["model_adi"]),
                csv.GetString(columns["batch_no"]),
                csv.GetString(columns["elyaf_pamuk_yuzde"]),
                csv.GetString(columns["elyaf_elastan_yuzde"]),
                csv.GetString(columns["uretim_yeri"]),
                csv.GetString(columns["karbon_kg_co2e"]));

            rows.Add(new CsvInputRow(rowNumber, product, Array.Empty<string>()));
        }

        if (rows.Count == 0)
        {
            throw new InvalidCsvException("CSV dosyasında başlıktan sonra en az bir ürün satırı bulunmalıdır.");
        }

        return rows;
    }
}

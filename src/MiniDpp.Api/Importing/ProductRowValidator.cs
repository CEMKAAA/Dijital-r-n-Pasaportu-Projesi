using System.Globalization;

namespace MiniDpp.Api.Importing;

public static class ProductRowValidator
{
    // ÖNEMLİ: InvariantCulture + sınırlı NumberStyles, 12.4 değerinin
    // Türkçe Windows ayarlarında da aynı sayı olarak okunmasını sağlar.
    // Binlik ayırıcı kabul edilmez; "12,4" yanlışlıkla 124'e dönüşmez.
    private const NumberStyles DecimalStyles =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static ProductRowValidationResult Validate(CsvProductRow row)
    {
        var errors = new List<string>();
        var gtin = Normalize(row.Gtin);

        if (gtin.Length == 0)
        {
            errors.Add("GTIN boş olamaz.");
        }
        else if (!GtinValidator.HasValidFormat(gtin))
        {
            errors.Add("GTIN, yalnızca 0-9 rakamlarından oluşan 14 haneli metin olmalıdır.");
        }
        else if (!GtinValidator.IsValid(gtin))
        {
            errors.Add("GTIN'in GS1 kontrol basamağı geçersiz.");
        }

        var modelName = ReadRequiredText(row.ModelName, "model_adi", errors);
        var batchNumber = ReadRequiredText(row.BatchNumber, "batch_no", errors);

        var cottonIsValid = TryReadPercentage(
            row.CottonPercentage, "elyaf_pamuk_yuzde", errors, out var cotton);
        var elastaneIsValid = TryReadPercentage(
            row.ElastanePercentage, "elyaf_elastan_yuzde", errors, out var elastane);

        // Önce aralık kontrol edilir. Böylece çok büyük iki sayıyı toplarken
        // taşma oluşmaz. decimal ile toplamı tam olarak 100'e karşılaştırırız.
        if (cottonIsValid && elastaneIsValid && cotton + elastane != 100m)
        {
            errors.Add("Pamuk ve elastan yüzdelerinin toplamı %100 olmalıdır.");
        }

        var productionLocation = ReadRequiredText(row.ProductionLocation, "uretim_yeri", errors);

        if (!TryReadDecimal(row.CarbonKgCo2e, out var carbon))
        {
            errors.Add("karbon_kg_co2e sayısal olmalıdır; ondalık ayırıcı olarak nokta kullanın.");
        }
        else if (carbon < 0m)
        {
            errors.Add("karbon_kg_co2e negatif olamaz.");
        }

        // ÖNEMLİ: Bir satırdaki bütün hatalar birlikte döner.
        // Hata varken kayıt hizmetine aktarılabilecek ürün oluşturulmaz.
        if (errors.Count > 0)
        {
            return new ProductRowValidationResult(null, errors);
        }

        var product = new ValidatedProduct(
            gtin, modelName, batchNumber, cotton, elastane, productionLocation, carbon);
        return new ProductRowValidationResult(product, Array.Empty<string>());
    }

    private static string ReadRequiredText(string? value, string fieldName, List<string> errors)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            errors.Add($"{fieldName} boş olamaz.");
        }

        return normalized;
    }

    private static bool TryReadPercentage(
        string? value, string fieldName, List<string> errors, out decimal percentage)
    {
        if (!TryReadDecimal(value, out percentage))
        {
            errors.Add($"{fieldName} sayısal olmalıdır; ondalık ayırıcı olarak nokta kullanın.");
            return false;
        }

        if (percentage is < 0m or > 100m)
        {
            errors.Add($"{fieldName} 0 ile 100 arasında olmalıdır.");
            return false;
        }

        return true;
    }

    private static bool TryReadDecimal(string? value, out decimal number) =>
        decimal.TryParse(Normalize(value), DecimalStyles, CultureInfo.InvariantCulture, out number);

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}

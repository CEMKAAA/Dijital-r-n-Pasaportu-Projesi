using System.Globalization;

namespace MiniDpp.Api.Passports;

public sealed record ProductPassport(
    string Gtin,
    string ModelName,
    string BatchNumber,
    decimal CottonPercentage,
    decimal ElastanePercentage,
    string ProductionLocation,
    decimal? CarbonKgCo2e)
{
    public bool IsAuthorized => CarbonKgCo2e.HasValue;

    public string Material => FormattableString.Invariant(
        $"%{CottonPercentage} pamuk, %{ElastanePercentage} elastan");

    // ÖNEMLİ: HTML ve JSON-LD alanları aynı kaynaktan oluşturulur.
    public IReadOnlyList<PassportProperty> GetProperties()
    {
        var properties = new List<PassportProperty>
        {
            new("batch_no", "Üretim partisi", BatchNumber),
            new("elyaf_pamuk_yuzde", "Pamuk", CottonPercentage, "%"),
            new("elyaf_elastan_yuzde", "Elastan", ElastanePercentage, "%"),
            new("uretim_yeri", "Üretim yeri", ProductionLocation)
        };

        if (CarbonKgCo2e is { } carbon)
        {
            properties.Add(new("karbon_kg_co2e", "Karbon ayak izi", carbon, "kg CO2e"));
        }

        return properties;
    }
}

public sealed record PassportProperty(string Id, string Label, object Value, string? Unit = null)
{
    public string FormattedValue => Value is IFormattable formattable
        ? formattable.ToString(null, CultureInfo.InvariantCulture)
        : Value.ToString() ?? string.Empty;
}

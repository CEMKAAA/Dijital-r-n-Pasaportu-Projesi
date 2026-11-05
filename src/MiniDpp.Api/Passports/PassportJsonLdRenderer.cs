namespace MiniDpp.Api.Passports;

public static class PassportJsonLdRenderer
{
    public static IReadOnlyDictionary<string, object> Render(ProductPassport passport, PassportUrlBuilder urls)
    {
        var properties = passport.GetProperties().Select(property =>
        {
            var value = new Dictionary<string, object>
            {
                ["@type"] = "PropertyValue",
                ["propertyID"] = property.Id,
                ["name"] = property.Label,
                ["value"] = property.Value
            };

            if (property.Unit is not null)
            {
                value["unitText"] = property.Unit;
            }

            return value;
        }).ToArray();

        return new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["@id"] = urls.HtmlUrl(passport.Gtin),
            ["url"] = urls.HtmlUrl(passport.Gtin),
            ["name"] = passport.ModelName,
            ["gtin14"] = passport.Gtin,
            ["material"] = passport.Material,
            ["additionalProperty"] = properties
        };
    }
}

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MiniDpp.Tests;

public class PassportApiTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HtmlAndJsonLdContainTheSameFieldsForEveryImportedProduct(bool authorized)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client, CsvSamples.ReadAssignmentFile());
        upload.EnsureSuccessStatusCode();

        var expected = new[]
        {
            new ExpectedProduct("08690000000012", "Slim Denim 501", "B2027-01", 98m, 2m, "Denizli", 12.4m),
            new ExpectedProduct("08690000000029", "Regular Denim 502", "B2027-02", 99m, 1m, "Denizli", 11.8m),
            new ExpectedProduct("08690000000050", "Mom Jean 505", "B2027-05", 97m, 3m, "Bursa", 12.2m)
        };

        var query = authorized ? "?rol=yetkili" : string.Empty;
        foreach (var product in expected)
        {
            using var htmlResponse = await client.GetAsync($"/dpp/{product.Gtin}{query}");
            using var jsonResponse = await client.GetAsync($"/dpp/{product.Gtin}.jsonld{query}");
            Assert.Equal(HttpStatusCode.OK, htmlResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, jsonResponse.StatusCode);
            Assert.Equal("text/html", htmlResponse.Content.Headers.ContentType!.MediaType);
            Assert.Equal("application/ld+json", jsonResponse.Content.Headers.ContentType!.MediaType);
            Assert.True(htmlResponse.Headers.CacheControl!.NoStore);
            Assert.True(jsonResponse.Headers.CacheControl!.NoStore);

            var html = await htmlResponse.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(await jsonResponse.Content.ReadAsStringAsync());
            var root = json.RootElement;
            Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
            Assert.Equal("Product", root.GetProperty("@type").GetString());
            Assert.Equal(product.Gtin, root.GetProperty("gtin14").GetString());
            Assert.Equal(product.Name, root.GetProperty("name").GetString());
            Assert.Equal($"http://localhost/dpp/{product.Gtin}", root.GetProperty("url").GetString());
            Assert.Contains(product.Name, WebUtility.HtmlDecode(html));
            Assert.Contains(product.Gtin, html);
            Assert.Contains($"href=\"/dpp/{product.Gtin}.jsonld{query}\"", html);
            Assert.Contains($"src=\"/dpp/{product.Gtin}/qr.png\"", html);

            var fields = root.GetProperty("additionalProperty").EnumerateArray()
                .ToDictionary(field => field.GetProperty("propertyID").GetString()!);
            Assert.Equal(authorized ? 5 : 4, fields.Count);
            AssertSharedField(html, fields, "batch_no", product.Batch);
            AssertSharedField(html, fields, "elyaf_pamuk_yuzde", product.Cotton, "%");
            AssertSharedField(html, fields, "elyaf_elastan_yuzde", product.Elastane, "%");
            AssertSharedField(html, fields, "uretim_yeri", product.Location);

            if (authorized)
            {
                AssertSharedField(html, fields, "karbon_kg_co2e", product.Carbon, "kg CO2e");
            }
            else
            {
                Assert.DoesNotContain("karbon_kg_co2e", html);
                Assert.DoesNotContain("Karbon ayak izi", WebUtility.HtmlDecode(html));
                Assert.False(fields.ContainsKey("karbon_kg_co2e"));
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("?rol=admin")]
    [InlineData("?rol=Yetkili")]
    [InlineData("?rol=yetkili&rol=tuketici")]
    public async Task DefaultAndUnrecognizedRolesNeverExposeCarbon(string query)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client, CsvSamples.Build(CsvSamples.FirstRow));
        upload.EnsureSuccessStatusCode();

        var html = await client.GetStringAsync($"/dpp/08690000000012{query}");
        var json = await client.GetStringAsync($"/dpp/08690000000012.jsonld{query}");

        Assert.DoesNotContain("karbon_kg_co2e", html);
        Assert.DoesNotContain("karbon_kg_co2e", json);
        Assert.DoesNotContain("12.4", html);
        Assert.DoesNotContain("12.4", json);
    }

    [Fact]
    public async Task ZeroCarbonRemainsVisibleInTheAuthorizedView()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client,
            CsvSamples.Build(CsvSamples.FirstRow.Replace(",12.4", ",0", StringComparison.Ordinal)));
        upload.EnsureSuccessStatusCode();

        var html = await client.GetStringAsync("/dpp/08690000000012?rol=yetkili");
        using var json = JsonDocument.Parse(await client.GetStringAsync("/dpp/08690000000012.jsonld?rol=yetkili"));
        var carbon = json.RootElement.GetProperty("additionalProperty").EnumerateArray()
            .Single(field => field.GetProperty("propertyID").GetString() == "karbon_kg_co2e");

        Assert.Equal(0m, carbon.GetProperty("value").GetDecimal());
        Assert.Contains("data-property=\"karbon_kg_co2e\"", html);
    }

    [Theory]
    [InlineData("/dpp/08690000000067")]
    [InlineData("/dpp/08690000000067.jsonld")]
    [InlineData("/dpp/08690000000067/qr.png")]
    [InlineData("/dpp/invalid")]
    [InlineData("/dpp/invalid.jsonld")]
    [InlineData("/dpp/invalid/qr.png")]
    public async Task UnknownOrInvalidGtinReturnsNotFound(string path)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadedMarkupIsEncodedInHtmlAndPreservedAsDataInJsonLd()
    {
        const string modelName = "<script>alert('x')</script>";
        const string location = "<img src=x onerror=alert(1)>";
        var csv = CsvSamples.Build($"08690000000012,{modelName},B1,98,2,{location},12.4");
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client, csv);
        upload.EnsureSuccessStatusCode();

        var html = await client.GetStringAsync("/dpp/08690000000012");
        using var json = JsonDocument.Parse(await client.GetStringAsync("/dpp/08690000000012.jsonld"));

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;img", html);
        Assert.Equal(modelName, json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task QrIsPngAndDoesNotChangeWithTheRoleParameter()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client, CsvSamples.Build(CsvSamples.FirstRow));
        upload.EnsureSuccessStatusCode();

        using var response = await client.GetAsync("/dpp/08690000000012/qr.png");
        using var authorizedResponse = await client.GetAsync("/dpp/08690000000012/qr.png?rol=yetkili");
        var png = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.True(png.Length > 100);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Equal(png, await authorizedResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task PassportUrlsUseConfigurationInsteadOfTheRequestHost()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var upload = await UploadAsync(client, CsvSamples.Build(CsvSamples.FirstRow));
        upload.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Host = "untrusted.example";

        using var json = JsonDocument.Parse(await client.GetStringAsync("/dpp/08690000000012.jsonld"));

        Assert.Equal("http://localhost/dpp/08690000000012",
            json.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task PassportStylesheetIsAvailable()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/styles.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType!.MediaType);
    }

    private static void AssertSharedField(
        string html, Dictionary<string, JsonElement> fields, string id, object expected, string? unit = null)
    {
        var field = fields[id];
        Assert.Equal("PropertyValue", field.GetProperty("@type").GetString());
        if (expected is decimal number)
        {
            Assert.Equal(number, field.GetProperty("value").GetDecimal());
        }
        else
        {
            Assert.Equal((string)expected, field.GetProperty("value").GetString());
        }

        if (unit is not null)
        {
            Assert.Equal(unit, field.GetProperty("unitText").GetString());
        }

        var match = Regex.Match(html,
            $"data-property=\"{Regex.Escape(id)}\">\\s*<dt>(?<label>.*?)</dt>\\s*<dd>(?<value>.*?)</dd>",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"HTML alanı bulunamadı: {id}");
        var actualText = WebUtility.HtmlDecode(Regex.Replace(match.Groups["value"].Value, "<[^>]*>", "")).Trim();
        if (expected is decimal expectedNumber)
        {
            if (unit is not null)
            {
                Assert.EndsWith(" " + unit, actualText);
                actualText = actualText[..^(unit.Length + 1)];
            }

            // SQLite 98'i 98.0 olarak geri getirebilir; sayısal eşitliği kontrol ederiz.
            Assert.Equal(expectedNumber, decimal.Parse(actualText, CultureInfo.InvariantCulture));
        }
        else
        {
            Assert.Equal((string)expected, actualText);
        }
        Assert.Equal(field.GetProperty("name").GetString(), WebUtility.HtmlDecode(match.Groups["label"].Value));
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string csv)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "urunler.csv");
        return await client.PostAsync("/api/products/import", form);
    }

    private sealed record ExpectedProduct(
        string Gtin, string Name, string Batch, decimal Cotton,
        decimal Elastane, string Location, decimal Carbon);
}

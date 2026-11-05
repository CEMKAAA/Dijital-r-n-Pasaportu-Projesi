using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MiniDpp.Api.Importing;
using MiniDpp.Api.Passports;

namespace MiniDpp.Tests;

public class HomeApiTests
{
    [Fact]
    public async Task EmptyHomePageContainsAnAccessibleUploadFormAndEmptyState()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Contains("action=\"/api/products/import\"", html);
        Assert.Contains("enctype=\"multipart/form-data\"", html);
        Assert.Contains("name=\"file\"", html);
        Assert.Contains("for=\"csv-file\"", html);
        Assert.Contains("Henüz ürün yok", html);
        Assert.Contains("href=\"/sample.csv\"", html);
        Assert.Contains("type=\"module\" src=\"/app.js\"", html);
    }

    [Fact]
    public async Task DownloadedSampleCanBeUploadedAndItsPassportsAppearInTheHomePage()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var sample = await client.GetAsync("/sample.csv");

        Assert.Equal(HttpStatusCode.OK, sample.StatusCode);
        Assert.Equal("text/csv", sample.Content.Headers.ContentType!.MediaType);
        Assert.Contains("urunler_duzeltilmis_test.csv", sample.Content.Headers.ContentDisposition!.ToString());
        var bytes = await sample.Content.ReadAsByteArrayAsync();
        var original = await File.ReadAllBytesAsync(Path.Combine(
            AppContext.BaseDirectory, "TestData", "urunler_duzeltilmis_test.csv"));
        Assert.Equal(original, bytes);

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "urunler.csv");
        using var response = await client.PostAsync("/api/products/import", form);
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;
        Assert.Equal(3, report.ImportedCount);
        Assert.Equal(2, report.RejectedCount);

        var products = (await client.GetFromJsonAsync<List<ProductSummary>>("/api/products"))!;
        Assert.Equal(3, products.Count);
        var html = await client.GetStringAsync("/");
        foreach (var product in products)
        {
            Assert.Contains(product.Gtin, html);
            Assert.Contains($"href=\"/dpp/{product.Gtin}\"", html);
            Assert.Contains($"href=\"/dpp/{product.Gtin}.jsonld\"", html);
            Assert.Contains($"href=\"/dpp/{product.Gtin}/qr.png\"", html);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("?rol=yetkili")]
    public async Task ProductListOnlyContainsPublicSummaryFields(string query)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(CsvSamples.Build(CsvSamples.FirstRow), Encoding.UTF8), "file", "urunler.csv");
        using var upload = await client.PostAsync("/api/products/import", form);
        upload.EnsureSuccessStatusCode();

        using var response = await client.GetAsync("/api/products" + query);
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        var product = Assert.Single(json.RootElement.EnumerateArray());

        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("08690000000012", product.GetProperty("gtin").GetString());
        Assert.Equal(4, product.EnumerateObject().Count());
        Assert.DoesNotContain("carbon", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("12.4", text);
    }

    [Fact]
    public async Task HomePageEncodesUploadedModelBatchAndLocation()
    {
        const string row = "08690000000012,<script>alert('model')</script>,<img src=x onerror=alert('batch')>,98,2,<b>Denizli</b>,12.4";
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(CsvSamples.Build(row), Encoding.UTF8), "file", "urunler.csv");
        using var upload = await client.PostAsync("/api/products/import", form);
        upload.EnsureSuccessStatusCode();

        var html = await client.GetStringAsync("/");
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.DoesNotContain("<b>Denizli</b>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;img", html);
        Assert.Contains("&lt;b&gt;Denizli&lt;/b&gt;", html);
    }

    [Theory]
    [InlineData("/app.js")]
    [InlineData("/import-client.js")]
    public async Task BrowserModulesAreServedAsJavaScript(string path)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType!.MediaType!);
    }
}

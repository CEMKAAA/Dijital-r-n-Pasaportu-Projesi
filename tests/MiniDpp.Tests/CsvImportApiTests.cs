using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniDpp.Api.Data;
using MiniDpp.Api.Endpoints;
using MiniDpp.Api.Importing;

namespace MiniDpp.Tests;

public class CsvImportApiTests
{
    [Fact]
    public async Task AssignmentUploadStoresThreeProductsAndReportsLinesFourAndFive()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client, CsvSamples.ReadAssignmentFile());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;
        Assert.Equal(5, report.TotalRows);
        Assert.Equal(3, report.ImportedCount);
        Assert.Equal(0, report.UnchangedCount);
        Assert.Equal(2, report.RejectedCount);
        Assert.Equal(new[] { 4, 5 }, report.Errors.Select(error => error.RowNumber));
        Assert.Contains(report.Errors[0].Errors, error => error.Contains("GTIN"));
        Assert.Contains(report.Errors[1].Errors, error => error.Contains("toplamı %100"));
        Assert.Equal(new[] { "08690000000012", "08690000000029", "08690000000050" },
            report.Products.Select(product => product.Gtin));

        await AssertCountsAsync(factory, products: 3, models: 3, batches: 3);
    }

    [Fact]
    public async Task ReuploadIsIdempotentAndRetainsTheOriginalValidationErrors()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var first = await UploadAsync(client, CsvSamples.ReadAssignmentFile());
        first.EnsureSuccessStatusCode();

        using var second = await UploadAsync(client, CsvSamples.ReadAssignmentFile());
        var report = (await second.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(0, report.ImportedCount);
        Assert.Equal(3, report.UnchangedCount);
        Assert.Equal(2, report.RejectedCount);
        Assert.All(report.Products, product => Assert.Equal("unchanged", product.Status));
        await AssertCountsAsync(factory, products: 3, models: 3, batches: 3);
    }

    [Fact]
    public async Task ProductsReuseModelsAndBatches()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        var csv = CsvSamples.Build(
            "08690000000012,Tek Model,B1,98,2,Denizli,12.4",
            "08690000000029,Tek Model,B1,99,1,Denizli,11.8",
            "08690000000050,Tek Model,B2,97,3,Bursa,12.2");

        using var response = await UploadAsync(client, csv);
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(3, report.ImportedCount);
        Assert.Empty(report.Errors);
        await AssertCountsAsync(factory, products: 3, models: 1, batches: 2);
    }

    [Fact]
    public async Task ConflictingGtinIsRejectedAndLaterValidRowsStillImport()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var initial = await UploadAsync(client, CsvSamples.Build(CsvSamples.FirstRow));
        initial.EnsureSuccessStatusCode();

        using var response = await UploadAsync(client, CsvSamples.Build(
            "08690000000012,Başka Model,YeniParti,98,2,Bursa,99",
            CsvSamples.SecondRow));
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(1, report.ImportedCount);
        Assert.Equal(1, report.RejectedCount);
        Assert.Equal(2, Assert.Single(report.Errors).RowNumber);
        await AssertCountsAsync(factory, products: 2, models: 2, batches: 2);

        await using var scope = factory.Services.CreateAsyncScope();
        var original = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Products.SingleAsync(product => product.Gtin == "08690000000012");
        Assert.Equal(12.4m, original.CarbonKgCo2e);
        Assert.Equal("Denizli", original.ProductionLocation);
    }

    [Fact]
    public async Task DuplicateRowsInsideOneFileAreCountedWithoutCreatingDuplicates()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client,
            CsvSamples.Build(CsvSamples.FirstRow, CsvSamples.FirstRow));
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(1, report.ImportedCount);
        Assert.Equal(1, report.UnchangedCount);
        Assert.Equal(0, report.RejectedCount);
        await AssertCountsAsync(factory, products: 1, models: 1, batches: 1);
    }

    [Fact]
    public async Task DatabaseFailureRollsBackOnlyThatRowsModelBatchAndProduct()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER RejectFirstProduct BEFORE INSERT ON Products
                WHEN NEW.Gtin = '08690000000012'
                BEGIN SELECT RAISE(ABORT, 'Test icin kayit hatasi'); END;
                """);
        }

        using var response = await UploadAsync(client,
            CsvSamples.Build(CsvSamples.FirstRow, CsvSamples.SecondRow));
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(1, report.ImportedCount);
        Assert.Equal(1, report.RejectedCount);
        Assert.Equal(2, Assert.Single(report.Errors).RowNumber);
        Assert.Equal("08690000000029", Assert.Single(report.Products).Gtin);
        await AssertCountsAsync(factory, products: 1, models: 1, batches: 1);
    }

    [Fact]
    public async Task AllInvalidRowsProduceAReportAndLeaveNoOrphanRecords()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client, CsvSamples.Build(
            ",Wide Leg Denim 503,B2027-03,100,0,Bursa,13.1",
            "08690000000043,Skinny Denim 504,B2027-04,95,3,Denizli,12.9"));
        var report = (await response.Content.ReadFromJsonAsync<CsvImportReport>())!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, report.ImportedCount);
        Assert.Equal(2, report.RejectedCount);
        await AssertCountsAsync(factory, products: 0, models: 0, batches: 0);
    }

    [Theory]
    [InlineData("gtin,model_adi\n08690000000012,Slim\n")]
    [InlineData(CsvSamples.Header)]
    public async Task InvalidFileStructureReturnsBadRequestWithoutWritingRecords(string csv)
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client, csv);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        await AssertCountsAsync(factory, products: 0, models: 0, batches: 0);
    }

    [Fact]
    public async Task MissingFileReturnsBadRequest()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Dosya bulunmuyor"), "other");
        using var response = await client.PostAsync("/api/products/import", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyFileReturnsBadRequest()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client, "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OversizedFileReturnsPayloadTooLarge()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var response = await UploadAsync(client, new string('x', ImportEndpoints.MaxUploadBytes + 1));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        await AssertCountsAsync(factory, products: 0, models: 0, batches: 0);
    }

    [Fact]
    public async Task InvalidUtf8ReturnsBadRequest()
    {
        await using var factory = new MiniDppFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([0xC3, 0x28]), "file", "invalid.csv");
        using var response = await client.PostAsync("/api/products/import", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertCountsAsync(factory, products: 0, models: 0, batches: 0);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string csv)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", "urunler.csv");
        return await client.PostAsync("/api/products/import", form);
    }

    private static async Task AssertCountsAsync(MiniDppFactory factory, int products, int models, int batches)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(products, await db.Products.CountAsync());
        Assert.Equal(models, await db.ProductModels.CountAsync());
        Assert.Equal(batches, await db.ProductBatches.CountAsync());
    }
}

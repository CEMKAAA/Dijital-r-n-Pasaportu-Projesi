using MiniDpp.Api.Importing;

namespace MiniDpp.Tests;

public class CsvProductReaderTests
{
    [Fact]
    public void AssignmentFileRetainsPhysicalLineNumbers()
    {
        var rows = CsvProductReader.Read(CsvSamples.ReadAssignmentFile());

        Assert.Equal(5, rows.Count);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, rows.Select(row => row.RowNumber));
        Assert.Empty(rows[2].Product!.Gtin!);
        Assert.All(rows, row => Assert.Empty(row.Errors));
    }

    [Fact]
    public void QuotedCommasAndEscapedQuotesAreReadAsPartOfTheField()
    {
        var rows = CsvProductReader.Read(CsvSamples.Build(
            "08690000000012,\"Slim, \"\"Edition\"\"\",B2027-01,98,2,Denizli,12.4"));

        var row = Assert.Single(rows);
        Assert.Empty(row.Errors);
        Assert.Equal("Slim, \"Edition\"", row.Product!.ModelName);
        Assert.Equal("08690000000012", row.Product.Gtin);
    }

    [Fact]
    public void HeaderOrderCaseAndSurroundingWhitespaceMayVary()
    {
        const string csv = " MODEL_ADI , GTIN ,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e\n"
            + "Slim Denim 501,08690000000012,B2027-01,98,2,Denizli,12.4\n";

        var row = Assert.Single(CsvProductReader.Read(csv));

        Assert.Empty(row.Errors);
        Assert.Equal("08690000000012", row.Product!.Gtin);
        Assert.Equal("Slim Denim 501", row.Product.ModelName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("gtin,model_adi\n08690000000012,Slim\n")]
    [InlineData("gtin,gtin,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e\n")]
    [InlineData("gtin;model_adi;batch_no;elyaf_pamuk_yuzde;elyaf_elastan_yuzde;uretim_yeri;karbon_kg_co2e\n")]
    [InlineData(CsvSamples.Header)]
    public void InvalidHeadersOrMissingDataAreRejected(string csv)
    {
        Assert.Throws<InvalidCsvException>(() => CsvProductReader.Read(csv));
    }

    [Fact]
    public void EmptyLinesAreReportedAndDoNotShiftFollowingLineNumbers()
    {
        var rows = CsvProductReader.Read(CsvSamples.Build(CsvSamples.FirstRow, "", CsvSamples.SecondRow));

        Assert.Equal(new[] { 2, 3, 4 }, rows.Select(row => row.RowNumber));
        Assert.Null(rows[1].Product);
        Assert.Single(rows[1].Errors);
        Assert.Equal("08690000000029", rows[2].Product!.Gtin);
    }

    [Fact]
    public void QuotedNewlinesDoNotShiftTheFollowingErrorLine()
    {
        var csv = CsvSamples.Build(
            "08690000000012,\"Slim\nDenim\",B2027-01,98,2,Denizli,12.4",
            ",Regular Denim 502,B2027-02,99,1,Denizli,11.8");

        var rows = CsvProductReader.Read(csv);

        Assert.Equal(new[] { 2, 4 }, rows.Select(row => row.RowNumber));
        Assert.Equal("Slim\nDenim", rows[0].Product!.ModelName);
        Assert.False(ProductRowValidator.Validate(rows[1].Product!).IsValid);
    }

    [Fact]
    public void MissingAndExtraColumnsAreRowErrorsAndLaterRowsAreStillRead()
    {
        var rows = CsvProductReader.Read(CsvSamples.Build(
            "08690000000012,Slim Denim 501,B2027-01,98,2,Denizli",
            CsvSamples.FirstRow + ",extra",
            CsvSamples.SecondRow));

        Assert.Null(rows[0].Product);
        Assert.Null(rows[1].Product);
        Assert.All(rows.Take(2), row => Assert.Single(row.Errors));
        Assert.NotNull(rows[2].Product);
        Assert.Equal(4, rows[2].RowNumber);
    }

    [Theory]
    [InlineData("08690000000012,Slim\"Denim,B2027-01,98,2,Denizli,12.4")]
    [InlineData("08690000000012,\"Slim\"tail,B2027-01,98,2,Denizli,12.4")]
    public void MalformedQuotesAreRejectedWithoutSavingRecoveredValues(string malformedRow)
    {
        var rows = CsvProductReader.Read(CsvSamples.Build(malformedRow, CsvSamples.SecondRow));

        Assert.Equal(2, rows.Count);
        Assert.Null(rows[0].Product);
        Assert.Contains("tırnak", Assert.Single(rows[0].Errors));
        Assert.NotNull(rows[1].Product);
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void Utf8BomDoesNotBecomePartOfTheGtinHeader()
    {
        var row = Assert.Single(CsvProductReader.Read("\uFEFF" + CsvSamples.Build(CsvSamples.FirstRow)));

        Assert.Equal("08690000000012", row.Product!.Gtin);
    }
}

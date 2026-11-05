using System.Globalization;
using MiniDpp.Api.Importing;

namespace MiniDpp.Tests;

public class ProductRowValidatorTests
{
    private static CsvProductRow ValidRow => new(
        "08690000000012", "Slim Denim 501", "B2027-01", "98", "2", "Denizli", "12.4");

    [Fact]
    public void ValidRowReturnsTypedValuesAndPreservesLeadingZero()
    {
        var result = ProductRowValidator.Validate(ValidRow);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        var product = Assert.IsType<ValidatedProduct>(result.Product);
        Assert.Equal("08690000000012", product.Gtin);
        Assert.Equal(98m, product.CottonPercentage);
        Assert.Equal(2m, product.ElastanePercentage);
        Assert.Equal(12.4m, product.CarbonKgCo2e);
    }

    [Theory]
    [InlineData("100", "0")]
    [InlineData("0", "100")]
    [InlineData("97.5", "2.5")]
    [InlineData("99.99", "0.01")]
    public void ValidPercentageBoundariesAndDecimalsAreAccepted(string cotton, string elastane)
    {
        var result = ProductRowValidator.Validate(ValidRow with
        {
            CottonPercentage = cotton,
            ElastanePercentage = elastane
        });

        Assert.True(result.IsValid);
        var product = Assert.IsType<ValidatedProduct>(result.Product);
        Assert.Equal(100m, product.CottonPercentage + product.ElastanePercentage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void RequiredTextFieldsCannotBeMissing(string? value)
    {
        var cases = new (CsvProductRow Row, string Field)[]
        {
            (ValidRow with { ModelName = value }, "model_adi"),
            (ValidRow with { BatchNumber = value }, "batch_no"),
            (ValidRow with { ProductionLocation = value }, "uretim_yeri")
        };

        foreach (var (row, field) in cases)
        {
            var result = ProductRowValidator.Validate(row);
            Assert.False(result.IsValid);
            Assert.Null(result.Product);
            Assert.Contains(field, Assert.Single(result.Errors));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("08690000000013")]
    [InlineData("8690000000012")]
    [InlineData("0869000000001A")]
    [InlineData("٠٨٦٩٠٠٠٠٠٠٠٠١٢")]
    public void InvalidGtinPreventsProductCreation(string? gtin)
    {
        var result = ProductRowValidator.Validate(ValidRow with { Gtin = gtin });

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Contains("GTIN", Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("12,4")]
    [InlineData("1,000")]
    [InlineData("1e2")]
    [InlineData("79228162514264337593543950336")]
    public void NonDecimalNumbersAreRejectedInEveryNumericColumn(string? value)
    {
        var result = ProductRowValidator.Validate(ValidRow with
        {
            CottonPercentage = value,
            ElastanePercentage = value,
            CarbonKgCo2e = value
        });

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, error => error.Contains("elyaf_pamuk_yuzde"));
        Assert.Contains(result.Errors, error => error.Contains("elyaf_elastan_yuzde"));
        Assert.Contains(result.Errors, error => error.Contains("karbon_kg_co2e"));
    }

    [Theory]
    [InlineData("-1", "101")]
    [InlineData("101", "-1")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
    public void OutOfRangePercentagesAreRejectedBeforeAddingThem(string cotton, string elastane)
    {
        var result = ProductRowValidator.Validate(ValidRow with
        {
            CottonPercentage = cotton,
            ElastanePercentage = elastane
        });

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, error => Assert.Contains("0 ile 100", error));
    }

    [Theory]
    [InlineData("95", "3")]
    [InlineData("100", "1")]
    [InlineData("0", "0")]
    [InlineData("99.99", "0")]
    public void PercentageTotalMustBeExactlyOneHundred(string cotton, string elastane)
    {
        var result = ProductRowValidator.Validate(ValidRow with
        {
            CottonPercentage = cotton,
            ElastanePercentage = elastane
        });

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Contains("toplamı %100", Assert.Single(result.Errors));
    }

    [Fact]
    public void CarbonCannotBeNegative()
    {
        var result = ProductRowValidator.Validate(ValidRow with { CarbonKgCo2e = "-0.01" });

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Contains("negatif", Assert.Single(result.Errors));
    }

    [Fact]
    public void ZeroCarbonIsAccepted()
    {
        var result = ProductRowValidator.Validate(ValidRow with { CarbonKgCo2e = "0" });

        Assert.True(result.IsValid);
        Assert.Equal(0m, Assert.IsType<ValidatedProduct>(result.Product).CarbonKgCo2e);
    }

    [Fact]
    public void SurroundingWhitespaceIsNormalized()
    {
        var result = ProductRowValidator.Validate(new CsvProductRow(
            " 08690000000012 ", " Slim Denim 501 ", " B2027-01 ",
            " 98 ", " 2 ", " Denizli ", " 12.4 "));

        Assert.True(result.IsValid);
        var product = Assert.IsType<ValidatedProduct>(result.Product);
        Assert.Equal("08690000000012", product.Gtin);
        Assert.Equal("Slim Denim 501", product.ModelName);
        Assert.Equal("B2027-01", product.BatchNumber);
        Assert.Equal("Denizli", product.ProductionLocation);
    }

    [Fact]
    public void DecimalParsingDoesNotDependOnTurkishSystemCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var result = ProductRowValidator.Validate(ValidRow);

            Assert.True(result.IsValid);
            Assert.Equal(12.4m, Assert.IsType<ValidatedProduct>(result.Product).CarbonKgCo2e);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void AllFieldErrorsAreReturnedTogether()
    {
        var result = ProductRowValidator.Validate(new CsvProductRow(
            "", "", "", "abc", "abc", "", "-1"));

        Assert.False(result.IsValid);
        Assert.Null(result.Product);
        Assert.Equal(7, result.Errors.Count);
    }
}

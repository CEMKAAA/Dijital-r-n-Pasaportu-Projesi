using MiniDpp.Api.Importing;

namespace MiniDpp.Tests;

public class GtinValidatorTests
{
    [Theory]
    [InlineData("08690000000012")]
    [InlineData("08690000000029")]
    [InlineData("08690000000043")]
    [InlineData("08690000000050")]
    [InlineData("05012345670003")]
    public void ValidGs1CheckDigitsAreAccepted(string gtin)
    {
        Assert.True(GtinValidator.IsValid(gtin));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("8690000000012")]
    [InlineData("008690000000012")]
    [InlineData("08690000000013")]
    [InlineData("0869000000001A")]
    [InlineData(" 08690000000012")]
    [InlineData("０８６９００００００００１２")]
    [InlineData("٠٨٦٩٠٠٠٠٠٠٠٠١٢")]
    public void InvalidShapeOrCheckDigitIsRejected(string? gtin)
    {
        Assert.False(GtinValidator.IsValid(gtin));
    }

    [Fact]
    public void ChangingAnySingleDigitInvalidatesTheGtin()
    {
        const string original = "08690000000012";

        for (var index = 0; index < original.Length; index++)
        {
            var changed = original.ToCharArray();
            changed[index] = (char)('0' + (changed[index] - '0' + 1) % 10);

            Assert.False(GtinValidator.IsValid(new string(changed)));
        }
    }
}

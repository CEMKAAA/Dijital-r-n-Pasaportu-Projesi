using Microsoft.Extensions.Options;
using MiniDpp.Api.Passports;

namespace MiniDpp.Tests;

public class DppOptionsTests
{
    [Theory]
    [InlineData("http://localhost:5100")]
    [InlineData("http://localhost:5100/")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com:8443/")]
    public void RootHttpUrlsAreAccepted(string value)
    {
        Assert.True(DppOptions.IsValidPublicBaseUrl(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("localhost:5100")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///tmp/dpp")]
    [InlineData("https://example.com/dpp")]
    [InlineData("https://example.com/?rol=yetkili")]
    [InlineData("https://example.com/#fragment")]
    [InlineData("https://user:password@example.com/")]
    public void InvalidOrAmbiguousBaseUrlsAreRejected(string? value)
    {
        Assert.False(DppOptions.IsValidPublicBaseUrl(value));
    }

    [Fact]
    public void CanonicalUrlPreservesLeadingZeroAndRoleIsOnlyAddedExplicitly()
    {
        var urls = new PassportUrlBuilder(Options.Create(new DppOptions
        {
            PublicBaseUrl = "http://localhost:5100/"
        }));

        Assert.Equal("http://localhost:5100/dpp/08690000000012", urls.HtmlUrl("08690000000012"));
        Assert.Equal("/dpp/08690000000012?rol=yetkili", urls.HtmlPath("08690000000012", true));
        Assert.Equal("/dpp/08690000000012.jsonld?rol=yetkili", urls.JsonLdPath("08690000000012", true));
        Assert.Equal("/dpp/08690000000012/qr.png", urls.QrPath("08690000000012"));
    }
}

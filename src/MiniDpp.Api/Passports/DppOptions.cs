namespace MiniDpp.Api.Passports;

public sealed class DppOptions
{
    public const string SectionName = "Dpp";

    public string PublicBaseUrl { get; set; } = "http://localhost:5100";

    public static bool IsValidPublicBaseUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && uri.UserInfo.Length == 0
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0;
}

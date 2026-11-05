using Microsoft.Extensions.Options;

namespace MiniDpp.Api.Passports;

public sealed class PassportUrlBuilder
{
    private readonly string _baseUrl;

    public PassportUrlBuilder(IOptions<DppOptions> options)
    {
        _baseUrl = new Uri(options.Value.PublicBaseUrl).GetLeftPart(UriPartial.Authority);
    }

    public string HtmlPath(string gtin, bool authorized = false) =>
        $"/dpp/{Uri.EscapeDataString(gtin)}{RoleQuery(authorized)}";

    // ÖNEMLİ: Kalıcı adres ve QR hedefi her zaman tüketici görünümüdür.
    // Adres, isteğin Host başlığı yerine yapılandırmadan alınır.
    public string HtmlUrl(string gtin) => _baseUrl + HtmlPath(gtin);

    public string JsonLdPath(string gtin, bool authorized = false) =>
        $"/dpp/{Uri.EscapeDataString(gtin)}.jsonld{RoleQuery(authorized)}";

    public string QrPath(string gtin) => $"/dpp/{Uri.EscapeDataString(gtin)}/qr.png";

    private static string RoleQuery(bool authorized) => authorized ? "?rol=yetkili" : string.Empty;
}

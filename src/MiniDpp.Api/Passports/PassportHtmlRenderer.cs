using System.Text;
using System.Text.Encodings.Web;
using MiniDpp.Api.Web;

namespace MiniDpp.Api.Passports;

public static class PassportHtmlRenderer
{
    public static string Render(ProductPassport passport, PassportUrlBuilder urls)
    {
        var rows = new StringBuilder();
        foreach (var property in passport.GetProperties())
        {
            var unit = property.Unit is null
                ? string.Empty
                : $" <span class=\"unit\">{Encode(property.Unit)}</span>";

            rows.AppendLine($"""
                <div class="fact" data-property="{Encode(property.Id)}">
                  <dt>{Encode(property.Label)}</dt>
                  <dd>{Encode(property.FormattedValue)}{unit}</dd>
                </div>
                """);
        }

        var role = passport.IsAuthorized ? "Yetkili görünümü" : "Tüketici görünümü";
        var otherRole = passport.IsAuthorized ? "Tüketici görünümüne geç" : "Yetkili görünümüne geç";
        var qrPath = urls.QrPath(passport.Gtin);
        var body = $$"""
            <main id="main-content" class="page-shell">
              <div class="page-heading">
                <div>
                  <p class="eyebrow">Dijital ürün pasaportu</p>
                  <h1>{{Encode(passport.ModelName)}}</h1>
                  <p class="gtin">GTIN <code>{{Encode(passport.Gtin)}}</code></p>
                </div>
                <nav class="role-switch" aria-label="Görünüm seçimi">
                  <span class="badge">{{role}}</span>
                  <a href="{{Encode(urls.HtmlPath(passport.Gtin, !passport.IsAuthorized))}}">{{otherRole}}</a>
                </nav>
              </div>
              <div class="passport-grid">
                <section class="card" aria-labelledby="product-details">
                  <h2 id="product-details">Ürün bilgileri</h2>
                  <dl class="facts">{{rows}}</dl>
                  <div class="card-actions">
                    <a class="button" href="{{Encode(urls.JsonLdPath(passport.Gtin, passport.IsAuthorized))}}">JSON-LD çıktısını aç</a>
                  </div>
                </section>
                <aside class="card qr-card" aria-labelledby="share-heading">
                  <h2 id="share-heading">Pasaportu paylaş</h2>
                  <img class="qr-image" src="{{Encode(qrPath)}}" width="240" height="240" alt="Ürün pasaportuna yönlendiren QR kodu">
                  <a class="button button-secondary" href="{{Encode(qrPath)}}" download="{{Encode(passport.Gtin)}}-qr.png">QR kodunu indir</a>
                  <p class="muted">QR kodu tüketici görünümünü açar.</p>
                </aside>
              </div>
              <p class="permalink">Kalıcı pasaport adresi: <a href="{{Encode(urls.HtmlUrl(passport.Gtin))}}">{{Encode(urls.HtmlUrl(passport.Gtin))}}</a></p>
            </main>
            """;

        var head = $$"""
            <link rel="canonical" href="{{Encode(urls.HtmlUrl(passport.Gtin))}}">
            <link rel="alternate" type="application/ld+json" href="{{Encode(urls.JsonLdPath(passport.Gtin, passport.IsAuthorized))}}">
            """;

        return HtmlLayout.Render(passport.ModelName, body, head);
    }

    public static string NotFound() => HtmlLayout.Render("Ürün bulunamadı", """
        <main id="main-content" class="page-shell">
          <section class="card">
            <p class="eyebrow">404</p>
            <h1>Ürün bulunamadı</h1>
            <p>Bu GTIN için kayıtlı bir ürün pasaportu bulunamadı.</p>
          </section>
        </main>
        """);

    // ÖNEMLİ: CSV'den gelen bütün metinler HTML'e yazılmadan önce kodlanır.
    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}

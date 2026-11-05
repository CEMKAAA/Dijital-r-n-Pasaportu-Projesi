using System.Text;
using System.Text.Encodings.Web;
using MiniDpp.Api.Passports;

namespace MiniDpp.Api.Web;

public static class HomePageRenderer
{
    public static string Render(IReadOnlyList<ProductSummary> products, PassportUrlBuilder urls)
    {
        var rows = new StringBuilder();
        foreach (var product in products)
        {
            rows.AppendLine($$"""
                <tr>
                  <td class="model-cell">{{Encode(product.ModelName)}}</td>
                  <td><code>{{Encode(product.Gtin)}}</code></td>
                  <td>{{Encode(product.BatchNumber)}}</td>
                  <td>{{Encode(product.ProductionLocation)}}</td>
                  <td class="product-links">
                    <a href="{{Encode(urls.HtmlPath(product.Gtin))}}">HTML</a>
                    <a href="{{Encode(urls.JsonLdPath(product.Gtin))}}">JSON-LD</a>
                    <a href="{{Encode(urls.QrPath(product.Gtin))}}" download="{{Encode(product.Gtin)}}-qr.png">QR</a>
                  </td>
                </tr>
                """);
        }

        if (products.Count == 0)
        {
            rows.AppendLine("<tr><td colspan=\"5\" class=\"empty-state\">Henüz ürün yok. Bir CSV dosyası yükleyerek başlayabilirsin.</td></tr>");
        }

        var body = $$"""
            <main id="main-content" class="page-shell">
              <div class="page-heading home-heading">
                <div>
                  <h1>Ürün pasaportları</h1>
                  <p class="intro">CSV dosyanı yükle, sonuçları incele ve ürün pasaportlarını aç.</p>
                </div>
              </div>
              <section class="card upload-section" aria-labelledby="upload-heading">
                <div>
                  <h2 id="upload-heading">Ürünleri içe aktar</h2>
                  <p class="section-copy">Geçerli satırlar kaydedilir. Hatalı satırların numarası ve nedeni aşağıda gösterilir.</p>
                  <a href="/sample.csv" download="urunler_duzeltilmis_test.csv">Örnek CSV'yi indir</a>
                </div>
                <form id="upload-form" action="/api/products/import" method="post" enctype="multipart/form-data">
                  <label for="csv-file">Ürün dosyası</label>
                  <input id="csv-file" name="file" type="file" accept=".csv,text/csv" required aria-describedby="file-help upload-error">
                  <p id="file-help" class="muted">UTF-8 CSV, en fazla 1 MiB. GTIN'lerin başındaki sıfırları koru.</p>
                  <p id="upload-error" class="error-message" role="alert" hidden></p>
                  <button id="upload-button" class="button" type="submit">CSV'yi yükle</button>
                  <noscript><p>Yükleme sonucu JSON olarak açılır. Ürün listesini görmek için ana sayfaya dönebilirsin.</p></noscript>
                </form>
              </section>
              <section id="import-result" class="card result-section" hidden tabindex="-1" aria-labelledby="result-heading">
                <h2 id="result-heading">Yükleme sonucu</h2>
                <p id="result-status" role="status" aria-live="polite"></p>
                <dl class="import-counts">
                  <div><dt>İşlenen satır</dt><dd id="total-count">0</dd></div>
                  <div><dt>Yeni kayıt</dt><dd id="imported-count">0</dd></div>
                  <div><dt>Değişmeyen</dt><dd id="unchanged-count">0</dd></div>
                  <div><dt>Hatalı satır</dt><dd id="rejected-count">0</dd></div>
                </dl>
                <div id="row-errors-section" hidden>
                  <h3>Düzeltilecek satırlar</h3>
                  <div class="table-wrap" role="region" aria-label="CSV hata raporu" tabindex="0">
                    <table><thead><tr><th scope="col">CSV satırı</th><th scope="col">Hata nedeni</th></tr></thead><tbody id="row-errors"></tbody></table>
                  </div>
                </div>
              </section>
              <section id="catalog-section" class="card catalog-section" aria-labelledby="catalog-heading">
                <div class="catalog-heading">
                  <h2 id="catalog-heading">Kayıtlı ürünler <span id="product-count" class="count-label">({{products.Count}})</span></h2>
                  <button id="refresh-button" class="button button-secondary" type="button">Listeyi yenile</button>
                </div>
                <p id="catalog-error" class="error-message" role="alert" hidden></p>
                <div class="table-wrap" role="region" aria-label="Kayıtlı ürünler" tabindex="0">
                  <table>
                    <thead><tr><th scope="col">Model</th><th scope="col">GTIN</th><th scope="col">Parti</th><th scope="col">Üretim yeri</th><th scope="col">Pasaport</th></tr></thead>
                    <tbody id="product-rows">{{rows}}</tbody>
                  </table>
                </div>
              </section>
            </main>
            """;

        return HtmlLayout.Render("Ürün pasaportları", body, includeAppScript: true);
    }

    private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
}

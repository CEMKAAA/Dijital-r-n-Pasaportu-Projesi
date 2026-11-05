using System.Text.Encodings.Web;

namespace MiniDpp.Api.Web;

public static class HtmlLayout
{
    public static string Render(string title, string body, string extraHead = "", bool includeAppScript = false)
    {
        var script = includeAppScript ? "<script type=\"module\" src=\"/app.js\"></script>" : string.Empty;
        return $$"""
            <!doctype html>
            <html lang="tr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{HtmlEncoder.Default.Encode(title)}} · Mini DPP</title>
              <link rel="icon" href="data:,">
              <link rel="stylesheet" href="/styles.css">
              {{extraHead}}
            </head>
            <body>
              <a class="skip-link" href="#main-content">İçeriğe geç</a>
              <header class="site-header">
                <div class="header-inner"><a class="brand" href="/">Mini DPP</a><span class="header-caption">Dijital ürün pasaportu</span></div>
              </header>
              {{body}}
              <footer class="site-footer">Mini Dijital Ürün Pasaportu</footer>
              {{script}}
            </body>
            </html>
            """;
    }
}

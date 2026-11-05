using System.Text.Json;
using MiniDpp.Api.Passports;

namespace MiniDpp.Api.Endpoints;

public static class DppEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static void MapDppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dpp/{gtin}", HtmlAsync).WithName("ProductPassport");
        endpoints.MapGet("/dpp/{gtin}.jsonld", JsonLdAsync).WithName("ProductPassportJsonLd");
        endpoints.MapGet("/dpp/{gtin}/qr.png", QrAsync).WithName("ProductPassportQr");
    }

    private static async Task<IResult> HtmlAsync(
        string gtin, HttpContext context, PassportService service,
        PassportUrlBuilder urls, CancellationToken cancellationToken)
    {
        SetResponseHeaders(context);
        var passport = await service.GetAsync(gtin, IsAuthorized(context), cancellationToken);

        return passport is null
            ? Results.Content(PassportHtmlRenderer.NotFound(), "text/html; charset=utf-8",
                statusCode: StatusCodes.Status404NotFound)
            : Results.Content(PassportHtmlRenderer.Render(passport, urls), "text/html; charset=utf-8");
    }

    private static async Task<IResult> JsonLdAsync(
        string gtin, HttpContext context, PassportService service,
        PassportUrlBuilder urls, CancellationToken cancellationToken)
    {
        SetResponseHeaders(context);
        var passport = await service.GetAsync(gtin, IsAuthorized(context), cancellationToken);

        return passport is null
            ? MissingProduct()
            : Results.Json(PassportJsonLdRenderer.Render(passport, urls), JsonOptions,
                contentType: "application/ld+json; charset=utf-8");
    }

    private static async Task<IResult> QrAsync(
        string gtin, HttpContext context, PassportService service,
        PassportUrlBuilder urls, CancellationToken cancellationToken)
    {
        SetResponseHeaders(context);
        var passport = await service.GetAsync(gtin, includeCarbon: false, cancellationToken);

        return passport is null
            ? MissingProduct()
            : Results.File(QrCodeService.CreatePng(urls.HtmlUrl(passport.Gtin)), "image/png");
    }

    private static bool IsAuthorized(HttpContext context) =>
        context.Request.Query["rol"].ToString() == "yetkili";

    private static void SetResponseHeaders(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    private static IResult MissingProduct() => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Ürün bulunamadı", detail: "Bu GTIN için kayıtlı bir ürün pasaportu bulunamadı.");
}

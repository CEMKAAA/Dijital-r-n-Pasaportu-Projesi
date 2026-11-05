using MiniDpp.Api.Passports;
using MiniDpp.Api.Web;

namespace MiniDpp.Api.Endpoints;

public static class HomeEndpoints
{
    public static void MapHomeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", async (
            HttpContext context, PassportService service, PassportUrlBuilder urls,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var products = await service.GetSummariesAsync(cancellationToken);
            return Results.Content(HomePageRenderer.Render(products, urls), "text/html; charset=utf-8");
        });

        endpoints.MapGet("/api/products", async (
            HttpContext context, PassportService service, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await service.GetSummariesAsync(cancellationToken));
        });

        endpoints.MapGet("/sample.csv", () => Results.File(
            Path.Combine(AppContext.BaseDirectory, "SampleData", "urunler.csv"),
            "text/csv; charset=utf-8", "urunler_duzeltilmis_test.csv"));
    }
}

using System.Text;
using MiniDpp.Api.Importing;

namespace MiniDpp.Api.Endpoints;

public static class ImportEndpoints
{
    public const int MaxUploadBytes = 1024 * 1024;

    public static void MapImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/products/import", ImportAsync)
            // Hesap/cookie kimlik doğrulaması kullanmayan yerel demo endpoint'i.
            .DisableAntiforgery()
            .WithName("ImportProducts");
    }

    private static async Task<IResult> ImportAsync(
        IFormFile? file, CsvImportService service, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "CSV dosyası gerekli", detail: "Dosyayı 'file' adlı form alanında gönderin.");
        }

        if (file.Length > MaxUploadBytes)
        {
            return Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Dosya çok büyük", detail: "CSV dosyası en fazla 1 MiB olabilir.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true),
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            var report = await service.ImportAsync(text, cancellationToken);

            return Results.Ok(report);
        }
        catch (DecoderFallbackException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dosya kodlaması geçersiz", detail: "CSV dosyasını UTF-8 kodlamasıyla gönderin.");
        }
        catch (InvalidCsvException exception)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "CSV biçimi geçersiz", detail: exception.Message);
        }
    }
}

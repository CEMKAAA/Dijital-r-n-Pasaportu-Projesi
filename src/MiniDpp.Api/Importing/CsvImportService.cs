using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Models;

namespace MiniDpp.Api.Importing;

public sealed class CsvImportService
{
    private readonly AppDbContext _database;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(AppDbContext database, ILogger<CsvImportService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<CsvImportReport> ImportAsync(string text, CancellationToken cancellationToken = default)
    {
        // Başlık gibi dosyanın tamamına ait sorunlar, kayıt yazılmadan tespit edilir.
        var rows = CsvProductReader.Read(text, cancellationToken);
        var errors = new List<CsvImportRowError>();
        var products = new List<ImportedProduct>();
        var importedCount = 0;
        var unchangedCount = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.Product is null)
            {
                errors.Add(new CsvImportRowError(row.RowNumber, row.Errors));
                continue;
            }

            var validation = ProductRowValidator.Validate(row.Product);
            if (validation.Product is not { } incoming)
            {
                errors.Add(new CsvImportRowError(row.RowNumber, validation.Errors));
                continue;
            }

            try
            {
                // ÖNEMLİ: Transaction bütün dosya için değil, tek satır içindir.
                // Model, parti ve ürün birlikte kaydedilir veya birlikte geri alınır.
                await using var transaction = await _database.Database.BeginTransactionAsync(cancellationToken);

                var existing = await _database.Products
                    .Include(product => product.ProductBatch)
                    .ThenInclude(batch => batch.ProductModel)
                    .SingleOrDefaultAsync(product => product.Gtin == incoming.Gtin, cancellationToken);

                if (existing is not null)
                {
                    if (HasSameValues(existing, incoming))
                    {
                        unchangedCount++;
                        products.Add(ToImportedProduct(incoming, "unchanged"));
                    }
                    else
                    {
                        errors.Add(new CsvImportRowError(row.RowNumber,
                            ["Bu GTIN farklı bilgilerle zaten kayıtlı. Mevcut kayıt değiştirilmedi."]));
                    }

                    continue;
                }

                var model = await _database.ProductModels
                    .SingleOrDefaultAsync(item => item.Name == incoming.ModelName, cancellationToken)
                    ?? new ProductModel { Name = incoming.ModelName };

                var batch = model.Id == 0 ? null : await _database.ProductBatches
                    .SingleOrDefaultAsync(item => item.ProductModelId == model.Id
                        && item.BatchNumber == incoming.BatchNumber, cancellationToken);

                batch ??= new ProductBatch
                {
                    BatchNumber = incoming.BatchNumber,
                    ProductModel = model
                };

                _database.Products.Add(new Product
                {
                    Gtin = incoming.Gtin,
                    CottonPercentage = incoming.CottonPercentage,
                    ElastanePercentage = incoming.ElastanePercentage,
                    ProductionLocation = incoming.ProductionLocation,
                    CarbonKgCo2e = incoming.CarbonKgCo2e,
                    ProductBatch = batch
                });

                await _database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                importedCount++;
                products.Add(ToImportedProduct(incoming, "created"));
            }
            catch (Exception exception) when (exception is DbUpdateException or SqliteException)
            {
                _logger.LogWarning(exception, "CSV satırı {RowNumber} kaydedilemedi.", row.RowNumber);
                errors.Add(new CsvImportRowError(row.RowNumber,
                    ["Bu satır veritabanına kaydedilemedi; diğer satırlar işlenmeye devam etti."]));
            }
            finally
            {
                // Geri alınan satırın nesneleri sonraki SaveChanges'e taşınmaz.
                _database.ChangeTracker.Clear();
            }
        }

        return new CsvImportReport(
            rows.Count, importedCount, unchangedCount, errors.Count, errors, products);
    }

    private static bool HasSameValues(Product existing, ValidatedProduct incoming) =>
        existing.ProductBatch.ProductModel.Name == incoming.ModelName
        && existing.ProductBatch.BatchNumber == incoming.BatchNumber
        && existing.CottonPercentage == incoming.CottonPercentage
        && existing.ElastanePercentage == incoming.ElastanePercentage
        && existing.ProductionLocation == incoming.ProductionLocation
        && existing.CarbonKgCo2e == incoming.CarbonKgCo2e;

    private static ImportedProduct ToImportedProduct(ValidatedProduct product, string status) =>
        new(product.Gtin, product.ModelName, product.BatchNumber, status);
}

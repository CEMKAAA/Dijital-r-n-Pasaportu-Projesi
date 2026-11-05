using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Importing;

namespace MiniDpp.Api.Passports;

public sealed class PassportService
{
    private readonly AppDbContext _database;

    public PassportService(AppDbContext database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<ProductSummary>> GetSummariesAsync(
        CancellationToken cancellationToken = default) =>
        await _database.Products.AsNoTracking()
            .OrderBy(product => product.ProductBatch.ProductModel.Name)
            .ThenBy(product => product.Gtin)
            .Select(product => new ProductSummary(
                product.Gtin, product.ProductBatch.ProductModel.Name,
                product.ProductBatch.BatchNumber, product.ProductionLocation))
            .ToListAsync(cancellationToken);

    public async Task<ProductPassport?> GetAsync(
        string gtin, bool includeCarbon, CancellationToken cancellationToken = default)
    {
        if (!GtinValidator.IsValid(gtin))
        {
            return null;
        }

        // ÖNEMLİ: Entity doğrudan döndürülmez. Tüketici için karbon değeri
        // sunum nesnesine hiç aktarılmaz; iki çıktı da bu seçimi kullanır.
        return await _database.Products.AsNoTracking()
            .Where(product => product.Gtin == gtin)
            .Select(product => new ProductPassport(
                product.Gtin,
                product.ProductBatch.ProductModel.Name,
                product.ProductBatch.BatchNumber,
                product.CottonPercentage,
                product.ElastanePercentage,
                product.ProductionLocation,
                includeCarbon ? product.CarbonKgCo2e : (decimal?)null))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Data;
using MiniDpp.Api.Models;

namespace MiniDpp.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task ProductRoundTripPreservesGtinAndRelatedModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.MigrateAsync();

        var model = new ProductModel { Name = "Slim Denim 501" };
        var batch = new ProductBatch { BatchNumber = "B2027-01", ProductModel = model };
        db.Products.Add(CreateProduct(batch));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var product = await db.Products
            .Include(item => item.ProductBatch)
            .ThenInclude(item => item.ProductModel)
            .SingleAsync();

        Assert.Equal("08690000000012", product.Gtin);
        Assert.Equal("B2027-01", product.ProductBatch.BatchNumber);
        Assert.Equal("Slim Denim 501", product.ProductBatch.ProductModel.Name);
        Assert.Equal(12.4m, product.CarbonKgCo2e);
        Assert.Equal(1, await db.ProductModels.CountAsync());
        Assert.Equal(1, await db.ProductBatches.CountAsync());
    }

    [Fact]
    public async Task DuplicateGtinIsRejectedBySqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.MigrateAsync();

        var batch = new ProductBatch
        {
            BatchNumber = "B2027-01",
            ProductModel = new ProductModel { Name = "Slim Denim 501" }
        };
        db.Products.Add(CreateProduct(batch));
        await db.SaveChangesAsync();

        db.Products.Add(CreateProduct(batch));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task ModelWithExistingBatchCannotBeDeleted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.MigrateAsync();

        db.ProductBatches.Add(new ProductBatch
        {
            BatchNumber = "B2027-01",
            ProductModel = new ProductModel { Name = "Slim Denim 501" }
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<SqliteException>(() => db.ProductModels.ExecuteDeleteAsync());
        Assert.Equal(1, await db.ProductModels.CountAsync());
    }

    [Fact]
    public async Task BatchNumberIsUniqueWithinItsModel()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.MigrateAsync();

        var firstModel = new ProductModel { Name = "Slim Denim 501" };
        db.ProductBatches.AddRange(
            new ProductBatch { BatchNumber = "B2027-01", ProductModel = firstModel },
            new ProductBatch
            {
                BatchNumber = "B2027-01",
                ProductModel = new ProductModel { Name = "Regular Denim 502" }
            });
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.ProductBatches.CountAsync());

        db.ProductBatches.Add(new ProductBatch
        {
            BatchNumber = "B2027-01",
            ProductModel = firstModel
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(2, await db.ProductBatches.CountAsync());
    }

    [Fact]
    public async Task ReopeningDatabaseAndReapplyingMigrationsPreservesData()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"mini-dpp-test-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False;Foreign Keys=True")
            .Options;

        try
        {
            await using (var firstContext = new AppDbContext(options))
            {
                await firstContext.Database.MigrateAsync();
                firstContext.Products.Add(CreateProduct(new ProductBatch
                {
                    BatchNumber = "B2027-01",
                    ProductModel = new ProductModel { Name = "Slim Denim 501" }
                }));
                await firstContext.SaveChangesAsync();
            }

            await using var reopenedContext = new AppDbContext(options);
            await reopenedContext.Database.MigrateAsync();

            Assert.Equal("08690000000012", (await reopenedContext.Products.SingleAsync()).Gtin);
            Assert.Single(await reopenedContext.Database.GetAppliedMigrationsAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static AppDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    private static Product CreateProduct(ProductBatch batch) => new()
    {
        Gtin = "08690000000012",
        CottonPercentage = 98m,
        ElastanePercentage = 2m,
        ProductionLocation = "Denizli",
        CarbonKgCo2e = 12.4m,
        ProductBatch = batch
    };
}

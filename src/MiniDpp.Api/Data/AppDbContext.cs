using Microsoft.EntityFrameworkCore;
using MiniDpp.Api.Models;

namespace MiniDpp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ÖNEMLİ: /dpp/{gtin} her zaman tek bir ürün kaydını tanımlar.
        modelBuilder.Entity<Product>()
            .HasIndex(product => product.Gtin)
            .IsUnique();

        modelBuilder.Entity<ProductModel>()
            .HasIndex(model => model.Name)
            .IsUnique();

        // Parti numarası, ait olduğu model içerisinde benzersizdir.
        modelBuilder.Entity<ProductBatch>()
            .HasIndex(batch => new { batch.ProductModelId, batch.BatchNumber })
            .IsUnique();

        modelBuilder.Entity<ProductBatch>()
            .HasOne(batch => batch.ProductModel)
            .WithMany()
            .HasForeignKey(batch => batch.ProductModelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(product => product.ProductBatch)
            .WithMany()
            .HasForeignKey(product => product.ProductBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

namespace MiniDpp.Api.Models;

public class Product
{
    public int Id { get; set; }

    public string Gtin { get; set; } = string.Empty;

    public decimal CottonPercentage { get; set; }

    public decimal ElastanePercentage { get; set; }

    public string ProductionLocation { get; set; } = string.Empty;

    public decimal CarbonKgCo2e { get; set; }

    public int ProductBatchId { get; set; }

    public required ProductBatch ProductBatch { get; set; }
}
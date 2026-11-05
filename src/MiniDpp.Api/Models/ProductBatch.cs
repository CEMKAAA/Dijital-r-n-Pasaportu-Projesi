namespace MiniDpp.Api.Models;

public class ProductBatch
{
     public int Id {get; set;}
     public string BatchNumber { get; set; } = string.Empty;

     public int ProductModelId {get; set;}
     public required ProductModel ProductModel {get; set;}
}
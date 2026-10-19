namespace StockFlow.Api.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";
    public Guid? PreferredSupplierId { get; set; }
    public Supplier? PreferredSupplier { get; set; }
    public int CurrentStock { get; set; }
}
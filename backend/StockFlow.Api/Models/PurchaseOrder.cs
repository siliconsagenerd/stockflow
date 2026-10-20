namespace StockFlow.Api.Models;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = "";
    public Guid PurchaseRequestId { get; set; }
    public PurchaseRequest PurchaseRequest { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public string Status { get; set; } = "Open"; // Open, PartiallyReceived, Completed
    public DateTime CreatedAt { get; set; }
}

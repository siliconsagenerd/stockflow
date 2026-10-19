namespace StockFlow.Api.Models;

public class GoodsReceipt
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public int Quantity { get; set; }
    public Guid ReceivedById { get; set; }
    public DemoUser ReceivedBy { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
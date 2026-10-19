namespace StockFlow.Api.Models;

public class PurchaseRequest
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public Guid RequesterId { get; set; }
    public DemoUser Requester { get; set; } = null!;
    public string Status { get; set; } = "Requested"; // Requested, Approved, Rejected
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedById { get; set; }
    public DemoUser? DecidedBy { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
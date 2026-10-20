using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Models;

public class PurchaseRequest
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public DemoWorkspace Workspace { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public string? Justification { get; set; }
    public Guid RequesterId { get; set; }
    public DemoUser Requester { get; set; } = null!;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedById { get; set; }
    public DemoUser? DecidedBy { get; set; }
    public string? RejectionReason { get; set; }
    [ConcurrencyCheck]
    public uint Xmin { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}

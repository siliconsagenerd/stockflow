namespace StockFlow.Api.Models;
public class StockMovement
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public DemoWorkspace Workspace { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

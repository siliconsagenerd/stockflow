namespace StockFlow.Api.Models;
public class StockBalance
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public DemoWorkspace Workspace { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int Quantity { get; set; }
}

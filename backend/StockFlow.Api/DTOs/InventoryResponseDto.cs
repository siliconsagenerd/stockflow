namespace StockFlow.Api.DTOs;
public record InventoryBalanceDto(
    Guid ProductId, string ProductName, string ProductSku, string ProductUnit,
    Guid WarehouseId, string WarehouseName, int Quantity);
public record StockMovementDto(
    Guid Id, Guid ProductId, string ProductName, Guid WarehouseId, string WarehouseName,
    int Delta, string Reason, Guid? ReferenceId, DateTime CreatedAt, Guid WorkspaceId);

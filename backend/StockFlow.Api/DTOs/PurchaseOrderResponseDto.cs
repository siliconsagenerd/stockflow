namespace StockFlow.Api.DTOs;

public record PurchaseOrderResponseDto(
    Guid Id,
    string Reference,
    Guid PurchaseRequestId,
    Guid SupplierId,
    string SupplierName,
    Guid WarehouseId,
    string WarehouseName,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int OrderedQuantity,
    int ReceivedQuantity,
    string Status,
    DateTime CreatedAt);

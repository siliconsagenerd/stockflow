namespace StockFlow.Api.DTOs;
public record ReceiptResponseDto(
    Guid Id,
    Guid PurchaseOrderId,
    Guid WorkspaceId,
    int Quantity,
    Guid ReceivedById,
    string ReceivedByName,
    DateTime ReceivedAt,
    string IdempotencyKey,
    int OrderReceivedQuantity,
    string OrderStatus);

namespace StockFlow.Api.DTOs;

public record RequestResponseDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ProductUnit,
    int Quantity,
    string? Justification,
    string Status,
    Guid RequesterId,
    string RequesterName,
    DateTime CreatedAt,
    Guid WorkspaceId);
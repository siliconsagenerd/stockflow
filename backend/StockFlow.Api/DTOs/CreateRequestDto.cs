namespace StockFlow.Api.DTOs;

public record CreateRequestDto(Guid ProductId, int Quantity, string? Justification);
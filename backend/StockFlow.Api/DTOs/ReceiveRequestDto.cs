namespace StockFlow.Api.DTOs;
public record ReceiveRequestDto(int Quantity, string IdempotencyKey);

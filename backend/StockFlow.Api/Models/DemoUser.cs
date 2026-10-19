namespace StockFlow.Api.Models;

public class DemoUser
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Employee, Manager, Warehouse
}
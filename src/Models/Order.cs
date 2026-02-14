namespace SizzlingHotProducts.Models;

public class Order
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public List<OrderEntry> Entries { get; init; } = [];
    public required DateOnly Date { get; init; }
    public required OrderStatus Status { get; init; }
}

public class OrderEntry
{
    public required string Id { get; init; }
    public required int Quantity { get; init; }
}

public enum OrderStatus
{
    Completed,
    Cancelled
}

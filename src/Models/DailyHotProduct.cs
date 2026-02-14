namespace SizzlingHotProducts.Models;

public class DailyHotProduct
{
    public required DateOnly Date { get; init; }
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int SalesCount { get; init; }
}

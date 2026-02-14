using System.Text.Json;
using System.Text.Json.Serialization;
using SizzlingHotProducts.Models;

namespace SizzlingHotProducts.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _inputFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderRepository(string inputFolder)
    {
        _inputFolder = inputFolder;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public IReadOnlyList<Order> GetOrders()
    {
        var path = Path.Combine(_inputFolder, "orders.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Order>>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize orders.json");
    }

    public IReadOnlyList<Product> GetProducts()
    {
        var path = Path.Combine(_inputFolder, "products.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Product>>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize products.json");
    }
}

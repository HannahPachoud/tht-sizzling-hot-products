using SizzlingHotProducts.Models;

namespace SizzlingHotProducts.Repositories;

public interface IOrderRepository
{
    IReadOnlyList<Order> GetOrders();
    IReadOnlyList<Product> GetProducts();
}

using SizzlingHotProducts.Models;
using SizzlingHotProducts.Repositories;

namespace SizzlingHotProducts.Services;

public class SalesService : ISalesService
{
    private readonly IOrderRepository _orderRepository;
    private readonly DateOnly _today;

    public SalesService(IOrderRepository orderRepository, DateOnly today)
    {
        _orderRepository = orderRepository;
        _today = today;
    }

    public IReadOnlyList<DailyHotProduct> GetDailyHotProducts()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<DailyHotProduct> GetHotProductForLastThreeDays()
    {
        throw new NotImplementedException();
    }
}
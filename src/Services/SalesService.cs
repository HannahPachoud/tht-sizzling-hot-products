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
        var flattenedOrders = FlattenOrders(_orderRepository.GetOrders());
        var cancelledOrders = RemoveCancelledOrders(_orderRepository.GetOrders(), flattenedOrders);
        var deduplicatedOrders = RemoveDuplicates(cancelledOrders);
   
        var salesPerProductPerDay = CountSalesPerProductPerDay(deduplicatedOrders);
        var dailyHotProducts = PickDailyWinners(salesPerProductPerDay);

        return dailyHotProducts;
    }

    public IReadOnlyList<DailyHotProduct> GetHotProductForLastThreeDays()
    {
        throw new NotImplementedException();
    }

    private record FlattenedOrder(string CustomerId, string ProductId, DateOnly Date, string OrderId);
    private record ProductSalesCount(string ProductId, DateOnly Date, int SalesCount);

    private List<ProductSalesCount> CountSalesPerProductPerDay(List<FlattenedOrder> orders)
    {
        return orders
            .GroupBy(o => (o.ProductId, o.Date))
            .Select(group => new ProductSalesCount(group.Key.ProductId, group.Key.Date, group.Count()))
            .ToList();
    }

    private List<DailyHotProduct> PickDailyWinners(List<ProductSalesCount> salesCounts)
    
    {
       var productNameLookup = _orderRepository.GetProducts().ToDictionary(p => p.Id, p => p.Name);

        return salesCounts
            .GroupBy(s => s.Date)
            .Select(dateGroup => dateGroup
                .OrderByDescending(s => s.SalesCount)
                .ThenBy(s => productNameLookup[s.ProductId])
                .First())
            .Select(winner => new DailyHotProduct
            {
                Date = winner.Date,
                ProductId = winner.ProductId,
                ProductName = productNameLookup[winner.ProductId],
                SalesCount = winner.SalesCount
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private List<FlattenedOrder> FlattenOrders(IReadOnlyList<Order> orders)
    {
        return orders
            .Where(order => order.Status == OrderStatus.Completed)
            .SelectMany(order => order.Entries.Select(entry =>
                new FlattenedOrder(order.CustomerId, entry.Id, order.Date, order.OrderId)))
            .ToList();
    }

    private List<FlattenedOrder> RemoveCancelledOrders(IReadOnlyList<Order> orders, List<FlattenedOrder> flattenedOrders)
    {
        var cancelledOrderIds = orders
            .Where(order => order.Status == OrderStatus.Cancelled)
            .Select(order => order.OrderId)
            .ToHashSet();

        return flattenedOrders
            .Where(fo => !cancelledOrderIds.Contains(fo.OrderId))
            .ToList();
    }

    private List<FlattenedOrder> RemoveDuplicates(List<FlattenedOrder> flattenedOrders)
    {
        return flattenedOrders
            .GroupBy(fo => (fo.CustomerId, fo.ProductId, fo.Date))
            .Select(group => group.First())
            .ToList();
    }
}
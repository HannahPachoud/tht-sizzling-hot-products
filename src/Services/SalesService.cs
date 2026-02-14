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
        var salesPerProductPerDay = GetSalesPerProductPerDay();
        return PickDailyWinners(salesPerProductPerDay);
    }

    public DailyHotProduct GetHotProductForLastThreeDays()
    {
        var dateWindowStart = _today.AddDays(-2);
        var salesPerProductPerDay = GetSalesPerProductPerDay();
        return PickWinnerForDateWindow(salesPerProductPerDay, dateWindowStart);
    }

    private List<ProductSalesCount> GetSalesPerProductPerDay()
    {
        var orders = _orderRepository.GetOrders();
        var flattenedOrders = FlattenOrders(orders);
        var withoutCancelled = RemoveCancelledOrders(orders, flattenedOrders);
        var deduplicated = RemoveDuplicates(withoutCancelled);
        return deduplicated
            .GroupBy(o => (o.ProductId, o.Date))
            .Select(group => new ProductSalesCount(group.Key.ProductId, group.Key.Date, group.Count()))
            .ToList();
    }

    private record FlattenedOrder(string CustomerId, string ProductId, DateOnly Date, string OrderId);
    private record ProductSalesCount(string ProductId, DateOnly Date, int SalesCount);

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

    private DailyHotProduct PickWinnerForDateWindow(List<ProductSalesCount> salesCounts, DateOnly dateWindowStart)
    {
        var productNameLookup = _orderRepository.GetProducts().ToDictionary(p => p.Id, p => p.Name);

        var winner = salesCounts
            .Where(sale => sale.Date >= dateWindowStart && sale.Date <= _today)
            .GroupBy(sale => sale.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                TotalSales = group.Sum(s => s.SalesCount)
            })
            .OrderByDescending(x => x.TotalSales)
            .ThenBy(x => productNameLookup[x.ProductId])
            .First();

        return new DailyHotProduct
        {
            Date = _today, //using todays date, as as of today this is the winner of the last x days. Up to front end if it wants to use it or not.
            ProductId = winner.ProductId,
            ProductName = productNameLookup[winner.ProductId],
            SalesCount = winner.TotalSales
        };
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
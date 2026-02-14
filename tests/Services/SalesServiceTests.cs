using Moq;
using SizzlingHotProducts.Models;
using SizzlingHotProducts.Repositories;
using SizzlingHotProducts.Services;

namespace SizzlingHotProducts.Tests.Services;

public class SalesServiceTests
{
    private readonly DateOnly _today = new(2021, 7, 21);

    private readonly List<Product> _products =
    [
        new() { Id = "P1", Name = "Ezy Storage 37L Flexi Laundry Basket - White" },
        new() { Id = "P2", Name = "Aandleford Black Seaford Post Mounted Letterbox" },
        new() { Id = "P3", Name = "Coolaroo 5.4m Square Graphite Premium Shade Sail Kit" },
        new() { Id = "P4", Name = "Ozito 80W Soldering Iron" },
        new() { Id = "P5", Name = "Richgro 25L All Purpose Garden Soil Mix" },
        new() { Id = "P6", Name = "Arlec 160W Crystalline Solar Foldable Charging Kit" }
    ];

    private SalesService CreateService(List<Order> orders)
    {
        var mockOrderRepo = new Mock<IOrderRepository>();
        mockOrderRepo.Setup(r => r.GetOrders()).Returns(orders);
        mockOrderRepo.Setup(r => r.GetProducts()).Returns(_products);
        return new SalesService(mockOrderRepo.Object, _today);
    }

    [Fact]
    public void GetDailyHotProducts_ReturnsProductWithMostSales()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O3", CustomerId = "C3", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        Assert.Equal("P1", result[0].ProductId);
        Assert.Equal(2, result[0].SalesCount);
    }

    [Fact]
    public void GetDailyHotProducts_QuantityDoesNotAffectSalesCount()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 50 }] }
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        Assert.Equal(1, result[0].SalesCount);
    }

    [Fact]
    public void GetDailyHotProducts_DuplicateOrdersSameCustomerSameDayCountOnce()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        Assert.Equal("P1", result[0].ProductId);
        Assert.Equal(1, result[0].SalesCount); 
    }

    [Fact]
    public void GetDailyHotProducts_CancelledOrdersRemoveSalesFromTotal()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            //cancel P2 (Aandleford...), as it would win in a tie against P1 (Ezy Storage...) as it's first alphabetically
            new() { OrderId = "O2", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Cancelled, CustomerId = "C1" }, 
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        Assert.Equal("P1", result[0].ProductId);
        Assert.Equal(1, result[0].SalesCount);
    }

    [Fact]
    public void GetDailyHotProducts_TiedSalesCountBreaksTieAlphabetically()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        // P2 = "Aandleford..." comes before P1 = "Ezy Storage..." alphabetically
        Assert.Equal("P2", result[0].ProductId);
    }

    [Fact]
    public void GetDailyHotProducts_MultipleDaysReturnsSortedByDate()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O3", CustomerId = "C3", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P3", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2021, 7, 19), result[0].Date);
        Assert.Equal(new DateOnly(2021, 7, 20), result[1].Date);
        Assert.Equal(new DateOnly(2021, 7, 21), result[2].Date);
    }

    [Fact]
    public void GetDailyHotProducts_MultipleEntriesInOneOrderCountAsSeparateProducts()
    {
        // One order with P1 and P2, plus a second order for P1 to break the tie
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed,
                Entries = [new() { Id = "P1", Quantity = 1 }, new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed,
                Entries = [new() { Id = "P1", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        // P1 has 2 sales (from order O1 and O2), P2 has 1 sale (from O1 only)
        // Proves the multi-entry order counted P2 as a separate sale
        Assert.Equal("P1", result[0].ProductId);
        Assert.Equal(2, result[0].SalesCount);
    }

    [Fact]
    public void GetDailyHotProducts_NoOrders_ReturnsEmptyList()
    {
        var orders = new List<Order>();

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Empty(result);
    }

    [Fact]
    public void GetDailyHotProducts_AllOrdersCancelled_ReturnsEmptyList()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Cancelled },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Empty(result);
    }

    [Fact]
    public void GetDailyHotProducts_SameCustomerSameProductDifferentDays_CountsSeparately()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C1", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Equal(2, result.Count);
        Assert.Equal("P1", result[0].ProductId);
        Assert.Equal(1, result[0].SalesCount);
        Assert.Equal("P1", result[1].ProductId);
        Assert.Equal(1, result[1].SalesCount);
    }

    [Fact]
    public void GetDailyHotProducts_CancelledOrderWithNoMatchingCompleted_DoesNotBreak()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O999", CustomerId = "C2", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Cancelled },
        };

        var result = CreateService(orders).GetDailyHotProducts();

        Assert.Single(result);
        Assert.Equal("P1", result[0].ProductId);
    }

    // GetHotProductForLastThreeDays tests

    [Fact]
    public void GetHotProductForLastThreeDays_ReturnsProductWithMostSalesAcrossPeriod()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O3", CustomerId = "C3", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O4", CustomerId = "C4", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetHotProductForLastThreeDays();

        Assert.Equal("P1", result.ProductId);
        Assert.Equal(3, result.SalesCount);
    }

    [Fact]
    public void GetHotProductForLastThreeDays_ExcludesOrdersOutsideThreeDayWindow()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 17), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 18), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O3", CustomerId = "C3", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetHotProductForLastThreeDays();

        Assert.Equal("P1", result.ProductId);
        Assert.Equal(1, result.SalesCount);
    }

    [Fact]
    public void GetHotProductForLastThreeDays_SumsAcrossDaysNotPerDay()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O3", CustomerId = "C3", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O4", CustomerId = "C4", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 1 }] },
            new() { OrderId = "O5", CustomerId = "C5", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetHotProductForLastThreeDays();

        Assert.Equal("P1", result.ProductId);
        Assert.Equal(3, result.SalesCount);
    }

    [Fact]
    public void GetHotProductForLastThreeDays_BreakTiedSalesAlphabetically()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O1", CustomerId = "C1", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O2", CustomerId = "C2", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P6", Quantity = 1 }] },
        };

        var result = CreateService(orders).GetHotProductForLastThreeDays();

        // Arlec (P6) before Ezy (P1) alphabetically
        Assert.Equal("P6", result.ProductId);
    }

    [Fact]
    public void GetHotProductForLastThreeDays_MatchesExpectedOutcome()
    {
        var orders = new List<Order>
        {
            new() { OrderId = "O10", CustomerId = "C1", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O20", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O30", CustomerId = "C2", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O31", CustomerId = "C3", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }, new() { Id = "P1", Quantity = 2 }] },
            new() { OrderId = "O32", CustomerId = "C32", Date = new DateOnly(2021, 7, 19), Status = OrderStatus.Completed, Entries = [new() { Id = "P2", Quantity = 1 }] },
            new() { OrderId = "O30", CustomerId = "C2", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Cancelled },
            new() { OrderId = "O40", CustomerId = "C3", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 2 }] },
            new() { OrderId = "O60", CustomerId = "C3", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 2 }, new() { Id = "P1", Quantity = 2 }] },
            new() { OrderId = "O70", CustomerId = "C4", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P5", Quantity = 2 }] },
            new() { OrderId = "O80", CustomerId = "C5", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 2 }] },
            new() { OrderId = "O81", CustomerId = "C5", Date = new DateOnly(2021, 7, 20), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 10 }] },
            new() { OrderId = "O90", CustomerId = "C5", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P1", Quantity = 1 }] },
            new() { OrderId = "O100", CustomerId = "C3", Date = new DateOnly(2021, 7, 21), Status = OrderStatus.Completed, Entries = [new() { Id = "P4", Quantity = 1 }, new() { Id = "P6", Quantity = 3 }] },
        };

        var result = CreateService(orders).GetHotProductForLastThreeDays();

        Assert.Equal("P1", result.ProductId);
        Assert.Equal("Ezy Storage 37L Flexi Laundry Basket - White", result.ProductName);
    }
}

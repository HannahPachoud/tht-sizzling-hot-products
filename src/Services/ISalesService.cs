using SizzlingHotProducts.Models;

namespace SizzlingHotProducts.Services;

public interface ISalesService
{
    /// <summary>
    /// Returns the top sizzling hot product for each day.
    /// </summary>
    IReadOnlyList<DailyHotProduct> GetDailyHotProducts();

    /// <summary>
    /// Returns the top sizzling hot product for each of the last 3 days.
    /// </summary>
    IReadOnlyList<DailyHotProduct> GetHotProductForLastThreeDays();
}

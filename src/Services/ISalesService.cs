using SizzlingHotProducts.Models;

namespace SizzlingHotProducts.Services;

public interface ISalesService
{
    /// <summary>
    /// Returns the top sizzling hot product for each day.
    /// </summary>
    IReadOnlyList<DailyHotProduct> GetDailyHotProducts();

    /// <summary>
    /// Returns the single top sizzling hot product over the last 3 days.
    /// </summary>
    DailyHotProduct GetHotProductForLastThreeDays();
}

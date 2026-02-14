using Microsoft.AspNetCore.Mvc;
using SizzlingHotProducts.Services;

namespace SizzlingHotProducts.Controllers;

[ApiController]
[Route("sizzling-hot")]
public class SizzlingHotController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SizzlingHotController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet("daily")]
    public IActionResult GetDailyHotProducts()
    {
        var results = _salesService.GetDailyHotProducts();
        return Ok(results);
    }

    [HttpGet("last-3-days")]
    public IActionResult GetHotProductLastThreeDays()
    {
        var result = _salesService.GetHotProductForLastThreeDays();
        return Ok(result);
    }
}

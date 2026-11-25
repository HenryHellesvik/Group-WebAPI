using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService service) : ControllerBase
{
    // GET /api/analytics/price-suggestion?categoryId=2&condition=Good
    [HttpGet("price-suggestion")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceSuggestionDto>> GetPriceSuggestion([FromQuery] PriceSuggestionQueryDto query) =>
        Ok(await service.GetPriceSuggestionAsync(query.CategoryId!.Value, query.Condition!.Value));

    // GET /api/analytics/categories/2/stats
    [HttpGet("categories/{id:int}/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryStatsDto>> GetCategoryStats(int id) =>
        Ok(await service.GetCategoryStatsAsync(id));

    // GET /api/analytics/overview
    [HttpGet("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryStatsDto>>> GetOverview() =>
        Ok(await service.GetOverviewAsync());

    // GET /api/analytics/listings/5/price-history
    [HttpGet("listings/{id:int}/price-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceHistoryDto>> GetPriceHistory(int id) =>
        Ok(await service.GetPriceHistoryAsync(id));
}
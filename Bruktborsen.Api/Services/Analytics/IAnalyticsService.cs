using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services;

public interface IAnalyticsService
{
    Task<PriceSuggestionDto> GetPriceSuggestionAsync(int categoryId, Condition condition);
    Task<PriceAssessment> AssessPriceAsync(int categoryId, Condition condition, int price);
    Task<CategoryStatsDto> GetCategoryStatsAsync(int categoryId);
    Task<List<CategoryStatsDto>> GetOverviewAsync();
    Task<PriceHistoryDto> GetPriceHistoryAsync(int listingId);
}
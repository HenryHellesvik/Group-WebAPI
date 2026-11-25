using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class AnalyticsService(IAnalyticsRepository analytics, ICategoryRepository categories) : IAnalyticsService
{
    public async Task<PriceSuggestionDto> GetPriceSuggestionAsync(int categoryId, Condition condition)
    {
        await GetCategoryOrThrowAsync(categoryId);

        var sold = await analytics.GetSoldListingsAsync(categoryId);
        var suggested = PriceCalculator.SuggestPrice(sold, condition);

        return new PriceSuggestionDto
        {
            CategoryId = categoryId,
            Condition = condition,
            SalesCount = sold.Count,
            HasEnoughData = suggested.HasValue,
            SuggestedPrice = suggested,
            Message = suggested.HasValue
                ? null
                : $"Ikke nok data: prisforslag krever minst {PriceCalculator.MinSalesForSuggestion} solgte annonser i kategorien."
        };
    }

    public async Task<PriceAssessment> AssessPriceAsync(int categoryId, Condition condition, int price) =>
        PriceCalculator.AssessPrice(await analytics.GetSoldListingsAsync(categoryId), condition, price);

    public async Task<CategoryStatsDto> GetCategoryStatsAsync(int categoryId) =>
        await BuildStatsAsync(await GetCategoryOrThrowAsync(categoryId));

    public async Task<List<CategoryStatsDto>> GetOverviewAsync()
    {
        var result = new List<CategoryStatsDto>();
        foreach (var category in await categories.GetAllAsync())
            result.Add(await BuildStatsAsync(category));
        return result;
    }

    public async Task<PriceHistoryDto> GetPriceHistoryAsync(int listingId)
    {
        var listing = await analytics.GetListingWithPriceChangesAsync(listingId)
                      ?? throw new NotFoundException("Annonsen finnes ikke.");

        return new PriceHistoryDto
        {
            ListingId = listing.Id,
            Title = listing.Title,
            OriginalPrice = PriceCalculator.OriginalPrice(listing),
            CurrentPrice = listing.Price,
            Changes = listing.PriceChanges
                .OrderBy(p => p.ChangedDate).ThenBy(p => p.Id)
                .Select(p => new PriceChangeDto
                {
                    OldPrice = p.OldPrice,
                    NewPrice = p.NewPrice,
                    ChangedDate = p.ChangedDate
                })
                .ToList()
        };
    }

    private async Task<CategoryStatsDto> BuildStatsAsync(Category category)
    {
        var statuses = await analytics.GetStatusesAsync(category.Id);
        var sold = await analytics.GetSoldListingsAsync(category.Id);
        var soldPrices = sold.Select(l => (double)l.SoldPrice!.Value).ToList();

        var activeCount = statuses.Count(s => s == ListingStatus.Active);
        var soldCount = statuses.Count(s => s == ListingStatus.Sold);
        var removedCount = statuses.Count(s => s == ListingStatus.Removed);
        var total = activeCount + soldCount + removedCount;

        return new CategoryStatsDto
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            ActiveCount = activeCount,
            SoldCount = soldCount,
            RemovedCount = removedCount,
            AverageSoldPrice = soldPrices.Count > 0 ? Round(Statistics.Mean(soldPrices)) : null,
            MedianSoldPrice = soldPrices.Count > 0 ? Round(Statistics.Median(soldPrices)) : null,
            StandardDeviationSoldPrice = soldPrices.Count > 1 ? Round(Statistics.SampleStandardDeviation(soldPrices)) : null,
            AverageDaysToSell = Round(PriceCalculator.AverageDaysToSell(sold)),
            AverageDiscountPercent = Round(PriceCalculator.AverageDiscountPercent(sold)),
            // Salgsandel: solgte / (solgte + aktive + fjernede)
            SellThroughPercent = total > 0 ? Round(soldCount * 100.0 / total) : null
        };
    }

    private async Task<Category> GetCategoryOrThrowAsync(int categoryId) =>
        await categories.GetByIdAsync(categoryId)
        ?? throw new NotFoundException("Kategorien finnes ikke.");

    // To desimaler i svaret er nok for visning
    private static double? Round(double? value) =>
        value.HasValue ? Math.Round(value.Value, 2) : null;
}
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;

namespace Bruktborsen.Tests;

// Tester AnalyticsService mot databasen: at riktige annonser hentes ut
// (solgte, i riktig kategori) og at svaret til klienten blir riktig.
public class AnalyticsServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly ApplicationDbContext _db;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        _db = _testDb.CreateContext();
        _service = new AnalyticsService(new AnalyticsRepository(_db), new CategoryRepository(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        _testDb.Dispose();
    }

    private void AddSale(Condition condition, int soldPrice, int? categoryId = null)
    {
        using var db = _testDb.CreateContext();
        db.Listings.Add(new Listing
        {
            Title = "Solgt vare",
            Price = soldPrice,
            Condition = condition,
            Status = ListingStatus.Sold,
            SoldPrice = soldPrice,
            CreatedDate = new DateTime(2026, 5, 1),
            SoldDate = new DateTime(2026, 5, 8),
            CategoryId = categoryId ?? _testDb.CategoryId,
            SellerId = TestDb.SellerId
        });
        db.SaveChanges();
    }

    // Datasettet fra dokumentasjonen: median av normaliserte priser er 8460
    private void AddSixSales()
    {
        AddSale(Condition.Good, 4200);
        AddSale(Condition.Used, 3850);
        AddSale(Condition.Good, 5544);
        AddSale(Condition.Defect, 2700);
        AddSale(Condition.New, 10000);
        AddSale(Condition.LikeNew, 9350);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithEnoughSales_ReturnsSuggestedPrice()
    {
        AddSixSales();

        var result = await _service.GetPriceSuggestionAsync(_testDb.CategoryId, Condition.Good);

        Assert.True(result.HasEnoughData);
        Assert.Equal(6, result.SalesCount);
        Assert.Equal(5920, result.SuggestedPrice);   // 8460 * 0,70 = 5922 -> 5920
        Assert.Null(result.Message);
    }

    // Påkrevd: prisforslag med færre enn 3 salg gir «ikke nok data»
    [Fact]
    public async Task GetPriceSuggestion_FewerThanThreeSales_ReturnsNotEnoughData()
    {
        AddSale(Condition.Good, 4200);
        AddSale(Condition.Used, 3850);

        var result = await _service.GetPriceSuggestionAsync(_testDb.CategoryId, Condition.Good);

        Assert.False(result.HasEnoughData);
        Assert.Equal(2, result.SalesCount);
        Assert.Null(result.SuggestedPrice);
        Assert.Contains("Ikke nok data", result.Message);
    }

    [Fact]
    public async Task GetPriceSuggestion_IgnoresActiveListingsAndOtherCategories()
    {
        AddSixSales();

        // Skal ikke telle med: aktiv annonse i samme kategori, og salg i en annen kategori
        _testDb.AddListing(price: 99000);
        int otherCategoryId;
        using (var db = _testDb.CreateContext())
        {
            var other = new Category { Name = "Sykkel" };
            db.Categories.Add(other);
            db.SaveChanges();
            otherCategoryId = other.Id;
        }
        AddSale(Condition.New, 99000, otherCategoryId);

        var result = await _service.GetPriceSuggestionAsync(_testDb.CategoryId, Condition.Good);

        Assert.Equal(6, result.SalesCount);
        Assert.Equal(5920, result.SuggestedPrice);
    }

    [Fact]
    public async Task GetPriceSuggestion_UnknownCategory_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetPriceSuggestionAsync(9999, Condition.Good));
    }

    [Fact]
    public async Task AssessPrice_FewerThanFiveSales_ReturnsUnknown()
    {
        AddSale(Condition.Good, 4200);
        AddSale(Condition.Used, 3850);
        AddSale(Condition.Good, 5544);
        AddSale(Condition.Defect, 2700);

        var result = await _service.AssessPriceAsync(_testDb.CategoryId, Condition.Good, 6000);

        Assert.Equal(PriceAssessment.Unknown, result);
    }

    [Fact]
    public async Task AssessPrice_ObviouslyTooHighPrice_ReturnsHigh()
    {
        AddSixSales();

        var result = await _service.AssessPriceAsync(_testDb.CategoryId, Condition.Good, 10000);

        Assert.Equal(PriceAssessment.High, result);
    }

    [Fact]
    public async Task GetCategoryStats_CountsStatusesAndCalculatesSoldPriceStatistics()
    {
        // Solgt for 1000, 2000 og 3000 -> snitt 2000, median 2000, standardavvik (n - 1) = 1000
        AddSale(Condition.Good, 1000);
        AddSale(Condition.Good, 2000);
        AddSale(Condition.Good, 3000);
        _testDb.AddListing();                                   // aktiv
        _testDb.AddListing(status: ListingStatus.Removed);      // fjernet

        var stats = await _service.GetCategoryStatsAsync(_testDb.CategoryId);

        Assert.Equal(1, stats.ActiveCount);
        Assert.Equal(3, stats.SoldCount);
        Assert.Equal(1, stats.RemovedCount);
        Assert.Equal(2000, stats.AverageSoldPrice);
        Assert.Equal(2000, stats.MedianSoldPrice);
        Assert.Equal(1000, stats.StandardDeviationSoldPrice);
        Assert.Equal(7, stats.AverageDaysToSell);
        Assert.Equal(0, stats.AverageDiscountPercent);          // solgt til utlyst pris
        Assert.Equal(60, stats.SellThroughPercent);             // 3 / (3 + 1 + 1)
    }
}

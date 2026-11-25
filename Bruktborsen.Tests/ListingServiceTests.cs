using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests;

public class ListingServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly ApplicationDbContext _db;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _db = _testDb.CreateContext();
        _service = new ListingService(
            new ListingRepository(_db),
            new CategoryRepository(_db),
            new FakeAnalyticsService());
    }

    public void Dispose()
    {
        _db.Dispose();
        _testDb.Dispose();
    }

    private ListingRequestDto Request(int price) => new()
    {
        Title = "Testannonse",
        Price = price,
        Condition = Condition.Good,
        CategoryId = _testDb.CategoryId
    };

    // Påkrevd: prisendring lager PriceChange med riktig gammel og ny pris
    [Fact]
    public async Task Update_ChangedPrice_LogsPriceChange()
    {
        var listingId = _testDb.AddListing(price: 1000);

        await _service.UpdateAsync(listingId, Request(800), TestDb.SellerId, isAdmin: false);

        using var db = _testDb.CreateContext();
        var change = Assert.Single(await db.PriceChanges.Where(p => p.ListingId == listingId).ToListAsync());
        Assert.Equal(1000, change.OldPrice);
        Assert.Equal(800, change.NewPrice);
        Assert.Equal(800, (await db.Listings.FindAsync(listingId))!.Price);
    }

    [Fact]
    public async Task Update_SamePrice_DoesNotLogPriceChange()
    {
        var listingId = _testDb.AddListing(price: 1000);

        await _service.UpdateAsync(listingId, Request(1000), TestDb.SellerId, isAdmin: false);

        using var db = _testDb.CreateContext();
        Assert.Empty(await db.PriceChanges.Where(p => p.ListingId == listingId).ToListAsync());
    }

    // Påkrevd: endring av solgt annonse avvises
    [Fact]
    public async Task Update_SoldListing_ThrowsConflict()
    {
        var listingId = _testDb.AddListing(status: ListingStatus.Sold);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(listingId, Request(800), TestDb.SellerId, isAdmin: false));
    }

    [Fact]
    public async Task Update_ByOtherUser_ThrowsForbidden()
    {
        var listingId = _testDb.AddListing();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.UpdateAsync(listingId, Request(800), TestDb.BuyerId, isAdmin: false));
    }

    [Fact]
    public async Task Update_ByAdmin_IsAllowed()
    {
        var listingId = _testDb.AddListing(price: 1000);

        var result = await _service.UpdateAsync(listingId, Request(900), TestDb.BuyerId, isAdmin: true);

        Assert.Equal(900, result.Price);
    }

    [Fact]
    public async Task Remove_SetsStatusRemoved_AndRejectsPendingOffers()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);

        await _service.RemoveAsync(listingId, TestDb.SellerId, isAdmin: false);

        using var db = _testDb.CreateContext();
        var listing = await db.Listings.FindAsync(listingId);
        Assert.NotNull(listing);                                  // ikke slettet
        Assert.Equal(ListingStatus.Removed, listing.Status);
        Assert.Equal(OfferStatus.Rejected, (await db.Offers.FindAsync(offerId))!.Status);
    }

    [Fact]
    public async Task Search_InvalidSort_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.SearchAsync(new ListingQueryDto { Sort = "cheapest" }));
    }
    
    
    private class FakeAnalyticsService : IAnalyticsService
    {
        public Task<PriceAssessment> AssessPriceAsync(int categoryId, Condition condition, int price) =>
            Task.FromResult(PriceAssessment.Unknown);

        // ListingService bruker bare AssessPriceAsync. Resten er ikke i bruk i disse testene.
        public Task<PriceSuggestionDto> GetPriceSuggestionAsync(int categoryId, Condition condition) =>
            throw new NotSupportedException();

        public Task<CategoryStatsDto> GetCategoryStatsAsync(int categoryId) =>
            throw new NotSupportedException();

        public Task<List<CategoryStatsDto>> GetOverviewAsync() =>
            throw new NotSupportedException();

        public Task<PriceHistoryDto> GetPriceHistoryAsync(int listingId) =>
            throw new NotSupportedException();
    }
}
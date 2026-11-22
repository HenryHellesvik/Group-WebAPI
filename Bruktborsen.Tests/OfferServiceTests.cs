using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;
using Bruktborsen.Api.Services;

namespace Bruktborsen.Tests;

public class OfferServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly ApplicationDbContext _db;
    private readonly OfferService _service;

    public OfferServiceTests()
    {
        _db = _testDb.CreateContext();
        _service = new OfferService(new OfferRepository(_db), new ListingRepository(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        _testDb.Dispose();
    }

    private static CreateOfferDto Bid(int amount) => new() { Amount = amount };

    // Regel 1
    [Fact]
    public async Task Create_OnOwnListing_ThrowsBadRequest()
    {
        var listingId = _testDb.AddListing();

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(listingId, TestDb.SellerId, Bid(900)));
    }

    // Regel 2
    [Fact]
    public async Task Create_OnSoldListing_ThrowsConflict()
    {
        var listingId = _testDb.AddListing(status: ListingStatus.Sold);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(listingId, TestDb.BuyerId, Bid(900)));
    }

    [Fact]
    public async Task Create_OnRemovedListing_ThrowsConflict()
    {
        var listingId = _testDb.AddListing(status: ListingStatus.Removed);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(listingId, TestDb.BuyerId, Bid(900)));
    }

    // Regel 3
    [Fact]
    public async Task Create_SecondPendingOfferFromSameBuyer_ThrowsConflict()
    {
        var listingId = _testDb.AddListing();
        await _service.CreateAsync(listingId, TestDb.BuyerId, Bid(800));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(listingId, TestDb.BuyerId, Bid(900)));
    }

    [Fact]
    public async Task Create_NewOfferAfterWithdrawingFirst_IsAccepted()
    {
        var listingId = _testDb.AddListing();
        var first = await _service.CreateAsync(listingId, TestDb.BuyerId, Bid(800));
        await _service.WithdrawAsync(first.Id, TestDb.BuyerId);

        var second = await _service.CreateAsync(listingId, TestDb.BuyerId, Bid(900));

        Assert.Equal(OfferStatus.Pending, second.Status);
    }

    // Regel 4
    [Fact]
    public async Task Create_BelowHalfPrice_ThrowsBadRequest()
    {
        var listingId = _testDb.AddListing(price: 1000);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(listingId, TestDb.BuyerId, Bid(499)));
    }

    [Fact]
    public async Task Create_ExactlyHalfPrice_IsAccepted()
    {
        var listingId = _testDb.AddListing(price: 1000);

        var offer = await _service.CreateAsync(listingId, TestDb.BuyerId, Bid(500));

        Assert.Equal(500, offer.Amount);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Equal("Kjøper Kjøpersen", offer.BuyerName);
    }

    [Fact]
    public async Task Create_OddPrice_RequiresAtLeastHalfRoundedUp()
    {
        // 50 % av 1001 er 500,5: 500 er for lavt, 501 er nok
        var listingId = _testDb.AddListing(price: 1001);

        await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(listingId, TestDb.BuyerId, Bid(500)));

        var offer = await _service.CreateAsync(listingId, TestDb.BuyerId, Bid(501));
        Assert.Equal(501, offer.Amount);
    }

    [Fact]
    public async Task Create_OnMissingListing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(9999, TestDb.BuyerId, Bid(500)));
    }

    // Regel 5
    [Fact]
    public async Task Accept_MarksListingSoldAndRejectsOtherPendingOffers()
    {
        var listingId = _testDb.AddListing(price: 1000);
        var acceptedId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);
        var otherPendingId = _testDb.AddOffer(listingId, TestDb.OtherBuyerId, 700);
        var withdrawnId = _testDb.AddOffer(listingId, TestDb.OtherBuyerId, 600, OfferStatus.Withdrawn);

        var result = await _service.AcceptAsync(acceptedId, TestDb.SellerId);

        Assert.Equal(OfferStatus.Accepted, result.Status);

        // Leser med ny context for å sjekke det som faktisk ble lagret
        using var db = _testDb.CreateContext();
        var listing = await db.Listings.FindAsync(listingId);
        Assert.Equal(ListingStatus.Sold, listing!.Status);
        Assert.Equal(800, listing.SoldPrice);
        Assert.NotNull(listing.SoldDate);

        Assert.Equal(OfferStatus.Accepted, (await db.Offers.FindAsync(acceptedId))!.Status);
        Assert.Equal(OfferStatus.Rejected, (await db.Offers.FindAsync(otherPendingId))!.Status);
        // Bud som ikke var Pending skal ikke røres
        Assert.Equal(OfferStatus.Withdrawn, (await db.Offers.FindAsync(withdrawnId))!.Status);
    }

    [Fact]
    public async Task Accept_ByNonSeller_ThrowsForbiddenAndChangesNothing()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.AcceptAsync(offerId, TestDb.BuyerId));

        using var db = _testDb.CreateContext();
        Assert.Equal(ListingStatus.Active, (await db.Listings.FindAsync(listingId))!.Status);
        Assert.Equal(OfferStatus.Pending, (await db.Offers.FindAsync(offerId))!.Status);
    }

    [Fact]
    public async Task Accept_AlreadyHandledOffer_ThrowsConflict()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800, OfferStatus.Rejected);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AcceptAsync(offerId, TestDb.SellerId));
    }

    [Fact]
    public async Task Reject_BySeller_SetsRejectedAndKeepsListingActive()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);

        var result = await _service.RejectAsync(offerId, TestDb.SellerId);

        Assert.Equal(OfferStatus.Rejected, result.Status);
        using var db = _testDb.CreateContext();
        Assert.Equal(ListingStatus.Active, (await db.Listings.FindAsync(listingId))!.Status);
    }

    // Regel 6
    [Fact]
    public async Task Withdraw_OwnPendingOffer_SetsWithdrawn()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);

        var result = await _service.WithdrawAsync(offerId, TestDb.BuyerId);

        Assert.Equal(OfferStatus.Withdrawn, result.Status);
    }

    [Fact]
    public async Task Withdraw_SomeoneElsesOffer_ThrowsForbidden()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.WithdrawAsync(offerId, TestDb.OtherBuyerId));
    }

    [Fact]
    public async Task Withdraw_NonPendingOffer_ThrowsConflict()
    {
        var listingId = _testDb.AddListing();
        var offerId = _testDb.AddOffer(listingId, TestDb.BuyerId, 800, OfferStatus.Accepted);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.WithdrawAsync(offerId, TestDb.BuyerId));
    }

    [Fact]
    public async Task GetForListing_ByNonSeller_ThrowsForbidden()
    {
        var listingId = _testDb.AddListing();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.GetForListingAsync(listingId, TestDb.BuyerId));
    }
}

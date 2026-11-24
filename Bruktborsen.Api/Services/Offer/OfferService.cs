using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class OfferService(IOfferRepository offers, IListingRepository listings) : IOfferService
{
    public async Task<List<OfferDto>> GetForListingAsync(int listingId, string userId)
    {
        var listing = await listings.GetByIdAsync(listingId)
                      ?? throw new NotFoundException("Annonsen finnes ikke.");

        if (listing.SellerId != userId)
            throw new ForbiddenException("Bare selger kan se budene på annonsen.");

        return (await offers.GetByListingAsync(listingId)).Select(ToDto).ToList();
    }

    public async Task<List<OfferDto>> GetMineAsync(string userId) =>
        (await offers.GetByBuyerAsync(userId)).Select(ToDto).ToList();

    public async Task<OfferDto> CreateAsync(int listingId, string buyerId, CreateOfferDto dto)
    {
        var listing = await listings.GetByIdAsync(listingId)
                      ?? throw new NotFoundException("Annonsen finnes ikke.");

        // Regel 1: ikke bud på egen annonse
        if (listing.SellerId == buyerId)
            throw new BadRequestException("Du kan ikke gi bud på din egen annonse.");

        // Regel 2: bare aktive annonser
        if (listing.Status != ListingStatus.Active)
            throw new ConflictException("Annonsen er ikke aktiv.");

        // Regel 3: ett Pending-bud per kjøper per annonse
        if (await offers.HasPendingAsync(listingId, buyerId))
            throw new ConflictException("Du har allerede et ventende bud på denne annonsen.");

        // Regel 4: minst 50 % av prisen. Heltallsregning, så nøyaktig 50 % godtas
        // og vi slipper avrundingsfeil ved oddetallspriser.
        if ((long)dto.Amount * 2 < listing.Price)
            throw new BadRequestException("Budet må være minst 50 % av annonsens pris.");

        var offer = new Offer
        {
            ListingId = listingId,
            BuyerId = buyerId,
            Amount = dto.Amount
        };
        offers.Add(offer);
        await offers.SaveChangesAsync();

        // Hentes på nytt for å få med Listing og Buyer til DTO-en
        return ToDto((await offers.GetByIdAsync(offer.Id))!);
    }

    public async Task<OfferDto> AcceptAsync(int offerId, string userId)
    {
        var offer = await GetPendingOfferForSellerAsync(offerId, userId);

        if (offer.Listing.Status != ListingStatus.Active)
            throw new ConflictException("Annonsen er ikke aktiv.");

        // Regel 5: alt skjer i én transaksjon
        await using var transaction = await offers.BeginTransactionAsync();

        offer.Status = OfferStatus.Accepted;

        offer.Listing.Status = ListingStatus.Sold;
        offer.Listing.SoldPrice = offer.Amount;
        offer.Listing.SoldDate = DateTime.UtcNow;

        foreach (var other in await offers.GetOtherPendingAsync(offer.ListingId, offer.Id))
            other.Status = OfferStatus.Rejected;

        await offers.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToDto(offer);
    }

    public async Task<OfferDto> RejectAsync(int offerId, string userId)
    {
        var offer = await GetPendingOfferForSellerAsync(offerId, userId);

        offer.Status = OfferStatus.Rejected;
        await offers.SaveChangesAsync();

        return ToDto(offer);
    }

    public async Task<OfferDto> WithdrawAsync(int offerId, string userId)
    {
        var offer = await offers.GetByIdAsync(offerId)
                    ?? throw new NotFoundException("Budet finnes ikke.");

        // Regel 6: bare budgiver kan trekke budet, og bare mens det er Pending
        if (offer.BuyerId != userId)
            throw new ForbiddenException("Du kan bare trekke dine egne bud.");

        if (offer.Status != OfferStatus.Pending)
            throw new ConflictException("Bare ventende bud kan trekkes.");

        offer.Status = OfferStatus.Withdrawn;
        await offers.SaveChangesAsync();

        return ToDto(offer);
    }

    // Felles sjekker for aksept og avslag: budet finnes, brukeren er selger, budet er Pending.
    private async Task<Offer> GetPendingOfferForSellerAsync(int offerId, string userId)
    {
        var offer = await offers.GetByIdAsync(offerId)
                    ?? throw new NotFoundException("Budet finnes ikke.");

        if (offer.Listing.SellerId != userId)
            throw new ForbiddenException("Bare selger kan akseptere eller avslå bud.");

        if (offer.Status != OfferStatus.Pending)
            throw new ConflictException("Budet er allerede behandlet.");

        return offer;
    }

    private static OfferDto ToDto(Offer offer) => new()
    {
        Id = offer.Id,
        ListingId = offer.ListingId,
        ListingTitle = offer.Listing.Title,
        BuyerId = offer.BuyerId,
        BuyerName = offer.Buyer.FullName,
        Amount = offer.Amount,
        Status = offer.Status,
        CreatedDate = offer.CreatedDate
    };
}

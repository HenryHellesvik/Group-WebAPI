using Bruktborsen.Api.DTOs.Offers;

namespace Bruktborsen.Api.Services;

public interface IOfferService
{
    Task<List<OfferDto>> GetForListingAsync(int listingId, string userId);
    Task<List<OfferDto>> GetMineAsync(string userId);
    Task<OfferDto> CreateAsync(int listingId, string buyerId, CreateOfferDto dto);
    Task<OfferDto> AcceptAsync(int offerId, string userId);
    Task<OfferDto> RejectAsync(int offerId, string userId);
    Task<OfferDto> WithdrawAsync(int offerId, string userId);
}

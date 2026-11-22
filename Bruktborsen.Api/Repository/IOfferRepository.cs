using Bruktborsen.Api.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bruktborsen.Api.Repository;

public interface IOfferRepository
{
    // Alle metodene som returnerer bud tar med Listing og Buyer.
    Task<Offer?> GetByIdAsync(int id);
    Task<List<Offer>> GetByListingAsync(int listingId);
    Task<List<Offer>> GetByBuyerAsync(string buyerId);
    Task<List<Offer>> GetOtherPendingAsync(int listingId, int exceptOfferId);
    Task<bool> HasPendingAsync(int listingId, string buyerId);

    void Add(Offer offer);
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}

using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bruktborsen.Api.Repository;

public class OfferRepository(ApplicationDbContext db) : IOfferRepository
{
    private IQueryable<Offer> OffersWithDetails =>
        db.Offers.Include(o => o.Listing).Include(o => o.Buyer);

    public Task<Offer?> GetByIdAsync(int id) =>
        OffersWithDetails.FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<Offer>> GetByListingAsync(int listingId) =>
        OffersWithDetails
            .Where(o => o.ListingId == listingId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

    public Task<List<Offer>> GetByBuyerAsync(string buyerId) =>
        OffersWithDetails
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

    public Task<List<Offer>> GetOtherPendingAsync(int listingId, int exceptOfferId) =>
        db.Offers
            .Where(o => o.ListingId == listingId
                        && o.Id != exceptOfferId
                        && o.Status == OfferStatus.Pending)
            .ToListAsync();

    public Task<bool> HasPendingAsync(int listingId, string buyerId) =>
        db.Offers.AnyAsync(o => o.ListingId == listingId
                                && o.BuyerId == buyerId
                                && o.Status == OfferStatus.Pending);

    public void Add(Offer offer) => db.Offers.Add(offer);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync() => db.Database.BeginTransactionAsync();
}

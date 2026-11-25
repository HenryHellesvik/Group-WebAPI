using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

// AsNoTracking: analysen bare leser, og forstyrrer da ikke annonser
// som ListingService holder på å endre i samme DbContext.
public class AnalyticsRepository(ApplicationDbContext db) : IAnalyticsRepository
{
    public Task<List<Listing>> GetSoldListingsAsync(int categoryId) =>
        db.Listings
            .AsNoTracking()
            .Include(l => l.PriceChanges)
            .Where(l => l.CategoryId == categoryId
                        && l.Status == ListingStatus.Sold
                        && l.SoldPrice != null)
            .ToListAsync();

    public Task<List<ListingStatus>> GetStatusesAsync(int categoryId) =>
        db.Listings
            .Where(l => l.CategoryId == categoryId)
            .Select(l => l.Status)
            .ToListAsync();

    public Task<Listing?> GetListingWithPriceChangesAsync(int listingId) =>
        db.Listings
            .AsNoTracking()
            .Include(l => l.PriceChanges)
            .FirstOrDefaultAsync(l => l.Id == listingId);
}
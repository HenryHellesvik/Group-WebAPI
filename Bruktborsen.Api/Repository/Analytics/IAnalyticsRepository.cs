using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Repository;

public interface IAnalyticsRepository
{
    // Solgte annonser i kategorien, med PriceChanges (trengs for rabatt)
    Task<List<Listing>> GetSoldListingsAsync(int categoryId);

    // Status for alle annonser i kategorien (for antall aktive, solgte og fjernede)
    Task<List<ListingStatus>> GetStatusesAsync(int categoryId);

    Task<Listing?> GetListingWithPriceChangesAsync(int listingId);
}
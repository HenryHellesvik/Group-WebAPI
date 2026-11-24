using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository;

public interface IListingRepository
{
    // Uten relaterte data (brukes av OfferService og FavoriteService)
    Task<Listing?> GetByIdAsync(int id);

    // Med Category, Seller og Offers
    Task<Listing?> GetWithDetailsAsync(int id);

    Task<List<Listing>> SearchActiveAsync(ListingQueryDto query);
    Task<List<Listing>> GetBySellerAsync(string sellerId);

    void Add(Listing listing);
    Task SaveChangesAsync();
}
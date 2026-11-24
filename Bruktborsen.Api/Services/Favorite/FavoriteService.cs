using Bruktborsen.Api.DTOs.Favorites;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class FavoriteService(IFavoriteRepository favorites, IListingRepository listings) : IFavoriteService
{
    public async Task<List<FavoriteDto>> GetMineAsync(string userId) =>
        (await favorites.GetByUserAsync(userId)).Select(ToDto).ToList();

    public async Task<FavoriteDto> AddAsync(int listingId, string userId)
    {
        var listing = await listings.GetByIdAsync(listingId)
                      ?? throw new NotFoundException("Annonsen finnes ikke.");

        if (listing.Status == ListingStatus.Removed)
            throw new ConflictException("Annonsen er fjernet.");

        if (await favorites.GetAsync(userId, listingId) != null)
            throw new ConflictException("Annonsen er allerede en favoritt.");

        var favorite = new Favorite { ListingId = listingId, UserId = userId, Listing = listing };
        favorites.Add(favorite);
        await favorites.SaveChangesAsync();

        return ToDto(favorite);
    }

    public async Task RemoveAsync(int listingId, string userId)
    {
        var favorite = await favorites.GetAsync(userId, listingId)
                       ?? throw new NotFoundException("Annonsen er ikke en favoritt.");

        favorites.Remove(favorite);
        await favorites.SaveChangesAsync();
    }

    private static FavoriteDto ToDto(Favorite f) => new()
    {
        Id = f.Id,
        ListingId = f.ListingId,
        ListingTitle = f.Listing.Title,
        Price = f.Listing.Price,
        Condition = f.Listing.Condition,
        Status = f.Listing.Status,
        ImageUrl = f.Listing.ImageUrl
    };
}
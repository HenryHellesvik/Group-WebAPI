using Bruktborsen.Api.DTOs.Favorites;

namespace Bruktborsen.Api.Services;

public interface IFavoriteService
{
    Task<List<FavoriteDto>> GetMineAsync(string userId);
    Task<FavoriteDto> AddAsync(int listingId, string userId);
    Task RemoveAsync(int listingId, string userId);
}
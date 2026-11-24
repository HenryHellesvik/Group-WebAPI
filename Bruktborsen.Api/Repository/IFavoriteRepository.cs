using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository;

public interface IFavoriteRepository
{
    Task<List<Favorite>> GetByUserAsync(string userId);
    Task<Favorite?> GetAsync(string userId, int listingId);
    void Add(Favorite favorite);
    void Remove(Favorite favorite);
    Task SaveChangesAsync();
}
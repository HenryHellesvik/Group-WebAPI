using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class FavoriteRepository(ApplicationDbContext db) : IFavoriteRepository
{
    public Task<List<Favorite>> GetByUserAsync(string userId) =>
        db.Favorites
            .Include(f => f.Listing)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.Id)
            .ToListAsync();

    public Task<Favorite?> GetAsync(string userId, int listingId) =>
        db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId);

    public void Add(Favorite favorite) => db.Favorites.Add(favorite);

    public void Remove(Favorite favorite) => db.Favorites.Remove(favorite);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class ListingRepository(ApplicationDbContext db) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(int id) =>
        db.Listings.FirstOrDefaultAsync(l => l.Id == id);
}

using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class ListingRepository(ApplicationDbContext db) : IListingRepository
{
    public Task<Listing?> GetByIdAsync(int id) =>
        db.Listings.FirstOrDefaultAsync(l => l.Id == id);

    public Task<Listing?> GetWithDetailsAsync(int id) =>
        db.Listings
            .Include(l => l.Category)
            .Include(l => l.Seller)
            .Include(l => l.Offers)
            .FirstOrDefaultAsync(l => l.Id == id);

    public Task<List<Listing>> SearchActiveAsync(ListingQueryDto query)
    {
        var listings = db.Listings
            .Include(l => l.Category)
            .Where(l => l.Status == ListingStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            listings = listings.Where(l =>
                l.Title.ToLower().Contains(search) ||
                (l.Description != null && l.Description.ToLower().Contains(search)));
        }

        if (query.CategoryId.HasValue)
            listings = listings.Where(l => l.CategoryId == query.CategoryId.Value);

        if (query.Condition.HasValue)
            listings = listings.Where(l => l.Condition == query.Condition.Value);

        if (query.MinPrice.HasValue)
            listings = listings.Where(l => l.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            listings = listings.Where(l => l.Price <= query.MaxPrice.Value);

        listings = query.Sort switch
        {
            "price_asc"  => listings.OrderBy(l => l.Price),
            "price_desc" => listings.OrderByDescending(l => l.Price),
            _            => listings.OrderByDescending(l => l.CreatedDate)   // newest
        };

        return listings.ToListAsync();
    }

    public Task<List<Listing>> GetBySellerAsync(string sellerId) =>
        db.Listings
            .Include(l => l.Category)
            .Where(l => l.SellerId == sellerId)
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync();

    public void Add(Listing listing) => db.Listings.Add(listing);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
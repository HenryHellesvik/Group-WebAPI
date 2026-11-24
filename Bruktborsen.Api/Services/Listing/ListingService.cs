using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class ListingService(
    IListingRepository listings,
    ICategoryRepository categories,
    IAnalyticsService analytics) : IListingService
{
    private static readonly string[] ValidSorts = ["newest", "price_asc", "price_desc"];

    public async Task<List<ListingDto>> SearchAsync(ListingQueryDto query)
    {
        if (query.Sort != null && !ValidSorts.Contains(query.Sort))
            throw new BadRequestException("Ugyldig sortering. Bruk newest, price_asc eller price_desc.");

        if (query.MinPrice > query.MaxPrice)
            throw new BadRequestException("Minstepris kan ikke være høyere enn makspris.");

        return (await listings.SearchActiveAsync(query)).Select(ToDto).ToList();
    }

    public async Task<ListingDetailsDto> GetByIdAsync(int id) =>
        await ToDetailsDtoAsync(await GetListingOrThrowAsync(id));

    public async Task<List<ListingDto>> GetMineAsync(string userId) =>
        (await listings.GetBySellerAsync(userId)).Select(ToDto).ToList();

    public async Task<ListingDetailsDto> CreateAsync(ListingRequestDto dto, string userId)
    {
        var category = await GetCategoryOrThrowAsync(dto.CategoryId);

        var listing = new Listing
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            Condition = dto.Condition,
            ImageUrl = dto.ImageUrl,
            Category = category,
            SellerId = userId,
            Status = ListingStatus.Active,
            CreatedDate = DateTime.UtcNow
        };

        listings.Add(listing);
        await listings.SaveChangesAsync();

        // Hentes på nytt for å få med Seller til DTO-en
        return await GetByIdAsync(listing.Id);
    }

    public async Task<ListingDetailsDto> UpdateAsync(int id, ListingRequestDto dto, string userId, bool isAdmin)
    {
        var listing = await GetListingOrThrowAsync(id);
        EnsureCanModify(listing, userId, isAdmin);

        var category = await GetCategoryOrThrowAsync(dto.CategoryId);

        // Regel: prisendring logges automatisk
        if (dto.Price != listing.Price)
        {
            listing.PriceChanges.Add(new PriceChange
            {
                OldPrice = listing.Price,
                NewPrice = dto.Price,
                ChangedDate = DateTime.UtcNow
            });
        }

        listing.Title = dto.Title.Trim();
        listing.Description = dto.Description?.Trim();
        listing.Price = dto.Price;
        listing.Condition = dto.Condition;
        listing.ImageUrl = dto.ImageUrl;
        listing.Category = category;

        await listings.SaveChangesAsync();

        return await ToDetailsDtoAsync(listing);
    }

    public async Task RemoveAsync(int id, string userId, bool isAdmin)
    {
        var listing = await GetListingOrThrowAsync(id);
        EnsureCanModify(listing, userId, isAdmin);

        // Regel: fjerning setter status, annonsen slettes ikke
        listing.Status = ListingStatus.Removed;

        // Ventende bud på en fjernet annonse gir ingen mening, så de avslås
        foreach (var offer in listing.Offers.Where(o => o.Status == OfferStatus.Pending))
            offer.Status = OfferStatus.Rejected;

        await listings.SaveChangesAsync();
    }

    // Felles sjekker for endring og fjerning
    private static void EnsureCanModify(Listing listing, string userId, bool isAdmin)
    {
        // Regel: bare selger eller Admin
        if (listing.SellerId != userId && !isAdmin)
            throw new ForbiddenException("Bare selger eller Admin kan endre annonsen.");

        // Regel: solgte annonser kan ikke endres
        if (listing.Status == ListingStatus.Sold)
            throw new ConflictException("Solgte annonser kan ikke endres.");

        if (listing.Status == ListingStatus.Removed)
            throw new ConflictException("Annonsen er allerede fjernet.");
    }

    private async Task<Listing> GetListingOrThrowAsync(int id) =>
        await listings.GetWithDetailsAsync(id)
        ?? throw new NotFoundException("Annonsen finnes ikke.");

    private async Task<Category> GetCategoryOrThrowAsync(int categoryId) =>
        await categories.GetByIdAsync(categoryId)
        ?? throw new BadRequestException("Kategorien finnes ikke.");

    private static ListingDto ToDto(Listing l) => new()
    {
        Id = l.Id,
        Title = l.Title,
        Price = l.Price,
        Condition = l.Condition,
        Status = l.Status,
        ImageUrl = l.ImageUrl,
        CreatedDate = l.CreatedDate,
        CategoryId = l.CategoryId,
        CategoryName = l.Category.Name
    };

    private async Task<ListingDetailsDto> ToDetailsDtoAsync(Listing l) => new()
    {
        Id = l.Id,
        Title = l.Title,
        Price = l.Price,
        Condition = l.Condition,
        Status = l.Status,
        ImageUrl = l.ImageUrl,
        CreatedDate = l.CreatedDate,
        CategoryId = l.CategoryId,
        CategoryName = l.Category.Name,
        Description = l.Description,
        SoldDate = l.SoldDate,
        SoldPrice = l.SoldPrice,
        SellerId = l.SellerId,
        SellerName = l.Seller.FullName,
        OfferCount = l.Offers.Count,
        PriceAssessment = await analytics.AssessPriceAsync(l.CategoryId, l.Condition, l.Price)
    };
}
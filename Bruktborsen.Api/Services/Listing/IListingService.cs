using Bruktborsen.Api.DTOs.Listings;

namespace Bruktborsen.Api.Services;

public interface IListingService
{
    Task<List<ListingDto>> SearchAsync(ListingQueryDto query);
    Task<ListingDetailsDto> GetByIdAsync(int id);
    Task<List<ListingDto>> GetMineAsync(string userId);
    Task<ListingDetailsDto> CreateAsync(ListingRequestDto dto, string userId);
    Task<ListingDetailsDto> UpdateAsync(int id, ListingRequestDto dto, string userId, bool isAdmin);
    Task RemoveAsync(int id, string userId, bool isAdmin);
}
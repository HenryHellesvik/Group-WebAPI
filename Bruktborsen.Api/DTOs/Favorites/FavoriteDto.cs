using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Favorites;

public class FavoriteDto
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public int Price { get; set; }
    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; }
    public string? ImageUrl { get; set; }
}
using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Listing
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int Price { get; set; }

    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? SoldDate { get; set; }
    public int? SoldPrice { get; set; }

    
    // Fremmednøkler + navigasjon
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    [Required]
    public string SellerId { get; set; } = string.Empty;
    public ApplicationUser Seller { get; set; } = null!;

    public List<Offer> Offers { get; set; } = new();
    public List<PriceChange> PriceChanges { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}
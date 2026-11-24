using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

// Brukes i lister (søk og mine annonser)
public class ListingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Price { get; set; }
    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

// Brukes for én annonse: GET /api/listings/{id}
public class ListingDetailsDto : ListingDto
{
    public string? Description { get; set; }
    public DateTime? SoldDate { get; set; }
    public int? SoldPrice { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public int OfferCount { get; set; }
    public PriceAssessment PriceAssessment { get; set; }
}

// Det klienten sender inn ved POST og PUT
public class ListingRequestDto
{
    [Required(ErrorMessage = "Tittel er påkrevd")]
    [MaxLength(100, ErrorMessage = "Tittel kan maks være 100 tegn")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Beskrivelse kan maks være 2000 tegn")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Prisen må være større enn 0")]
    public int Price { get; set; }

    [EnumDataType(typeof(Condition))]
    public Condition Condition { get; set; }

    [MaxLength(500)]
    [Url(ErrorMessage = "Ugyldig bilde-URL")]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Velg en kategori")]
    public int CategoryId { get; set; }
}

// Query-parametre for GET /api/listings
public class ListingQueryDto
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public Condition? Condition { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? Sort { get; set; }   // newest, price_asc, price_desc
}
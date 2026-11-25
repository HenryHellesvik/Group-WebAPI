using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Analytics;

// Query: /api/analytics/price-suggestion?categoryId=&condition=
public class PriceSuggestionQueryDto
{
    [Required(ErrorMessage = "categoryId er påkrevd")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "condition er påkrevd")]
    public Condition? Condition { get; set; }
}

public class PriceSuggestionDto
{
    public int CategoryId { get; set; }
    public Condition Condition { get; set; }
    public int SalesCount { get; set; }
    public bool HasEnoughData { get; set; }
    public int? SuggestedPrice { get; set; }
    public string? Message { get; set; }
}

public class CategoryStatsDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public int ActiveCount { get; set; }
    public int SoldCount { get; set; }
    public int RemovedCount { get; set; }

    // null når det ikke finnes nok salg til å regne ut verdien
    public double? AverageSoldPrice { get; set; }
    public double? MedianSoldPrice { get; set; }
    public double? StandardDeviationSoldPrice { get; set; }
    public double? AverageDaysToSell { get; set; }
    public double? AverageDiscountPercent { get; set; }
    public double? SellThroughPercent { get; set; }
}

public class PriceChangeDto
{
    public int OldPrice { get; set; }
    public int NewPrice { get; set; }
    public DateTime ChangedDate { get; set; }
}

public class PriceHistoryDto
{
    public int ListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OriginalPrice { get; set; }
    public int CurrentPrice { get; set; }
    public List<PriceChangeDto> Changes { get; set; } = new();
}
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services;

// Forretningsreglene for analysen. Tar imot solgte annonser og regner. Ingen database.
public static class PriceCalculator
{
    public const int MinSalesForSuggestion = 3;
    public const int MinSalesForAssessment = 5;

    public static double ConditionFactor(Condition condition) => condition switch
    {
        Condition.New     => 1.00,
        Condition.LikeNew => 0.85,
        Condition.Good    => 0.70,
        Condition.Used    => 0.55,
        Condition.Defect  => 0.30,
        _ => throw new ArgumentOutOfRangeException(nameof(condition))
    };

    // Hva varen ville kostet som ny
    public static double Normalize(int price, Condition condition) =>
        price / ConditionFactor(condition);

    public static List<double> NormalizedSoldPrices(IEnumerable<Listing> sold) =>
        sold.Where(l => l.SoldPrice.HasValue)
            .Select(l => Normalize(l.SoldPrice!.Value, l.Condition))
            .ToList();

    // A. Median av normaliserte salgspriser × ønsket tilstandsfaktor, avrundet til nærmeste 10 kr.
    // Returnerer null hvis det er færre enn 3 salg.
    public static int? SuggestPrice(IEnumerable<Listing> sold, Condition condition)
    {
        var normalized = NormalizedSoldPrices(sold);
        if (normalized.Count < MinSalesForSuggestion) return null;

        return RoundToNearest10(Statistics.Median(normalized) * ConditionFactor(condition));
    }

    // B. Lav under Q1 − 1,5·IQR, Høy over Q3 + 1,5·IQR, ellers Normal. Ukjent ved færre enn 5 salg.
    public static PriceAssessment AssessPrice(IEnumerable<Listing> sold, Condition condition, int price)
    {
        var normalized = NormalizedSoldPrices(sold);
        if (normalized.Count < MinSalesForAssessment) return PriceAssessment.Unknown;

        var q1 = Statistics.Quartile(normalized, 1);
        var q3 = Statistics.Quartile(normalized, 3);
        var iqr = q3 - q1;
        var normalizedPrice = Normalize(price, condition);

        if (normalizedPrice < q1 - 1.5 * iqr) return PriceAssessment.Low;
        if (normalizedPrice > q3 + 1.5 * iqr) return PriceAssessment.High;
        return PriceAssessment.Normal;
    }

    // Første OldPrice hvis prisen er endret, ellers Price
    public static int OriginalPrice(Listing listing) =>
        listing.PriceChanges.Count == 0
            ? listing.Price
            : listing.PriceChanges.OrderBy(p => p.ChangedDate).ThenBy(p => p.Id).First().OldPrice;

    // C. Gjennomsnittlig rabatt i prosent, fra opprinnelig pris til SoldPrice
    public static double? AverageDiscountPercent(IEnumerable<Listing> sold)
    {
        var discounts = sold
            .Where(l => l.SoldPrice.HasValue)
            .Select(l =>
            {
                var original = OriginalPrice(l);
                return (original - l.SoldPrice!.Value) * 100.0 / original;
            })
            .ToList();

        return discounts.Count == 0 ? null : discounts.Average();
    }

    // C. Gjennomsnittlig salgstid i dager (SoldDate − CreatedDate)
    public static double? AverageDaysToSell(IEnumerable<Listing> sold)
    {
        var days = sold
            .Where(l => l.SoldDate.HasValue)
            .Select(l => (l.SoldDate!.Value - l.CreatedDate).TotalDays)
            .ToList();

        return days.Count == 0 ? null : days.Average();
    }

    public static int RoundToNearest10(double value) =>
        (int)(Math.Round(value / 10, MidpointRounding.AwayFromZero) * 10);
}
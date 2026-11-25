using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services;

namespace Bruktborsen.Tests;

// Samme håndregnede datasett som i dokumentasjonen (kapittel 3):
//
//   Salg  Tilstand  Salgspris  Faktor  Normalisert pris
//   A     Good       4 200     0,70     6 000
//   B     Used       3 850     0,55     7 000
//   C     Good       5 544     0,70     7 920
//   D     Defect     2 700     0,30     9 000
//   E     New       10 000     1,00    10 000
//   F     LikeNew    9 350     0,85    11 000
//
//   Median = (7920 + 9000) / 2 = 8460
//   Q1 = 7230, Q3 = 9750, IQR = 2520
//   Nedre grense = 7230 - 1,5 * 2520 = 3450
//   Øvre grense  = 9750 + 1,5 * 2520 = 13530
public class PriceCalculatorTests
{
    private static Listing Sold(Condition condition, int soldPrice) => new()
    {
        Title = "Solgt vare",
        Condition = condition,
        Price = soldPrice,
        SoldPrice = soldPrice,
        Status = ListingStatus.Sold
    };

    private static List<Listing> SixSales() =>
    [
        Sold(Condition.Good, 4200),
        Sold(Condition.Used, 3850),
        Sold(Condition.Good, 5544),
        Sold(Condition.Defect, 2700),
        Sold(Condition.New, 10000),
        Sold(Condition.LikeNew, 9350)
    ];

    [Fact]
    public void NormalizedSoldPrices_DividesSoldPriceByConditionFactor()
    {
        var normalized = PriceCalculator.NormalizedSoldPrices(SixSales()).OrderBy(p => p).ToList();

        double[] expected = [6000, 7000, 7920, 9000, 10000, 11000];
        Assert.Equal(expected.Length, normalized.Count);
        for (var i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], normalized[i], precision: 6);
    }

    // Påkrevd: prisforslaget blir riktig, inkludert normalisering og avrunding
    [Theory]
    [InlineData(Condition.New, 8460)]      // 8460 * 1,00 = 8460
    [InlineData(Condition.LikeNew, 7190)]  // 8460 * 0,85 = 7191 -> rundes ned
    [InlineData(Condition.Good, 5920)]     // 8460 * 0,70 = 5922 -> rundes ned
    [InlineData(Condition.Used, 4650)]     // 8460 * 0,55 = 4653 -> rundes ned
    [InlineData(Condition.Defect, 2540)]   // 8460 * 0,30 = 2538 -> rundes opp
    public void SuggestPrice_IsMedianTimesFactor_RoundedToNearest10(Condition condition, int expected)
    {
        Assert.Equal(expected, PriceCalculator.SuggestPrice(SixSales(), condition));
    }

    // Påkrevd: færre enn 3 salg gir «ikke nok data»
    [Fact]
    public void SuggestPrice_FewerThanThreeSales_ReturnsNull()
    {
        var twoSales = SixSales().Take(2);

        Assert.Null(PriceCalculator.SuggestPrice(twoSales, Condition.Good));
    }

    [Fact]
    public void SuggestPrice_ExactlyThreeSales_ReturnsSuggestion()
    {
        // Normalisert: 6000, 7000, 7920 -> median 7000 -> 7000 * 0,70 = 4900
        var threeSales = SixSales().Take(3);

        Assert.Equal(4900, PriceCalculator.SuggestPrice(threeSales, Condition.Good));
    }

    // Påkrevd: Lav, Normal og Høy
    [Theory]
    [InlineData(Condition.Used, 1500, PriceAssessment.Low)]      // 1500 / 0,55 = 2727 < 3450
    [InlineData(Condition.Good, 6000, PriceAssessment.Normal)]   // 6000 / 0,70 = 8571
    [InlineData(Condition.Good, 10000, PriceAssessment.High)]    // 10000 / 0,70 = 14286 > 13530
    [InlineData(Condition.New, 3500, PriceAssessment.Normal)]    // like over nedre grense (3450)
    [InlineData(Condition.New, 3400, PriceAssessment.Low)]       // like under nedre grense
    [InlineData(Condition.New, 13500, PriceAssessment.Normal)]   // like under øvre grense (13530)
    [InlineData(Condition.New, 13600, PriceAssessment.High)]     // like over øvre grense
    public void AssessPrice_ComparesNormalizedPriceWithIqrFences(
        Condition condition, int price, PriceAssessment expected)
    {
        Assert.Equal(expected, PriceCalculator.AssessPrice(SixSales(), condition, price));
    }

    // Samme pris kan være normal for en ny vare og høy for en defekt, fordi prisen normaliseres
    [Fact]
    public void AssessPrice_SamePrice_DependsOnCondition()
    {
        Assert.Equal(PriceAssessment.Normal, PriceCalculator.AssessPrice(SixSales(), Condition.New, 9000));
        Assert.Equal(PriceAssessment.High, PriceCalculator.AssessPrice(SixSales(), Condition.Defect, 9000)); // 30000
    }

    // Påkrevd: Ukjent med færre enn 5 salg
    [Fact]
    public void AssessPrice_FewerThanFiveSales_ReturnsUnknown()
    {
        var fourSales = SixSales().Take(4);

        Assert.Equal(PriceAssessment.Unknown, PriceCalculator.AssessPrice(fourSales, Condition.Good, 6000));
    }

    [Fact]
    public void AssessPrice_ExactlyFiveSales_GivesAssessment()
    {
        var fiveSales = SixSales().Take(5);

        Assert.Equal(PriceAssessment.Normal, PriceCalculator.AssessPrice(fiveSales, Condition.Good, 6000));
    }

    // Påkrevd: gjennomsnittlig rabatt bruker opprinnelig pris når prisen er endret
    [Fact]
    public void AverageDiscountPercent_UsesFirstOldPrice_WhenPriceWasChanged()
    {
        // Lagt ut for 5000, satt ned til 4500 og så 4200, solgt for 4000.
        // Rabatt fra opprinnelig pris: (5000 - 4000) / 5000 = 20 %  (ikke 4,76 % fra 4200)
        var listing = Sold(Condition.Good, 4000);
        listing.Price = 4200;
        // Lagt inn i feil rekkefølge med vilje: første endring skal finnes ut fra dato
        listing.PriceChanges.Add(new PriceChange
            { OldPrice = 4500, NewPrice = 4200, ChangedDate = new DateTime(2026, 3, 10) });
        listing.PriceChanges.Add(new PriceChange
            { OldPrice = 5000, NewPrice = 4500, ChangedDate = new DateTime(2026, 3, 1) });

        Assert.Equal(5000, PriceCalculator.OriginalPrice(listing));
        Assert.Equal(20.0, PriceCalculator.AverageDiscountPercent([listing])!.Value, precision: 6);
    }

    [Fact]
    public void AverageDiscountPercent_UsesPrice_WhenPriceWasNeverChanged()
    {
        // Lagt ut for 4000, solgt for 3000: (4000 - 3000) / 4000 = 25 %
        var listing = Sold(Condition.Good, 3000);
        listing.Price = 4000;

        Assert.Equal(25.0, PriceCalculator.AverageDiscountPercent([listing])!.Value, precision: 6);
    }

    [Fact]
    public void AverageDiscountPercent_IsAverageAcrossListings()
    {
        // 20 % (endret pris) og 10 % (uendret pris) -> snitt 15 %
        var changed = Sold(Condition.Good, 4000);
        changed.Price = 4500;
        changed.PriceChanges.Add(new PriceChange
            { OldPrice = 5000, NewPrice = 4500, ChangedDate = new DateTime(2026, 3, 1) });

        var unchanged = Sold(Condition.Used, 1800);
        unchanged.Price = 2000;

        Assert.Equal(15.0, PriceCalculator.AverageDiscountPercent([changed, unchanged])!.Value, precision: 6);
    }

    [Fact]
    public void AverageDiscountPercent_NoSales_ReturnsNull()
    {
        Assert.Null(PriceCalculator.AverageDiscountPercent([]));
    }

    [Fact]
    public void AverageDaysToSell_IsAverageOfSoldDateMinusCreatedDate()
    {
        // 4 dager og 10 dager -> snitt 7
        var fast = Sold(Condition.Good, 1000);
        fast.CreatedDate = new DateTime(2026, 5, 1);
        fast.SoldDate = new DateTime(2026, 5, 5);

        var slow = Sold(Condition.Good, 1000);
        slow.CreatedDate = new DateTime(2026, 5, 1);
        slow.SoldDate = new DateTime(2026, 5, 11);

        Assert.Equal(7.0, PriceCalculator.AverageDaysToSell([fast, slow])!.Value, precision: 6);
    }
}

using Bruktborsen.Api.Services;

namespace Bruktborsen.Tests;

// Håndregnede datasett. Statistics er ren matematikk, så testene trenger ingen database.
public class StatisticsTests
{
    // Normaliserte salgspriser fra eksempelet i dokumentasjonen (kapittel 3)
    private static readonly double[] SixPrices = [6000, 7000, 7920, 9000, 10000, 11000];

    // Påkrevd: median med oddetall antall verdier
    [Fact]
    public void Median_OddCount_ReturnsMiddleValue()
    {
        // Sortert: 6000, 7000, 7920, 9000, 10000 -> midterste er 7920
        double[] values = [9000, 6000, 10000, 7920, 7000];

        Assert.Equal(7920, Statistics.Median(values));
    }

    // Påkrevd: median med partall antall verdier
    [Fact]
    public void Median_EvenCount_ReturnsAverageOfTwoMiddleValues()
    {
        // De to midterste er 7920 og 9000 -> (7920 + 9000) / 2 = 8460
        Assert.Equal(8460, Statistics.Median(SixPrices));
    }

    [Fact]
    public void Median_SingleValue_ReturnsThatValue()
    {
        Assert.Equal(500, Statistics.Median([500]));
    }

    // Påkrevd: Q1 og Q3 stemmer med QUARTILE.INC
    [Fact]
    public void Quartile_MatchesExcelQuartileInc_ForSixValues()
    {
        // Posisjon = (n - 1) * p, regnet fra 0
        // Q1: 5 * 0,25 = 1,25 -> 7000 + 0,25 * (7920 - 7000) = 7230
        // Q3: 5 * 0,75 = 3,75 -> 9000 + 0,75 * (10000 - 9000) = 9750
        Assert.Equal(7230, Statistics.Quartile(SixPrices, 1), precision: 6);
        Assert.Equal(9750, Statistics.Quartile(SixPrices, 3), precision: 6);
    }

    [Fact]
    public void Quartile_MatchesExcelQuartileInc_ForUnsortedEightValues()
    {
        // Sortert: 1, 2, 4, 7, 8, 9, 10, 12
        // Q1: 7 * 0,25 = 1,75 -> 2 + 0,75 * (4 - 2) = 3,5
        // Q3: 7 * 0,75 = 5,25 -> 9 + 0,25 * (10 - 9) = 9,25
        // Excel: =QUARTILE.INC({1;2;4;7;8;9;10;12};1) = 3,5 og ;3) = 9,25
        double[] values = [7, 1, 12, 4, 9, 2, 10, 8];

        Assert.Equal(3.5, Statistics.Quartile(values, 1), precision: 6);
        Assert.Equal(9.25, Statistics.Quartile(values, 3), precision: 6);
    }

    [Fact]
    public void Quartile_Two_EqualsMedian()
    {
        Assert.Equal(Statistics.Median(SixPrices), Statistics.Quartile(SixPrices, 2), precision: 6);
    }

    [Fact]
    public void SampleStandardDeviation_UsesNMinusOne()
    {
        // Snitt = 5. Kvadratavvik: 9 + 1 + 1 + 1 + 0 + 0 + 4 + 16 = 32
        // Utvalg (n - 1): rot(32 / 7) = 2,13809. Populasjon (n) ville gitt nøyaktig 2.
        double[] values = [2, 4, 4, 4, 5, 5, 7, 9];

        Assert.Equal(2.13809, Statistics.SampleStandardDeviation(values), precision: 5);
    }

    [Fact]
    public void Median_EmptyList_Throws()
    {
        Assert.Throws<ArgumentException>(() => Statistics.Median([]));
    }
}

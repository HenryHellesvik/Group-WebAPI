namespace Bruktborsen.Api.Services;

// Ren matematikk etter definisjonene i oppgaven. Ingen database, så den er lett å teste.
public static class Statistics
{
    public static double Mean(IEnumerable<double> values) => Sorted(values).Average();

    // Midterste verdi. Ved partall antall brukes snittet av de to midterste.
    public static double Median(IEnumerable<double> values)
    {
        var sorted = Sorted(values);
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    // Lineær interpolasjon, samme metode som Excel QUARTILE.INC.
    // quartile: 0 = min, 1 = Q1, 2 = median, 3 = Q3, 4 = maks
    public static double Quartile(IEnumerable<double> values, int quartile)
    {
        if (quartile is < 0 or > 4)
            throw new ArgumentOutOfRangeException(nameof(quartile), "Kvartil må være mellom 0 og 4.");

        var sorted = Sorted(values);
        var position = (sorted.Count - 1) * quartile / 4.0;
        var lower = (int)Math.Floor(position);
        var fraction = position - lower;

        return lower + 1 < sorted.Count
            ? sorted[lower] + fraction * (sorted[lower + 1] - sorted[lower])
            : sorted[lower];
    }

    // Utvalgsstandardavvik (n − 1)
    public static double SampleStandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2)
            throw new ArgumentException("Standardavvik krever minst to verdier.", nameof(values));

        var mean = list.Average();
        var sumOfSquares = list.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumOfSquares / (list.Count - 1));
    }

    private static List<double> Sorted(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
            throw new ArgumentException("Listen kan ikke være tom.", nameof(values));
        return sorted;
    }
}
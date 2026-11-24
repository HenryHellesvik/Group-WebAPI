using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services;

// TODO: erstattes med ekte beregninger (Q1, Q3 og IQR på normaliserte salgspriser)
public class AnalyticsService : IAnalyticsService
{
    public Task<PriceAssessment> AssessPriceAsync(int categoryId, Condition condition, int price) =>
        Task.FromResult(PriceAssessment.Unknown);
}
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services;

// Utvides med prisforslag, kategoristatistikk osv. når analysemodulen lages
public interface IAnalyticsService
{
    Task<PriceAssessment> AssessPriceAsync(int categoryId, Condition condition, int price);
}
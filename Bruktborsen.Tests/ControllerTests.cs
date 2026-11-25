using System.Net;
using System.Net.Http.Json;

namespace Bruktborsen.Tests;

// Controller-tester som sjekker statuskoder. Kallene går gjennom hele API-et
// (ruting, [Authorize], validering, service og GlobalExceptionHandler).
[Collection(ApiCollection.Name)]
public class ControllerTests(ApiFactory factory)
{
    private sealed record IdDto(int Id);

    private async Task<int> FirstCategoryIdAsync()
    {
        var categories = await factory.CreateClient().GetFromJsonAsync<List<IdDto>>("/api/categories");
        return categories!.First().Id;
    }

    private async Task<object> ListingRequestAsync(int price = 1000) => new
    {
        title = "Testannonse fra controller-test",
        description = "Laget av en test",
        price,
        condition = "Good",
        categoryId = await FirstCategoryIdAsync()
    };

    // Oppretter en annonse som selger og returnerer id-en
    private async Task<int> CreateListingAsync(HttpClient seller, int price = 1000)
    {
        var response = await seller.PostAsJsonAsync("/api/listings", await ListingRequestAsync(price));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    private static Task<HttpResponseMessage> BidAsync(HttpClient buyer, int listingId, int amount) =>
        buyer.PostAsJsonAsync($"/api/listings/{listingId}/offers", new { amount });

    // ---------- ListingsController ----------

    [Fact]
    public async Task GetListings_Anonymous_Returns200()
    {
        var response = await factory.CreateClient().GetAsync("/api/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetListing_UnknownId_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/listings/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetListings_InvalidSort_Returns400()
    {
        var response = await factory.CreateClient().GetAsync("/api/listings?sort=cheapest");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/listings", await ListingRequestAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_LoggedIn_Returns201WithLocation()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);

        var response = await ola.PostAsJsonAsync("/api/listings", await ListingRequestAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateListing_InvalidBody_Returns400()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);

        // Tom tittel og pris 0 bryter data annotations på ListingRequestDto
        var response = await ola.PostAsJsonAsync("/api/listings", new
        {
            title = "",
            price = 0,
            condition = "Good",
            categoryId = await FirstCategoryIdAsync()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateListing_ByOtherUser_Returns403()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var kari = await factory.CreateClientForAsync(ApiFactory.Kari);
        var listingId = await CreateListingAsync(ola);

        var response = await kari.PutAsJsonAsync($"/api/listings/{listingId}", await ListingRequestAsync(500));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteListing_BySeller_Returns204_AndListingStillExistsAsRemoved()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var listingId = await CreateListingAsync(ola);

        var response = await ola.DeleteAsync($"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Annonsen er ikke slettet, bare satt til Removed
        var after = await ola.GetAsync($"/api/listings/{listingId}");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.Contains("\"Removed\"", await after.Content.ReadAsStringAsync());
    }

    // ---------- OffersController ----------

    [Fact]
    public async Task CreateOffer_WithoutToken_Returns401()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var listingId = await CreateListingAsync(ola);

        var response = await BidAsync(factory.CreateClient(), listingId, 800);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ValidBid_Returns201()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var kari = await factory.CreateClientForAsync(ApiFactory.Kari);
        var listingId = await CreateListingAsync(ola);

        var response = await BidAsync(kari, listingId, 800);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_OnOwnListing_Returns400()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var listingId = await CreateListingAsync(ola);

        var response = await BidAsync(ola, listingId, 800);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_BelowHalfPrice_Returns400()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var kari = await factory.CreateClientForAsync(ApiFactory.Kari);
        var listingId = await CreateListingAsync(ola, price: 1000);

        var response = await BidAsync(kari, listingId, 499);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_SecondPendingBidFromSameBuyer_Returns409()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var kari = await factory.CreateClientForAsync(ApiFactory.Kari);
        var listingId = await CreateListingAsync(ola);
        await BidAsync(kari, listingId, 800);

        var response = await BidAsync(kari, listingId, 900);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AcceptOffer_ByNonSeller_Returns403_AndBySeller_Returns200()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);
        var kari = await factory.CreateClientForAsync(ApiFactory.Kari);
        var per = await factory.CreateClientForAsync(ApiFactory.Per);
        var listingId = await CreateListingAsync(ola);
        var offerId = (await (await BidAsync(kari, listingId, 800)).Content.ReadFromJsonAsync<IdDto>())!.Id;

        var byOther = await per.PostAsync($"/api/offers/{offerId}/accept", null);
        var bySeller = await ola.PostAsync($"/api/offers/{offerId}/accept", null);

        Assert.Equal(HttpStatusCode.Forbidden, byOther.StatusCode);
        Assert.Equal(HttpStatusCode.OK, bySeller.StatusCode);

        // Annonsen er nå solgt, så nye bud avvises med 409
        var bidOnSold = await BidAsync(per, listingId, 900);
        Assert.Equal(HttpStatusCode.Conflict, bidOnSold.StatusCode);
    }

    // ---------- CategoriesController ----------

    [Fact]
    public async Task CreateCategory_AsUser_Returns403()
    {
        var ola = await factory.CreateClientForAsync(ApiFactory.Ola);

        var response = await ola.PostAsJsonAsync("/api/categories", new { name = "Bøker" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_Returns201_AndDeleteReturns204()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.Admin);

        var created = await admin.PostAsJsonAsync("/api/categories", new { name = "Hage og uterom" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var id = (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;
        var deleted = await admin.DeleteAsync($"/api/categories/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithListings_Returns409()
    {
        var admin = await factory.CreateClientForAsync(ApiFactory.Admin);

        // Første seed-kategori har annonser
        var response = await admin.DeleteAsync($"/api/categories/{await FirstCategoryIdAsync()}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- AnalyticsController ----------

    [Fact]
    public async Task PriceSuggestion_WithoutToken_Returns401_AndLoggedIn_Returns200()
    {
        var url = $"/api/analytics/price-suggestion?categoryId={await FirstCategoryIdAsync()}&condition=Good";

        var anonymous = await factory.CreateClient().GetAsync(url);
        var loggedIn = await (await factory.CreateClientForAsync(ApiFactory.Ola)).GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
    }
}

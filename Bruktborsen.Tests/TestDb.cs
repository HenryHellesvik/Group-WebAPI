using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests;

// SQLite i minnet: ekte relasjonsdatabase med transaksjoner, men ingen fil på disk.
// Databasen lever så lenge tilkoblingen er åpen.
public sealed class TestDb : IDisposable
{
    public const string SellerId = "seller";
    public const string BuyerId = "buyer";
    public const string OtherBuyerId = "other-buyer";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public int CategoryId { get; }

    public TestDb()
    {
        _connection.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();

        db.Users.AddRange(
            NewUser(SellerId, "Selger Selgersen"),
            NewUser(BuyerId, "Kjøper Kjøpersen"),
            NewUser(OtherBuyerId, "Annen Kjøper"));

        var category = new Category { Name = "Elektronikk" };
        db.Categories.Add(category);
        db.SaveChanges();

        CategoryId = category.Id;
    }

    // Ny context mot samme database, slik at testene leser det som faktisk er lagret.
    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);

    public int AddListing(int price = 1000, ListingStatus status = ListingStatus.Active)
    {
        using var db = CreateContext();
        var listing = new Listing
        {
            Title = "Testannonse",
            Price = price,
            Condition = Condition.Good,
            Status = status,
            CategoryId = CategoryId,
            SellerId = SellerId
        };
        db.Listings.Add(listing);
        db.SaveChanges();
        return listing.Id;
    }

    public int AddOffer(int listingId, string buyerId, int amount, OfferStatus status = OfferStatus.Pending)
    {
        using var db = CreateContext();
        var offer = new Offer { ListingId = listingId, BuyerId = buyerId, Amount = amount, Status = status };
        db.Offers.Add(offer);
        db.SaveChanges();
        return offer.Id;
    }

    private static ApplicationUser NewUser(string id, string fullName) => new()
    {
        Id = id,
        UserName = $"{id}@test.no",
        Email = $"{id}@test.no",
        FullName = fullName
    };

    public void Dispose() => _connection.Dispose();
}

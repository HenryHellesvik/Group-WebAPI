using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";
    public const string DefaultPassword = "Passord1!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Kjører migrasjoner automatisk ved oppstart
        await context.Database.MigrateAsync();

        // Roller og brukere 
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(userManager, "admin@brukt.no", "Admin Adminsen", AdminRole);
        var ola  = await EnsureUserAsync(userManager, "ola@brukt.no",  "Ola Nordmann", UserRole);
        var kari = await EnsureUserAsync(userManager, "kari@brukt.no", "Kari Nordmann", UserRole);
        var per  = await EnsureUserAsync(userManager, "per@brukt.no",  "Per Pettersen", UserRole);

        // Er databasen allerede seedet, stopper vi her
        if (await context.Categories.AnyAsync()) return;

        
        
        //  Kategorier 
        var el = new Category { Name = "Elektronikk" };
        var sy = new Category { Name = "Sykkel" };
        var mo = new Category { Name = "Møbler" };
        var fr = new Category { Name = "Friluft" };
        context.Categories.AddRange(el, sy, mo, fr);

        var now = DateTime.UtcNow.Date;
        var listings = new List<Listing>();

        
        
        // ---------- Hjelpefunksjoner ----------
        // earlierPrices = tidligere priser før nåværende pris (gir PriceChange-rader)
        Listing Create(Category cat, ApplicationUser seller, string title, int price, Condition cond,
                       int daysAgo, int[] earlierPrices, DateTime? endDate)
        {
            var created = now.AddDays(-daysAgo);
            var listing = new Listing
            {
                Title = title,
                Description = $"{title}. Tilstand: {ConditionText(cond)}. Kan hentes eller sendes.",
                Price = price,
                Condition = cond,
                Status = ListingStatus.Active,
                ImageUrl = $"https://picsum.photos/seed/brukt{listings.Count + 1}/600/400",
                CreatedDate = created,
                Category = cat,
                SellerId = seller.Id
            };

            // Prisendringer fordeles jevnt mellom opprettelse og salg (eller i dag)
            var prices = earlierPrices.Append(price).ToArray();
            var step = ((endDate ?? now) - created).TotalDays / prices.Length;
            for (var i = 0; i < prices.Length - 1; i++)
            {
                listing.PriceChanges.Add(new PriceChange
                {
                    OldPrice = prices[i],
                    NewPrice = prices[i + 1],
                    ChangedDate = created.AddDays(step * (i + 1))
                });
            }

            listings.Add(listing);
            return listing;
        }

        Listing Active(Category cat, ApplicationUser seller, string title, int price, Condition cond,
                       int daysAgo, params int[] earlierPrices) =>
            Create(cat, seller, title, price, cond, daysAgo, earlierPrices, null);

        Listing Removed(Category cat, ApplicationUser seller, string title, int price, Condition cond, int daysAgo)
        {
            var listing = Create(cat, seller, title, price, cond, daysAgo, [], null);
            listing.Status = ListingStatus.Removed;
            return listing;
        }

        Listing Sold(Category cat, ApplicationUser seller, ApplicationUser buyer, string title, int price,
                     int soldPrice, Condition cond, int daysAgo, int daysToSell, params int[] earlierPrices)
        {
            var soldDate = now.AddDays(-daysAgo + daysToSell);
            var listing = Create(cat, seller, title, price, cond, daysAgo, earlierPrices, soldDate);
            listing.Status = ListingStatus.Sold;
            listing.SoldDate = soldDate;
            listing.SoldPrice = soldPrice;
            listing.Offers.Add(new Offer
            {
                BuyerId = buyer.Id,
                Amount = soldPrice,
                Status = OfferStatus.Accepted,
                CreatedDate = soldDate.AddDays(-1)
            });
            return listing;
        }

        void Bid(Listing listing, ApplicationUser buyer, int amount, OfferStatus status, int daysAfterCreated = 1) =>
            listing.Offers.Add(new Offer
            {
                BuyerId = buyer.Id,
                Amount = amount,
                Status = status,
                CreatedDate = listing.CreatedDate.AddDays(daysAfterCreated)
            });

        // Elektronikk (9 solgt) 
        var iphone13 = Sold(el, ola,  kari, "iPhone 13 128GB", 4500, 4200, Condition.Good, 120, 6);
        Sold(el, kari, per,  "Samsung Galaxy S22", 5200, 5000, Condition.LikeNew, 95, 4);
        Sold(el, per,  ola,  "iPad Air 4. generasjon", 3800, 3500, Condition.Good, 200, 10);
        Sold(el, ola,  per,  "Sony WH-1000XM4 hodetelefoner", 2400, 2200, Condition.LikeNew, 60, 3);
        var macbook = Sold(el, kari, ola, "MacBook Air M1 2020", 4500, 4000, Condition.Used, 150, 21, 5000);
        Sold(el, per,  kari, "Nintendo Switch med to kontrollere", 2200, 2000, Condition.Good, 80, 5);
        Sold(el, ola,  kari, "Apple Watch SE", 1500, 1300, Condition.Used, 45, 7);
        var ps5 = Sold(el, kari, per, "PlayStation 5 Disc Edition", 4800, 4500, Condition.LikeNew, 30, 2);
        Sold(el, per,  ola,  "Kindle Paperwhite med sprukket skjerm", 400, 300, Condition.Defect, 250, 14);

        var iphone12 = Active(el, per, "iPhone 12 64GB", 25000, Condition.Good, 10); // Åpenbart for høy pris
        var pixel    = Active(el, ola, "Google Pixel 7", 3500, Condition.Good, 40, 4200, 3800);
        var bose     = Active(el, kari, "Bose QuietComfort 45", 2000, Condition.LikeNew, 15);
        Active(el, per, "Samsung 55\" 4K TV", 3000, Condition.Used, 25);
        var xbox     = Active(el, ola, "Xbox Series S", 2900, Condition.New, 5);
        Removed(el, kari, "Canon EOS 2000D kamera", 2500, Condition.Used, 70);

        // Sykkel (7 solgt) 
        var trekFx = Sold(sy, ola, per, "Trek FX 3 hybridsykkel", 6500, 6000, Condition.Good, 180, 30, 7500);
        Sold(sy, kari, ola,  "Specialized Rockhopper terrengsykkel", 8500, 8000, Condition.LikeNew, 140, 9);
        Sold(sy, per,  kari, "Merida Scultura racer", 5500, 5000, Condition.Used, 300, 40);
        Sold(sy, ola,  kari, "Cube Aim Pro", 7000, 6500, Condition.Good, 110, 12);
        Sold(sy, kari, per,  "Giant Escape 2 (ubrukt)", 9500, 9000, Condition.New, 75, 8);
        Sold(sy, per,  ola,  "Bianchi Via Nirone 7", 9000, 8500, Condition.LikeNew, 55, 6);
        Sold(sy, ola,  per,  "Scott Aspect med knekt gir", 3000, 2800, Condition.Defect, 210, 18);

        Active(sy, per, "Trek Marlin 5 – må bort raskt", 2500, Condition.Good, 3); // Lav pris
        var cannondale = Active(sy, kari, "Cannondale Quick 4", 7900, Condition.LikeNew, 35, 8500);
        Active(sy, ola, "Kona Dew", 5000, Condition.Used, 20);
        Active(sy, per, "Specialized Allez", 6500, Condition.Good, 12);
        var ghost = Active(sy, kari, "Ghost Kato 2.9", 9400, Condition.New, 8);
        Removed(sy, ola, "Gammel BMX", 800, Condition.Used, 90);

        // Møbler (6 solgt)
        Sold(mo, kari, ola, "IKEA Ektorp sofa", 2500, 2200, Condition.Good, 160, 11);
        var spisebord = Sold(mo, per, kari, "Spisebord i eik", 3500, 3200, Condition.LikeNew, 130, 25, 4000);
        Sold(mo, ola,  per, "Kontorstol med armlener", 1800, 1600, Condition.Used, 70, 5);
        Sold(mo, kari, per, "Hemnes bokhylle", 2000, 1800, Condition.Good, 100, 9);
        Sold(mo, per,  ola, "Lenestol i skinn", 2200, 2000, Condition.Used, 220, 15);
        Sold(mo, ola, kari, "Salongbord (ubrukt, i eske)", 3000, 2800, Condition.New, 40, 4);

        Active(mo, ola, "Malm kommode", 1500, Condition.Good, 50, 1800);
        var tvBenk = Active(mo, kari, "TV-benk i hvit", 2500, Condition.LikeNew, 22);
        var seng   = Active(mo, per, "Seng 160x200 med madrass", 2000, Condition.Used, 18);
        Active(mo, ola, "Skrivebord 140 cm", 1800, Condition.Good, 9);
        Active(mo, kari, "4 spisestoler (trenger nytt trekk)", 600, Condition.Defect, 30);
        Removed(mo, per, "Vitrineskap", 1200, Condition.Good, 60);

        // Friluft (bare 2 solgt → «ikke nok data») 
        Sold(fr, per,  ola, "Fjellreven-telt for 2", 2500, 2300, Condition.Good, 90, 10);
        Sold(fr, kari, per, "Sovepose -10 °C", 800, 700, Condition.Used, 65, 6);

        var osprey = Active(fr, ola, "Osprey Atmos 65 sekk", 1500, Condition.LikeNew, 14);
        Active(fr, kari, "Madshus langrennsski", 1200, Condition.Good, 28);
        Active(fr, per, "Fjellsko str. 43", 700, Condition.Used, 11);
        Active(fr, ola, "Truger (ubrukte)", 900, Condition.New, 6);
        var kajakk = Active(fr, kari, "Havkajakk med åre", 6000, Condition.Good, 45, 7000);

        // Ekstra bud (alle statuser) 
        // Avslåtte bud på annonser som senere ble solgt til noen andre
        Bid(iphone13,  per,  3500, OfferStatus.Rejected);
        Bid(macbook,   per,  3000, OfferStatus.Rejected);
        Bid(ps5,       ola,  4000, OfferStatus.Rejected);
        Bid(trekFx,    kari, 5000, OfferStatus.Rejected);
        Bid(spisebord, ola,  2500, OfferStatus.Rejected);

        // Ventende bud på aktive annonser
        Bid(pixel,      kari, 3000, OfferStatus.Pending, 30);
        Bid(bose,       per,  1600, OfferStatus.Pending);
        Bid(iphone12,   ola, 13000, OfferStatus.Pending);
        Bid(cannondale, ola,  7000, OfferStatus.Pending, 20);
        Bid(kajakk,     per,  5000, OfferStatus.Pending, 35);

        // Trukket og avslått på aktive annonser
        Bid(tvBenk, ola,  1500, OfferStatus.Withdrawn);
        Bid(xbox,   per,  2000, OfferStatus.Withdrawn);
        Bid(seng,   kari, 1000, OfferStatus.Rejected);

        context.Listings.AddRange(listings);

        // Favoritter 
        context.Favorites.AddRange(
            new Favorite { Listing = cannondale, UserId = ola.Id },
            new Favorite { Listing = kajakk,     UserId = ola.Id },
            new Favorite { Listing = iphone12,   UserId = ola.Id },
            new Favorite { Listing = pixel,      UserId = kari.Id },
            new Favorite { Listing = bose,       UserId = per.Id },
            new Favorite { Listing = osprey,     UserId = kari.Id },
            new Favorite { Listing = ghost,      UserId = per.Id },
            new Favorite { Listing = tvBenk,     UserId = per.Id }
        );

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Kunne ikke opprette {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        return user;
    }

    private static string ConditionText(Condition condition) => condition switch
    {
        Condition.New => "ny",
        Condition.LikeNew => "som ny",
        Condition.Good => "god",
        Condition.Used => "brukt",
        Condition.Defect => "defekt",
        _ => ""
    };
}
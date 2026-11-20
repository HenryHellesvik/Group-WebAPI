using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Data;

// IdentityDbContext<ApplicationUser> gir oss alle Identity-tabellene (AspNetUsers, AspNetRoles osv.)
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<PriceChange> PriceChanges => Set<PriceChange>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Lagre enums som tekst ("Active") i stedet for tall (0)
        builder.Entity<Listing>().Property(l => l.Condition).HasConversion<string>();
        builder.Entity<Listing>().Property(l => l.Status).HasConversion<string>();
        builder.Entity<Offer>().Property(o => o.Status).HasConversion<string>();
        
        builder.Entity<Listing>()
            .HasOne(l => l.Category)
            .WithMany(c => c.Listings)
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Listing>()
            .HasOne(l => l.Seller)
            .WithMany(u => u.Listings)
            .HasForeignKey(l => l.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<Offer>()
            .HasOne(o => o.Buyer)
            .WithMany(u => u.Offers)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Offer>()
            .HasOne(o => o.Listing)
            .WithMany(l => l.Offers)
            .HasForeignKey(o => o.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PriceChange>()
            .HasOne(p => p.Listing)
            .WithMany(l => l.PriceChanges)
            .HasForeignKey(p => p.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Favorite>()
            .HasIndex(f => new { f.UserId, f.ListingId })
            .IsUnique();

        builder.Entity<Favorite>()
            .HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
            .HasOne(f => f.Listing)
            .WithMany(l => l.Favorites)
            .HasForeignKey(f => f.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
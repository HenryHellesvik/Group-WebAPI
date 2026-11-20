namespace Bruktborsen.Api.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    public List<Listing> Listings { get; set; } = new();
    public List<Offer> Offers { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}
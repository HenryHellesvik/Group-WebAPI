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
        
        // Migrerer ved oppstart
        await context.Database.MigrateAsync();
        
        // Roller / Brukere
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        
        await EnsureUserAsync(userManager, "admin@brukt.no", "Admin Adminsen", AdminRole);
        var ole  = await EnsureUserAsync(userManager, "ola@brukt.no",  "Ola Nordman", UserRole);
        var dole = await EnsureUserAsync(userManager, "kari@brukt.no", "Kari Nordman", UserRole);
        var doffen  = await EnsureUserAsync(userManager, "per@brukt.no",  "Per Pettersen", UserRole);

        // Er databasen allerede seedet, stopper vi her
        if (await context.Categories.AnyAsync()) return;
        
        
        await context.SaveChangesAsync();
    }
    
    // Hjelpefunksjon for skapelse av brukere
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
}


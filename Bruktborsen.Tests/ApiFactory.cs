using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bruktborsen.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bruktborsen.Tests;

// Starter hele API-et i minnet, med ekte ruting, autentisering og feilhåndtering.
// Databasen byttes ut med SQLite i minnet, og DbSeeder fyller den ved oppstart
// på samme måte som når API-et kjøres vanlig.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Passord1!";
    public const string Admin = "admin@brukt.no";
    public const string Ola = "ola@brukt.no";
    public const string Kari = "kari@brukt.no";
    public const string Per = "per@brukt.no";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));
        });
    }

    // Klient som er logget inn som en av seed-brukerne
    public async Task<HttpClient> CreateClientForAsync(string email)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }

    private sealed record LoginResponse(string AccessToken);
}

// Én felles instans for alle controller-testene, så API-et bare startes og seedes én gang
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

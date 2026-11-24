# Bruktbørsen API

REST-API for en markedsplass for brukte ting. Brukere kan legge ut annonser, gi og akseptere bud og lagre favoritter. Analysemodulen bruker historiske salg til å foreslå pris, avdekke avvikende priser og vise statistikk per kategori.

Laget med ASP.NET Core Web API (.NET 10), EF Core med SQLite og ASP.NET Core Identity.

## Kom i gang

**Krav:** .NET 10 SDK

1. Gå til API-prosjektet:
   ```bash
   cd Bruktborsen.Api
   ```
2. Start API-et:
   ```bash
   dotnet run
   ```
   Databasen (`bruktborsen.db`) opprettes, migreres og fylles med testdata automatisk ved første oppstart.
3. Åpne dokumentasjonen i nettleseren: http://localhost:5242/scalar

**Logg inn:** Kall `POST /api/auth/login` med e-post og passord fra tabellen under. Bruk `accessToken` fra svaret som `Authorization: Bearer <token>`.

**Kjør testene** (fra rotmappen):
```bash
dotnet test
```

**Nullstill databasen:** Slett `bruktborsen.db` og start API-et på nytt.

**Blazor-klienten:** Legg inn klientens adresse under `Cors:AllowedOrigins` i `appsettings.json`.

## Testbrukere

| E-post          | Passord     | Rolle |
|-----------------|-------------|-------|
| admin@brukt.no  | Passord1! | Admin |
| ola@brukt.no    | Passord1! | User  |
| kari@brukt.no   | Passord1! | User  |
| per@brukt.no    | Passord1! | User  |

Databasen migreres og seedes automatisk ved oppstart.

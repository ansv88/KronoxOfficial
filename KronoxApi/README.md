# KronoxApi – API för innehåll, sidor och administration

API:t exponerar endpoints för sidinnehåll, sektioner (intro, features, FAQ), dokument, navigation, medlemslogotyper, handlingsplan och utvecklingsförslag.
Säkerhet hanteras med API‑nyckel och rollbaserad auktorisering.

## Krav
- .NET 8 SDK
- SQL Server (lokal eller extern)
- Konfigurerad API‑nyckel (RequireApiKey)

## Konfiguration
- Anslutningssträng (ConnectionStrings:DefaultConnection) i appsettings.*.json.
- API‑nyckel för RequireApiKey (t.ex. X-API-Key). Se attributet `RequireApiKey` i projektet (definierar header och validering).
- Rate limiting policy "API" (EnableRateLimiting("API")).

## Databas
- Skapa/uppgradera DB:
  - Package Manager Console: `Update-Database`
  - CLI: `dotnet ef database update`
- Migrationer inkluderar bl.a. `AddActionPlanAndDevelopmentSuggestion` (ActionPlan och DevelopmentSuggestion-tabeller).

## Seeding
Seeding körs automatiskt vid uppstart (se `Program.cs` → `SeedAllAsync`). Det finns tre seedfiler med följande ansvar:

- `Data/Seed/StartupSeed.cs` (entrypoint)
  - Skapar grundroller (“Admin”, “Styrelse”, “Medlem”, “Ny användare”).
  - Skapar admin‑användare (läser lösenord från `Admin:Password` i konfigurationen).
  - Skapar/uppdaterar dokumentkategorier.
  - Anropar navigations- och innehållsseeding.

- `Data/Seed/NavigationSeed.cs`
  - Seedar standard‑navigation (sidspecifika poster, ordning/synlighet och systemposter).
  - Idempotent: skapar saknade poster och lämnar befintliga anpassningar i fred.

- `Data/Seed/ContentSeed.cs`
  - Skapar `ContentBlocks` med metadata per sida (intro‑sektion, `sectionConfig`), feature‑sektioner, FAQ (för “omsystemet”).
  - Lägger in/registrerar bilder och logotyper:
    - Featurebilder från `SeedAssets/FeatureImages` kopieras till `wwwroot/images/pages/home` och registreras i DB.
    - Medlemslogotyper hämtas från frontendens `KronoxFront/wwwroot/images/members` och registreras i DB med länkningar.
  - Skapar kontaktdata (postadress, kontaktpersoner, e‑postlistor).
  - Sidor som seedas (exempel): `home`, `omkonsortiet`, `visioner`, `dokument`, `omsystemet`, `kontaktaoss`, `forvaltning`, `medlemsnytt` (inkl. NavigationButtons där relevant).
  - Respekterar anpassningar: fyller endast på saknade delar (t.ex. lägger till `introSection` om den saknas).

Tips
- Vill du re‑seeda en enskild del, ta bort de berörda raderna i databasen (t.ex. `FeatureSections` för en `PageKey`) och starta om API:t.
- För admin‑användaren krävs att `Admin:Password` är satt i konfigurationen.

## Säkerhet
- API‑nyckel krävs globalt för kontroller märkta `[RequireApiKey]`.
- Rollen styrs med `[RequireRole(...)]` på skyddade endpoints (t.ex. PUT/GET authenticerat innehåll).
- Rate limiting via `[EnableRateLimiting("API")]`.

## API‑dokumentation (Swagger / OpenAPI)
- När API:t körs: öppna Swagger UI på `/swagger` (t.ex. https://localhost:5001/swagger).
- OpenAPI‑specen finns på `/swagger/v1/swagger.json`.

## Omfattning (områden)
README listar inte enskilda endpoints (för att undvika att de blir inaktuella). Använd Swagger för fullständig lista. Huvudområden i API:t:
- Sidinnehåll och metadata (Content)
- Intro‑ och feature‑sektioner (inkl. privat innehåll och kontaktpersoner)
- FAQ (sektioner + frågor/svar)
- Navigation och sidstatus
- Dokument och kategorier
- Nyheter
- Medlemslogotyper
- Handlingsplan
- Utvecklingsförslag (inlämning och administrativ hantering)
- Användar-/adminfunktioner (roller, godkännanden m.m.)

## Bygga och köra
- Visual Studio: Starta `KronoxApi` (IIS Express/Kestrel).
- dotnet CLI: `dotnet build` / `dotnet run --project KronoxApi`

## Loggning och fel
- ILogger används genomgående; fel loggas och returnerar 5xx med generiska meddelanden.
- Vid JSON-fel i controllers fångas undantag och loggas med detaljer.

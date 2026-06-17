# KronoxFront – Blazor Server frontend

Blazor Server (.NET 8) som konsumerar KronoxApi. Innehåll renderas dynamiskt via sidors sektionskonfiguration (banner, intro, feature, FAQ, dokument, nyheter, handlingsplan m.m.).
Admin‑gränssnittet hanterar sidor, navigation, sektioner, nyheter, dokument, logotyper, användare.

## Krav
- .NET 8 SDK
- Tillgängligt API (KronoxApi) med giltig API‑nyckel

## API‑anslutning
- HttpClientFactory-namn: `KronoxAPI` (konfigureras i `Program.cs`):
  - BaseAddress = API‑URL
  - Lägg till API‑nyckel i requestheader (t.ex. `X-API-Key`)
- Tjänsten `CmsService` sköter alla anrop, caching och synk mot API.

## Konfiguration (appsettings)

### Nyheter – förhandsvisningens längd
- Antal tecken som visas i nyheternas förhandsvisning (innan texten kortas av med "...") styrs centralt i `appsettings.json`: "NewsSettings": { "PreviewLength": 300 }
- Bindning sker i `Program.cs` via `builder.Services.Configure<NewsSettings>(...)` till klassen `Configuration/NewsSettings.cs`.
- Komponenten `Components/Shared/Content/NewsSection.razor` läser värdet med `IOptionsMonitor<NewsSettings>`, vilket innebär att ändringar i `appsettings.json` slår igenom **utan omstart** (vid nästa rendering av nyhetslistan).
- Standardvärde om sektionen saknas: `300` (default i `NewsSettings`).
- En enskild sida kan fortfarande override värdet per anrop, t.ex. `<NewsSection PreviewLength="500" ... />`. Sätts ingen parameter används värdet från `appsettings.json`.

### Maskerade länkar (Redirects)
- Maskerade navigeringslänkar mappas slug → mål‑URL i `appsettings.json`: "Redirects": { "manualen": "https://exempel.se/manual" }
- Endpointen `/go/{slug}` i `Program.cs` slår upp slug och gör en 302‑redirect till målet. I produktion krävs ett `https://`‑mål (annars blockeras det).
- Konfigurationen läses in vid uppstart, så **starta om appen** efter ändringar.
- Mer detaljerad info (admin‑UI, slug‑validering, felsökning av 404/400/302): se [`README-Navigeringsknappar.md`](README-Navigeringsknappar.md).

## Körning
- Visual Studio: starta `KronoxFront` (Blazor Server).
- dotnet CLI: `dotnet run --project KronoxFront`

## UI‑bibliotek och laddningskällor
All laddning sker i `Components/App.razor`.

- TinyMCE (lokalt)
  - Core: `/lib/tinymce/tinymce.min.js` (serveras från `wwwroot/lib/tinymce/`)
  - Konfiguration: `wwwroot/js/tinymce-config.js`
  - Initieras i admin‑editorer (Intro/Feature/FAQ) via JS‑hjälpare i `tinymce-config.js` och `wwwroot/js/main.js`.
- Bootstrap 5.3.6 (CDN)
  - CSS: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.6/dist/css/bootstrap.min.css`
  - JS bundle: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.6/dist/js/bootstrap.bundle.min.js`
- Font Awesome 6.7.2 (CDN)
  - CSS: `https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/css/all.min.css`
- Google Fonts (CDN)
  - Anaheim: `https://fonts.googleapis.com/...` och `https://fonts.gstatic.com/...`
- Egen CSS (lokalt)
  - `wwwroot/css/app.css`
  - `KronoxFront.styles.css` (genererad av projektet)

Offline/fallback: Vill du köra helt utan internet, lägg in Bootstrap/Font Awesome lokalt under `wwwroot/` och uppdatera länkarna i `App.razor` (ta bort SRI/crossorigin när du går över till lokala filer).

## Caching
- `CacheService` cachar bl.a. sidinnehåll, features, FAQ och handlingsplan per sida.
- Invalidering sker automatiskt vid sparning; manuellt via `InvalidateGroup/InvalidatePageCache` där relevant.

## Sidtyper och sektioner
- Sidor (exempel): `/`, `/omkonsortiet`, `/visioner`, `/omsystemet`, `/kontaktaoss`, `/medlemsnytt`, `/forvaltning`, `/forvnsg`, `/dokument`
- Varje sida styrs av `SectionConfigItem` i metadata (banner, intro, navigation buttons, feature, faq, dokument, nyheter, handlingsplan, utvecklingsförslag, kontaktformulär, medlemslogotyper).
- Intro/Feature/FAQ redigeras via admin och skrivs både till API och metadata för fallback.

## Admin
- Dashboard: `/admin/dashboard`
- Fasta sidor: `/admin/{PageKey}` (t.ex. `/admin/home`, `/admin/omkonsortiet`)
- Anpassade sidor: `/admin/page/{PageKey}` + översikt `/admin/pages`
- Andra vyer (urval): `users`, `navigation`, `memberlogos`, `news`, `documents/manage`, `actionplan`, `suggestions`

Editorerna (TinyMCE) initialiseras via wwwroot/js/tinymce-config.js.

## Bilder
- Bilduppladdning via `api/content/{pageKey}/images`.
- Hero-bild identifieras via alt‑text som börjar med `hero:`; sidorna hämtar den via helper‑metoder.
- `SyncImagesFromApiAsync()` kan hämta ner bilder/loggor till wwwroot för snabbare visning.

## Feature‑sektioner och privat innehåll
- Inloggade användare ser privat innehåll (HasPrivateContent/PrivateContent) samt kontaktpersoner.
- `CmsService` synkar feature‑sektioner: läser/skriv­er alla fält (privat innehåll, contactHeading, contactPersons) till API och metadata.

## Vanliga flöden
- Redigera sida: gå till `/admin/{PageKey}` eller `/admin/page/{PageKey}`, aktivera sektioner, redigera och spara. Cacher invalideras automatiskt.
- Ladda upp banner eller sektionsbild: editorerna visar uppladdare/bibliotek och uppdaterar metadata/DB.

## Felsökning
- Ser du gammalt innehåll? Rensa cache: spara igen via admin eller starta om appen.
- Feature‑sektioner saknar privata fält? Kontrollera att `CmsService.SavePageContentAsync` och `SaveFeatureSectionsAsync` körts och att API PUT lyckats.
- 404 vid direktbesök: sidan kan vara inaktiverad i navigationsinställningar. Komponent `RequirePageEnabled` blockerar.

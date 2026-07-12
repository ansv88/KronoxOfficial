# KronoxFront – Blazor Server frontend

Blazor Server (.NET 10) som konsumerar KronoxApi. Innehåll renderas dynamiskt via sidors sektionskonfiguration (banner, intro, feature, FAQ, dokument, nyheter, handlingsplan m.m.).
Admin‑gränssnittet hanterar sidor, navigation, sektioner, nyheter, dokument, logotyper, användare.

## Krav
- .NET 10 SDK
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
- Bilduppladdning via `api/content/{pageKey}/images`. Bildbiblioteket hanteras i admin under `/admin/images`.
- Bannerbild: sanningskälla är `PageImage.IsActive` – exakt en aktiv bild per sida. När en ny banner sätts som aktiv inaktiveras övriga automatiskt via API:t (`SetImageActive`). Sidorna hämtar den via extension-metoderna `PageContentViewModel.GetBannerUrl()` / `GetBannerAlt()` (se `Extensions/PageImageExtensions.cs`).
- Bilder som används på en sida kan inte raderas förrän de tagits bort/bytts ut där (referenskontroll server-side).
- `SyncImagesFromApiAsync()` kan hämta ner bilder/logotyper till wwwroot för snabbare visning.

## Dokument
- Delad modul `Components/Shared/Content/DocumentSection.razor` visar dokument som kort och återanvänds på flera sidor (t.ex. `/dokument`, `/forstyrelsen`, `/forvnsg`).
- Varje dokument har en huvudkategori (`MainCategoryId`) och kan ha flera underkategorier (`SubCategories` som ID-lista). Behörighet styrs server-side via huvudkategorins `AllowedRoles`.
- Parametrar (urval): `ShowSubcategories` (filter/sortering per underkategori), `ShowArchiveToggle` (admin kan visa/dölja arkiverade), `MaxDocumentsPerCategory`, `AutoDetectCategoryFromUrl` (av på "visa allt"-sidor som `/dokument`).
- Arkivering: dokument kan arkiveras (`IsArchived`, `ArchivedAt`, `ArchivedBy`). Arkiverade visas endast för admin och bara när "Visa arkiverade" är ibockad.
- Kategorier hanteras i admin under `/admin/documents/manage`; underkategorinamn hämtas via `CategoryService`.

## Feature‑sektioner och privat innehåll
- Inloggade användare ser privat innehåll (`HasPrivateContent`/`PrivateContent`) samt kontaktpersoner.
- Admin kan per sektion kryssa i **"Dölj den allmänna texten för inloggade"** (`HidePublicForMembers`). Då döljs den publika texten (`Content`) för inloggade, som istället bara ser medlemsinformationen. Oinloggade ser alltid den publika texten.
- Admin kan även kryssa i **"Dölj hela sektionen för inloggade"** (`HideEntireSectionForMembers`). Då renderas hela sektionen (rubrik, bild, text och avdelare) enbart för oinloggade besökare. Väljs detta döljs övriga medlemsinställningar i editorn för att undvika motstridiga val.
- Renderingslogiken finns i `Components/Shared/Content/FeatureSections.razor`: hela sektioner filtreras bort för inloggade via `VisibleSections` (`HideEntireSectionForMembers`), och `GetEnhancedContent` tar bort den publika texten när `isLoggedIn && HidePublicForMembers`.
- `CmsService` synkar feature‑sektioner: läser/skriv­er alla fält (privat innehåll, `HidePublicForMembers`, `HideEntireSectionForMembers`, contactHeading, contactPersons) till API och metadata.

## Vanliga flöden
- Redigera sida: gå till `/admin/{PageKey}` eller `/admin/page/{PageKey}`, aktivera sektioner, redigera och spara. Cacher invalideras automatiskt.
- Ladda upp banner eller sektionsbild: editorerna visar uppladdare/bibliotek och uppdaterar metadata/DB.

## Felsökning
- Ser du gammalt innehåll? Rensa cache: spara igen via admin eller starta om appen.
- Feature‑sektioner saknar privata fält? Kontrollera att `CmsService.SavePageContentAsync` och `SaveFeatureSectionsAsync` körts och att API PUT lyckats.
- 404 vid direktbesök: sidan kan vara inaktiverad i navigationsinställningar. Komponent `RequirePageEnabled` blockerar.

### reCAPTCHA v3 (registrering och kontakt)
- Frontend läser publik nyckel från `Recaptcha:SiteKey` (t.ex. via user-secrets eller `appsettings.json`): "Recaptcha": { "SiteKey": "din-site-key" }
- Sidorna `Registrera.razor` och kontaktformuläret laddar Googles v3-skript via JS-hjälparen `window.recaptchaHelper` i `wwwroot/js/main.js` (`recaptchaHelper.load(siteKey)`) och genererar en token per formulär med `recaptchaHelper.execute("register" | "contact")`.
- Token skickas med i request till API:t, som verifierar den server-side (se KronoxApi-README).
- Site key är publik och exponeras avsiktligt i klienten; håll endast secret key hemlig (den ligger i API:t).
- Obs: `ContactFormSection` genererar reCAPTCHA-token endast i sin inbyggda submit-väg (POST till `api/contact/send`). Om en förälder tar över via `OnFormSubmitted` måste den själv hämta och skicka en token (`recaptchaHelper.execute("contact")`).

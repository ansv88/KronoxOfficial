# Bra att ha – Vart saker ändras i kod (utanför admin)

Denna lista pekar ut koddelar som ofta efterfrågas men inte ligger i admin‑gränssnittet.

## 1) Lärosäten i formuläret för utvecklingsförslag
- Fil: `KronoxFront/Components/Shared/Forms/DevelopmentSuggestionForm.razor`
- Vad: Dropdown för Lärosäte/organisation är hårdkodad i `<select>`.
- Ändra: Lägg till/ta bort `<option>` här.
- API: POST till `api/developmentsuggestion` (se `DevelopmentSuggestionController`).

## 2) Svar/hantering av utvecklingsförslag (admin) – var ändrar jag texten?
- Adminvy: `KronoxFront/Components/Admin/SuggestionsAdmin.razor`
- “Kopiera text”: genereras av `GetSuggestionText(DevelopmentSuggestionViewModel suggestion)` och används för knappen “Kopiera text” (urklipp). Innehåller bl.a. Organisation, Avsändare, Datum, Behov, Nytta, Ytterligare info och Status.
- “Svara via e‑post”: mailets brödtext genereras av `GetEmailBody(DevelopmentSuggestionViewModel suggestion)`. Den:
  - URL‑kodas med `Uri.EscapeDataString(...)` och skickas via en `mailto:`‑länk.
  - Sätter ämnesraden till: `Angående ditt utvecklingsförslag` (i länken).
  - Inkluderar förnamn, datum samt en förhandsvisning av behovet: `suggestion.Requirement.Substring(0, Math.Min(suggestion.Requirement.Length, 50)) + "..."`.
- Ändra mailsvaret: uppdatera metoden `GetEmailBody(...)`.
- Tips: E‑postklienter hanterar radbrytningar i `mailto:` olika. `EscapeDataString` ser till att radbrytningar och citationstecken kodas korrekt, men testa i din klient.

API‑del (oförändrat):
- `KronoxApi/Controllers/DevelopmentSuggestionController.cs`
  - GET `api/developmentsuggestion` (Admin, lista)
  - PUT `api/developmentsuggestion/{id}/process` (markera behandlad)

## 3) Institutioner och länkar (”Om konsortiet”)
- Fil: `KronoxFront/Components/Pages/Omkonsortiet.razor`
- Vad: `institutionUrls` (Dictionary) mappar lärosätesnamn till URL.
- Ändra: Lägg till/uppdatera här för att påverka länkningen i sidan.

## 4) Hero‑bild per sida
- Var: Respektive sida (t.ex. `Home.razor`, `Omkonsortiet.razor`, …) metoder `GetHeroBannerUrl()`/`GetHeroBannerAlt()`.
- Mekanik: Hero identifieras av bilder vars alt‑text börjar med `hero:`. Ladda upp bild via admin (Banner‑sektionen) och sätt alt‑text.

## 5) Fasta sidor i adminvyer
- Fil: `KronoxFront/Components/Pages/Admin/MainPageAdmin.razor`
- Vad: `validFixedPages` (visa möjliga fasta sidor) och `pageUrlMapping` (URL‑mapping).
- Ändra: Lägg till här om du introducerar nya fasta sidor eller byter URL.

## 6) Rollupptäckt för sidor i navigation (Authorize)
- Fil: `KronoxFront/Components/Shared/Admin/NavigationSettings.razor` → `DetectPageAuthorizeRoles()`
- Vad: Kartläggning `PageKey -> komponenttyp` för att läsa `[Authorize]`‑attribut.
- Ändra: Uppdatera mapping om du byter klassnamn/filnamn för fasta sidor (t.ex. `Visionerverksamhetside`).

## 7) Feature‑sektioner: privat innehåll och kontaktpersoner
- Fil (frontend): `KronoxFront/Services/CmsService.cs`
  - `SavePageContentAsync` (synkar features från metadata till API)
  - `SaveFeatureSectionsAsync` (PUT till API, uppdaterar metadata)
  - Fallback: `GetFeatureSectionsFromMetadata` läser även `hasPrivateContent`, `privateContent`, `contactHeading`, `contactPersons`.
- Tips: Om privat innehåll ”försvinner” – kontrollera att båda vägarna (metadata <-> API) hanterar alla fält samt att cache invalid­eras.

## 8) TinyMCE‑editorinställningar
- Var: `wwwroot/js/tinymce-config.js` (laddas i `Components/App.razor`)
- Vad: Knappuppsättning, plugins, höjd mm för editorerna.
- Ändra: Anpassa filen för att ändra redigerarupplevelsen i admin.

## 9) Cachegrupper och nycklar
- Var: `KronoxFront/Services/CmsService.cs` (användning/invalidering) och `CacheService` (implementation).
- Konventioner:
  - Grupp per sida/feature: `features_{pageKey}`
  - Nycklar per synlighet: `features_public_{pageKey}`, `features_private_{pageKey}`
  - FAQ per sida: `faq_{pageKey}`
  - Handlingsplan (grupp): `actionplans`
  - Navigation (grupp): `navigation`
  - Sidinnehåll: använd `InvalidatePageCache(pageKey)` (intern nyckel hanteras av `CacheService`)

När behöver man bry sig?
- När du lägger till ny cache eller ska invalidera efter PUT/POST/DELETE.
- När UI visar gammalt innehåll (felsökning: invalidera rätt grupp/nyckel).

## 10) Medlemslogotyper
- Frontend hämtar via `CmsService.GetMemberLogosAsync()` och visar i sektionen ”Våra medlemmar”.
- Admin: `/admin/memberlogos`
- Sync till wwwroot: `SyncImagesFromApiAsync()` (körs vid första laddning av startsidan).

## 11) Navigationsknappar
- Se även: `KronoxFront/README-Navigeringsknappar.md` som förklarar:
  - var knapparna lagras (introSection.ShowNavigationButtons och introSection.NavigationButtons i metadata),
  - hur de aktiveras via sektionskonfigurationen,
  - maskerade länkar via `/go/<slug>` och `Redirects` i `appsettings.json`,
  - var rendering och admin‑hantering finns (`Components/Shared/Layout/NavigationButtons.razor` och `Components/Shared/Admin/NavigationButtonManager.razor`),
  - ikonklasser (Font Awesome).

## 12) 404‑spärr (RequirePageEnabled)
- Var: Topp på sidor (t.ex. `<RequirePageEnabled PageKey="visioner" FailClosed="true" />`).
- Effekt: Om navigationskonfig inaktiverar sidan returnerar UI en 404 vid direktbesök. Styrs via admin‑navigation.

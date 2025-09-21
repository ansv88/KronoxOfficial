# Navigeringsknappar – vanliga och maskerade länkar

Den här guiden beskriver hur navigeringsknapparna under intro-sektionen hanteras, inklusive hur du maskerar externa (eller känsliga) mål-URL:er så att endast en intern, “vänlig” väg syns vid hover (t.ex. `/go/manualen`).

Innehållet gäller Blazor-projektet KronoxFront (.NET 8).

## Begrepp
- Vanlig länk: Du skriver den riktiga URL:en direkt i admin (t.ex. `/dokument` eller `https://exempel.se`). Inget extra behövs.
- Maskerad länk: Du döljer den riktiga URL:en bakom en intern väg, t.ex. `/go/manualen`, och låter servern göra en 302-redirect till målet. Hover i webbläsaren visar då bara `/go/manualen`.

Maskerade länkar används när du inte vill exponera extern mål-URL (t.ex. en inloggningssida hos tredje part) i statusraden.

## Snabbstart

1) Lägg till redirect i konfigurationen (endast för maskerade länkar)
  - I `KronoxFront/appsettings.json`, under `Redirects`, lägg till en slug och dess mål:
  - { "Redirects": { "manualen": "https://extern.exempel.se/login?next=/docs" } }

2) Redirect-endpoint i Program.cs
  - I `KronoxFront/Program.cs` finns endpointen som hanterar `/go/{slug}`. Den gör följande:
  - Släpper bara igenom slugs som finns i `Redirects` (whitelist).
  - Krav på HTTPS i produktion.
  - Svarar med 302 (tillfällig redirect) och no-cache-headrar.

3) Lägg till/ändra knappar i admin
  - Öppna sidan i admin (MainPageAdmin), rulla till “Navigeringsknappar”.
  - Kryssa i “Visa navigeringsknappar”.
  - För en maskerad länk: ange URL som `/go/<slug>` (t.ex. `/go/manualen`).
  - NavigationButtonManager validerar att slugen finns i `Redirects`. Saknas den visas en röd varning och knappen kan inte läggas till förrän du lagt till slugen i `appsettings.json`.
  - För vanliga länkar: skriv den riktiga URL:en direkt (ingen konfig behövs).

4) Spara så att det syns publikt
  - Spara via “Spara intro-sektion” eller “Spara allt innehåll”. Cachen för sidan invalidieras automatiskt, så ändringarna syns direkt.

## När visas knapparna på sidan?
- Sektionen “Navigeringsknappar” måste vara aktiverad i sektionskonfigurationen för aktuell sida.
- Intro-sektionens flagga “Visa navigeringsknappar” måste vara ikryssad.
- Minst en knapp måste finnas.

Komponenten som renderar publikt är `Components/Shared/Layout/NavigationButtons.razor`.

## Exempel

Konfiguration:
"Redirects": { "manualen": "https://extern.exempel.se/login?next=/docs", "partnerportal": "https://partner.exempel.se/auth/sso" }

I admin:
- Knapp 1
  - Text: Manualen
  - URL: /go/manualen
- Knapp 2
  - Text: Partnerportal
  - URL: /go/partnerportal
- Knapp 3
  - Text: Dokument
  - URL: /dokument (vanlig intern länk, ingen redirect)

## Felsökning
- “Slug saknas i Redirects” i admin:
  - Lägg till slugen under `Redirects` i `appsettings.json` eller ändra URL till en vanlig länk.
- 404 på `/go/slug`:
  - Slugen finns inte i `Redirects` eller stavas fel.
- 400 “Insecure target” i produktion:
  - Målet använder `http://`. Endast `https://` tillåts i prod.
- Ändringar syns inte direkt:
  - Spara intro-sektionen eller allt innehåll; cache rensas då.

## Vanliga frågor
- Måste alla länkar läggas i appsettings?
  - Nej. Endast de du vill maskera. Vanliga länkar skriver du direkt i admin.
- Kan vi byta mål-URL utan att röra admin?
  - Ja. Uppdatera `appsettings.json` för den slugen, knapparna pekar fortsatt på `/go/<slug>`.

---
Berörda filer:
- `KronoxFront/Program.cs` – redirect-endpointen för `/go/{slug}`.
- `KronoxFront/appsettings.json` – `Redirects`-sektion med slug -> target.
- `KronoxFront/Components/Shared/Admin/NavigationButtonManager.razor` – admin-UI + validering.
- `KronoxFront/Components/Shared/Layout/NavigationButtons.razor` – publika knappar.
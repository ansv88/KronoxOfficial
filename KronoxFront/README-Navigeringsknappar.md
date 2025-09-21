# Navigeringsknappar � vanliga och maskerade l�nkar

Den h�r guiden beskriver hur navigeringsknapparna under intro-sektionen hanteras, inklusive hur du maskerar externa (eller k�nsliga) m�l-URL:er s� att endast en intern, �v�nlig� v�g syns vid hover (t.ex. `/go/manualen`).

Inneh�llet g�ller Blazor-projektet KronoxFront (.NET 8).

## Begrepp
- Vanlig l�nk: Du skriver den riktiga URL:en direkt i admin (t.ex. `/dokument` eller `https://exempel.se`). Inget extra beh�vs.
- Maskerad l�nk: Du d�ljer den riktiga URL:en bakom en intern v�g, t.ex. `/go/manualen`, och l�ter servern g�ra en 302-redirect till m�let. Hover i webbl�saren visar d� bara `/go/manualen`.

Maskerade l�nkar anv�nds n�r du inte vill exponera extern m�l-URL (t.ex. en inloggningssida hos tredje part) i statusraden.

## Snabbstart

1) L�gg till redirect i konfigurationen (endast f�r maskerade l�nkar)
  - I `KronoxFront/appsettings.json`, under `Redirects`, l�gg till en slug och dess m�l:
  - { "Redirects": { "manualen": "https://extern.exempel.se/login?next=/docs" } }

2) Redirect-endpoint i Program.cs
  - I `KronoxFront/Program.cs` finns endpointen som hanterar `/go/{slug}`. Den g�r f�ljande:
  - Sl�pper bara igenom slugs som finns i `Redirects` (whitelist).
  - Krav p� HTTPS i produktion.
  - Svarar med 302 (tillf�llig redirect) och no-cache-headrar.

3) L�gg till/�ndra knappar i admin
  - �ppna sidan i admin (MainPageAdmin), rulla till �Navigeringsknappar�.
  - Kryssa i �Visa navigeringsknappar�.
  - F�r en maskerad l�nk: ange URL som `/go/<slug>` (t.ex. `/go/manualen`).
  - NavigationButtonManager validerar att slugen finns i `Redirects`. Saknas den visas en r�d varning och knappen kan inte l�ggas till f�rr�n du lagt till slugen i `appsettings.json`.
  - F�r vanliga l�nkar: skriv den riktiga URL:en direkt (ingen konfig beh�vs).

4) Spara s� att det syns publikt
  - Spara via �Spara intro-sektion� eller �Spara allt inneh�ll�. Cachen f�r sidan invalidieras automatiskt, s� �ndringarna syns direkt.

## N�r visas knapparna p� sidan?
- Sektionen �Navigeringsknappar� m�ste vara aktiverad i sektionskonfigurationen f�r aktuell sida.
- Intro-sektionens flagga �Visa navigeringsknappar� m�ste vara ikryssad.
- Minst en knapp m�ste finnas.

Komponenten som renderar publikt �r `Components/Shared/Layout/NavigationButtons.razor`.

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
  - URL: /dokument (vanlig intern l�nk, ingen redirect)

## Fels�kning
- �Slug saknas i Redirects� i admin:
  - L�gg till slugen under `Redirects` i `appsettings.json` eller �ndra URL till en vanlig l�nk.
- 404 p� `/go/slug`:
  - Slugen finns inte i `Redirects` eller stavas fel.
- 400 �Insecure target� i produktion:
  - M�let anv�nder `http://`. Endast `https://` till�ts i prod.
- �ndringar syns inte direkt:
  - Spara intro-sektionen eller allt inneh�ll; cache rensas d�.

## Vanliga fr�gor
- M�ste alla l�nkar l�ggas i appsettings?
  - Nej. Endast de du vill maskera. Vanliga l�nkar skriver du direkt i admin.
- Kan vi byta m�l-URL utan att r�ra admin?
  - Ja. Uppdatera `appsettings.json` f�r den slugen, knapparna pekar fortsatt p� `/go/<slug>`.

---
Ber�rda filer:
- `KronoxFront/Program.cs` � redirect-endpointen f�r `/go/{slug}`.
- `KronoxFront/appsettings.json` � `Redirects`-sektion med slug -> target.
- `KronoxFront/Components/Shared/Admin/NavigationButtonManager.razor` � admin-UI + validering.
- `KronoxFront/Components/Shared/Layout/NavigationButtons.razor` � publika knappar.
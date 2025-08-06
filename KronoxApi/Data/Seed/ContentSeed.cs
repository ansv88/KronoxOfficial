using KronoxApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace KronoxApi.Data.Seed;

// Klass för att seeda standardinnehåll, bilder och metadata till databasen.
public static class ContentSeed
{
    // Kör hela seed-processen för innehåll, bilder och metadata.
    public static async Task SeedContentAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Säkerställ att databasen är skapad
            //await dbContext.Database.EnsureCreatedAsync();

            await dbContext.Database.MigrateAsync(); // Test

            // Registrera medlemslogotyper
            await EnsureMemberLogosAsync(env, logger, dbContext);

            // Kopiera och registrera feature-bilder
            await EnsureFeatureImagesAndRegisterAsync(env, logger, dbContext);

            // Seeda standardinnehåll
            await SeedDefaultContentAsync(dbContext, logger);

            // Seeda intro-sektion för startsidan
            await SeedIntroSectionAsync(serviceProvider, logger);

            // Seeda feature-sektioner för startsidan
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "home");

            // Seeda intro-sektion för Om konsortiet
            await SeedOmkonsortietsIntroSectionAsync(serviceProvider, logger);

            // Seeda feature-sektioner för Om konsortiet
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "omkonsortiet");

            // Seeda intro-sektion för Visioner & Verksamhetsidé
            await SeedVisionerIntroSectionAsync(serviceProvider, logger);

            // Seeda feature-sektioner för Visioner & Verksamhetsidé
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "visioner");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ett fel uppstod vid initiering av innehåll.");
            throw;
        }
    }

    // Seedar feature-sektioner för en specifik sida om de inte redan finns.
    private static async Task SeedFeatureSectionsFromMetadataAsync(IServiceProvider serviceProvider, ILogger logger, string pageKey)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await dbContext.FeatureSections.AnyAsync(fs => fs.PageKey == pageKey))
            {
                logger.LogInformation("Feature-sektioner för {PageKey} finns redan. Hoppar över seeding.", pageKey);
                return;
            }

            logger.LogInformation("Seedar feature-sektioner för {PageKey}...", pageKey);

            var featureSections = pageKey switch
            {
                "home" => GetDefaultFeatureSections(),
                "omkonsortiet" => GetOmkonsortietsFeatureSections(),
                "visioner" => GetVisionerFeatureSections(),
                _ => GetDefaultFeatureSections()
            };

            int sortOrder = 0;

            foreach (var section in featureSections)
            {
                var imageUrl = section.imageUrl;
                if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("/"))
                {
                    imageUrl = "/" + imageUrl;
                }

                dbContext.FeatureSections.Add(new FeatureSection
                {
                    PageKey = pageKey,
                    Title = section.title,
                    Content = section.content,
                    ImageUrl = section.imageUrl,
                    ImageAltText = section.imageAltText,
                    HasImage = section.hasImage,
                    SortOrder = sortOrder++,
                    HasPrivateContent = section.hasPrivateContent,
                    PrivateContent = section.privateContent,
                    ContactPersonsJson = section.contactPersons?.Count > 0
                        ? JsonSerializer.Serialize(section.contactPersons) 
                        : ""
                });
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation($"Seedade {sortOrder} feature-sektioner för {pageKey}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av feature-sektioner för {PageKey}", pageKey);
        }
    }

    // Returnerar standardvärden för intro-sektionen.
    private static dynamic GetDefaultIntroSection()
    {
        return new
        {
            title = "Våga släpp taget!",
            content = "<p>Schemaläggning för högre utbildning! Satsa på framtidens schemaläggningssystem! KronoX är ett heltäckande system för såväl avancerad schemaläggning som enklare lokal- och resursbokning via webb och app. Det är anpassat för universitet och högskolor och styrs av medlemmarna med användarna i fokus.</p>"
        };
    }

    // Returnerar intro-sektion för Om konsortiet-sidan.
    private static dynamic GetOmkonsortietsIntroSection()
    {
        return new
        {
            title = "En sammanslutning av högskolor och universitet",
            content = @"<p>Konsortiet är en sammanslutning av högskolor och universitet för utveckling och drift av schemaläggningssystemet KronoX. Dess hemvist är Högskolan i Borås, som svarar för administration, drift och utveckling. Verksamhetens leds av en styrelse bestående av ledamöter från medlemslärosätena. Verksamheten finansieras genom avgifter från medlemmarna i form av en årsavgift. Årsavgiften bestäms av respektive medlems/lärosätes antal helårsstudenter. Rätten att få utnyttja programvaran KronoX förbehålls konsortiets medlemmar. För mer information kontakta: <a href=""mailto:info@kronox.se"">info@kronox.se</a></p>",
            hasImage = false,
            imageUrl = "",
            imageAltText = ""
        };
    }

    // Returnerar intro-sektion för Visioner & Verksamhetsidé-sidan.
    private static dynamic GetVisionerIntroSection()
    {
        return new
        {
            title = "Vision",
            content = "<p>KronoX är, med sin inriktning mot högskolor och universitet, en viktig aktör på marknaden för system för schemaläggning och relaterade aktiviteter. Som konsortium tillhandahåller KronoX, i nära samverkan med sina medlemmar, efterfrågade tjänster med hög service och kvalitet på ett effektivt sätt. Allt fler lärosäten efterfrågar KronoX tjänster.</p>",
            hasImage = false,
            imageUrl = "",
            imageAltText = ""
        };
    }

    // Returnerar standard-feature-sektioner för startsidan.
    private static dynamic[] GetDefaultFeatureSections()
    {
        return new[]
        {
            new {
                title = "",
                content = "<p>KronoX är skapat för användning i universitets- och högskolevärlden och utvecklas kontinuerligt i nära samarbete med användare och professionella schemaläggare. KronoX ägs av medlemmarna i form av ett konsortium.</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Oöverträffad flexibilitet",
                content = "<p>KronoX har stora möjligheter att integreras med lärosätenas övriga datasystem, som till exempel kursdatabas och fastighetssystem. KronoX erbjuder möjlighet att anpassa systemet efter lärosätenas behov när det gäller databas, utseende och arbetssätt.</p>",
                imageUrl = "/images/pages/home/KronoX-bokningsdialogen-med-lagerfunktionen.png",
                imageAltText = "Illustration av systemets flexibilitet",
                hasImage = true,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "KronoX tillgodoser vitt skilda behov",
                content = "<p>KronoX erbjuder allt från enkel schemasökning eller bokning av grupprum till komplex och avancerad schemaläggning. Studenter, lärare och erfarna schemaläggare får sina behov tillgodosedda.</p><ul><li>Synkronisering med externa kalendrar för lärare och studenter (ICAL)</li><li>Anpassningsbar kollisionskontroll för den professionella schemaläggaren.</li><li>Anpassning av schemavyn.</li><li>Möjlighet för till exempel studenter att själva boka grupprum via webb eller app.</li><li>Schemaläggningsassistent på webben (möjligt att skapa schemaunderlag som överförs in i systemet)</li></ul>",
                imageUrl = "/images/pages/home/bokningsdialogen.png",
                imageAltText = "Användare med olika behov",
                hasImage = true,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Övergripande information",
                content = "<p>Systemet ger överblick över lokalnyttjandet både på kursnivå och på lokalnivå. Det ger bra underlag för uttag av statistik och för planering av lärsosätenas lokalförsörjning.</p>",
                imageUrl = "/images/pages/home/oversikt.png",
                imageAltText = "Informationsöverblick",
                hasImage = true,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Debiteringsfunktion",
                content = "<p>KronoX möjliggör för lärosäten att individuellt sätta priser på lokalanvändningen. Systemet levererar tydliga debiteringsunderlag för fakturering. Möjlighet till tidsintervall i debitering finns att tillgå.</p>",
                imageUrl = "/images/pages/home/debiteringsflik.png",
                imageAltText = "Debiteringsfunktion",
                hasImage = true,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            }
        };
    }

    // Returnerar feature-sektioner för Om konsortiet-sidan med synlighetsegenskaper.
    private static dynamic[] GetOmkonsortietsFeatureSections()
    {
        return new[]
        {
            new {
                title = "För högskolor och universitet",
                content = @"<p>Även ditt lärosäte har möjlighet att använda högskolornas gemensamma system för schemaläggning samt resurs- och lokalbokning. Ett lärosäte som blir medlem i KronoX får tillgång till de tjänster som ägs av medlemshögskolorna. Verksamheten drivs helt utan vinstintresse och medlemskap i konsortiet är endast möjligt för universitet och högskolor.</p>
                           <p>Om ni överväger ett medlemskap i KronoX-konsortiet, kontakta Konsortiechefen Per-Anders Månsson, Högskolan i Borås för ytterligare information. Per-Anders kan nås på telefon 033‑435 41 88 eller e-post <a href=""mailto:pam@kronox.se"">pam@kronox.se</a>.</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Användarstyrd flexibilitet",
                content = "<p>Användarna styr utvecklingen genom bl.a. regelbundna användarträffar. All support ingår i medlemsavgiften. Det finns inget vinstintresse och konsortieformen garanterar insyn, inflytande och möjligheter att direkt påverka utvecklingen.</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Verksamhetsnära styrgrupp (VNSG) tillvaratar medlemmarnas intresse",
                content = "<p>Styrgruppen består av ca fem användare från olika medlemslärosäten och har en viktig funktion i arbetet med att vidareutveckla och förbättra KronoX. Gruppen har ansvar för att ta emot, bearbeta och prioritera önskemål och synpunkter från medlemmarna. En annan viktig uppgift är att planera en årlig användarträff samt träffar riktade mot specifika funktioner i systemet. Styrgruppen producerar också de manualer som riktar sig till KronoX användare. Dessa manualer är webbaserade och hålls kontinuerligt uppdaterade.</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = true,
                privateContent = "<p><strong>Gruppen består i dagsläget av följande personer:</strong></p>",
                contactPersons = new List<object>
                {
                    new { 
                        name = "Catharina Edvardsson", 
                        email = "catharina.edvardsson@hkr.se", 
                        phone = "044 – 250 39 12", 
                        organization = "HKR",
                        showNamePublicly = false,        // Visa inte namn publikt
                        showEmailPublicly = false,       // Visa inte e-post publikt  
                        showPhonePublicly = false,       // Visa inte telefon publikt
                        showOrganizationPublicly = true, // Visa organisation publikt
                        showEmailToMembers = true,       // Visa e-post för medlemmar
                        showPhoneToMembers = true        // Visa telefon för medlemmar
                    },
                    new { 
                        name = "Marie Palmnert", 
                        email = "marie.palmnert@mau.se", 
                        phone = "040 – 665 83 63", 
                        organization = "MAU",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Emma Bengtsson", 
                        email = "emma.bengtsson@ltu.se", 
                        phone = "0920 – 49 36 74", 
                        organization = "LTU",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Adrian Tremearne", 
                        email = "adrian.tremearne@sh.se", 
                        phone = "", 
                        organization = "SH",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = false // Ingen telefon att visa
                    }
                }
            },
            new {
                title = "Styrelsen",
                content = @"<div class='row'>
                             <div class='col-md-6'>
                               <p>Micael Melander (ordförande, HIG)</p>
                               <p>Daniel Blomberg (MDU)</p>
                               <p>Catharina Edvardsson (HKR)</p>
                               <p>Maria Strand (HIG)</p>
                               <p>Hanna Markusson (HB)</p>
                             </div>
                             <div class='col-md-6'>
                               <p><strong>Suppleanter för styrelsen</strong></p>
                               <p>Camilla Lindqvist (LTU)</p>
                             </div>
                           </div>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = true,
                privateContent = "<p><strong>Kontaktinformation för styrelsen:</strong></p>",
                contactPersons = new List<object>
                {
                    new { 
                        name = "Micael Melander", 
                        email = "micael.melander@hig.se", 
                        phone = "026 – 64 85 85", 
                        organization = "HIG (ordförande)",
                        showNamePublicly = true,         // Visa namn publikt för styrelsen
                        showEmailPublicly = false,       // Visa inte e-post publikt
                        showPhonePublicly = false,       // Visa inte telefon publikt
                        showOrganizationPublicly = true, // Visa organisation publikt
                        showEmailToMembers = true,       // Visa e-post för medlemmar
                        showPhoneToMembers = true        // Visa telefon för medlemmar
                    },
                    new { 
                        name = "Daniel Blomberg", 
                        email = "daniel.blomberg@mdu.se", 
                        phone = "021-10 73 03", 
                        organization = "MDU",
                        showNamePublicly = true,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Catharina Edvardsson", 
                        email = "catharina.edvardsson@hkr.se", 
                        phone = "044 – 250 39 12", 
                        organization = "HKR",
                        showNamePublicly = true,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Maria Strand", 
                        email = "maria.strand@hig.se", 
                        phone = "026 – 64 84 20", 
                        organization = "HIG",
                        showNamePublicly = true,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Hanna Markusson", 
                        email = "hanna.markusson@hb.se", 
                        phone = "033-435 4273", 
                        organization = "HB",
                        showNamePublicly = true,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Camilla Lindqvist", 
                        email = "camilla.lindqvist@ltu.se", 
                        phone = "0920-493527", 
                        organization = "LTU (suppleant)",
                        showNamePublicly = true,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    }
                }
            },
            new {
                title = "Konsortiets medlemmar",
                content = @"<div class='row'>
                             <div class='col-md-6'>
                               <p><a href='https://www.hb.se' target='_blank' rel='noopener'>Högskolan i Borås</a></p>
                               <p><a href='https://www.hig.se' target='_blank' rel='noopener'>Högskolan i Gävle</a></p>
                               <p><a href='https://www.hkr.se' target='_blank' rel='noopener'>Högskolan Kristianstad</a></p>
                               <p><a href='https://www.hv.se' target='_blank' rel='noopener'>Högskolan Väst</a></p>
                               <p><a href='https://www.jth.se' target='_blank' rel='noopener'>Johannelunds teologiska högskola</a></p>
                               <p><a href='https://www.konstfack.se' target='_blank' rel='noopener'>Konstfack</a></p>
                             </div>
                             <div class='col-md-6'>
                               <p><a href='https://www.ltu.se' target='_blank' rel='noopener'>Luleå tekniska universitet</a></p>
                               <p><a href='https://www.mau.se' target='_blank' rel='noopener'>Malmö universitet</a></p>
                               <p><a href='https://www.mdu.se' target='_blank' rel='noopener'>Mälardalens universitet</a></p>
                               <p><a href='https://www.sh.se' target='_blank' rel='noopener'>Södertörns högskola</a></p>
                               <p><a href='https://www.oru.se' target='_blank' rel='noopener'>Örebro universitet</a></p>
                             </div>
                           </div>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = true,
                privateContent = "<p><strong>Kontaktpersoner vid medlemslärosätena:</strong></p>",
                contactPersons = new List<object>
                {
                    new { 
                        name = "Andrea Boldizar", 
                        email = "andrea.boldizar@hb.se", 
                        phone = "033-435 4273", 
                        organization = "Högskolan i Borås",
                        showNamePublicly = false,        // Visa inte namn publikt för medlemskontakter
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true, // Visa skolan publikt
                        showEmailToMembers = true,       // Visa kontaktinfo för medlemmar
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Lena Ask", 
                        email = "lena.ask@hig.se", 
                        phone = "", 
                        organization = "Högskolan i Gävle",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = false
                    },
                    new { 
                        name = "Catharina Edvardsson", 
                        email = "catharina.edvardsson@hkr.se", 
                        phone = "044-20 40 12", 
                        organization = "Högskolan Kristianstad",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Charlotta Andersson Jonsson", 
                        email = "charlotta.andersson.jonsson@hv.se", 
                        phone = "0520-223731", 
                        organization = "Högskolan Väst",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Mattias Neve", 
                        email = "mattias.neve@jth.se", 
                        phone = "018-16 99 07", 
                        organization = "Johannelunds teologiska högskola",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Alexander Jakobsson", 
                        email = "alexander.jakobsson@konstfack.se", 
                        phone = "08-450 43 77", 
                        organization = "Konstfack",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Emma Bengtsson", 
                        email = "emma.bengtsson@ltu.se", 
                        phone = "0920-49 36 74", 
                        organization = "Luleå tekniska universitet",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Teresa Weidsten", 
                        email = "teresa.weidsten@mdu.se", 
                        phone = "021-10 70 51", 
                        organization = "Mälardalens universitet",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Marie Palmnert", 
                        email = "marie.palmnert@mau.se", 
                        phone = "040-665 83 11", 
                        organization = "Malmö universitet",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    },
                    new { 
                        name = "Adrian Tremearne", 
                        email = "adrian.tremearne@sh.se", 
                        phone = "", 
                        organization = "Södertörns högskola",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = false
                    },
                    new { 
                        name = "Ines Vilasevic", 
                        email = "ines.vilasevic@oru.se", 
                        phone = "019-30 10 57", 
                        organization = "Örebro universitet",
                        showNamePublicly = false,
                        showEmailPublicly = false,
                        showPhonePublicly = false,
                        showOrganizationPublicly = true,
                        showEmailToMembers = true,
                        showPhoneToMembers = true
                    }
                }
            }
        };
    }

    // Returnerar feature-sektioner för Visioner & Verksamhetsidé-sidan.
    private static dynamic[] GetVisionerFeatureSections()
    {
        return new[]
        {
            new {
                title = "Verksamhetsidé",
                content = "<p>Det finns nästan lika många olika professioner som har nytta av systemet som sätt det gör sig nyttigt på. Nedan hittar du ett par av de fördelar för respektive profession som våra medlemmar själva anser extra värdefulla.</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            },
            new {
                title = "Mål",
                content = @"<p>Verksamhet ska bygga på en nära samverkan inom konsortiet där användarnas erfarenheter och synpunkter tas tillvara på ett konstruktivt sätt. Genom samspelet mellan konsortiet och medlemmarna uppnås en hög kundlojalitet (samverkansmål).</p>
                           <p>Arbetet inom konsortiet ska utföras på ett professionellt sätt och verksamheten ska vara välorganiserad och ha tillgång till god kompetens (personalmål).</p>
                           <p>Universitet och högskolor ska känna stort förtroende för KronoX och systemet ska vara erkänt och attraktivt på marknaden. KronoX ska eftersträva en anslutning av lärosäten i en omfattning som innebär att minst en tredjedel av marknaden sett till antalet studenter täcks in (marknadsmål).</p>
                           <p>KronoX kommunikation ska vara effektiv och anpassad till skilda målgruppers behov. Kommunikationen ska vara tillgänglig, tydlig, enkel och lättbegriplig (kommunikationsmål).</p>
                           <p>KronoXsystemet ska ha en hög tillgänglighet och driftsäkerhet. Systemet ska vara användarvänligt med en anpassning som innebär att det möter behovet av systemstöd från samtliga användarkategorier (tillgänglighetsmål).</p>
                           <p>Utvecklingen av KronoX ska ske med modern, etablerad och relevant teknik. Systemet ska följa etablerade standarder och kunna samverka med andra relevanta system (teknikmål).</p>
                           <p>KronoX verksamhet ska vara i ekonomisk balans. För att garantera en långsiktig trygghet och beredskap ska det ska det finnas en buffert i det balanserade egna kapitalet. Konsortiet ska genom sin verksamhet bidra till kostnadseffektiva lösningar för medlemmarna (finansieringsmål).</p>",
                imageUrl = "",
                imageAltText = "",
                hasImage = false,
                hasPrivateContent = false,
                privateContent = "",
                contactPersons = new List<object>()
            }
        };
    }

    // Seedar standardinnehåll för startsidan och andra standardsidor.
    private static async Task SeedDefaultContentAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "home"))
        {
            logger.LogInformation("Startsidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogInformation("Börjar seeda standardinnehåll för startsidan...");

            var introSection = GetDefaultIntroSection();
            var featureSections = GetDefaultFeatureSections();

            var metadataJson = JsonSerializer.Serialize(new
            {
                introSection,
                features = featureSections
            });

            var homeContent = new ContentBlock
            {
                PageKey = "home",
                Title = "Startsida",
                HtmlContent = CreateDefaultHomeContent(),
                Metadata = metadataJson,
                LastModified = DateTime.UtcNow
            };

            dbContext.ContentBlocks.Add(homeContent);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Startsidans innehåll har seedats framgångsrikt.");
        }

        // Seeda Om konsortiet-sidan
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "omkonsortiet"))
        {
            logger.LogInformation("Om konsortiet-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogInformation("Börjar seeda standardinnehåll för Om konsortiet-sidan...");

            var introSection = GetOmkonsortietsIntroSection();
            var featureSections = GetOmkonsortietsFeatureSections();

            var metadataJson = JsonSerializer.Serialize(new
            {
                introSection,
                features = featureSections
            });

            var omkonsortietsContent = new ContentBlock
            {
                PageKey = "omkonsortiet",
                Title = "Om konsortiet",
                HtmlContent = CreateOmkonsortietsContent(),
                Metadata = metadataJson,
                LastModified = DateTime.UtcNow
            };

            dbContext.ContentBlocks.Add(omkonsortietsContent);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Om konsortiet-sidans innehåll har seedats framgångsrikt.");
        }

        // Seeda Visioner & Verksamhetsidé-sidan
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "visioner"))
        {
            logger.LogInformation("Visioner & Verksamhetsidé-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogInformation("Börjar seeda standardinnehåll för Visioner & Verksamhetsidé-sidan...");

            var visionerIntroSection = GetVisionerIntroSection();
            var visionerFeatureSections = GetVisionerFeatureSections();

            var visionerMetadataJson = JsonSerializer.Serialize(new
            {
                introSection = visionerIntroSection,
                features = visionerFeatureSections
            });

            var visionerContent = new ContentBlock
            {
                PageKey = "visioner",
                Title = "Visioner & Verksamhetsidé",
                HtmlContent = CreateVisionerContent(),
                Metadata = visionerMetadataJson,
                LastModified = DateTime.UtcNow
            };

            dbContext.ContentBlocks.Add(visionerContent);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Visioner & Verksamhetsidé-sidans innehåll har seedats framgångsrikt.");
        }

        // Seeda andra standardsidor
        var standardPages = new List<(string key, string title)>
        {
            ("omsystemet", "Om systemet"),
            ("kontaktaoss", "Kontakta oss")
        };

        foreach (var (key, title) in standardPages)
        {
            if (!await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == key))
            {
                dbContext.ContentBlocks.Add(new ContentBlock
                {
                    PageKey = key,
                    Title = title,
                    HtmlContent = $"<p>Innehåll för sidan {title} kommer snart.</p>",
                    LastModified = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Standardsidor har seedats framgångsrikt.");
    }

    // Kopierar och registrerar medlemslogotyper från frontend och seedar dem i databasen.
    private static async Task EnsureMemberLogosAsync(
    IWebHostEnvironment env,
    ILogger logger,
    ApplicationDbContext dbContext)
    {
        var membersDir = Path.Combine(env.WebRootPath, "images", "members");
        Directory.CreateDirectory(membersDir);

        var frontendPath = Path.Combine(env.ContentRootPath, "..", "KronoxFront");
        var frontendMembersDir = Path.Combine(frontendPath, "wwwroot", "images", "members");

        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };

        // Mappa filnamn till URL:er
        var universityUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "hb_logo1_cmyk.jpg", "https://www.hb.se" },
            { "HiG_original.png", "https://www.hig.se" },
            { "hogskolan-vast-logotyp-bla.png", "https://www.hv.se" },
            { "Konstfack-logo.png", "https://www.konstfack.se" },
            { "logga_jlund_brons.png", "https://www.jth.se" },
            { "Logo_2_SÖDERTÖRNS HÖGSKOLA_rgb.png", "https://www.sh.se" },
            { "Luleå_tekniska_universitet_Logo.svg", "https://www.ltu.se" },
            { "MAU_SV_main_RGB.png", "https://www.mau.se" },
            { "MDU_logotyp.svg", "https://www.mdu.se" },
            { "og_image_hkr.png", "https://www.hkr.se" },
            { "Oru_logo_rgb.png", "https://www.oru.se" }
        };

        if (Directory.Exists(frontendMembersDir))
        {
            logger.LogInformation($"Hittade frontend-mapp för medlemslogotyper: {frontendMembersDir}");

            foreach (var file in Directory.GetFiles(frontendMembersDir)
                    .Where(f => allowedExt.Contains(Path.GetExtension(f).ToLowerInvariant())))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(membersDir, fileName);

                if (!File.Exists(destFile))
                {
                    try
                    {
                        File.Copy(file, destFile);
                        logger.LogInformation($"Kopierade medlemslogotyp från frontend: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Misslyckades med att kopiera logotypen {fileName} från frontend");
                    }
                }
            }
        }
        else
        {
            logger.LogWarning("Frontend-mapp för medlemslogotyper hittades inte: {Path}", frontendMembersDir);
        }

        if (Directory.Exists(membersDir))
        {
            var logoFiles = Directory.GetFiles(membersDir)
                .Where(f => allowedExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (logoFiles.Any())
            {
                logger.LogInformation($"Registrerar {logoFiles.Count} medlemslogotyper i databasen");

                int sortOrder = 1;
                foreach (var file in logoFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var relativeUrl = $"/images/members/{fileName}";

                    if (!await dbContext.MemberLogos.AnyAsync(logo => logo.Url == relativeUrl))
                    {
                        var altText = Path.GetFileNameWithoutExtension(file)
                                      .Replace("_", " ")
                                      .Replace("-", " ");

                        // Hitta URL för denna logotyp
                        var linkUrl = universityUrls.ContainsKey(fileName) ? universityUrls[fileName] : "";

                        dbContext.MemberLogos.Add(new MemberLogo
                        {
                            Url = relativeUrl,
                            AltText = altText,
                            SortOrd = sortOrder++,
                            LinkUrl = linkUrl
                        });

                        logger.LogInformation($"Medlemslogotyp registrerad i databasen: {fileName} med länk: {linkUrl}");
                    }
                    else
                    {
                        // Uppdatera befintliga logotyper med URL:er om de saknas
                        var existingLogo = await dbContext.MemberLogos.FirstOrDefaultAsync(logo => logo.Url == relativeUrl);
                        if (existingLogo != null && string.IsNullOrEmpty(existingLogo.LinkUrl) && universityUrls.ContainsKey(fileName))
                        {
                            existingLogo.LinkUrl = universityUrls[fileName];
                            logger.LogInformation($"Uppdaterade länk för befintlig logotyp: {fileName} med länk: {universityUrls[fileName]}");
                        }
                        else
                        {
                            logger.LogInformation($"Medlemslogotyp redan registrerad i databasen: {fileName}");
                        }
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }

    // Seedar eller uppdaterar intro-sektionen för startsidan.
    private static async Task SeedIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var homeContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "home");
            if (homeContent == null)
            {
                logger.LogWarning("Kunde inte hitta ContentBlock för startsidan, skapar ingen intro-sektion.");
                return;
            }

            var introSection = GetDefaultIntroSection();

            if (string.IsNullOrEmpty(homeContent.Metadata))
            {
                homeContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    features = GetDefaultFeatureSections()
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(homeContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    writer.WritePropertyName("introSection");
                    writer.WriteStartObject();
                    writer.WriteString("title", introSection.title);
                    writer.WriteString("content", introSection.content);
                    writer.WriteEndObject();

                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != "introSection")
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    homeContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    homeContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        features = GetDefaultFeatureSections()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seedade intro-sektion för startsidan");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion");
        }
    }

    // Seedar eller uppdaterar intro-sektionen för Om konsortiet-sidan.
    private static async Task SeedOmkonsortietsIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var omkonsortietsContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "omkonsortiet");
            if (omkonsortietsContent == null)
            {
                logger.LogWarning("Kunde inte hitta ContentBlock för Om konsortiet-sidan, skapar ingen intro-sektion.");
                return;
            }

            var introSection = GetOmkonsortietsIntroSection();

            if (string.IsNullOrEmpty(omkonsortietsContent.Metadata))
            {
                omkonsortietsContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    features = GetOmkonsortietsFeatureSections()
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(omkonsortietsContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    writer.WritePropertyName("introSection");
                    writer.WriteStartObject();
                    writer.WriteString("title", introSection.title);
                    writer.WriteString("content", introSection.content);
                    writer.WriteBoolean("hasImage", introSection.hasImage);
                    writer.WriteString("imageUrl", introSection.imageUrl);
                    writer.WriteString("imageAltText", introSection.imageAltText);
                    writer.WriteEndObject();

                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != "introSection")
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    omkonsortietsContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    omkonsortietsContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        features = GetOmkonsortietsFeatureSections()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seedade intro-sektion för Om konsortiet-sidan");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Om konsortiet");
        }
    }

    // Seedar eller uppdaterar intro-sektionen för Visioner & Verksamhetsidé-sidan.
    private static async Task SeedVisionerIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var visionerContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "visioner");
            if (visionerContent == null)
            {
                logger.LogWarning("Kunde inte hitta ContentBlock för Visioner-sidan, skapar ingen intro-sektion.");
                return;
            }

            var introSection = GetVisionerIntroSection();

            if (string.IsNullOrEmpty(visionerContent.Metadata))
            {
                visionerContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    features = GetVisionerFeatureSections()
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(visionerContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    writer.WritePropertyName("introSection");
                    writer.WriteStartObject();
                    writer.WriteString("title", introSection.title);
                    writer.WriteString("content", introSection.content);
                    writer.WriteBoolean("hasImage", introSection.hasImage);
                    writer.WriteString("imageUrl", introSection.imageUrl);
                    writer.WriteString("imageAltText", introSection.imageAltText);
                    writer.WriteEndObject();

                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != "introSection")
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    visionerContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    visionerContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        features = GetVisionerFeatureSections()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seedade intro-sektion för Visioner & Verksamhetsidé-sidan");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Visioner-sidan");
        }
    }

    // Skapar HTML för startsidan baserat på standardvärden.
    private static string CreateDefaultHomeContent()
    {
        var introSection = GetDefaultIntroSection();
        var featureSections = GetDefaultFeatureSections();

        return BuildHtmlContent(introSection.content, featureSections);
    }

    // Skapar HTML för Om konsortiet-sidan baserat på standardvärden.
    private static string CreateOmkonsortietsContent()
    {
        var introSection = GetOmkonsortietsIntroSection();
        var featureSections = GetOmkonsortietsFeatureSections();

        return BuildHtmlContent(introSection.content, featureSections);
    }

    // Skapar HTML för Visioner & Verksamhetsidé-sidan baserat på standardvärden.
    private static string CreateVisionerContent()
    {
        var introSection = GetVisionerIntroSection();
        var featureSections = GetVisionerFeatureSections();

        return BuildHtmlContent(introSection.content, featureSections);
    }

    // Hjälpmetod för att bygga HTML-innehållet för en sida.
    private static string BuildHtmlContent(string introContent, dynamic[] featureSections)
    {
        var fullHtml = new StringBuilder();

        fullHtml.Append(introContent);

        foreach (var section in featureSections)
        {
            fullHtml.Append("<div class='feature-section'>");

            if (!string.IsNullOrEmpty(section.title))
            {
                fullHtml.Append($"<h3 class='text-center mb-4 fw-bold'>{section.title}</h3>");
            }

            fullHtml.Append($"<div class='text-center'>{section.content}</div>");
            fullHtml.Append("</div>");

            if (section != featureSections.Last())
            {
                fullHtml.Append("<div class='divider'></div>");
            }
        }

        return fullHtml.ToString();
    }

    // Kopierar och registrerar feature-bilder från seed-mapp till wwwroot och databasen.
    private static async Task EnsureFeatureImagesAndRegisterAsync(
     IWebHostEnvironment env,
     ILogger logger,
     ApplicationDbContext dbContext)
    {
        var seedImageDir = Path.Combine(env.ContentRootPath, "SeedAssets", "FeatureImages");
        var wwwrootImageDir = Path.Combine(env.WebRootPath, "images", "pages", "home");

        Directory.CreateDirectory(wwwrootImageDir);

        if (!Directory.Exists(seedImageDir))
        {
            logger.LogWarning($"Seed-bildmapp saknas: {seedImageDir}");
            return;
        }

        var fileMappings = new Dictionary<string, (string sectionId, string altText)>
    {
        { "KronoX-bokningsdialogen-med-lagerfunktionen.png", ("feature:1", "Illustration av systemets flexibilitet") },
        { "bokningsdialogen.png", ("feature:2", "Användare med olika behov") },
        { "oversikt.png", ("feature:3", "Informationsöverblick") },
        { "debiteringsflik.png", ("feature:4", "Debiteringsfunktion") }
    };

        foreach (var file in Directory.GetFiles(seedImageDir))
        {
            var filename = Path.GetFileName(file);
            var destFile = Path.Combine(wwwrootImageDir, filename);

            // Kopiera originalfilen om den inte finns
            if (!File.Exists(destFile))
            {
                File.Copy(file, destFile);
                logger.LogInformation($"Featurebild kopierad: {destFile}");
            }

            var relativeUrl = $"/images/pages/home/{filename}";

            var sectionInfo = fileMappings.ContainsKey(filename) ?
                fileMappings[filename] : (sectionId: "feature:0", altText: filename);

            var existingImage = await dbContext.PageImages
                .FirstOrDefaultAsync(pi => pi.Url == relativeUrl);

            if (existingImage == null)
            {
                dbContext.PageImages.Add(new PageImage
                {
                    PageKey = "home",
                    Url = relativeUrl,
                    AltText = sectionInfo.sectionId
                });

                logger.LogInformation($"Registrerad bild i databasen: {filename}");
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
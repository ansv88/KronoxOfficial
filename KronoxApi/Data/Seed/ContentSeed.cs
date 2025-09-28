using KronoxApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace KronoxApi.Data.Seed;

/// <summary>
/// Seedar standardinnehåll (ContentBlocks), bilder, metadata, FAQ och kontaktdata.
/// Idempotent där det är möjligt: uppdaterar endast när nödvändigt och respekterar anpassningar.
/// </summary>
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
            // Registrera medlemslogotyper
            await EnsureMemberLogosAsync(env, logger, dbContext);

            // Kopiera och registrera feature-bilder
            await EnsureFeatureImagesAndRegisterAsync(env, logger, dbContext);

            // Seeda standardinnehåll
            await SeedDefaultContentAsync(dbContext, logger);

            // Seeda intro-sektioner och feature-sektioner per sida
            await SeedIntroSectionAsync(serviceProvider, logger);
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "home");

            await SeedOmkonsortietsIntroSectionAsync(serviceProvider, logger);
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "omkonsortiet");

            await SeedVisionerIntroSectionAsync(serviceProvider, logger);
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "visioner");

            await SeedDokumentIntroSectionAsync(serviceProvider, logger);
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "dokument");

            await SeedOmsystemetIntroSectionAsync(serviceProvider, logger);
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger, "omsystemet");

            // FAQ + förvaltningssidor
            await SeedFaqSectionsAsync(serviceProvider, logger);
            await SeedForvaltningIntroSectionAsync(serviceProvider, logger);
            await SeedKontaktaossAsync(serviceProvider, logger);
            await SeedContactInformationAsync(serviceProvider, logger);

            // Medlemsnytt
            await SeedMedlemsnyttIntroSectionAsync(serviceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ett fel uppstod vid initiering av innehåll.");
            throw;
        }
    }

    // ---------------------------
    // Förvaltningssidan: intro
    // ---------------------------

    // Returnerar intro-sektion för förvaltningssidan.
    private static dynamic GetForvaltningIntroSection()
    {
        return new
        {
            title = "Handlingsplan och utvecklingsförslag",
            content = @"<p>Här finns konsortiets handlingsplan (road map) för 2024 och framåt. Denna uppdateras löpande.</p>
                       <p>Sist på sidan hittar du också ett formulär för utvecklingsförslag, där du kan du skicka in dina utvecklingsförslag direkt till styrgruppen. Glöm inte att vara tydlig när du beskriver ditt förslag för att undvika missförstånd.</p>",
            hasImage = false,
            imageUrl = "",
            imageAltText = "",
            breadcrumbTitle = "FÖRVALTNING"
        };
    }

    // Seedar eller uppdaterar intro-sektionen för förvaltningssidan.
    private static async Task SeedForvaltningIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var forvaltningContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "forvaltning");
            if (forvaltningContent == null)
            {
                logger.LogDebug("Skapar ContentBlock för förvaltningssidan...");

                var introSection = GetForvaltningIntroSection();

                var sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "ActionPlanTable", IsEnabled = true, SortOrder = 2 },
                    new { Type = "DevelopmentSuggestionForm", IsEnabled = true, SortOrder = 3 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                };

                var metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    sectionConfig,
                    lastConfigUpdate = DateTime.UtcNow
                });

                forvaltningContent = new ContentBlock
                {
                    PageKey = "forvaltning",
                    Title = "Förvaltning",
                    HtmlContent = "<p>Handlingsplan och utvecklingsförslag för KronoX-konsortiet.</p>",
                    Metadata = metadata,
                    LastModified = DateTime.UtcNow
                };

                dbContext.ContentBlocks.Add(forvaltningContent);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Förvaltning ContentBlock har skapats och seedats.");
                return;
            }

            // Respektera eventuella anpassningar
            if (!string.IsNullOrEmpty(forvaltningContent.Metadata))
            {
                try
                {
                    var md = JsonDocument.Parse(forvaltningContent.Metadata);
                    if (md.RootElement.TryGetProperty("introSection", out var existing))
                    {
                        bool hasCustomization = existing.TryGetProperty("breadcrumbTitle", out _) ||
                                                existing.TryGetProperty("showNavigationButtons", out _) ||
                                                existing.TryGetProperty("navigationButtons", out _);
                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (förvaltning) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // korrupt metadata hanteras nedan
                }
            }

            var introSectionData = GetForvaltningIntroSection();

            if (string.IsNullOrEmpty(forvaltningContent.Metadata))
            {
                var sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "ActionPlanTable", IsEnabled = true, SortOrder = 2 },
                    new { Type = "DevelopmentSuggestionForm", IsEnabled = true, SortOrder = 3 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                };

                forvaltningContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection = introSectionData,
                    sectionConfig,
                    lastConfigUpdate = DateTime.UtcNow
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(forvaltningContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    bool hasIntroSection = false;
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "introSection")
                        {
                            hasIntroSection = true;
                            property.WriteTo(writer); // behåll befintlig
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!hasIntroSection)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSectionData.title);
                        writer.WriteString("content", introSectionData.content);
                        writer.WriteBoolean("hasImage", introSectionData.hasImage);
                        writer.WriteString("imageUrl", introSectionData.imageUrl);
                        writer.WriteString("imageAltText", introSectionData.imageAltText);
                        writer.WriteString("breadcrumbTitle", introSectionData.breadcrumbTitle);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    writer.Flush();
                    forvaltningContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    var sectionConfig = new[]
                    {
                        new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                        new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                        new { Type = "ActionPlanTable", IsEnabled = true, SortOrder = 2 },
                        new { Type = "DevelopmentSuggestionForm", IsEnabled = true, SortOrder = 3 },
                        new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                    };

                    forvaltningContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection = introSectionData,
                        sectionConfig,
                        lastConfigUpdate = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogDebug("Seedade/uppdaterade intro-sektion för förvaltningssidan (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för förvaltning");
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
                logger.LogDebug("Feature-sektioner för {PageKey} finns redan. Hoppar över seeding.", pageKey);
                return;
            }

            logger.LogDebug("Seedar feature-sektioner för {PageKey}...", pageKey);

            var featureSections = pageKey switch
            {
                "home" => GetDefaultFeatureSections(),
                "omkonsortiet" => GetOmkonsortietsFeatureSections(),
                "visioner" => GetVisionerFeatureSections(),
                "dokument" => GetDokumentFeatureSections(),
                "omsystemet" => GetOmsystemetFeatureSections(),
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
                    ImageUrl = imageUrl,
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
            logger.LogDebug("Seedade {Count} feature-sektioner för {PageKey}", sortOrder, pageKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av feature-sektioner för {PageKey}", pageKey);
        }
    }

    // ---------------------------
    // Intro-sektioner (standard)
    // ---------------------------

    private static dynamic GetDefaultIntroSection() => new
    {
        title = "Våga släpp taget!",
        content = "<p>Schemaläggning för högre utbildning! Satsa på framtidens schemaläggningssystem! KronoX är ett heltäckande system för såväl avancerad schemaläggning som enklare lokal- och resursbokning via webb och app. Det är anpassat för universitet och högskolor och styrs av medlemmarna med användarna i fokus.</p>"
    };

    private static dynamic GetOmkonsortietsIntroSection() => new
    {
        title = "En sammanslutning av högskolor och universitet",
        content = @"<p>Konsortiet är en sammanslutning av högskolor och universitet för utveckling och drift av schemaläggningssystemet KronoX. Dess hemvist är Högskolan i Borås, som svarar för administration, drift och utveckling. Verksamhetens leds av en styrelse bestående av ledamöter från medlemslärosätena. Verksamheten finansieras genom avgifter från medlemmarna i form av en årsavgift. Årsavgiften bestäms av respektive medlems/lärosätes antal helårsstudenter. Rätten att få utnyttja programvaran KronoX förbehålls konsortiets medlemmar. För mer information kontakta: <a href=""mailto:info@kronox.se"">info@kronox.se</a></p>",
        hasImage = false,
        imageUrl = "",
        imageAltText = ""
    };

    private static dynamic GetVisionerIntroSection() => new
    {
        title = "Vision",
        content = "<p>KronoX är, med sin inriktning mot högskolor och universitet, en viktig aktör på marknaden för system för schemaläggning och relaterade aktiviteter. Som konsortium tillhandahåller KronoX, i nära samverkan med sina medlemmar, efterfrågade tjänster med hög service och kvalitet på ett effektivt sätt. Allt fler lärosäten efterfrågar KronoX tjänster.</p>",
        hasImage = false,
        imageUrl = "",
        imageAltText = ""
    };

    private static dynamic GetDokumentIntroSection() => new
    {
        title = "Ladda ner alla filer du behöver här",
        content = "<p>Här hittar du alla mötesprotokoll, anteckningar, förvaltningsdokument med mera.</p><p>Letar du efter Manualen hittar du den <a href=\"/manual\" class=\"text-decoration-underline text-dark\">här <i class=\"fa fa-arrow-right ms-1\"></i></a></p>",
        hasImage = false,
        imageUrl = "",
        imageAltText = ""
    };

    private static dynamic GetOmsystemetIntroSection() => new
    {
        title = "Varför välja KronoX?",
        content = "<p>Oavsett vem du är och vilken roll du har i organisationen så förenklar KronoX din arbetsdag och ger dig fördelar som många andra schemaläggnings- och resursbokningsprogram saknar.</p>",
        hasImage = false,
        imageUrl = "",
        imageAltText = "",
        breadcrumbTitle = "Om systemet"
    };

    private static dynamic GetMedlemsnyttIntroSection() => new
    {
        title = "För medlemmar",
        content = @"<p>Här hittar du de senaste uppdateringarna och nyheterna om KronoX och konsortiet.</p>
                    <p>Vill du läsa mer om Konsortiets medlemmar och hur de arbetar med KronoX hittar du det under <strong>Hur vi arbetar med KronoX</strong>.</p>
                    <p>Letar du efter kontaktuppgifter till medlemmarna finns det under <strong>Om konsortiet</strong>.</p>
                    <p>Söker du information om handhavande/användarinstruktioner för schemasystemet, hittar du det i <strong>Manualen</strong>.</p>",
        hasImage = false,
        imageUrl = "",
        imageAltText = "",
        breadcrumbTitle = "MEDLEMSNYTT",
        showNavigationButtons = true,
        navigationButtons = new[]
        {
            new { text = "Manualen", url = "/manual", iconClass = "fa-solid fa-book", sortOrder = 0 },
            new { text = "Användarträffar", url = "/anvandartraffar", iconClass = "fa-solid fa-users", sortOrder = 1 },
            new { text = "Hur vi arbetar med KronoX", url = "/omkonsortiet", iconClass = "fa-solid fa-cogs", sortOrder = 2 }
        }
    };

    // ---------------------------
    // Feature-sektioner (standard)
    // ---------------------------

    private static dynamic[] GetDefaultFeatureSections() => new[]
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

    private static dynamic[] GetOmkonsortietsFeatureSections() => new[]
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
                    showPhoneToMembers = false
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
                    showNamePublicly = true,
                    showEmailPublicly = false,
                    showPhonePublicly = false,
                    showOrganizationPublicly = true,
                    showEmailToMembers = true,
                    showPhoneToMembers = true
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
                    showNamePublicly = false,
                    showEmailPublicly = false,
                    showPhonePublicly = false,
                    showOrganizationPublicly = true,
                    showEmailToMembers = true,
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

    private static dynamic[] GetVisionerFeatureSections() => new[]
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

    private static dynamic[] GetDokumentFeatureSections() => new[]
    {
        new {
            title = "Organiserat efter kategorier",
            content = "<p>Alla dokument är organiserade i tydliga kategorier för enkel navigation. Hitta snabbt det du söker genom att browsa efter typ av dokument.</p>",
            imageUrl = "",
            imageAltText = "",
            hasImage = false,
            hasPrivateContent = false,
            privateContent = "",
            contactPersons = new List<object>()
        },
        new {
            title = "Rollbaserad åtkomst",
            content = "<p>Olika dokument visas baserat på din användarroll. Detta säkerställer att känslig information endast är tillgänglig för behöriga personer.</p>",
            imageUrl = "",
            imageAltText = "",
            hasImage = false,
            hasPrivateContent = false,
            privateContent = "",
            contactPersons = new List<object>()
        },
        new {
            title = "Enkel filhantering",
            content = "<p>Ladda ner dokument direkt eller förhandsgranska dem i webbläsaren. Alla filer är optimerade för snabb nedladdning och visning.</p>",
            imageUrl = "",
            imageAltText = "",
            hasImage = false,
            hasPrivateContent = false,
            privateContent = "",
            contactPersons = new List<object>()
        }
    };

    private static dynamic[] GetOmsystemetFeatureSections() => new[]
    {
        new {
            title = "Användarstyrd flexibilitet",
            content = @"<p>KronoX är ett etablerat system för så väl avancerad schemaläggning som enklare lokal- och resursbokning via webb. Det är skapat för användning i universitets- och högskolevärlden, i nära samarbete med alla de professioner som berörs av systemet.</p>
                       <p>Användarna omfattar dels de som möter systemet dagligen och dels de som mest är intresserade av resultatet av övrigas arbete. I den första grupperingen återfinns studenter, lärare, tekniker, centralbokare och tentamensadministratörer. I den senare systemadministratörer, ekonomer, controllers och fastighetsförvaltare.</p>
                       <p>Systemet är utvecklat för att kunna anpassas till olika behov både inom enskilda lärosäten och mellan de olika medlemmarnas varierande arbetssätt och organisationsflöden.</p>
                       <p>KronoX utvecklas av ett konsortium som ägs av sina 11 medlemmar bland universitet och högskolor. Allt fokus ligger på att optimera nyttan för medlemmarna. Användarna styr utvecklingen genom bl.a. regelbundna användarträffar. All support ingår i medlemsavgiften. Det finns inget vinstintresse och konsortieformen garanterar insyn, inflytande och möjligheter att direkt påverka utvecklingen.</p>
                       <p>Att bli medlem kräver ingen upphandling.</p>",
            imageUrl = "",
            imageAltText = "",
            hasImage = false,
            hasPrivateContent = false,
            privateContent = "",
            contactPersons = new List<object>()
        }
    };

    // ---------------------------
    // Standardinnehåll per sida
    // ---------------------------

    private static async Task SeedDefaultContentAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "home"))
        {
            logger.LogDebug("Startsidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogDebug("Seedar standardinnehåll för startsidan...");

            var introSection = GetDefaultIntroSection();
            var featureSections = GetDefaultFeatureSections();

            var metadataJson = JsonSerializer.Serialize(new
            {
                introSection,
                features = featureSections,
                sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "FeatureSections", IsEnabled = true, SortOrder = 2 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 3 }
                },
                lastConfigUpdate = DateTime.UtcNow
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
            logger.LogDebug("Startsidans innehåll seedat.");
        }

        // Om konsortiet
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "omkonsortiet"))
        {
            logger.LogDebug("Om konsortiet-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogDebug("Seedar standardinnehåll för Om konsortiet-sidan...");

            var introSection = GetOmkonsortietsIntroSection();
            var featureSections = GetOmkonsortietsFeatureSections();

            var metadataJson = JsonSerializer.Serialize(new
            {
                introSection,
                features = featureSections,
                sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "FeatureSections", IsEnabled = true, SortOrder = 2 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 3 }
                },
                lastConfigUpdate = DateTime.UtcNow
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
            logger.LogDebug("Om konsortiet-sidans innehåll seedat.");
        }

        // Visioner & Verksamhetsidé
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "visioner"))
        {
            logger.LogDebug("Visioner & Verksamhetsidé-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogDebug("Seedar standardinnehåll för Visioner & Verksamhetsidé-sidan...");

            var visionerIntroSection = GetVisionerIntroSection();
            var visionerFeatureSections = GetVisionerFeatureSections();

            var visionerMetadataJson = JsonSerializer.Serialize(new
            {
                introSection = visionerIntroSection,
                features = visionerFeatureSections,
                sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "FeatureSections", IsEnabled = true, SortOrder = 2 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 3 }
                },
                lastConfigUpdate = DateTime.UtcNow
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
            logger.LogDebug("Visioner & Verksamhetsidé-sidans innehåll seedat.");
        }

        // Dokument
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "dokument"))
        {
            logger.LogDebug("Dokument-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogDebug("Seedar standardinnehåll för Dokument-sidan...");

            var dokumentIntroSection = GetDokumentIntroSection();
            var dokumentFeatureSections = GetDokumentFeatureSections();

            var dokumentMetadataJson = JsonSerializer.Serialize(new
            {
                introSection = dokumentIntroSection,
                features = dokumentFeatureSections,
                sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "DocumentSection", IsEnabled = true, SortOrder = 2 }
                },
                lastConfigUpdate = DateTime.UtcNow
            });

            var dokumentContent = new ContentBlock
            {
                PageKey = "dokument",
                Title = "Dokument",
                HtmlContent = CreateDokumentContent(),
                Metadata = dokumentMetadataJson,
                LastModified = DateTime.UtcNow
            };

            dbContext.ContentBlocks.Add(dokumentContent);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("Dokument-sidans innehåll seedat.");
        }

        // Om systemet
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "omsystemet"))
        {
            logger.LogDebug("Om systemet-sidans innehåll finns redan. Hoppar över seeding.");
        }
        else
        {
            logger.LogDebug("Seedar standardinnehåll för Om systemet-sidan...");

            var omsystemetIntroSection = GetOmsystemetIntroSection();
            var omsystemetFeatureSections = GetOmsystemetFeatureSections();

            var omsystemetMetadataJson = JsonSerializer.Serialize(new
            {
                introSection = omsystemetIntroSection,
                features = omsystemetFeatureSections,
                sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 }
                },
                lastConfigUpdate = DateTime.UtcNow
            });

            var omsystemetContent = new ContentBlock
            {
                PageKey = "omsystemet",
                Title = "Om systemet",
                HtmlContent = CreateOmsystemetContent(),
                Metadata = omsystemetMetadataJson,
                LastModified = DateTime.UtcNow
            };

            dbContext.ContentBlocks.Add(omsystemetContent);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("Om systemet-sidans innehåll seedat.");
        }

        // Andra standardsidor
        var standardPages = new List<(string key, string title)>
        {
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
        logger.LogDebug("Standardsidor seedade (vid behov).");
    }

    // ---------------------------
    // Hjälpmetoder för HTML
    // ---------------------------

    private static string CreateDefaultHomeContent()
    {
        var introSection = GetDefaultIntroSection();
        var featureSections = GetDefaultFeatureSections();
        return BuildHtmlContent(introSection.content, featureSections);
    }

    private static string CreateOmkonsortietsContent()
    {
        var introSection = GetOmkonsortietsIntroSection();
        var featureSections = GetOmkonsortietsFeatureSections();
        return BuildHtmlContent(introSection.content, featureSections);
    }

    private static string CreateVisionerContent()
    {
        var introSection = GetVisionerIntroSection();
        var featureSections = GetVisionerFeatureSections();
        return BuildHtmlContent(introSection.content, featureSections);
    }

    private static string CreateDokumentContent()
    {
        var introSection = GetDokumentIntroSection();
        var featureSections = GetDokumentFeatureSections();
        return BuildHtmlContent(introSection.content, featureSections);
    }

    private static string CreateOmsystemetContent()
    {
        var introSection = GetOmsystemetIntroSection();
        var featureSections = GetOmsystemetFeatureSections();
        return BuildHtmlContent(introSection.content, featureSections);
    }

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

    // ---------------------------
    // Bilder och medlemsloggor
    // ---------------------------

    /// <summary>
    /// Kopierar feature-bilder från seed-mapp och registrerar i databasen om saknas.
    /// </summary>
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
            logger.LogDebug("Seed-bildmapp saknas: {SeedDir}", seedImageDir);
            return;
        }

        // Mappning: filnamn -> (sektion-id, alt-text)
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

            if (!File.Exists(destFile))
            {
                File.Copy(file, destFile);
                logger.LogDebug("Featurebild kopierad: {Destination}", destFile);
            }

            var relativeUrl = $"/images/pages/home/{filename}";

            var sectionInfo = fileMappings.ContainsKey(filename)
                ? fileMappings[filename]
                : (sectionId: "feature:0", altText: filename);

            var existingImage = await dbContext.PageImages
                .FirstOrDefaultAsync(pi => pi.Url == relativeUrl);

            if (existingImage == null)
            {
                dbContext.PageImages.Add(new PageImage
                {
                    PageKey = "home",
                    Url = relativeUrl,
                    // Fix: använd altText (tidigare skrevs sectionId felaktigt till AltText)
                    AltText = sectionInfo.altText
                });

                logger.LogDebug("Registrerad bild i databasen: {FileName}", filename);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    // Kopierar/registrerar medlemslogotyper. Uppdaterar saknade länkar om logga redan finns.
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
            logger.LogDebug("Frontend-mapp för medlemslogotyper: {Path}", frontendMembersDir);

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
                        logger.LogDebug("Kopierade medlemslogotyp från frontend: {FileName}", fileName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Misslyckades med att kopiera logotypen {FileName} från frontend", fileName);
                    }
                }
            }
        }
        else
        {
            logger.LogDebug("Frontend-mapp för medlemslogotyper hittades inte: {Path}", frontendMembersDir);
        }

        if (Directory.Exists(membersDir))
        {
            var logoFiles = Directory.GetFiles(membersDir)
                .Where(f => allowedExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (logoFiles.Any())
            {
                logger.LogDebug("Registrerar {Count} medlemslogotyper i databasen", logoFiles.Count);

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

                        var linkUrl = universityUrls.ContainsKey(fileName) ? universityUrls[fileName] : "";

                        dbContext.MemberLogos.Add(new MemberLogo
                        {
                            Url = relativeUrl,
                            AltText = altText,
                            SortOrd = sortOrder++,
                            LinkUrl = linkUrl
                        });

                        logger.LogDebug("Medlemslogotyp registrerad i databasen: {FileName} med länk: {Url}", fileName, linkUrl);
                    }
                    else
                    {
                        var existingLogo = await dbContext.MemberLogos.FirstOrDefaultAsync(logo => logo.Url == relativeUrl);
                        if (existingLogo != null && string.IsNullOrEmpty(existingLogo.LinkUrl) && universityUrls.ContainsKey(fileName))
                        {
                            existingLogo.LinkUrl = universityUrls[fileName];
                            logger.LogDebug("Uppdaterade länk för befintlig logotyp: {FileName} -> {Url}", fileName, existingLogo.LinkUrl);
                        }
                        else
                        {
                            logger.LogDebug("Medlemslogotyp redan registrerad: {FileName}", fileName);
                        }
                    }
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }

    // ---------------------------
    // Intro-sektioner för specifika sidor (respekterar anpassningar)
    // ---------------------------

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

            if (!string.IsNullOrEmpty(homeContent.Metadata))
            {
                try
                {
                    var metadata = JsonDocument.Parse(homeContent.Metadata);
                    if (metadata.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                               existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                               existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (startsidan) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Korrupt metadata -> skriv om nedan
                }
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

                    bool hasIntroSection = false;
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "introSection")
                        {
                            hasIntroSection = true;
                        }
                        property.WriteTo(writer);
                    }

                    if (!hasIntroSection)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSection.title);
                        writer.WriteString("content", introSection.content);
                        writer.WriteEndObject();
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
            logger.LogDebug("Seedade intro-sektion för startsidan (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion (startsidan)");
        }
    }

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

            if (!string.IsNullOrEmpty(omkonsortietsContent.Metadata))
            {
                try
                {
                    var metadata = JsonDocument.Parse(omkonsortietsContent.Metadata);
                    if (metadata.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                               existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                               existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (Om konsortiet) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignorera och skriv om nedan
                }
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

                    bool hasIntroSection = false;
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "introSection")
                        {
                            hasIntroSection = true;
                            property.WriteTo(writer);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!hasIntroSection)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSection.title);
                        writer.WriteString("content", introSection.content);
                        writer.WriteBoolean("hasImage", introSection.hasImage);
                        writer.WriteString("imageUrl", introSection.imageUrl);
                        writer.WriteString("imageAltText", introSection.imageAltText);
                        writer.WriteEndObject();
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
            logger.LogDebug("Seedade intro-sektion för Om konsortiet (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Om konsortiet");
        }
    }

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

            if (!string.IsNullOrEmpty(visionerContent.Metadata))
            {
                try
                {
                    var metadata = JsonDocument.Parse(visionerContent.Metadata);
                    if (metadata.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                               existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                               existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (Visioner) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignorera och skriv om
                }
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

                    // Skriv alltid introSection om vi kom hit (dvs. inga markerade anpassningar)
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
            logger.LogDebug("Seedade intro-sektion för Visioner & Verksamhetsidé (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Visioner");
        }
    }

    private static async Task SeedDokumentIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dokumentContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "dokument");
            if (dokumentContent == null)
            {
                logger.LogWarning("Kunde inte hitta ContentBlock för Dokument-sidan, skapar ingen intro-sektion.");
                return;
            }

            if (!string.IsNullOrEmpty(dokumentContent.Metadata))
            {
                try
                {
                    var metadata = JsonDocument.Parse(dokumentContent.Metadata);
                    if (metadata.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                               existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                               existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (Dokument) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignorera och skriv om
                }
            }

            var introSection = GetDokumentIntroSection();

            if (string.IsNullOrEmpty(dokumentContent.Metadata))
            {
                dokumentContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    features = GetDokumentFeatureSections()
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(dokumentContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    bool hasIntroSection = false;
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "introSection")
                        {
                            hasIntroSection = true;
                            property.WriteTo(writer);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!hasIntroSection)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSection.title);
                        writer.WriteString("content", introSection.content);
                        writer.WriteBoolean("hasImage", introSection.hasImage);
                        writer.WriteString("imageUrl", introSection.imageUrl);
                        writer.WriteString("imageAltText", introSection.imageAltText);
                        writer.WriteString("breadcrumbTitle", introSection.breadcrumbTitle);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    dokumentContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    dokumentContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        features = GetDokumentFeatureSections()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogDebug("Seedade intro-sektion för Dokument (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Dokument");
        }
    }

    private static async Task SeedOmsystemetIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var omsystemetContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "omsystemet");
            if (omsystemetContent == null)
            {
                logger.LogWarning("Kunde inte hitta ContentBlock för Om systemet-sidan, skapar ingen intro-sektion.");
                return;
            }

            if (!string.IsNullOrEmpty(omsystemetContent.Metadata))
            {
                try
                {
                    var metadata = JsonDocument.Parse(omsystemetContent.Metadata);
                    if (metadata.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                               existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                               existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (Om systemet) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignorera och skriv om
                }
            }

            var introSection = GetOmsystemetIntroSection();

            if (string.IsNullOrEmpty(omsystemetContent.Metadata))
            {
                omsystemetContent.Metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    features = GetOmsystemetFeatureSections()
                });
            }
            else
            {
                try
                {
                    var metadata = JsonDocument.Parse(omsystemetContent.Metadata);
                    var root = metadata.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    bool hasIntroSection = false;
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "introSection")
                        {
                            hasIntroSection = true;
                            property.WriteTo(writer);
                        }
                        else
                        {
                            property.WriteTo(writer);
                        }
                    }

                    if (!hasIntroSection)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSection.title);
                        writer.WriteString("content", introSection.content);
                        writer.WriteBoolean("hasImage", introSection.hasImage);
                        writer.WriteString("imageUrl", introSection.imageUrl);
                        writer.WriteString("imageAltText", introSection.imageAltText);
                        writer.WriteString("breadcrumbTitle", introSection.breadcrumbTitle);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    omsystemetContent.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    omsystemetContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        features = GetOmsystemetFeatureSections()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogDebug("Seedade intro-sektion för Om systemet (vid behov).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för Om systemet");
        }
    }

    // ---------------------------
    // FAQ och kontakt
    // ---------------------------

    private static async Task SeedFaqSectionsAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await dbContext.FaqSections.AnyAsync(fs => fs.PageKey == "omsystemet"))
            {
                logger.LogDebug("FAQ-sektioner för omsystemet finns redan. Hoppar över seeding.");
                return;
            }

            logger.LogDebug("Seedar FAQ-sektioner för Om systemet-sidan...");

            var faqSection = new FaqSection
            {
                PageKey = "omsystemet",
                Title = "Enkelt, kraftfullt och flexibelt",
                Description = "Här hittar du svar på vanliga frågor om KronoX-systemet och dess funktioner.",
                SortOrder = 0,
                FaqItems = new List<FaqItem>
                {
                    new FaqItem
                    {
                        Question = "Förenklar livet för studenter",
                        Answer = "<p>KronoX gör det enkelt att oavsett var du befinner dig, via webben eller vår app (för iPhone och Android) lösa dina schemaproblem - från det mest självklara, att få ut ditt schema, till att boka grupprum och av-/anmäla dig till tentor.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 0
                    },
                    new FaqItem
                    {
                        Question = "Underlätta vardagen för lärare",
                        Answer = "<p>Som lärare får du förstås också ut ditt schema med samma lätthet som dina studenter. Långt före kursstart har KronoX redan börjat förenkla din tillvaro. Kopiera upplägg för hela kurser, lägg beställningar till centrala schemaläggare eller boka helt på egen hand. Lista dina krav på lokaler och hjälpmedel och få förslag på lediga resurser för de tillfällen du vill boka. Slipp problem med dubbelbokade lokaler och bli påmind i tid före tentamenstillfället om när det är dags att lämna in provet.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 1
                    },
                    new FaqItem
                    {
                        Question = "Ger professionella schemaläggare ett genomtänkt stöd",
                        Answer = "<p>Som professionell schemaläggare och centralbokare behöver du ett system som enkelt låter dig lägga schemat så som du behöver ha det, med stöd för att kunna undvika dubbelbokningar och möjlighet att kringgå dessa spärrar när verksamheten så kräver. Schemalägg enkelt på olika gruppnivåer utan att fundera över schemavisning för studenter då systemet hanterar det åt dig. Du handskas även smidigt med lärares önskemålsbokningar och får stöd av systemet för att hitta förslag på lediga resurser som uppfyller önskemålen, till lägsta möjliga kostnad. Systemet ger bara förslag, du bestämmer!</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 2
                    },
                    new FaqItem
                    {
                        Question = "Förenklad administration för tentamensadministratörer",
                        Answer = "<p>Minska dubbeladministrationen genom att du kan arbeta mot Ladok inne i KronoX och skapa både tentamenstillfällen, samt av-/anmäla studenter och hantera beviljat stöd så att du enkelt kan placera tentanderna på bästa möjliga sätt.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 3
                    },
                    new FaqItem
                    {
                        Question = "Låter ekonomer ägna sig åt annat än interndebitering",
                        Answer = "<p>Spara tid och pengar med marknadens mest kompetenta och flexibla stöd för interndebitering med olika taxor baserade på tid på dygnet, veckodag och klassning av lokaler och hjälpmedel.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 4
                    },
                    new FaqItem
                    {
                        Question = "Ger fastighetsförvaltare bättre statistik och styrmedel",
                        Answer = "<p>Tomma, outnyttjade lokaler kostar pengar. I synnerhet när andra lokaler är överbelagda. Analysera lokalutnyttjandegraden per lokal eller fastighet. Locka fler att boka lågutnyttjade tider genom att sänka hyror och stimulera ett bättre utnyttjande av befintliga resurser, istället för att dra på er ännu fler kostnader för externa lokaler.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 5
                    },
                    new FaqItem
                    {
                        Question = "Möjliggör för controllers att få en bättre överblick",
                        Answer = "<p>Få en överblick över hyreskostnader för genomförda utbildningar (avser endast schemalagd arbetstid, inte övrig arbetstid för lärarkåren som t ex rättning, förberedelser mm) och använd underlaget för uppföljning av gjorda investeringar i t ex hjälpmedel och fast utrustning.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 6
                    },
                    new FaqItem
                    {
                        Question = "Lätt för systemadministratörer att integrera",
                        Answer = "<p>Synkronisera med lätthet data från era kringsystem till KronoX för att slippa manuell administration av användarkonton, studenter, lärare, lokaler, hjälpmedel, kurser och program. Samkör KronoX med Ladok för tentamensadministration via LPW-tjänster och/eller integrera med er befintliga studentportal. Ta emot notifieringar från systemet för händelser till exempelvis växel-/passagesystem för automatisk hänvisning när lärare har undervisning och upplåsning av utrymmen för berörda lärare och studentgrupper.</p>",
                        HasImage = false,
                        ImageUrl = "",
                        ImageAltText = "",
                        SortOrder = 7
                    }
                }
            };

            dbContext.FaqSections.Add(faqSection);
            await dbContext.SaveChangesAsync();

            logger.LogDebug("Seedade {Count} FAQ-items för Om systemet-sidan", faqSection.FaqItems.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av FAQ-sektioner");
        }
    }

    private static async Task SeedKontaktaossAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingContent = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "kontaktaoss");

            if (existingContent == null)
            {
                logger.LogDebug("Seedar standardinnehåll för Kontakta oss-sidan...");

                var introSection = new
                {
                    breadcrumbTitle = "KONTAKTA OSS",
                    title = "Kontakt",
                    content = @"<p>Kontakta oss för mer information. Supportfrågor för KronoX-systemet hanteras av vårt lärosätes KronoX-administratör.</p>
                               <p>Formuläret ska ej användas för lärosätesspecifika frågor, som t ex glömt lösenord eller problem vid anmälan till tentamen (i dessa fall bör ni ta kontakt med aktuellt lärosäte).</p>",
                    hasImage = false,
                    imageUrl = "",
                    imageAltText = "",
                    showNavigationButtons = false,
                    navigationButtons = new object[0]
                };

                var sectionConfig = new[]
                {
                    new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                    new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                    new { Type = "NavigationButtons", IsEnabled = false, SortOrder = 2 },
                    new { Type = "ContactForm", IsEnabled = true, SortOrder = 3 },
                    new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                };

                var metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    sectionConfig,
                    lastConfigUpdate = DateTime.UtcNow
                });

                var kontaktaossContent = new ContentBlock
                {
                    PageKey = "kontaktaoss",
                    Title = "Kontakta oss",
                    HtmlContent = "<p>Kontakta oss för mer information om KronoX-systemet.</p>",
                    Metadata = metadata,
                    LastModified = DateTime.UtcNow
                };

                dbContext.ContentBlocks.Add(kontaktaossContent);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Kontakta oss-sidans innehåll seedat.");
            }
            else
            {
                logger.LogDebug("Kontakta oss-sidans innehåll finns redan. Kontrollerar metadata...");

                bool needsUpdate = false;

                if (string.IsNullOrEmpty(existingContent.Metadata))
                {
                    logger.LogDebug("Metadata saknas helt, lägger till...");
                    needsUpdate = true;
                }
                else
                {
                    try
                    {
                        var metadata = JsonDocument.Parse(existingContent.Metadata);
                        if (!metadata.RootElement.TryGetProperty("sectionConfig", out _))
                        {
                            logger.LogDebug("SectionConfig saknas i metadata, lägger till...");
                            needsUpdate = true;
                        }
                        if (!metadata.RootElement.TryGetProperty("introSection", out _))
                        {
                            logger.LogDebug("IntroSection saknas i metadata, lägger till...");
                            needsUpdate = true;
                        }
                    }
                    catch (JsonException)
                    {
                        logger.LogWarning("Korrupt metadata hittad för 'kontaktaoss', reparerar...");
                        needsUpdate = true;
                    }
                }

                if (needsUpdate)
                {
                    var introSection = new
                    {
                        breadcrumbTitle = "KONTAKTA OSS",
                        title = "Kontakt",
                        content = @"<p>Kontakta oss för mer information. Supportfrågor för KronoX-systemet hanteras av vårt lärosätes KronoX-administratör.</p>
                                   <p>Formuläret ska ej användas för lärosätesspecifika frågor, som t ex glömt lösenord eller problem vid anmälan till tentamen (i dessa fall bör ni ta kontakt med aktuellt lärosäte).</p>",
                        hasImage = false,
                        imageUrl = "",
                        imageAltText = "",
                        showNavigationButtons = false,
                        navigationButtons = new object[0]
                    };

                    var sectionConfig = new[]
                    {
                        new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                        new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                        new { Type = "NavigationButtons", IsEnabled = false, SortOrder = 2 },
                        new { Type = "ContactForm", IsEnabled = true, SortOrder = 3 },
                        new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                    };

                    existingContent.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection,
                        sectionConfig,
                        lastConfigUpdate = DateTime.UtcNow
                    });
                    existingContent.LastModified = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    logger.LogDebug("Kontakta oss metadata har uppdaterats.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av Kontakta oss-sidan");
        }
    }

    private static async Task SeedContactInformationAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await dbContext.PostalAddresses.AnyAsync())
            {
                logger.LogDebug("Seedar postadress...");

                var postalAddress = new PostalAddress
                {
                    OrganizationName = "KronoX",
                    AddressLine1 = "Högskolan i Borås",
                    AddressLine2 = "",
                    PostalCode = "501 90",
                    City = "Borås",
                    Country = "Sverige",
                    LastModified = DateTime.UtcNow
                };

                dbContext.PostalAddresses.Add(postalAddress);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Postadress seedad.");
            }
            else
            {
                logger.LogDebug("Postadress finns redan. Hoppar över seeding.");
            }

            if (!await dbContext.ContactPersons.AnyAsync())
            {
                logger.LogDebug("Seedar kontaktpersoner...");

                var contactPersons = new List<ContactPerson>
                {
                    new ContactPerson
                    {
                        Name = "Per-Anders Månsson",
                        Title = "Konsortiechef",
                        Email = "per-anders.mansson@hb.se",
                        Phone = "033 – 435 43 64",
                        SortOrder = 10,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    },
                    new ContactPerson
                    {
                        Name = "Göran Golcher",
                        Title = "Arkitekt, systemutvecklare",
                        Email = "goran.golcher@hb.se",
                        Phone = "033 – 435 43 64",
                        SortOrder = 20,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    },
                    new ContactPerson
                    {
                        Name = "Petter Lidbeck",
                        Title = "Systemutvecklare",
                        Email = "petter.lidbeck@hb.se",
                        Phone = "033 – 435 46 80",
                        SortOrder = 30,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    },
                    new ContactPerson
                    {
                        Name = "Marie Palmnert",
                        Title = "Administratör",
                        Email = "administrator@kronox.se",
                        Phone = "040 – 665 83 63",
                        SortOrder = 40,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    }
                };

                dbContext.ContactPersons.AddRange(contactPersons);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Seedade {Count} kontaktpersoner.", contactPersons.Count);
            }
            else
            {
                logger.LogDebug("Kontaktpersoner finns redan. Hoppar över seeding.");
            }

            if (!await dbContext.EmailLists.AnyAsync())
            {
                logger.LogDebug("Seedar e-postlistor...");

                var emailLists = new List<EmailList>
                {
                    new EmailList
                    {
                        Name = "Styrelsen",
                        Description = "Kontakta KronoX-konsortiet styrelse direkt",
                        EmailAddress = "styrelsen@kronox.se",
                        SortOrder = 10,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    },
                    new EmailList
                    {
                        Name = "Alla kontaktpersoner på medlemsskolorna",
                        Description = "Samlingslista för alla KronoX-kontaktpersoner vid medlemslärosätena",
                        EmailAddress = "kontakt@kronox.se",
                        SortOrder = 20,
                        IsActive = true,
                        LastModified = DateTime.UtcNow
                    }
                };

                dbContext.EmailLists.AddRange(emailLists);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Seedade {Count} e-postlistor.", emailLists.Count);
            }
            else
            {
                logger.LogDebug("E-postlistor finns redan. Hoppar över seeding.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av kontaktinformation");
        }
    }

    // ---------------------------
    // Medlemsnytt
    // ---------------------------

    private static async Task SeedMedlemsnyttIntroSectionAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var content = await dbContext.ContentBlocks.FirstOrDefaultAsync(c => c.PageKey == "medlemsnytt");
            if (content == null)
            {
                logger.LogDebug("Skapar ContentBlock för medlemsnytt-sidan...");

                var introSection = GetMedlemsnyttIntroSection();

                var metadata = JsonSerializer.Serialize(new
                {
                    introSection,
                    sectionConfig = new[]
                    {
                        new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                        new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                        new { Type = "NavigationButtons", IsEnabled = true, SortOrder = 2 },
                        new { Type = "NewsSection", IsEnabled = true, SortOrder = 3 },
                        new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                    },
                    lastConfigUpdate = DateTime.UtcNow
                });

                content = new ContentBlock
                {
                    PageKey = "medlemsnytt",
                    Title = "Medlemsnytt",
                    HtmlContent = "<p>Senaste nyheterna för KronoX-konsortiet medlemmar.</p>",
                    Metadata = metadata,
                    LastModified = DateTime.UtcNow
                };

                dbContext.ContentBlocks.Add(content);
                await dbContext.SaveChangesAsync();
                logger.LogDebug("Medlemsnytt ContentBlock har skapats och seedats.");
                return;
            }

            // Respektera anpassningar
            if (!string.IsNullOrEmpty(content.Metadata))
            {
                try
                {
                    var md = JsonDocument.Parse(content.Metadata);
                    if (md.RootElement.TryGetProperty("introSection", out var existingIntro))
                    {
                        bool hasCustomization = existingIntro.TryGetProperty("breadcrumbTitle", out _) ||
                                                existingIntro.TryGetProperty("showNavigationButtons", out _) ||
                                                existingIntro.TryGetProperty("navigationButtons", out _);

                        if (hasCustomization)
                        {
                            logger.LogDebug("Intro-sektion (medlemsnytt) har anpassningar, hoppar över seeding.");
                            return;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignorera och skriv om nedan
                }
            }

            var introSectionData = GetMedlemsnyttIntroSection();

            if (string.IsNullOrEmpty(content.Metadata))
            {
                content.Metadata = JsonSerializer.Serialize(new
                {
                    introSection = introSectionData,
                    sectionConfig = new[]
                    {
                        new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                        new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                        new { Type = "NavigationButtons", IsEnabled = true, SortOrder = 2 },
                        new { Type = "NewsSection", IsEnabled = true, SortOrder = 3 },
                        new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                    },
                    lastConfigUpdate = DateTime.UtcNow
                });
            }
            else
            {
                try
                {
                    var md = JsonDocument.Parse(content.Metadata);
                    var root = md.RootElement;

                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms);
                    writer.WriteStartObject();

                    bool hasIntro = false;
                    foreach (var p in root.EnumerateObject())
                    {
                        if (p.Name == "introSection")
                        {
                            hasIntro = true;
                            p.WriteTo(writer);
                        }
                        else
                        {
                            p.WriteTo(writer);
                        }
                    }

                    if (!hasIntro)
                    {
                        writer.WritePropertyName("introSection");
                        writer.WriteStartObject();
                        writer.WriteString("title", introSectionData.title);
                        writer.WriteString("content", introSectionData.content);
                        writer.WriteBoolean("hasImage", introSectionData.hasImage);
                        writer.WriteString("imageUrl", introSectionData.imageUrl);
                        writer.WriteString("imageAltText", introSectionData.imageAltText);
                        writer.WriteString("breadcrumbTitle", introSectionData.breadcrumbTitle);
                        writer.WriteBoolean("showNavigationButtons", introSectionData.showNavigationButtons);

                        writer.WritePropertyName("navigationButtons");
                        writer.WriteStartArray();
                        foreach (var button in introSectionData.navigationButtons)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("text", button.text);
                            writer.WriteString("url", button.url);
                            writer.WriteString("iconClass", button.iconClass);
                            writer.WriteNumber("sortOrder", button.sortOrder);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();

                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                    writer.Flush();

                    content.Metadata = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    content.Metadata = JsonSerializer.Serialize(new
                    {
                        introSection = introSectionData,
                        sectionConfig = new[]
                        {
                            new { Type = "Banner", IsEnabled = true, SortOrder = 0 },
                            new { Type = "Intro", IsEnabled = true, SortOrder = 1 },
                            new { Type = "NavigationButtons", IsEnabled = true, SortOrder = 2 },
                            new { Type = "NewsSection", IsEnabled = true, SortOrder = 3 },
                            new { Type = "MemberLogos", IsEnabled = true, SortOrder = 4 }
                        },
                        lastConfigUpdate = DateTime.UtcNow
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            logger.LogDebug("Seedade intro-sektion för medlemsnytt-sidan");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av intro-sektion för medlemsnytt");
        }
    }
}
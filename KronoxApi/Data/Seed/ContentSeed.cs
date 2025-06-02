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
            await dbContext.Database.EnsureCreatedAsync();

            // Registrera medlemslogotyper
            await EnsureMemberLogosAsync(env, logger, dbContext);

            // Kopiera och registrera feature-bilder
            await EnsureFeatureImagesAndRegisterAsync(env, logger, dbContext);

            // Seeda standardinnehåll
            await SeedDefaultContentAsync(dbContext, logger);

            // Seeda intro-sektion
            await SeedIntroSectionAsync(serviceProvider, logger);

            // Seeda feature-sektioner
            await SeedFeatureSectionsFromMetadataAsync(serviceProvider, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ett fel uppstod vid initiering av innehåll.");
            throw;
        }
    }

    // Seedar feature-sektioner för startsidan om de inte redan finns.
    private static async Task SeedFeatureSectionsFromMetadataAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await dbContext.FeatureSections.AnyAsync(fs => fs.PageKey == "home"))
            {
                logger.LogInformation("Feature-sektioner för startsidan finns redan. Hoppar över seeding.");
                return;
            }

            logger.LogInformation("Seedar feature-sektioner...");

            var featureSections = GetDefaultFeatureSections();
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
                    PageKey = "home",
                    Title = section.title,
                    Content = section.content,
                    ImageUrl = section.imageUrl,
                    ImageAltText = section.imageAltText,
                    HasImage = section.hasImage,
                    SortOrder = sortOrder++
                });
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation($"Seedade {sortOrder} feature-sektioner för startsidan");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fel vid seeding av feature-sektioner");
        }
    }

    // Returnerar standardvärden för intro-sektionen.
    private static dynamic GetDefaultIntroSection()
    {
        return new
        {
            title = "Våga släpp taget!",
            content = "<p>Schemaläggning för högre utbildning! Satsa på framtidens schemaläggningssystem! KronoX är ett heltäckande system " +
                "för såväl avancerad schemaläggning som enklare lokal‑ och resursbokning via webb och app. Det är anpassat för " +
                "universitet och högskolor och styrs av medlemmarna med användarna i fokus.</p>"
        };
    }

    // Returnerar standard-feature-sektioner.
    private static dynamic[] GetDefaultFeatureSections()
    {
        return new[]
        {
        new {
            title = "",
            content = "<p>KronoX är skapat för användning i universitets- och högskolevärlden och utvecklas kontinuerligt i nära samarbete med användare och professionella schemaläggare. KronoX ägs av medlemmarna i form av ett konsortium.</p>",
            imageUrl = "",
            imageAltText = "",
            hasImage = false
        },
        new {
            title = "Oöverträffad flexibilitet",
            content = "<p>KronoX har stora möjligheter att integreras med lärosätenas övriga datasystem, som till exempel kursdatabas och fastighetssystem. KronoX erbjuder möjlighet att anpassa systemet efter lärosätenas behov när det gäller databas, utseende och arbetssätt.</p>",
            imageUrl = "/images/pages/home/KronoX-bokningsdialogen-med-lagerfunktionen.png",
            imageAltText = "Illustration av systemets flexibilitet",
            hasImage = true
        },
        new {
            title = "KronoX tillgodoser vitt skilda behov",
            content = "<p>KronoX erbjuder allt från enkel schemasökning eller bokning av grupprum till komplex och avancerad schemaläggning. Studenter, lärare och erfarna schemaläggare får sina behov tillgodosedda.</p><ul><li>Synkronisering med externa kalendrar för lärare och studenter (ICAL)</li><li>Anpassningsbar kollisionskontroll för den professionella schemaläggaren.</li><li>Anpassning av schemavyn.</li><li>Möjlighet för till exempel studenter att själva boka grupprum via webb eller app.</li><li>Schemaläggningsassistent på webben (möjligt att skapa schemaunderlag som överförs in i systemet)</li></ul>",
            imageUrl = "/images/pages/home/bokningsdialogen.png",
            imageAltText = "Användare med olika behov",
            hasImage = true
        },
        new {
            title = "Övergripande information",
            content = "<p>Systemet ger överblick över lokalnyttjandet både på kursnivå och på lokalnivå. Det ger bra underlag för uttag av statistik och för planering av lärsosätenas lokalförsörjning.</p>",
            imageUrl = "/images/pages/home/oversikt.png",
            imageAltText = "Informationsöverblick",
            hasImage = true
        },
        new {
            title = "Debiteringsfunktion",
            content = "<p>KronoX möjliggör för lärosäten att individuellt sätta priser på lokalanvändningen. Systemet levererar tydliga debiteringsunderlag för fakturering. Möjlighet till tidsintervall i debitering finns att tillgå.</p>",
            imageUrl = "/images/pages/home/debiteringsflik.png",
            imageAltText = "Debiteringsfunktion",
            hasImage = true
        }
    };
    }

    // Seedar standardinnehåll för startsidan och andra standardsidor.
    private static async Task SeedDefaultContentAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.ContentBlocks.AnyAsync(c => c.PageKey == "home"))
        {
            logger.LogInformation("Startsidans innehåll finns redan. Hoppar över seeding.");
            return;
        }

        logger.LogInformation("Börjar seeda standardinnehåll...");

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

        var standardPages = new List<(string key, string title)>
    {
        ("omsystemet", "Om systemet"),
        ("kontaktaoss", "Kontakta oss"),
        ("omkonsortiet", "Om konsortiet"),
        ("visioner", "Visioner & Verksamhetsidé")
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
        logger.LogInformation("Standardinnehåll har seedats framgångsrikt.");
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

                        dbContext.MemberLogos.Add(new MemberLogo
                        {
                            Url = relativeUrl,
                            AltText = altText,
                            SortOrd = sortOrder++,
                            LinkUrl = ""
                        });

                        logger.LogInformation($"Medlemslogotyp registrerad i databasen: {fileName}");
                    }
                    else
                    {
                        logger.LogInformation($"Medlemslogotyp redan registrerad i databasen: {fileName}");
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

    // Skapar HTML för startsidan baserat på standardvärden.
    private static string CreateDefaultHomeContent()
    {
        var introSection = GetDefaultIntroSection();
        var featureSections = GetDefaultFeatureSections();

        return BuildHtmlContent(introSection.content, featureSections);
    }

    // Hjälpmetod för att bygga HTML-innehållet för startsidan.
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
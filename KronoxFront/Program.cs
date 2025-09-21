using KronoxFront.Components;
using KronoxFront.DTOs;
using KronoxFront.Middleware;
using KronoxFront.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace KronoxFront;

public class Program
{

    // DTO som matchar <input name="…"> i formuläret
    public record LoginDto(string Username, string Password);

    // Minimal användartyp – behövs bara för SignInManager-generics
    public class ApplicationUser : IdentityUser { }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Lägg till Blazor och cookie-baserad autentisering
        builder.Services.AddRazorComponents()
               .AddInteractiveServerComponents(options =>
               {
                   // Ökad timeouts för filuppladdning
                   options.DisconnectedCircuitMaxRetained = 100;
                   options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
                   options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
                   options.MaxBufferedUnacknowledgedRenderBatches = 20;
               });

        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 20 * 1024 * 1024; // 20MB
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(300);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        });

        builder.Services.AddAuthentication("KronoxAuth")
               .AddCookie("KronoxAuth", o =>
               {
                   o.Cookie.Name = "KronoxAuth";
                   o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                   o.Cookie.SameSite = SameSiteMode.Strict;
                   o.SlidingExpiration = true;
                   o.ExpireTimeSpan = TimeSpan.FromHours(2);
                   o.LoginPath = new PathString("/404");

                   o.Events = new CookieAuthenticationEvents
                   {
                       OnRedirectToLogin = context =>
                       {
                           // Omdirigera till 404 istället för login
                           context.Response.Redirect("/404");
                           return Task.CompletedTask;
                       },
                       OnRedirectToAccessDenied = context =>
                       {
                           // Även omdirigera access denied till 404
                           context.Response.Redirect("/404");
                           return Task.CompletedTask;
                       }
                   };
               });

        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                    !context.User.IsInRole("Ny användare") &&
                    context.User.Identity.IsAuthenticated)
                .Build();

            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));
        });

        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddHttpContextAccessor();

        // Registrera ApiAuthHandler
        builder.Services.AddTransient<ApiAuthHandler>();

        // Använd appsettings.json för API-adressen
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7231/";

        // Registrera named HttpClient med ApiAuthHandler
        builder.Services.AddHttpClient("KronoxAPI", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        // Registrera typed HttpClient för CmsService med ApiAuthHandler
        builder.Services.AddHttpClient<CmsService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        builder.Services.AddHttpClient<DocumentService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        builder.Services.AddHttpClient<CategoryService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        builder.Services.AddHttpClient<NewsService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        builder.Services.AddHttpClient<HealthService>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<ApiAuthHandler>();

        // Lägg till memory cache
        builder.Services.AddMemoryCache();
        // Konfigurera Memory Cache med limits
        builder.Services.Configure<MemoryCacheOptions>(options =>
        {
            options.SizeLimit = 1000; // Max 1000 cache-poster
            options.CompactionPercentage = 0.25; // Rensa 25% när limiten nås
        });

        builder.Services.AddScoped<CacheService>();

        builder.Services.AddScoped<IToastService, ToastService>();

        builder.Services.AddResponseCompression(options => {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/javascript", "text/css", "image/svg+xml" });
        });

        // Konfiguration av loggning
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Logging.AddFilter("KronoxFront.Services.ApiAuthHandler", LogLevel.Debug);
            builder.Logging.AddFilter("KronoxFront.Services.CacheService", LogLevel.Debug);
        }

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        // Proxy-endpoint för inloggning
        app.MapPost("/auth/login", async (HttpContext ctx,
                                 IFormCollection form,
                                 IHttpClientFactory http) =>
        {
            try
            {
                var dto = new LoginDto(
                    form["username"].ToString(),
                    form["password"].ToString());

                var api = http.CreateClient("KronoxAPI");
                var apiRs = await api.PostAsJsonAsync("api/auth/login", dto);

                // Hantera olika typer av felstatus
                if (!apiRs.IsSuccessStatusCode)
                {
                    var errorCode = apiRs.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "credentials", // Fel användarnamn/lösenord
                        System.Net.HttpStatusCode.Forbidden => "unauthorized",   // Obehörig
                        _ => "unknown"                                           // Okänt fel
                    };
                    return Results.Redirect($"/?loginerror={errorCode}");
                }

                var user = await apiRs.Content.ReadFromJsonAsync<UserDto>();

                // Kontrollera om användaren har rollen "Ny användare" och förhindra inloggning
                if (user!.Roles.Contains("Ny användare"))
                {
                    return Results.Redirect("/?loginerror=newuserlogin");
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user!.UserId),
                    new(ClaimTypes.Name,           user.UserName)
                };
                claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

                await ctx.SignInAsync("KronoxAuth",
                    new ClaimsPrincipal(new ClaimsIdentity(claims, "KronoxAuth")));

                // Hämta returnUrl från formuläret
                var returnUrl = form["returnUrl"].ToString();
                
                // Om ingen returnUrl finns eller om den inte är lokal, använd /medlemsnytt
                if (string.IsNullOrEmpty(returnUrl) || !IsLocalUrl(ctx, returnUrl))
                {
                    returnUrl = "/medlemsnytt";
                }

                return Results.Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                // Logga undantaget
                var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Fel vid inloggning");

                // Omdirigera användaren med ett generiskt fel
                return Results.Redirect("/?loginerror=unknown");
            }
        })
        // stäng av CSRF-kravet just här
        .DisableAntiforgery();

        // Proxy-endpoint för utloggning
        app.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync("KronoxAuth");
            return Results.Redirect("/");        // 302
        })
        .DisableAntiforgery();

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<PageAuthorizationMiddleware>();

        // Explicit hantering av robots.txt
        app.Map("/robots.txt", async context =>
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("User-agent: *\nDisallow:");
        });

        // Mappa Blazor-komponenter
        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();
        // Middleware för att automatiskt hämta saknade bilder från API
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            // Kontrollera om det är en bildsökväg vi är intresserade av
            if (path.StartsWithSegments("/images/pages") || path.StartsWithSegments("/images/members"))
            {
                // Skapa lokal filsökväg
                var localPath = Path.Combine(app.Environment.WebRootPath, path.Value.TrimStart('/'));

                // Om filen inte finns lokalt, försök hämta den från API
                if (!File.Exists(localPath))
                {
                    // Skapa mapp om den inte finns
                    Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Bild saknas lokalt, hämtar från API: {Path}", path);

                    try
                    {
                        // Hämta HTTP-klient för API-anrop
                        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var client = httpClientFactory.CreateClient("KronoxAPI");

                        // Hämta bilden från API
                        var response = await client.GetAsync(path.Value);

                        if (response.IsSuccessStatusCode)
                        {
                            // Spara bilden lokalt
                            using (var fileStream = new FileStream(localPath, FileMode.Create))
                            {
                                await response.Content.CopyToAsync(fileStream);
                            }

                            logger.LogInformation("Bild kopierad från API och sparad lokalt: {Path}", localPath);

                            // Returnera bilden direkt (istället för att fortsätta till StaticFiles middleware)
                            context.Response.ContentType = GetContentType(Path.GetExtension(localPath));
                            await context.Response.SendFileAsync(localPath);
                            return;
                        }
                        else
                        {
                            logger.LogWarning("Kunde inte hämta bild från API: {Path}, Status: {Status}",
                                path, response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Fel vid hämtning av bild från API: {Path}", path);
                    }
                }
            }

            // Fortsätt med nästa middleware om vi inte har hanterat förfrågan här
            await next();
        });

        app.UseResponseCompression();

        // Hjälpfunktion för att bestämma content-type för olika filtyper
        string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        // Hjälpmetod för att validera lokala URL:er
        static bool IsLocalUrl(HttpContext context, string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Måste börja med /
            if (!url.StartsWith("/"))
            {
                return false;
            }

            // Får inte vara en absolut URI
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                return false;
            }

            // Får inte innehålla dubbla slashar (förutom i query string)
            var queryIndex = url.IndexOf('?');
            var pathPart = queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
            if (pathPart.Contains("//"))
            {
                return false;
            }

            return true;
        }

        app.Run();
    }
}
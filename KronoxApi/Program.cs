using KronoxApi.Data;
using KronoxApi.Data.Seed;
using KronoxApi.Models;
using KronoxApi.Services;
using KronoxApi.SwaggerFilter;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

namespace KronoxApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Konfigurera databas och Identity
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' saknas.");

        builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));

        // Lägg till Identity för användarhantering
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Lägg till stöd för Authorization
        builder.Services.AddAuthorization();

        // CORS för Blazor/Frontend
        builder.Services.AddCors(p =>
        {
            p.AddPolicy("AllowBlazor", cors =>
            {
                var trustedOrigins = builder.Configuration.GetSection("ApiSettings:TrustedOrigins").Get<string[]>() ?? Array.Empty<string>();
                cors.WithOrigins(trustedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Lägg till controllers och Swagger för API-dokumentation
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "KronoxApi",
                    Version = "v1",
                    Description = "API för KronoX. Använd headern 'X-API-Key' med din API-nyckel för skyddade endpoints. Vissa endpoints kräver dessutom inloggning/roll (t.ex. Admin)."
                });
                o.OperationFilter<SwaggerFileOperationFilter>();
                o.CustomSchemaIds(type => type.FullName);

                o.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });

                o.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-API-Key",
                    Description = "API-nyckel för autentisering"
                });
                o.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }, Array.Empty<string>() }
                });
            });
        }

        // Lägg till e-posttjänst
        builder.Services.AddTransient<IEmailService, MailKitEmailService>();
        builder.Services.AddOptions<EmailTemplates>()
            .Bind(builder.Configuration.GetSection("EmailTemplates"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IRoleValidationService, RoleValidationService>();
        builder.Services.AddScoped<EmailTemplateService>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            // När en icke‐autentiserad träffar [Authorize] på /api/*
            options.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                // annars vanlig redirect till Login-sida
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };

            // När en inloggad utan rätt roll träffar [Authorize(Roles="Admin")]
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        builder.Services.AddHealthChecks();

        //Rate limiting för att förhindra överbelastningsattacker
        builder.Services.AddRateLimiter(options =>
        {
            // HUVUDLIMITER - mer generös för att hantera cache bursts
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "localhost",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 2000,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // SPECIFIK LIMITER för API-anrop
            options.AddPolicy("API", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1500,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // STRIKTARE för admin-operationer
            options.AddPolicy("Admin", httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.User?.Identity?.Name ?? "anonymous",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 60,
                        AutoReplenishment = true
                    }));

            // Upload policy för filuppladdningar
            options.AddPolicy("Upload", httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "localhost",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 5,
                        AutoReplenishment = true
                    }));
        });

        // Konfiguration av extra loggning för säkerhet
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
        }

        var app = builder.Build();

        //  Middleware-pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        // Statisk filserver + headers
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
            OnPrepareResponse = ctx =>
            {
                // CORS för bilder: öppna GET
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");

                // Cache images för prestanda
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
            }
        });

        // Skapa mappar vid behov
        var webRootPath = app.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "members"));
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "pages"));
        var privateStoragePath = Path.Combine(app.Environment.ContentRootPath, "PrivateStorage", "documents");
        Directory.CreateDirectory(privateStoragePath);

        app.UseHttpsRedirection();
        app.UseCors("AllowBlazor");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRateLimiter();

        app.MapHealthChecks("/health");

        // Mappa controllers
        app.MapControllers();

        // Initiera seed-data
        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.SeedAllAsync(builder.Configuration);
        }

        app.Run();
    }
}
﻿using KronoxApi.Data;
using KronoxApi.Data.Seed;
using KronoxApi.Models;
using KronoxApi.Services;
using KronoxApi.SwaggerFilter;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        builder.Services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(connectionString));

        // Lägg till Identity för användarhantering
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Lägg till stöd för Authorization
        builder.Services.AddAuthorization();

        // Konfigurera CORS-policy för att tillåta Blazor-applikationen
        builder.Services.AddCors(p =>
        {
            p.AddPolicy("AllowBlazor", p =>
            {
                var trustedOrigins = builder.Configuration.GetSection("ApiSettings:TrustedOrigins")
                .Get<string[]>() ?? new[] { "https://localhost:7122" };

                p.WithOrigins("https://localhost:7122")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
            });
        });

        // Lägg till controllers och Swagger för API-dokumentation
        builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "KronoxApi", Version = "v1" });
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
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        // Lägg till e-posttjänst
        builder.Services.AddTransient<IEmailService, MailKitEmailService>();
        builder.Services.Configure<EmailTemplates>(builder.Configuration.GetSection("EmailTemplates"));

        builder.Services.AddScoped<IFileService, FileService>();
        //builder.Services.AddScoped<ICmsService, CmsService>();

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
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "localhost",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
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

        app.UseStaticFiles();

        var webRootPath = app.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "members"));
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "pages"));

        // Skapa privatStorage för säker dokumentlagring
        var privateStoragePath = Path.Combine(app.Environment.ContentRootPath, "PrivateStorage", "documents");
        Directory.CreateDirectory(privateStoragePath);
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "members"));
        Directory.CreateDirectory(Path.Combine(webRootPath, "images", "pages"));

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
            await scope.ServiceProvider.SeedContentAsync();
        }

        app.Run();
    }
}
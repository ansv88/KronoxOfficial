using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KronoxApi.Models;

namespace KronoxApi.Data;

// Databascontext för applikationen. Hanterar alla entiteter och deras konfiguration.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Tabeller för olika entiteter i databasen
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<NewsModel> NewsModel { get; set; } = null!;
    public DbSet<MainCategory> MainCategories { get; set; } = null!;
    public DbSet<SubCategory> SubCategories { get; set; } = null!;
    public DbSet<ContentBlock> ContentBlocks { get; set; } = null!;
    public DbSet<PageImage> PageImages { get; set; } = null!;
    public DbSet<FeatureSection> FeatureSections { get; set; } = null!;
    public DbSet<MemberLogo> MemberLogos { get; set; } = null!;

    // Konfigurerar entiteter och relationer i modellen.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // CMS och övriga relationer/konfigurationer

        // Unik index på PageKey för ContentBlock
        builder.Entity<ContentBlock>()
            .HasIndex(cb => cb.PageKey)
            .IsUnique();

        // Index på PageKey för PageImage (ej unik)
        builder.Entity<PageImage>()
            .HasIndex(pi => pi.PageKey);

        // En ContentBlock kan ha flera PageImages, kopplade via PageKey
        builder.Entity<ContentBlock>()
            .HasMany(cb => cb.PageImages)
            .WithOne(pi => pi.ContentBlock)
            .HasForeignKey(pi => pi.PageKey)
            .HasPrincipalKey(cb => cb.PageKey)
            .OnDelete(DeleteBehavior.Cascade);

        // Index på SortOrd för MemberLogo (för sortering i karusell)
        builder.Entity<MemberLogo>()
            .HasIndex(l => l.SortOrd);
    }
}
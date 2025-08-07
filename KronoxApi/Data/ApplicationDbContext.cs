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
    public DbSet<FaqSection> FaqSections { get; set; } = null!;
    public DbSet<FaqItem> FaqItems { get; set; } = null!;

    // Konfigurerar entiteter och relationer i modellen.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Konfigurera MainCategory
        builder.Entity<MainCategory>(entity =>
        {
            entity.HasKey(mc => mc.Id);
            
            entity.Property(mc => mc.Name)
                  .IsRequired()
                  .HasMaxLength(255);
            
            // Konfigurera AllowedRoles som CSV-kolumn
            var allowedRolesProperty = entity.Property(mc => mc.AllowedRoles)
                  .HasConversion(
                      v => string.Join(',', v), // Till databas: "Admin,Styrelse,Medlem"
                      v => string.IsNullOrEmpty(v) 
                          ? new List<string>() 
                          : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .HasColumnType("nvarchar(max)");

            // Konfigurera value comparer separat
            allowedRolesProperty.Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            
            entity.Property(mc => mc.IsActive)
                  .HasDefaultValue(true);
            
            entity.Property(mc => mc.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });

        // Konfigurera Document
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            
            entity.Property(d => d.FileName)
                  .IsRequired()
                  .HasMaxLength(255);
            
            entity.Property(d => d.FilePath)
                  .IsRequired()
                  .HasMaxLength(500);
            
            // Konfigurera SubCategories som CSV
            var subCategoriesProperty = entity.Property(d => d.SubCategories)
                  .HasConversion(
                      v => string.Join(',', v.Select(id => id.ToString())),
                      v => string.IsNullOrEmpty(v) 
                          ? new List<int>() 
                          : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(int.Parse).ToList()
                  )
                  .HasColumnType("nvarchar(max)");

            // Konfigurera value comparer separat
            subCategoriesProperty.Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<int>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
            
            entity.Property(d => d.IsArchived)
                  .HasDefaultValue(false);
            
            entity.Property(d => d.UploadedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(d => d.ArchivedBy)
                  .HasMaxLength(256);
            
            // Foreign Key till MainCategory
            entity.HasOne(d => d.MainCategory)
                  .WithMany(mc => mc.Documents)
                  .HasForeignKey(d => d.MainCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Indexes för prestanda
        builder.Entity<MainCategory>()
               .HasIndex(mc => mc.IsActive);
        
        builder.Entity<Document>()
               .HasIndex(d => new { d.MainCategoryId, d.IsArchived });
        
        builder.Entity<Document>()
               .HasIndex(d => d.UploadedAt);

        // Existing CMS configurations...
        builder.Entity<ContentBlock>()
            .HasIndex(cb => cb.PageKey)
            .IsUnique();

        builder.Entity<PageImage>()
            .HasIndex(pi => pi.PageKey);

        builder.Entity<ContentBlock>()
            .HasMany(cb => cb.PageImages)
            .WithOne(pi => pi.ContentBlock)
            .HasForeignKey(pi => pi.PageKey)
            .HasPrincipalKey(cb => cb.PageKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MemberLogo>()
            .HasIndex(l => l.SortOrd);

        // FAQ-konfiguration (befintlig)
        builder.Entity<FaqSection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasMany(e => e.FaqItems)
                  .WithOne(e => e.FaqSection)
                  .HasForeignKey(e => e.FaqSectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FaqItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Question).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Answer).IsRequired();
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ImageAltText).HasMaxLength(200);
        });
    }
}
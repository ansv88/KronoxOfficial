using KronoxApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KronoxApi.Data;

/// <summary>
/// Databascontext för applikationen (Identity + domänmodeller).
/// Innehåller konfigurationer för CSV-konverteringar, index och relationer.
/// Notera att CSV-konverterade listor (t.ex. AllowedRoles, SubCategories) har ValueComparer
/// för korrekt change tracking i EF Core.
/// </summary>
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
    public DbSet<PostalAddress> PostalAddresses { get; set; } = null!;
    public DbSet<ContactPerson> ContactPersons { get; set; } = null!;
    public DbSet<EmailList> EmailLists { get; set; } = null!;
    public DbSet<NewsDocument> NewsDocuments { get; set; } = null!;
    public DbSet<ActionPlanTable> ActionPlanTables { get; set; }
    public DbSet<ActionPlanItem> ActionPlanItems { get; set; }
    public DbSet<DevelopmentSuggestion> DevelopmentSuggestions { get; set; }
    public DbSet<CustomPage> CustomPages { get; set; } = null!;
    public DbSet<NavigationConfig> NavigationConfigs { get; set; } = null!;

    /// <summary>
    /// Konfigurerar entiteter, relationer, index och konverteringar i modellen.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // MainCategory
        builder.Entity<MainCategory>(entity =>
        {
            entity.HasKey(mc => mc.Id);

            entity.Property(mc => mc.Name)
                  .IsRequired()
                  .HasMaxLength(255);

            // AllowedRoles lagras som CSV i databasen.
            // ValueComparer krävs för korrekt change tracking av listor i EF Core.
            var allowedRolesProperty = entity.Property(mc => mc.AllowedRoles)
                  .HasConversion(
                      v => string.Join(',', v), // Till databas: "Admin,Styrelse,Medlem"
                      v => string.IsNullOrEmpty(v)
                          ? new List<string>()
                          : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  )
                  .HasColumnType("nvarchar(max)");

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

        // Document
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.FileName)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(d => d.FilePath)
                  .IsRequired()
                  .HasMaxLength(500);

            // SubCategories lagras som CSV (lista av int)
            var subCategoriesProperty = entity.Property(d => d.SubCategories)
                  .HasConversion(
                      v => string.Join(',', v.Select(id => id.ToString())),
                      v => string.IsNullOrEmpty(v)
                          ? new List<int>()
                          : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(int.Parse).ToList()
                  )
                  .HasColumnType("nvarchar(max)");

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

            // Foreign key till MainCategory (Restrict för att skydda dokument)
            entity.HasOne(d => d.MainCategory)
                  .WithMany(mc => mc.Documents)
                  .HasForeignKey(d => d.MainCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CustomPage
        builder.Entity<CustomPage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.NavigationType).HasMaxLength(20);
            entity.Property(e => e.ParentPageKey).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);

            var requiredRolesProperty = entity.Property(e => e.RequiredRoles)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => string.IsNullOrEmpty(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                  );

            requiredRolesProperty.Metadata.SetValueComparer(
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            entity.HasIndex(e => e.PageKey).IsUnique();
        });

        // NavigationConfig
        builder.Entity<NavigationConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PageKey).IsUnique();
            entity.Property(e => e.PageKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ItemType).IsRequired().HasMaxLength(20);
        });

        // Indexer för prestanda (vanliga queries)
        builder.Entity<MainCategory>()
               .HasIndex(mc => mc.IsActive);

        builder.Entity<Document>()
               .HasIndex(d => new { d.MainCategoryId, d.IsArchived });

        builder.Entity<Document>()
               .HasIndex(d => d.UploadedAt);

        builder.Entity<ContentBlock>()
            .HasIndex(cb => cb.PageKey)
            .IsUnique();

        builder.Entity<PageImage>()
            .HasIndex(pi => pi.PageKey);

        // ContentBlock → PageImages (via PageKey)
        builder.Entity<ContentBlock>()
            .HasMany(cb => cb.PageImages)
            .WithOne(pi => pi.ContentBlock)
            .HasForeignKey(pi => pi.PageKey)
            .HasPrincipalKey(cb => cb.PageKey)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MemberLogo>()
            .HasIndex(l => l.SortOrd);

        // FAQ
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

        // PostalAddress
        builder.Entity<PostalAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AddressLine2).HasMaxLength(100);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.City).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(50);
        });

        // ContactPerson
        builder.Entity<ContactPerson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.HasIndex(e => e.SortOrder);
        });

        // EmailList
        builder.Entity<EmailList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EmailAddress).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SortOrder);
        });

        // NewsDocument relationer
        builder.Entity<NewsDocument>(entity =>
        {
            entity.HasKey(nd => nd.Id);

            entity.HasOne(nd => nd.News)
                .WithMany(n => n.NewsDocuments)
                .HasForeignKey(nd => nd.NewsId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(nd => nd.Document)
                .WithMany()
                .HasForeignKey(nd => nd.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(nd => new { nd.NewsId, nd.DocumentId })
                .IsUnique();
        });

        // ActionPlan
        builder.Entity<ActionPlanTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PageKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.PageKey).IsUnique();
        });

        builder.Entity<ActionPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Module).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Activity).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DetailedDescription).HasColumnType("nvarchar(max)");
            entity.Property(e => e.PlannedDelivery).HasMaxLength(100);
            entity.Property(e => e.Completed).HasMaxLength(100);

            entity.HasOne(e => e.ActionPlanTable)
                  .WithMany(t => t.Items)
                  .HasForeignKey(e => e.ActionPlanTableId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DevelopmentSuggestion
        builder.Entity<DevelopmentSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Organization).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Requirement).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ExpectedBenefit).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.AdditionalInfo).HasMaxLength(2000);
            entity.Property(e => e.ProcessedBy).HasMaxLength(100);
        });
    }
}
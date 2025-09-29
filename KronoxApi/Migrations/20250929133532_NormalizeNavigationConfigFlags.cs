using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeNavigationConfigFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Sätt IsSystemItem korrekt
            migrationBuilder.Sql(@"
UPDATE NavigationConfigs
SET IsSystemItem = CASE WHEN PageKey IN ('home','admin','logout') THEN 1 ELSE 0 END
");

            // 2) Sätt ItemType konsekvent
            migrationBuilder.Sql(@"
-- Systemlänkar
UPDATE NavigationConfigs SET ItemType = 'system' WHERE PageKey IN ('admin','logout');

-- Statiska sidor (inkl. home)
UPDATE NavigationConfigs SET ItemType = 'static'
WHERE PageKey IN ('home','omkonsortiet','omsystemet','visioner','kontaktaoss','dokument','forvaltning','medlemsnytt');

-- Rollspecifika sidor
UPDATE NavigationConfigs SET ItemType = 'role-specific'
WHERE PageKey IN ('forstyrelsen','forvnsg');
");

            // 3) Säkra startsidan
            migrationBuilder.Sql(@"
UPDATE NavigationConfigs
SET IsActive = 1,
    IsVisibleToGuests = 1,
    IsVisibleToMembers = 1,
    RequiredRoles = NULL,
    DisplayName = COALESCE(DisplayName, 'Startsida'),
    SortOrder = CASE WHEN SortOrder IS NULL OR SortOrder > 0 THEN 0 ELSE SortOrder END
WHERE PageKey = 'home';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Återställ ItemType != 'system' för allt utom admin/logout.
            migrationBuilder.Sql(@"
UPDATE NavigationConfigs SET IsSystemItem = CASE WHEN PageKey IN ('admin','logout') THEN 1 ELSE 0 END;
");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class FixHomeNavigationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Skapa eller normalisera home-posten
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM NavigationConfigs WHERE PageKey = 'home')
BEGIN
    INSERT INTO NavigationConfigs
    (PageKey, DisplayName, ItemType, SortOrder, IsVisibleToGuests, IsVisibleToMembers, IsActive, IsSystemItem, RequiredRoles, CreatedAt, LastModified)
    VALUES ('home', 'Startsida', 'static', 0, 1, 1, 1, 1, NULL, GETUTCDATE(), GETUTCDATE())
END
ELSE
BEGIN
    UPDATE NavigationConfigs
    SET IsActive = 1,
        IsVisibleToGuests = 1,
        IsVisibleToMembers = 1,
        IsSystemItem = 1,
        RequiredRoles = NULL,
        LastModified = GETUTCDATE()
    WHERE PageKey = 'home'
END
");

            // Constraint som tvingar 'home' att vara aktiv och publik
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NavigationConfigs_HomeAlwaysActive')
BEGIN
    ALTER TABLE NavigationConfigs WITH NOCHECK
    ADD CONSTRAINT CK_NavigationConfigs_HomeAlwaysActive
    CHECK (
        CASE WHEN PageKey = 'home'
             THEN CASE WHEN IsActive = 1 AND IsVisibleToGuests = 1 AND IsVisibleToMembers = 1 AND RequiredRoles IS NULL THEN 1 ELSE 0 END
             ELSE 1
        END = 1
    )
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NavigationConfigs_HomeAlwaysActive')
BEGIN
    ALTER TABLE NavigationConfigs DROP CONSTRAINT CK_NavigationConfigs_HomeAlwaysActive
END
");
        }
    }
}

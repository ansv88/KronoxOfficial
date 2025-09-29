using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleSpecificNavigationConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: sätt roller för role-specific sidor som saknar RequiredRoles
            migrationBuilder.Sql(@"
UPDATE NavigationConfigs
SET RequiredRoles = 'Admin,Styrelse',
    IsVisibleToGuests = 0,
    IsVisibleToMembers = 1
WHERE PageKey IN ('forstyrelsen','forvnsg')
  AND (RequiredRoles IS NULL OR LTRIM(RTRIM(RequiredRoles)) = '')
");

            // Dataskydd: kräv RequiredRoles när ItemType = 'role-specific'
            migrationBuilder.Sql(@"
ALTER TABLE NavigationConfigs WITH NOCHECK
ADD CONSTRAINT CK_NavigationConfigs_RoleSpecificRoles
CHECK (CASE WHEN ItemType = 'role-specific'
            THEN CASE WHEN RequiredRoles IS NOT NULL AND LTRIM(RTRIM(RequiredRoles)) <> '' THEN 1 ELSE 0 END
            ELSE 1 END = 1)
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NavigationConfigs_RoleSpecificRoles')
BEGIN
    ALTER TABLE NavigationConfigs DROP CONSTRAINT CK_NavigationConfigs_RoleSpecificRoles
END
");
        }
    }
}

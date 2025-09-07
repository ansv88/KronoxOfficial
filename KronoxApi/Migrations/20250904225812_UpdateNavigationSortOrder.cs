using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNavigationSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Uppdatera SortOrder för befintliga navigationskonfigurationer
            migrationBuilder.Sql(@"
                UPDATE NavigationConfigs 
                SET SortOrder = CASE PageKey
                    WHEN 'omkonsortiet' THEN 1
                    WHEN 'omsystemet' THEN 2  
                    WHEN 'visioner' THEN 3
                    WHEN 'medlemsnytt' THEN 5
                    WHEN 'forvaltning' THEN 10
                    WHEN 'dokument' THEN 15
                    WHEN 'for-styrelsen' THEN 16
                    WHEN 'kontakt' THEN 17
                    WHEN 'admin' THEN 19
                    WHEN 'logout' THEN 20
                    ELSE SortOrder
                END,
                LastModified = GETUTCDATE()
                WHERE PageKey IN ('omkonsortiet', 'omsystemet', 'visioner', 'medlemsnytt', 'forvaltning', 'dokument', 'for-styrelsen', 'kontakt', 'admin', 'logout');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Återställ till gamla värden om nödvändigt (baserat på ursprunglig seed)
            migrationBuilder.Sql(@"
                UPDATE NavigationConfigs 
                SET SortOrder = CASE PageKey
                    WHEN 'omkonsortiet' THEN 10
                    WHEN 'omsystemet' THEN 20  
                    WHEN 'visioner' THEN 30
                    WHEN 'medlemsnytt' THEN 1
                    WHEN 'forvaltning' THEN 70
                    WHEN 'dokument' THEN 80
                    WHEN 'for-styrelsen' THEN 50
                    WHEN 'kontakt' THEN 85
                    WHEN 'admin' THEN 90
                    WHEN 'logout' THEN 99
                    ELSE SortOrder
                END,
                LastModified = GETUTCDATE()
                WHERE PageKey IN ('omkonsortiet', 'omsystemet', 'visioner', 'medlemsnytt', 'forvaltning', 'dokument', 'for-styrelsen', 'kontakt', 'admin', 'logout');
            ");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class CleanupKontaktReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ta bort alla referenser till 'kontakt' från ContentBlocks
            migrationBuilder.Sql(@"
                DELETE FROM ContentBlocks 
                WHERE PageKey = 'kontakt' 
            ");

            // Ta bort från NavigationConfigs
            migrationBuilder.Sql(@"
                DELETE FROM NavigationConfigs 
                WHERE PageKey = 'kontakt'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

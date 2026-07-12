using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHideEntireSectionForMembersToFeatureSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideEntireSectionForMembers",
                table: "FeatureSections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideEntireSectionForMembers",
                table: "FeatureSections");
        }
    }
}

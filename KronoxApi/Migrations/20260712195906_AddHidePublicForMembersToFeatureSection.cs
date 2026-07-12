using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHidePublicForMembersToFeatureSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HidePublicForMembers",
                table: "FeatureSections",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HidePublicForMembers",
                table: "FeatureSections");
        }
    }
}

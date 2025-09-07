using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestAndMemberSortOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GuestSortOrder",
                table: "NavigationConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberSortOrder",
                table: "NavigationConfigs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestSortOrder",
                table: "NavigationConfigs");

            migrationBuilder.DropColumn(
                name: "MemberSortOrder",
                table: "NavigationConfigs");
        }
    }
}

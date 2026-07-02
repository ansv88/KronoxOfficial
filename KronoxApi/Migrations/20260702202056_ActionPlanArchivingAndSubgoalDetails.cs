using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class ActionPlanArchivingAndSubgoalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ActionPlanItems");

            migrationBuilder.AddColumn<bool>(
                name: "ShowArchivedPublicly",
                table: "ActionPlanTables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DetailedDescription",
                table: "ActionPlanSubgoals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ActionPlanItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ActionPlanItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanItems_IsArchived",
                table: "ActionPlanItems",
                column: "IsArchived");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionPlanItems_IsArchived",
                table: "ActionPlanItems");

            migrationBuilder.DropColumn(
                name: "ShowArchivedPublicly",
                table: "ActionPlanTables");

            migrationBuilder.DropColumn(
                name: "DetailedDescription",
                table: "ActionPlanSubgoals");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ActionPlanItems");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ActionPlanItems");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ActionPlanItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

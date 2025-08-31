using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddActionPlanAndDevelopmentSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionPlanTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlanTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DevelopmentSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExpectedBenefit = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanTableId = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PlannedDelivery = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Completed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionPlanItems_ActionPlanTables_ActionPlanTableId",
                        column: x => x.ActionPlanTableId,
                        principalTable: "ActionPlanTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanItems_ActionPlanTableId",
                table: "ActionPlanItems",
                column: "ActionPlanTableId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlanTables_PageKey",
                table: "ActionPlanTables",
                column: "PageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionPlanItems");

            migrationBuilder.DropTable(
                name: "DevelopmentSuggestions");

            migrationBuilder.DropTable(
                name: "ActionPlanTables");
        }
    }
}

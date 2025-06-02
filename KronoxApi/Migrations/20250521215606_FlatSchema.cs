using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class FlatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_MainCategories_MainCategoryId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentSubCategory");

            migrationBuilder.DropIndex(
                name: "IX_Documents_MainCategoryId",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "SubCategories",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubCategories",
                table: "Documents");

            migrationBuilder.CreateTable(
                name: "DocumentSubCategory",
                columns: table => new
                {
                    DocumentsId = table.Column<int>(type: "int", nullable: false),
                    SubCategoriesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSubCategory", x => new { x.DocumentsId, x.SubCategoriesId });
                    table.ForeignKey(
                        name: "FK_DocumentSubCategory_Documents_DocumentsId",
                        column: x => x.DocumentsId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentSubCategory_SubCategories_SubCategoriesId",
                        column: x => x.SubCategoriesId,
                        principalTable: "SubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_MainCategoryId",
                table: "Documents",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSubCategory_SubCategoriesId",
                table: "DocumentSubCategory",
                column: "SubCategoriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_MainCategories_MainCategoryId",
                table: "Documents",
                column: "MainCategoryId",
                principalTable: "MainCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

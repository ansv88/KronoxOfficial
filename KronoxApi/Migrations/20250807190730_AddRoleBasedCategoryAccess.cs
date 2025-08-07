using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KronoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleBasedCategoryAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MainCategories",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AllowedRoles",
                table: "MainCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MainCategories",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MainCategories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MainCategories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "Documents",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedBy",
                table: "Documents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MainCategories_IsActive",
                table: "MainCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_MainCategoryId_IsArchived",
                table: "Documents",
                columns: new[] { "MainCategoryId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedAt",
                table: "Documents",
                column: "UploadedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_MainCategories_MainCategoryId",
                table: "Documents",
                column: "MainCategoryId",
                principalTable: "MainCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_MainCategories_MainCategoryId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_MainCategories_IsActive",
                table: "MainCategories");

            migrationBuilder.DropIndex(
                name: "IX_Documents_MainCategoryId_IsArchived",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UploadedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AllowedRoles",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MainCategories");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "MainCategories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "Documents",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "FilePath",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class DropRemainingIdentityExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_CompanyId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_PermissionGroupId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_TenantId_IsDeleted",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreationTime",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeleterId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LastModifierId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PermissionGroupId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "ensa",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "ensa",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "IsStatic",
                schema: "ensa",
                table: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId",
                schema: "ensa",
                table: "User",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_TenantId",
                schema: "ensa",
                table: "User");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                schema: "ensa",
                table: "User",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeleterId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                schema: "ensa",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                schema: "ensa",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastModifierId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermissionGroupId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "ensa",
                table: "Role",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "ensa",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStatic",
                schema: "ensa",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_User_CompanyId",
                schema: "ensa",
                table: "User",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_User_PermissionGroupId",
                schema: "ensa",
                table: "User",
                column: "PermissionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_IsDeleted",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "IsDeleted" });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleProfileAndUserProfileCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "ensa",
                table: "UserProfile",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleProfile",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsStatic = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleProfile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_CompanyId",
                schema: "ensa",
                table: "UserProfile",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleProfile_RoleId",
                schema: "ensa",
                table: "RoleProfile",
                column: "RoleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleProfile",
                schema: "ensa");

            migrationBuilder.DropIndex(
                name: "IX_UserProfile_CompanyId",
                schema: "ensa",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "ensa",
                table: "UserProfile");
        }
    }
}

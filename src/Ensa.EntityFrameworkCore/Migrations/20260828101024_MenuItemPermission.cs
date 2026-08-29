using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class MenuItemPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PermissionId",
                schema: "ensa",
                table: "MenuItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItem_PermissionId",
                schema: "ensa",
                table: "MenuItem",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItem_PermissionId",
                schema: "ensa",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "PermissionId",
                schema: "ensa",
                table: "MenuItem");
        }
    }
}

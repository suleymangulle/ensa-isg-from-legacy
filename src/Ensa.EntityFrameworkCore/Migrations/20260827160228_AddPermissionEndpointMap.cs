using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionEndpointMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionEndpoint",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControllerName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionEndpoint", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionEndpoint_ControllerName_ActionName",
                schema: "ensa",
                table: "PermissionEndpoint",
                columns: new[] { "ControllerName", "ActionName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionEndpoint_PermissionId",
                schema: "ensa",
                table: "PermissionEndpoint",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermissionEndpoint",
                schema: "ensa");
        }
    }
}

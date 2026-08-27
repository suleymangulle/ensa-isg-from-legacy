using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RelaxCompanyCheckIndexAndAddVisitDateLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyCheck_CompanyId_CheckMonth",
                schema: "ensa",
                table: "CompanyCheck");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheck_CompanyId_CheckMonth",
                schema: "ensa",
                table: "CompanyCheck",
                columns: new[] { "CompanyId", "CheckMonth" },
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyCheck_CompanyId_CheckMonth",
                schema: "ensa",
                table: "CompanyCheck");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCheck_CompanyId_CheckMonth",
                schema: "ensa",
                table: "CompanyCheck",
                columns: new[] { "CompanyId", "CheckMonth" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}

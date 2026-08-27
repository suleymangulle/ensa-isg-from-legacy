using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RelaxMedicationDoseUnitCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationDoseUnit_Code",
                schema: "ensa",
                table: "MedicationDoseUnit");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDoseUnit_Code",
                schema: "ensa",
                table: "MedicationDoseUnit",
                column: "Code",
                filter: "[Code] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationDoseUnit_Code",
                schema: "ensa",
                table: "MedicationDoseUnit");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDoseUnit_Code",
                schema: "ensa",
                table: "MedicationDoseUnit",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");
        }
    }
}

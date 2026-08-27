using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RelaxTrainingProgressIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeTrainingProgress_CompanyEmployeeId_TrainingId_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_CompanyEmployeeId_TrainingId_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                columns: new[] { "CompanyEmployeeId", "TrainingId", "TrainingTopicId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeTrainingProgress_CompanyEmployeeId_TrainingId_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainingProgress_CompanyEmployeeId_TrainingId_TrainingTopicId",
                schema: "ensa",
                table: "EmployeeTrainingProgress",
                columns: new[] { "CompanyEmployeeId", "TrainingId", "TrainingTopicId" },
                unique: true,
                filter: "[TrainingTopicId] IS NOT NULL");
        }
    }
}

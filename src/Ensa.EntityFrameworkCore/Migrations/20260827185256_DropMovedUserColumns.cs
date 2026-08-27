using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class DropMovedUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_CityId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_DistrictId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_OfficeId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_PhotoDocumentId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_TenantId_NationalId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Address",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CityId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Color",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ContractApproved",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "GrossSalary",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Gsm",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "HireDate",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "MedulaPassword",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "MedulaUserName",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "MonthlyWorkDurationMinutes",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NationalId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OfficeAdmin",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "OrganizationAdmin",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PartTime",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PhotoDocumentId",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "StaffRole",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "SystemAdministrator",
                schema: "ensa",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                schema: "ensa",
                table: "User");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "ensa",
                table: "User",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                schema: "ensa",
                table: "User",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "ensa",
                table: "User",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ContractApproved",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossSalary",
                schema: "ensa",
                table: "User",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gsm",
                schema: "ensa",
                table: "User",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                schema: "ensa",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "ensa",
                table: "User",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedulaPassword",
                schema: "ensa",
                table: "User",
                type: "nvarchar(704)",
                maxLength: 704,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedulaUserName",
                schema: "ensa",
                table: "User",
                type: "nvarchar(704)",
                maxLength: 704,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyWorkDurationMinutes",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "ensa",
                table: "User",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                schema: "ensa",
                table: "User",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OfficeAdmin",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OfficeId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrganizationAdmin",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PartTime",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PhotoDocumentId",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffRole",
                schema: "ensa",
                table: "User",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SystemAdministrator",
                schema: "ensa",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                schema: "ensa",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_CityId",
                schema: "ensa",
                table: "User",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_User_DistrictId",
                schema: "ensa",
                table: "User",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_User_OfficeId",
                schema: "ensa",
                table: "User",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_User_PhotoDocumentId",
                schema: "ensa",
                table: "User",
                column: "PhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_NationalId",
                schema: "ensa",
                table: "User",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [NationalId] IS NOT NULL");
        }
    }
}

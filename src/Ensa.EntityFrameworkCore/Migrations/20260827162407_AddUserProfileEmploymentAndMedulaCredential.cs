using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileEmploymentAndMedulaCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmployment",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserTypeId = table.Column<int>(type: "int", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PartTime = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmployment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMedulaCredential",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MedulaUserName = table.Column<string>(type: "nvarchar(704)", maxLength: 704, nullable: true),
                    MedulaPassword = table.Column<string>(type: "nvarchar(704)", maxLength: 704, nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMedulaCredential", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                schema: "ensa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    DistrictId = table.Column<int>(type: "int", nullable: true),
                    PhotoDocumentId = table.Column<int>(type: "int", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    ContractApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<int>(type: "int", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeleterId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmployment_UserId",
                schema: "ensa",
                table: "UserEmployment",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEmployment_UserTypeId",
                schema: "ensa",
                table: "UserEmployment",
                column: "UserTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMedulaCredential_UserId",
                schema: "ensa",
                table: "UserMedulaCredential",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_CityId",
                schema: "ensa",
                table: "UserProfile",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_DistrictId",
                schema: "ensa",
                table: "UserProfile",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_PhotoDocumentId",
                schema: "ensa",
                table: "UserProfile",
                column: "PhotoDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_TenantId_NationalId",
                schema: "ensa",
                table: "UserProfile",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [NationalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_UserId",
                schema: "ensa",
                table: "UserProfile",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmployment",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserMedulaCredential",
                schema: "ensa");

            migrationBuilder.DropTable(
                name: "UserProfile",
                schema: "ensa");
        }
    }
}

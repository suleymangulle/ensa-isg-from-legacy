using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddYearEndReviewLineDateText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DateText",
                schema: "ensa",
                table: "YearEndReviewLine",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateText",
                schema: "ensa",
                table: "YearEndReviewLine");
        }
    }
}

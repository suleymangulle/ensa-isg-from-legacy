using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ensa.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RelaxInvoiceNumberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoice_TenantId_InvoiceNo",
                schema: "ensa",
                table: "Invoice");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TenantId_InvoiceNo",
                schema: "ensa",
                table: "Invoice",
                columns: new[] { "TenantId", "InvoiceNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoice_TenantId_InvoiceNo",
                schema: "ensa",
                table: "Invoice");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_TenantId_InvoiceNo",
                schema: "ensa",
                table: "Invoice",
                columns: new[] { "TenantId", "InvoiceNo" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}

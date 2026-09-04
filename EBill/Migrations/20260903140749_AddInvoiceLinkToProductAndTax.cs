using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBill.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLinkToProductAndTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "Taxes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_InvoiceId",
                table: "Taxes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_InvoiceId",
                table: "Products",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Invoices_InvoiceId",
                table: "Products",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Taxes_Invoices_InvoiceId",
                table: "Taxes",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Invoices_InvoiceId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Taxes_Invoices_InvoiceId",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_InvoiceId",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Products_InvoiceId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Taxes");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Products");
        }
    }
}

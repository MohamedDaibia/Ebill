using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBill.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerInvoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "LedgerDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerDetails_InvoiceId",
                table: "LedgerDetails",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerDetails_Invoices_InvoiceId",
                table: "LedgerDetails",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerDetails_Invoices_InvoiceId",
                table: "LedgerDetails");

            migrationBuilder.DropIndex(
                name: "IX_LedgerDetails_InvoiceId",
                table: "LedgerDetails");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "LedgerDetails");
        }
    }
}

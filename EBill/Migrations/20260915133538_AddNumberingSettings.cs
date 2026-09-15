using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBill.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NextInvoiceNumber",
                table: "Settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NextPurchaseOrderNumber",
                table: "Settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NextQuotationNumber",
                table: "Settings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextInvoiceNumber",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NextPurchaseOrderNumber",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "NextQuotationNumber",
                table: "Settings");
        }
    }
}

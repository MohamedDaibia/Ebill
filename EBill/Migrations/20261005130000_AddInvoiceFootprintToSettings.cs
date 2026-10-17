using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBill.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceFootprintToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceFootprint",
                table: "Settings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceFootprint",
                table: "Settings");
        }
    }
}

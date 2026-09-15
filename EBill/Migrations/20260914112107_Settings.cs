using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBill.Migrations
{
    /// <inheritdoc />
    public partial class Settings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StockCompanyId",
                table: "Companies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StockConnectedAt",
                table: "Companies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockCompanyId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StockConnectedAt",
                table: "Companies");
        }
    }
}

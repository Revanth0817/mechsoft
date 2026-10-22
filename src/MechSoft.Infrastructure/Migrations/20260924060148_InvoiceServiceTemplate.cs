using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechSoft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceServiceTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Shipping",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "InvoiceLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            // Existing lines only stored a line Amount, so derive each unit rate from it.
            // Lines with no quantity keep the whole amount as the rate.
            migrationBuilder.Sql(
                "UPDATE InvoiceLines SET Rate = CASE WHEN Qty > 0 THEN Amount / Qty ELSE Amount END");

            // Give existing invoices a due date matching the Net-30 default.
            migrationBuilder.Sql(
                "UPDATE Invoices SET DueDate = DATEADD(day, 30, InvoiceDate) WHERE DueDate IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Shipping",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "InvoiceLines");
        }
    }
}

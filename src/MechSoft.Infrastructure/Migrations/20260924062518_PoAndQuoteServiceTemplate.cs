using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechSoft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PoAndQuoteServiceTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Quotes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "QuoteLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "PurchaseOrderLines",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            // Existing lines only stored a line Amount, so derive each unit rate from it.
            migrationBuilder.Sql(
                "UPDATE PurchaseOrderLines SET Rate = CASE WHEN Qty > 0 THEN Amount / Qty ELSE Amount END");
            migrationBuilder.Sql(
                "UPDATE QuoteLines SET Rate = CASE WHEN Qty > 0 THEN Amount / Qty ELSE Amount END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "QuoteLines");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "PurchaseOrderLines");
        }
    }
}

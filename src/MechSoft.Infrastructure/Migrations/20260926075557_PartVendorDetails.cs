using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechSoft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartVendorDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VendorContactPerson",
                table: "Parts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorEmail",
                table: "Parts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Parts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorPhone",
                table: "Parts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VendorContactPerson",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "VendorEmail",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "VendorPhone",
                table: "Parts");
        }
    }
}

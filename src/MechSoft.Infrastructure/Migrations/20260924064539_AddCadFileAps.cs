using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechSoft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCadFileAps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApsStatus",
                table: "CadFiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApsUrn",
                table: "CadFiles",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApsStatus",
                table: "CadFiles");

            migrationBuilder.DropColumn(
                name: "ApsUrn",
                table: "CadFiles");
        }
    }
}

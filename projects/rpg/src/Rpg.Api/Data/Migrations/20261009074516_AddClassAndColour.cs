using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassAndColour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "sentinel");

            migrationBuilder.AddColumn<int>(
                name: "Colour",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Class",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Colour",
                table: "characters");
        }
    }
}

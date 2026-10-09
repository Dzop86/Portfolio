using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Build",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Hair",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Skin",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Build",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Hair",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Skin",
                table: "characters");
        }
    }
}

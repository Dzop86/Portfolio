using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Town",
                table: "characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "X",
                table: "characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Y",
                table: "characters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Town",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "X",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "characters");
        }
    }
}

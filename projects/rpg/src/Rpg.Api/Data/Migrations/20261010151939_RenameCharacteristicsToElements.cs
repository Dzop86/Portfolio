using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <summary>
    /// The characteristics take the names of their elements (D62): Strength becomes Earth, Intelligence
    /// Fire, Chance Water, Agility Air, the points kept. Written by hand: EF Core paired the columns by
    /// position and would have swapped Strength and Chance.
    /// </summary>
    public partial class RenameCharacteristicsToElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Strength",
                table: "characters",
                newName: "Earth");

            migrationBuilder.RenameColumn(
                name: "Intelligence",
                table: "characters",
                newName: "Fire");

            migrationBuilder.RenameColumn(
                name: "Chance",
                table: "characters",
                newName: "Water");

            migrationBuilder.RenameColumn(
                name: "Agility",
                table: "characters",
                newName: "Air");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Earth",
                table: "characters",
                newName: "Strength");

            migrationBuilder.RenameColumn(
                name: "Fire",
                table: "characters",
                newName: "Intelligence");

            migrationBuilder.RenameColumn(
                name: "Water",
                table: "characters",
                newName: "Chance");

            migrationBuilder.RenameColumn(
                name: "Air",
                table: "characters",
                newName: "Agility");
        }
    }
}

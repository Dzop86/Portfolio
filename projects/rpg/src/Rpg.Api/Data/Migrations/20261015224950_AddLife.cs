using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DungeonHp",
                table: "characters",
                newName: "Hp");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HpAt",
                table: "characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InFight",
                table: "characters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // The hit points kept in a dungeon become the character's life, set now (sprint 101).
            migrationBuilder.Sql("UPDATE characters SET \"HpAt\" = now() WHERE \"Hp\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HpAt",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "InFight",
                table: "characters");

            migrationBuilder.RenameColumn(
                name: "Hp",
                table: "characters",
                newName: "DungeonHp");
        }
    }
}

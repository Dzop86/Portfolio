using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildHall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Chest",
                table: "guilds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Crest",
                table: "guilds",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Goal",
                table: "guilds",
                type: "integer",
                nullable: false,
                // The guilds founded before sprint 156 get the default goal of the week.
                defaultValue: 120);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "guilds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Points",
                table: "guilds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Week",
                table: "guilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeekPoints",
                table: "guilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Credited",
                table: "guild_members",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractsOffered",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DaysPlayed",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastDayPlayed",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SecondsPlayed",
                table: "characters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Chest",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Crest",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Goal",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Week",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "WeekPoints",
                table: "guilds");

            migrationBuilder.DropColumn(
                name: "Credited",
                table: "guild_members");

            migrationBuilder.DropColumn(
                name: "ContractsOffered",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "DaysPlayed",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "LastDayPlayed",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "SecondsPlayed",
                table: "characters");
        }
    }
}

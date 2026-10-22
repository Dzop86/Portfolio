using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBattles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scenario = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Map = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Starter = table.Column<Guid>(type: "uuid", nullable: false),
                    Start = table.Column<string>(type: "text", nullable: false),
                    Engine = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "battle_actions",
                columns: table => new
                {
                    BattleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_battle_actions", x => new { x.BattleId, x.Number });
                    table.ForeignKey(
                        name: "FK_battle_actions_battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "battle_actions");

            migrationBuilder.DropTable(
                name: "battles");
        }
    }
}

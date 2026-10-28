using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildRecruitment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Contributed",
                table: "guild_members",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "guild_members",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "guild_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guild_applications_characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guild_applications_guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Who = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    By = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Value = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guild_events_guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guild_applications_CharacterId_GuildId",
                table: "guild_applications",
                columns: new[] { "CharacterId", "GuildId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guild_applications_GuildId",
                table: "guild_applications",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_guild_events_GuildId_Id",
                table: "guild_events",
                columns: new[] { "GuildId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_applications");

            migrationBuilder.DropTable(
                name: "guild_events");

            migrationBuilder.DropColumn(
                name: "Contributed",
                table: "guild_members");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "guild_members");
        }
    }
}

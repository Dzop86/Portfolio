using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Seen",
                table: "accounts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Seen",
                table: "accounts");
        }
    }
}

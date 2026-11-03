using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fork",
                table: "characters",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fork",
                table: "characters");
        }
    }
}

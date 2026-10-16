using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveToTwelveMapVillage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clairval's square went from 16 x 12 cells to 48 x 32 (sprint 102, D74): who stood on the old one
            // goes to the new one's arrival, the old cells meaning nothing there.
            migrationBuilder.Sql("UPDATE characters SET \"X\" = 24, \"Y\" = 16 WHERE \"Town\" = 'clairval';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old square's arrival.
            migrationBuilder.Sql("UPDATE characters SET \"X\" = 7, \"Y\" = 11 WHERE \"Town\" = 'clairval';");
        }
    }
}

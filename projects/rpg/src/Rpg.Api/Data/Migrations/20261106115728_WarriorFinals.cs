using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WarriorFinals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Warrior's lineages end in two final classes (sprint 197, D96, docs/classes-guerrier.md §6): a hero of 100 on a lineage takes the one taken back from the
            // evolution at 100 it had, and fights as before; the new one stays open, at the price of a change.
            foreach ((string fork, string final) in Taken)
                migrationBuilder.Sql($"""UPDATE characters SET "Final" = '{final}' WHERE "Class" = 'warrior' AND "Level" >= 100 AND "Fork" = '{fork}' AND "Final" IS NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to evolutions at 100 that come alone: no Warrior has a final class.
            migrationBuilder.Sql("""UPDATE characters SET "Final" = NULL WHERE "Class" = 'warrior';""");
        }

        /// <summary>Each lineage of the Warrior, and its final class taken back from its old evolution at 100.</summary>
        public static readonly (string Fork, string Final)[] Taken =
            [("rampart", "crusader"), ("bastion", "duelist"), ("jailer", "blade-master"), ("pincer", "strategist"), ("ram", "executioner"), ("harpooner", "arena-champion")];
    }
}

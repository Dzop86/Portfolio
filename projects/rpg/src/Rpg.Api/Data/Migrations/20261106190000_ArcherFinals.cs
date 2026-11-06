using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArcherFinals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Archer's tree of four ranks (sprint 198, D96, docs/classes-archer.md §4): a hero of 100 on a lineage that stays takes the final class taken back from the
            // evolution at 100 it had, and fights as before.
            foreach ((string fork, string final) in Taken)
                migrationBuilder.Sql($"""UPDATE characters SET "Final" = '{final}' WHERE "Class" = 'archer' AND "Level" >= 100 AND "Fork" = '{fork}' AND "Final" IS NULL;""");
            // A lineage that leaves its branch gives way to its sister in the branch kept (Q1 a): the Portcullis goes to the Artillerist, the Hedgehopper is no more. Their final
            // class is still to be chosen, free, as a first choice.
            foreach ((string spec, string from, string to) in Moved)
                migrationBuilder.Sql($"""UPDATE characters SET "Fork" = '{to}', "Final" = NULL WHERE "Class" = 'archer' AND "Spec" = '{spec}' AND "Fork" = '{from}';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to evolutions at 100 that come alone: no Archer has a final class; the Bounty Hunters are Portcullis again. Who was a Hedgehopper is not known any more:
            // they stay Skirmishers.
            migrationBuilder.Sql("""UPDATE characters SET "Final" = NULL WHERE "Class" = 'archer';""");
            migrationBuilder.Sql("""UPDATE characters SET "Fork" = 'barrage' WHERE "Class" = 'archer' AND "Spec" = 'sightline' AND "Fork" = 'bounty-hunter';""");
        }

        /// <summary>Each lineage of the Archer that stays, and its final class taken back from its old evolution at 100.</summary>
        public static readonly (string Fork, string Final)[] Taken =
            [("marksman", "dragon-eye"), ("trapper", "forest-master"), ("falconer", "beast-lord"), ("runner", "vaulter")];

        /// <summary>The lineages that leave their branch, and the sister that takes their place.</summary>
        public static readonly (string Spec, string From, string To)[] Moved = [("sightline", "barrage", "bounty-hunter"), ("trail", "leaper", "runner")];
    }
}

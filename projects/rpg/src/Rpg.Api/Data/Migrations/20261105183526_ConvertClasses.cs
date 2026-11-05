using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rpg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Class",
                table: "characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "archer",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "sentinel");

            // The Guard becomes the Warrior, the Sentinel the Archer (sprint 195, D96): ranks, deck, talents, path and level stay as they are.
            migrationBuilder.Sql("""UPDATE characters SET "Class" = 'warrior' WHERE "Class" = 'guard';""");
            migrationBuilder.Sql("""UPDATE characters SET "Class" = 'archer' WHERE "Class" = 'sentinel';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Class",
                table: "characters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "sentinel",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "archer");

            migrationBuilder.Sql("""UPDATE characters SET "Class" = 'guard' WHERE "Class" = 'warrior';""");
            migrationBuilder.Sql("""UPDATE characters SET "Class" = 'sentinel' WHERE "Class" = 'archer';""");
        }
    }
}

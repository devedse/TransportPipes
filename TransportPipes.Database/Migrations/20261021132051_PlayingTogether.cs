using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class PlayingTogether : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Markets",
                table: "World");

            migrationBuilder.DropColumn(
                name: "GatePolicies",
                table: "Estates");

            migrationBuilder.DropColumn(
                name: "Side",
                table: "Conductors");

            migrationBuilder.AddColumn<int>(
                name: "Rules",
                table: "World",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rules",
                table: "World");

            migrationBuilder.AddColumn<string>(
                name: "Markets",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "GatePolicies",
                table: "Estates",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Side",
                table: "Conductors",
                type: "integer",
                nullable: true);
        }
    }
}

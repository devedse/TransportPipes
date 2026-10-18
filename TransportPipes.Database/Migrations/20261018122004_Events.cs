using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Events",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "Damaged",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Events",
                table: "World");

            migrationBuilder.DropColumn(
                name: "Damaged",
                table: "Buildings");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class Outposts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Outposts",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Outposts",
                table: "World");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class Widen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Widen",
                table: "Conductors",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Widen",
                table: "Conductors");
        }
    }
}

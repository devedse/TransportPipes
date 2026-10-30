using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class OwnEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Corporations");

            migrationBuilder.DropColumn(
                name: "Era",
                table: "World");

            migrationBuilder.DropColumn(
                name: "Projects",
                table: "World");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "World");

            migrationBuilder.DropColumn(
                name: "Trunks",
                table: "World");

            migrationBuilder.DropColumn(
                name: "ReservedFor",
                table: "Estates");

            migrationBuilder.DropColumn(
                name: "ReservedUntil",
                table: "Estates");

            migrationBuilder.DropColumn(
                name: "Widen",
                table: "Conductors");

            migrationBuilder.DropColumn(
                name: "ResearchPoints",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Xp",
                table: "Companies");

            migrationBuilder.AddColumn<int>(
                name: "OutskirtsParcels",
                table: "Estates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Price",
                table: "Buildings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutskirtsParcels",
                table: "Estates");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Buildings");

            migrationBuilder.AddColumn<int>(
                name: "Era",
                table: "World",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Projects",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Trunks",
                table: "World",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<long>(
                name: "ReservedFor",
                table: "Estates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUntil",
                table: "Estates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Widen",
                table: "Conductors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ResearchPoints",
                table: "Companies",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Xp",
                table: "Companies",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Buyer = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Days = table.Column<double>(type: "double precision", nullable: false),
                    Delivered = table.Column<double>(type: "double precision", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Expected = table.Column<double>(type: "double precision", nullable: false),
                    PenaltyRate = table.Column<double>(type: "double precision", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    Proposer = table.Column<long>(type: "bigint", nullable: false),
                    Rate = table.Column<double>(type: "double precision", nullable: false),
                    Resource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Seller = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Corporations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    DistrictX = table.Column<int>(type: "integer", nullable: false),
                    DistrictY = table.Column<int>(type: "integer", nullable: false),
                    HqLevel = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PerkResource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Tag = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Tax = table.Column<double>(type: "double precision", nullable: false),
                    Treasury = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Corporations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_State",
                table: "Contracts",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_Corporations_Tag",
                table: "Corporations",
                column: "Tag",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class Statistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Moments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Company = table.Column<long>(type: "bigint", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Big = table.Column<bool>(type: "boolean", nullable: false),
                    Ref = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatSamples",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<long>(type: "bigint", nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Values = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatSamples", x => new { x.Kind, x.Subject, x.At });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Moments_Company_At",
                table: "Moments",
                columns: new[] { "Company", "At" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Moments");

            migrationBuilder.DropTable(
                name: "StatSamples");
        }
    }
}

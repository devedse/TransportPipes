using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalConnections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false),
                    LastCalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Gold = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransportAction = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    ExecutedById = table.Column<long>(type: "bigint", nullable: false),
                    ActionDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Executed = table.Column<bool>(type: "boolean", nullable: false),
                    DateToBeExecuted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GoldClaimed = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_Users_ExecutedById",
                        column: x => x.ExecutedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientUpdates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExecutedById = table.Column<long>(type: "bigint", nullable: true),
                    ClientUpdateDetails = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientUpdates_Users_ExecutedById",
                        column: x => x.ExecutedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameObjects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    GameObjectType = table.Column<int>(type: "integer", nullable: false),
                    ChunkX = table.Column<int>(type: "integer", nullable: false),
                    ChunkY = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false),
                    DbGlobalConnectionId = table.Column<long>(type: "bigint", nullable: true),
                    DbOwnerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameObjects_GlobalConnections_DbGlobalConnectionId",
                        column: x => x.DbGlobalConnectionId,
                        principalTable: "GlobalConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameObjects_Users_DbOwnerId",
                        column: x => x.DbOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsumerConnections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionId = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false),
                    DestinationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerConnections_GameObjects_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "GameObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderConnections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConnectionId = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderConnections_GameObjects_SourceId",
                        column: x => x.SourceId,
                        principalTable: "GameObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_DateToBeExecuted",
                table: "Actions",
                column: "DateToBeExecuted");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_Executed",
                table: "Actions",
                column: "Executed");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ExecutedById",
                table: "Actions",
                column: "ExecutedById");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_X_Y",
                table: "Actions",
                columns: new[] { "X", "Y" },
                unique: true,
                filter: "NOT \"Executed\"");

            migrationBuilder.CreateIndex(
                name: "IX_ClientUpdates_ExecutedById",
                table: "ClientUpdates",
                column: "ExecutedById");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerConnections_ConnectionId",
                table: "ConsumerConnections",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerConnections_DestinationId",
                table: "ConsumerConnections",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumerConnections_GlobalConnectionId",
                table: "ConsumerConnections",
                column: "GlobalConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameObjects_ChunkX_ChunkY",
                table: "GameObjects",
                columns: new[] { "ChunkX", "ChunkY" });

            migrationBuilder.CreateIndex(
                name: "IX_GameObjects_DbGlobalConnectionId",
                table: "GameObjects",
                column: "DbGlobalConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameObjects_DbOwnerId",
                table: "GameObjects",
                column: "DbOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameObjects_GlobalConnectionId",
                table: "GameObjects",
                column: "GlobalConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameObjects_X_Y",
                table: "GameObjects",
                columns: new[] { "X", "Y" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalConnections_GlobalConnectionId",
                table: "GlobalConnections",
                column: "GlobalConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConnections_ConnectionId",
                table: "ProviderConnections",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConnections_GlobalConnectionId",
                table: "ProviderConnections",
                column: "GlobalConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConnections_SourceId",
                table: "ProviderConnections",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUserName",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "ClientUpdates");

            migrationBuilder.DropTable(
                name: "ConsumerConnections");

            migrationBuilder.DropTable(
                name: "ProviderConnections");

            migrationBuilder.DropTable(
                name: "GameObjects");

            migrationBuilder.DropTable(
                name: "GlobalConnections");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}

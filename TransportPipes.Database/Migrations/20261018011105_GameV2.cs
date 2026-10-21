using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransportPipes.Database.Migrations
{
    /// <inheritdoc />
    public partial class GameV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "Users");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ConstructionEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Contents = table.Column<string>(type: "jsonb", nullable: false),
                    Imports = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Gold = table.Column<double>(type: "double precision", nullable: false),
                    Xp = table.Column<double>(type: "double precision", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ResearchPoints = table.Column<double>(type: "double precision", nullable: false),
                    LifetimeEarnings = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsNpc = table.Column<bool>(type: "boolean", nullable: false),
                    DistrictX = table.Column<int>(type: "integer", nullable: false),
                    DistrictY = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Settings = table.Column<string>(type: "jsonb", nullable: false),
                    Report = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conductors",
                columns: table => new
                {
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conductors", x => new { x.X, x.Y });
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estates",
                columns: table => new
                {
                    DistrictX = table.Column<int>(type: "integer", nullable: false),
                    DistrictY = table.Column<int>(type: "integer", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    UnlockedParcels = table.Column<int>(type: "integer", nullable: false),
                    GatePolicies = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estates", x => new { x.DistrictX, x.DistrictY, x.Slot });
                });

            migrationBuilder.CreateTable(
                name: "TerrainOverrides",
                columns: table => new
                {
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false),
                    Terrain = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerrainOverrides", x => new { x.X, x.Y });
                });

            migrationBuilder.CreateTable(
                name: "World",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    Era = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTick = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_World", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_OwnerId",
                table: "Buildings",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UserId",
                table: "Companies",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Conductors");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "Estates");

            migrationBuilder.DropTable(
                name: "TerrainOverrides");

            migrationBuilder.DropTable(
                name: "World");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.AddColumn<double>(
                name: "Gold",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExecutedById = table.Column<long>(type: "bigint", nullable: false),
                    ActionDetails = table.Column<string>(type: "jsonb", nullable: true),
                    DateToBeExecuted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Executed = table.Column<bool>(type: "boolean", nullable: false),
                    GoldClaimed = table.Column<double>(type: "double precision", nullable: false),
                    TransportAction = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false)
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
                name: "GameObjects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DbGlobalConnectionId = table.Column<long>(type: "bigint", nullable: true),
                    DbOwnerId = table.Column<long>(type: "bigint", nullable: false),
                    ChunkX = table.Column<int>(type: "integer", nullable: false),
                    ChunkY = table.Column<int>(type: "integer", nullable: false),
                    GameObjectType = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<int>(type: "integer", nullable: false),
                    Y = table.Column<int>(type: "integer", nullable: false)
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
                    DestinationId = table.Column<long>(type: "bigint", nullable: false),
                    ConnectionId = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false)
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
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    ConnectionId = table.Column<int>(type: "integer", nullable: false),
                    GlobalConnectionId = table.Column<int>(type: "integer", nullable: false)
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
        }
    }
}

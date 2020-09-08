using System;
using System.Collections.Generic;
using System.Text.Json;
using Hive.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

namespace Hive.Migrations
{
    public partial class CleanReset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    AdditionalData = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AdditionalData = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    CreationTime = table.Column<Instant>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.Guid);
                });

            migrationBuilder.CreateTable(
                name: "Mods",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    ID = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<Instant>(type: "timestamp", nullable: false),
                    EditedAt = table.Column<Instant>(type: "timestamp", nullable: true),
                    Uploader = table.Column<string>(type: "text", nullable: false),
                    Authors = table.Column<string[]>(type: "text[]", nullable: false),
                    Contributors = table.Column<string[]>(type: "text[]", nullable: false),
                    Dependencies = table.Column<IList<ModReference>>(type: "jsonb", nullable: false),
                    Conflicts = table.Column<IList<ModReference>>(type: "jsonb", nullable: false),
                    ChannelName = table.Column<string>(type: "text", nullable: false),
                    AdditionalData = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    Links = table.Column<IList<ValueTuple<string, Uri>>>(type: "jsonb", nullable: false),
                    DownloadLink = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mods", x => x.Guid);
                    table.UniqueConstraint("AK_Mods_ID", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Mods_Channels_ChannelName",
                        column: x => x.ChannelName,
                        principalTable: "Channels",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameVersionMod",
                columns: table => new
                {
                    GameVersion_Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Mod_ID = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersionMod", x => new { x.GameVersion_Guid, x.Mod_ID });
                    table.ForeignKey(
                        name: "FK_GameVersionMod_GameVersions_GameVersion_Guid",
                        column: x => x.GameVersion_Guid,
                        principalTable: "GameVersions",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameVersionMod_Mods_Mod_ID",
                        column: x => x.Mod_ID,
                        principalTable: "Mods",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModLocalizations",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Changelog = table.Column<string>(type: "text", nullable: true),
                    Credits = table.Column<string>(type: "text", nullable: true),
                    OwningModGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModLocalizations", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_ModLocalizations_Mods_OwningModGuid",
                        column: x => x.OwningModGuid,
                        principalTable: "Mods",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameVersionMod_Mod_ID",
                table: "GameVersionMod",
                column: "Mod_ID");

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_Name",
                table: "GameVersions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModLocalizations_OwningModGuid",
                table: "ModLocalizations",
                column: "OwningModGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Mods_ChannelName",
                table: "Mods",
                column: "ChannelName");

            migrationBuilder.CreateIndex(
                name: "IX_Mods_ID_Version",
                table: "Mods",
                columns: new[] { "ID", "Version" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVersionMod");

            migrationBuilder.DropTable(
                name: "ModLocalizations");

            migrationBuilder.DropTable(
                name: "GameVersions");

            migrationBuilder.DropTable(
                name: "Mods");

            migrationBuilder.DropTable(
                name: "Channels");
        }
    }
}

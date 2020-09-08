using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hive.Migrations
{
    public partial class ManyToManyModGameVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVersion_Mod_Joiner");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Mods_ID",
                table: "Mods",
                column: "ID");

            migrationBuilder.CreateTable(
                name: "GameVersionMod",
                columns: table => new
                {
                    GameVersion_Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Mod_ID = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
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

            migrationBuilder.CreateIndex(
                name: "IX_GameVersionMod_GameVersion_Guid",
                table: "GameVersionMod",
                column: "GameVersion_Guid");

            migrationBuilder.CreateIndex(
                name: "IX_GameVersionMod_Mod_ID",
                table: "GameVersionMod",
                column: "Mod_ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVersionMod");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Mods_ID",
                table: "Mods");

            migrationBuilder.CreateTable(
                name: "GameVersion_Mod_Joiner",
                columns: table => new
                {
                    ModGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_GameVersion_Mod_Joiner_GameVersions_VersionGuid",
                        column: x => x.VersionGuid,
                        principalTable: "GameVersions",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameVersion_Mod_Joiner_Mods_ModGuid",
                        column: x => x.ModGuid,
                        principalTable: "Mods",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameVersion_Mod_Joiner_ModGuid",
                table: "GameVersion_Mod_Joiner",
                column: "ModGuid");

            migrationBuilder.CreateIndex(
                name: "IX_GameVersion_Mod_Joiner_VersionGuid",
                table: "GameVersion_Mod_Joiner",
                column: "VersionGuid");
        }
    }
}

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hive.Migrations
{
    public partial class UsersChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameVersionMod_GameVersions_GameVersion_Guid",
                table: "GameVersionMod");

            migrationBuilder.DropForeignKey(
                name: "FK_GameVersionMod_Mods_Mod_Id",
                table: "GameVersionMod");

            migrationBuilder.DropForeignKey(
                name: "FK_Mods_Channels_ChannelName",
                table: "Mods");

            migrationBuilder.RenameColumn(
                name: "Mod_Id",
                table: "GameVersionMod",
                newName: "SupportedVersionsGuid");

            migrationBuilder.RenameColumn(
                name: "GameVersion_Guid",
                table: "GameVersionMod",
                newName: "SupportedModsId");

            migrationBuilder.RenameIndex(
                name: "IX_GameVersionMod_Mod_Id",
                table: "GameVersionMod",
                newName: "IX_GameVersionMod_SupportedVersionsGuid");

            migrationBuilder.AlterColumn<string>(
                name: "ChannelName",
                table: "Mods",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Username = table.Column<string>(type: "text", nullable: false),
                    AlternativeId = table.Column<string>(type: "text", nullable: false),
                    AdditionalData = table.Column<Dictionary<string, JsonElement>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Username);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersionMod_GameVersions_SupportedVersionsGuid",
                table: "GameVersionMod",
                column: "SupportedVersionsGuid",
                principalTable: "GameVersions",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersionMod_Mods_SupportedModsId",
                table: "GameVersionMod",
                column: "SupportedModsId",
                principalTable: "Mods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mods_Channels_ChannelName",
                table: "Mods",
                column: "ChannelName",
                principalTable: "Channels",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameVersionMod_GameVersions_SupportedVersionsGuid",
                table: "GameVersionMod");

            migrationBuilder.DropForeignKey(
                name: "FK_GameVersionMod_Mods_SupportedModsId",
                table: "GameVersionMod");

            migrationBuilder.DropForeignKey(
                name: "FK_Mods_Channels_ChannelName",
                table: "Mods");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.RenameColumn(
                name: "SupportedVersionsGuid",
                table: "GameVersionMod",
                newName: "Mod_Id");

            migrationBuilder.RenameColumn(
                name: "SupportedModsId",
                table: "GameVersionMod",
                newName: "GameVersion_Guid");

            migrationBuilder.RenameIndex(
                name: "IX_GameVersionMod_SupportedVersionsGuid",
                table: "GameVersionMod",
                newName: "IX_GameVersionMod_Mod_Id");

            migrationBuilder.AlterColumn<string>(
                name: "ChannelName",
                table: "Mods",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersionMod_GameVersions_GameVersion_Guid",
                table: "GameVersionMod",
                column: "GameVersion_Guid",
                principalTable: "GameVersions",
                principalColumn: "Guid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameVersionMod_Mods_Mod_Id",
                table: "GameVersionMod",
                column: "Mod_Id",
                principalTable: "Mods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mods_Channels_ChannelName",
                table: "Mods",
                column: "ChannelName",
                principalTable: "Channels",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

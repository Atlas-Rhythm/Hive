using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Hive.Migrations
{
    public partial class UpdateNpgsql_UseUsersRef : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authors",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "Contributors",
                table: "Mods");

            migrationBuilder.RenameColumn(
                name: "Uploader",
                table: "Mods",
                newName: "UploaderAlternativeId");

            migrationBuilder.AlterColumn<Instant>(
                name: "UploadedAt",
                table: "Mods",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "EditedAt",
                table: "Mods",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "GameVersions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp without time zone");

            migrationBuilder.CreateTable(
                name: "ModUser",
                columns: table => new
                {
                    AuthoredId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorsAlternativeId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModUser", x => new { x.AuthoredId, x.AuthorsAlternativeId });
                    table.ForeignKey(
                        name: "FK_ModUser_Mods_AuthoredId",
                        column: x => x.AuthoredId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModUser_Users_AuthorsAlternativeId",
                        column: x => x.AuthorsAlternativeId,
                        principalTable: "Users",
                        principalColumn: "AlternativeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModUser1",
                columns: table => new
                {
                    ContributedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributorsAlternativeId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModUser1", x => new { x.ContributedToId, x.ContributorsAlternativeId });
                    table.ForeignKey(
                        name: "FK_ModUser1_Mods_ContributedToId",
                        column: x => x.ContributedToId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModUser1_Users_ContributorsAlternativeId",
                        column: x => x.ContributorsAlternativeId,
                        principalTable: "Users",
                        principalColumn: "AlternativeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AlternativeId",
                table: "Users",
                column: "AlternativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Mods_UploaderAlternativeId",
                table: "Mods",
                column: "UploaderAlternativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ModUser_AuthorsAlternativeId",
                table: "ModUser",
                column: "AuthorsAlternativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ModUser1_ContributorsAlternativeId",
                table: "ModUser1",
                column: "ContributorsAlternativeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mods_Users_UploaderAlternativeId",
                table: "Mods",
                column: "UploaderAlternativeId",
                principalTable: "Users",
                principalColumn: "AlternativeId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mods_Users_UploaderAlternativeId",
                table: "Mods");

            migrationBuilder.DropTable(
                name: "ModUser");

            migrationBuilder.DropTable(
                name: "ModUser1");

            migrationBuilder.DropIndex(
                name: "IX_Users_AlternativeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Mods_UploaderAlternativeId",
                table: "Mods");

            migrationBuilder.RenameColumn(
                name: "UploaderAlternativeId",
                table: "Mods",
                newName: "Uploader");

            migrationBuilder.AlterColumn<Instant>(
                name: "UploadedAt",
                table: "Mods",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Instant>(
                name: "EditedAt",
                table: "Mods",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Authors",
                table: "Mods",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "Contributors",
                table: "Mods",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AlterColumn<Instant>(
                name: "CreationTime",
                table: "GameVersions",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(Instant),
                oldType: "timestamp with time zone");
        }
    }
}

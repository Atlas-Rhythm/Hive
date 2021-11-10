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
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Authors",
                table: "Mods");

            migrationBuilder.DropColumn(
                name: "Contributors",
                table: "Mods");

            migrationBuilder.RenameColumn(
                name: "Uploader",
                table: "Mods",
                newName: "UploaderUsername");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Username");

            migrationBuilder.CreateTable(
                name: "ModUser",
                columns: table => new
                {
                    AuthoredId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorsUsername = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModUser", x => new { x.AuthoredId, x.AuthorsUsername });
                    table.ForeignKey(
                        name: "FK_ModUser_Mods_AuthoredId",
                        column: x => x.AuthoredId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModUser_Users_AuthorsUsername",
                        column: x => x.AuthorsUsername,
                        principalTable: "Users",
                        principalColumn: "Username",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModUser1",
                columns: table => new
                {
                    ContributedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContributorsUsername = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModUser1", x => new { x.ContributedToId, x.ContributorsUsername });
                    table.ForeignKey(
                        name: "FK_ModUser1_Mods_ContributedToId",
                        column: x => x.ContributedToId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModUser1_Users_ContributorsUsername",
                        column: x => x.ContributorsUsername,
                        principalTable: "Users",
                        principalColumn: "Username",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AlternativeId",
                table: "Users",
                column: "AlternativeId");

            migrationBuilder.CreateIndex(
                name: "IX_Mods_UploaderUsername",
                table: "Mods",
                column: "UploaderUsername");

            migrationBuilder.CreateIndex(
                name: "IX_ModUser_AuthorsUsername",
                table: "ModUser",
                column: "AuthorsUsername");

            migrationBuilder.CreateIndex(
                name: "IX_ModUser1_ContributorsUsername",
                table: "ModUser1",
                column: "ContributorsUsername");

            migrationBuilder.AddForeignKey(
                name: "FK_Mods_Users_UploaderUsername",
                table: "Mods",
                column: "UploaderUsername",
                principalTable: "Users",
                principalColumn: "Username",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mods_Users_UploaderUsername",
                table: "Mods");

            migrationBuilder.DropTable(
                name: "ModUser");

            migrationBuilder.DropTable(
                name: "ModUser1");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AlternativeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Mods_UploaderUsername",
                table: "Mods");

            migrationBuilder.RenameColumn(
                name: "UploaderUsername",
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

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "AlternativeId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

namespace Hive.Migrations
{
    public partial class AddGaveVersionCreationTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "CreationTime",
                table: "GameVersions",
                type: "timestamp",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "GameVersions");
        }
    }
}

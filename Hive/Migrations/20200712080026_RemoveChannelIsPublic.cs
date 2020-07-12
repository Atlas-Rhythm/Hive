using Microsoft.EntityFrameworkCore.Migrations;

namespace Hive.Migrations
{
    public partial class RemoveChannelIsPublic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Channels");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}

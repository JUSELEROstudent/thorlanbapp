using GotsThorlabs.Database.EntityRepo;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GotsThorlabs.Migrations
{
    public partial class AddCameraDriverType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "driverType",
                table: "camera",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "generic");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "driverType",
                table: "camera");
        }
    }
}

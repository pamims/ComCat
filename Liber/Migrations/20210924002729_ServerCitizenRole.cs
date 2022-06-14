using Microsoft.EntityFrameworkCore.Migrations;

namespace ComCat.Migrations
{
    public partial class ServerCitizenRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "ActiveUsers",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<ulong>(
                name: "CitizenRole",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveUsers",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "CitizenRole",
                table: "Servers");
        }
    }
}

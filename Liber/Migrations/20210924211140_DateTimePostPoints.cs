using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComCat.Migrations
{
    public partial class DateTimePostPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "Cooldown",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "PointsMultiplier",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "ServerMembers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<uint>(
                name: "Points",
                table: "ServerMembers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cooldown",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "PointsMultiplier",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "ServerMembers");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "ServerMembers");
        }
    }
}


using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_.Api.Migrations
{
    public partial class AddEventDistanceKm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Events",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 5.00m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_DistanceKm",
                table: "Events",
                sql: "[DistanceKm] > 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_DistanceKm",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Events");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTypesAndDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryType",
                table: "Categories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Age");

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Categories",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_CategoryType",
                table: "Categories",
                sql: "[CategoryType] IN ('Age', 'Distance')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_TypeDistance",
                table: "Categories",
                sql: "([CategoryType] = 'Age' AND [DistanceKm] IS NULL) OR ([CategoryType] = 'Distance' AND [DistanceKm] IS NOT NULL AND [DistanceKm] > 0 AND [MinimumAge] IS NULL AND [MaximumAge] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_CategoryType",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_TypeDistance",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CategoryType",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Categories");
        }
    }
}

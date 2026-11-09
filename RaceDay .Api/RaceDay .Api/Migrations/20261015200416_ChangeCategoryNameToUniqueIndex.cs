using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCategoryNameToUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "UQ_Categories_Event_CategoryName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_Event_CategoryName",
                table: "Categories",
                columns: new[] { "EventId", "CategoryName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Categories_Event_CategoryName",
                table: "Categories");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_Categories_Event_CategoryName",
                table: "Categories",
                columns: new[] { "EventId", "CategoryName" });
        }
    }
}

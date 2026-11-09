using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "UQ_Users_Email",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_Email",
                table: "Users");

            migrationBuilder.AddUniqueConstraint(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email");
        }
    }
}

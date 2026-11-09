using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay_.Api.Migrations;

public partial class AddAzureBlobStorageImageReferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BannerBlobName",
            table: "Events",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureBlobName",
            table: "Users",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BannerBlobName", table: "Events");
        migrationBuilder.DropColumn(name: "ProfilePictureBlobName", table: "Users");
    }
}

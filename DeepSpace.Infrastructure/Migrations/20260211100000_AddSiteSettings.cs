using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepSpace.Infrastructure.Migrations;

public partial class AddSiteSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiteSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SiteTitle = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                SeoDescription = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                SeoKeywords = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                FaviconPath = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                HomePageH1 = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                HomePageSubtitle = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteSettings", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SiteSettings");
    }
}

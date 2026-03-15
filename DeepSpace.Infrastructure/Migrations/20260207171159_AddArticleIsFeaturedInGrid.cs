using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleIsFeaturedInGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeaturedInGrid",
                table: "Articles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeaturedInGrid",
                table: "Articles");
        }
    }
}

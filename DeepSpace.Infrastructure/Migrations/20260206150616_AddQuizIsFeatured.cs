using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeepSpace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizIsFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Quizzes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Quizzes");
        }
    }
}

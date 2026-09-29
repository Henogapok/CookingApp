using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipeDraftDishChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DishChoicesJson",
                table: "RecipeDrafts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedDishesJson",
                table: "RecipeDrafts",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DishChoicesJson",
                table: "RecipeDrafts");

            migrationBuilder.DropColumn(
                name: "SelectedDishesJson",
                table: "RecipeDrafts");
        }
    }
}

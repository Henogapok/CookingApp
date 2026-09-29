using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IngredientPieceWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PieceWeight",
                table: "IngredientCatalog",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PieceWeight",
                table: "IngredientCatalog");
        }
    }
}

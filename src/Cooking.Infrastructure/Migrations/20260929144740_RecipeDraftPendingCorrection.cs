using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipeDraftPendingCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingCorrection",
                table: "RecipeDrafts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingCorrection",
                table: "RecipeDrafts");
        }
    }
}

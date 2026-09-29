using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipeDraftVideoSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSourceLoaded",
                table: "RecipeDrafts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaFilePath",
                table: "RecipeDrafts",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "RecipeDrafts",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSourceLoaded",
                table: "RecipeDrafts");

            migrationBuilder.DropColumn(
                name: "MediaFilePath",
                table: "RecipeDrafts");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "RecipeDrafts");
        }
    }
}

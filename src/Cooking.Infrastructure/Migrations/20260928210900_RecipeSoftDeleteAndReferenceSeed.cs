using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecipeSoftDeleteAndReferenceSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalCalories",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalCarbs",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalFat",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TotalProtein",
                table: "Recipes");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Recipes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Complexities",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000002-0000-0000-0000-000000000001"), "Easy" },
                    { new Guid("00000002-0000-0000-0000-000000000002"), "Medium" },
                    { new Guid("00000002-0000-0000-0000-000000000003"), "Hard" }
                });

            migrationBuilder.InsertData(
                table: "DataSources",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000004-0000-0000-0000-000000000001"), "Manual" },
                    { new Guid("00000004-0000-0000-0000-000000000002"), "LLM" },
                    { new Guid("00000004-0000-0000-0000-000000000003"), "FatSecret" }
                });

            migrationBuilder.InsertData(
                table: "IngredientCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000005-0000-0000-0000-000000000001"), "Мясо" },
                    { new Guid("00000005-0000-0000-0000-000000000002"), "Птица" },
                    { new Guid("00000005-0000-0000-0000-000000000003"), "Рыба и морепродукты" },
                    { new Guid("00000005-0000-0000-0000-000000000004"), "Овощи" },
                    { new Guid("00000005-0000-0000-0000-000000000005"), "Фрукты" },
                    { new Guid("00000005-0000-0000-0000-000000000006"), "Крупы и бобовые" },
                    { new Guid("00000005-0000-0000-0000-000000000007"), "Молочные продукты" },
                    { new Guid("00000005-0000-0000-0000-000000000008"), "Яйца" },
                    { new Guid("00000005-0000-0000-0000-000000000009"), "Масла и жиры" },
                    { new Guid("00000005-0000-0000-0000-000000000010"), "Мучное и выпечка" },
                    { new Guid("00000005-0000-0000-0000-000000000011"), "Специи и приправы" },
                    { new Guid("00000005-0000-0000-0000-000000000012"), "Напитки" }
                });

            migrationBuilder.InsertData(
                table: "MeasurementUnits",
                columns: new[] { "Id", "Abbreviation", "Name" },
                values: new object[,]
                {
                    { new Guid("00000003-0000-0000-0000-000000000001"), "г", "Грамм" },
                    { new Guid("00000003-0000-0000-0000-000000000002"), "мл", "Миллилитр" },
                    { new Guid("00000003-0000-0000-0000-000000000003"), "шт", "Штука" },
                    { new Guid("00000003-0000-0000-0000-000000000004"), "ст.л.", "Столовая ложка" },
                    { new Guid("00000003-0000-0000-0000-000000000005"), "ч.л.", "Чайная ложка" },
                    { new Guid("00000003-0000-0000-0000-000000000006"), "стак.", "Стакан" },
                    { new Guid("00000003-0000-0000-0000-000000000007"), "щеп.", "Щепотка" }
                });

            migrationBuilder.InsertData(
                table: "SourceTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), "Manual" },
                    { new Guid("00000001-0000-0000-0000-000000000002"), "Instagram" },
                    { new Guid("00000001-0000-0000-0000-000000000003"), "YouTube" },
                    { new Guid("00000001-0000-0000-0000-000000000004"), "Website" }
                });

            migrationBuilder.InsertData(
                table: "TagTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("00000006-0000-0000-0000-000000000001"), "MealType" },
                    { new Guid("00000006-0000-0000-0000-000000000002"), "Cuisine" },
                    { new Guid("00000006-0000-0000-0000-000000000003"), "CookingMethod" },
                    { new Guid("00000006-0000-0000-0000-000000000004"), "Diet" }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Name", "TagTypeId" },
                values: new object[,]
                {
                    { new Guid("00000007-0000-0000-0001-000000000001"), "Завтрак", new Guid("00000006-0000-0000-0000-000000000001") },
                    { new Guid("00000007-0000-0000-0001-000000000002"), "Обед", new Guid("00000006-0000-0000-0000-000000000001") },
                    { new Guid("00000007-0000-0000-0001-000000000003"), "Ужин", new Guid("00000006-0000-0000-0000-000000000001") },
                    { new Guid("00000007-0000-0000-0001-000000000004"), "Перекус", new Guid("00000006-0000-0000-0000-000000000001") },
                    { new Guid("00000007-0000-0000-0001-000000000005"), "Десерт", new Guid("00000006-0000-0000-0000-000000000001") },
                    { new Guid("00000007-0000-0000-0002-000000000001"), "Русская", new Guid("00000006-0000-0000-0000-000000000002") },
                    { new Guid("00000007-0000-0000-0002-000000000002"), "Итальянская", new Guid("00000006-0000-0000-0000-000000000002") },
                    { new Guid("00000007-0000-0000-0002-000000000003"), "Азиатская", new Guid("00000006-0000-0000-0000-000000000002") },
                    { new Guid("00000007-0000-0000-0002-000000000004"), "Кавказская", new Guid("00000006-0000-0000-0000-000000000002") },
                    { new Guid("00000007-0000-0000-0002-000000000005"), "Средиземноморская", new Guid("00000006-0000-0000-0000-000000000002") },
                    { new Guid("00000007-0000-0000-0003-000000000001"), "Варка", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000002"), "Жарка", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000003"), "Запекание", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000004"), "Тушение", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000005"), "На пару", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000006"), "Гриль", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0003-000000000007"), "Без готовки", new Guid("00000006-0000-0000-0000-000000000003") },
                    { new Guid("00000007-0000-0000-0004-000000000001"), "Вегетарианское", new Guid("00000006-0000-0000-0000-000000000004") },
                    { new Guid("00000007-0000-0000-0004-000000000002"), "Веганское", new Guid("00000006-0000-0000-0000-000000000004") },
                    { new Guid("00000007-0000-0000-0004-000000000003"), "Высокобелковое", new Guid("00000006-0000-0000-0000-000000000004") },
                    { new Guid("00000007-0000-0000-0004-000000000004"), "Низкоуглеводное", new Guid("00000006-0000-0000-0000-000000000004") },
                    { new Guid("00000007-0000-0000-0004-000000000005"), "Без глютена", new Guid("00000006-0000-0000-0000-000000000004") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Complexities",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Complexities",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Complexities",
                keyColumn: "Id",
                keyValue: new Guid("00000002-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "DataSources",
                keyColumn: "Id",
                keyValue: new Guid("00000004-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "DataSources",
                keyColumn: "Id",
                keyValue: new Guid("00000004-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "DataSources",
                keyColumn: "Id",
                keyValue: new Guid("00000004-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "IngredientCategories",
                keyColumn: "Id",
                keyValue: new Guid("00000005-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "MeasurementUnits",
                keyColumn: "Id",
                keyValue: new Guid("00000003-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "SourceTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0001-000000000001"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0001-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0001-000000000003"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0001-000000000004"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0001-000000000005"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0002-000000000001"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0002-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0002-000000000003"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0002-000000000004"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0002-000000000005"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000001"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000003"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000004"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000005"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000006"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0003-000000000007"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0004-000000000001"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0004-000000000002"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0004-000000000003"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0004-000000000004"));

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: new Guid("00000007-0000-0000-0004-000000000005"));

            migrationBuilder.DeleteData(
                table: "TagTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000006-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "TagTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000006-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "TagTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000006-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "TagTypes",
                keyColumn: "Id",
                keyValue: new Guid("00000006-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Recipes");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "Recipes",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCalories",
                table: "Recipes",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCarbs",
                table: "Recipes",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFat",
                table: "Recipes",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProtein",
                table: "Recipes",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
